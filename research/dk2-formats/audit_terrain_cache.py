"""Audit complete native Terrain-cache evidence and unchanged research inputs."""
from pathlib import Path
from collections import Counter
import json
from build_terrain_cache import ROOT,load,digest,primary
from emulate_terrain_cache import PROFILES,cache_cases,select_cases,build_cache,build_select
from native_terrain_cache import enumerate_id,cache_ids,light_selection

def main():
    s=load('terrain-cache-semantics.json');functions,blocks=primary()
    assert functions==s['primary_program_bytes'] and blocks==s['primary_vslot_data_ranges']
    assert len(functions)==11 and sum(r['executed'] for r in functions)==10 and len(blocks)==6
    canonical={}
    for profile in PROFILES:
        for kind,cases,build in [('cache',cache_cases(),build_cache),('select',select_cases(),build_select)]:
            for c in cases:canonical[(profile,kind,c['name'])]=(c,*build(profile,c))
    assert len(canonical)==44
    counts=Counter();queries=0
    for row in s['native_emulations']:
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());r=saved['result'];key=(row['profile'],row['kind'],row['case'])
        c,args,expected=canonical.pop(key)
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['case']==c and saved['expected']==expected
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
        counts[row['kind']]+=1
        if row['kind']=='cache':queries+=len(expected['enum_queries'])
    assert not canonical and counts==dict(cache=18,select=26) and queries==90
    assert enumerate_id([0,7,0,0,0,0,0,5],8,0)==7
    assert enumerate_id([9,1,0,3],4,2)==0
    assert enumerate_id([0,1],1,0)==0
    assert cache_ids([0,1,0,3],4,4)=={0,1,3}
    assert cache_ids([0,1],2,0xFFFFFFFF)=={0,1}
    for name in ['current-writes-false','fallback38-writes-false']:
        c=next(c for c in select_cases() if c['name']==name)
        ret,selection,called=light_selection(bytes.fromhex(c['tile']),c['flags'],c['fallback_flags'],c['query_result'])
        assert ret==0 and selection is not None and not called
    invalid=[lambda:enumerate_id([0,1],3,0),lambda:enumerate_id([0,1],2,-1),lambda:enumerate_id([0,256],2,0),
        lambda:cache_ids([0,1],2,True),lambda:light_selection(bytes(15),0,0,0),lambda:light_selection(bytes(16),0,0,-1)]
    for fn in invalid:
        try:fn()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
    totals=Counter()
    expected_searches=[(93,1320,10,1,dict(controlFlags=32)),(203,4233,3,0,dict(controlFlags=177,controlRaw19A=30)),
        (69,1423,8,0,dict(controlFlags=27)),(196,4197,3,0,dict(controlFlags=173,controlRaw19A=30))]
    for row,want in zip(s['searches'],expected_searches):
        q=load(row['file']);assert digest((ROOT/row['file']).read_bytes())==row['sha256']
        assert (len(q['producer_roots']),q['root_states'],q['callee_summaries'],q['limited_callees'],dict(Counter(r['field'] for r in q['candidates'])))==want
        assert q['limited_roots']==row['limited_roots']==0
        assert not any(r['limited_argument_contexts'] for r in q['producer_roots'])
        assert len(q['producer_roots'])==row['producer_roots'] and q['root_states']==row['root_states']
        assert dict(Counter(r['field'] for r in q['candidates']))==row['field_candidates']
        totals.update(dict(roots=row['producer_roots'],states=row['root_states'],callees=row['callee_summaries'],limited_callees=row['limited_callees'],controls=len(q['candidates'])))
    assert totals==dict(roots=561,states=11173,callees=24,limited_callees=1,controls=469)
    for row in s['corpus']['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    assert s['corpus']['file_count']==len(s['corpus']['files'])==82 and s['corpus']['records']==3306
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['terrain_lighting_semantics']['phase74_cache']==s
    for key,row in pack['components'].items():
        raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==pack[key]
    v=dict(phase=74,date='2026-10-27',native_complete_compositions=44,cache_initializations=18,selection_helpers=26,additional_native_ordinal_queries=90,
        complete_executed_native_functions=10,static_caller_functions=1,primary_vslot_ranges=6,
        new_terrain_search_roots=561,root_states=11173,callee_summaries=24,limited_callees=1,known_field_candidate_leads=469,
        new_unknown_field_gameplay_meanings=0,source_files_hashchecked=82,source_records=3306,source_executables_unchanged=4,package_components=67,new_raw_patch_cases=0,
        api_invalid_input_checks=6,research_writes_to_game_data=0,modified_gameplay_runs=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase74-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))

if __name__=='__main__':main()
