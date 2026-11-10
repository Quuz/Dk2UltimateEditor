"""Bundle evidence schemas for consumption by the independently built editor."""
from pathlib import Path
import hashlib,json

ROOT=Path(__file__).resolve().parent
PARTS={
 'catalogue_layouts':'layouts.json','thing_layouts':'thing-layouts.json','trigger_parameter_layouts':'trigger-layouts.json',
 'level_layouts':'level-layouts.json','variable_layouts':'variable-layouts.json','enums':'enums.json',
 'engine_overrides':'verified-overrides.json','shot_parameters':'shot-semantics.json',
 'variable_runtime':'variable-runtime-map.json','creature_stat_runtime':'creature-stat-runtime-map.json',
 'creature_subrecords':'creature-subrecords.json','scalar_storage_paths':'scalar-storage-paths.json',
 'catalogue_loader_constraints':'catalogue-loader-constraints.json','effect_semantics':'effect-semantics.json',
 'spell_semantics':'spell-semantics.json','catalogue_loading':'catalogue-loading.json',
 'unresolved_field_traces':'unresolved-field-traces.json','editor_field_bindings':'editor-field-bindings.json',
 'editor_composition':'editor-composition.json','object_resource_bindings':'object-resource-bindings.json',
 'room_resource_semantics':'room-resource-semantics.json','trap_semantics':'trap-semantics.json',
 'trap_fire_semantics':'trap-fire-semantics.json','door_semantics':'door-semantics.json',
 'manufacture_semantics':'manufacture-semantics.json','object_property_semantics':'object-property-semantics.json',
 'effect_chain_semantics':'effect-chain-semantics.json','creature_priority_semantics':'creature-priority-semantics.json',
 'creature_editor_scalars':'creature-editor-scalars.json','creature_editor_scaled':'creature-editor-scaled.json',
 'creature_editor_normalization':'creature-editor-normalization.json','element_color_semantics':'element-color-semantics.json',
 'catalogue_checksums':'catalogue-checksums.json','mana_semantics':'mana-semantics.json',
 'creature_creep_semantics':'creature-creep-semantics.json',
 'creature_first_person_semantics':'creature-first-person-semantics.json',
 'keeper_ai_semantics':'keeper-ai-semantics.json',
 'effect_editor_semantics':'effect-editor-semantics.json','map_cell_semantics':'map-cell-semantics.json',
 'placed_structure_semantics':'placed-structure-semantics.json',
 'placed_object_semantics':'placed-object-semantics.json',
 'placed_creature_semantics':'placed-creature-semantics.json',
 'hero_party_semantics':'hero-party-semantics.json',
 'action_point_semantics':'action-point-semantics.json',
 'action_point_editor_semantics':'action-point-editor-semantics.json',
 'level_descriptor_semantics':'level-descriptor-semantics.json',
 'creature_lightning_immunity':'creature-lightning-immunity.json',
 'effect_generator_semantics':'effect-generator-semantics.json',
 'dead_body_semantics':'dead-body-semantics.json',
 'placed_room_semantics':'placed-room-semantics.json',
 'camera_semantics':'camera-semantics.json',
 'spell_writer_semantics':'spell-writer-semantics.json',
 'spell_alternative_semantics':'spell-alternative-semantics.json',
 'creature_extension_semantics':'creature-extension-semantics.json',
 'keeper_pipeline_semantics':'keeper-pipeline-semantics.json',
 'action_point_visibility_semantics':'action-point-visibility-semantics.json',
 'camera_machine_spec_semantics':'camera-machine-spec-semantics.json',
 'creature_version_layout_semantics':'creature-version-layout-semantics.json',
 'terrain_lighting_semantics':'terrain-lighting-semantics.json',
 'camera_catalogue_semantics':'camera-catalogue-semantics.json',
 'rc3_compatibility_semantics':'rc3-compatibility-semantics.json',
 'rc3_action_point_semantics':'rc3-action-point-semantics.json',
 'version_crosscheck_semantics':'version-crosscheck-semantics.json',
 'keeper_cast_semantics':'keeper-cast-semantics.json',
 'shot_creation_semantics':'shot-creation-semantics.json',
 'action_point_condition_semantics':'action-point-condition-semantics.json',
 'creature_anger_semantics':'creature-anger-semantics.json',
 'sacrifice_semantics':'sacrifice-semantics.json',
 'creature_reward_semantics':'creature-reward-semantics.json',
 'portal_exit_semantics':'portal-exit-semantics.json',
 'possession_speed_semantics':'possession-speed-semantics.json',
 'possession_health_semantics':'possession-health-semantics.json',
 'room_species_bonus_semantics':'room-species-bonus-semantics.json',
 'possession_neighbour_semantics':'possession-neighbour-semantics.json',
 'possession_recharge_semantics':'possession-recharge-semantics.json',
 'navigation_speed_semantics':'navigation-speed-semantics.json',
 'navigation_step_semantics':'navigation-step-semantics.json',
 'navigation_map_semantics':'navigation-map-semantics.json',
 'unknown_instruction_boundary_review':'unknown-instruction-boundary-review.json',
 'possession_scalar_version_semantics':'possession-scalar-version-semantics.json',
 'element_graphics_semantics':'element-graphics-semantics.json',
 'element_model_semantics':'element-model-semantics.json',
 'possession_damage_semantics':'possession-damage-semantics.json',
 'melee_attack_semantics':'melee-attack-semantics.json',
 'crypt_threat_semantics':'crypt-threat-semantics.json',
 'crypt_tick_semantics':'crypt-tick-semantics.json',
 'object_lifetime_semantics':'object-lifetime-semantics.json',
 'factory_mana_semantics':'factory-mana-semantics.json',
 'object_state_semantics':'object-state-semantics.json',
 'vault_runtime_semantics':'vault-runtime-semantics.json',
 'vault_map_semantics':'vault-map-semantics.json',
 'creature_resistance_semantics':'creature-resistance-semantics.json',
 'shot_damage_semantics':'shot-damage-semantics.json',
 'poison_process_semantics':'poison-process-semantics.json',
 'work_point_semantics':'work-point-semantics.json',
 'editor_variant_semantics':'editor-variant-semantics.json',
 'combat_crosscheck_semantics':'combat-crosscheck-semantics.json',
 'trap_creation_semantics':'trap-creation-semantics.json',
 'trap_removal_semantics':'trap-removal-semantics.json',
 'editor_object_icon_semantics':'editor-object-icon-semantics.json',
 'room_writer_semantics':'room-writer-semantics.json',
 'room_torch_light_semantics':'room-torch-light-semantics.json',
 'game_executable_compatibility':'game-executable-compatibility.json',
 'combat_category_semantics':'combat-category-semantics.json',
 'resistance_version_semantics':'resistance-version-semantics.json',
 'room_model_chain_semantics':'room-model-chain-semantics.json',
 'terrain_model_chain_semantics':'terrain-model-chain-semantics.json',
 'procedural_resource_semantics':'procedural-resource-semantics.json',
 'art_resource_editor_semantics':'art-resource-editor-semantics.json',
 'area_damage_semantics':'area-damage-semantics.json',
 'procedural_cursor_semantics':'procedural-cursor-semantics.json',
 'terrain_lean_semantics':'terrain-lean-semantics.json',
 'terrain_opaque_table_semantics':'terrain-opaque-table-semantics.json',
 'temple_object_entry_semantics':'temple-object-entry-semantics.json',
 'temple_creature_entry_semantics':'temple-creature-entry-semantics.json',
 'room_adjacency_semantics':'room-adjacency-semantics.json',
 'map_room_chain_semantics':'map-room-chain-semantics.json',
 'native_map_reader_semantics':'native-map-reader-semantics.json',
 'element_palette_chain_semantics':'element-palette-chain-semantics.json',
 'effect_generation_scale_semantics':'effect-generation-scale-semantics.json',
 'door_manufacture_split_semantics':'door-manufacture-split-semantics.json',
 'effect_mode18_semantics':'effect-mode18-semantics.json',
 'wander_target_path_semantics':'wander-target-path-semantics.json',
 'room_wander_semantics':'room-wander-semantics.json',
 'trap_model_word_semantics':'trap-model-word-semantics.json',
 'melee_animation_flag_semantics':'melee-animation-flag-semantics.json',
 'shot_lifecycle_semantics':'shot-lifecycle-semantics.json',
 'unknown_search_review':'unknown-search-review.json',
 'room_resource_loader_semantics':'room-resource-loader-semantics.json',
 'tremor_damage_semantics':'tremor-damage-semantics.json'}

