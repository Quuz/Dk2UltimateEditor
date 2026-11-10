"""Audit published raw128B semantics, exact source preservation and open goal."""
import json
import build_recovery_step as b

def main():
    package=b.load('editor-format-package.json');old=b.load('phase184-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and package[b.KEY]==s and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==162 and len(old['components'])==161
    for k,r in old['components'].items():assert package['components'][k]==r and b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    assert s['field']['raw_offset']==0x128B and s['field']['width']==4
    assert s['counts']['newly_assigned_existing_raw_components']==1 and s['counts']['Crypt_counter_meanings_remaining']==10
    assert not s['priority']['block_complete'] and len(s['storage']['edits'])==5 and s['storage']['extra88_bytes_preserved']
    assert all(x['exact_container_edit'] and x['inverse_restores_source'] for x in s['storage']['edits'])
    text=(b.ROOT/'CREATURE_RECOVERY_STEP_RU.md').read_text(encoding='utf-8')
    for part in ['8 принятых','+128B','желаемая точка','не доказательство','88байт','цель по15']:assert part in text,part
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап184:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=184,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('CREATURE_RECOVERY_STEP_RU.md'),
        prior_components_preserved=161,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        exact_source_roundtrips=7,exact_raw128B_container_edits=5,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase184-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
