from pathlib import Path
import json,struct
from build_possession_neighbours import ROOT,load,digest,primary,results,corpus,current
from audit_targeted_spell import executable_ranges
import emulate_possession_neighbours as native
import native_possession_neighbours as api

def main():
    s=load('possession-neighbour-semantics.json');assert primary()==s['primary'];rows,out=results();assert rows==s['native_replays'] and out==s['outcomes']
    assert corpus()==s['corpus'] and current()==s['current_sources']
    exe=Path(s['profile']['executable']).read_bytes();assert digest(exe)==s['profile']['sha256']
    protected=executable_ranges(exe)+[(r['address'],r['length']) for r in s['primary']['blocks']];by={c['name']:c for c in native.cases()}
    for row in rows:
        args=native.build(by[row['case']])[0];mem=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        spans=sorted((a,len(b)) for a,b in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        assert all(a+len(b)<=p or a>=p+n for a,b in mem for p,n in protected)
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    p=load('editor-format-package.json');prior=load('phase95-prior-components.json');assert len(p['components'])==74 and len(prior)==73
    assert p['possession_neighbour_semantics']==s and set(prior)==set(p['components'])-{'possession_neighbour_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    actor=s['scope']['source_actor'];assert digest((ROOT/actor['file']).read_bytes())==actor['sha256']
    examples={r['case']['name']:r['expected'] for r in out}
    assert examples['open']['speed']==examples['raw-current-door-flag40']['speed']==6144
    assert examples['blocked']['speed']==examples['raw-current-door-blocked']['speed']==4096
    assert examples['clear-unpossessed']['target_bit08']==examples['clear-unpossessed']['source_bit08']==0
    assert examples['skip-source']['source_bit08']==8 and examples['target-also-possessed']['speed']==12288
    # API refuses ambiguous flag types/record lengths rather than silently coercing them.
    rejected=0
    for raw,enabled in [(bytes(615),True),(bytes(617),False),(bytes(616),1),(bytes(616),'yes')]:
        try:api.patch_door_bit40(raw,enabled)
        except ValueError:rejected+=1
        else:raise AssertionError('Invalid raw flag patch accepted')
    assert rejected==4
    assert (ROOT/'POSSESSION_NEIGHBOURS_RU.md').exists() and 'Актуальный этап 95:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=95,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=74,previous73_components_preserved_byte_exact=True,
      source_executables_unchanged=4,original_game_data_written=0,live_game_runs=0,API_invalid_inputs_rejected=rejected,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase95-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))

if __name__=='__main__':main()
