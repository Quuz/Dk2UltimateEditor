"""Audit phase175 semantic publication, evidence links and byte preservation."""
from pathlib import Path
from copy import deepcopy
import json
import build_shot_source_status as b
ROOT=Path(__file__).resolve().parent

def main():
    package=b.load('editor-format-package.json');old=b.load('phase175-prior-package-state.json');schema=b.load(b.SCHEMA)
    assert b.load(b.MATRIX)==b.coverage() and schema==b.build()
    assert len(package['components'])==153 and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(old['components'])==152 and b.link('dk2_formats.py')==old['codec']
    for k,ref in old['components'].items():assert package['components'][k]==ref and b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    effective=deepcopy(package['effective_shot_parameters']);ext=effective['combination_rules'].pop('source_status_extension')
    assert effective==old['effective_shot_parameters'] and ext['source']==b.link(b.SCHEMA) and ext['coverage']==b.link(b.MATRIX) and not ext['block_complete']
    for ref in schema['helpers']:assert b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    result=dict(phase=175,passed=True,counts=schema['counts'],coverage_counts=b.load(b.MATRIX)['counts'],package=b.link('editor-format-package.json'),schema=b.link(b.SCHEMA),matrix=b.link(b.MATRIX),
        preserved_prior_components=152,prior_codec_unchanged=True,all_other_effective_shot_parameters_unchanged=True,all_other_package_top_level_unchanged=True,source_writes=0,first_priority_block_complete=False,goal_status='active')
    b.dump('phase175-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
