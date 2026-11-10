"""Audit Variables19 semantics, exact edits and preservation of older components."""
import json
import build_tick_rate as b


def main():
    p=b.load('editor-format-package.json');s=b.load(b.SCHEMA);old=b.load('phase195-prior-package-state.json')
    assert s==b.build() and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==173 and len(old['components'])==172
    for key,ref in old['components'].items():assert p['components'][key]==ref and b.link(ref['file'])==ref
    for key,h in old['preserved_top_level'].items():assert b.canonical(p[key])==h
    assert b.link('dk2_formats.py')==old['codec']
    for ref in s['helpers']:assert b.link(ref['file'])==ref
    assert s['field']['type']==19 and s['field']['Value_bytes']==4 and not s['field']['source_record_present']
    assert s['Level']['raw_bytes']==2 and s['Level']['status']=='static_runtime_writer_plus_current_physical_field_edits'
    assert p['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    text=(b.ROOT/'SIMULATION_TICK_RATE_RU.md').read_text(encoding='utf8')
    for fragment in ['12 native','580936','11930','173 компонента','621→622','не установлен','только статически','Crypt отложен']:
        assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:
        assert (b.ROOT/name).read_text(encoding='utf8').startswith('Актуальный этап195:')
    r=dict(phase=195,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
        documentation=b.link('SIMULATION_TICK_RATE_RU.md'),prior_components_preserved=172,codec_unchanged=True,
        goal_status='active',current_block='Global parameters',Crypt_deferred=True)
    b.dump('phase195-validation.json',r);print(json.dumps(r))


if __name__=='__main__':main()
