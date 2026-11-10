from pathlib import Path
import json,subprocess,sys
from build_unknown_boundary_review import ROOT,load,digest,primary,review,sources
def main():
    # Recompute the small bounded staticproof,not the historical broadscan.
    subprocess.run([sys.executable,str(ROOT/'review_old_object_boundaries.py')],check=True)
    s=load('unknown-instruction-boundary-review.json');assert primary()==s['primary'];r,c=review();assert r==s['review'] and c==s['classification_counts'];assert sources()==s['corpus']
    assert digest(Path(s['profile']['original_file']).read_bytes())==s['profile']['sha256']
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    p=load('editor-format-package.json');prior=load('phase100-prior-components.json');assert len(p['components'])==79 and len(prior)==78
    assert p['unknown_instruction_boundary_review']==s and set(prior)==set(p['components'])-{'unknown_instruction_boundary_review'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for row in s['evidence']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert (ROOT/'UNKNOWN_BOUNDARIES_RU.md').exists() and 'Актуальный этап 100:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=100,date='2026-10-28',**s['counts'],previous_goal_turn_classification='progress',package_components=79,
      previous78_components_preserved_byte_exact=True,source_files_checked=s['corpus']['file_count'],source_executables_unchanged=4,
      original_game_data_written=0,live_game_runs=0,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase100-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
