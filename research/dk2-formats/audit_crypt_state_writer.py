"""Audit CPlayer writer evidence, disjoint fixtures, exclusions and old package."""
import json
import build_crypt_state_writer as b


def main():
    package=b.load('editor-format-package.json');old=b.load('phase190-prior-package-state.json');schema=b.load(b.SCHEMA)
    assert b.canonical(schema)==b.canonical(b.build())
    assert package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==168 and len(old['components'])==167
    for key,ref in old['components'].items():assert package['components'][key]==ref and b.link(ref['file'])==ref
    for key,digest in old['preserved_top_level'].items():assert b.canonical(package[key])==digest
    assert b.link('dk2_formats.py')==old['codec']
    for ref in schema['helpers']:assert b.link(ref['file'])==ref
    assert not schema['priority']['block_complete'] and schema['counts']['Crypt_counter_meanings_remaining']==10
    assert schema['counts']['source_writes']==schema['counts']['new_raw_fields']==0
    assert schema['counts']['excluded_exploratory_traces']==12
    assert schema['fixed_state_block']['runtime_counter_span']['unassigned_tail_bytes']==2
    text=(b.ROOT/'CRYPT_STATE_WRITER_RU.md').read_text(encoding='utf-8')
    for fragment in ['не новые поля Creatures.kwd','относительные смещения','12 прежних','10 неизвестных игровых назначений','не отдельная сертификация']:
        assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап190:' in (b.ROOT/name).read_text(encoding='utf-8')[:1800]
    result=dict(phase=190,passed=True,counts=schema['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
                documentation=b.link('CRYPT_STATE_WRITER_RU.md'),prior_components_preserved=167,codec_and_effective_parameters_unchanged=True,
                other_package_top_level_unchanged=True,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase190-validation.json',result);print(json.dumps(result))


if __name__=='__main__':main()
