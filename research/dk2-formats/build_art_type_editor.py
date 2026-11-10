"""Publish native editor type-change stack residue and stable-type preservation."""
from pathlib import Path
import hashlib,json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import emulate_art_type_editor as native
import crosscheck_art_type_editor as independent
import prepare_art_type_primary as primary

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def variants():
    blob=native.EXE.read_bytes();body=pe_read(blob,0x41BBB0,145)
    stock=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor.exe'
    old=ROOT/'source-binaries/DK2-Editor-rc3.exe'
    assert pe_read(stock.read_bytes(),0x41BBB0,145)==body
    rc3=pe_read(old.read_bytes(),0x426240,145)
    md=Cs(CS_ARCH_X86,CS_MODE_32);pro=list(md.disasm(body,0x41BBB0));other=list(md.disasm(rc3,0x426240))
    assert len(pro)==len(other)==46 and pro[-1].mnemonic==other[-1].mnemonic=='ret'
    changed=[]
    for a,b in zip(pro,other):
        assert a.address-0x41BBB0==b.address-0x426240 and a.mnemonic==b.mnemonic and a.size==b.size
        if a.bytes!=b.bytes:
            assert a.address in [0x41BBB8,0x41BBEA,0x41BC27]
            changed.append(dict(offset=a.address-0x41BBB0,Pro=a.op_str,RC3=b.op_str,kind=a.mnemonic))
    assert len(changed)==3
    return dict(stock=dict(file=str(stock),sha256=sha(stock.read_bytes()),entry=0x41BBB0,bytes=145,identical_to_Pro=True,native_CPU_cases=0),
        RC3=dict(file=str(old),sha256=sha(old.read_bytes()),candidate_entry=0x426240,bytes=145,instructions=46,
            changed_instruction_immediates=changed,remaining_instruction_bytes_equal=True,native_CPU_cases=0,
            scope='Static PE/Capstone comparison only. Same dual-REP copy and signedness instructions; different handler and CString calls. No new RC3 Ghidra/full UI or native execution.'))


