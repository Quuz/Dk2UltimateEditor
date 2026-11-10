"""Publish bounded Terrain16WORD storage/checksum evidence, leaving meaning unknown."""
from pathlib import Path
import copy,json
import emulate_terrain_opaque_table as native
import crosscheck_terrain_opaque_table as controls
import prepare_terrain_table_primary as primary
from validate_terrain_table_edit import validate as validate_edits
from validate_terrain_table_editor import validate as validate_editor

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_terrain_layout():
    value=copy.deepcopy(load('terrain-lean-semantics.json')['effective_terrain_layout'])
    count=0
    for field in value['fields']:
        if 0x1AE<=field['offset']<0x1CE:
            assert field['size']==2 and field['kind']=='u16' and field.get('semantics')=='unresolved'
            field.update(phase134_evidence='terrain-opaque-table-semantics.json',
                opaque_array_index=(field['offset']-0x1AE)//2,
                native_storage='raw16WORD1AE→intermediate266 with presenceDWORD B4→editor model328. Preserve order and all16bits.',
                selected_game_loader='Two exact EXEs read these32bytes only in additive checksum loop in selected complete load paths; no semantic property copy to460byte definition. Not a proof of global nonuse.',
                preservation_rule='Preserve unknown values. They contribute to runtime intermediate-byte signature when its computation is enabled; this is not a raw checksum field.')
            count+=1
    assert count==16
    return value

def build():
    attested=primary.build();assert attested==load('phase134-Terrain-table-primary.json')
    runs=load('phase134-Terrain-table-emulations.json')['replays'];assert len(runs)==len(native.cases())==12
    for case,row in zip(native.cases(),runs):
        args,checks=native.build(case);path=native.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==args and saved['case']==case and sha(path.read_bytes())==row['sha256']
        assert saved['result']['source_exe_sha256']==sha(native.PROFILES[case['profile']]['exe'].read_bytes())
        assert native.verify(case,args,checks,saved['result'])==row['outcome']
    by_name={r['case']:r['outcome'] for r in runs}
    assert len({r['outcome']['definition_without_checksum_sha256'] for r in runs})==1
    for profile in native.PROFILES:
        assert by_name[f'{profile}-0-ascending']['entire_definition_sha256']==by_name[f'{profile}-0-reversed']['entire_definition_sha256']
    cross=load('phase134-Terrain-table-crosschecks.json')['replays'];assert len(cross)==4
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    corpus=validate_edits();assert corpus==load('phase134-Terrain-table-api-validation.json')
    editor=validate_editor();assert editor==load('phase134-Terrain-table-editor-validation.json')
    preservation=load('terrain-lean-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:
        assert sha(Path(row['file']).read_bytes())==row['sha256']
    assert len(load('phase134-prior-components.json'))==112
    return dict(phase=134,date='2026-10-30',family='Terrain',semantic_status='Unknown purpose; storage and selected loader checksum role proved',
        field=dict(raw_offset=0x1AE,bytes=32,count=16,element_kind='u16 little endian',intermediate_offset=0x266,
            intermediate_presence_DWORD=0xB4,editor_model_offset=0x328,meaning=None,unknown_preserved=True),
        primary=attested,editor_storage=editor,
        loader=dict(intermediate_bytes=736,definition_bytes=460,checksum_destination=0x84,
            checksum_formula='Sum all736unsigned intermediate bytes; not sum16WORDs, CRC or cryptographic hash.',
            DX=dict(function=0x4F66E0,loop_PC=0x4F670E,gate_DWORD=0x6FCDCF,condition='Compute only when gate==0. At nonzero gate tested constructor sentinelA5A5A5A5 remains atdefinition84.',
                sound='The same prepared nonzero gate skips selected sound lookups; raw table still converted and preserved in intermediate.'),
            old=dict(function=0x4EB330,loop_PC=0x4EB355,condition='Always computes at entry in this full loader; no matching gate here.',
                version='Exact supplied DKII-local13.exe hash only. Retail1.3 provenance not certified.'),
            observed_table_reads='All32table bytes and4presence bytes read one byte at a time at checksumPC when enabled; zero reads of those spans when skipped. Full460definition otherwise identical in all12prepared cases.',
            byte_sum_examples=dict(zero=8396,ascending=8908,reversed=8908,highbit=12124),
            permutation_result='Ascending and reversed16WORDs have same byte sum and entire460byte definition, despite different raw/intermediate table order.',
            World_accumulator_static_only=dict(DX_callback=0x51EB50,old_callback=0x512820,
                rule='Normal allocation branch adds selecteddefinition84 into World15B0 and incrementsWorld15AC. DX alternateA3C7branch loads existing definition and returns before these updates.',
                executed=False,whole_game_checksums_or_network_protocol_proven=False),
            raw_container_distinction='Runtime sum lives in allocated definition84, after raw→736byte conversion. It is not a stored32byte table checksum or a new KWD header repair rule.'),
        native_executions=runs,independent_Ghidra_controls=cross,corpus=corpus,
        effective_terrain_layout=effective_terrain_layout(),preservation=preservation,
        counts=dict(full_native_raw_loads=12,DX_loads=8,old_loads=4,original_executed_function_spans=12,
            static_only_callback_spans=2,inherited_Pro_storage_spans=4,
            unique_original_PCs_by_profile={p:v['unique_executed_original_PCs'] for p,v in attested['profiles'].items()},
            independent_Ghidra_CPU_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),new_exact_container_rebuilds=82,
            identity_record_roundtrips=82,original_Terrain_files=82,preserved_source_files=85,original_executable_paths=7,
            newly_named_unknown_fields=0,source_writes=0),
        helpers=[link(n) for n in ['emulate_terrain_opaque_table.py','crosscheck_terrain_opaque_table.py',
            'prepare_terrain_table_primary.py','native_terrain_opaque_table.py','validate_terrain_table_edit.py',
            'validate_terrain_table_editor.py','native_unicorn_factorytrace.py','native_terrain_catalogue.py']],
        limitations=['Array purpose and individual index meanings remain unknown. Selected whole loader paths are not a proof of global nonuse or editor-only storage.',
            'Original ClaimedPath8 raw base, only16WORDs and specialgraphics3000h cleared. Null graphics and declared null sound virtual provider; no live renderer, fullWorld loading or game bootstrap.',
            'World15B0 accumulation callbacks are full-span static evidence only; not executed by12native cases. No claim about multiplayer checksum usage, saves or desync consequences.',
            'Old executable identified by hash, not certified retail1.3. No normal Windows/patchDLL run or original editor GUI execution.',
            'RC3getter and29byte local writer loop match statically. No wholeRC3writer execution; one bounded Proxref pergetter/setter does not rule out indirect/UI access.',
            'Gate nonzero preserves prepared constructor checksum sentinel; A5A5A5A5 is diagnostic initial memory, not a game-defined default.',
            'No original game/editor file changes. All other significant unknowns across15families remain; no resistance repair or spell-slot expansion.'])

if __name__=='__main__':
    value=build();(ROOT/'terrain-opaque-table-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
