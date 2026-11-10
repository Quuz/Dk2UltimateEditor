"""Publish original Door reader/file/World-catalogue semantics and strict storage controls."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_native_door_reader as native
import crosscheck_native_door_reader as controls
import prepare_native_door_reader_primary as primary
import validate_door_catalogue_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase160-prior-effective-doors.json'))
    for f in out['fields']:
        if f['offset']==0x244:f['verified_native_catalogue']=dict(definition_offset=0xAD,raw_type='unsignedBYTE',
            insertion='Original51FF30 inserts before firstID>=newID; equal IDs allowed, new duplicate precedes prior equal records.',
            lookup='50D650 starts at min((requestedID&255)-1,count-1), scansback while rowID>requestedlow8; firstequal encountered wins, smallerID/missing returnsNULL.',
            zero='ID0 can be inserted but lookup0 immediately returnsNULL in tested originalgetter.',
            scope='PreparedWorldarray/nativefile reader/callbacks/getter; physical file order must be preserved on save.')
        elif f['offset']==0x236:f['verified_native_catalogue']=dict(status='unresolved',definition_offset=0x99,
            preservation='All16bits survive complete reader/converter/callback/definition, including appendedID9 and sortedinsertion copies.',
            observation='Whole source/opaque pair differs only source/raw/MID/definition copies and definition/World checksum. No gameplaypurpose or globalnonuse assigned.')
    return out

def build():
    attest=primary.build();assert attest==load('phase160-native-door-reader-primary.json')
    manifest=load('phase160-native-door-reader-emulations.json');assert len(manifest['replays'])==21
    posts={};trace=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,path=native.execute(c);post,out=native.verify(c,args,ch,d['result']);posts[c['name']]=post;trace.append(d['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
    pair=native.compare_opaque_pair(posts['source'],posts['source-opaque']);assert manifest['paired_controls']==[pair]
    assert posts['source'][native.ARRAY]==posts['source-two-pass'][native.ARRAY]==posts['reverse-source'][native.ARRAY]
    cross=load('phase160-native-door-reader-crosschecks.json')['replays'];assert len(cross)==11
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert row['sha256']==sha((ROOT/row['file']).read_bytes()) and d['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase160-door-catalogue-storage-validation.json')
    assert sha((ROOT/'dk2_formats.py').read_bytes())==load('phase159-terrain-light-storage-validation.json')['codec']['sha256']
    prior=load('phase160-prior-components.json');assert len(prior)==138
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('terrain-light-submission-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    def visits(a):return sum(r['visited_instructions'].get(hex(a),0) for r in trace)
    counts=dict(native_scenarios=21,successful_scenarios=sum(r['success'] for r in trace),native_reader_entries=visits(0x56AC80),
        native_reader_normal_returns=sum(c['returned'] for r in trace for c in r['before_calls'] if c['address']=='0x56ac80'),expected_divide_exceptions=1,native_header_callbacks=visits(0x51FF10),native_World_callbacks=visits(0x51FF30),
        native_definition_loads=visits(0x4A7100),native_RAW_converters=visits(0x56AA40),native_MID_initializers=visits(0x56A920),
        native_prepared_array_slot_constructors=visits(0x4A7040),Windows_read_requests=sum(len(r['outcome']['read_requests']) for r in manifest['replays']),
        independent_full_Ghidra_controls=11,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        whole_memory_pairs=1,pair_compared_regions=pair['compared_regions'],pair_compared_bytes=pair['compared_bytes'],
        original_code_extents=len(attest['functions']),static_only_function_extents=2,unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        exact_changed_container_rebuilds=serial['exact_changed_container_rebuilds'],source_Door_records=serial['records'],source_Door_files=len(serial['files']),
        newly_named_opaque_fields=0,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=160,date='2026-11-01',family='Doors',profile=attest['profile'],primary=attest,
        native_pipeline=dict(reader=0x56AC80,header_callback=0x51FF10,record_callback=0x51FF30,lookup=0x50D650,
            calls='Original56AC80->5B3EB0->MyFile/Disc->declaredWindowsimports; original56A920/56AA40->51FF30->4A7100; root50D650.',
            entry='Position12 after outer12-byte prefix; originalMyFileconstructor5B6E40 andattach5B6F20 run. Outerdispatch/open/close not executed.',
            two_pass='Original51DF90 registers header51FF10,51E0A0 registers data51FF30, statically attested. Source-two-pass executes actualreader header-only then nativeMyFileseek12 then actualreader data-only; sameWorldarray as combined-callback diagnostic.',
            prepared='WorldA400bytes withoriginal66E3ECvtable, nullWorld+4visualprovider; hostallocated12-slot181byte array with252native slotconstructors across21scenes. WholeWorld allocation/bootstrap not executed.'),
        framing=dict(raw_record_bytes=616,intermediate_bytes=732,definition_bytes=181,
            header='Readerconsumesfixed36bytes at file12, requires headerTag141 at12, takesN DWORD20. Then8bytes at48, requires dataTag142; D DWORD52.',
            active_data='UnsignedDIV D/N runs before N==0 test; stride=floor(D/N). ExactlyN records requested atstride bytes; D%N remainder ignored.',
            short_stride='Ifstride!=616, clears616rawbytes before eachread; tested600byte records. Converter stillreads616byte buffer.',
            source='Actual4984byteDoors.kwd: N8,D4928,rawpayload56. Appendeddiagnostic5600bytes: N9,D5544.',
            format_policy='Owneditor keeps canonical616stride, verifies declaredlengths/counts/EOF and preserves otherbytes. Native laxacceptance is not a compatible serialization requirement.'),
        catalogue=dict(World_offsets=dict(array=0x15D4,capacity=0x15D8,count=0x15DC,checksum=0x15E0,update_mode=0xA3C7),
            header='51FF10 addsN toWorldcapacity; does not allocatearray.',
            insertion='Ifcount<capacity, find insertion by backwardscan until oldID<newID. Shift181-byte definitions includingunknowns; place newrow before equals; loadselectedslot; count++ and addnewdefinitionchecksum toWorldchecksum.',
            no_space='Ifcount>=capacity, callbackreturns1 withoutloadingrow orchangingcount/checksum. Infixturefourrecords/twocapacity onlyfirsttwoinputsareinserted; readerstillreturns1.',
            update='NonzeroWorldA3C7 usesoriginallookup by MID_ID andloads existingdefinition. No insertion, count/capacity/Worldchecksum unchanged. ExistingIDs1/3 tested; missingIDupdate not executed.',
            duplicate='Allowedby insertion. Withcount4/ID1duplicates lookupstarts0 andseesnewduplicate; ID4duplicates start3 andseesolderduplicate. No first/lastglobalduplicate rule.',
            physical_order='Normal source, reversedsource andoriginaltwo-pass produceidentical finalarray. Thisdoesnot authorize rewriting sourcefile recordorder.'),
        lookup=dict(World_vslot=0x138,definition_ID_byte=0xAD,
            algorithm='id=requestedDWORD&255; i=min(id-1,count-1); scanback: equal->rowpointer, smaller->NULL; reachnegative->NULL.',
            zero='DiagnosticID0row inserted, lookup0 returnsNULL.',highword='Requested101hex resolvesrawID1; inputhighbitsignored inthisgetter.',missing='PreparedIDs1/3 queried2 returnNULL.'),
        checksum=dict(definition_offset=0x87,World_offset=0x15E0,
            rule='Atgate6FCDCF==0 definitionchecksum=sum732unsignedMIDbytes. Newinsert addsnewchecksum toWorldDWORD modulo2^32. Update-existing doesnot updateWorldsum.',
            nonzero_gate='Gate1 leaves priorchecksum unchanged; constructor leaveschecksum87 seededA5. NativeWorldsumthenaccumulates thosestale values. This is a fixture state, not a canonical filechecksum.',
            not_file_header='Worldruntimechecksum mustnot be written into S/N/D containerfields.'),
        reader_failures=dict(short_read=dict(case='truncated-tail',reader_return=1,short_reads=1,requested_last=616,actual_last=556,
            result='NativeReadFile/MyFile reportsshortread0,readeronlytests<0. Finalrecordtailretains previousrawbytes; intendedID2 becomesID1,Unknown1101 becomes1100. Catalogue[1,1],lookup2NULL.',
            scope='Actualreader atposition12 withdeclaredexternalfile; outerdispatcher/wholeWindowsgame notexecuted. No claimthatunalteredsourcefilesarecorrupt.'),
            zero_count=dict(case='zero-count',active_callback=True,EIP=0x56ADAE,divisor_ECX=0,expected_native_exception=True,
                result='DIV D/N withN0 raisesUnicornCPUexception before the laterN test; normalreturn notclaimed.',
                scope='Thisdirectreaderpath; wholeouterloadermayhaveotherchecks. Null-data-callbackcase skipsN0/D0 andreturns1.'),
            header_or_data_tags='Wrong141/142tag returns0; thesearecheckedunlikesourcecounts/shortreadcompletion.'),
        opaque_review=dict(raw_WORD=0x236,MID_WORD=0x2AA,definition_WORD=0x99,status='unresolved',
            all16bits_preserved=True,whole_memory_pair=pair,
            caveat='Sortedinsertion copieswhole181-byte rows,includingopaque99. These storage/copyreads are not a new gameplayconsumer; no globalnonuse proof.',global_nonuse_proved=False),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,effective_door_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['emulate_native_door_reader.py','crosscheck_native_door_reader.py','prepare_native_door_reader_primary.py','validate_door_catalogue_storage.py']],
        historical_triage=link('phase160-door-triage-review.json'),
        limitations=['OnlyexactDXEXE; oldprofile pointerhints reviewedbut notnewlyexecuted. Normal/DX.text equality from159notwholeprofileequivalence.',
            'PreparedWorld andhostallocatedarray; originalslotconstructors run. No completeWorldallocation/bootstrap,outer12bytedispatch,open/close or scheduler.',
            'Memory-backedWindowsimports andsoundlookup0; nullvisualprovider. No nativeWindowsgame,realassets/GPU/pixels.',
            'Twenty successfulscenarios andoneexpectedDIVexception; zero-countfault isnot a successfulfullreturn/Ghidracontrol.',
            'D/N<616 testedonly600; >616overflowpathsnotexecuted. MissingIDupdate/globalfaultpropagation nottested.',
            'Runtime insertion/lookup/copy/checksum do notassign Door236 gameplaymeaning. Physicalrecordorder/unknowns preserved.',
            'Previous138schemas andphysicalcodec unchanged; onlycurrentDoorsannotations extended. Significantunknowns across15families remain.'])

def main():
    s=build();(ROOT/'native-door-reader-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase160-prior-components.json');key='native_door_reader_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('native-door-reader-semantics.json');p['effective_catalogue_layouts']['Doors']=s['effective_door_layout']
    note='native_door_reader_semantics phase160 executes whole56AC80/MyFile/Disc/Windowsboundary->51FF10/51FF30->Worldcatalogue->50D650.21scenarios/22readerentries:20successfulscenarios,oneexpectedDIV0at56ADAE. Originalsource/reverse/twopass/appendedID9,sort/equalIDs/capacitysilentskip/updateexisting verified. TruncatedtailnativeacceptanceyieldsID2->duplicate1; stricteditorframingrequired.91Worldcallbacks/91definitionloads/11fullGhidra/257regions/569972bytes;24extents/911PC/5blocks.164append/reordercontrols82files/656Doors. Unknown236preservedbutunresolved. Preparedarray/World/nullvisual/declaredAPIproviders;outerdispatch/bootstrap/GPUunexecuted. Prior138schemas/420sources/7EXEpreserved;effectiveDoorsannotationsonly.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
