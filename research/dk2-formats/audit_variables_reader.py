"""Audit phase197 evidence, preservation and explicit scope boundaries."""
import json
import build_variables_reader as b
import prepare_variables_reader_primary as primary


def main():
    before=b.link('phase197-Variables-primary.json');primary.main();assert b.link('phase197-Variables-primary.json')==before
    p=b.load('editor-format-package.json');s=b.load(b.SCHEMA);old=b.load('phase197-prior-package-state.json')
    assert s==b.build() and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==175 and len(old['components'])==174
    for key,ref in old['components'].items():assert p['components'][key]==ref and b.link(ref['file'])==ref
    for key,h in old['preserved_top_level'].items():assert b.canonical(p[key])==h
    assert b.link('dk2_formats.py')==old['codec']
    for ref in s['helpers']:assert b.link(ref['file'])==ref
    assert p['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    assert s['counts']['Ghidra_compared_regions']==176 and s['counts']['Ghidra_compared_bytes']==518588
    text=(b.ROOT/'VARIABLES_READER_ORDER_RU.md').read_text(encoding='utf8')
    for token in ['23 сцены','518588','12429','621','198','пакет175','Полный конструктор World не выполнялся','Crypt отложен']:
        assert token in text,token
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert (b.ROOT/name).read_text(encoding='utf8').startswith('Актуальный этап197:')
    out=dict(phase=197,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('VARIABLES_READER_ORDER_RU.md'),
        prior_components_preserved=174,codec_unchanged=True,goal_status='active',current_block='Global parameters',Crypt_deferred=True)
    b.dump('phase197-validation.json',out);print(json.dumps(out))


if __name__=='__main__':main()
