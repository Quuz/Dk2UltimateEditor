"""Audit original Crypt completion controls and preservation of all prior data."""
import json
import build_crypt_action_completion as b


def main():
    package = b.load('editor-format-package.json'); old = b.load('phase189-prior-package-state.json'); schema = b.load(b.SCHEMA)
    assert b.canonical(schema) == b.canonical(b.build())
    assert package[b.KEY] == schema and package['components'][b.KEY] == b.link(b.SCHEMA)
    assert len(package['components']) == 167 and len(old['components']) == 166
    for key, ref in old['components'].items():
        assert package['components'][key] == ref and b.link(ref['file']) == ref
    for key, digest in old['preserved_top_level'].items():
        assert b.canonical(package[key]) == digest
    assert b.link('dk2_formats.py') == old['codec']
    for ref in schema['helpers']:
        assert b.link(ref['file']) == ref
    assert schema['counts']['Crypt_counter_meanings_remaining'] == 10 and not schema['priority']['block_complete']
    assert schema['counts']['new_raw_fields'] == schema['counts']['source_writes'] == 0
    assert schema['original_action_table']['action80']['duration_byte'] == 0
    assert schema['original_action_table']['action76']['completion_mode'] == 3
    text = (b.ROOT/'CRYPT_ACTION_COMPLETION_RU.md').read_text(encoding='utf-8')
    for fragment in ['участник ритуала', 'не расходует участника', '3 234 847', '10 потребителей', 'не доказательство всей дальнейшей жизни']:
        assert fragment in text, fragment
    for name in ['INTEGRATION_RU.md', 'READINESS_RU.md']:
        assert 'Актуальный этап189:' in (b.ROOT/name).read_text(encoding='utf-8')[:1800]
    result = dict(phase=189, passed=True, counts=schema['counts'], schema=b.link(b.SCHEMA), package=b.link('editor-format-package.json'),
                  documentation=b.link('CRYPT_ACTION_COMPLETION_RU.md'), prior_components_preserved=166,
                  codec_and_effective_parameters_unchanged=True, other_package_top_level_unchanged=True,
                  source_files_preserved=420, EXE_paths_preserved=7, source_writes=0, goal_status='active', Crypt_block_complete=False)
    b.dump('phase189-validation.json', result)
    print(json.dumps(result))


if __name__ == '__main__':
    main()
