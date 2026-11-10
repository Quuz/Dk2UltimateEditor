"""Integrate completed Variables reader/order evidence, preserving earlier work."""
from copy import deepcopy
import json,struct
import emulate_variables_reader as n
import crosscheck_variables_reader as cross
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha
KEY,SCHEMA='variables_reader_order_semantics','variables-reader-order-semantics.json'
link=lambda name:dict(file=name,sha256=sha((ROOT/name).read_bytes()))
canonical=lambda value:sha(json.dumps(value,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['emulate_variables_reader.py','native_variables_file_api.py','crosscheck_variables_reader.py','prepare_variables_reader_primary.py','build_variables_reader.py','native_unicorn_mapreader.py','emulate_tick_rate.py']


def physical():
    rows=[]
    for path in [n.GLOBAL,n.GLOBAL.parent/'AutoSaveVariables.kld']:
        raw=path.read_bytes();section=next(s for s in n.fmt.parse(raw) if s.signature==230)
        assert section.offset==0 and section.stride==16
        assert b''.join(s.pack() for s in n.fmt.parse(raw))==raw
        count=int.from_bytes(raw[20:24],'little');assert len(section.records)==count
        # Reorder complete records in memory only; all headers/opaque DWORDs/tails survive.
        changed=bytearray(raw);changed[56:88]=raw[72:88]+raw[56:72]
        assert changed[:56]==raw[:56] and changed[88:]==raw[88:]
        assert b''.join(s.pack() for s in n.fmt.parse(bytes(changed)))==changed
        changed[56:88]=changed[72:88]+changed[56:72];assert changed==raw
        rows.append(dict(file=str(path),sha256=sha(raw),first_section_records=count,first_section_end=56+16*count,
            whole_file_bytes=len(raw),raw_byte_exact_roundtrip=True,record_swap_inverse=True,headers_unknown_words_tail_preserved=True))
    return dict(source_writes=0,sources=rows,new_physical_fields=0)


def build():
    prior=load('phase197-prior-package-state.json');assert len(prior['components'])==174
    for ref in prior['components'].values():assert link(ref['file'])==ref
    baseline=load('phase194-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(n.Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    accepted=load('phase197-Variables-emulations.json');assert len(accepted['replays'])==23 and accepted['registration']==n.registration()[1]
    for c,row in zip(n.cases(),accepted['replays']):
        args,ch,saved,file=n.execute(c);_,out=n.verify(c,args,ch,saved['result'])
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
    controls=load('phase197-Variables-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (count,size)==(row['checked_regions'],row['checked_bytes']) and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    primary=load('phase197-Variables-primary.json');assert primary['all_executed_original_PCs_covered']
    scaled=[int(k) for k,v in load('variable-runtime-map.json')['rows'].items() if v['transform']=='signed_i32_divide_by_TPS'];assert len(scaled)==16
    counts=dict(native_reader_scenes=23,capture_scenes=15,actual_per_World_callback_scenes=8,registration_segments=1,registered_types=209,
        independent_Ghidra_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        original_PCs=primary['unique_executed_original_PCs'],whole_function_byte_extents=len(primary['functions']),source_first_section_records=[621,198],
        TPS_scaled_types=len(scaled),prior_components_preserved=174,new_physical_fields=0,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0)
    return dict(phase=197,status='original_outer_dispatch_Variables_reader_file_wrappers_and_selected_runtime_record_order_verified',profile=primary['profile'],counts=counts,
        original_chain=dict(outer_dispatch=0x56CBD0,Variables_reader=0x5716E0,read_wrapper=0x5B3EB0,MyFile_read=0x5B6EA0,Disc_read=0x5FFFB0,
            World_callback=0x521790,scalar_handler=0x51CD90,scaled_handler=0x51CB80),
        registration=dict(global_initializer=0x51AA90,global_rows=0x6A5CD0,global_stride=20,World_offset=0x1F24,World_stride=16,
            World_slots=243,World_bytes=3888,execution=accepted['registration'],scope='Original initializer plus isolated constructor copy segment; its measured table is explicit input to reader scenes. Whole World constructor not executed.'),
        framing=dict(outer_bytes=12,section_tag=230,header_tag=231,header_bytes=36,data_tag=232,data_tag_bytes=8,record_bytes=16,
            record_mid_bytes=32,record_mid_presence=[1,1,1,1],record_raw_words='All four DWORDs copied unchanged to MID+10h..1Ch; trailing two DWORDs remain opaque for scalar cases.',
            record_count_file_offset=20,declared_data_size_file_offset=52,first_record_file_offset=56,
            header_callback_loader_offset=0xD4,record_callback_loader_offset=0xD8,callback_context_loader_offset=0xDC,
            observed='Capture provider received all621 first-section common records and198 first-section siblingAutoSave records, byte exact including opaque words. Not a full application of every union handler.'),
        order_dependence=dict(TPS_type=19,TPS_address=0x6F2590,scaled_types=sorted(scaled),scaled_division=0x51CBA0,
            transform='IDIV signed_i32(raw_value, current_TPS), quotient toward zero; value stored during its own record callback.',
            no_recalculation_scope='A later Type19 record does not revisit earlier scaled records within the executed reader call.',
            diagnostic_initial_TPS=20,examples=[dict(records='19=30; 4=101',final_TPS=30,Type4_runtime=3),dict(records='4=101; 19=30',final_TPS=30,Type4_runtime=5),dict(records='4=101; 19=30; 4=101',final_TPS=30,Type4_runtime=3)],
            negative_example='-101 / 20 = -5, stored asFFFFFFFB.',type174='Bypasses division; an explicit sequence19=0,174=F1234567,19=20 succeeds.',
            runtime_consumer='After each runtime reader, actual collision/Freeze consumes finalTPS and stores low16(5*TPS).',
            limits='Initial20 is a fixture input, not a universal/default TPS. No complete natural reference traversal or live scheduler measurement.'),
        permissive_reader=dict(short_read=dict(wrapper_store=0x5B3EDA,record_test=0x571840,observed='ReadFile successful EOF-short-read -> wrapper status0 -> reader accepts nonnegative status; callback receives prior-buffer bytes for missing record suffix, may return success1.'),
            examples=['Missing last DWORD reuses previous record DWORD4.','Missing whole second record repeats previous16-byte buffer.','Header count3 with only2 data records produces3 callbacks and success1.'],
            ignored_size='With active record callback, declared data size0 still reads count*16; prepared header sizeA5A5A5A5 accepted.',
            skip='With record callback NULL, reader seeks declared data size and compares positions. Explicit SetFilePointer provider accepts seeking beyond EOF; no ReadFile validation occurs for skipped data.',
            rejections='WrongE7/E8 tags, selected header rejection and first record rejection return0.',
            editor_rule='Validate physical boundaries and count/size consistency independently; never treat game success1 as proof of a complete/valid file. Preserve unknown words, record order and trailing sections.'),
        unknowns=dict(Type29=dict(runtime_address=0x6F25C1,bytes=4,meaning='unknown'),Type186=dict(runtime_address=0x6F296A,bytes=4,meaning='unknown'),
            unregistered='Type0 ignored; bounded Type243 probe reads next World table slot before the Type<F3 guard, then skips invocation. This is not arbitrary invalid-Type safety certification.'),
        fixtures=dict(original_PE_code_IAT_vtables='unchanged',OS_imports='Declared bounded memory-backed ReadFile/SetFilePointer/InterlockedIncrement at original external targets. No actual disk access by emulated executable.',
            capture='External header/record capture probes; runtime record handler is original521790.',
            source_extent='Exactly one original56CBD0 section dispatch per scene. Other sections/trailing bytes preserved as input, not dispatched.',
            diagnostics='Rejected early174 scene putTPS0 before inherited definition loaders and faulted before Variables; accepted scene changesTPS inside file after setup. No game installation modified.'),
        primary=primary,native_executions=accepted,independent_Ghidra_controls=controls,physical_preservation=physical(),helpers=[link(name) for name in HELPERS],
        limitations=['No original file open/close, automatic Level reference traversal, whole World constructor or Variables editor writer executed.',
            'Scalar examples do not certify every union callback or all later gameplay consumers. UnknownGlobals29/186 meanings remain unknown.',
            'Exact installed DX profile; no version-generalization or live-game timing claim. No overall completion-percent increase.',
            'Global parameters remain active; Crypt deferred with10 unknown counter meanings; creature resistances/flags next in user priority.'],
        next=['Automatic standard final-pass/reference traversal with declared file providers.','UnknownGlobals29/186 consumers and remaining global parameters.','Creature resistances/flags after global parameters.'])


NOTE='variables_reader_order_semantics phase197:23 original56CBD0/5716E0/MyFile/Disc scenes,4Ghidra/176regions/518588B,12429PC/154byteextents;621+198real first-section records captured;8real521790runtime scenes. Original209-row registration copy segment measured,wholeWorldctor not executed.16TPS-scaled Types divide by currentTPS in record order;later19does not recalculate previous values. EOFshortreads accepted,stale record suffixes repeated;stricteditorbounds required.174priorcomponents/codec/420sources/7EXEs unchanged;175components,Globalsactive,Cryptdeferred.'


def main():
    package=load('editor-format-package.json');path=ROOT/'phase197-prior-package-state.json'
    if not path.exists():
        assert len(package['components'])==174
        dump(path.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    prior=load(path.name)
    for k,h in prior['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(prior['components']),set(prior['components'])|{KEY}]
    s=build();dump(SCHEMA,s);package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package);dump('phase197-Variables-storage.json',s['physical_preservation'])
    dump('phase197-search-status.json',dict(phase=197,goal_status='active',current_block='Global parameters',block_complete=False,counts=s['counts'],schema=link(SCHEMA),next=s['next']))
    print(json.dumps(s['counts']))


if __name__=='__main__':main()
