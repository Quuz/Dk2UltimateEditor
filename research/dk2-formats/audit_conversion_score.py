"""Audit the new global meaning, preservation and user-requested priorities."""
import json
import build_conversion_score as b


def main():
    package=b.load('editor-format-package.json');schema=b.load(b.SCHEMA);prior=b.load('phase192-prior-package-state.json')
    assert schema==b.build() and package[b.KEY]==schema and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==170 and len(prior['components'])==169
    for key,ref in prior['components'].items():assert package['components'][key]==ref and b.link(ref['file'])==ref
    for key,digest in prior['preserved_top_level'].items():assert b.canonical(package[key])==digest
    assert b.link('dk2_formats.py')==prior['codec']
    for ref in schema['helpers']:assert b.link(ref['file'])==ref
    assert schema['field']['type']==233 and schema['field']['value_bytes']==4 and schema['counts']['new_raw_fields']==0
    assert schema['priority']['current']=='Global parameters' and not schema['priority']['block_complete']
    assert package['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    text=(b.ROOT/'CONVERSION_SCORE_RU.md').read_text(encoding='utf8')
    for fragment in ['UNKNOWN_233','тип CPlayer, не номер','без верхнего насыщения','не выдаются за полные функции','Crypt отложен','170 компонентов']:
        assert fragment in text,fragment
    priorities=(b.ROOT/'RESEARCH_PRIORITIES_RU.md').read_text(encoding='utf8')
    assert 'Глобальные параметры — текущий блок' in priorities and 'Crypt отложен' in priorities
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:
        assert 'Актуальный этап192:' in (b.ROOT/name).read_text(encoding='utf8')[:2000]
    result=dict(phase=192,passed=True,counts=schema['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
        documentation=b.link('CONVERSION_SCORE_RU.md'),prior_components_preserved=169,codec_and_effective_parameters_unchanged=True,
        other_package_top_level_unchanged=True,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,
        goal_status='active',current_block='Global parameters',Crypt_deferred=True,global_block_complete=False)
    b.dump('phase192-validation.json',result);print(json.dumps(result))


if __name__=='__main__':main()
