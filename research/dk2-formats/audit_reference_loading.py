"""Audit original final-pass reference loading, preservation and scope."""
import json
import build_reference_loading as b
import prepare_reference_loading_primary as primary


def main():
    before=b.link('phase198-reference-primary.json');primary.main();assert b.link('phase198-reference-primary.json')==before
    p=b.load('editor-format-package.json');s=b.load(b.SCHEMA);old=b.load('phase198-prior-package-state.json')
    assert s==b.build() and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==176 and len(old['components'])==175
    for key,ref in old['components'].items():assert p['components'][key]==ref and b.link(ref['file'])==ref
    for key,h in old['preserved_top_level'].items():assert b.canonical(p[key])==h
    assert b.link('dk2_formats.py')==old['codec']
    for ref in s['helpers']:assert b.link(ref['file'])==ref
    assert p['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    assert s['counts']['Ghidra_compared_regions']==400 and s['counts']['Ghidra_compared_bytes']==3532000
    text=(b.ROOT/'LEVEL_REFERENCE_LOADING_RU.md').read_text(encoding='utf8')
    for token in ['13 сцен','3532000','15177','245','пакет176','полный конструктор World','Crypt отложен','51EAE3']:
        assert token in text,token
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert (b.ROOT/name).read_text(encoding='utf8').startswith('Актуальный этап198:')
    out=dict(phase=198,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('LEVEL_REFERENCE_LOADING_RU.md'),
        prior_components_preserved=175,codec_unchanged=True,goal_status='active',current_block='Global parameters',Crypt_deferred=True)
    b.dump('phase198-validation.json',out);print(json.dumps(out))


if __name__=='__main__':main()
