"""Audit phase131 evidence, effective damage rows and preserved110-component package."""
import json
from build_area_damage import ROOT,load,sha,link,build,effective_shot_parameters
from build_poison import effective_shot_parameters as prior_shot_parameters
from validate_area_damage import validate


def main():
    value=load('area-damage-semantics.json');assert value==build()
    assert load('phase131-area-api-validation.json')==validate()
    package=load('editor-format-package.json');prior=load('phase131-prior-components.json')
    assert len(package['components'])==110 and len(prior)==109
    assert set(package['components'])-set(prior)=={'area_damage_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['area_damage_semantics']==value and package['effective_shot_parameters']==effective_shot_parameters()
    old=prior_shot_parameters();current=package['effective_shot_parameters']
    assert current['processes']==old['processes']
    assert all(current['damage_types'][k]==v for k,v in old['damage_types'].items() if k not in ['1','2','3','4'])
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_object_resource_bindings']['slots'][2]['display_name']=='EditorMapIconResource'
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['native_compositions']==27 and c['original_HP_requests']==51
    assert c['independent_Ghidra_CPU_controls']==4 and c['Ghidra_compared_regions']==228 and c['Ghidra_compared_bytes']==541680
    assert c['fresh_function_spans']==118 and c['fresh_data_blocks']==6 and c['unique_executed_original_PCs']==5356
    assert value['primary']['all_executed_original_PCs_covered']
    assert value['corpus']['damage_mode_counts']=={'0':3357,'1':111,'2':812,'7':1271}
    assert 'Актуальный этап 131:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 110 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=131,date='2026-10-29',**c,previous109_components_preserved_byte_exact=True,package_components=110,
        effective_mode_rows_updated=[1,2,3,4],historical_schemas_unchanged=True,working_resistance_formula_found=False,
        full_game_scheduler_executed=False,live_patch_DLL_detours_executed=False,goal_achieved=False,
        significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase131-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=131,published_phase=131,date='2026-10-29',package_components=110,readiness_estimate_percent=75,
        readiness_basis='Expert estimate for compatible editor specification across15families; not a measured field count or probability. Approximate range70..80.',
        status='Published original order-dependent multi-target range damage and native TIME/filled expanding-square modes; physical layouts preserved.',
        previous_goal_turn_classification='Progress:phase130 published native editor type-change uninitialized stack defect,22native/3Ghidra controls and4fullRoom writer paths.',
        current_goal_turn_classification='Progress:27native AoE paths,4Ghidra controls,118whole functions/6blocks and246exact edits;110component publication.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['area-damage-semantics.json','AREA_DAMAGE_RU.md','phase131-area-primary.json',
            'phase131-area-native-emulations.json','phase131-area-crosschecks.json','phase131-area-api-validation.json',
            'phase131-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat the completed27AoE/4Ghidra cases. Follow fresh remaining field consumers or native scheduling/collision transitions for unverified damage modes5/6/8 if relevant; no universal resistance percentage, extra spell slots or patch implementation is established. Preserve remaining Terrain198/224/1AE, Traps204, Doors236, CreatureJump/Priority, KeeperC8, spell/Element tails and other unresolved semantics. No original-source or parallel editor edits.',
        limitations=value['limitations'])
    (ROOT/'phase131-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))


if __name__=='__main__':main()
