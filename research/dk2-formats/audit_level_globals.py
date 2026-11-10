"""Audit accepted full Level semantics and unchanged prior format components."""
import json
import build_level_globals as b
import prepare_level_globals_primary as primary


def main():
    old_primary=b.link('phase196-Level-primary.json');primary.main();assert b.link('phase196-Level-primary.json')==old_primary
    p=b.load('editor-format-package.json');s=b.load(b.SCHEMA);old=b.load('phase196-prior-package-state.json')
    assert s==b.build() and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==174 and len(old['components'])==173
    for key,ref in old['components'].items():assert p['components'][key]==ref and b.link(ref['file'])==ref
    for key,h in old['preserved_top_level'].items():assert b.canonical(p[key])==h
    assert b.link('dk2_formats.py')==old['codec']
    for ref in s['helpers']:assert b.link(ref['file'])==ref
    assert s['narrowing']['physical_bytes']==2 and s['narrowing']['runtime_bytes']==1
    assert p['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    text=(b.ROOT/'LEVEL_GLOBAL_PARAMETERS_RU.md').read_text(encoding='utf8')
    for token in ['Девять native','590997','12745','68 точечных','пакет174','только статически','Crypt отложен','не универсальное правило']:
        assert token in text,token
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert (b.ROOT/name).read_text(encoding='utf8').startswith('Актуальный этап196:')
    r=dict(phase=196,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('LEVEL_GLOBAL_PARAMETERS_RU.md'),prior_components_preserved=173,codec_unchanged=True,goal_status='active',current_block='Global parameters',Crypt_deferred=True)
    b.dump('phase196-validation.json',r);print(json.dumps(r))


if __name__=='__main__':main()
