"""Audit corrected public Door WORD API, source preservation and121 schemas."""
import json
import dk2_formats as fmt
from build_door_manufacture_split import ROOT,load,sha,link,build,effective_door_layout

def main():
    value=load('door-manufacture-split-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase142-prior-components.json')
    assert len(package['components'])==121 and len(prior)==120
    assert set(package['components'])-set(prior)=={'door_manufacture_split_semantics'}
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==package[key]
        if key in prior:assert row==prior[key]
    assert package['door_manufacture_split_semantics']==value
    current=package['effective_catalogue_layouts']['Doors'];old=package['catalogue_layouts']['Doors']
    assert current==effective_door_layout()==fmt.CURRENT_DOOR_LAYOUT
    assert current['record_size']==old['record_size']==616 and len(current['fields'])==len(old['fields'])+1
    fields={f['offset']:f for f in current['fields']};assert fields[0x234]['size']==fields[0x236]['size']==2
    assert fields[0x234]['kind']==fields[0x236]['kind']=='u16'
    for f in old['fields']:
        if f['offset']!=0x234:assert fields[f['offset']]==f
    assert [i for f in current['fields'] for i in range(f['offset'],f['offset']+f['size'])]==list(range(616))
    assert package['effective_catalogue_layouts']['Effects']==package['effect_generation_scale_semantics']['effective_effect_layout']
    assert package['effective_catalogue_layouts']['EffectElements']==package['element_palette_chain_semantics']['effective_element_layout']
    assert package['effective_catalogue_layouts']['Terrain']==package['terrain_opaque_table_semantics']['effective_terrain_layout']
    assert package['effective_shot_parameters']==package['area_damage_semantics']['effective_shot_parameters']
    c=value['counts'];assert c['native_raw_chains']==5 and c['paired_whole_memory_controls']==3 and c['paired_compared_bytes']==5283
    assert c['independent_Ghidra_CPU_controls']==3 and c['Ghidra_compared_regions']==21 and c['Ghidra_compared_bytes']==5283
    assert c['executed_function_spans']==7 and c['unique_executed_original_PCs']==417
    assert c['new_exact_container_edits']==c['public_decode_rebuild_controls']==c['legacy_clobber_reproductions']==82
    assert c['preserved_source_files']==200 and c['preserved_executable_paths']==7
    assert c['newly_corrected_public_field_widths']==1 and c['newly_named_opaque_fields']==c['source_writes']==0
    assert value['API_validation']['native_door_records']==656 and value['API_validation']['source_nonzero_upper_words']==0
    assert 'Актуальный этап 142:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 121 компонент' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=142,date='2026-10-31',**c,previous120_components_preserved_byte_exact=True,package_components=121,
        public_research_codec_fixed=True,historical_codec_frozen=True,original_editor_bug_claim=False,
        source_corruption_claim=False,unknown236_meaning='Unresolved',global_unused_claim=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase142-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=142,published_phase=142,date='2026-10-31',package_components=121,readiness_estimate_percent=75,
        status='Published corrected Door manufacture WORD API/effective layout andnative raw-loader proof; unknown236 remainsunresolved.',
        previous_goal_turn_classification='Progress: phase141 published actual generation-scale consumer,18 raw chains/9 fragments/5 Ghidra/82 edits and120 components.',
        current_goal_turn_classification='Progress: reproduced researchcodec DWORD clobber, froze historicalcodec, corrected publicdecode/edit/effectiveDoorwidth,5 nativechains/3 pairs/3 Ghidra,82 decode/rebuild/exactedits,121components. Added independentlyhashedDoors source to200 preservation list.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['door-manufacture-split-semantics.json','DOOR_MANUFACTURE_SPLIT_RU.md','phase142-door-split-primary.json',
            'phase142-door-split-emulations.json','phase142-door-split-crosschecks.json','phase142-door-split-api-validation.json','phase142-effect-misc-initial-review.json',
            'phase142-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do not repeat five Door chains/three Ghidra/82 edits or historicalgeneric setter bug. Unknown236 meaning stillopen. EffectsMisc literal inventory is only firstpass; seek genuine adjusted/helper consumer or unclosedgeneration path. Other remaining Traps204,Terrain198224,CreatureJump/Priority,KeeperC8 stillopen. Source/parallel editor mustremainunmodified.',
        limitations=value['limitations'])
    (ROOT/'phase142-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
