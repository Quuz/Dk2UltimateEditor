"""Audit132 components and exact native cursor-fallback/priority evidence."""
import json
from build_pickup_selection import ROOT,load,sha,link,build
import native_pickup_selection as api
def main():
    s=load('pickup-selection-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase153-prior-components.json')
    assert len(p['components'])==132 and len(old)==131 and set(p['components'])-set(old)=={'pickup_selection_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['pickup_selection_semantics']==s
    assert p['effective_shot_parameters']==p['tremor_damage_semantics']['effective_shot_parameters']
    assert p['effective_room_resource_consumption']==p['room_resource_loader_semantics']['resource_consumption']
    assert p['effective_creature_flag_enum']==p['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert p['effective_catalogue_layouts']['Rooms']==p['room_writer_semantics']['effective_room_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    expected=dict(native_scenarios=34,complete_fallback_calls=34,actual_Creature_definition_loads=68,actual_Object_definition_loads=34,
        independent_Ghidra_CPU_controls=8,Ghidra_compared_regions=273,Ghidra_compared_bytes=653000,original_code_extents=69,static_only_extents=2,
        unique_executed_original_PCs=3615,primary_data_blocks=1,source_files_per_family=82,source_Creature_records=3734,source_Object_records=11756,
        exact_container_edits=328,public_decode_rebuild_controls=328,public_scalar_controls=174,extended_Creature_helper_controls=154,
        preserved_source_files=310,preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    for name in ['first-farther-creature','priority-255-0','creatures-tie','earlier-cell-first','Y-outer-X-inner-order','heart-special-ID13']:
        assert rows[name]['selected_tag']==9,name
    for name in ['reversed-creatures','objects-nearest','near-object-before-creature','object-priority-255','first-excluded-state']:
        assert rows[name]['selected_tag']==10,name
    assert rows['object-only-nearest']['selected_tag']==11
    for name in ['outside-radius','object-default-boundary','object-only-boundary','no-candidates','object-unpickable']:
        assert rows[name]['selected_tag'] is None and rows[name]['native_return']==0,name
    assert all(r['priority_or_Jump_reads_in_fallback']==0 for r in rows.values())
    assert not s['priority_Jump']['global_nonuse_proved'] and not s['priority_Jump']['primary_graphics_path_covered']
    assert api.distance(-1000,-1000)==1500 and api.distance(16,0)==16
    for family,size,off in [('Creatures',5449,0xF55),('Creatures',5537,0xF55),('Objects',894,0x35D)]:
        raw=bytes([165])*size;b=api.patch_priority(raw,family,255);assert b==raw[:off]+b'\xff'+raw[off+1:]
    for args in [(b'','Creatures',1),(bytes(5449),'Creatures',True),(bytes(894),'Other',1),(bytes(894),'Objects',256)]:
        try:api.patch_priority(*args)
        except ValueError:pass
        else:raise AssertionError('Accepted invalid priority input')
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 153:')
    assert 'пакет содержит 132 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=153,date='2026-10-31',**expected,package_components=132,previous131_components_preserved_byte_exact=True,
        physical_and_effective_views_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase153-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=153,published_phase=153,date='2026-10-31',package_components=132,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase152published82actualKeeperprecastvalidatorpaths/10Ghidra/246edits,131components.',
        current_goal_turn_classification='Progress:publishedrealrawCreature/Object->cursorfallbackselection,34native/8Ghidra/69extents/328exactedits. NarrowedPriority/Jump consumer scope; primarygraphicsroute locatedbutnotexecuted.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['pickup-selection-semantics.json','PICKUP_SELECTION_RU.md','phase153-pickup-emulations.json','phase153-pickup-crosschecks.json',
            'phase153-pickup-primary.json','phase153-pickup-api-validation.json','phase153-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat34fallbackcases/8Ghidra/328edits. Fresh concrete Priorityroute isprimary572820->588A60->actualrenderobjectvslot0C; caller5992B0 fallback44DEC0closedonlyboundedly. Need genuine graphicsinstance/descriptor consumer before namingPriority/Jump. RemainingunknownCreatureJump/Priority,Objects34C,Traps204,Doors236,Terrain198224/AP0408/Crypt10groups stayopen. CurrentlocalbridgefoundnoGhidrainstance oncachedcontrol replay; no livejob. Checkactualserver/process/projectstate andstartauthorizedGhidra ifneededfornewrequests,do not restartfromstaleassumptions. Alloriginalfiles/EXEs/parallel editor unchanged.',
        limitations=s['limitations'])
    (ROOT/'phase153-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
