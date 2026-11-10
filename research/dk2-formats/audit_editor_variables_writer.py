"""Revalidate phase199 artifacts, original sources, opaque bytes and package preservation."""
import json
import build_editor_variables_writer as b
ROOT,load,dump,sha=b.ROOT,b.load,b.dump,b.sha


def main():
    schema=b.build();assert schema==load(b.SCHEMA)
    package=load('editor-format-package.json');old=load('phase199-prior-package-state.json')
    assert len(package['components'])==177 and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert set(package['components'])==set(old['components'])|{b.KEY}
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert old['codec']==b.link('dk2_formats.py')
    doc=(ROOT/'EDITOR_VARIABLES_WRITER_RU.md').read_text(encoding='utf8')
    for s in ['2089020','MaxHealth','StartingHealth','Ghidra','29/186']:assert s in doc
    out=dict(phase=199,passed=True,counts=schema['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('EDITOR_VARIABLES_WRITER_RU.md'),
        physical_preservation=schema['physical_preservation'],prior_components_preserved=176,codec_unchanged=True,goal_status='active',current_block='Global parameters',Crypt_deferred=True)
    dump('phase199-validation.json',out);print('PASS phase199',schema['counts'])


if __name__=='__main__':main()
