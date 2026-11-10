"""Publish causal raw128B recovery movement with precise PE and storage limits."""
from pathlib import Path
from copy import deepcopy
import json,struct
import emulate_recovery_step as native
import crosscheck_recovery_step as cross
import prepare_recovery_step_primary as primary
import native_recovery_step as api
import build_crypt_movement as previous_storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='creature_recovery_step_semantics'
SCHEMA='creature-recovery-step-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def storage():
    out=previous_storage.storage();p=native.probe.old.old.old
    fmt=p.prior.fmt;file=p.SOURCE;data=file.read_bytes();parts=fmt.parse(data)
    si,s=next((i,s) for i,s in enumerate(parts) if s.signature==170)
    ri,raw=next((i,r) for i,r in enumerate(s.records) if r[0xF4B]==5)
    changes=[]
    for value in [0,-1490,4096,-2147483648,2147483647]:
        edited=api.patch_signed(raw,value,api.DX)
        assert len(edited)==len(raw) and edited[:0x128B]==raw[:0x128B] and edited[0x128F:]==raw[0x128F:]
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride)
        records=list(s.records);records[ri]=edited;clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+s.stride*ri+0x128B
        assert packed==data[:at]+struct.pack('<i',value)+data[at+4:]
        assert clone.header==s.header and clone.suffix==s.suffix
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(x) for x in fmt.parse(packed)]))==packed
        records[ri]=api.patch_bits(edited,int.from_bytes(raw[0x128B:0x128F],'little'),api.DX);clone.replace_records(records)
        assert b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))==data
        changes.append(dict(signed_value=value,width=4,exact_container_edit=True,inverse_restores_source=True,
            native_movement_tested=value in [0,-1490,4096]))
    # The longer editor record is allowed as physical storage; the added
    # extension must survive even though its gameplay meaning is unassigned.
    extended=raw+bytes((i*29+7)&255 for i in range(88))
    assert len(extended)==5537
    extended_edit=api.patch_signed(extended,-1490,api.DX)
    assert extended_edit[5449:]==extended[5449:] and len(extended_edit)==5537
    assert sha(file.read_bytes())==sha(data)
    return dict(prior_seven_source_roundtrips=out,source_file=str(file),sha256=sha(data),selected_creature=5,raw_offset=0x128B,
        edits=changes,extra88_bytes_preserved=True,source_writes=0)

