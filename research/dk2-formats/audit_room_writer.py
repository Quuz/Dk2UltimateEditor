"""Audit complete Room save evidence and current physical layout correction."""
from pathlib import Path
import json
from build_room_writer_semantics import ROOT,load,link,build,native
from build_poison import effective_shot_parameters

def main():
    value=load('room-writer-semantics.json');assert value==build()
    package=load('editor-format-package.json');prior=load('phase122-prior-components.json')
    assert len(package['components'])==101 and len(prior)==100
    assert set(package['components'])-set(prior)=={'room_writer_semantics'}
    assert package['room_writer_semantics']==value
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert native.sha(b)==row['sha256'] and json.loads(b)==package[key]
        if key in prior:assert row==prior[key]
    current=package['effective_catalogue_layouts']
    assert current['Rooms']==value['effective_room_layout']
    for family,layout in package['catalogue_layouts'].items():
        if family!='Rooms':assert current[family]==layout
    fields=current['Rooms']['fields']
    assert next(f for f in fields if f['offset']==0x36E)['size']==1
    assert next(f for f in fields if f['offset']==0x36F)['kind']=='opaque8'
    assert sorted(i for f in fields for i in range(f['offset'],f['offset']+f['size']))==list(range(1055))
    assert package['effective_object_resource_bindings']['slots'][2]['display_name']=='EditorMapIconResource'
    assert package['effective_shot_parameters']==effective_shot_parameters()
    assert package['outstanding_controls_from_phases114_115']==0
    assert 'Актуальный этап 122:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 101 компонент' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    primary=load('phase122-primary-manifest.json');assert len(primary['functions'])==22 and primary['linear_match_ranges']==21
    controls=load('phase122-room-writer-crosschecks.json')['replays']
    assert sum(r['checked_regions'] for r in controls)==20 and sum(r['checked_bytes'] for r in controls)==42008
    output=dict(phase=122,date='2026-10-29',full_native_Room_writer_cases=17,
        independent_Ghidra_CPU_controls=2,Ghidra_compared_regions=20,Ghidra_compared_bytes=42008,
        original_native_date_helper_executed=True,original_code_substitutions=0,Win32_IAT_data_bindings_per_fixture=5,
        key_function_memory_spans=22,linear_instruction_match_spans=21,primary_data_blocks=5,
        original_Room_source_files_preserved=82,Room_records_inspected=2075,
        exact_container_rebuilds=246,changed=164,identity=82,API_refusals=6,
        native_TorchIntensity_width=1,raw36F_preserved_as_opaque=True,
        original_writer_unassigned_raw_bytes=[0x36F],Room_header_unassigned_bytes=[42,43,47],
        unknown_Room1C4_meaning_resolved=False,unknown_Trap204_meaning_resolved=False,
        original_executable_paths_preserved=6,previous100_components_preserved_byte_exact=True,
        package_components=101,goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase122-validation.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    status=dict(phase=122,published_phase=122,date='2026-10-29',
        status='Published complete original Room serialization with native calendar path and TorchIntensityu8 correction;Rooms1C4/Traps204 remainunresolved.',
        previous_goal_turn_classification='progress:ObjectsC8 editor-map icon confirmed,18CPU/2Ghidra/328rebuilds and100componentpackage.',
        current_goal_turn_classification='progress:17completeRoomwriter runs,native date helper/TLS calendar preserved,2Ghidra controls,246exactrebuilds,corrected raw36Ewidth and101componentpackage.',
        pending_native_jobs=[],pending_controls=[],package_components=101,readiness_estimate_percent=75,
        evidence=[link(n) for n in ['room-writer-semantics.json','ROOM_WRITER_RU.md',
            'phase122-primary-manifest.json','phase122-room-writer-emulations.json','phase122-room-writer-crosschecks.json',
            'phase122-room-api-validation.json','phase122-validation.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']],
        next_action='Do notrepeat17Roomwriter cases/2Ghidra controls/246rebuilds. NewverifiedTorchIntensitybyte goes todefinition45E;actual lighting consumer/units and palette path canbe traced fromthis typedproducer ratherthanolduntypedoffsets. Rooms1C4purpose remainsopen;finite24model280scan gave no newrenderer. TrapManaGen edit1788/dialog5C doesnotestablishraw204. Otherremaining15family consumer/load/save questions remainpriority. Native timestamp helper facts canbe checkedin othercatalogue callers/readers without reusing provider-stub padding assumptions. BothresearchGhidra projects currently retained;verifytheiractualprocesses beforefuture use.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=value['limitations'])
    (ROOT/'phase122-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in output.items() if k!='limitations'}))

if __name__=='__main__':main()
