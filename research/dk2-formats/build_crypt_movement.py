"""Publish bounded original Crypt AI movement, renderer context and ritual chain."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_movement as native
import crosscheck_crypt_movement as cross
import prepare_crypt_movement_primary as primary
import validate_crypt_action_storage as prior_storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='crypt_movement_semantics'
SCHEMA='crypt-movement-semantics.json'
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())

def storage():
    out=prior_storage.validate()
    fmt=native.probe.old.prior.fmt
    for name in ['Rooms.kwd','Terrain.kwd','CreatureSpells.kwd']:
        file=native.probe.old.SOURCE.parent/name;data=file.read_bytes();parts=fmt.parse(data)
        assert b''.join(s.pack() for s in parts)==data
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in parts]))==data
        out['source_files'].append(dict(file=str(file),sha256=sha(data),bytes=len(data),exact_public_decode_rebuild=True))
    assert len(out['source_files'])==7
    return out

def build():
    manifest=load('phase182-crypt-movement-emulations.json');assert len(manifest['replays'])==len(native.cases())==9
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,s,file=native.execute(c);_,out=native.verify(c,args,ch,s['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        traces.append(s['result'])
    controls=load('phase182-crypt-movement-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase182-crypt-movement-primary.json')
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    prior=load('phase182-prior-package-state.json')
    for ref in prior['components'].values():assert sha((ROOT/ref['file']).read_bytes())==ref['sha256']
    visit=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=9,independent_Ghidra_controls=6,checkpoint_snapshots=54,
        whole_Creature_ticks=visit(0x488930),AI_dispatches=visit(0x476FC0),Crypt_registration_callbacks=visit(0x483790),
        Crypt_bellside_callbacks=visit(0x483870),native_action_activations=visit(0x496700),Crypt_action_callbacks=visit(0x499CA0),
        native_force_initializers=visit(0x4D56E0),follow_path_calls=visit(0x4D3C40),HP_requests=sum(len(r['hp_requests']) for r in traces),
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        additional_primary_extents=len(proof['functions']),inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        prior_components_preserved=159,source_files_preserved=420,EXE_paths_preserved=7,new_raw_fields=0,new_raw_edits=0,source_writes=0,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    constructors=[];blob=native.EXE.read_bytes()
    for target,context in [(0x4D2400,0x6ECAB0),(0x4D2470,0x6ED330),(0x4D24E0,0x6ED358),(0x4D2550,0x6ED3D8),
                           (0x4D25C0,0x6ED3B0),(0x4D2630,0x6ECA88),(0x4D26A0,0x6ECAF0),(0x4D2710,0x6ED388)]:
        stub=target-32;raw=native.p.pe_read(blob,stub,10)
        assert raw[0]==0xB9 and int.from_bytes(raw[1:5],'little')==context and raw[5]==0xE8
        assert stub+10+int.from_bytes(raw[6:10],'little',signed=True)==target
        constructors.append(dict(initializer_stub=stub,original_call=stub+5,constructor=target,context=context,bytes_hex=raw.hex()))
    return dict(phase=182,status='verified_bounded_native_AI_walk_and_ritual_not_whole_Crypt',counts=counts,
        priority=dict(current='Crypt groups',block_complete=False,next=['Original normal job selection leading to274, including definition job eligibility and ranking.',
            'Natural interruptions and longer action80 lifetime in actual World scheduling.', 'Actual owner-counter readers for remaining ten groups; direct active effects do not assign those counter meanings.']),
        supplied_DX=dict(file=str(native.EXE),sha256=sha(blob)),
        observed_chain=dict(initial='Explicit original476D30 request274 attaches actualCreature101 to sourceCrypt26/room300. Prepared32x32 map, Bell142, owner7, HP900, no startingaction. Owner IDs7/8 are explicit registry identities; their faction labels are not assigned by this test.',
            registration='Original476FC0 dispatches483790, which requests path471890 and calls4D8E60: state6, room53=101, owner108A=1. No directcallback/setter hostcall.',
            walking='72whole488930 updates, explicit clocks1..72 and original48D010 speed256: XY22528,26624 ->30469,26615; native map transfers preserve lists. No supplied desiredvelocity or postentryXY.',
            turning='At64updates state275 anddirection600h but action0; the next AI evaluation can activate only after fulltick turning has finished. 64updates are not failure to enter Crypt.',
            activation='By72updates original AI dispatch has called483870->496700(action80); HP900->905 during fullticks. Neither483790,483870,4D8E60 nor496700 is in fixturebeforecalls.',
            cycle='Actiontick atclock100 heals905->910; original categoryexit clearsroom/counter but leavesaction80; next wholeCreaturetick atclock200 enters89 andheals910->915.',
            clear_control='Original4966A0 afterexit stopsaction80: matched fulltick keepsHP910, rather than915. Participant andrecipient remainalive.'),
        navigation=dict(constructors=constructors,initializer='Original4D56E0, explicit256bytevectorstorage, eight original descriptorconstructors and four original Worldcallbackregistrations.',
            forces=[dict(name=n,weight_Q16=w,descriptor=d) for n,w,d in [('avoid_walls',0x9999,0x6ECAF0),('repel_obstacle',0x6666,0x6ED3B0),('follow_path',0x5999,0x6ED388),('separation',0x2147,0x6ED358)]],
            negative='Withoutforceinitialization, same72fullticks keepXY22528,26624/state6/action0. Emptyforcearray was a fixture limitation, not a game defect.',
            trig='Exact10240sourcePEbytes at6C1488, not the inherited cardinal-only table. Speed4095 over32ticks overshoots target and remains6; this does not prove broken live navigation.'),
        graphics_fixture=dict(mode='FreshWorld+A3C7=0; original492320 receives declaredCPCEngineInterface. Externalassetquery returns success andhandle0; original504D80 parses sourceArtResource metadata/presence.',
            bounds='No actual meshes, GPU or original live renderer loaded. Numeric-only update-mode/null-service context was insufficient for prolonged animation/action changes.',
            strict='Earlier exploratory traces with read0F at4967BA are rejected and not amongnine acceptedscenes. They are retained, not treated as proof or game crashes.'),
        controls=dict(already_at_goal='One476FC0 continuation reaches275/action0; second starts80. Owner7 and8 cycles checked.',
            guards='OnlyDWORD FS:[0] below4096; source raw/vtables/otherowner/thirdCreature/guards checked. Allnative methods andvtable targets intact.',
            map='Everyactualcellnode appears exactlyonce, with native prev/next and coordinatecell consistent; actor101,recipient100,Bell102 allchecked.',
            clock='Clock values are explicit host conditions; no actual World scheduler or frame-rate duration claim.'),
        storage=storage(),native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,preservation=baseline,
        helpers=[link(n) for n in ['probe_crypt_movement.py','emulate_crypt_movement.py','crosscheck_crypt_movement.py','prepare_crypt_movement_primary.py']],
        limitations=['The initial state274 request is explicit: spontaneous ordinary job selection and exact work/job classification remain unknown.',
            'Bounded providedDX/Warlock/preparedmap/speed256/nullrenderer; not allCreaturetypes/versions/normalmovement speeds or fullWorldsimulation.',
            'Participant survives accepted ritual/exit; no consumption observed. All alternative paths and whole lifetime remain unproved.',
            '8passive counter meanings and10active group roles are distinct. Ten owner-counter consumers remain unassigned.',
            'No new serialized fields or codec changes; full15family goal remains active.'])

def main():
    package=load('editor-format-package.json');file=ROOT/'phase182-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==159
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    prior=load(file.name);s=build();dump(SCHEMA,s)
    assert set(package['components']) in [set(prior['components']),set(prior['components'])|{KEY}]
    for k,v in prior['components'].items():assert package['components'][k]==v
    for k,h in prior['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    note='crypt_movement_semantics phase182:9native/6Ghidra/54checkpoints. Original AI274->6, force-driven walk, turn,275->action80->heal->exit. Explicit state274/room/speed256/clock/nullrenderer; no forcedCryptcallbacks or XY. StrictFS0/maplists/bytes;159previouscomponents preserved. Normaljobselection and10counterreaders open;goalactive.'
    if note not in package['precedence']:package['precedence'].append(note)
    dump('editor-format-package.json',package)
    dump('phase182-search-status.json',dict(phase=182,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='no_progress',reason='Previous conceptual answer only inspected existingphase181; revalidated unfinishedphase182probe, then extended actualnative movement rather than restating status.'),
        current_turn=dict(classification='progress',reason='Completed force constructors, actual72Creaturetick walk/turn/AIritual; strictrejections retained, independentGhidra/PE/storage proofs published.'),
        counts=s['counts'],remaining=s['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))

if __name__=='__main__':main()
