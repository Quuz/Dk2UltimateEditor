"""Audit118component publication: complete native Map reader and EOF defect."""
import json
from build_native_map_reader import ROOT,load,sha,link,build

def main():
    value=load('native-map-reader-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase139-prior-components.json')
    assert len(package['components'])==118 and len(prior)==117
    assert set(package['components'])-set(prior)=={'native_map_reader_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in prior:assert row==prior[key]
    assert package['native_map_reader_semantics']==value
    assert package['effective_catalogue_layouts']['Rooms']==package['room_writer_semantics']['effective_room_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    assert package['effective_enums']['Creature.ResistanceCategory']==package['combat_category_semantics']['native_resistance_categories']
    assert package['outstanding_controls_from_phases114_115']==0
    c=value['counts'];assert c['native_reader_paths']==17 and c['World_reader_paths']==5 and c['capture_or_skip_paths']==12
    assert c['original_World_Map_callbacks']==125 and c['captured_parser_records']==34298 and c['native_ReadFile_calls']==48
    assert c['independent_Ghidra_CPU_controls']==6 and c['Ghidra_compared_regions']==134 and c['Ghidra_compared_bytes']==1216175
    assert c['paired_complete_memory_controls']==5 and c['paired_compared_bytes']==1812831
    assert c['executed_function_spans']==223 and c['reused_phase138_function_byte_ranges']==206 and c['unique_executed_original_PCs']==17076
    assert c['primary_blocks']==12 and c['new_exact_Map_opaque_edits']==110 and c['preserved_source_files']==199 and c['preserved_executable_paths']==7
    assert c['source_writes']==c['newly_named_opaque_fields']==0
    assert value['primary']['all_executed_original_PCs_covered']
    assert value['file_layer']['IAT_patches']==value['file_layer']['game_function_patches']==value['file_layer']['vtable_substitutions']==0
    ex={r['case']:r['outcome'] for r in value['native_executions']['replays']}
    for name in ['truncated-last-fourth','truncated-last-flags','empty-payload']:
        assert ex[name]['returned']==1 and ex[name]['actual_short_reads']==1 and ex[name]['callback_captures']==6
    assert ex['world-declared0']['world']==ex['world-square']['world']
    assert ex['source-AutoSaveMap']['callback_captures']==625 and ex['source-AutoSaveMap-opaque-edit']['callback_captures']==625
    assert ex['chunk-crossing']['callback_captures']==16512 and ex['skip-beyondEOF']['file_position']==1036
    assert ex['wrong-header-tag']['returned']==ex['wrong-data-tag']['returned']==0
    assert len(value['diagnostic_exclusions'])==3 and all(not r['accepted_coverage'] for r in value['diagnostic_exclusions'])
    assert 'Актуальный этап 139:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 118 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=139,date='2026-10-30',**c,previous117_components_preserved_byte_exact=True,package_components=118,
        full_native_Map_section_reader_executed=True,native_file_layer_executed=True,complete_native_outer_dispatcher_executed=False,
        native_open_close_executed=False,complete_World_bootstrap_executed=False,normal_AI_caller_executed=False,
        short_read_acceptance_confirmed=True,strict_lossless_codec_not_relaxed=True,raw_KLD_fourth_byte_still_opaque=True,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase139-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=139,published_phase=139,date='2026-10-30',package_components=118,readiness_estimate_percent=75,
        status='Published original Map section reader/native file layer with unchangedIAT/code/vtables; confirmedshort-read acceptance,625realAutoSave cells,chunkboundary and strictpreservation.',
        previous_goal_turn_classification='Progress: original reader/file-layer traces and externalAPI/backend scaffolding saved; runtimevalidation remainedincomplete becauseofreviewusage/diskfull and twoincompleteWorld fixtures.',
        current_goal_turn_classification='Progress:17readerpaths,6Ghidra controls,5pairedmemory checks,223functionspans,110opaqueedits and118componentpublication. Priorreview limit cleared; requiredproof completed despite fullC byreusing206exactunchangedranges.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        current_environment='DiskC reports0free and breaks default helper/newassembly dumps. Authorized elevatednative/smallGhidra read-memory operations completed thisphase. Reviewservice rejection didnot recur.',
        resolved_prior_blocker='Approvalreviewusage failure no longer blocks currentcommands; priorWIP findingsnowtested. FullC remains environmentconstraint, not a claimthisphase iscompleteoverall.',
        evidence=[link(n) for n in ['native-map-reader-semantics.json','NATIVE_MAP_READER_RU.md','phase139-native-map-reader-primary.json',
            'phase139-native-map-reader-emulations.json','phase139-native-map-reader-crosschecks.json','phase139-map-reader-api-validation.json',
            'phase139-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat17readerpaths/6Ghidra/5pairs/110edits or remainingsectionreader basics. Prioritize fresh real consumers of stillunknown fields across15families (Traps204/Doors236/Objects34C/Terrain198/224/etc), or normalAIstate65activation. Outerfiledispatch/open/fullWorldheaderbootstrap are explicitlyopen but mustnotdisplaceunknownfield priorities indefinitely. KeeprawKLDbyte3opaque; no source/parallel editor writes. FullC may require externalspace restoration for newGhidraassembly work.',
        limitations=value['limitations'])
    (ROOT/'phase139-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
