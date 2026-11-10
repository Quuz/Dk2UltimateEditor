"""Audit phase166 without promoting editor-loading facts to gameplay meaning."""
import json
from build_editor_variable_loading import ROOT,load,sha,link,build,KEY,SCHEMA

def main():
    s=load(SCHEMA);assert s==build();p=load('editor-format-package.json');prior=load('phase166-prior-components.json')
    assert len(prior)==144 and len(p['components'])==145 and set(p['components'])-set(prior)=={KEY}
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert sha(b)==row['sha256'] and json.loads(b)==p[key]
        if key in prior:assert row==prior[key]
    for family,key,subkey in [('Terrain','terrain_text_stat_semantics','effective_terrain_layout'),
        ('Creatures','creature_work_initial_semantics','effective_creature_layout'),('Rooms','room_writer_semantics','effective_room_layout'),
        ('Doors','native_door_reader_semantics','effective_door_layout'),('Traps','trap_creation_activation_semantics','effective_trap_layout'),
        ('Effects','effect_adjacent_semantics','effective_effect_layout')]:assert p['effective_catalogue_layouts'][family]==p[key][subkey]
    expected=dict(native_scenarios=16,whole_default_initializers=17,whole_Variables_callbacks=21,independent_full_Ghidra_controls=4,
        Ghidra_compared_regions=249,Ghidra_compared_bytes=1175180,literal_default_slots=248,literal_numeric_IDs=193,
        additional_function_extents=7,static_only_function_extents=2,reused_function_extents=4,
        unique_executed_original_PCs=1261,primary_data_blocks=2,exact_container_edits=8,newly_named_opaque_gameplay_fields=0,
        preserved_source_files=420,preserved_executable_paths=7,source_writes=0)
    assert s['counts']==expected
    numeric=s['numeric_loading'];assert numeric['scaled_IDs']==[67,155,158,185,220,221]
    assert numeric['unknown233']['gameplay_meaning']=='unresolved' and numeric['unknown233']['default_editor_model']==40
    assert len(numeric['accepted_IDs'])==193 and 233 in numeric['accepted_IDs'] and 242 not in numeric['accepted_IDs']
    assert s['default_table']['values'][100]==5 and s['default_table']['values'][113:115]==[50,50]
    assert len(bytes.fromhex(s['actual_recipe_control']['raw_hex']))==16
    assert int.from_bytes(bytes.fromhex(s['actual_recipe_control']['raw_hex'])[:4],'little')==43
    byname={r['case']['name']:r['outcome'] for r in s['native_executions']['replays']}
    assert byname['negative80']['values'][0]['model']==-80
    assert byname['unsigned-bits']['values'][0]['model']==-1
    assert byname['reinitialize-retains']['values'][0]['model']==42
    assert all(byname[n]['accepted']==[False] for n in ['unknown19','unknown29','unknown186','not-supported242','temple43'])
    assert all(r['all248_doubles_match_literal_oracle'] and r['all193_IDs_match_literal_oracle'] for r in byname.values())
    assert (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf-8').startswith('Актуальный этап 166:')
    assert 'пакет содержит 145 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf-8')
    assert 'не доказывает потерю' in (ROOT/'EDITOR_VARIABLE_LOADING_RU.md').read_text(encoding='utf-8')
    out=dict(phase=166,date='2026-11-02',**expected,package_components=145,previous144_components_preserved_byte_exact=True,
        physical_codec_and_all_effective_catalogue_layouts_unchanged=True,goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase166-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
    status=dict(phase=166,published_phase=166,date='2026-11-02',package_components=145,readiness_estimate_percent=75,
        previous_goal_turn_classification='Progress: pinnedexternalDk2maptoolrevieweliminatedstubdecodersasnewsemanticevidence.',
        current_goal_turn_classification='Progress:publishedphase165Stat19bindingandphase166originalProVariablesload/defaults.16native/4Ghidra/8exactedits;excluded48131FasTerrainusingtypedarrayproof.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in [SCHEMA,'EDITOR_VARIABLE_LOADING_RU.md','phase166-editor-variables-emulations.json',
            'phase166-editor-variables-crosschecks.json','phase166-editor-variables-primary.json','phase166-editor-variable-storage.json',
            'phase166-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do notrepeatTerrainStat19orProdefaults/selectednumericcallbacks. 48131F/6A0isglobaldoublearray,notTerrain. PursuerealgameUnknown233runtime6F29E9 consumer/nativeUIbinding,otherunknownvariables,fulloriginalVariableswriter/recipepreservation,orremainingopaque15familyfields. Unknown233naming/formulaunresolved;Crypt8/18unchanged.',
        limitations=s['limitations'])
    (ROOT/'phase166-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
