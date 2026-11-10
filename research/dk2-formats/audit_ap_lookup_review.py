"""Audit AP getter/stack evidence and positive control for pointer-lifetime pass."""
from pathlib import Path
from collections import Counter
import json
from build_ap_lookup_review import ROOT,load,digest,primary
from emulate_ap_lookup import cases,build
from native_ap_lookup import lookup_tag

def main():
    s=load('ap-lookup-review.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_vslot_data_ranges']
    assert len(functions)==5 and sum(r['executed'] for r in functions)==4 and len(blocks)==3
    canonical={(c['profile'],c['name']):c for c in cases()};kinds=Counter()
    for row in s['native_emulations']:
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());c=canonical.pop((row['profile'],row['case']));args,expected=build(c);r=saved['result']
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['case']==json.loads(json.dumps(c)) and saved['expected']==expected
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
        kinds['contract' if c['contract'] else c['profile']]+=1
    assert not canonical and kinds==dict(DX=16,old=16,contract=4)
    assert lookup_tag({5:(7,9),9:(7,0)},5,7)==5
    assert lookup_tag({0:(0,0)},0,0)==0
    assert lookup_tag({65535:(7,0)},65535,0x12340007)==65535
    invalid=[lambda:lookup_tag({},65536,0),lambda:lookup_tag({},0,-1),lambda:lookup_tag({1:(256,0)},1,0),lambda:lookup_tag({1:(0,65536)},1,0),lambda:lookup_tag({True:(1,0)},1,1),lambda:lookup_tag({1:(1,1)},1,2)]
    for fn in invalid:
        try:fn()
        except ValueError:pass
        else:raise AssertionError('Invalid/cyclic fixture accepted')
    review=s['search_review'];q=load(review['file']);assert digest((ROOT/review['file']).read_bytes())==review['sha256']
    assert len(q['producer_roots'])==8 and q['root_states']==351 and q['limited_roots']==q['limited_callees']==0 and q['callee_summaries']==0
    fields=dict(Counter(r['field'] for r in q['candidates']));assert fields==review['field_candidates']
    assert len(q['candidates'])==28 and fields['possibleAPFlags']==1
    control=next(r for r in q['candidates'] if r['field']=='possibleAPFlags');assert control['read']['address']=='0x47205e' and control['root']['address']=='0x471ed0'
    baseline=load(review['baseline']['file']);assert digest((ROOT/review['baseline']['file']).read_bytes())==review['baseline']['sha256']
    assert not [r for r in baseline['candidates'] if r['field']=='possibleAPFlags' and r['root']['producer']!='global-tag-table-untyped']
    extension=review['tag_table_extension'];tag_search=load(extension['file']);assert digest((ROOT/extension['file']).read_bytes())==extension['sha256']
    assert len(tag_search['producer_roots'])==extension['producer_roots']==2623 and tag_search['root_states']==extension['root_states']==135428
    assert tag_search['callee_summaries']==extension['callee_summaries']==614 and tag_search['limited_callees']==extension['limited_callees']==28
    assert tag_search['limited_roots']==extension['limited_roots']==8
    assert dict(Counter(r['field'] for r in tag_search['candidates']))==extension['field_candidates']==dict(controlBounds=241,possibleAPFlags=20,controlNext=14)
    before={r['read']['address'] for r in baseline['candidates'] if r['field']=='possibleAPFlags'}
    after={r['read']['address'] for r in tag_search['candidates'] if r['field']=='possibleAPFlags'}
    assert after-before=={'0x47205e'} and extension['new_unique_flag_read_addresses']==['0x47205e']
    patrol=(ROOT/'ghidra/phase76-final-DX-471eb0-assembly.txt').read_text();assert all(x in patrol for x in ['00471ef8: MOV dword ptr [ESP + 0x20],EAX','00472053: MOV ECX,dword ptr [ESP + 0x20]','0047205e: MOV DL,byte ptr [ECX + 0x32]','00472061: TEST DL,0x40'])
    for row in s['corpus']['files']:assert digest(Path(row.get('input_path',row['file'])).read_bytes())==row['sha256']
    assert s['corpus']['file_count']==len(s['corpus']['files'])==106 and s['corpus']['records']==3501
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['action_point_semantics']['phase76_lookup_review']==s
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=76,date='2026-10-28',native_compositions=36,standalone_AP_getters=32,DX_stack_contract_compositions=4,
        complete_executed_native_functions=4,static_positive_control_functions=1,primary_vslot_ranges=3,
        lookup_search_roots=8,root_states=351,limited_roots=0,callee_summaries=0,known_AP_flags_control_recovered=1,
        extended_tag_search_roots=2623,extended_root_states=135428,extended_limited_roots=8,extended_callee_summaries=614,extended_limited_callees=28,
        extended_tag_candidate_leads=275,
        candidate_leads=28,new_AP04_08_meanings=0,api_invalid_and_cycle_guard_cases=6,
        AP_corpus_inputs_hashchecked=106,source_AP_records=3501,source_executables_unchanged=4,package_components=67,new_raw_patch_cases=0,
        research_writes_to_game_data=0,modified_gameplay_runs=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase76-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))

if __name__=='__main__':main()
