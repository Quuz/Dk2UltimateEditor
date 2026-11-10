"""Publish full native possession/secondary CreatureSpell recharge consumers."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
import emulate_possession_recharge as native
import native_possession_recharge as api
from native_spell_writer import patch_raw
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,items in load('phase96-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    exe=Path(load('possession-health-semantics.json')['profile']['executable']).read_bytes()
    assert struct.unpack('<I',pe_read(exe,0x66e3ec+0xe8,4))[0]==0x50dfa0
    return out

def results():
    rows=load('phase96-possession-recharge-emulations.json')['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==38
    for row in rows:
        path=ROOT/'ghidra'/row['file'];v=json.loads(path.read_text());c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(path.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['interval']).items())
        assert api.interval(e['base_ticks'],c['percent'],c['scalar'],possessed=c['state'] in ['possessed','both'],secondary=c['state'] in ['secondary','both'])==e['interval']
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out

def patch_container(parts,si,ri,changed):
    s=parts[si];rr=list(s.records);rr[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
    return b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))

def corpus():
    variable_files=[];spell_files=[];scalar_records=spell_records=scalar_patches=spell_patches=0;vh=hashlib.sha256();sh=hashlib.sha256()
    for row in load('phase88-sacrifice-corpus.json')['files']:
        data=Path(row['file']).read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data;local=0
        for si,s in enumerate(parts):
            if s.signature!=0xe6:continue
            relative=len(s.header)
            for ri,raw in enumerate(s.records):
                if len(raw)==16 and struct.unpack_from('<I',raw)[0]==210:
                    scalar_records+=1
                    for value in [0,50,100,255,256,0xffffffff,0xabcd0064]:
                        changed=api.patch_scalar210(raw,value);packed=patch_container(parts,si,ri,changed);expected=bytearray(data)
                        struct.pack_into('<I',expected,s.offset+relative+4,value)
                        assert packed==expected and fmt.parse(packed)[si].records[ri]==changed and changed[8:]==raw[8:]
                        vh.update(hashlib.sha256(packed).digest());scalar_patches+=1;local+=1
                relative+=len(raw)
        variable_files.append(dict(file=row['file'],sha256=row['sha256'],scalar210_patches=local))
    for row in load('spell-writer-semantics.json')['corpus']['files']:
        data=Path(row['file']).read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data;local=0
        for si,s in enumerate(parts):
            if fmt.FAMILIES.get(s.signature)!='CreatureSpells':continue
            relative=len(s.header)
            for ri,raw in enumerate(s.records):
                assert len(raw)==266;spell_records+=1
                for value in [0,4096,6144,0xffffffff]:
                    changed=patch_raw(raw,0xea,value);packed=patch_container(parts,si,ri,changed);expected=bytearray(data)
                    struct.pack_into('<I',expected,s.offset+relative+0xea,value)
                    assert packed==expected and fmt.parse(packed)[si].records[ri]==changed
                    sh.update(hashlib.sha256(packed).digest());spell_patches+=1;local+=1
                relative+=len(raw)
        spell_files.append(dict(file=row['file'],sha256=row['sha256'],recharge_patches=local))
    assert len(variable_files)==557 and scalar_records==475 and scalar_patches==3325
    assert len(spell_files)==82 and spell_records==3535 and spell_patches==14140
    return dict(variable_files=variable_files,spell_files=spell_files,variable_source_files=557,spell_source_files=82,
      scalar210_records=scalar_records,scalar210_exact_container_patches=scalar_patches,spell_records=spell_records,spell_exact_container_patches=spell_patches,
      scalar_patch_digest=vh.hexdigest(),spell_patch_digest=sh.hexdigest(),
      scope='Exact whole-container patches:scalar210DWORD4 and CreatureSpellDWORD_EA only. All source hashes/roundtrips checked,unknownbytes/headers/order/neighbours preserved. Earlier24Variablesstrictrefusals not repaired or newly diagnosed. No source writes.')

def current():
    doc=load('decoded/GlobalVariables.kwd.json');data=Path(doc['source']).read_bytes();assert digest(data)==doc['sha256'];rr=[]
    for row in doc['sections'][0]['records']:
        if row['words_u32'][0]==210 or row['words_u32'][:2]==[224,31]:
            raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+16]==raw;rr.append(dict(offset=row['offset'],raw_hex=raw.hex(),words_u32=row['words_u32']))
    assert len(rr)==2
    spell=load('decoded/CreatureSpells.kwd.json');sd=Path(spell['source']).read_bytes();assert digest(sd)==spell['sha256'];first=spell['sections'][0]['records'][0]
    assert sd[first['offset']:first['offset']+266]==bytes.fromhex(first['raw_hex'])
    return dict(variables=dict(file=doc['source'],sha256=doc['sha256'],records=rr),creature_spells=dict(file=spell['source'],sha256=spell['sha256'],first_record=first))

def main():
    p=primary();rows,out=results();c=corpus();source=current();prior=ROOT/'phase96-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==74;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=96,date='2026-10-28',profile=load('possession-health-semantics.json')['profile'],primary=p,current_sources=source,native_replays=rows,outcomes=out,corpus=c,
      api_helpers=[link(n) for n in ['native_possession_recharge.py','emulate_possession_recharge.py','native_spell_writer.py','native_spell_alternatives.py','native_possession_stats.py']],
      settings=dict(stat31=dict(record_type=224,stat_id=31,value_dword=8,loaded='low16 via51DC70→6F28A1;495080 reads signed16',current_value=25,
          baseline_name='MELEE_RECHARGE_TIME_SECONDS;actualconsumer receivesCreatureSpelldefinition andisnot locallygatedbymelee/type.'),
        scalar210=dict(record_type=210,value_dword=4,loaded='low8 via51CD90→BYTE6F29B0;getterreadsDWORDthenAND_FF',current_value=100,
          baseline_name='FIRST_PERSON_RECHARGE_MODIFY_PERCENTAGE',verified_effect='Additional purepercentage factor onlywhenactualpossession flag3E6/80 applies;secondary-only path bypasses it.',
          preserve='All32physicalvaluebits andDWORD8/C retained;256loadslike0,ABCD0064like100. RuntimewidthdoesnotchangetherawDWORDwidth.'),
        base_time=dict(family='CreatureSpells',raw_dword=0xea,raw_bytes=4,intermediate_dword=0x13a,definition_dword=0x2d,
          native_loader='4F7860:baseTicks=low32(rawDWORD_EA*global6F2590)>>12,logicalshift. It isnot64-bitunboundedmultiplication.',
          new_raw_paths='5nativeinit56A490→converter56A560→loader4F7860→interval/readiness/progress compositions;oneusesfirstcurrentrawrecordwithonlyEApatched.',
          overflow_example='rawFFFFFFFF*TPS20→base1048575;possessedQ12shift/productgivesintervalFFFFFFFE infixture. Forensicinput,norecommendationfornormalcontent.')),
      interval=dict(function='4F7AB0',arguments='ECX=CreatureSpelldefinition;stackarg=Creature;RET4',
        flags='Possessed3E6/80 takespriority oversecondary3EA/08. Bothflags actuallypreparedbyrealsetter/neighbourproducer,newsecondaryrayuses sameXYcell.',
        notation='U(x)=low32(x),S(x)=signed32(x),P=sign16(loaded224/31),C=low8(loaded210)',
        primitives=dict(qmul='U((S(a)*S(b))>>12);native signedIMUL+SHRD12',
          qdiv='U(truncTowardZero((U(a)<<12)/S(b))) with signed32quotientbound;native441990zeroesEDXbeforeSHLD/SHL/IDIV,notordinarysign-extendeddivision.'),
        exact=['Neitherflag:returnbaseTicksunmodified.',
          'ratio=QDIV(U((100+P)<<12),(100ifpossessedelse200)<<12).',
          'Ifpossessed:ratio=QMUL(ratio,QDIV(C<<12,100<<12)).',
          'ReturnU(S(QMUL(ratio,U(baseTicks<<12)))>>12).'],
        ordinary_approximation='Forordinarysmallpositiveinputs:base*(100+P)/100*C/100 possessed,base*(100+P)/200 secondary;Q12roundingatintermediatestagesmustbereproduced.',
        native_examples=['B20,P25,C100:normal20,possessed25,secondary12,both25.',
          'B20,P25,C50→12possessed;C0or256→0;C255→63;ABCD0064→25.',
          'B20,P0,C100→20possessed;P100→40;FFFF/sign−1→19.',
          'SecondaryP25remains12whenCchanged100→0;scalar98/113/1140/6/30000unchangedinnewpaths.']),
      readiness=dict(function='48C800',slot='Low8query;4943F0returnsdefinition+5E1+19*slotwhenindex<BYTE789,otherwisezeroIDfallback.',
        lookup='AbilityID0→false;otherwiseWorldvslotE8→real50DFA0 looksup70byteCreatureSpellarrayWorld1614/count161C,IDbyte43. IDs1and7executed.',
        state='CreatureDWORD41B+4*slot islastuse;actorDWORD3AF+DWORD3B3 wrapped32isdenominator. NamesofthesetwoDWORDsnotassignedbythisphase.',
        formula='Ifabilityabsent:return0. Iflastuse0:return1. Elsedeadline=U(lastuse+U(adjustedTicks<<12)//U(actor3AF+actor3B3));returnunsigneddeadline<unsignedcurrentTick.',
        boundary='Strictlessthan:equalityreturnsnotready.Normal20,last100,tick120→0,tick121would1;possessed25,last100,tick125→0,tick126→1actuallyexecuted.',
        division='Zero denominator wouldnativeDIV0 whenlastuse!=0;notexecuted/aneditorcorruptionclaim.',
        clock='517490returnsSessionDWORD4throughWorld+4;nativewrapperexecuted.'),
      progress=dict(function='48C8B0',formula='Ifabilityabsent0. elapsed=S(U(tick-lastuse));ifelapsed>=S(adjustedTicks),return100;elsereturnQMUL(QDIV(100<<12,U(adjustedTicks<<12)),U(elapsed)).',
        difference='Doesnotreadactor3AF/3B3.ForpreparednormalB20,denominator8192,last100,tick111:ready1whileprogress55.',
        equality='Atpossessed25,last100,tick125progress100whileready0;tick126ready1.',
        forensic='Preparedtick99,last100→rawprogressFFFFFFFC,signed−4,noclamp0inthisgetter. Actualrenderingofsuchreturnnotobserved.',
        not_ui_run='Nativegetterreturnchecked,notaliveUIoractualcombatcast.'),
      counts=dict(native_cases=len(out),native_replays=len(rows),raw_spell_compositions=sum('raw_recharge' in r['case'] for r in out),
        whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),variable_source_files=c['variable_source_files'],spell_source_files=c['spell_source_files'],
        scalar210_records=c['scalar210_records'],scalar210_exact_container_patches=c['scalar210_exact_container_patches'],spell_records=c['spell_records'],spell_exact_container_patches=c['spell_exact_container_patches'],
        new_actor_images=0,original_game_function_or_vtable_patches=0),
      scope=dict(source_actor=load('possession-health-semantics.json')['scope']['source_actor'],
        setup='Preparednativephase90actorandsecondsourceactor,World/Sessionandabilitylist/70byteSpellarray;notnewallocationornativeliveworldsnapshot.',
        raw='Everycaseusesactualraw224/31and210callbacks.5casesaddwholeCreatureSpellinitializer/converter/loader;gate1,nullresourceprovider0,suppressesvisual/checksumbranches.',
        lazy_guard='4943F0lazyguard6E4168=1andzero19bytefallback6E4178prepared;nativeCRTatexitfirstregistrationintentionallyoutsidefixture.',
        preserved='Completepreparedactor+guard,definition,world,map,owner,session,vars,rawrecords andallbufferscheckedafterexecution;nogame/vtable/table/codepatches.'),
      limitations=['ExactprovidedDXprofileonly;othergame/editorversionsnotexecuted.',
        'Nativeinterval/readiness/progresscomplete;fullShot26/control,combatselection45CB00andactualattack/eventlifecycle notcomposed.',
        'Namesofactor3AF/3B3notassigned;denominator8192andclock-before-lastarepreparedforensicinputs,notnormal-sessionororiginal-editorbugproof.',
        'CreatureSpellloadergate1/nullresourcesexcludesvisuals/checksum,actualfileparser/worldinsertionnotrerun.',
        'Signedminimum/negativeQDIVdenominators/quotientoverflow,DIV0andmissingnonzeroSpellIDlookupnotexecuted;modelraisesforfaultinputswithoutnativefaultcoverage.',
        'BaselineMELEElabeldoesnotprovelimittoMelee;actualhelper/readinessusespassedSpellandabilityslot,IDs1/7tested.',
        'Scalar98/113/114 andother224statglobalsemanticsremainopen;unchangedresultsonthispathnotglobalnonuse.',
        'Objects34C,Traps204,Doors236,Terrain198/224,CreaturePriority/Jumpandother15formatquestionskeepgoalopen.'])
    (ROOT/'possession-recharge-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))

if __name__=='__main__':main()
