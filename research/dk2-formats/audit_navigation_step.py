"""Validate cached original executions, protected PE spans and lossless package."""
from pathlib import Path
import json
from build_navigation_step import ROOT,load,digest,primary,results,corpus
from audit_targeted_spell import executable_ranges
import emulate_navigation_step as native

def main():
    s=load('navigation-step-semantics.json');assert primary()==s['primary']
    rows,out=results();assert rows==s['native_replays'] and out==s['outcomes'];assert corpus()==s['corpus']
    exe=Path(s['profile']['executable']).read_bytes();assert digest(exe)==s['profile']['sha256']
    protected=executable_ranges(exe)+[(r['address'],r['length']) for r in s['primary']['blocks']];by={c['name']:c for c in native.cases()}
    for row in rows:
        args=native.build(by[row['case']])[0];mem=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        spans=sorted((a,len(b)) for a,b in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        assert all(a+len(b)<=p or a>=p+n for a,b in mem for p,n in protected)
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    source=s['current_speed_source'];data=Path(source['file']).read_bytes();assert digest(data)==source['sha256']
    assert data[source['record_offset']:source['record_offset']+16]==bytes.fromhex(source['raw_hex'])
    p=load('editor-format-package.json');prior=load('phase98-prior-components.json');assert len(p['components'])==77 and len(prior)==76
    assert p['navigation_step_semantics']==s and set(prior)==set(p['components'])-{'navigation_step_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    actor=s['scope']['source_actor'];assert digest((ROOT/actor['file']).read_bytes())==actor['sha256']
    e={r['case']['name']:r['expected'] for r in out}
    assert e['open']['xy']==[25528,26624] and e['blocked']['xy']==[22528,26624]
    assert e['diagonal-slide']['velocity_q12']==[2121,0]
    assert e['apply-open']['actor_position']==[25528,26624,8192] and e['apply-zero-blocked-current']['apply_accepted']==0
    assert e['preflight-nav1-off']['preflight']==0 and e['preflight-nav1-on']['preflight']==1
    assert (ROOT/'NAVIGATION_STEP_RU.md').exists() and 'Актуальный этап 98:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=98,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=77,
      previous76_components_preserved_byte_exact=True,source_executables_unchanged=4,original_game_data_written=0,
      live_game_runs=0,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase98-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))

if __name__=='__main__':main()
