"""Audit127 components and bounded original Shot firsttick/terminal semantics."""
import json
from build_shot_lifecycle import ROOT,load,sha,link,build
import native_shot_lifecycle as api

def main():
    value=load('shot-lifecycle-semantics.json');assert value==build()
    package=load('editor-format-package.json');old=load('phase148-prior-components.json')
    assert len(package['components'])==127 and len(old)==126
    assert set(package['components'])-set(old)=={'shot_lifecycle_semantics'}
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert sha(data)==row['sha256'] and json.loads(data)==package[key]
        if key in old:assert row==old[key]
    assert package['shot_lifecycle_semantics']==value
    current=package['effective_shot_parameters'];assert current==api.effective_parameters()
    previous=package['area_damage_semantics']['effective_shot_parameters']
    assert current['processes']==previous['processes']
    assert [k for k in current['damage_types'] if current['damage_types'][k]!=previous['damage_types'][k]]==['1','5','6']
    assert package['catalogue_layouts']['Shots']['fields']==load('layouts.json')['Shots']['fields']
    assert package['effective_creature_flag_enum']==package['melee_animation_flag_semantics']['effective_creature_flag_enum']
    assert package['effective_catalogue_layouts']['Traps']==package['trap_model_word_semantics']['effective_trap_layout']
    assert package['effective_catalogue_layouts']['Doors']==package['door_manufacture_split_semantics']['effective_door_layout']
    c=value['counts']
    for key,wanted in dict(native_scenarios=22,independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=354,Ghidra_compared_bytes=812784,
        original_function_extents=120,unique_executed_original_PCs=5020,primary_data_blocks=4,new_exact_container_edits=164,
        public_decode_rebuild_controls=164,source_Shot_records=5551,observed_source_mode5_records=0,observed_source_mode6_records=0,
        preserved_source_files=310,preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0).items():assert c[key]==wanted,(key,c[key])
    rows={r['case']['name']:r['outcome'] for r in value['native_executions']['replays']}
    assert rows['start-factory']['HP']==rows['end-first-tick']['HP']==[1000,1000]
    assert rows['start-first-tick']['HP']==rows['start-second-tick']['HP']==[920,920]
    assert rows['end-gate1']['HP']==rows['end-gate2']['HP']==[1000,1000]
    assert rows['end-handler']['HP']==[920,920] and rows['end-handler']['ticks']==0 and rows['end-handler']['shot_health']==32
    assert rows['end-expire-tick']['HP']==[1000,1000] and rows['end-expire-tick']['shot_health']==0 and rows['end-expire-tick']['state']==0
    assert rows['end-expire-then-handler']['HP']==[920,920] and rows['end-expire-then-handler']['state']==2
    assert rows['start-negative-data1']['HP']==rows['end-negative-data1']['HP']==[1080,1080]
    assert rows['start-radius-zero']['requests']==[] and len(rows['start-data1-zero']['requests'])==2
    assert all(r[1]==0 for r in rows['start-data1-zero']['requests'])
    assert rows['start-no-decay-second-tick']['shot_health']==32
    assert 'Актуальный этап 148:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 127 компонентов' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    out=dict(phase=148,date='2026-10-31',**c,package_components=127,previous126_components_preserved_byte_exact=True,
        physical_Data1_storage_unchanged=True,diagnostic_modes5and6=True,normal_World_scheduler_or_pool_free_executed=False,
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase148-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=148,published_phase=148,date='2026-10-31',package_components=127,readiness_estimate_percent=75,
        status='Published complete selectedShot firsttick/terminal execution, exactzero terminalgate and testednegativeData1; no opaquephysicalfield newlynamed.',
        previous_goal_turn_classification='No progress on research: answered user readiness question and checked current authoritative phase147 package; activeobjective unchanged.',
        current_goal_turn_classification='Progress:22acceptednativecases/6independentGhidra/120originalfunctionextents/164exactcontaineredits; lifecycle andsignedparameter evidence published as127thcomponent.',
        transient_tool_failure='Automaticapproval usage-limit review failure interrupted one primarybuild; sameapprovedcommand laterexecuted successfully. No unresolvedtoolblocker.',
        pending_native_jobs=[],pending_controls=[],goal_achieved=False,significant_unknowns_remain=True,
        evidence=[link(n) for n in ['shot-lifecycle-semantics.json','SHOT_LIFECYCLE_RU.md','phase148-shot-lifecycle-emulations.json','phase148-shot-lifecycle-crosschecks.json',
            'phase148-shot-lifecycle-primary.json','phase148-shot-lifecycle-api-validation.json','phase148-validation.json','editor-format-package.json','INTEGRATION_RU.md','READINESS_RU.md']],
        next_action='Do not repeat22Shotcases/6Ghidra/164edits. FreshWorldgatewriter510E90 writesWorld723B=1 wheneventownerWORD10 equalsWorldvslot264; broadermeaningunresolved andnotnamed. Use savedassembly/decompile aslead,not proof. Prioritize unknownCreatureextraFlagsbit2,Jump/Priority,Traps204/Doors236/Terrain198224/AP0408/KeeperC8. CreaturemainFlags800/1000 straightline/untyped scans negative; preserve unknown labels. NormalWorldscheduler,physicalpoolfree anddamageoverflow boundaries remainopen. No source/parallel editor writes.',
        limitations=value['limitations'])
    (ROOT/'phase148-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in out.items() if k!='limitations'}))

if __name__=='__main__':main()
