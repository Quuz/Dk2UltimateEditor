"""Audit phase176 source/cause proof, package preservation and bounded handoff."""
from pathlib import Path
from copy import deepcopy
import json
import build_shot_chainfire as b
ROOT=Path(__file__).resolve().parent

def main():
    package=b.load('editor-format-package.json');old=b.load('phase176-prior-package-state.json');schema=b.load(b.SCHEMA);summ=b.load(b.SUMMARY)
    assert summ==b.summary() and schema==b.build()
    assert len(package['components'])==154 and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(old['components'])==153 and b.link('dk2_formats.py')==old['codec']
    for k,ref in old['components'].items():assert package['components'][k]==ref and b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    effective=deepcopy(package['effective_shot_parameters']);ext=effective['combination_rules'].pop('chainfire_event8_extension')
    assert effective==old['effective_shot_parameters'] and ext['source']==b.link(b.SCHEMA) and ext['summary']==b.link(b.SUMMARY) and ext['current_priority_block_review_complete']
    for ref in schema['helpers']:assert b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    assert summ['priority_handoff']['next']=='Crypt groups' and not summ['priority_handoff']['full_goal_complete'] and summ['residual_questions']
    result=dict(phase=176,passed=True,counts=schema['counts'],coverage_counts=summ['counts'],package=b.link('editor-format-package.json'),schema=b.link(b.SCHEMA),summary=b.link(b.SUMMARY),preserved_prior_components=153,
        prior_codec_unchanged=True,all_other_effective_shot_parameters_unchanged=True,all_other_package_top_level_unchanged=True,source_writes=0,current_priority_block_review_complete=True,next_block='Crypt groups',goal_status='active')
    b.dump('phase176-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
