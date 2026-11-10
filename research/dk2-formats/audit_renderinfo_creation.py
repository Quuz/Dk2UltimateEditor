"""Audit134 components and strictly separate native creation from helper controls."""
import json
from build_renderinfo_creation import ROOT,load,sha,link,build
def main():
    s=load('renderinfo-creation-semantics.json');assert s==build()
    p=load('editor-format-package.json');old=load('phase155-prior-components.json')
    assert len(p['components'])==134 and len(old)==133 and set(p['components'])-set(old)=={'renderinfo_creation_semantics'}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in old:assert row==old[key]
    assert p['renderinfo_creation_semantics']==s
    assert p['effective_shot_parameters']==p['tremor_damage_semantics']['effective_shot_parameters']
    assert p['effective_room_resource_consumption']==p['room_resource_loader_semantics']['resource_consumption']
    assert p['effective_creature_flag_enum']==p['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert p['effective_catalogue_layouts']['Rooms']==p['room_writer_semantics']['effective_room_layout']
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_catalogue_layouts']['Traps']==p['trap_model_word_semantics']['effective_trap_layout']
    expected=dict(native_creation_scenarios=10,complete_native_creators=20,actual_Creature_definition_loads=20,actual_Object_definition_loads=10,
        native_support_scenarios=4,independent_Ghidra_support_controls=4,independent_full_creation_controls_passed=0,terminal_Ghidra_FCOS_limitations=1,
        Ghidra_support_compared_regions=264,Ghidra_support_compared_bytes=356404,original_code_extents=79,static_only_extents=3,
        unique_executed_original_PCs=4234,primary_data_blocks=4,new_exact_container_edits=0,inherited_exact_priority_edits=328,
        preserved_source_files=310,preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0)
    assert s['counts']==expected
    rows={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert all(r['raw_priority_Jump_reads']==0 and r['low_reads_only_FS_SEH'] and r['native_complete_creators']==2 for r in rows.values())
    for r in rows.values():
        assert all(x['mesh_selection_index_unassigned_hex']=='a5a5a5a5' for x in r['primitives'])
    assert [r['forwarded_AC_byte'] for r in rows['bank255']['primitives']]==[255,255]
    assert not s['remaining_priority']['global_nonuse_proved'] and not s['full_Ghidra_limitation']['full_chain_control_passed']
    assert s['full_Ghidra_limitation']['final_pc']=='0041c61d'
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8').startswith('Актуальный этап 155:')
    assert 'пакет содержит 134 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=155,date='2026-10-31',**expected,package_components=134,previous133_components_preserved_byte_exact=True,
        physical_and_effective_views_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase155-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=155,published_phase=155,date='2026-10-31',package_components=134,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress:phase154publishedactualprimarymodelpicking27native/60callbacks/8Ghidra,133components.',
        current_goal_turn_classification='Progress:20actualnativeallocated/constructedprimitivesin10scenes,4supporthelpers/4independentGhidracontrols,79byteextents. FCOSfulllimitationandfailedunstagedpickexplicitlyexcluded,134components.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['renderinfo-creation-semantics.json','RENDERINFO_CREATION_RU.md','phase155-renderinfo-emulations.json','phase155-support-emulations.json',
            'phase155-support-crosschecks.json','phase155-renderinfo-Ghidra-limitation.json','phase155-renderinfo-primary.json','phase155-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat10creatorcases/4support/4Ghidra orFCOSfailure. CreationfrompreparedRenderInfoexecuted; sourceActor/AI-to-RenderInfobindingandfullrenderer-stagingremainopen. Avoidgrowinggraphicscasecountswithoutfreshrawfieldconsumer. Prioritizefreshremainingrawunknowns Terrain198224,Objects34C,Traps204,Doors236,AP0408,Crypt10groups,CreatureJump/Priority orspell/Elementtails. RIACforwardingisnotPriorityproof. Ifnewtypedbindingexistsfollowit; donotforceconstructor70/5Cdefaultsorclaimgamebugfromunstagedfixture. Alloriginalsources/parallel editorunchanged; full15familygoalactive.',
        limitations=s['limitations'])
    (ROOT/'phase155-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
