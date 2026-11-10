"""Audit phase179 evidence/preservation; full Crypt/format goal stays active."""
import json
import build_crypt_object_lifecycle as b

def main():
    package=b.load('editor-format-package.json');old=b.load('phase179-prior-package-state.json');schema=b.load(b.SCHEMA)
    assert schema==b.build() and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==157 and len(old['components'])==156
    for k,ref in old['components'].items():assert package['components'][k]==ref and b.sha((b.ROOT/ref['file']).read_bytes())==ref['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for ref in schema['helpers']:assert b.sha((b.ROOT/ref['file']).read_bytes())==ref['sha256']
    assert schema['counts']['Crypt_groups_remaining']==10 and not schema['priority']['block_complete']
    result=dict(phase=179,passed=True,counts=schema['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
        preserved_prior_components=156,codec_and_effective_parameters_preserved=True,all_other_package_top_level_preserved=True,
        source_writes=0,Crypt_block_complete=False,goal_status='active')
    b.dump('phase179-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
