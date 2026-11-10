"""Audit125 schemas, native original Trap chains and the current atomic WORD API."""
import json
from build_trap_model_word import ROOT,load,sha,link,build
from native_trap_field_layout import effective_trap_layout
def main():
    v=load('trap-model-word-semantics.json');assert v==build()
    p=load('editor-format-package.json');old=load('phase146-prior-components.json')
    assert len(p['components'])==125 and len(old)==124
    assert set(p['components'])-set(old)=={'trap_model_word_semantics'}
    for k,row in p['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==p[k]
        if k in old:assert row==old[k]
    assert p['trap_model_word_semantics']==v
    assert p['effective_catalogue_layouts']['Traps']==effective_trap_layout()
    assert p['catalogue_layouts']['Traps']!=effective_trap_layout()
    before=p['catalogue_layouts']['Traps']['fields'];after=effective_trap_layout()['fields']
    assert len(after)==len(before)-1 and [f for f in before if f['offset'] not in [0x204,0x205]]==[f for f in after if f['offset']!=0x204]
    word=next(f for f in after if f['offset']==0x204);assert word['size']==2 and word['kind']=='u16' and word['name']=='Unknown204'
    assert p['effective_catalogue_layouts']['Doors']==p['door_manufacture_split_semantics']['effective_door_layout']
    assert p['effective_thing_layouts']==p['wander_target_path_semantics']['effective_thing_layouts']
    c=v['counts'];assert c['native_scenarios']==6 and c['total_raw_model_chains']==21 and c['complete_writer_runs']==6
    assert c['independent_Ghidra_CPU_controls']==2 and c['Ghidra_compared_regions']==142 and c['Ghidra_compared_bytes']==600370
    assert c['additional_original_function_extents']==47 and c['inherited_original_function_extents']==26 and c['unique_executed_original_PCs']==2336
    assert c['new_exact_container_edits']==c['public_decode_rebuild_controls']==82 and c['source_Trap_records']==1293
    assert c['preserved_source_files']==310 and c['preserved_executable_paths']==7
    assert c['newly_named_opaque_gameplay_fields']==c['source_writes']==0
    assert v['preservation']['previous_source_roster_count']==309 and v['preservation']['added_historical_Trap_sources']==1
    assert v['codec']['not_a_legacy_corruption_bug'] and v['field']['meaning'].startswith('Unresolved')
    assert 'Актуальный этап 146:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 125 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=146,date='2026-10-31',**c,package_components=125,previous124_components_preserved_byte_exact=True,
        original_Trap_record_roundtrip=True,current_Traps_WORD204=True,original_gameplay_meaning='Unresolved',
        goal_achieved=False,significant_unknowns_remain=True,limitations=v['limitations'])
    (ROOT/'phase146-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=146,published_phase=146,date='2026-10-31',package_components=125,readiness_estimate_percent=75,
      status='Published full originalTrap raw/model/save preservation and nativeWORD204 currentAPI; gameplay purpose/signedness still unresolved.',
      previous_goal_turn_classification='Progress:phase145published38room-nativecases,6Ghidracontrols and124-component package.',
      current_goal_turn_classification='Progress:6fullTrapmodel/savecases,2Ghidra/47newspans+26inherited/82exactcontaineredits,atomicWORD204publicAPI and125-componentpublication. OldTerrainscanlimit was loader4F66E0, not a new consumer; not rerun.',
      pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
      evidence=[link(n) for n in ['trap-model-word-semantics.json','TRAP_MODEL_WORD_RU.md','phase146-Traps-model-emulations.json','phase146-Traps-model-crosschecks.json',
        'phase146-Traps-model-primary.json','phase146-Trap-WORD-api-validation.json','phase146-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
      next_action='Do not repeat6Trapmodelcases/2Ghidra/82WORDedits oroldTerrain88scan. Unknown204purpose remainsopen despite nativeWORD shape. Prioritize newtyped gameplay consumer or realeditorbinding for remainingTraps204/Doors236/CreatureJumpEE0/PriorityF55/Terrain198224/AP0408/KeeperC8. Current effectiveTraps/publiccodec useUnknown204; historicalbytefields are provenance, not two properties. No source/parallel editor writes.',
      limitations=v['limitations'])
    (ROOT/'phase146-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))
if __name__=='__main__':main()
