"""Audit source preservation, original context results, public arithmetic and package."""
import build_possession_speed_context as b
ROOT,load,dump,sha=b.ROOT,b.load,b.dump,b.sha


def main():
    s=b.build();assert s==load(b.SCHEMA);old=load('phase201-prior-package-state.json');p=load('editor-format-package.json')
    assert len(p['components'])==179 and set(p['components'])==set(old['components'])|{b.KEY}
    assert p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    for k,v in old['components'].items():assert p['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert b.canonical(p[k])==h
    assert old['codec']==b.link('dk2_formats.py')
    doc=(ROOT/'POSSESSION_SPEED_CONTEXT_RU.md').read_text(encoding='utf8')
    for text in ['109','52428','199','200','92','32']:assert text in doc
    dump('phase201-validation.json',dict(phase=201,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('POSSESSION_SPEED_CONTEXT_RU.md'),
        prior_components_preserved=178,codec_unchanged=True,physical_preservation=s['physical_preservation'],goal_status='active',current_block='Global parameters',Crypt_deferred=True))
    print('PASS phase201',s['counts'])


if __name__=='__main__':main()
