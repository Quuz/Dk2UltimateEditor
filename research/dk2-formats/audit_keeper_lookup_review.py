"""Verify native pointers, lookup diagnostics, search bounds and source integrity."""
from pathlib import Path
import json
from build_keeper_lookup_review import ROOT,load,digest,primary
from emulate_keeper_lookup import cases,build_case
from native_keeper_lookup import lookup_index


def main():
    s=load('keeper-lookup-review.json');functions,blocks=primary()
    assert s['primary_program_bytes']==functions and s['primary_data_and_code_fragments']==blocks
    assert len(cases())==len(s['native_emulations'])==26
    for c,row in zip(cases(),s['native_emulations']):
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());args,want,native=build_case(c);r=saved['result']
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['expected_native_return']==native and saved['case']==c
        assert saved['expected_registers']==want and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items())
        assert lookup_index(c['ids'],c['key'])==c['wanted_index']
        m={int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        for fn in functions:
            if fn['program']==args['program']:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in m.items())
    invalid=[lambda:lookup_index([1,True],1),lambda:lookup_index([256],1),lambda:lookup_index([1],-1),lambda:lookup_index([1],False)]
    for fn in invalid:
        try:fn()
        except ValueError:pass
        else:raise AssertionError('Invalid lookup API input accepted')
    for row in s['consumer_search']:
        assert digest((ROOT/row['file']).read_bytes())==row['sha256'];q=load(row['file'])
        assert len(q['producer_roots'])==row['producer_roots'] and q['root_states']==row['root_states']
        assert row['limited_roots']==row['limited_callees']==row['limited_argument_context_roots']==row['unknown_C8_candidates']==0
        assert len(q['candidates'])==row['control_candidates']==27
    for row in s['source_tools']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    files=0
    for row in s['corpus']['files']:assert digest(Path(row['file']).read_bytes())==row['sha256'];files+=1
    assert files==82
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['keeper_pipeline_semantics']['phase72_lookup_review']==s
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=72,date='2026-10-27',native_lookup_cases=26,complete_executed_native_functions=2,primary_data_and_code_fragments=2,
        DX_search_roots=45,old_search_roots=48,DX_root_states=835,old_root_states=711,DX_callee_summaries=9,old_callee_summaries=5,
        control_read_leads=54,unknown_C8_read_leads=0,saturated_budget_limits=0,api_invalid_input_checks=4,
        source_files_hashchecked=82,source_executables_unchanged=4,package_components=67,new_raw_patch_cases=0,
        research_writes_to_game_data=0,modified_gameplay_runs=0,KeeperC8_gameplay_still_open=True,limitations=s['limitations'],search_limitations=s['search_limitations'])
    (ROOT/'phase72-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k not in ['limitations','search_limitations']}))


if __name__=='__main__':main()
