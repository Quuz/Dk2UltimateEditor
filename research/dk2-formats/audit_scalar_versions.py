from pathlib import Path
import json
from build_scalar_versions import ROOT,load,digest,primary,results,corpus,bindings
from audit_targeted_spell import executable_ranges
import emulate_possession_scalar_versions as native
def main():
    s=load('possession-scalar-version-semantics.json');assert primary()==s['primary'];rows,out=results();assert rows==s['native_replays'] and out==s['outcomes'] and corpus()==s['corpus'] and bindings()==s['profiles']
    by={c['name']:c for c in native.cases()}
    for row in rows:
        c=by[row['case']];p=next(p for p in s['profiles'] if p['profile']==c['profile']);exe=Path(p['exe']).read_bytes();assert digest(exe)==p['sha256']
        protect=executable_ranges(exe)+[(r['address'],r['length']) for r in s['primary']['blocks'] if r['program']==native.PROFILES[c['profile']]['program']]
        args=native.build(c)[0];regions=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        spans=sorted((a,len(b)) for a,b in regions);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        assert all(a+len(b)<=p or a>=p+n for a,b in regions for p,n in protect)
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    p=load('editor-format-package.json');prior=load('phase101-prior-components.json');assert len(p['components'])==80 and len(prior)==79
    assert p['possession_scalar_version_semantics']==s and set(prior)==set(p['components'])-{'possession_scalar_version_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['helpers']+[s['search']['evidence']]:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert (ROOT/'POSSESSION_SCALARS_RU.md').exists() and 'Актуальный этап 101:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=101,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=80,previous79_components_preserved_byte_exact=True,
      source_executables_unchanged=4,original_game_data_written=0,live_game_runs=0,original_game_function_or_vtable_patches=0,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase101-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
