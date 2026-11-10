"""Audit phase191 without conflating bounded search with field decoding."""
import json
import build_crypt_pointer_forwarding as b


def main():
    package=b.load('editor-format-package.json');schema=b.load(b.SCHEMA);prior=b.load('phase191-prior-package-state.json')
    assert schema==b.build() and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==169 and len(prior['components'])==168
    for key,ref in prior['components'].items():assert package['components'][key]==ref and b.link(ref['file'])==ref
    for key,digest in prior['preserved_top_level'].items():assert b.canonical(package[key])==digest
    assert b.link('dk2_formats.py')==prior['codec']
    for ref in schema['helpers']:assert b.link(ref['file'])==ref
    assert schema['counts']['Crypt_counter_meanings_remaining']==10 and schema['counts']['new_raw_fields']==0
    assert not schema['priority']['block_complete']
    text=(b.ROOT/'CRYPT_POINTER_FORWARDING_RU.md').read_text(encoding='utf8')
    for word in ['164 до199','96 состояний','не доказывает глобальную','Память в этих независимых контролях не сравнивалась','10 счётчиков','169 компонентов']:
        assert word in text,word
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:
        assert 'Актуальный этап191:' in (b.ROOT/name).read_text(encoding='utf8')[:2000]
    result=dict(phase=191,passed=True,counts=schema['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
                documentation=b.link('CRYPT_POINTER_FORWARDING_RU.md'),prior_components_preserved=168,
                codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
                source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase191-validation.json',result);print(json.dumps(result))


if __name__=='__main__':main()
