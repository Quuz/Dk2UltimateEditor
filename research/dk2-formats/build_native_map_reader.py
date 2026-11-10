"""Publish actual native Map reader/file layer, EOF defect and source controls."""
from pathlib import Path
import json
import emulate_native_map_reader as native
import verify_native_map_reader as oracle
import crosscheck_native_map_reader as controls
import prepare_native_map_reader_primary as primary
from validate_map_reader_boundaries import validate as validate_edits

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase139-native-map-reader-primary.json')
    manifest=load('phase139-native-map-reader-emulations.json');runs=manifest['replays'];assert len(runs)==17
    posts={};pairs=[]
    for c,row in zip(native.cases(),runs):
        args,checks,saved,path=native.execute(c);post,out=oracle.verify(c,args,checks,saved['result']);posts[c['name']]=post
        assert out==row['outcome'] and sha(path.read_bytes())==row['sha256'] and str(path.relative_to(ROOT))==row['file']
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
        if c['name'] in ['world-fourthFF','world-declared0','world-flags2']:pairs.append(oracle.pair(c['name'],posts['world-square'],post))
        if c['name']=='chunk-fourthFF':pairs.append(oracle.pair(c['name'],posts['chunk-crossing'],post))
        if c['name']=='source-AutoSaveMap-opaque-edit':pairs.append(oracle.pair(c['name'],posts['source-AutoSaveMap'],post))
    assert pairs==manifest['paired_controls']
    cross=load('phase139-native-map-reader-crosschecks.json')['replays'];assert len(cross)==len(controls.CONTROLS)==6
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size and row['native_reader_return_checked']
    edits=validate_edits();assert edits==load('phase139-map-reader-api-validation.json')
    preservation=load('map-room-chain-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    source=next(c for c in native.cases() if c['name']=='source-AutoSaveMap');source_path=Path(source['source_file'])
    assert any(r['file'].lower()==str(source_path).lower() and r['sha256']==sha(source_path.read_bytes()) for r in preservation['source_files'])
    prior=ROOT/'phase139-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==117
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==117
    diagnostics=[]
    for path in sorted((ROOT/'ghidra').glob('phase139-native-map-reader-*.json')):
        saved=load(str(path.relative_to(ROOT)))
        if 'result' not in saved or saved['result']['success']:continue
        r=saved['result'];timeout=r['emulation_error'] is None and not r['protected_write_or_execution_failures']
        diagnostics.append(dict(**link(str(path.relative_to(ROOT))),accepted_coverage=False,error=r['emulation_error'],wall_timeout=timeout,
            reason='Original5second wall budget ended during largecapture; same input passed with30second budget, original snapshot retained.' if timeout else
                'Incomplete earlyWorld fixture: absent neutral owner or non-solid perimeter caused invalid outside-grid reads. Corrected current fixture has nativeTerrain1 solidborder and allplayer tags; no gamecrash claim.'))
    assert len(diagnostics)==3
    counts=dict(native_reader_paths=17,World_reader_paths=5,capture_or_skip_paths=12,
        original_World_Map_callbacks=sum(r['outcome']['world']['native_Map_callbacks'] for r in runs if r['outcome']['world']),
        captured_parser_records=sum(r['outcome']['callback_captures'] for r in runs),native_ReadFile_calls=sum(r['outcome']['read_calls'] for r in runs),
        native_file_wrappers_constructed=17,short_read_paths=3,rejected_bad_tag_paths=2,skip_seek_paths=2,
        original_AutoSave_cells=625,exact_source_copy_opaque_edit_paths=1,
        independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        paired_complete_memory_controls=len(pairs),paired_compared_bytes=sum(r['whole_bytes'] for r in pairs),
        executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_blocks=len(attested['blocks']),
        reused_phase138_function_byte_ranges=attested['reused_phase138_function_byte_ranges'],new_exact_Map_opaque_edits=110,
        preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0)
    return dict(phase=139,date='2026-10-30',families=['Map','Rooms','Terrain','Objects'],profile=attested['profile'],primary=attested,
        coverage_extension=dict(new_scope='Original56D2D0 section reader->original5B3EB0->nativeMyFile5B6EA0->nativeDisc5FFFB0->declaredReadFile import, with actual51EC60World callbacks in five scenes.',
            supersedes='Phase138 started from host MapMID. This phase constructs17nativeMyFilewrappers and reads rawKLDsection bytes through unchanged native file-layer bodies and IAT/vtables.',
            remaining='Entry fileposition12 follows outer Map descriptor; outerdispatch/open/close/fullWorld allocation/header callback and normalAI caller remain unexecuted.'),
        file_layer=dict(reader=0x56D2D0,read_wrapper=0x5B3EB0,MyFile_constructor=0x5B6E40,pushStream=0x5B6F20,native_MyFile_read=0x5B6EA0,native_Disc_read=0x5FFFB0,
            native_MyFile_vtable=0x671F80,native_Disc_vtable=0x672FE8,native_Disc_bytes=32,stream_prestate='Prepared post-open Disk handle/offset12, zero mapping/semaphore fields; native open/CRT allocation/close not executed.',
            imports=[dict(name='ReadFile',IAT=0x66C1D4,target=0x28CF2A),dict(name='SetFilePointer',IAT=0x66C1D8,target=0x28CF42),dict(name='InterlockedIncrement',IAT=0x66C0A0,target=0x28C5D4)],
            original_IAT_bytes=1056,IAT_patches=0,game_function_patches=0,vtable_substitutions=0,
            external_scope='Synchronous bounded memory-backed single handle; EOF shortreads, selected nonnegative seeks including beyondEOF, exactrefcount increment. Not a complete WindowsAPI implementation or liveOS experiment.'),
        native_reader=dict(header_tag=101,data_tag=102,header_size_field_checked=False,dimensions='Native32bit width*height cell product; arbitrary dimension overflow not exhaustively tested.',
            active_callback_data_size='Reads4*width*height bytes in chunks, ignores declaredDataSize in active callback route; declared0 scene still loads25cells.',
            chunk_cells=16384,chunk_bytes=65536,buffer=0x746EC0,
            tested_chunk_crossing=dict(width=129,height=128,cells=16512,first_chunk_cells=16384,second_chunk_cells=128,all_coordinates_and_MapMID_records_match=True),
            MapMID='Three presenceDWORDs1, BYTE+C terrain/BYTE+D player/BYTE+E flags/BYTE+F0; rawfourthbyte ignored. Runtime cell+C mask is a different structure.',
            skip_route='No cellcallback: getoffset then seek bydeclaredDataSize; fixture permits1000byte skip beyondEOF and native reader returns1 after matching seekposition.',
            wrong_tag_routes='Wrong101 or102 tag returns0; these are ordinary native returns, not CPU exceptions.'),
        short_read_defect=dict(wrapper_result='5B3EB0 stores0when actualReadBytes!=requested; otherwise1.',reader_check='56D2D0 tests result<0, so both0and1 proceed.',
            tested_cases=[r for r in runs if r['case'] in ['truncated-last-fourth','truncated-last-flags','empty-payload']],
            buffer_prestate=0xA5,confirmed_effect='Truncated24byte payload still produces6callbacks andreturn1. Missing flags are read asA5 from prepared buffer; empty payload produces6A5-derived records.',
            limits='Capture-only route for malformed files; no claim malformedbytes created a validWorld or caused a livegame crash. Other buffer prestates remain possible.',
            editor_rule='Retain strict length/dimension/header checks. Native accidental acceptance is not a writer requirement.'),
        source_control=dict(file=str(source_path),sha256=sha(source_path.read_bytes()),file_bytes=2536,width=25,height=25,cells=625,
            original_bytes_read_natively=True,scope='Generic reader capture; not fullWorld construction for the real625cellmap.',
            copy_edit='One opaque byte3 toggled by80h in memory. All625callback records and otherprepared outputs remain identical; originalsource unchanged.'),
        World_control=dict(prepared_dimensions=[5,5],native_callbacks=125,perimeter='OriginalTerrain1 solidrock, loaded natively; no fabricatedexteriorcellguard.',
            owners='All rawPlayerId2 inputs; native nonownableTerrain1/3 is reassigned to explicitneutralTag8, Temple22 remains2.',
            native_initialized='Filewrapper/pushStream, Objectallocator, Roomslots, Terrain/Rooms/Objects loaders andnavsetup.',
            not_newly_executed=['nativeWorldheadercallback/allocation','outerfiledispatcher','normalAIstate65caller','physicalCreature drop or sacrificeafterthisload','finalCreature deletion']),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,diagnostic_exclusions=diagnostics,
        preservation=preservation,counts=counts,
        helpers=[link(n) for n in ['native_unicorn_mapreader.py','native_file_api_fixture.py','emulate_native_map_reader.py','verify_native_map_reader.py','crosscheck_native_map_reader.py','prepare_native_map_reader_primary.py','validate_map_reader_boundaries.py']],
        limitations=['Exact supplied DX profile; no new oldEXE/RC3/Flame/liveWindows profiles.',
            'Native section reader is complete, but World arrays/dimensions, freeRoomlist and postopenDiskstream are prepared. HeadercallbackNULL; no native outerdispatch/open/close/fullWorldbootstrap.',
            'External Windows API adapters have explicit singlehandle/bounds contracts. Game/IAT/vtable bytes unchanged; no real filesystem handles, audio/render/UI.',
            'Short-read corrupt-input scenes use capturecallback, not51EC60World construction; A5is explicit buffer prestate, not a guaranteed gamevalue.',
            'Ghidra whole-scene equality includes opaqueoutputs; independent semantic oracles cover complete stream/callback records, selectedWorldinvariants andpairedmemory, not meanings of allmesh/definition bytes.',
            'Shared original functionbyte evidence is reused fromphase138 only after exactPE/hash checks. DiskC remainsfull and stopped one duplicateassemblydump; allrequiredranges and6CPUcontrols were nevertheless verified.',
            'RawKLD byte3 remains opaque and preserved; shortread acceptance must not relax stricteditorvalidation. Substantial15family unknowns remain.'])

if __name__=='__main__':
    value=build();(ROOT/'native-map-reader-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
