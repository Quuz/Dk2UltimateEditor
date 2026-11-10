"""Audit phase172 publication, complete checked contexts and prior preservation."""
from copy import deepcopy
from pathlib import Path
import json
import build_shot_instant_experience as b
import validate_shot_instant_storage as storage
ROOT=Path(__file__).resolve().parent

def main():
    package=b.load('editor-format-package.json');old=b.load('phase172-prior-package-state.json')
    schema=b.load(b.SCHEMA);assert schema==b.build()
    assert len(package['components'])==150 and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(old['components'])==149
    for key,ref in old['components'].items():assert package['components'][key]==ref and b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    assert b.link('dk2_formats.py')==old['codec']
    for key,value in old['preserved_top_level'].items():assert b.canonical(package[key])==value
    effective=deepcopy(package['effective_shot_parameters']);ext=effective['combination_rules'].pop('instant_experience_extension')
    assert effective==old['effective_shot_parameters'] and ext['source']==b.link(b.SCHEMA) and not ext['block_complete']
    assert storage.validate()==schema['storage_validation']
    for ref in schema['helpers']:assert b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    result=dict(phase=172,passed=True,counts=schema['counts'],package=b.link('editor-format-package.json'),schema=b.link(b.SCHEMA),
        evidence=b.link('phase172-search-status.json'),preserved_prior_components=149,prior_codec_unchanged=True,
        all_other_effective_shot_parameters_unchanged=True,all_other_package_top_level_unchanged=True,
        source_writes=0,first_priority_block_complete=False,goal_status='active',readiness='Expertestimateabout75%,notmeasuredbytecoverage.')
    b.dump('phase172-validation.json',result);print(json.dumps(result,ensure_ascii=False))

if __name__=='__main__':main()
