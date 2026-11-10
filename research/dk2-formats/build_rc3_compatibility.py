"""RC3/Pro catalogue converters andtheverifiedCreatureextraFlags divergence."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_keeper_pipeline import digest
from build_effect_semantics import pe_read,asm_range
from build_placed_creatures import whole
from compare_rc3_converters import PRO,RC3
from native_rc3_compatibility import PROFILES,extra_flags_view
from inventory import DATA
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def primary():
    rows=[]
    for r in load('phase50-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip();v=json.loads(m.read_text());b=bytes(v['data']);assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);rows.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows
def converter_pairs():
    pro=PRO.read_bytes();rc=RC3.read_bytes();manifest=load('phase50-primary-manifest.json');md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;out=[]
    helpers={}
    for r in load('phase50-helper-pair-candidates.json'):
        a=int(r['pro'],16);b=int(r['rc3'],16);pa=next(v for v in manifest if v['program']=='DK2 Editor pro.exe' and v['address']==a);pb=next(v for v in manifest if v['program']=='DK2-Editor-rc3.exe' and v['address']==b);assert pa['length']==pb['length'] and pe_read(pro,a,pa['length'])==pe_read(rc,b,pb['length']);helpers[(a,b)]=dict(pro=a,rc3=b,bytes=pa['length'],identical_sha256=digest(pe_read(pro,a,pa['length'])))
    for r in load('phase50-converter-match-candidates.json'):
        if len(r['rc3_candidates'])!=1:continue
        a=r['pro_address'];b=r['rc3_candidates'][0];n=r['bytes'];x=pe_read(pro,a,n);y=pe_read(rc,b,n);mask=bytes.fromhex(r['mask_hex']);calls=[]
        assert all(not m or u==v for u,v,m in zip(x,y,mask)) and len(mask)==n
        for v in r['masked_relocations']:
            off=v['offset'];pc=int(v['instruction'],16);ins=next(md.disasm(pe_read(pro,pc,5),pc));assert ins.mnemonic=='call' and ins.size==5 and v['kind']=='branch' and v['bytes']==4 and off==pc-a+1;ta=pc+5+struct.unpack_from('<i',x,off)[0];tb=b+pc-a+5+struct.unpack_from('<i',y,off)[0];assert (ta,tb) in helpers;calls.append(dict(pro_call=pc,rc3_call=b+pc-a,**helpers[(ta,tb)]))
        assert sum(not m for m in mask)==4*len(calls);rr=next(v for v in manifest if v['program']=='DK2-Editor-rc3.exe' and v['address']==b);assert rr['length']==n
        out.append(dict(family=r['family'],pro=a,rc3=b,bytes=n,unmasked_bytes=sum(bool(m) for m in mask),only_differences='Rel32 CALL displacements;allcalledhelper bodiesverifiedbyteidentical',helper_calls=calls,masked_pro_sha256=digest(bytes(v&m for v,m in zip(x,mask))),masked_rc3_sha256=digest(bytes(v&m for v,m in zip(y,mask)))))
    assert len(out)==10;return out
def inputs():
    rows=[]
    for p in [DATA/'Creatures.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        b=p.read_bytes();sections=fmt.parse(b);rs=[r for s in sections if s.signature==170 for r in s.records];king=next(r for r in rs if r[0xF4B]==21);rows.append(dict(file=str(p),sha256=digest(b),records=len(rs),record_sizes=sorted({len(r) for r in rs}),king={v['name']:extra_flags_view(king,k) for k,v in PROFILES.items()}))
    return rows
def main():
    s=dict(family='Exact RC3/Pro catalogue compatibility profiles',profiles=PROFILES,primary_program_bytes=primary(),converter_pairs=converter_pairs(),native_emulations=load('phase50-rc3-creature-emulations.json')['cases'],inputs=inputs(),
        comparison_scope='Ten fullcatalogue converter instructionbodies identicalexceptrel32CALLaddresses. Theonlytwoexternalhelperpairs(strncpyandTerrain16WORDsetter)arewhole-byteidentical. This coversconversionalgorithms,notallparsers,GUI,modelcallbacks,resources,savepathsorwholeEXE.',
        comparison_emulations='90 RC3 nativecomparisoncases:10families×3rawpatterns×3midseeds. ExecuteactualRC3 converterandProbodyrelocatedinEmulatorHelpermemory,withexternalCALLsretargetedtoverifiedidenticalRC3helpers. Compareall9000bytesincludingretainedseedbeyondrealmid. Thisiscontrolledalgorithmcomparison,notexecutionoftheentireProprograminsideRC3.',
        creatures=dict(rc3_initializer='0x4199E0',rc3_converter='0x419FF0',rc3_parser='0x41B5E0',rc3_callback='0x4A58F0',model_fragment=['0x4A68DF','0x4A68F4'],rc3_writer='0x4AD720',rc3_extra_decoder='0x4292B0',rc3_extra_encoder='0x429270',
            raw='RC3record5449,FlagsDWORD1545;mid6457,Flags1935,presence260. Prorecord5537,Flags1599;mid6561,Flags1999,presence264. Rawotherfieldoffsetsdonotallshift;useexactprofile.',
            model='RC3fragment checks midpresence260,callsdecoder4292B0withmid1935. DecoderwritesBOOLs1880/1884/1888/1828frombits1/2/4/8;encoder429270returnsbitsF. Otherrawflagbitsarenotrepresentedbythismodel. Same4BOOLoffsetsasProdoesnotimplysamerawflaglocation.',
            writer='FullRC3writer4AD720emits5449pernonnullmodel. HeaderN=context480,array47C,S=56+5449*N,D=5449*N. Realencoder429270→EAX,storeatframe15D9;rawbaseframe94,so15D9-94=1545. Pro4A7F00insteadwritesencoderresultatraw1599anddoesnotassigngame1545(phase43).',
            writer_validation='25 fullwriter executions:4stackseeds×6flagvaluesplus2consecutiverecords. Assertinitial12-byteheader,countofrecordwritesandlastemittedrecordDWORD1545. Doesnotassertall5449fieldbytesorfulldateheader. Allnativewriter/getters/resourcecopy/ftol/CString/stackprobeinstructionsrun;onlydateandfileIOfixtures.',
            chain='9chains:fullRC3init+converter→real4A68DF..F4modelfragment+fulldecoder→fullRC3writer. Construction/registrationandunrelatedcallbackfieldsbypassed.7syntheticflagpairsand2realKinginputs.Slot1545flags&Fpreserved;slot1599doesnotbackfillgameflags.',
            king='RealstandaloneKingId21hasgame1545=1,andchainemits1.AutoSaveGlobalsKinggame1545=0;Prolong1599mayhave1,butRC3chainstilloutputs0.NothistoricalproofthatRC3orProcausedspecificpastdatachanges,andnolivemodified-gameplayrun.',
            parser='RC3raw556CA4+1549=mid5581EDadjacency. Forstride5537,readwrites88bytespast5449rawbufferintomidprefix;actualconverterrewritesthetestedprefix.4fullparsercases(5449/5537×1/2records)compareall6457callbackbytesagainstisolatednativeinit+converter;longIOfixturealsocomparesexactoverwritten88bytesimmediatelyafterread. Thisdoesnotcertifyarbitrarylargerstridesorsafeunknownversions.',
            ignored_extension='6pairedinit/convertercases compareall6457byteswhilechanging80opaqueextensionbytes,last4bytesorPro1599slot.RC3ignorestheselongextensionbytesintheseconverterpaths.Savingthrough5449RC3writerdoesnotpreservethephysical88-bytetail;ourraw-preservingeditor must.',
            comparison_warning='RC3supportisnotauniversalrepairinstruction.Neverautomaticallycopy1599to1545,truncate5537to5449,clearunknownflagsorresaveoriginalsthroughRC3.'),
        further_checks=dict(catalogues='18independentPythonoraclechecks:9RC3Keeper526and9CreatureSpell346wholeintermediatebufferswith00/FF/patternrawand00/A5/FFseed.',model_codec='14fullRC3decoder→encodercasescomparewhole1890modelandcapturedFlagsF;allunrelatedmodelbytesretained.'),
        api=dict(file='native_rc3_compatibility.py',operations=['extra_flags_view','patch_extra_flags'],profile_key='ExactinspectededitorSHA256only',rule='PatchselectedDWORDbitsinoriginalrecordonly;preserveallotherbytes,lengthandotherflagslot.RefuseabsentPro1599inshortrecord;noimplicitextension.'),
        evidence=[asm_range('phase50-rc3-419ff0-disassemble_function.txt',0x41A7C5,0x41A7E6),asm_range('phase50-rc3-4a58f0-disassemble_function.txt',0x4A68DF,0x4A68F0),asm_range('phase50-rc3-4ad720-disassemble_function.txt',0x4ADE85,0x4ADEAA),asm_range('phase50-rc3-4ad720-disassemble_function.txt',0x4AE38B,0x4AE39D)],
        limitations=['166 nativecases:25fullwriters,14fullmodelcodecs,4fullparsers,6pairedCreatureconverters,18catalogueoraclecases,9conversion/modelfragment/writerchains,90Pro/RC3convertercomparisons.',
            'Tenconverterequivalenceclaimislimitedtocapturedfullbodiesand2wholeidenticalhelpers;otherparsersorGUI/saverscanstilldiffer. Creaturesisdemonstrablydifferent.',
            'OnlyextraFlagsiscarriedacross9chainmodelfragments;otherfieldsuseprebuiltwriterfixturemodel.Wehavenotexecutedcomplete4A58F0construction/resource/registrationcallback.',
            'NoGUI,registry,gameprocessororiginalfilewrites.TheRC3resultdescribesnativeencodingandloadercompatibility;gameplayimmunitymeaningreliesonseparatephase36Process9evidence.',
            'Negativeunknownfieldsearchesdonotproveunused:Terrain198/224,Traps204,Objects34C,KeeperC8andCreature/APunknownsremainopen. RC3PlinthCEUIandotherexclusivepathsremainfuturework.',
            'Earlierbad-boundarydiscovery41B5D0onlycontainedRET4;correctparser41B5E0isusedandverified. Codecfixtureoverlapwascaughtbeforeemulationandfixed;nofailednativecaseisincluded.'])
    assert len(s['native_emulations'])==166
    if (ROOT/'rc3-job-overlap-semantics.json').exists():s['phase87_job_overlap']=load('rc3-job-overlap-semantics.json')
    (ROOT/'rc3-compatibility-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('RC3:166 nativecases;10converterpairs;37primaryfunctions')
if __name__=='__main__':main()
