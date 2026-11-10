"""Audit integrated directional melee evidence and preservation."""
import json
import build_melee_direction as b


def main():
    p=b.load('editor-format-package.json');s=b.load(b.SCHEMA);old=b.load('phase194-prior-package-state.json')
    assert s==b.build() and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==172 and len(old['components'])==171
    for key,ref in old['components'].items():assert p['components'][key]==ref and b.link(ref['file'])==ref
    for key,h in old['preserved_top_level'].items():assert b.canonical(p[key])==h
    assert b.link('dk2_formats.py')==old['codec']
    for ref in s['helpers']:assert b.link(ref['file'])==ref
    assert s['counts']['new_raw_fields']==0 and not s['contract']['target_facing_read']
    assert p['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    text=(b.ROOT/'BACKSTAB_DIRECTION_RU.md').read_text(encoding='utf8')
    for fragment in ['54 native','683076','4392','172 компонента','6171','не всех32-битных','не исполнялся','Crypt отложен','AutoSaveMap.kld']:
        assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:
        assert (b.ROOT/name).read_text(encoding='utf8').startswith('Актуальный этап194:')
    r=dict(phase=194,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
        documentation=b.link('BACKSTAB_DIRECTION_RU.md'),prior_components_preserved=171,codec_unchanged=True,
        goal_status='active',current_block='Global parameters',Crypt_deferred=True)
    b.dump('phase194-validation.json',r);print(json.dumps(r))


if __name__=='__main__':main()
