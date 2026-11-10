"""Publish independently executed RC3 overlap and a physical lossless patch API."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
import native_creature_jobs as api
import emulate_rc3_hate_jobs as native
import emulate_creature_job_overlap as paired
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    out=[]
    for r in load('phase87-primary-manifest.json'):
        p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b),assembly_sha256=digest((ROOT/'ghidra'/r['assembly']).read_bytes())))
    assert len(out)==10;return out
def native_results():
    rows=load('phase87-rc3-job-emulations.json')['cases'];by={c['name']:c for c in native.cases()};out=[];images={}
    for row in rows:
        c=by.pop(row['case']);p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());args,e=native.build(c)
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['input']==args and saved['expected']==e
        b=native.verify(c,saved['result'],e);name='ghidra/phase87-rc3-job-'+c['name']+'.bin';assert (ROOT/name).read_bytes()==b;images[c['name']]=b
        out.append(dict(case=c,expected=e,capture=link(name)))
        if c['kind']=='converter':
            _,raw=paired.records(dict(c,program='game'));view=api.view(raw,api.RC3);assert view['final_intermediate_hate_dwords']==e['hate_mid_dwords']
            assert b[:192]==(ROOT/'ghidra'/('phase86-job-overlap-game-'+c['name'][10:]+'.bin')).read_bytes()[:192]
        else:
            old=(ROOT/'ghidra'/('phase86-hate-writer-'+c['name'][7:]+'.bin')).read_bytes();assert b[:32]==old[:32]
    assert len(rows)==13 and not by
    assert images['converter-extras-zero']==images['converter-extras-distinct']==images['converter-extras-highbits']
    return rows,out
def exact_getter_pairs(functions):
    old=load('phase86-primary-manifest.json')['functions'];pro=Path(next(r['exe'] for r in old if r['program']=='DK2 Editor pro.exe')).read_bytes();rc=Path(functions[0]['exe']).read_bytes();rows=[]
    for a,b in [(0x41E5A0,0x428BD0),(0x41E5B0,0x428BE0),(0x41E5D0,0x428C00),(0x41E5E0,0x428C10)]:
        x=next(r for r in old if r['program']=='DK2 Editor pro.exe' and r['address']==a);y=next(r for r in functions if r['address']==b);assert x['length']==y['length'];data=pe_read(pro,a,x['length']);assert data==pe_read(rc,b,y['length'])
        rows.append(dict(pro=a,rc3=b,whole_function_bytes=x['length'],identical_sha256=digest(data)))
    return rows
def api_checks():
    files=[];patches=0;records=0;aggregate=hashlib.sha256();views=0
    for r in load('creature-job-overlap-semantics.json')['corpus']['files']:
        p=Path(r['file']);data=p.read_bytes();assert digest(data)==r['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        sections=[(i,s) for i,s in enumerate(parts) if s.signature==170];records+=sum(len(s.records) for _,s in sections);si,s=sections[0];raw=s.records[0]
        for sha,profile in api.PROFILES.items():
            v=api.view(raw,sha);expected=[int.from_bytes(raw[o:o+4],'little') for o in [0xE74,0xE78,0xE7C,0xE80]];assert v['final_intermediate_hate_dwords']==expected and v['record_bytes']==len(raw)
            if sha==api.DX:assert v['runtime_hate_bytes']==[x&255 for x in expected]
            else:assert v['native_writer_record_bytes']==profile['writer_bytes'] and not v['native_writer_preserves_independent_hate3_4']
            views+=1
        for field,(offset,width) in api.FIELDS.items():
            for value in [0,1<<(width*8-1),(1<<(width*8))-1]:
                changed=api.patch_field(raw,field,value);rs=list(s.records);rs[0]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rs)
                rebuilt=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));want=bytearray(data);at=s.offset+len(s.header)+offset;want[at:at+width]=value.to_bytes(width,'little')
                assert rebuilt==want and fmt.parse(rebuilt)[si].records==rs and len(changed)==len(raw);patches+=1;aggregate.update(hashlib.sha256(rebuilt).digest())
        files.append(dict(file=str(p),sha256=r['sha256']))
    assert patches==1722 and len(files)==82 and records==3734 and views==246
    # Validate physical-width/profile refusals;none emits/truncatesa record.
    sample=paired.records(dict(next(c for c in paired.cases() if c['program']=='game')))[0];refusals=0
    for operation in [lambda:api.patch_field(sample,'not_a_field',0),lambda:api.patch_field(sample,'alternative_mood',65536),lambda:api.patch_field(sample,'hate1',-1),lambda:api.patch_field(sample,'hate1',True),lambda:api.patch_field(sample[:-1],'hate1',0),lambda:api.view(sample,'unknown_sha256')]:
        try:operation()
        except ValueError:refusals+=1
        else:raise AssertionError('Expected explicit rejection')
    return dict(files=files,source_files_hashchecked=82,source_records_roundtripped=records,new_api_exact_container_patch_checks=patches,ordered_patch_digest=aggregate.hexdigest(),profile_views=views,explicit_refusals=refusals,
      scope='Newnative_creature_jobs.py API validatedagainstindependentwhole-container reference for7fields×3values×82sources. This is newAPI verification,notnewnativecases orrecountingprior1722patches asnewfielddiscoveries. No originalfilewrites.')
def main():
    functions=primary();rows,outcomes=native_results();previous=load('rc3-compatibility-semantics.json');previous.pop('phase87_job_overlap',None)
    s=dict(phase=87,date='2026-10-28',family='Creatures',profile=dict(name='RC3',program='DK2-Editor-rc3.exe',research_copy=functions[0]['exe'],original_file=str(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor rc3.exe'),sha256=api.RC3),
      primary_program_bytes=functions,native_emulations=rows,outcomes=outcomes,exact_model_function_pairs=exact_getter_pairs(functions),api_checks=api_checks(),
      counts=dict(new_native_invocations=13,paired_converter_scenarios=7,original_raw_initializers=14,original_raw_converters=14,original_full_rc3_writers=6,complete_mid_bytes_compared=6457,whole_source_model_bytes_compared=6288,whole_function_spans_attested=10),
      converter=dict(initializer=0x4199E0,converter=0x419FF0,hate_setter=0x41B180,alternative_setter=0x41B1F0,hate_mid_start=0x1264,hate_presence=0x2B4,alternative0_mid=0x126C,
        sequence=['rawE74/E78→mid1264/1268','raw14E1/14E5→mid126C/1270','rawE7C..E83→mid126C..1273;overwritesextra pair'],final_hate_dwords='[DWORDrawE74,DWORDrawE78,DWORDrawE7C,WORDrawE80|(WORDrawE82<<16)]',
        native_verification='7pairednative RC3init/conversions comparefull6457bytes againstseparatelyconvertedbaseline withonly8explicitrawalternative0 bytes patched. FullMIDstableunder3differentextraDWORDpairs;source/targetguards checked.'),
      writer=dict(function=0x4AD720,raw_bytes=5449,model_bytes=6288,model_hate=0x156C,model_alternative=0x157C,hate_getter=0x428BD0,alternative_getter=0x428C00,
        first_loop='4hateDWORDs→rawE74+4*i',second_loop='3alternative8byte records→rawE7C+8*i',result='IndependentmodelHateJobs3/4 overwrittenbyalternative0;raw14E1/14E5 retainworkingstackseedin6fixtures.',
        exact_checks=['rawE74..E93 32bytes','raw14E1..14E8 8bytes','whole6288byte source model','count1','first12headerbytes 170/4/(56+5449)','flagsDWORD1545=0 forpreparedmodels'],clock_adapter=0x4B0B30,
        capture='256-byte registercapture includes selectedrawbands;not acomplete5449-byte semanticoracle'),
      cross_profile=dict(reference=link('creature-job-overlap-semantics.json'),RC3='IndependentoriginalRC3 converter/writer executed;no relocatedProbody innewtests',
        model='4completePro/RC3modelgetters/setters byte-identical;modelarrays separate inboth',same_verified_raw_band='For6matchingwriter models,all32rawE74..E93bytesmatchpriorProoutput. Extra8stackbytes notrequiredto matchbecauseframeaddressesdiffer.',
        scope='Samejob-overlap behaviordoesnotmakewholeprofilesidentical. RC3writes5449/flags1545;Prowrites5537/flags1599;retainpreviousprofile distinction.',game='NewRC3testsareeditoronly. ActualDXmembership andrealPrince-map proofsremainphase86;notanewRC3game/AI claim.'),
      api=dict(**link('native_creature_jobs.py'),operations=['view(raw,exact_executable_sha256)','patch_field(raw,physical_field,unsigned_value)'],fields={k:dict(raw_offset=o,bytes=n) for k,(o,n) in api.FIELDS.items()},
        profile_keys=api.PROFILES,rule='Keepcompleteoriginalrawrecord. Patchonlyselected2/4bytes;keeprecordlength,extension,unknownbits,otherjobs. Viewreportsshadowingbutdoesnotrepair. Refuseunknownprofile/field,widthoverflow,boolandunknownrecordvariant.'),
      prior_rc3_schema_sha256_without_new_supplement=digest(json.dumps(previous,ensure_ascii=False,sort_keys=True).encode()),
      limitations=['ExactprovidedRC3/Pro/DXhashprofiles;oldgamefolder13notincludedinnewsemanticclaim.',
        '7newRC3converter and6newfullwriter invocations;priorphase50 166casesandphase86 22casesreferenced,notrerun orcountedasnew.',
        'Onlywriterclockhelper4B0B30 replacedin6EmulatorHelperruns;explicitfileprovider,noactualfileoutput. Originalinvestigatedcode/getters retained.',
        'Onlystated6457MIDcomparisons/6288model/selectedrawband/header/flagchecks;GUIcommit,completecallbackmodelconstruction,wholeAIandallrawproperties notnewlyverified.',
        'SourcecompiledfunctionsfromGhidra matchoriginalPE;noexecutables/gamefiles/parallel editor changed.',
        'Priority/Jump andothermajorunknowns remainunresolved;full15familygoalnotachieved.'])
    (ROOT/'rc3-job-overlap-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    previous['phase87_job_overlap']=s;(ROOT/'rc3-compatibility-semantics.json').write_text(json.dumps(previous,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase87:',len(rows),'RC3nativecases;1722newAPIexactchecks;prior166/22nativecasesnotrerun.')
if __name__=='__main__':main()
