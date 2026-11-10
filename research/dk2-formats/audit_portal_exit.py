"""Audit portal semantics against native result caches, raw bytes and package."""
from pathlib import Path
import json
from build_portal_exit import ROOT,load,digest,primary,native_results,corpus,examples,refusals
from audit_targeted_spell import executable_ranges
from build_effect_semantics import pe_read
import emulate_portal_exit as native
def main():
    s=load('portal-exit-semantics.json');assert primary()==s['primary']
    rows,outcomes,images=native_results();assert rows==s['native_replays'] and outcomes==s['outcomes']
    assert corpus()==s['corpus'] and examples()==s['current_examples'] and refusals()==s['api']['refusals']==8
    exe=Path(s['profile']['executable']).read_bytes();assert digest(exe)==s['profile']['sha256'];protected=executable_ranges(exe);by={c['name']:c for c in native.cases()}
    for row in rows:
        args=native.build(by[row['case']],row['page'])[0];mem=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        spans=sorted((a,len(b)) for a,b in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        assert all(a+len(b)<=p or a>=p+n for a,b in mem for p,n in protected)
        for a,b in mem:
            for block in s['primary']['blocks']:
                start=max(a,block['address']);end=min(a+len(b),block['address']+block['length'])
                if start<end:assert block['label']=='AI-category-initial-data' and b[start-a:end-a]==pe_read(exe,start,end-start)
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    p=load('editor-format-package.json');prior=load('phase91-prior-components.json');assert len(p['components'])==70 and len(prior)==69
    assert p['portal_exit_semantics']==s and set(prior)==set(p['components'])-{'portal_exit_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert (ROOT/'PORTAL_EXIT_RU.md').exists() and 'Актуальный этап 91:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=91,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',previous69_components_preserved_byte_exact=True,
      package_components=70,source_executables_unchanged=4,original_game_data_written=0,live_game_runs=0,accepted_memory_probes=0,
      goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase91-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
