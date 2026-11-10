"""Integrate original final-pass file traversal and reference failure semantics."""
from copy import deepcopy
from pathlib import Path
import json,struct
import emulate_reference_loading as n
import crosscheck_reference_loading as cross
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha
KEY,SCHEMA='level_reference_loading_semantics','level-reference-loading-semantics.json'
link=lambda name:dict(file=name,sha256=sha((ROOT/name).read_bytes()))
canonical=lambda value:sha(json.dumps(value,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['native_reference_file_api.py','native_unicorn_reference.py','emulate_reference_loading.py','crosscheck_reference_loading.py','prepare_reference_loading_primary.py','build_reference_loading.py','emulate_level_globals.py','emulate_variables_reader.py','crosscheck_variables_reader.py']


def physical():
    raw=n.level.SOURCE.read_bytes();sections=n.variables.fmt.parse(raw);s=next(s for s in sections if s.signature==220)
    assert b''.join(s.pack() for s in sections)==raw
    df=len(s.header);length=int.from_bytes(s.header[-4:],'little');refstart=s.offset+df+length+8
    assert length==25683 and int.from_bytes(s.header[20:22],'little')>=2
    edited=bytearray(raw);edited[refstart:refstart+144]=raw[refstart+72:refstart+144]+raw[refstart:refstart+72]
    assert edited[:refstart]==raw[:refstart] and edited[refstart+144:]==raw[refstart+144:]
    assert b''.join(s.pack() for s in n.variables.fmt.parse(bytes(edited)))==edited
    edited[refstart:refstart+144]=edited[refstart+72:refstart+144]+edited[refstart:refstart+72];assert edited==raw
    edits=[];off=s.offset+df+0x1182
    for val in [0,1,65535]:
        changed=raw[:off]+struct.pack('<H',val)+raw[off+2:]
        assert b''.join(s.pack() for s in n.variables.fmt.parse(changed))==changed
        assert changed[:off]+raw[off:off+2]+changed[off+2:]==raw
        edits.append(dict(field='TicksPerSecond',file_offset=off,bytes=2,value=val,exact_neighbors=True,inverse_restores=True))
    return dict(source=link_source(n.level.SOURCE),record_swap=dict(file_offset=refstart,record_bytes=72,records=2,exact_inverse=True,headers_DF_opaque_reference_words_tail_preserved=True),
        exact_field_inverse_edits=edits,source_writes=0,new_physical_fields=0)


def link_source(path):return dict(file=str(path),sha256=sha(path.read_bytes()))


def build():
    old=load('phase198-prior-package-state.json');assert len(old['components'])==175
    for ref in old['components'].values():assert link(ref['file'])==ref
    for ref in load('phase194-source-baseline.json')['source_files']+load('phase194-source-baseline.json')['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    accepted=load('phase198-reference-emulations.json');assert len(accepted['replays'])==13
    for c,row in zip(n.cases(),accepted['replays']):
        args,ch,saved,file=n.execute(c);_,out=n.verify(c,args,ch,saved['result']);assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
    controls=load('phase198-reference-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (count,size)==(row['checked_regions'],row['checked_bytes']) and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    primary=load('phase198-reference-primary.json');assert primary['all_executed_original_PCs_covered']
    rows=accepted['replays'];outcomes=[r['outcome'] for r in rows]
    counts=dict(native_scenes=13,direct_file_scenes=2,prepared_standard_finalpass_calls=11,successful_finalpass_calls=9,reference_callbacks=17,
        nested_file_load_failures_ignored=4,file_open_attempts=sum(len(r['opened']) for r in outcomes),successful_file_opens=sum(r['successful_opens'] for r in outcomes),
        closed_file_handles=sum(r['closed_file_handles'] for r in outcomes),native_instructions=sum(r['native_steps'] for r in outcomes),
        independent_Ghidra_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        original_PCs=primary['unique_executed_original_PCs'],whole_function_byte_extents=len(primary['functions']),
        prior_components_preserved=175,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,new_physical_fields=0)
    assert counts['file_open_attempts']==29 and counts['successful_file_opens']==counts['closed_file_handles']==26
    return dict(phase=198,status='original_automatic_Level_reference_file_traversal_and_ignored_child_failure_verified',profile=primary['profile'],counts=counts,
        precedence='Upgrades phase196 static reference-order inference and phase197 section-only loading to dynamic file-open/section-loop/reference/close evidence under the stated prepared final-pass conditions.',
        original_chain=dict(finalpass=0x51DDC0,Level_registration=0x51DE50,file_loader=0x56C960,file_open=0x5B3980,section_dispatch=0x56CBD0,
            Level_parser=0x56C4E0,Level_callback=0x51E6D0,reference_callback=0x51EA40,reference_loader_registration=0x51E0A0,
            Variables_parser=0x5716E0,Variables_World_callback=0x521790,file_close_delete=0x5B3A30),
        prepared_conditions=dict(World_flags_offset=0x7239,successful_initial_flags=0x36,required_mask=6,success_adds_mask=8,
            scope='Existing World and loaded definitions are prepared, actual native definition loaders/factory execute as inherited setup. Per-World Variables table is measured phase197 constructor-segment output. Initializer51AA90 executes; whole World constructor and preceding preload/count/allocation/definition passes do not.',
            virtual_environment='Explicit ASCII C:\\DK2 directory, memory-backed files, commandlineC:\\DK2\\DKII-DX.exe, bounded Windows heap and single-thread successful synchronization providers.'),
        natural_order=dict(sequence='Full original51DDC0->51DE50->56C960 opensLevel.56C4E0 appliesDF via51E6D0 before DE references; each DE invokes51EA40->51E0A0->56C960, which automatically opens/closes and reads sections to EOF.',
            references='Stored DE order controls application. No built-in unconditional common-vs-map priority established; names common.kwd/map.kld are prepared diagnostic filenames.',
            examples=[dict(DE=['common.kwd','map.kld'],DF_TPS=4,common_TPS=30,map_TPS=60,final_TPS=60,Type4_runtime=1,freeze_Data1=5,freeze_timer=300),
                dict(DE=['map.kld','common.kwd'],DF_TPS=4,common_TPS=30,map_TPS=60,final_TPS=30,Type4_runtime=3,freeze_Data1=5,freeze_timer=150)],
            scaled_order=[dict(DE=['rate.kwd','tick.kld'],DF_TPS=4,Type4_raw=101,final_TPS=30,Type4_runtime=25),dict(DE=['tick.kld','rate.kwd'],DF_TPS=4,Type4_raw=101,final_TPS=30,Type4_runtime=3)],
            duplicates='Repeated common.kwd reference is reopened and its callbacks applied again; no deduplication in the selected path.',
            multiple_sections='Direct real56C960 reads two Variables sections in one file sequentially; secondType19 overrides first.'),
        failures=dict(child_return_observations=[0x51E274,0x51EAE3],root_return_observations=[0x51DED1,0x51DDE9],
            swallowing_instruction=0x51EAE3,swallowing_operation='MOV EAX,1 after CALL51E0A0; ignores its0 result.',
            observed='Absent child or malformed known Variables header -> child loader0, reference callback1, later references continue; finalpass1 and World7239bit8 set.',
            partial_application='Valid Variables section setsTPS60/Type4=1; a following badE7 section makes child56C960 return0. Already-applied values remain, outer finalpass still1.',
            root_failure='Missing root file propagates0 through51DE50/51DDC0; bit8 not set. Missing required Worldbit2 rejects before opening files.',
            editor_rule='Validate and report each referenced file/section, retain ordered source provenance and partial-load diagnostics. A successful outer load does not prove every reference was read.',
            diagnostic_correction='An early bad-child probe used an unknown all-zero outer section; original generic dispatch accepted it. Accepted failure probes instead have knownE6 framing with wrongE7 header. Unknown-section behavior is not presented as a malformed-known-section failure.'),
        fixture_boundaries=dict(original_code_IAT_vtables='Unchanged; native backend rejects writes to all original read-only PE sections, all code and IAT.',
            original_heap='Original operator new/malloc/free and file/stream/string constructors/destructors execute; CRT small-block threshold0 and heap handle1234 explicitly prepared.',
            OS_only='Declared original external targets implement boundedHeapAlloc/HeapFree, ASCII path attributes/open/read/seek/size/close, error/TLS/interlocked and single-thread semaphore/critical-section services. No real game file/network access by emulated executable.',
            failure_counts='Normal native CloseHandle attempts on invalid temporary handles return0/error6 and are counted separately; provider-contract errors are0 in accepted scenes.',
            external_layout='Inherited long IsBadReadPtr and ASCII WideCharToMultiByte adapters are relocated behind five-byte external thunks to avoid overlap with newly used external import addresses; original IAT targets unchanged.',
            text_sound='Twenty prepared ASCII messages use the scoped ACP-success provider; hero wide string and separate synthetic sound/GFX slots retain phase196 bounds. No live renderer/audio/text subsystem.',
            raw_inputs='Level DF copied from explicitly prepared phase196 source projection; file/reference layouts are native-compatible synthetic views. This is not a complete actual installed-map load.'),
        native_executions=accepted,independent_Ghidra_controls=controls,primary=primary,physical_preservation=physical(),helpers=[link(name) for name in HELPERS],
        limitations=['No complete live World bootstrap, all preceding passes, archive contents/compression, path-search fallback matrix, localization, allocator exhaustion or scheduler execution.',
            'Only selected scalar Variables and exact DX profile; other union handlers, other EXEs and later gameplay effects remain separately scoped.',
            'UnknownGlobals29/186 meanings remain open; Variables writer and additional globals still needed. No overall completion-percentage increase.',
            'Global parameters active and incomplete. Crypt deferred with10 counter meanings open; creature resistances/flags follow global parameters.'],
        next=['UnknownGlobals29/186 actual consumers and other remaining global parameters.','Original Variables editor writer and remaining loader/version boundaries.','Creature resistances and flags after global parameters.'])


NOTE='level_reference_loading_semantics phase198:13realfiletraversals,11preparedfinalpasscalls/9success,17DEcallbacks/4ignoredchildfailures,29openattempts/26successandcloses;4Ghidra/400regions/3532000B,15177PC/245functionbyteextents. Full51DDC0->51DE50->56C960->DFthenorderedDE->51E0A0->Variables->close runs. No unconditionalcommon/mappriority;missing/badchilddoesnotfailroot,earliersectionwritesnotrolledback.175priorcomponents/codec/420sources/7EXEs preserved;package176,Globalsactive,Cryptdeferred;wholeWorldbootstrap/archive/otherglobals/writer remainopen.'


def main():
    package=load('editor-format-package.json');path=ROOT/'phase198-prior-package-state.json'
    if not path.exists():
        assert len(package['components'])==175
        dump(path.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(path.name)
    for key,h in old['preserved_top_level'].items():assert canonical(package[key])==h
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    s=build();dump(SCHEMA,s);package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package);dump('phase198-reference-storage.json',s['physical_preservation'])
    dump('phase198-search-status.json',dict(phase=198,goal_status='active',current_block='Global parameters',block_complete=False,counts=s['counts'],schema=link(SCHEMA),next=s['next']))
    print(json.dumps(s['counts']))


if __name__=='__main__':main()
