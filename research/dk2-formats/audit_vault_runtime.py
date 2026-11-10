"""Audit phase111 native factory/ownership compositions and complete package."""
import json
from build_vault_runtime import ROOT,build,load,digest,link


def main():
    schema=load('vault-runtime-semantics.json');assert schema==build()
    package,prior=load('editor-format-package.json'),load('phase111-prior-components.json')
    assert len(package['components'])==90 and len(prior)==89
    assert set(package['components'])-set(prior)=={'vault_runtime_semantics'}
    assert package['vault_runtime_semantics']==schema
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert digest(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    for row in schema['helpers']+schema['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert schema['fresh_creation']['Object_ID_overrides']==[101,108,114]
    assert schema['fresh_creation']['native_period_label']=='EffectTriggerRepeatTimer'
    results={r['case']:r['expected'] for r in schema['creation_replays']}
    assert results['vault-start59-overridden']['active']==results['claimed-start255-overridden']['active']==42
    assert results['ordinary-state42-divide-zero']['expected_divide_zero']
    assert results['vault-misc0']['effect_requests']==1 and results['vault-misc0']['effect_id']==0
    owners={r['case']:r['expected'] for r in schema['income_replays']}
    assert owners['unclaimed-terrain']['current']==1120 and owners['claimed-terrain']['current']==1220
    assert owners['claimed-edited-gain']['current']==1245 and owners['claimed-signed-words']['current']==1119
    assert owners['transfer8-to7']['original_count_helper_calls']==2
    assert not schema['unknown_mana_gen']['meaning_resolved'] and not schema['enclosing_map_route']['executed']
    assert 'Актуальный этап 111:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 90 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    validation=dict(phase=111,date='2026-10-29',**schema['counts'],package_components=90,
                    prior89_components_preserved_byte_exact=True,unchanged_source_executables=4,
                    original_source_data_writes=0,gameplay_function_replacements=0,live_game_runs=0,
                    full_CMap_capture_runs=0,frozen_fieldtrace_backend_unchanged=True,
                    goal_achieved=False,significant_unknowns_remain=True,limitations=schema['limitations'])
    (ROOT/'phase111-validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf8')
    names=['vault-runtime-semantics.json','MANA_VAULT_RU.md','phase111-validation.json','phase111-primary-manifest.json',
           'phase111-vault-creation-emulations.json','phase111-vault-income-emulations.json','phase111-vault-runtime-crosschecks.json',
           'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']
    status=dict(phase=111,date='2026-10-29',published_phase=111,
                status='Published/audited real Object-ID-specific state42 construction and Terrain ownership/income; full CMap capture remains open',
                previous_goal_turn_classification='progress: phase110 published89-component state specification; phase111 finds actual constructor override, state42 type precondition and unmodified Terrain/count/mana composition',
                package_components=90,pending_native_jobs=[],evidence=[link(n) for n in names],readiness_estimate_percent=75,
                findings=[
                    'Original factory overrides StartState for101/108/114 with42 and cached2*TPS EffectTriggerRepeatTimer.',
                    'Other Object17 with StartState42 reaches original DIV ECX withzero after full factory; CPU exception is expected and separately counted.',
                    'Periodic42 reads MiscEffectId358 even ifzero; real ManaVault data287/288 andHeroGate310 confirmed.',
                    'RawTerrain31/32 ownership through real4BE070 andfullmana explains0/100 income contribution andtransfer8->7.',
                    '18factory compositions/423tick entries/1expecteddivide;6ownership-income chains/4Ghidra controls;prior89schemas intact.',
                ],
                next_action='Do not repeat18factory or6ownership cases. Complete enclosing44FC40 using knownsixstackargs X,Y,TerrainID,owner,flag5,flag6 and realembeddedCMapWorld+9DA. '
                            'Natural spawnXY iscellcenter andZ0; currentacceptedfactory fixtures useZ2000. '
                            'Inspect originalZ0/RTTI/readability path before adding an explicit externalOSadapter if genuinelyneeded; never patch gameplaycode. '
                            'NativeTerrain/map/navigation fixtures99 can seed embeddedCMap, butloading flag40 skips vault creation in44FC40, so a full claim requires correctordinarymode neighbors/events. '
                            'If this route yields no newunknown consumer, pivot to another concrete field ratherthanrepeat broadfailedscans. ManaGen/Terrain198224/Trap204/Door236/Jump/Priority/113114 andall15family scope remainopen.',
                goal_achieved=False,significant_unknowns_remain=True,limitations=schema['limitations'])
    (ROOT/'phase111-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in validation.items() if k!='limitations'}))


if __name__=='__main__':main()
