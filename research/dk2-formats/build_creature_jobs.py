"""Publish the observed HateJobs/alternative0 alias, without replicating it."""
from pathlib import Path
import json,hashlib,struct,collections
import dk2_formats as fmt
from build_effect_semantics import pe_read
import emulate_creature_job_overlap as native
import emulate_creature_hate_writer as writer
import emulate_creature_jobs_real_maps as real
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    result=[]
    for group in ['functions','blocks']:
        rows=[]
        for r in load('phase86-primary-manifest.json')[group]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            row={**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)}
            if group=='functions':row['assembly_sha256']=digest((ROOT/'ghidra'/r['assembly']).read_bytes())
            else:row['hex']=b.hex()
            rows.append(row)
        result.append(rows)
    return tuple(result)
def evidence():
    rows=[];outcomes=[];images={}
    for kind,module,manifest,prefix in [('converter',native,'phase86-job-overlap-emulations.json','phase86-job-overlap-'),('writer',writer,'phase86-hate-writer-emulations.json','phase86-hate-writer-'),('real_map',real,'phase86-real-job-emulations.json','phase86-real-job-')]:
        savedrows=load(manifest)['cases'];by={c['name']:c for c in module.cases()}
        for row in savedrows:
            p=ROOT/'ghidra'/row['file'];v=json.loads(p.read_text());c=by.pop(row['case']);args,e=module.build(c);assert v['case']==c and v['input']==args and v['expected']==e and digest(p.read_bytes())==row['sha256']
            b=module.verify(c,v['result'],e);n='ghidra/'+prefix+c['name']+'.bin';assert (ROOT/n).read_bytes()==b;images[c['name']]=b
            rows.append(dict(**row,kind=kind));outcomes.append(dict(case=c,expected=e,capture=link(n)))
        assert not by
    assert len(rows)==22
    for p in ['game','editor']:
        assert images[p+'-extras-zero']==images[p+'-extras-distinct']==images[p+'-extras-highbits']
    # Changing only model HateJobs3/4 cannot alter the inspected output bands.
    assert images['hates-high-altzero']==images['hates-changed-last']
    by={r['case']['name']:r['expected'] for r in outcomes}
    assert by['game-alt-job5']['membership_results']==[0,1,0,0,0,0,0,0]
    assert by['game-alt-mood9']['membership_results']==[0,0,1,0,0,0,0,0]
    assert by['game-alt-job105']['membership_results']==by['game-alt-job5']['membership_results']
    return rows,outcomes
