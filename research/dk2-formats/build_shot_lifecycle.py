"""Publish original complete Shot firsttick/terminal damage and signedData1 evidence."""
from pathlib import Path
import json,os
import emulate_shot_lifecycle as native
import crosscheck_shot_lifecycle as controls
import prepare_shot_lifecycle_primary as primary
import native_shot_lifecycle as api
from validate_shot_lifecycle import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase148-shot-lifecycle-primary.json')
    manifest=load('phase148-shot-lifecycle-emulations.json');rows=manifest['replays']
    assert len(rows)==22 and len({r['file'] for r in rows})==22
    for c,row in zip(native.cases(),rows):
        args,checks,saved,path=native.execute(c);_,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(path.relative_to(ROOT)) and row['sha256']==sha(path.read_bytes())
    cross=load('phase148-shot-lifecycle-crosschecks.json')['replays'];assert len(cross)==6
    assert [r['case'] for r in cross]==controls.CONTROLS
    for row in cross:
        args,n,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase148-shot-lifecycle-api-validation.json')
    assert edits['damage_mode_counts']=={'0':3357,'1':111,'2':812,'7':1271}
    preservation=load('melee-animation-flag-semantics.json')['preservation']
    # Reuse the previous attested roster and add only previously hashed Shot
    # sources missing from it. No current hash is silently adopted as baseline.
    preservation=json.loads(json.dumps(preservation))
    known={os.path.normcase(os.path.abspath(r['file'])):r['sha256'] for r in preservation['source_files']}
    for row in edits['files']:
        key=os.path.normcase(os.path.abspath(row['file']))
        if key in known:assert known[key]==row['sha256']
        else:preservation['source_files'].append(dict(file=row['file'],sha256=row['sha256']));known[key]=row['sha256']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    before=ROOT/'phase148-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==126
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==126
    return dict(phase=148,date='2026-10-31',family='Shots',profile=attested['profile'],primary=attested,
        physical_fields=dict(raw_record_bytes=239,raw_Data1_u32=0x98,raw_Data2_u32=0x9C,raw_flags=0xA8,raw_initialHP_u16=0xBC,
            raw_damage_mode=0xC5,raw_ProcessType=0xC7,definition_Data1=0x37,definition_Data2=0x3B,definition_damage_mode=0x6B,
            instance_Data1=0x13D,instance_Data2=0x141,instance_first_tick_guard=0x149,instance_tick_counter=0x12D),
        start_damage=dict(mode=5,root=0x4AB180,test_guard=0x4AB196,test_mode=0x4AB270,call_handler=0x4AB278,set_guard=0x4AB286,
            handler=0x4AC5A0,meaning='Once on firsttick, not on factory creation. Same mode5 firsttick damage aftercreation_arg1 in selected diagnostic; no generalmeaning assigned to creation_arg.',
            tested_first_and_second_tick_target_HP=[[920,920],[920,920]],
            terminal_before_tick='No damage; firsttickguard remains0, genericstate becomes2.',
            terminal_after_tick='No second damage; firsttickguard remains1, genericstate becomes2.'),
        terminal_damage=dict(mode=6,root=0x4AAF70,world_getter_call=0x4AAFAD,world_getter=0x509850,world_field=0x723B,world_setter=0x509860,
            world_test=0x4AAFB0,mode_test=0x4AAFBA,handler_call=0x4AAFC2,handler=0x4AC5A0,
            gate='Only exactzero permits damage. Prepared1and2 bothskip damage; cleanup stillruns.',
            live_HP='Explicitterminal withShotHP32/tickcount0 applies damage and setsNodeDWORD16=2. No requirement forHPalready0 inside thiscallback.',
            expiry='After originalHPsetter -31 then onewhole tick,ShotHP0 and targets remain1000. Theexpiry path requests deletion throughoriginalvslot20/4B2AD0; Node16 remains0. Explicitsubsequent4AAF70 causes80damage andstate2.',
            not_executed='NormalWorldscheduler choice/timing ofterminalcallback andphysicalpooldeallocation; no repeatedterminal/idempotence claim.'),
        parameter_interpretation=dict(handler=0x4AC5A0,
            Data1='Native32-bitnegation before originalHPmethod; tested+80/0/-80. Negative-80 requests+80HP in selected enemy-target scene, and1000->1080 was applied. Existingeligibility/caps stillbelong to originalHPpath.',
            raw_negative80=0xFFFFFFB0,raw_storage='u32 unchanged; do not globally convertData1 to signed for allShotprocesses.',
            Data2='Integer half-open square extent2 hits bothpreparedtargets;0producesnoHPrequests.',
            sharing='DamageType andProcessType are separated dispatches sharingData1/Data2. Acceptedlifecycle cases explicitlyuse ProcessNONE0; no claim that replacing a shippedProcessType preserves its effects.'),
        compositions=dict(native_chain='Creaturedefinition ctor/MID/rawconverter/loader -> Shotdefinition ctor/MID/rawconverter/loader -> originalShotfactory4B7CD0 -> originalWorldsetter509860 -> selectedwhole4AB180ticks/4AAF70terminal.',
            health='Optional original4B2CE0 HPchange beforetick; no hostwrite to synthesizeterminalstate.',
            prepared_scene='Diagnostic rawShot17 andCreaturetargets; owner7 creator versusowner8 twoeligibleCreatures; preparedWorld/map/list/pool contexts fromphase131, directCPU invocation.',
            external_IO='Resource/session/audio adapters retained fromphase131. PositionalSoundvslot30 adapter accepts(handle,XYZ*),returnsopaquehandle andlogsposition; soundhandles2E/32 eachupdated perselectedtick. Original renderer64/80 methods execute from the originalCPCEngine table.',
            code='OriginalPE/vtables unchanged. OnlyexternalIO adapters executeoutsideoriginalPE. Allcheckednonstackregions andoriginalShotvtable included inCPUcontrols.',
            excluded_trials='Earlyincompletefixtures missing renderer slots/audio positionalcallback failed beforecompletion; excludedfrom22acceptedcases andnot evidence of a game crash.'),
        effective_shot_parameters=api.effective_parameters(),native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
        counts=dict(native_scenarios=22,independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),original_function_extents=len(attested['functions']),
            unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
            new_exact_container_edits=edits['new_exact_container_edits'],public_decode_rebuild_controls=edits['public_decode_rebuild_controls'],source_Shot_records=edits['source_records'],
            observed_source_mode5_records=0,observed_source_mode6_records=0,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),
            newly_named_opaque_fields=0,source_writes=0),
        closed_negative_leads=[link('phase148-flag-extract-leads.json'),link('phase148-untyped-flag-leads.json')],
        helpers=[link(n) for n in ['probe_shot_lifecycle.py','emulate_shot_lifecycle.py','crosscheck_shot_lifecycle.py','prepare_shot_lifecycle_primary.py','validate_shot_lifecycle.py','native_shot_lifecycle.py']],
        limitations=['Exact suppliedDX; no automatic transfer toRC3,localoldEXE,normalDKII orpatchDLLs.',
            'No modes5/6 in5551sourceShotrecords; diagnosticexecution demonstrates support, not shippedusage.',
            'PreparedWorld/map/targets/pool andexplicitresource/audioIO; directwholecallbacks, not a complete livegame/flight/collision-to-deallocation scene.',
            'NormalWorldscheduler andphysicalpoolfree notcomposed. Genericdeletionrequest andterminalcallback aredistinct; directterminaltestedonce percase.',
            'Data1tested+80/0/-80 andData2zero/2; no exhaustiveoverflow/INT_MIN/largemap/HPcap coverage or global signedness forsharedprocessparameters.',
            'World723B onlyprovenas exactzero terminaldamagegate; its broadergameplaymeaning remainsunnamed.',
            'No newopaquephysicalfield receives a name. CreatureFlags800/1000 searches yielded no newconfirmedmeaning; significant15familyunknowns remain andreadiness staysabout75%.'])

if __name__=='__main__':
    d=build();(ROOT/'shot-lifecycle-semantics.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(d['counts']))
