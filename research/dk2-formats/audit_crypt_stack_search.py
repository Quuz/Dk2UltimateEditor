"""Audit phase177; no bonus assigned by a negative bounded search."""
from pathlib import Path
import json
import build_crypt_stack_search as b
ROOT=Path(__file__).resolve().parent

def main():
    package=b.load('editor-format-package.json');old=b.load('phase177-prior-package-state.json');schema=b.load(b.SCHEMA)
    assert schema==b.build() and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA) and len(package['components'])==155
    for k,ref in old['components'].items():assert package['components'][k]==ref and b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    assert len(old['components'])==154 and b.link('dk2_formats.py')==old['codec']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    for ref in schema['helpers']:assert b.sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    assert schema['conclusion']['new_Crypt_group_meanings']==0 and not schema['conclusion']['global_nonuse_proved']
    result=dict(phase=177,passed=True,counts=schema['counts'],package=b.link('editor-format-package.json'),schema=b.link(b.SCHEMA),preserved_prior_components=154,
        prior_codec_unchanged=True,all_other_package_top_level_unchanged=True,all_effective_parameters_unchanged=True,source_writes=0,Crypt_block_complete=False,goal_status='active')
    b.dump('phase177-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