def build():
    manifest=load('phase184-recovery-step-emulations.json');assert len(manifest['replays'])==len(native.cases())==8
    traces=[];trig=native.p.pe_read(native.EXE.read_bytes(),0x6C1488,10240)
    for c,row in zip(native.cases(),manifest['replays']):
        a,ch,s,file=native.execute(c);post,out=native.verify(c,a,ch,s['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        raw=post[native.p.target.RAW]
        assert api.candidate_xy(raw,api.DX,out['initial_position'][:2],out['direction'],trig)==out['final_position'][:2]
        assert api.view(raw,api.DX)['signed_distance']==out['native_slot2'][1]
        traces.append(s['result'])
    controls=load('phase184-recovery-step-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        a,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==a and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase184-recovery-step-primary.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase184-prior-package-state.json')
    for ref in old['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=8,independent_Ghidra_controls=6,checkpoint_snapshots=48,source_raw_Creature_loaders=visit(0x492320),
        post_recovery257_callbacks=visit(0x481200),original_vector_getters=visit(0x494650),generic_room_assignments=visit(0x4E8DA0),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=0,inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        exact_in_memory_container_edits=5,source_files_preserved=420,EXE_paths_preserved=7,prior_components_preserved=161,
        newly_assigned_existing_raw_components=1,source_writes=0,Crypt_counter_meanings_remaining=10)
    field=dict(family='Creatures',raw_offset=0x128B,width=4,storage='Preserve all32 bits; signed32 interpretation in this consumer',
        original_array=dict(raw_start=0x127B,raw_index=1,raw_stride=12,runtime_index=2,raw_X=0x1287,raw_Y=0x128B,raw_Z=0x128F,
            intermediate_Y=0x167B,definition_Y=0x708,reference=link('creature-definition-semantics.json')),
        meaning='Facing-relative backward recovery displacement scalar; candidateXY after get-up action, before generic room assignment.',
        units='4096position units percell; not seconds or speed',consumer='481200/state257, original494650(2)->DWORD+4',
        known_unused_in_this_consumer=['raw1287 X','raw128F Z'],warning='Do not label those components unused globally or erase them.')
    return dict(phase=184,status='verified_bounded_raw_recovery_distance_consumer',profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
        counts=counts,field=field,
        candidate_formula=dict(direction='ActualWORD F4, tested0/200h/400h/600h; API requires0..2047 and does not silently normalizeinvalidgameangles.',
            trig=dict(address=0x6C1488,bytes=10240,sha256=sha(trig),cosine_byte_offset=0x800),
            instructions='48124F calls494650(2);481254 reads DWORD+4. NEG EDX, IMUL32sin/cos, SAR16, then ADD/SUB32 baseXY.',
            expression='m=wrap_s32(-rawY); dx=wrap_s32(sinQ16[theta]*m)>>16; dy=wrap_s32(cosQ16[theta]*m)>>16; candidate=(wrap_s32(X+dx),wrap_s32(Y-dy))',
            boundary='Formula gives candidateXY only. OriginalTerrain bit0/current-vs-target predicate gates movement; native4B5560 and floor logic set actual position/Z.'),
        causal_controls=dict(source='Warlock(0,1490,0),direction600:22528/26624 ->24018/26624.',
            zero='rawY0 keepsXY22528/26624; native Z becomes3584.',negative='rawY-1490 givesX21038; sign reverses observed direction.',
            cardinal='theta0 givesY28114;200givesX21038;400givesY25134;600givesX24018.',
            cell='rawY4096 reachesX26624 withfloorZ3584.',neighbours='rawX30001/rawZ-30002 load to actualslot2 but keepXYsame as sourceY1490.',
            no_ritual='Allcasesend274/action0/HP900/counter0; sourceSpellcatalogue1/2/14/41 is genuinelyloaded for481400. Registration,action80 andfullWorldscheduler are outside this small boundary.'),
        fixture=dict(initial='Explicit257after-recovery state and no action. Preparedmap/Bell/sourceCrypt26. Not oneuninterruptedheld/drop/animationchain.',
            initialized='Sameinitialized494650singleton/nullfallback and original32x32NAV as183; CRTstartup/shutdown excluded.',
            graphics='Freshsource492320/504D80 with declaredexternalnull-rendereravailability/handle0. No gamecode/vtable/native function substitution.',
            strict='OnlyDWORD FS:[0]below4096, source raw/vtables/unrelatedCreature/otherowner/guards, actualBell/mapprevnext andpoolusedbytes checked.',
            excluded='EarlymissingWarlockSpellcontext lowread27 excluded; terminalobservationcopytimeout retained and separately retried. No faulting trace promoted to evidence.'),
        API=dict(file=link('native_recovery_step.py'),reader='view',physical_bits_patch='patch_bits',signed_patch='patch_signed',candidate_only_projection='candidate_xy',
            source='Exact inspectedDX profile required; raw5449/5537storageallowed, extra88physicalbytespreserved. OtherEXEversions notassigned.'),
        storage=storage(),native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_recovery_step.py','check_recovery_step_discovery.py','emulate_recovery_step.py','native_recovery_step.py','crosscheck_recovery_step.py','prepare_recovery_step_primary.py']],
        priority=dict(current='Crypt groups',block_complete=False,next=['Actualheld/drop76->255/52->257animationcompletion beforeCrypt; ordinaryjobselection stillopen.',
            'Naturalaction80interruptions andtenremainingowner-counterreaders; recoverydistanceproofdoesnotassignthosecounters.']),
        limitations=['Only boundedprovidedDX/Warlock/selectedmap/directions/values. NotallCreaturetypes/allangles/floors oroverflowedpositions executed.',
            'Arithmeticformula fromactualinstructions; APIpredictscandidateonly. Extremeint32values arestorage/inversepatchtests, notsafeorvalidlivepositions.',
            'Currentfieldnameapplies tothisconsumer. No globalnonuseclaimforneighbourcomponents, no separateportalcoordinatechange.',
            'Crypt remainsincomplete;8passivegroups/10active rolesdistinct,tenowner-countermeaningsunknown. Full15familygoalactive.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase184-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==161
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(file.name);s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='creature_recovery_step_semantics phase184:8native/6Ghidra. Creaturesraw128B->mid167B->def708/XYZslot2Y->native481200 facing-relativecandidateXY afterrecovery257. Zero/negative/cardinal/cell/XZcontrols; exact4byteAPI/5inversecontaineredits.161priorcomponents/source/codec preserved;fullanimationbridge/10Cryptcounterreadersopen;goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase184-search-status.json',dict(phase=184,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Published/audited183; eightstrictnative raw128Bcontrols saved asunpublished184discovery.'),
        current_turn=dict(classification='progress',reason='IndependentGhidra/fullPE/storageproof andboundedfieldAPIpublished;161componentspreserved.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
