"""Integrate the original Pro Variables writer without normalizing opaque bytes."""
from copy import deepcopy
from pathlib import Path
import json,struct
import emulate_editor_variables_writer as n
import crosscheck_editor_variables_writer as cross
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha
KEY,SCHEMA='editor_variables_writer_semantics','editor-variables-writer-semantics.json'
link=lambda name:dict(file=name,sha256=sha((ROOT/name).read_bytes()))
canonical=lambda value:sha(json.dumps(value,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['emulate_editor_variables_writer.py','native_editor_variables_writer_cpu.py','crosscheck_editor_variables_writer.py','prepare_editor_variables_writer_primary.py','build_editor_variables_writer.py','audit_editor_variables_writer.py','emulate_editor_global_variables.py','native_editor_text_executor.py','emulate_room_model_chain.py','emulate_room_writer.py']


def physical():
    base=load('phase194-source-baseline.json');files=sections=records=edits=additions=0;examples=[]
    ids,_,_=n.defaults.static_defaults()
    sources=base['source_files']+[dict(file=str(ROOT.parents[1]/'Dungeon Keeper 2/Data/editor/AutoSaveVariables.kld'))]
    for row in sources:
        raw=Path(row['file']).read_bytes();assert not row.get('sha256') or sha(raw)==row['sha256']
        allparts=n.defaults.fmt.parse(raw);assert b''.join(s.pack() for s in allparts)==raw
        selected=[(i,s) for i,s in enumerate(allparts) if s.signature==230]
        if selected:files+=1
        for index,s in selected:
            sections+=1;records+=len(s.records)
            choice=next((i for i,r in enumerate(s.records) if int.from_bytes(r[:4],'little') in ids),None)
            if choice is not None:
                offset=s.offset+len(s.header)+choice*16+4
                for value in [0,0x7fffffff,0x80000000,0xffffffff,4096]:
                    parts=n.defaults.fmt.parse(raw);rs=parts[index].records.copy();rs[choice]=rs[choice][:4]+struct.pack('<I',value)+rs[choice][8:]
                    parts[index].replace_records(rs);changed=b''.join(x.pack() for x in parts)
                    assert changed==raw[:offset]+struct.pack('<I',value)+raw[offset+4:]
                    assert b''.join(x.pack() for x in n.defaults.fmt.parse(changed))==changed
                    assert changed[:offset]+raw[offset:offset+4]+changed[offset+4:]==raw
                    edits+=1
            parts=n.defaults.fmt.parse(raw);part=parts[index];oldhead=part.header
            opaque=struct.pack('<4I',29,0x12345678,0xA55A5AA5,0xCAFEBABE)
            part.replace_records(part.records+[opaque]);changed=b''.join(x.pack() for x in parts)
            expected=bytearray(oldhead)
            for off,val in [(8,int.from_bytes(oldhead[8:12],'little')+16),(20,len(s.records)+1),(len(oldhead)-4,int.from_bytes(oldhead[-4:],'little')+16)]:struct.pack_into('<I',expected,off,val)
            assert part.header==expected and part.suffix==s.suffix
            reparsed=n.defaults.fmt.parse(changed);assert reparsed[index].records[-1]==opaque
            assert reparsed[index].records[:-1]==s.records
            for j,other in enumerate(allparts):
                if j!=index:assert reparsed[j].pack()==other.pack()
            reparsed[index].replace_records(reparsed[index].records[:-1]);assert b''.join(x.pack() for x in reparsed)==raw
            additions+=1
            if len(examples)<2:examples.append(dict(file=row['file'],section=index,source_records=len(s.records),appended_unknown_type=29,exact_inverse=True))
    return dict(files_with_Variables=files,Variables_sections=sections,original_records=records,exact_scalar_value_edits=edits,
        opaque_record_append_remove_controls=additions,examples=examples,baseline_source_files_roundtrip=420,supplemental_source=dict(file=sources[-1]['file'],sha256=sha(Path(sources[-1]['file']).read_bytes())),all_source_files_roundtrip=len(sources),unknown_bytes_headers_suffixes_and_other_sections_preserved=True,source_writes=0)


def build():
    old=load('phase199-prior-package-state.json');assert len(old['components'])==176
    for ref in old['components'].values():assert link(ref['file'])==ref
    assert old['codec']==link('dk2_formats.py')
    base=load('phase194-source-baseline.json')
    for ref in base['source_files']+base['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    accepted=load('phase199-editor-Variables-writer-emulations.json');assert len(accepted['replays'])==22
    for c,row in zip(n.cases(),accepted['replays']):
        f,saved,file=n.execute(c);out=n.verify(c,f,saved['result']);assert row==json.loads(json.dumps(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)))
    controls=load('phase199-editor-Variables-writer-crosschecks.json');assert [r['case'] for r in controls['replays']]==cross.CONTROLS
    for row in controls['replays']:
        args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result'];assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert (count,size)==(row['checked_regions'],row['checked_bytes']) and row['known_FISTP_differences']==cross.rounding_differences(row['case'])
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    primary=load('phase199-editor-Variables-writer-primary.json');assert primary['all_executed_original_PCs_covered']
    for ref in primary['functions']+primary['blocks']:
        assert sha((ROOT/ref['memory']).read_bytes())==ref['memory_sha256']
        if ref.get('assembly'):assert sha((ROOT/ref['assembly']).read_bytes())==ref['assembly_sha256']
    counts=dict(native_scenes=22,successful_writes=20,expected_division_faults=2,native_instructions=sum(r['outcome']['native_steps'] for r in accepted['replays']),
        Ghidra_controls=7,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls['replays']),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls['replays']),
        Ghidra_equal_native_bytes=sum(r['equal_native_bytes'] for r in controls['replays']),Ghidra_known_FISTP_DWORD_differences=sum(len(r['known_FISTP_differences']) for r in controls['replays']),
        whole_native_memory_equal_Ghidra_controls=sum(r['whole_native_output_equal'] for r in controls['replays']),original_PCs=primary['unique_executed_original_PCs'],function_byte_extents=len(primary['functions']),
        prior_components_preserved=176,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,new_physical_fields=0)
    ids,_,_=n.defaults.static_defaults();assert len(ids)==193 and not set(ids)&{19,29,74,186,241,242}
    return dict(phase=199,date='2026-11-11',status='original_Pro_numeric_Variables_writer_and_derived74_verified',profile=primary['profile'],counts=counts,
        chain=dict(Terrain_converter=0x42ED90,Terrain_model_callback=0x49F5E0,numeric_initializer=0x47FFB0,writer=0x4937C0,count=0x48CA00,CreaturePool_writer=0x4943C0,numeric_getter=0x48D840,ftol=0x4ACDA8),
        framing=dict(section=230,info_marker=231,data_marker=232,header_bytes=56,record_bytes=16,metadata_count='48CA00: selected player/pool/availability records + global scalar ID count + Map A80 Type66 count + accepted Type77 entries +3.',
            prepared_scene='Players A68=0 and Type77 count110=0; hence193+3, plus Type66 list or explicitly extended scalar list. Full player/pool/availability/Type77 branches not covered.'),
        numeric_model=dict(Map_double_pointer_offset=0x134,Map_double_count_offset=0x138,prepared_count=248,global_ID_pointer=0x54FC04,global_ID_count=0x54FC08,literal_IDs=ids,
            distinction='Scalar loop directly indexes Map134 without checking138; derived74 helper48D840 bounds-checks Type7 against138 and returns constant0 out of range. Global defaults live in a different context6A0 array.',
            conversion='Load binary64; multiply by4096 only for67/155/158/185/220/221; original __ftol switches x87 to truncation toward0, low32 result stored.',
            fixed_point_types=[67,155,158,185,220,221],fractional_cases='Type100 +/-3.75 -> +/-3; six scaled +/-1.25 -> +/-5120. Signed32 endpoints checked; NaN/infinity/outside signed64 conversion not covered.',
            writer_id_policy='Native initialized list has193 IDs and omits19/29/186/74/241/242. Changing unused array cells does not serialize them. Explicit external caller adding29 to that list makes writer emit it: this is a list policy, not a hard-coded29 blacklist or supported GUI feature.'),
        scratch_bytes=dict(record_offsets=[8,12],widths=[4,4],loop_range=[0x49404C,0x4940D7],type_store=0x494055,value_store=0x4940B9,
            observed='For every scalar in empty-player scenes only Type and Value are assigned; trailing8 bytes retain writer-entry stack scratch. Distinct11223344/55667788 andDEADBEEF/A5A55A5A tails reproduce exactly in all193 records.',
            unseeded_setup_example=[0x004D8A6F,0x144],scope='This proves uninitialized scalar tails in this writer path, not that words2/3 of every Variables union variant are unused. Native model save is not a byte-preserving raw-record roundtrip.',
            editor_rule='Preserve the entire original16-byte record and replace only explicitly edited ranges. Do not classify arbitrary scalar tail values as hashes or counts; do not generate stack residue in the new editor.'),
        derived74=dict(historical_name='GAME_TICKS',verified_consumer_name='GOLD_CAPACITY_MARGIN',appended_unconditionally=True,direct_Map_double74_ignored=True,
            input_type=7,input_conversion='original__ftol first, then signed32 abs via CDQ/XOR/SUB; low32 overflow is retained.',
            Terrain_ID=6,Terrain_GoldValue=dict(raw_offset=0x19A,model_offset=0x36A,kind='u16'),Terrain_MaxHealth=dict(raw_offset=0x1FF,MID_offset=0x2B7,model_offset=0x34A,kind='u16',note='Do not confuse StartingHealth raw1D7/MID28F/model348 with MaxHealth raw1FF/model34A.'),
            TPS=dict(Map_model_offset=0x946,kind='u16'),
            exact_formula='s32(low32(abs(s32(low32(trunc0(Map.values[7]))))*u16(Terrain6.raw19A))) IDIV u16(Terrain6.raw1FF) IDIV u16(Map946), in that order; store low32.',
            ordinary_formula='trunc0(trunc0(abs(trunc0(Type7))*GoldValue/MaxHealth)/TPS), only where product does not overflow signed32.',
            examples=[dict(Type7=-300,GoldValue=3000,MaxHealth=660,TPS=4,result=340),dict(Type7=300,GoldValue=3000,MaxHealth=660,TPS=4,result=340),dict(Type7=-300.99,GoldValue=3000,MaxHealth=660,TPS=4,result=340),dict(Type7=-300,GoldValue=6000,MaxHealth=660,TPS=4,result=681),dict(Type7=-300,GoldValue=3000,MaxHealth=330,TPS=4,result=681),dict(Type7=-300,GoldValue=3000,MaxHealth=660,TPS=20,result=68),dict(Type7=0x40000000,GoldValue=3,MaxHealth=660,TPS=4,result=-406720)],
            source_match='Actual editor AutoSaveVariables hasType7=-300 andType74=340; common GlobalVariables hasType7=-300. Matching arithmetic is not proof that this installation was last saved by this exact EXE.',
            existing_game_consumer='Phase193/195 DX464360 compares carried gold with u32(capacity-margin); this phase explains Pro production of the margin and does not replace that independent consumer evidence.'),
        special_suffix=dict(Type66='Map A7C WORD list, countA80: low byte -> word1, high byte -> word2, word3=0. Diagnostic0201/FEC8 become(66,1,2,0)/(66,200,254,0).',
            Type241='Map B8 binary64*4096 -> __ftol low32, trailing words0; input1.25 ->5120.',Type242='Map C0 DWORD copied verbatim, trailing words0; input11223344.',
            boundary='Other special record groups and their meanings are not declared decoded by this numeric writer phase.'),
        invalid_inputs=dict(observed='TPS0 faults at494181; Terrain6 MaxHealth0 faults at494164, no preceding divisor guard in writer.193scalar records and full196-count header have already reached sink; appended74/241/242 absent.',
            scope='Prepared internal model. This does not prove GUI lets users create these inputs, nor OS error recovery or actual disk-file behavior after a CPU exception.'),
        Ghidra_FPU_limitation=dict(primary=link('ghidra/phase199-Ghidra-FISTP-language.txt'),explanation='Installed12.0.4 ia.sinc FISTP m64 uses round(ST0), fpop, trunc(tmp) independently of FPUControlWord; FLDCW only assigns the register. Original__ftol OR AH,0C before FISTP requires truncation.',
            comparison='Full unmodified Ghidra chain executes. Oracle records exact rounded DWORD differences and requires every other declared non-stack byte to match native. This is not a claim of full native memory equality for all7 scenes; original instructions are never patched to force agreement.',
            observed='DefaultType155=1.6: native6553 versusGhidra6554. FractionalType100=-3.75: native-3 versusGhidra-4. Conversion conclusions use original bytes and native x86 Unicorn semantics, with these independent limits explicit.'),
        fixture_boundaries=dict(source_code='Original PE function bytes untouched; runtime rejects writes to code/read-only sections. Existing fixture initially rebinds explicit Win32 IAT data targets, unlike the DX phase198 no-IAT-rebinding environment.',
            model='Prepared Map A90 and248doubles, not complete original document constructor or actual whole-map load. Native Terrain converter/model callback and numerical default initializer execute.',
            OS='Bounded heap/CFile output sink and declared time/TLS/error/CString synchronization providers; no game disk writes.',
            stack='Two defaults scenes retain scratch produced by preceding native setup. Other scenes explicitly initialize prior4KiB stack scratch A5 before writer via declared external caller, then optionally set two diagnostic tail DWORDs; same inputs in both emulators.',
            discovery_corrections='Unaccepted early probes omitted Map138 count (getter returned0) and mutated StartingHealth1D7 rather than divisor MaxHealth1FF. Corrected accepted suite includes both original-byte mappings and count248. Initial Ghidra driver omitted first stack argument; discarded before comparison. No meaning was inferred from those discovery outputs.'),
        native_executions=accepted,independent_Ghidra_controls=controls,primary=primary,physical_preservation=physical(),helpers=[link(x) for x in HELPERS],
        limitations=['UnknownGlobals29/186 gameplay meanings and alias consumers remain open; this writer does not establish global non-use.',
            'No full original GUI/document load/save, all union branches, I/O failure/exception recovery, missingTerrain6, NaN/infinity or integer-divide overflow matrix.',
            'Native code and chosen providers execute in bounded CPU scenes; no live gameplay/rendering/scheduler.',
            'Global parameters active and incomplete; Crypt deferred, ten counter meanings open; creature resistances/flags follow globals. No overall completion percentage increase.'],
        next=['Resolve actual consumers of unknownGlobals29/186 and other remaining global parameters.','Validate remaining Variables union writer branches where needed.','Creature resistances and flags after global parameters.'])


NOTE='editor_variables_writer_semantics phase199:22Pro native scenes/20writes/2expecteddividefaults;193scalarIDs,6Q12types,uninitializedscalar8bytetrails;74recomputedfromType7*Gold6GoldValue/MaxHealth/TPS,notdirectslot74;66/241/242suffixverified.7unmodifiedGhidracontrolsretainexplicitFISTPround-vs-truncdifferences,otherdeclaredmemoryexact.176priorcomponents/codec/420sources/7EXEspreserved;package177,Globalsactive,Cryptdeferred;29/186meanings/otherwriterbranchesremainopen.'


def main():
    package=load('editor-format-package.json');path=ROOT/'phase199-prior-package-state.json'
    if not path.exists():
        assert len(package['components'])==176
        dump(path.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(path.name)
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    schema=build();dump(SCHEMA,schema);package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package);dump('phase199-editor-Variables-writer-storage.json',schema['physical_preservation'])
    dump('phase199-search-status.json',dict(phase=199,goal_status='active',current_block='Global parameters',block_complete=False,counts=schema['counts'],schema=link(SCHEMA),next=schema['next'],Crypt_deferred=True))
    print(json.dumps(schema['counts']));print(json.dumps(schema['physical_preservation']))


if __name__=='__main__':main()