def build():
    manifest=primary.build();assert manifest==load('phase130-art-type-primary.json')
    runs=load('phase130-art-type-emulations.json')['replays'];assert len(runs)==len(native.cases())==22
    copied=same=writer_bytes=0
    for case,row in zip(native.cases(),runs):
        fixture=native.build(case);path=native.path_for(fixture);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==fixture and sha(path.read_bytes())==row['sha256']
        outcome=native.verify(fixture,saved['result']);assert outcome==row['outcome']
        copied+=outcome['stack_to_raw_provenance_checked'];same+=bool(saved['result']['transitions'] and not outcome['stack_to_raw_provenance_checked'])
        writer_bytes+=outcome['exact_original_writer_record_bytes']
    assert copied==12 and same==8 and writer_bytes==4220
    controls=load('phase130-art-type-crosschecks.json')['replays'];assert len(controls)==3
    for row in controls:
        args,count,size=independent.build(row['case']);saved=load(row['file']);result=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    preserved=load('room-model-chain-semantics.json')['preservation']
    for row in preserved['original_executable_paths']+preserved['original_Room_sources']:
        assert sha(Path(row['file']).read_bytes())==row['sha256']
    previous=ROOT/'phase130-prior-components.json'
    if not previous.exists():
        prior=load('editor-format-package.json')['components'];assert len(prior)==108
        previous.write_text(json.dumps(prior,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==108
    return dict(phase=130,date='2026-10-29',family='Shared ArtResource original editor model/type-change behavior',
        profile=dict(executable=str(native.EXE),sha256=sha(native.EXE.read_bytes())),primary=manifest,
        native_type_labels=manifest['type_labels'],
        UI_binding=dict(dialog=276,language=2057,type_combo=1658,type_change_UI_handler=0x433CF0,
            OK_control=12431,OK_handler=0x4349F0,initializer=0x4333D0,type7_numeric_label='ID',
            type7_label_address=0x532BA8,procedure_field_control=1660,dialog_value_offset=0x68,dialog_value_kind='double',
            getter=0x41C080,setter=0x41C090,model_resource_object_offset=0,model_type_offset=0x54,
            model_procedure_DWORD_offset=0x48,raw_ArtResource_procedure_offset=0x44,
            native_type_tables=[0x50C0F8,0x513198],table_ids=list(range(9)),terminator=255,
            static_only='Native table, resource bytes and call sites; live dialog/DDX commit not executed. No universal enum limit or UI numeric range inferred.'),
        type_change=dict(entry=0x41BBB0,object_raw_offset=4,raw_bytes=84,
            guard='signextend8(requested type) == zeroextend8(existing type) skips reinitialization.',
            sequence=['Keep old name in a real CString.','Write requested type at local84-byte stack buffer+80.',
                'Clear all84model raw bytes through REP STOSD.','Copy21DWORD from local buffer through REP MOVSD.',
                'Restore name and recompute cached #suffix through41BC50/41C0B0.'],
            local_buffer_entry_stack_relative=-0x60,only_source_byte_assigned=80,unassigned_source_bytes=83,
            source_write_PC=0x41BBE6,zero_loop_PC=0x41BBFC,copy_loop_PC=0x41BC05,
            confirmed_defect='Requested-type changes copy caller stack residue into resource metadata; clearing destination doesnot initialize the local source.',
            unsigned_type_edge='Diagnostic same128/same255 also reinitialize because requested type is sign-extended. Native GUI table onlyoffers0..8; unsupported-value UI scenario not executed.',
            preserved_when_known_type_same='Full prepared ArtResource and cached index remain identical for same7→7; selecting procedureID afterward alters only its DWORD.',
            unknowns='Doesnot assign meanings to opaque union DWORDs; doesnot prove every strange corpus value originated here.'),
        other_helpers=dict(constructor=dict(entry=0x41BB40,raw84_zero=True,cached_index_zero=True),
            set_procedure=dict(entry=0x41C090,requires_type7=True,width=4,unknown_u32_preserved=True),
            get_procedure=dict(entry=0x41C080,wrong_type_returns=0),
            enable_or_clear=dict(entry=0x41C240,nonzero='OR1 into flags',zero='Clears all84rawbytes including name/type/union; cached #index is not cleared in this path.')),
        native_executions=runs,independent_Ghidra_controls=controls,
        full_Room_writer=dict(compositions=4,source_base='One standalone Treasury RoomID1 record with explicit diagnostic CompleteResource',
            raw_resource_offset=0xC8,model_resource_object_offset=0x16C,model_raw_resource_offset=0x170,
            chain=[0x42DF90,0x49FC30,0x41BBB0,0x41C240,0x41C090,0x4A7AC0],
            compared_entire_models_bytes=5408,compared_entire_records_bytes=4220,containers_reparsed_and_repacked=4,
            record_scope='All1055bytes predicted from prepared raw Room plus resource change and the previously established unassigned raw36F writer-stack byte, captured before writer. Not source-file identity.',
            stack_metadata_result='For prepared new0→7 fullRoom cases, opaque1 becomes17000000h andopaque2=0 from the observed prior native stack; these are fixture outcomes, not defaults.',
            stable7_result='Known opaque1=55667788h/opaque2=99AABBCCh survive same-type ID edit and original writer.'),
        other_editor_static_projection=variants(),
        counts=dict(native_compositions=22,type_helper_calls=20,stack_copy_transitions=12,known_same_type_no_copy=8,
            original_writer_compositions=4,exact_record_bytes_verified=4220,entire_Room_models_preserved_except_resource_bytes=5408,
            independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),fresh_function_spans=11,inherited_key_spans=31,fresh_data_blocks=7,
            new_corpus_API_rebuilds=0,original_Room_source_files_preserved=82,original_executable_paths_preserved=7,source_writes=0),
        preservation=preserved,
        implementation_guidance=['Preserve all84resource bytes and unknown values when type is unchanged; ID-only edits must not normalize adjacent DWORDs.',
            'For a requested type change, initialize the new resource deterministically and explicitly carry intended name/flags; do not reproduce uninitialized stack residue.',
            'Loading type0 raw data and explicitly clearing a resource are different paths; do not destroy stored type0 metadata merely because the native GUI clear helper does so.',
            'Unknown resource types are not invalid merely because this native combo offers0..8; avoid fallback-to-none data loss.',
            'Native getter/setter support fullu32 procedure ID; ID label does not by itself establish all valid gameplay values.'],
        helpers=[link(n) for n in ['emulate_art_type_editor.py','crosscheck_art_type_editor.py','prepare_art_type_primary.py',
            'emulate_room_model_chain.py','native_room_writer.py','native_procedural_resources.py']],
        limitations=['Exact Pro runtime, prepared MFC/CRT/Win32/file/stack environment; no live Windows GUI, MFC DDX/whole OKhandler or complete application bootstrap.',
            'Type-change metadata is not stable default data; four Room fixtures do not reproduce every real caller frame or every catalogue writer.',
            'First-source overwrite is directly observed; it doesnot explain every historical nonzero opaqueDWORD or establish their general game meaning.',
            'Stock helper bytes and RC3 instruction pattern are static projections, with0new runtime controls in those profiles.',
            'Only the selected short NUL-terminated name/#suffix fixture is exercised, not all text lengths/encodings.',
            'Previously published2320procedural API rebuilds remain historical, not recounted;4new native-writer containers are separately validated.',
            'No original or parallel editor fixes, source writes or game patch. Procedural selector3, opaqueDWORD meanings, ElementB1B2 and significant15family unknowns remain.'])


if __name__=='__main__':
    value=build();(ROOT/'art-resource-editor-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
