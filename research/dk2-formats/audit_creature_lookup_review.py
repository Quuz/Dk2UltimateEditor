"""Audit full native arrays/returns, search bounds and original preservation."""
from pathlib import Path
import json
from collections import Counter
from build_creature_lookup_review import ROOT,load,digest,primary
from emulate_creature_lookup import cases,build_case
from native_creature_lookup import lookup_index


def main():
    s=load('creature-lookup-review.json');functions,blocks=primary()
    assert s['primary_program_bytes']==functions and s['primary_vslot_data_ranges']==blocks
    assert len(cases())==len(s['native_emulations'])==34
    for c,row in zip(cases(),s['native_emulations']):
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());args,want,native=build_case(c);r=saved['result']
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['case']==json.loads(json.dumps(c))
        assert saved['expected_registers']==want and saved['expected_native_return']==native
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items())
        assert lookup_index(c['rows'],c['key'],c['route'])==c['wanted_index']
        m={int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in m.items())
    invalid=[lambda:lookup_index([(1,1,0)],-1,'raw'),lambda:lookup_index([(1,1,0)],1,'other'),
        lambda:lookup_index([(256,1,0)],1,'raw'),lambda:lookup_index([(1,True,0)],1,'raw')]
    for fn in invalid:
        try:fn()
        except ValueError:pass
        else:raise AssertionError('Invalidlookupinputaccepted')
    for row in s['searches']:
        assert digest((ROOT/row['file']).read_bytes())==row['sha256'];q=load(row['file'])
        assert len(q['producer_roots'])==row['producer_roots'] and q['root_states']==row['root_states']
        assert dict(Counter(r['field'] for r in q['candidates']))==row['field_candidates']
    a,b=s['searches'];assert (a['producer_roots'],a['root_states'],a['callee_summaries'],a['limited_callees'])==(739,12032,48,1)
    assert a['field_candidates']==dict(controlCreatureFlags=230,controlHealth=127)
    assert (b['producer_roots'],b['root_states'],b['callee_summaries'],b['limited_callees'])==(246,3828,17,1)
    assert b['field_candidates']==dict(controlHealth=3)
    exposure=load(s['pointer_exposure']['file']);assert digest((ROOT/s['pointer_exposure']['file']).read_bytes())==s['pointer_exposure']['sha256']
    assert [len(p['candidates']) for p in exposure['profiles']]==[9,9]
    assert all(r['address_taking_or_indexed_or_immediate_candidates']==0 for r in s['pointer_exposure']['counts_by_exe'])
    files=0
    for row in s['corpus']['files']:assert digest(Path(row['file']).read_bytes())==row['sha256'];files+=1
    assert files==82
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['catalogue_loading']['phase73_creature_lookup']==s
    for key,row in pack['components'].items():
        raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==pack[key]
    v=dict(phase=73,date='2026-10-27',native_full_getters=34,effective_calls=17,raw_calls=17,complete_executed_native_functions=3,primary_vslot_ranges=2,
        current_creature_search_roots=739,creature_root_states=12032,creature_callee_summaries=48,limited_creature_callees=1,
        object_priority_search_roots=246,object_root_states=3828,object_callee_summaries=17,limited_object_callees=1,known_field_leads=360,
        new_jump_or_priority_semantic_consumers=0,source_files_hashchecked=82,source_executables_unchanged=4,package_components=67,new_raw_patch_cases=0,
        api_invalid_input_checks=4,research_writes_to_game_data=0,modified_gameplay_runs=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase73-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))


if __name__=='__main__':main()