def corpus():
    fields=[(0xE74,4),(0xE78,4),(0x14E1,4),(0x14E5,4),(0xE7C,4),(0xE80,2),(0xE82,2)];files=[];patches=records=0;checksum=hashlib.sha256();stats=collections.Counter();extras=collections.Counter();examples=[]
    for row in load('creature-lookup-review.json')['corpus']['files']:
        p=Path(row['file']);data=p.read_bytes();assert digest(data)==row['sha256'];sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
        found=[(i,s) for i,s in enumerate(sections) if s.signature==170];assert found
        for _,s in found:
            for r in s.records:
                records+=1;old_extra=struct.unpack_from('<2I',r,0x14E1);alt=struct.unpack_from('<IHH',r,0xE7C);first=[r[0xE74],r[0xE78]];effective=[alt[0]&255,alt[1]&255]
                extras[old_extra]+=1;stats['nonzero_extra_pair']+=int(any(old_extra));stats['nonzero_alt0_job']+=int(alt[0]!=0);stats['nonzero_alt0_mood']+=int(alt[1]!=0)
                stats['nonzero_effective_hate3']+=int(effective[0]!=0);stats['nonzero_effective_hate4']+=int(effective[1]!=0)
                stats['effective_extra_job_not_in_first_two']+=int(any(v!=0 and v not in first for v in effective))
                if any(effective) and len(examples)<8:examples.append(dict(file=str(p),creature_id=r[0xF4B],extra_dwords=list(old_extra),alternative0=list(alt),effective_hate_bytes=first+effective))
        si,s=found[0];original=s.records[0]
        for offset,width in fields:
            for value in [0,1<<(width*8-1),(1<<(width*8))-1]:
                modified=bytearray(original);modified[offset:offset+width]=value.to_bytes(width,'little');rs=list(s.records);rs[0]=bytes(modified);clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rs)
                packed=b''.join(clone.pack() if j==si else x.pack() for j,x in enumerate(sections));want=bytearray(data);absolute=s.offset+len(s.header)+offset;want[absolute:absolute+width]=value.to_bytes(width,'little')
                assert packed==want and fmt.parse(packed)[si].records==rs;checksum.update(hashlib.sha256(packed).digest());patches+=1
        files.append(dict(file=str(p),sha256=row['sha256']))
    assert len(files)==82 and records==3734 and patches==1722
    return dict(files=files,source_files_hashchecked=82,records=records,new_exact_container_patches=patches,patch_fields=[dict(raw_offset=o,bytes=n) for o,n in fields],ordered_patch_digest=checksum.hexdigest(),observations=dict(stats),common_raw_extra_pairs=[dict(values=list(pair),records=n) for pair,n in extras.most_common(6)],examples=examples,
      scope='7raw spans ×3boundary values ×82firstCreature records;3734physical records roundtrip. Whole-container equality outside selected2/4bytes. Counts describe copies of records,not unique creatures or AI sessions. Nonzero extras are preserved;no automatic repairs.')
def indexed_leads():
    j=load('phase85-creature-index-diagnostics.json');events={e['event']['address']:e['event'] for e in j['indexed_events']};rows=[]
    spec={
      '0x494729':('Two DWORD values','def675+4*i,i<2;675..67C',False),
      '0x494709':('Two BYTE values','def673+i,i<2;673..674',False),
      '0x494870':('Resource pointer','def+27*resolvedAnimation;validslot0..47',True),
      '0x4948ff':('Resource presence/fallback','def+11+27*requestedAnimation;special37..41 usefixedresourceflags',True),
      '0x494a5c':('Resource presence predicate','def+11+27*animation;special37..41fixed;validslots0..47',True),
      '0x494693':('Eight XYZ rows','def6EC+12*i,unsignedi<8;6EC..74B;phase85nativeproof',False),
      '0x494521':('Packed6-byte job entries','def628+6*i;i<count78B;loadercount<=3;628..639',True),
      '0x4945a6':('Packed6-byte job entries','def63C+6*i;i<count78C;loadercount<=2;63C..647',True),
      '0x494621':('Packed6-byte job entries','def64A+6*i;i<count78D;loadercount<=3;64A..65B',True),
      '0x48c5e6':('Footstep WORD pair','def7A3+4*uint8(animation),and7A5;addressesabovePriority/Jump',True),
      '0x48c5ee':('Footstep WORD pair base','Sameprecedingdef+4*uint8(animation);finalread+7A5',True),
      '0x4967f0':('Footstep pair for animation state','def+4*uint8(stateAnimation),thenread7A3/7A5',True),
      '0x4c4972':('String/index WORD array','def6CD+2*i;SETNZmakesi0or1;6CD..6D0',False),
      '0x4943c3':('Packed3-byte attraction entries','def5DA+3*i;i<count788;loadercount<=2;5DA..5DF',True),
      '0x49449c':('Hated-job membership','def66E+i;i<count78E;loadercount<=4;66E..671;newnativealiasproof',True)}
    assert set(events)==set(spec)
    for a,e in events.items():
        meaning,formula,caller_bound=spec[a];rows.append(dict(address=a,instruction=e['instruction'],callee=e.get('callee'),classification=meaning,address_arithmetic=formula,requires_valid_loader_or_caller_bound=caller_bound,
          scope='Classification of this local memory expression;not proof of allcallers or globalnonuse. Entity+370 hints alone do not establish receiver type.'))
    return dict(source=link('phase85-creature-index-diagnostics.json'),events=116,unique_events=15,classifications=rows,
      limitation='All15localexpressions nowclassified,includingprior8XYZlead. Malformed counts/unbounded animation arguments notglobally validated. No gameplaypurpose assignedPriority6EB/Jump75C.')
