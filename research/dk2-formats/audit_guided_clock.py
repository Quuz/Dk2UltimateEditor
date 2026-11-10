"""Audit phase173 correction and preservation of all previous components."""
from copy import deepcopy
from pathlib import Path
import json
import build_guided_clock as b
ROOT=Path(__file__).resolve().parent

def main():
    package=b.load('editor-format-package.json');old=b.load('phase173-prior-package-state.json');schema=b.load(b.SCHEMA)
    assert schema==b.build() and len(package['components'])==151 and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(old['components'])==150 and b.link('dk2_formats.py')==old['codec']
    for k,ref in old['components'].items():assert package['components'][k]==ref and b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    effective=deepcopy(package['effective_shot_parameters']);ext=effective['combination_rules'].pop('guided_clock_correction')
    assert effective==old['effective_shot_parameters'] and ext['source']==b.link(b.SCHEMA) and not ext['block_complete']
    for ref in schema['helpers']+schema['storage_validation']['inherited_checks']:assert b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    result=dict(phase=173,passed=True,counts=schema['counts'],package=b.link('editor-format-package.json'),schema=b.link(b.SCHEMA),
        preserved_prior_components=150,prior_codec_unchanged=True,all_other_effective_shot_parameters_unchanged=True,all_other_package_top_level_unchanged=True,
        source_writes=0,new_physical_fields=0,new_exact_edits=0,first_priority_block_complete=False,goal_status='active')
    b.dump('phase173-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
