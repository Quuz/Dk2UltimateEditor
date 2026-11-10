"""Audit phase200 byte attestations, effects, raw preservation and package integrity."""
import build_global_table_indices as b
ROOT,load,dump,sha=b.ROOT,b.load,b.dump,b.sha


def main():
    s=b.build();assert s==load(b.SCHEMA);old=load('phase200-prior-package-state.json');p=load('editor-format-package.json')
    assert len(p['components'])==178 and set(p['components'])==set(old['components'])|{b.KEY}
    assert p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    for k,v in old['components'].items():assert p['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert b.canonical(p[k])==h
    assert old['codec']==b.link('dk2_formats.py')
    doc=(ROOT/'GLOBAL_TABLE_INDICES_RU.md').read_text(encoding='utf8')
    for phrase in ['StatId=31','Level=11','29/186','512','без проверки']:assert phrase in doc
    dump('phase200-validation.json',dict(phase=200,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('GLOBAL_TABLE_INDICES_RU.md'),
        physical_preservation=s['physical_preservation'],prior_components_preserved=177,codec_unchanged=True,goal_status='active',current_block='Global parameters',Crypt_deferred=True))
    print('PASS phase200',s['counts'])


if __name__=='__main__':main()
