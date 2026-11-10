from pathlib import Path
import json,struct
from build_possession_recharge import ROOT,load,digest,primary,results,corpus,current
from audit_targeted_spell import executable_ranges
import emulate_possession_recharge as native
import native_possession_recharge as api

def main():
    s=load('possession-recharge-semantics.json');assert primary()==s['primary'];rows,out=results();assert rows==s['native_replays'] and out==s['outcomes']
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
    p=load('editor-format-package.json');prior=load('phase96-prior-components.json');assert len(p['components'])==75 and len(prior)==74
    assert p['possession_recharge_semantics']==s and set(prior)==set(p['components'])-{'possession_recharge_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    actor=s['scope']['source_actor'];assert digest((ROOT/actor['file']).read_bytes())==actor['sha256']
    examples={r['case']['name']:r['expected'] for r in out}
    assert [examples[n]['interval'] for n in ['normal','possessed','secondary','both-flags']]==[20,25,12,25]
    assert examples['equal-deadline']['ready']==0 and examples['equal-deadline']['progress']==100 and examples['after-deadline']['ready']==1
    assert examples['denominator-double']['ready']==1 and examples['denominator-double']['progress']==55
    assert examples['scalar256']['interval']==examples['scalar0']['interval']==0
    raw=struct.pack('<4I',210,123,0x11223344,0x55667788);rejected=0
    for bad,value in [(raw[:-1],1),(raw+bytes(1),1),(bytes(16),1),(raw,-1),(raw,0x100000000),(raw,True)]:
        try:api.patch_scalar210(bad,value)
        except ValueError:rejected+=1
        else:raise AssertionError('Invalid scalar210 patch accepted')
    assert rejected==6
    assert (ROOT/'POSSESSION_RECHARGE_RU.md').exists() and 'Актуальный этап 96:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=96,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=75,previous74_components_preserved_byte_exact=True,
      source_executables_unchanged=4,original_game_data_written=0,live_game_runs=0,API_invalid_inputs_rejected=rejected,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase96-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))

if __name__=='__main__':main()