def main():
    data=dict(package_version=1,byte_order='little',packed_records=True,
        purpose='Evidence schemas for native DK2 formats; not a claim of verified gameplay for every named field.',
        authoritative_storage='Keep complete raw headers/records, unknown bytes/bits, source path and order; patch explicitly selected spans.',
        precedence=['Engine overrides for the exact inspected executable take precedence over conflicting baseline fields.',
            'Use effective_shot_parameters for current Shot process meanings. Phase115 corrects the reversed Process38 Data1/Data2 interpretation; historical shot_parameters remains unchanged for provenance only and its Process38 row is superseded.',
            'shot_damage_semantics phase114 extends phase113 melee resistance to selected direct-hit and two AoE paths. Its historical0 pendingGhidra controls are superseded by combat_crosscheck_semantics phase118:5 damage and3 poison controls actuallypassed;seevalidation_updates.',
            'vault_map_semantics phase112 executes the prepared enclosing CMap route that vault_runtime_semantics phase111 recorded as static-only; historical components retain their original coverage statements.',
            'Original editors can contain defects. A label, validation range or matching RC3/Pro behavior proves only that editor behavior, not game semantics or a required serializer rule.',
            'editor_variant_semantics bounds the exact supplied stock2.0/Pro comparison:13dialog-resource switches andonepostwarning save branch. Their shared reader/writer bytes are not independent bug-free corroboration; do not transfer this equivalence toRC3.',
            'Baseline source-derived names/units remain available for audit; loaded storage alone does not verify gameplay semantics.',
            'Runtime offsets are not file offsets; runtime widths do not change raw field sizes.',
            'Native editor labels/ranges and conditional commits describe that GUI; they do not automatically override game behavior or lossless serialization.'],
        catalogue_ids={'Terrain':'TerrainId','Rooms':'RoomId','Doors':'DoorId','Traps':'TrapId','Creatures':'CreatureId',
            'Objects':'ObjectId','CreatureSpells':'CreatureSpellId','KeeperSpells':'KeeperSpellId','Shots':'ShotId',
            'Effects':'EffectId','EffectElements':'EffectElementId'},
        containers={
          'catalogue':dict(header_bytes=56,count_offset=20,declared_data_bytes_offset=52,whole_size_offset=8,
            signature_offset=0,record_stride='D/N for nonzero N; select known schema, preserve unknown variants raw',
            warning='All11 inspected catalogue parsers can divide by zero before checking N=0 when a record callback is registered.'),
          'things':dict(header_bytes=56,count_offset=20,record_envelope=['tag:u32','payloadBytes:u32','payload:bytes'],
            native_overrides='See placed_structure_semantics:Trap/Door; placed_object_semantics:Object; placed_creature_semantics:C6/C7/C8; hero_party_semantics:C9; action_point_semantics:C5; effect_generator_semantics:CB; dead_body_semantics:CA; placed_room_semantics:CC; camera_semantics:CD BYTE ID,opaque tail,native presets/flags,phase63_activation. Pro map-Things path omitsCD;separate GlobalVariables camera catalogue path is specified in camera_catalogue_semantics.',
            warning='D can be stale; validate N envelopes and complete-section size. Preserve unknown tags raw.'),
          'triggers':dict(header_bytes=60,condition_count_offset=20,action_count_offset=24,declared_data_bytes_offset=56,
            record_envelope=['tag:u32 (D5/D6)','payloadBytes:u32','payload:16 bytes'],
            warning='D usually counts only condition payloads: 16*Nconditions; preserve the observed convention.'),
          'variables':dict(header_bytes=56,count_offset=20,record_bytes=16,tag_offset=0,
            warning='Several map files have contradictory N/S/D; report and preserve forensic data, do not silently infer repairs.'),
          'map':dict(header_bytes=36,width_offset=20,height_offset=24,cell_bytes=4,cell_order='row-major',
            cell_fields=['terrain:u8','owner:u8','flags:u8','opaque:u8'],
            legacy_field_aliases={'bridge':'flags','unknown':'opaque'},
            native_rules='See map_cell_semantics; preserve all4 physical bytes by default.'),
          'level':dict(header_bytes=56,reference_count_u16_offset=20,creature_list_count_u16_offset=22,
            property_data_tag='0xDF',references_tag='0xDE',reference_bytes=72,creature_list_item_bytes=4,
            warning='Only low8 of each creature-list DWORD is used by the inspected callback; preserve all4 bytes.')},
        components={})
    for key,file in PARTS.items():
        b=(ROOT/file).read_bytes();data[key]=json.loads(b)
        data['components'][key]=dict(file=file,sha256=hashlib.sha256(b).hexdigest())
    # Derived current view keeps historical component hashes auditable while
    # preventing the disproved PoisonSpit row from remaining the effective UI rule.
    data['effective_shot_parameters']=json.loads(json.dumps(data['shot_parameters']))
    corrected=data['poison_process_semantics']['corrected_process38']
    assert corrected['process_type']==38
    data['effective_shot_parameters']['processes'][38]=corrected
    data['semantic_corrections']=[dict(component='shot_parameters',json_pointer='/processes/38',
        replacement_component='poison_process_semantics',replacement_key='corrected_process38',
        effective_view='effective_shot_parameters',historical_value_retained_for_audit=True)]
    # Phase131 extends four damage-mode rows without editing historical schemas.
    data['effective_shot_parameters']=data['area_damage_semantics']['effective_shot_parameters']
    data['semantic_corrections'].append(dict(component='shot_parameters',json_pointer='/damage_types',
        replacement_component='area_damage_semantics',replacement_key='effective_shot_parameters',
        effective_view='effective_shot_parameters',historical_value_retained_for_audit=True,
        scope='Modes1..4 only: native multi-target accumulator defect, filled square geometry and Q12 units; all other mode/process rows retain previous current values.'))
    data['effective_shot_parameters']=data['shot_lifecycle_semantics']['effective_shot_parameters']
    data['semantic_corrections'].append(dict(component='shot_parameters',json_pointer='/damage_types',
        replacement_component='shot_lifecycle_semantics',replacement_key='effective_shot_parameters',
        effective_view='effective_shot_parameters',historical_value_retained_for_audit=True,
        scope='Modes5/6 complete selectedtick/terminal execution, firsttickguard149, exactzero World723B terminalgate, tested signedData1+80/0/-80 andintegerextent. Mode1 gains a two-whole-tick control note. All process rows andother damage rows retain phase131 values.'))
    data['precedence'].append('shot_lifecycle_semantics phase148 executes originalrawloaders/factory andcomplete selectedShot4AB180ticks/4AAF70terminal. Mode5firesonceonfirsttick;mode6requiresWorld723B==0. HPexpiryrequestsdeletionbutdoesnotitselfexecuteAT_END. Data1negative80requestspositive80HP inselectedtargets; rawu32storageunchanged. Modes5/6areabsentin5551sourceShotrecords. PreparedWorld/resource/audioIO anddirectterminal; normalWorldscheduler/poolfree andbroaderWorldgatemeaning remainopen.')
    data['precedence'].append('unknown_search_review phase149 preserves unresolvedCreatureGameExtraFlags bit2 andtenCryptgroups. Newpointerforwarding scans identify sixextraFlagreadsites testingonly1/4/8 andquarantine12falseCryptCounter108B chains: original4C7360 checksexactlyE97/EB0/EC9, not108B. Eightnativehelpercases/fourGhidraregistercontrols confirmbounds; searchstate/depthlimits preventglobalnonuseclaims. No effectivefield/enum/parameter view changes ornewopaquefieldnames.')
    data['effective_room_resource_consumption']=data['room_resource_loader_semantics']['resource_consumption']
    data['precedence'].append('room_resource_loader_semantics phase150 executes complete originalRoom/Terrain rawloaders withsuccessfulasset-requestIO andnative504D80 metadataunpacking. In13selectedconstructionmodes rawRoom11C/170/1C4/218 feedonlytheMIDchecksumloop,withidenticalassetrequests/allothercheckedbytes; existingStraight/Inside/Outside editorlabels do not establish independentgamefloormeshreplacement. CompleteMeshnamepositivecontrol changesrequests. Allfourblocks arezero in2075sourceRoomrecords; populatedcasesarediagnostic. Unknown1C4 remainsunnamed,globalnonuse/renderingunproven. RuntimechecksumA2 isnotafilepatchoffset; updatepreservesexistingvalue. Preserveallrawbytes/useeffective_room_resource_consumption forboundedgameconsumptionannotations.')
    data['effective_shot_parameters']=data['tremor_damage_semantics']['effective_shot_parameters']
    data['semantic_corrections'].append(dict(component='shot_parameters',json_pointer='/damage_types/8',
        replacement_component='tremor_damage_semantics',replacement_key='effective_shot_parameters',effective_view='effective_shot_parameters',
        historical_value_retained_for_audit=True,scope='Onlymode8: actualQ12square/XthenY/bounds/SOLIDnotIMPENETRABLE, full32sign pluslow16TerrainHPapplication andrawDestroyedType/CMap/NAVtransition. Allothermode/process rowsretainphase148values.'))
    data['precedence'].append('tremor_damage_semantics phase151 executes originalrawShot/Terrain loaders,factory,complete selectedShot ticks andactualcellHP/CMap/NAVdestruction. Mode8usesData2>>12 andlow16damageapplication:65536->0,65537->1; testednegative80heals. SourceRock2DestroyedType3 becomesDirt3/HP1/nav0; selftype2leavesHP0/type2/nav8. Cellownerlow12,notcreatorowner, forwarded. RealNAVinitializers eliminateNULLgraphaccess; preparedWorld andresource/audioIO remainexplicit. No mode8 in5551sourceShotrecords. RuntimecellHP+6 isnot aKLDfileoffset; no opaqueMapbyte3meaning assigned.')
    data['effective_object_resource_bindings']=json.loads(json.dumps(data['object_resource_bindings']))
    assert data['effective_object_resource_bindings']['slots'][2]['raw_offset']==0xC8
    data['effective_object_resource_bindings']['slots'][2]=data['editor_object_icon_semantics']['effective_resource_slot']
    data['semantic_corrections'].append(dict(component='object_resource_bindings',json_pointer='/slots/2',
        replacement_component='editor_object_icon_semantics',replacement_key='effective_resource_slot',
        effective_view='effective_object_resource_bindings',historical_value_retained_for_audit=True))
    data['effective_catalogue_layouts']=json.loads(json.dumps(data['catalogue_layouts']))
    data['effective_catalogue_layouts']['Rooms']=data['room_writer_semantics']['effective_room_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Rooms',
        replacement_component='room_writer_semantics',replacement_key='effective_room_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True))
    # Native resistance categories replace only the four resistance type fields.
    # Historical Creature.AttackType also labels other fields: keep those scopes
    # separate, including dynamic spell-ID lists in the original attack dialog.
    data['effective_enums']=json.loads(json.dumps(data['enums']))
    data['effective_enums']['Creature.ResistanceCategory']=data['combat_category_semantics']['native_resistance_categories']
    data['effective_enums']['Shot.AttackCategory']=data['combat_category_semantics']['native_shot_categories']
    for family,layout in data['combat_category_semantics']['effective_creature_layouts'].items():
        data['effective_catalogue_layouts'][family]=layout
        data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/'+family,
            replacement_component='combat_category_semantics',replacement_key='effective_creature_layouts',replacement_member=family,
            effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
            scope='Four resistance type enums and four editor value-range annotations; physical bytes unchanged.'))
    data['semantic_corrections'].append(dict(component='enums',json_pointer='/Shot.AttackCategory',
        replacement_component='combat_category_semantics',replacement_key='native_shot_categories',
        effective_view='effective_enums',historical_value_retained_for_audit=True,
        scope='Native labels4..15; resistance fields use new Creature.ResistanceCategory rather than the historical spell list.'))
    data['precedence'].append('Use effective_catalogue_layouts with effective_enums. Creature resistance fields use Creature.ResistanceCategory from original Editor table512D28, not the historical spell-ID list Creature.AttackType. Unknown IDs16/30/32 must remain opaque; native labels0..15 do not prove every runtime ID.')
    data['precedence'].append('ArtResource type7 uses procedural_resource_semantics.ArtResource_variant: procedure_id is a full u32 selector at resource+68; preserve opaque DWORDs+72/+76. Successful procedural graphics requests return handle0 and need not load an external asset. This does not name opaque DWORDs or prove GPU output.')
    data['precedence'].append('art_resource_editor_semantics proves native Pro SetType41BBB0 copies83uninitialized local bytes during reinitialization. This defect is not a serializer requirement. Preserve opaque bytes for unchanged types; initialize deliberate new variants deterministically. Native clear41C240(0) erases all84rawbytes, unlike passive loading of a type0 resource.')
    data['precedence'].append('area_damage_semantics phase131 supersedes single-target falloff assumptions: mode2 retains a shared damage accumulator across targets in one dispatch. Modes1/3/4 use filled half-open squares; mode3/4 Data2 is Q12, unlike integer-cell mode1/2. Modes3/4 are diagnostic support absent from5551 source records. Use effective_shot_parameters; do not infer a working resistance percentage or reproduce a game defect as a file-format rule.')
    data['effective_catalogue_layouts']['Terrain']=data['procedural_cursor_semantics']['effective_terrain_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Terrain',
        replacement_component='procedural_cursor_semantics',replacement_key='effective_terrain_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='Eight raw deformation bytes1CE..1D5 receive native runtime/units annotations. Physical sizes, field names and all unknown198/1AE/224 bytes remain unchanged.'))
    data['precedence'].append('procedural_cursor_semantics phase132 supersedes the prior static-only status of procedural selector3: native generic wrapper, Cursor-textured wireframe, shared pulse and Terrain deformation execute. Exact Wibble integer ranges and Lean float32 BYTE/512 consumers are established; use effective_catalogue_layouts.Terrain. Full Ghidra deformed frame remains unsupportedFSIN and fractional floor differs; these retained diagnostics are not passing controls or game defects.')
    data['effective_catalogue_layouts']['Terrain']=data['terrain_lean_semantics']['effective_terrain_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Terrain',
        replacement_component='terrain_lean_semantics',replacement_key='effective_terrain_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='Coverage extension for six Lean bytes: actual mixed-occupancy boundary effect and3layer mapping; all physical/raw/unknown fields retained.'))
    data['precedence'].append('terrain_lean_semantics phase133 confirms LeanH/V boundary conditions, three ordered render layers, native1.4cornerfactor and real World20C/220 occupancy predicates. Use effective_catalogue_layouts.Terrain.22full raw→cursor paths have independently checked complete final grids;6Ghidra controls cover helper/raw→occupancy scopes. HeterogeneousTerrain definitions and fullGhidraFSIN/GPU remain outside this confirmation.')
    data['effective_catalogue_layouts']['Terrain']=data['terrain_opaque_table_semantics']['effective_terrain_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Terrain',
        replacement_component='terrain_opaque_table_semantics',replacement_key='effective_terrain_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='Sixteen opaqueWORDs1AE..1CD retain unknown semantics and physical layout. Adds verified storage and selected loader checksum-role annotations; no new semantic field names.'))
    data['precedence'].append('terrain_opaque_table_semantics phase134 confirms raw16WORD1AE is preserved in intermediate266 and contributes to the selected736byte runtime sum when enabled. Two exact game loader profiles do not copy it into semantic definition properties in12prepared cases. Meaning and index labels remain unknown; preserve values/order. Runtime definition84 sum is not a KWD header checksum field. World accumulation callbacks and RC3writer loop are static-only.')
    data['precedence'].append('temple_object_entry_semantics phase135 supersedes the historical static-only Object sacrifice entry limitation in sacrifice_semantics: rawObject loader plus full49F600callback, recipe matching andmana reward execute in19prepared cases. Gold40h at rawObjectFlags344/definitionF4 blocks this route before history/HP/reward. ActorHP0 is not fullWorld removal; callback alwaysreturns0. Normal callbackactivation, Creature472930entry and realvisual effects remain outside this confirmation. Original44recipes have noObject ingredients; newObject→mana recipes are diagnostic support.')
    data['precedence'].append('temple_creature_entry_semantics phase136 supersedes historical static-only472930Creature entry coverage. Native state table registers callback at65h/category12;14full paths distinguish12registered-state65fixtures from2explicitdirect-state1diagnostics. Zero surface recordsCreaturelevel andsetsHP0; nonzero surface canrequest67h. Return1is not sacrifice or guaranteedtransition. State65→67doesnot re-enter category/capacity check; state1capacity-refusal diagnostics mustnot become a normalTemple rule. CanonicalVariablesappend621→622updatesS/N/D andloads45type43recipes, but fullnativeKWDparser is not newlyexecuted.')
    data['validation_updates']=data['combat_crosscheck_semantics']['supersedes_validation_status']
    data['precedence'].append('room_adjacency_semantics phase137 proves rawRoom395->intermediate469->definition452->CMap450470/RoomMgr4EDC10->construction7 mask at runtime16byte cell+C, composed with three Creature Temple entries. Eight neighbour TerrainIds and owner low12 define the mask; neighbour RoomTag/instance and owner high4 are not compared. This does not decode rawKLD byte3. NAV4 prestate exercises only unchanged bitmap branch; fullremesh, rawKLD World bootstrap and normal AIcaller remain open. Use native_room_adjacency for bounded runtime projection; preserve all rawunknowns.')
    data['outstanding_controls_from_phases114_115']=data['combat_crosscheck_semantics']['remaining_prepared_controls']
    data['effective_catalogue_layouts']['EffectElements']=data['element_palette_chain_semantics']['effective_element_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/EffectElements',replacement_component='element_palette_chain_semantics',replacement_key='effective_element_layout',effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,scope='B1/B2 legacyOpenKeepernames remainunverified; annotate storage and fullselectedraw/spawn/update/render nonread scope, physicalfields unchanged.'))
    data['precedence'].append('element_palette_chain_semantics phase140 composes raw182->MID290->definition79->actualspawn/fullupdate/physics->sprite/quadqueue. B1/B2 copy to11D/11E and4D/4E;23selectedpaths do notreadterminaldefinitionbytes,15wholememorypairs differonlyinstorage. No globalnonuse or paletteindexmeaning proved. PositiveRGB/fadecontrols work; effective_catalogue_layouts.EffectElements labels unknownmeaning explicitly whilepreservinglegacy names/fullrawvalues. No GPU/assets/Worldscheduler/finaldeletion claim.')
    data['precedence'].append('native_map_reader_semantics phase139 executes complete56D2D0 plus originalMyFile/Disc5B3EB0/5B6EA0/5FFFB0 to declared externalWindowsimports with unchangedIAT/vtables. Shortreads produce result0but reader tests<0, so corruptpayload may feed stalebuffer andreturn1. Keep stricteditorframing; activecallback ignores declaredDataSize, rawbyte3 remainsopaque/preserved. RealAutoSaveMap625records and16384cellchunkboundary proved. HeadercallbackNULL and entryposition12 mean nativeouterdispatch/open/close/fullWorldbootstrap remain outside this proof.')
    data['precedence'].append('map_room_chain_semantics phase138 executes complete original51EC60 Map callbacks, native Room slot activation/join, decoration Object creation/cleanup with original554AD0allocator and real changedNAV0->4 remesh. RawMap->MapMID remains host projection backed by historical parser evidence, not a new fullKLD World bootstrap. World72E7 accumulates signed8 of16MIDbytes, not raw4bytes or a headerchecksum. Four pairedflag scenes differ only in MIDflags/Worldsum/WORD8bit1000. OpaqueKLDbyte3, wholemesh meanings, normalAIcaller and finalCreaturedeletion remain open.')
    data['effective_catalogue_layouts']['Effects']=data['effect_generation_scale_semantics']['effective_effect_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Effects',
        replacement_component='effect_generation_scale_semantics',replacement_key='effective_effect_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='Mode4 flags4000 and BYTEF3 count annotations. Physical offsets, sizes and legacy names preserved; other modes and unknown tails remain separate.'))
    data['precedence'].append('effect_generation_scale_semantics phase141 confirms mode4 adaptive element batches: max(N!=0,floor(N*runtimeScale/255)) when bit4000 is set and target40 clear. N is not a per-second rate or guaranteed live count. Runtime scale is timer-derived, not a KWD field or verified manual GUI setting; do not transfer this rule to other generation modes.')
    data['effective_catalogue_layouts']['Doors']=data['door_manufacture_split_semantics']['effective_door_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Doors',
        replacement_component='door_manufacture_split_semantics',replacement_key='effective_door_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='Split obsolete DWORD ManufToBuild234 into nativeu16requirement234 andseparateopaqueu16Unknown236. Full616bytes retained, no new gameplay meaning for236.'))
    data['precedence'].append('door_manufacture_split_semantics phase142 corrects researchcodec decode/set_scalar andeffectiveDoorslayout: ManufToBuild is u16 at234, not the historicalu32. Preserve opaqueWORD236 independently. Historical dk2_formats_phase141.py reproduces the old four-byte clobber andmustnot be used for currentedits; layouts.json remains provenance only.')
    data['effective_catalogue_layouts']['Effects']=data['effect_mode18_semantics']['effective_effect_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Effects',
        replacement_component='effect_mode18_semantics',replacement_key='effective_effect_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='Mode18 firstfour slots/reverseorder/floor(N/4) annotations extend prior mode4scale annotations. Full raw field names/sizes preserved; opaqueMisc remainsunresolved.'))
    data['precedence'].append('effect_mode18_semantics phase143 confirms native type18→CBridge→Effectfactory, first4slots inreverse andfloor(N/4)batches. NextEffectId chains canraise totalcount; partialallocation keeps priorinstances andcanreturn1. Slots4..7 andunknownMisc words mustbe preserved. Preparedproviders/nullassets andboundedaliasreview do notprove wholeWorld lifecycle or globalMisc nonuse.')
    data['effective_thing_layouts']=data['wander_target_path_semantics']['effective_thing_layouts']
    data['semantic_corrections'].append(dict(component='thing_layouts',json_pointer='/',
        replacement_component='wander_target_path_semantics',replacement_key='effective_thing_layouts',
        effective_view='effective_thing_layouts',historical_value_retained_for_audit=True,
        scope='Native WanderRadius label/storage annotations only at C6payload17/C7payload1D/C9embedded1D; field names/widths/bytes preserved, gameplayeffect/units unresolved.'))
    data['precedence'].append('wander_target_path_semantics phase144 executes state87->targethelper->original NAV path acceptance/state6 with preparedActor/World and nativeAI/NAVsetup. HardcodedRNG4+4 radius is separate from rawWanderRadius Actor418. Successful wholememory0/255pair proves only thisselectedboundary; no globalnonuse/zero-disables/units inference. effective_thing_layouts preserves rawfields andlabels uncertainty. No physicalmovement/normalWorldscheduler proof.')
    data['precedence'].append('room_wander_semantics phase145 extends the selected native state87 helper to nonempty prepared owner-room lists. RNG8==0 enables the room branch; counting then decrementing tries the last eligible room first, and after failure may try later excluded types. Room targets are not limited by the ring-radius argument. Actual AI/NAV acceptance is proved for selected scenes, not physicalmovement/normalWorldscheduler; rawWanderRadius418 remains unresolved. Historical phase144 schemas/annotations are preserved.')
    data['effective_catalogue_layouts']['Traps']=data['trap_model_word_semantics']['effective_trap_layout']
    data['semantic_corrections'].append(dict(component='catalogue_layouts',json_pointer='/Traps',
        replacement_component='trap_model_word_semantics',replacement_key='effective_trap_layout',
        effective_view='effective_catalogue_layouts',historical_value_retained_for_audit=True,
        scope='One nativeWORD204 replaces two historical rawbytefields204/205 in current decode/edit view. Record579bytes unchanged; no native signedness/gameplay meaning assigned.'))
    data['precedence'].append('trap_model_word_semantics phase146 executes complete original Pro converter/allocated-model/registration/writer chains, preserving all16sourceTrap records and5boundarycopies. CurrentAPI Unknown204:u16 groups formerbytealiases; frozen dk2_formats_phase145.py andbaseline layout preserve oldshape. This is a rawWORD contract, not evidence of ManaGen/upkeep/signedness or old data corruption. No fullMFCGUI/parserinitializer/bootstrap; unknown gameplay remainsopen.')
    data['effective_creature_flag_enum']=data['melee_animation_flag_semantics']['effective_creature_flag_enum']
    data['semantic_corrections'].append(dict(component='enums',json_pointer='/Creature.CreatureFlag',
        replacement_component='melee_animation_flag_semantics',replacement_key='effective_creature_flag_enum',
        effective_view='effective_creature_flag_enum',historical_value_retained_for_audit=True,
        scope='Only bit20000000 is renamed UNKNOWN_5->PLAY_MELEE_INSTANCE_ANIMATION. All other existing labels inherited; no new confidence assigned to them.'))
    data['precedence'].append('melee_animation_flag_semantics phase147 links rawCreatureEFC bit29 via actual loader andmelee entry498E30 to Actor3EA1000, nativelynamed PlayInstanceAnimation. Whole48BE50 uses thisbit in standing active-instance animation selection; selected movingmode0 bypasses it. Currentcodec/effective_creature_flag_enum rename only thisbit; baselineenums andfrozen dk2_formats_phase146.py preserve oldlabel. Raw/melee andrender-selection are separate compositions; no GPU/pixel/fullAI proof.')
    out=ROOT/'editor-format-package.json';out.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
    assert json.loads(out.read_text(encoding='utf8'))==data
    print('Format package:',len(out.read_bytes()),'bytes;',len(PARTS),'schema components')

if __name__=='__main__':main()
