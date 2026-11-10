"""Exact old/current native checks; unresolved semantics are kept explicit."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read,asm_range
from build_keeper_pipeline import digest
from native_version_crosschecks import PROFILES,OLD,DX
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def primary():
    out=[]
    for r in load('phase52-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip()
        v=json.loads(m.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and v['length']==r['length']==len(b) and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return out
def labels():
    out=[]
    for r in load('phase52-label-manifest.json'):
        m=ROOT/'ghidra'/r['memory'];v=json.loads(m.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and v['length']==r['length']==len(b) and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return out
def equivalence():
    ps=load('phase52-primary-manifest.json');exes={r['program']:Path(r['exe']).read_bytes() for r in ps};out=[];md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    for old,new in [(0x5607D0,0x56A490),(0x5608A0,0x56A560),(0x627CD0,0x6361A0)]:
        ro=next(r for r in ps if r['program']=='DKII-local13.exe' and r['address']==old);rn=next(r for r in ps if r['program']=='DKII-DX.exe' and r['address']==new);assert ro['length']==rn['length'];n=ro['length'];x=pe_read(exes['DKII-local13.exe'],old,n);y=pe_read(exes['DKII-DX.exe'],new,n);mask=bytearray([255])*n;calls=[]
        for ins in md.disasm(x,old):
            if ins.mnemonic=='call':
                assert ins.size==5;off=ins.address-old+1;old_target=ins.address+5+struct.unpack_from('<i',x,off)[0];new_target=new+(ins.address-old)+5+struct.unpack_from('<i',y,off)[0];assert (old_target,new_target)==(0x627CD0,0x6361A0);mask[off:off+4]=bytes(4);calls.append(dict(old_call=ins.address,current_call=new+ins.address-old,old_target=old_target,current_target=new_target))
        assert all(not mm or a==b for a,b,mm in zip(x,y,mask));out.append(dict(old=old,current=new,bytes=n,only_masked_call_operands=calls,masked_sha256=digest(bytes(a&m for a,m in zip(x,mask))),whole_byte_identical=x==y))
    assert [r['bytes'] for r in out]==[198,360,254] and [r['whole_byte_identical'] for r in out]==[True,False,True];return out
def rtti():
    out=load('phase52-ap-rtti-vtables.json')
    for r in out:
        data=Path(r['file']).read_bytes();td=pe_read(data,r['type_descriptor'],27);col=pe_read(data,r['complete_object_locator'],20);vt=pe_read(data,r['vtable']-4,88)
        assert td[8:]==b'.?AVCActionPoint@@\0' and struct.unpack_from('<I',col,12)[0]==r['type_descriptor'] and struct.unpack_from('<I',vt)[0]==r['complete_object_locator'] and list(struct.unpack_from('<21I',vt,4))==r['methods'];r['exe_sha256']=digest(data)
    assert {r['exe_sha256'] for r in out}=={DX,OLD};return out
def state_labels():
    rows=[];md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    expected=['CreaturesPassedThrough','WaitTimer','Flags','NextWaypointId','TriggerRoot','Id','TriggerRootRepeatCount','TopLeft','BottomRight']
    ps=load('phase52-primary-manifest.json')
    for program,address in [('DKII-local13.exe',0x4A7700),('DKII-DX.exe',0x4B0B90)]:
        r=next(v for v in ps if v['program']==program and v['address']==address);b=Path(r['exe']).read_bytes();found=[]
        for i in md.disasm(pe_read(b,address,r['length']),address):
            if i.mnemonic!='push' or i.operands[0].type!=2:continue
            a=i.operands[0].imm
            try:raw=pe_read(b,a,80).split(b'\0',1)[0]
            except (ValueError,AssertionError):continue
            if any(raw.lstrip().startswith(s.encode()) for s in expected):found.append(dict(instruction=i.address,address=a,ascii=raw.decode('ascii'),sha256=digest(raw)))
        assert len(found)==9 and all(f['ascii'].lstrip().startswith(s) for f,s in zip(found,expected));rows.append(dict(program=program,function=address,strings=found))
    return rows
def main():
    s=dict(family='Exact old/current CreatureSpell and ActionPoint compatibility checks',profiles=PROFILES,primary_program_bytes=primary(),native_rtti_ranges=labels(),action_point_rtti=rtti(),state_native_labels=state_labels(),whole_function_equivalence=equivalence(),native_emulations=load('phase52-version-emulations.json')['cases'],
        version_scope='OLD SHA identifies supplied DKII.exe from folder DKII13; no PE version resource or independent proof of retail1.3 provenance. Do not select layouts by folder name alone.',
        creature_spells=dict(raw_bytes=266,intermediate_bytes=346,definition_bytes=70,
            converters='Old5607D0 initializer198 andDX56A490 whole-byteidentical. Old5608A0 converter360 differsfromDX56A560 onlyoneCALLrel32to verifiedwholeidentical254-byte strncpy627CD0/6361A0. Covers fullbodies/helpers,notallfilesystemparsersorresourceproviders.',
            validation='Six full initializers and12 full converters compareall346bytes withseed00/A5/FFandtail00/FF. Thirtysixfullloaders compareall70definitionbytes:2EXEs×2gatevalues×3alternativepairs×3TPSvalues. Providerisnull;resourceloadbranchesexcluded.',
            checksum='Old4EC440 unconditionallyzeroesdefinitionDWORD1B andsums all346 unsignedmidbytes. Current4F7860doesonlywhen global6FCDCF==0;nonzerogatepreservespriorDWORD1B. ThreeTPSvalues0/20/FFFFFFFFdo notalterthechecksumrule. VaryingDXgateaddressinsideoldemulatorhasnoeffectinthetestedpath;thereisnogatebranchinthecapturedoldbody.',
            alternative_references='Mid138/139(rawE8/E9)are notassigned tosemantic70-bytefields by eitherinspectedloader. Differences contribute tochecksumwhenrecomputed;noverifiedalternative-shotreplacementmechanismisclaimed. RawEEpredicate isaseparatefield.',
            physical_checksum_warning='DefinitionDWORD1B isruntime checksumoverconverted346 bytes;notahashstoredinraw266record. No permission toreplaceitwithsumofrawbytes.'),
        action_points=dict(runtime_bytes=64,flags_byte_offset=50,raw_flags_word_offset=18,
            old_factory='Fullold4AEA80/4AE070/4A6F60runin4cases:2success seeds00/A5,gate0,allocationfailure. Allocatorfixturecapturesrequested64bytes. Whole64nodecompared;successsetsRTTIvtable65E73C,ID37andbounds38..3F,clearslocal2E..36,incrementscreatorDWORD32andassignsoutpointer. Baseconstructor/initandallocatorarefixtures;noactualallocator/listregistrationclaim.',
            identity='Native RTTI name.?AVCActionPoint@@ linksoldvtable65E73C andDX66D7BC toactualstatewriter/readermethods. SixRTTIinitializedrangesmatchPE. SameAPoffsetsdo notfollowfromCreatureinstance offsetdifferencesinphase47.',
            writer='Old4A6FB0 /DX4B0460 writeIDBYTE37,topLeft4bytes38,bottomRight4bytes3C,invoke base-statewriter,then9contiguousbytes2E..36.12fullwriterscompare18localemittedbytes/fourI/Ocalls andwhole64-bytemodelunchanged. Base writer andI/Oarefixtures;thisisnotcomplete savegameencoding.',
            reader='Old4A70A0 /DX4B0550 firstcallbase reader;whenitsreturnis0,exitbeforelocal9readandpost-load. Onsuccessread9bytesat2E..36andcallpost-load withposition1A/1E andzeroZ.12fullreaderscompareall64bytes;localread/post-loadcounts0or1verified. Base reader,I/Oandpost-loadarefixtures;noactualspatialregistrationclaim.',
            local_state_fields=[dict(offset=0x2E,bytes=2,label='CreaturesPassedThrough'),dict(offset=0x30,bytes=2,label='WaitTimer'),dict(offset=0x32,bytes=1,label='Flags'),dict(offset=0x33,bytes=1,label='NextWaypointId'),dict(offset=0x34,bytes=2,label='TriggerRoot'),dict(offset=0x36,bytes=1,label='TriggerRootRepeatCount')],
            labels='Actualold4A7700andDX4B0B90diagnosticbodiesandnative stringpointersgive local-state labels. WaitTimerhereisruntimeWORD,notnewrawdata. NoexternalannotationusedtojustifyHeroGate08gameplay.',
            bit08='StatewriterscopythewholeflagsBYTEwithoutmask;state readerscopyitback.08andallotherbitsarepreservedinthetestedlocalstatepath. Bothnative08settersandinitializersusethe same32byteoffset. Thisdoesnotestablish anyactivegameplayconsumerfor08.',
            raw_distinction='C5 map payload56 differsfromruntime64andfromlocal9 state. Do notpaste9 runtimebytesintoC5raworinterpret savedwaitticksasmapWaitDelay withoutthe separatelyprovenTPSconversion.'),
        variables=dict(types=[61,62,63,64],old_initializer=0x50E860,current_initializer=0x51AA90,old_rows=0x697914,current_rows=0x6A611C,old_callback=0x511940,current_callback=0x51DC20,
            evidence='Twofullinitializerscomparefour20-byte table rows(type,callback,0,0,0). Bothcallbacksreadmid10/14/18andcallastub:old4EB320,current401200 eachsingleRET. Sixcompletecallbackscompareinput32bytesunchanged;nohelperfixturesneeded.',
            conclusion='No-op for these exactregisteredcallbacks inbothEXEs. NotproofthatallVariablesareignored,notreasonforremoving rawrecords,andnotuniversalclaimaboutallversions.'),
        api=dict(file='native_version_crosschecks.py',operations=['profile','spell_projection','ap_state_segments','ap_loaded_local_state'],policy='ExactSHA profilesonly;reject guessedversionnames. AP state helperoperates64/9runtimebytesanddoesnoteditgamefiles.'),
        negative_leads=dict(ap08='phase52-ap-packed-read-leads.json has448 mappedoverlappingreads;phase52-old-ap-byte-read-leads.json15linearoldbyte32candidates. ReviewedAPmethods/readers/dumpsdonotyieldverifiedHeroGate08behavior. Objectidentityisrequired;displacement-onlymatchesarenotdecodedfields.',rc3='Noverifiedon-diskPlinthCEorImpTaskPriorityschemaestablishedbyexclusive-dialog/immediate searches. Resourceexistenceisnotacode/file binding.'),
        evidence=[asm_range('phase52-old-4ec440-disassemble_function.txt',0x4EC440,0x4EC477),asm_range('phase52-DKII-DX.exe-4f7860-disassemble_function.txt',0x4F7860,0x4F78A0),asm_range('phase52-DKII-local13.exe-4a70a0-disassemble_function.txt',0x4A70A0,0x4A7126),asm_range('phase52-DKII-local13.exe-511940-disassemble_function.txt',0x511940,0x51195D)],
        limitations=['90nativecases:36loaders,12converters,6initializers,12APwriters,12APreaders,6no-opcallbacks,2variabletableinitializers,4oldAPfactories. Wholemodelcomparisons applyonlywiththeexplicitfixturesandboundaries.',
            'Fullspellresourceprovider branchesnotrun. APbase-stateI/O/postloadnotrun. AP08 effect,CreatureSpellsalternativereplacementandotherunknowncataloguepurposesremainopen.',
            'Initial discovery4EC430wasonlyRET;actualspellloader4EC440 isused. OneinitialAPwriterI/Ofixturehadwrongstackoffsetsandtimedout;excludedfrommanifest. Correctedv2casesareseparatelysavedandvalidated.',
            'Read-onlyresearchoriginals,noGUIorlivegameplay.Registryandparallel editoruntouched. Nochangeofwholefilehashbaselinesdueto independentAutoSaveactivity.'])
    assert len(s['native_emulations'])==90
    (ROOT/'version-crosscheck-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Versions:90cases;',len(s['primary_program_bytes']),'functions;',len(s['native_rtti_ranges']),'RTTI ranges')
if __name__=='__main__':main()
