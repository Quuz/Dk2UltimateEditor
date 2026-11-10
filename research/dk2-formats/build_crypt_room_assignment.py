"""Publish original roomtable choice, raw HateJobs boundary and Crypt entry paths."""
from pathlib import Path
from copy import deepcopy
import json,struct
import emulate_crypt_room_assignment as native
import crosscheck_crypt_room_assignment as cross
import prepare_crypt_room_assignment_primary as primary
import build_crypt_movement as prior_build
import native_creature_jobs as jobs
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_room_assignment_semantics'
SCHEMA='crypt-room-assignment-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def storage():
    out=prior_build.storage();fmt=native.probe.old.old.prior.fmt
    file=native.probe.old.old.SOURCE;data=file.read_bytes();parts=fmt.parse(data)
    si,s=next((i,s) for i,s in enumerate(parts) if s.signature==170)
    ri,raw=next((i,r) for i,r in enumerate(s.records) if r[0xF4B]==5)
    rows=[]
    for value in [1,17,255,0xFFFFFFFF]:
        edited=jobs.patch_field(raw,'hate1',value)
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride)
        records=list(s.records);records[ri]=edited;clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+s.stride*ri+0xE74
        assert packed==data[:at]+value.to_bytes(4,'little')+data[at+4:]
        assert clone.header==s.header and clone.suffix==s.suffix
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(x) for x in fmt.parse(packed)]))==packed
        records[ri]=jobs.patch_field(edited,'hate1',int.from_bytes(raw[0xE74:0xE78],'little'));clone.replace_records(records)
        assert b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))==data
        rows.append(dict(raw_offset=0xE74,width=4,value=value,exact_container_edit=True,inverse_restores_source=True))
    assert sha(file.read_bytes())==sha(data)
    out['Creature_hate1_exact_edits']=rows;out['Creature_hate1_native_loader_controls']=[1,17,255,0xFFFFFFFF]
    return out

