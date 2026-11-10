"""Audit bounded opaque Terrain storage/checksum evidence and113component package."""
import json
from build_terrain_table import ROOT,load,sha,link,build,effective_terrain_layout
from build_area_damage import effective_shot_parameters

def main():
    value=load('terrain-opaque-table-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase134-prior-components.json')
    assert len(package['components'])==113 and len(prior)==112
    assert set(package['components'])-set(prior)=={'terrain_opaque_table_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['terrain_opaque_table_semantics']==value
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['effective_catalogue_layouts']['Terrain']==effective_terrain_layout()
    before=package['terrain_lean_semantics']['effective_terrain_layout'];after=package['effective_catalogue_layouts']['Terrain']
    changed=0
    for a,b in zip(before['fields'],after['fields']):
        assert all(a[k]==b[k] for k in ['offset','size','name','kind'])
        if a!=b:
            assert 0x1AE<=a['offset']<0x1CE and b['semantics']=='unresolved';changed+=1
    assert changed==16
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['full_native_raw_loads']==12 and c['independent_Ghidra_CPU_controls']==4
    assert c['Ghidra_compared_regions']==38 and c['Ghidra_compared_bytes']==8328 and c['newly_named_unknown_fields']==0
    assert c['unique_original_PCs_by_profile']=={'DX':761,'old':750}
    assert all(p['all_executed_original_PCs_covered'] for p in value['primary']['profiles'].values())
    assert 'Актуальный этап 134:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 113 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    output=dict(phase=134,date='2026-10-30',**c,previous112_components_preserved_byte_exact=True,package_components=113,
        opaque_fields_annotated=16,historical_schemas_unchanged=True,unknown_array_meaning_resolved=False,
        whole_game_nonuse_proven=False,World_accumulation_executed=False,RC3_CPU_executed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase134-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=134,published_phase=134,date='2026-10-30',package_components=113,readiness_estimate_percent=75,
        status='Published two full rawTerrain loader profiles: opaque16WORD array preserved, reads only selected checksum loop, exact32byte edits. Meaning remains unknown.',
        previous_goal_turn_classification='No progress: user-requested readiness assessment only; authoritative research was not changed in that status turn.',
        current_goal_turn_classification='Progress: completed pending134evidence with14whole-function primary spans,4inherited Pro spans,RC3local loop,82exact edits and113component publication; meaning explicitly unresolved.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['terrain-opaque-table-semantics.json','TERRAIN_OPAQUE_TABLE_RU.md',
            'phase134-Terrain-table-primary.json','phase134-Terrain-table-emulations.json','phase134-Terrain-table-crosschecks.json',
            'phase134-Terrain-table-editor-validation.json','phase134-Terrain-table-api-validation.json',
            'phase134-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat12rawloader or82opaque edits. Array index purpose is unresolved; editor Pro directgetter/setter and RC3localwriter yielded no semantic names. Seek genuinely new typed consumers forTerrain198/224 or other15family unknowns; alternatively follow unassigned Crypt groups into real actor consumers, distinguishing loaded counters from working bonuses. Worldchecksum callback remains static-only; execute only if it resolves a specific outstanding question. No original/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase134-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
