"""Validate original map-producing executions and immutable earlier schemas."""
from pathlib import Path
import json
from build_navigation_map import ROOT,load,digest,primary,results,corpus,examples
from audit_targeted_spell import executable_ranges
import emulate_navigation_map as native

def main():
    s=load('navigation-map-semantics.json');assert primary()==s['primary'];rows,out=results()
    assert rows==s['native_replays'] and out==s['outcomes'] and corpus()==s['corpus'] and examples()==s['current_source']
    exe=Path(s['profile']['executable']).read_bytes();assert digest(exe)==s['profile']['sha256']
    protected=executable_ranges(exe)+[(r['address'],r['length']) for r in s['primary']['blocks']];by={c['name']:c for c in native.cases()}
    for row in rows:
        args=native.build(by[row['case']])[0];mem=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        spans=sorted((a,len(b)) for a,b in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        assert all(a+len(b)<=p or a>=p+n for a,b in mem for p,n in protected)
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    p=load('editor-format-package.json');prior=load('phase99-prior-components.json');assert len(p['components'])==78 and len(prior)==77
    assert p['navigation_map_semantics']==s and set(prior)==set(p['components'])-{'navigation_map_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    actor=s['scope']['source_actor'];assert digest((ROOT/actor['file']).read_bytes())==actor['sha256']
    e={r['case']['name']:r['expected'] for r in out}
    assert e['raw-water-blocked']['xy']==[22528,26624] and e['raw-water-allowed']['xy']==[25528,26624]
    assert e['raw-lava-blocked']['xy']==[22528,26624] and e['raw-lava-allowed']['xy']==[25528,26624]
    assert e['raw-both-water-first']['terrain_navigation_byte']==1 and e['raw-solid-both']['terrain_navigation_byte']==8
    assert (ROOT/'NAVIGATION_MAP_RU.md').exists() and 'Актуальный этап 99:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=99,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=78,
      previous77_components_preserved_byte_exact=True,source_executables_unchanged=4,original_game_data_written=0,
      live_game_runs=0,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase99-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))

if __name__=='__main__':main()
