"""Audit phase174 publication, primary evidence and preservation."""
from pathlib import Path
from copy import deepcopy
import json
import build_shot_child_chain as b
import validate_shot_child_storage as storage
ROOT=Path(__file__).resolve().parent

def main():
    package=b.load('editor-format-package.json');old=b.load('phase174-prior-package-state.json');schema=b.load(b.SCHEMA)
    assert schema==b.build() and len(package['components'])==152 and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(old['components'])==151 and b.link('dk2_formats.py')==old['codec']
    for k,ref in old['components'].items():assert package['components'][k]==ref and b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    effective=deepcopy(package['effective_shot_parameters']);ext=effective['combination_rules'].pop('child_chain_extension')
    assert effective==old['effective_shot_parameters'] and ext['source']==b.link(b.SCHEMA) and not ext['block_complete']
    assert storage.validate()==schema['storage_validation']
    for ref in schema['helpers']:assert b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    result=dict(phase=174,passed=True,counts=schema['counts'],package=b.link('editor-format-package.json'),schema=b.link(b.SCHEMA),preserved_prior_components=151,
        prior_codec_unchanged=True,all_other_effective_shot_parameters_unchanged=True,all_other_package_top_level_unchanged=True,source_writes=0,first_priority_block_complete=False,goal_status='active')
    b.dump('phase174-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