def build():
    manifest=load('phase183-crypt-room-assignment-emulations.json');assert len(manifest['replays'])==len(native.cases())==17
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        a,ch,s,file=native.execute(c);_,out=native.verify(c,a,ch,s['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        traces.append(s['result'])
    controls=load('phase183-crypt-room-assignment-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        a,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==a and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase183-crypt-room-assignment-primary.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase183-prior-package-state.json')
    for ref in old['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    blob=native.EXE.read_bytes();table=native.p.pe_read(blob,0x6A1E9F,27*41)
    g=bytes(load('ghidra/phase183-room-table-memory.txt')['data']);assert g==table
    row=table[26*41:27*41];assert row[0]==26 and struct.unpack_from('<4I',row,25)==(274,0,0,0)
    source=native.probe.old.old.SOURCE;fmt=native.probe.old.old.prior.fmt
    warlock=next(r for s in fmt.parse(source.read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==5)
    coordinates=load('creature-definition-semantics.json')['coordinate_array']
    assert coordinates['additional']['raw_offset']==0x127B and coordinates['additional']['raw_stride']==12
    assert struct.unpack_from('<3i',warlock,0x1287)==(0,1490,0)
    # Metadata origins are the measured fixed table, not an editable RoomKWD
    # offset or a speculative mapping of category-mask800h to raw Job IDs.
    roomrows=[dict(room_type=i,start_state=struct.unpack_from('<I',table,41*i+25)[0],
        alternate_state=struct.unpack_from('<I',table,41*i+29)[0],parameter21_unassigned=struct.unpack_from('<I',table,41*i+33)[0],
        HateJob_ID=struct.unpack_from('<I',table,41*i+37)[0]) for i in range(1,27)]
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=17,independent_Ghidra_controls=6,checkpoint_snapshots=102,
        room_assignment_calls=visit(0x4E8DA0),HateJob_checks=visit(0x494480),HateJob_zero_early_returns=visit(0x4944BD),
        native_hand_drops=visit(0x4BC710),drop_state76_callbacks=visit(0x46EAB0),post_recovery257_callbacks=visit(0x481200),
        whole_Creature_ticks=visit(0x488930),AI_dispatches=visit(0x476FC0),Crypt_registration_callbacks=visit(0x483790),
        native_action_activations=visit(0x496700),HP_requests=sum(len(r['hp_requests']) for r in traces),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=len(proof['functions']),inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        prior_components_preserved=160,source_files_preserved=420,EXE_paths_preserved=7,raw_hate_exact_edits=4,new_raw_fields=0,source_writes=0,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    return dict(phase=183,status='verified_bounded_generic_Crypt_assignment_and_separate_drop_recovery_boundaries',counts=counts,
        priority=dict(current='Crypt groups',block_complete=False,next=['Causal raw128B recovery-step control suggested by source/loader/getter path; exact X/Z uses remain unassigned.',
            'Bridge realdrop76->255/52->257 animationcompletion and follow ordinary AIjobselection, rather than forcing274.',
            'Natural action80 interruptions and realreaders of ten unassigned owner counters.']),
        profile=dict(file=str(native.EXE),sha256=sha(blob)),
        room_table=dict(address=0x6A1E9F,row_bytes=41,index='Roomtype1..26',source=link('ghidra/phase183-room-table-memory.txt'),
            fields=[dict(offset=25,width=4,meaning='Generic room placement/start state consumed by4E8DA0 and4E6C90'),
                    dict(offset=29,width=4,meaning='Alternate state getter4E6D10 used in foreign-room release path'),
                    dict(offset=33,width=4,meaning='Getter4E6CD0 parameter; exact meaning unassigned here'),
                    dict(offset=37,width=4,meaning='JobID passed to HateJobs check and stored in CreatureDWORD2BC after assignment')],
            Crypt=dict(room_type=26,source_row_hex=row.hex(),start_state=274,alternate_state=0,parameter21_unassigned=0,HateJob_ID=0),
            rows=roomrows,storage='FixedprovidedEXEtable; these runtime dispatch values are not new Rooms.kwd fields.'),
        generic_entry=dict(function=0x4E8DA0,mode0='Atactualcurrenttile room26, statusWORD3F2=0: readtable274, HateJobsquery0, original48AD30(1),48AD30(274). ActualcategoryentryattachesCreature; CreatureDWORD2BC=0.',
            eligibility='SourceWarlock5,Imp1,King21,Wizard16,Fairy20,lastspecies46 allenter274 in acceptedgenericpath. This doesnotprove allspecies ordinaryjobselection or everygame condition.',
            HateJobs='FouractualsourceRAW-E74 diagnosticvalues1/17/255/FFFFFFFF loadthroughfull568E30/492320 todefinitionBYTE66E. Allstillenter274; actual494480 visitszeroearlyreturn4944BD without countread49448E/arrayread49449C.',
            mode1='4E8DA0(1) checks specialrooms3/11/12/13/16; Crypt26 takesdefaultreturn1 withstate1/noattachment. Do not confuse it with assignmentmode0.',
            blocked='Nonzero CreatureWORD3F2 returns0 withoutCryptentry in mode0. This is explicit runtime status, not a new KWD flag.'),
        composed_paths=dict(generic='Mode0 chooses274 itself, then originalAIregisters->72wholeCreatureticks walk/turn/80/heal->exit; HP900->905->910->915. No274/registration/actionhostrequest.',
            cleared='Otherwiseidenticalgenericchain, original4966A0afterexit: HP910 ratherthan915; earlier action-retention observation preserved.',
            after_recovery='Explicitoriginalstate257 boundary, action0, initialized494650nullsingleton. OriginalAI481200 applies slot2 recovery displacement then4E8DA0(0) selects274. Nativemovement/ritual/exitHP900->910->915->920.',
            captured_return='Observationwrapper stores actualEAX from4E8DA0 in genericcases,4BC710 in dropcases,476FC0 in recoverycase; zero recovery return is AI return, not failedroomassignment.'),
        drop_boundary=dict(function=0x4BC710,prepared='Alreadyheld actualCreature101 in ownerhandWORD B2,countBYTE1BF=1; ActorDWORD16=2 means notinmap, prev/next0, heldflag1. BellisrealfactoryObject102.',
            observed='Originaldrop decrementscountto0, retainsstalehandtagstorage, moves actualCreature backtoordinarymaplist, clearsheldflag andrequestsstate76. Oneoriginal476FC0executes46EAB0, whichselects255/action52; participantaliveHP900, Cryptcounter0.',
            strict_correction='Do not fabricate a held maplist atcellWORD4. Native4B4F60 skipsunlinkentirely whenActor16=2; erroneous exploratorypreparation leftduplicate101 andwasrejected.',
            uncompleted='Drop-to-recoveryanimationcompletion not executed asoneacceptedchain. Aseparatepost-recoverystate257fixture isnotproof thatfullheld/drop/recoverychaincompleted.',
            excluded='Exploratorylongdropfulltickinvalidfetch andinitialCRTatexitcriticalsectionpath excluded. No lazy lowpage mapping or patchedgamemethod used.'),
        source_lead=dict(physical='Earlierphase85 coordinate-array: raw127B +12*(runtimeIndex-1), sevenadditionaltriples. Runtimeindex2 startsraw1287; Yraw128B.',
            current='SourceWarlock raw1287 triple(0,1490,0). Fullnativegetter494650(2) givesdefinition704 triple;481200 usesonlyYdefinition708 toshiftoppositethefacingvector beforegenericassignment.',
            observation='Facing600h gives initialX22528->24018 inacceptedrecoveryboundary, thenwalkto30644/26617. Causalrawediting remainsnextstep; noindependentmeaningforX/Zassigned.'),
        storage=storage(),native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_room_assignment.py','emulate_crypt_room_assignment.py','crosscheck_crypt_room_assignment.py','prepare_crypt_room_assignment_primary.py']],
        limitations=['Exact providedDX/preparedmap/owners7/8numericIDs/nullrenderer, notallversions or normalWorldscheduler.',
            'Genericroomassignment/recoveryboundary proven; spontaneousjobselection andfullanimationcompletion not proven.',
            'NoCreatureconsumptionobserved; participantalive doesnotexcludealternativelifetimepaths.',
            'HateJob0bypass isforthispath only; doesnotprove noother Crypteligibilityrestriction or alljobsimmune.',
            '8passive groups/10active roles distinct;10owner-counter readers stillunknown. Full15familygoalactive.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase183-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==160
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_room_assignment_semantics phase183:17native/6Ghidra. Generic4E8DA0 chooses274fromfixedroomtable; CryptJobID0 bypassesHateJobs,4native rawE74controls. Actualhanddrop->76->255/action52,separate257recovery->274->walk/ritual/exit; fullanimationbridge open.160priorcomponents/source/codec preserved;10counterreadersunknown;goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase183-search-status.json',dict(phase=183,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Savednativegenericentryandprimaryroom/dropcallers; finalhanddropreplaywasnotexecutedbecauseapprovalserviceusagefailure.'),
        current_turn=dict(classification='progress',reason='Originalgeneric/recoveryentry,actualhanddropboundaries,HateJobrawcontrols,independentGhidra/PE/storageevidencepublished;noongoingapprovalblock.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