def main():
    functions,blocks=primary();rows,outcomes=evidence()
    # Codec unchanged after1722 comparisons;reuse only with allsource hashes.
    existing=ROOT/'creature-job-overlap-semantics.json'
    if existing.exists():
        corpus_result=load(existing.name)['corpus'];assert corpus_result['new_exact_container_patches']==1722
        for r in corpus_result['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
    else:corpus_result=corpus()
    s=dict(phase=86,date='2026-10-28',family='Creatures',executables=[dict(file=f,sha256=sha) for f,sha in load('executable-hash-verification.json').items()],primary_program_bytes=functions,primary_data_ranges=blocks,
      native_emulations=rows,outcomes=outcomes,corpus=corpus_result,indexed_leads=indexed_leads(),
      counts=dict(native_invocations=22,paired_converter_scenarios=14,unchanged_real_map_scenarios=2,original_raw_converters=30,original_raw_initializers=30,original_game_constructors=9,original_game_definition_loaders=9,original_job_membership_calls=72,original_full_editor_writers=6,code_spans_attested=48,original_world_vtables=1),
      file_fields=dict(first_hate_jobs=[0xE74,0xE78],physical_bytes=4,extra_candidates=[0x14E1,0x14E5],extra_bytes=4,first_alternative=dict(job_offset=0xE7C,job_bytes=4,mood_offset=0xE80,mood_bytes=2,mana_offset=0xE82,mana_bytes=2)),
      game_converter=dict(initializer='0x568820',converter='0x568E30',bytes=6457,presence_start=0x2B4,hate_dword_start=0x1264,hate_slots=4,
        sequence=['rawE74/E78→mid1264/1268','raw14E1/14E5→mid126C/1270','rawE7C→mid126C;rawE80/E82→mid1270/1272;overwritesextra pair'],
        final_hate_dwords='[rawDWORD(E74),rawDWORD(E78),rawDWORD(E7C),rawWORD(E80)|(rawWORD(E82)<<16)]',
        loader='0x492320:compact presentmid2B4..2C0 slots intoBYTEarraydef66E,countdef78E;convertersetsall4presenceflags evenforzero'),
      editor_converter=dict(initializer='0x4281B0',converter='0x4287C0',bytes=6561,hate_setter='0x429940',alternative_setter='0x4299B0',presence_start=0x2C4,hate_dword_start=0x1274,alternative0_start=0x127C,
        sequence=['rawE74/E78→mid1274/1278','raw14E1/14E5→mid127C/1280','rawE7C..E83→mid127C..1283;overwritesextra pair'],
        result='All6561bytes match baseline native conversion withonlyrawalternative0 eight-byte patch;varying14E1/14E5 leavesall6561unchanged forfixtures'),
      editor_model=dict(bytes=6288,hate_dword_start=0x156C,hate_slots=4,alternative_start=0x157C,alternative_stride=8,alternative_slots=3,
        getter_hate='0x41E5A0',setter_hate='0x41E5B0',getter_alternative='0x41E5D0',setter_alternative='0x41E5E0',overlap=False,model_creation_scope='Model-creation transfer inspectedstatically inprior4A01B0;newwriter uses explicitmodels,notactualGUIcommits'),
      writer=dict(function='0x4A7F00',raw_bytes=5537,loop1='4iterations,41E5A0:model156C+4*i→rawE74+4*i',loop2='3iterations,41E5D0:model157C+8*i→rawE7C+8*i',
        consequence='ModelHateJobs3/4 initiallywrittenrawE7C/E80,thenoverwritten bymodelalternative0 eightbytes. raw14E1/14E5 remainpriorstackseed inall6fixtures;notrestoredfrommodel.',
        checked='Fulloriginalwriter runs;32-byte rawE74..E93 band,8bytesraw14E1..14E8,whole6288-byte source model andrecordcount1compared. Other5537fields notindependentlydecoded.',
        external_boundaries='Onlyfixeddate4AB320 emulator-memory adapter andexplicitfileprovider. No actualfiles written. Datehelper substitution countedseparately;originalwriter/getters/stack/numeric/string/resourcecode retained.'),
      membership=dict(function='0x494480',query_bytes=4,array_offset=0x66E,count_offset=0x78E,condition='queryDWORD!=0 && exists storedBYTE==fullqueryDWORD',
        narrowing='LoadertruncateseachhateDWORD tolow8. Queryisnottruncated:stored5 matchesquery5,not105hex.',
        actual_consumer=dict(function='0x4E8DA0',call=0x4E9018,job_source='DWORD[6A1E9F+41*jobIndex+25],jobIndexBYTE1..26inthispath',branch='Membershipnonzero→4E90F6 return0;thiscandidate rejected',scope='Originalwholeconsumer instructions inspected/PEattested;completeconsumer/AIloopnotnewlynativelyexecuted'),
        new_proof='rawalternativeJob low8=5 orMood low8=9 producesactualnative membershiptrue for5/9 despitezero rawE74/E78 andarbitraryignoredextraDWORDs'),
      real_map_proof=dict(records='Unmodified Prince ID2 fromBattle for the Sun Qz2Globals.kld andFrontEnd3DLevelGlobals.kld',raw_extra_pair=[0,0],alternative0=[16,0,0],
        actual_hate_bytes=[[0,0,16,0],[5,0,16,0]],membership16=True,membership0x110=False,scope='Fullnativeinit/converter/ctor/loader/membership;emptyWorldreferences,nullmainGFX,fixedsound. NoactualAIassignment.'),
      serialization_rules=['Preserve rawE74/E78 and14E1/14E5 asindependentphysicalDWORDs;do notregenerate14E1/14E5 fromintermediate/model orworkingstack.',
        'Do notcopyPro overlapintonewlosslessserializer. Two mainHateJobs areindependentphysicalfields;third/fourthlogicalHateJobs cannotbefixed forinspectedgame justbyediting14E1/14E5.',
        'Alternative0 fields arephysicalindependentdata infilebutpoisonruntimeHateJobs3/4ininspectedDX. Exposethecompatibilitydiagnostic;do notsilentlyrewritealternatefields oroldmaps.',
        'Do notdeclare14E1/14E5unread:bothconvertersreadthem,thenoverwrite. Thiscorrectsearlierphase2inspection.',
        'No binarypatch orautomaticnormalization implemented;gamefiles/executables unchanged.'],
      limitations=['ExactsuppliedDX/Pro. NoRC3/oldgame claim fortheoverlap;sourcehashes verifiedbutnotnewlynativelytestedversions.',
        'Originaljobmembership executed;completeAIassignment andoriginaleditorGUIwerenotrun. Meaningsofotherjobparameters andallindexedcallersremainoutsideclaim.',
        '14converter runs usepairednativebaseline withexplicit8-byteexpectedpatch,notanindependentfullconverterimplementation. 3gamecontrolcases comparefull2199bytes withpriorImpimage.',
        '6writer runs useoneclockhelperreplacement eachinEmulatorHelpermemory. Allinvestigatedserialization/getters/modelbytes remainoriginal;do notcountaszero-code-substitutioncases.',
        'Full15-familygoalremainsincomplete;Priority/Jump gameplay andotherunknownsremainopen.'])
    (ROOT/'creature-job-overlap-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    import build_creature_subrecords
    build_creature_subrecords.main()
    print('Phase86:',len(rows),'native cases;',s['corpus']['new_exact_container_patches'],'exactpatches;overlapproved;Priority/Jump remainunknown.')
if __name__=='__main__':main()
