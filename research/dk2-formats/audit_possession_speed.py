from pathlib import Path
import json
from build_possession_speed import ROOT,load,digest,primary,results,corpus,current
from audit_targeted_spell import executable_ranges
import emulate_possession_speed as native
def main():
    s=load('possession-speed-semantics.json');assert primary()==s['primary'];rows,out=results();assert rows==s['native_replays'] and out==s['outcomes']
    assert corpus()==s['corpus'] and current()==s['current_source'];exe=Path(s['profile']['executable']).read_bytes();assert digest(exe)==s['profile']['sha256'];code=executable_ranges(exe)
    by={c['name']:c for c in native.cases()}
    for row in rows:
        args=native.build(by[row['case']])[0];mem=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        spans=sorted((a,len(b)) for a,b in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        protected=code+[(r['address'],r['length']) for r in s['primary']['blocks']]
        assert all(a+len(b)<=p or a>=p+n for a,b in mem for p,n in protected)
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');prior=load('phase92-prior-components.json');assert len(package['components'])==71 and len(prior)==70
    assert package['possession_speed_semantics']==s and set(prior)==set(package['components'])-{'possession_speed_semantics'}
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
        if key in prior:assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    src=s['native_scope']['source_actor'];assert digest((ROOT/src['file']).read_bytes())==src['sha256']
    assert (ROOT/'POSSESSION_SPEED_RU.md').exists() and 'Актуальный этап 92:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=92,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=71,previous70_components_preserved_byte_exact=True,
      source_executables_unchanged=4,original_game_data_written=0,live_game_runs=0,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase92-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
