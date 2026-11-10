"""Publish actual cursor fallback rules and bounded priority/Jump non-consumption."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_pickup_selection as native
import prepare_pickup_primary as primary
import crosscheck_pickup_selection as controls
from validate_pickup_priority_api import validate
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase153-pickup-primary.json')
    manifest=load('phase153-pickup-emulations.json');rows=manifest['replays'];assert len(rows)==34
    for c,row in zip(native.cases(),rows):
        args,checks,e,d,path=native.execute(c);_,out=native.verify(c,args,checks,e,d['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
    cross=load('phase153-pickup-crosschecks.json')['replays'];assert len(cross)==8 and [r['case'] for r in cross]==controls.CONTROLS
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase153-pickup-api-validation.json')
    preservation=load('keeper-validation-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    old=load('phase153-prior-components.json');assert len(old)==131
    for row in old.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    blob=native.EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for a,target in [(0x59968A,0x572820),(0x599707,0x44DEC0)]:
        ins=next(md.disasm(pe_read(blob,a,5),a));assert ins.mnemonic=='call' and int(ins.op_str,16)==target
    byname={r['case']['name']:r['outcome'] for r in rows}
    return dict(phase=153,date='2026-10-31',families=['Creatures','Objects'],profile=attested['profile'],primary=attested,
        field_lineage=dict(CreaturePriority=dict(raw=0xF55,raw_width=1,runtime=0x6EB,native_editor_label='Pick-Up Priority',gameplay_ordering='unresolved'),
            CreatureJump=dict(raw=0xEE0,raw_width=4,runtime=0x75C,native_editor_label='Ability Jump (Tiles)',gameplay_purpose='unresolved'),
            ObjectPriority=dict(raw=0x35D,raw_width=1,runtime=0x112,gameplay_ordering='unresolved'),
            ObjectFlags=dict(raw=0x344,raw_width=4,runtime=0xF4,pickup_mask=0x100,prior_enum_name='CAN_BE_PICKED_UP')),
        native_chain=dict(helper=0x44DEC0,entry_arguments='this=preparedcontext;stack=(signedQ12X,signedQ12Y,outputActorPointer*,mode)',
            Creature_loaders=[0x491DC0,0x568820,0x568E30,0x492320],Object_loaders=[0x56D4F0,0x56D620,0x49DD60],
            per_scenario='Two real Creature definitions from sourceImpID1 with explicitly selectedPriority/Jump bits; one actual sourceObject definition (Gold1, DoorKey17 or DungeonHeart13).',
            providers='Prepared World/Session/cell lists/Actors; original Worldvtable66E3EC. NullGFX provider and explicit external soundlookup adapter; originalcode/vtables intact.',
            caller_static=dict(entry=0x5992B0,primary_graphics_picker=0x572820,primary_call_site=0x59968A,fallback_call_site=0x599707,
                scope='Originalcall instructions andcomplete savedlistings/bytes attested. Thegraphics-pick pipeline andwholecaller arenotexecuted.')),
        map_scan=dict(runtime_cell_bytes=16,cell_head_tag_u16=0,Actor_next_cell_tag_u16=0xC,tag_pointer_table=0x6EE540,
            world_dimensions=[0x9E6,0x9EA],world_cell_pointer=0x9EE,
            bounds='Eachaxis:signed coordinate>>12; clamp center-1..center+1 to0..dimension-1, inclusive.',
            order='Youter ascending, Xinner ascending, then nativecelltaglinkedlist order. Actual coordinate reads at44DFBC recorded andcheckedagainstpreparedlists.',
            raw_warning='Runtime16-bytecells/listheads are not raw4-byteKLDcells; no unknownserializedMapbyte renamed.'),
        distance=dict(formula='u32(max(abs(dx),abs(dy))+(min(abs(dx),abs(dy))>>1))',
            unit='Differences useQ12worldcoordinates. This is the actualapproximation, not a Euclideancircle.',
            default_creature_threshold='strictly<800hex',default_object_threshold='strictly<400hex',object_only_threshold='strictly<800hex',
            boundary_cases='DefaultObject3FFaccepted/400rejected; object-only7FFaccepted/800rejected; Creature800rejected. Extreme signedsubtraction/INT_MIN overflow notexecuted.'),
        selection=dict(default_mode0='FirsteligibleCreature wins andcannotbereplacedbylaterCreature,evencloser. EligibleObject candidatesare rememberedbystrictlysmallerdistance until aCreature ischosen; Creaturetakesprecedence overrememberedObject. IfnoCreature,nearestrememberedObject isreturned.',
            mode_nonzero='Only eligibleObjects are considered; eachstrictlycloser candidate replaces previous. Testedmode1 only, not allnonzero modes.',
            ties='Strictcomparison retains earlierlist/scan candidate at equalapproximateddistance.',
            creature_rejections=dict(Actor_type_must_equal=0,Actor_byte_36F_must_not_equal=0x2E,Actor_flags_3E6_excluded_mask=0x200000,
                Actor_state_25E_excluded=[0xD6,0x4C,0xD1],tested_states=[0xD1],
                scope='Typebyte2E,flag200000 andstateD1 eachtested independently. StatesD6/4C confirmedbyoriginalinstructions only; rawfieldmeaningnotnewlynamed.'),
            Object_eligibility='Actor type2 and (definitionDWORD F4 bit100 OR ActorBYTE12C==13decimal). ActualsourceGold1passesbit100; DoorKey17fails; DungeonHeart13passesIDexception.',
            output='Return1andstoreselectedActorPointer on success. On failure return0 andpreserveexistingoutputDWORD; CAFEBABE sentinels verified.'),
        priority_Jump=dict(status='All threepriority/Jump purposesremainunresolved outside thishelper',
            new_scope='Rawpriority/jump fields copiedbyrealinit/converter/completeCreatureloader. ObjectprioritycopiedbyrealObjectloader. No read overlappingCreature6EB/75C orObject112 byexecuted44DEC0 in34scenarios.',
            controls='CreaturePrioritypairs0/255,255/0,1/40,128/255,255/255 keepfirsttag9; reversinglistchangesselectiontotag10. Objectpriority0/1/255keepsnearesttag10. Jump0/80000000/FFFFFFFFdoesnotchangeselection.',
            global_nonuse_proved=False,primary_graphics_path_covered=False,
            next_scope='Primary572820 ->588A60 ->originalrenderobjectvslot0C remainsunexecuted. NativeUIlabelalone doesnotprovewhichselectionpathusesrawpriority.'),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
        counts=dict(native_scenarios=34,complete_fallback_calls=34,actual_Creature_definition_loads=68,actual_Object_definition_loads=34,
            independent_Ghidra_CPU_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_code_extents=len(attested['functions']),static_only_extents=sum(r['static_only'] for r in attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],
            primary_data_blocks=len(attested['blocks']),source_files_per_family=82,source_Creature_records=3734,source_Object_records=11756,
            exact_container_edits=328,public_decode_rebuild_controls=328,public_scalar_controls=174,extended_Creature_helper_controls=154,
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['emulate_pickup_selection.py','prepare_pickup_primary.py','crosscheck_pickup_selection.py','validate_pickup_priority_api.py','native_pickup_selection.py']],
        limitations=['ExactDX; nativehelperandrawdefinitionloaders executed,notfullcursor/rendering/click/pickup action ornormalWorldscheduler.',
            'Actors/celllinks prepared directly; noActorfactory/normalmapconstruction orKLDbyte3interpretation. Tagtable/list integrity assumesvalidinput.',
            'Primary572820graphicsray/modelpicking notexecuted; fields mayhaveconsumers thereor elsewhere. No globalpriority/Jump nonuseclaim.',
            'Same sourceImp usedfortwoCreaturedefinitions; diagnosticpriorityvalues do notclaimshippedbehavior forallCreaturetypes.',
            'No extremecoordinateoverflow/INT_MINdistance emulation; strictthresholdsverifiedonlyselectedboundaryscenes.',
            edits['scalar_API_limit'],
            'Allunknownfields retain originalbytes/names; no physical/effectiveviewchanges ororiginal/parallel-editor writes. Goal remainsincomplete.'])
def main():
    s=build();(ROOT/'pickup-selection-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase153-prior-components.json')
    assert set(p['components']) in [set(old),set(old)|{'pickup_selection_semantics'}]
    for key,row in old.items():assert p['components'][key]==row
    p['pickup_selection_semantics']=s;p['components']['pickup_selection_semantics']=link('pickup-selection-semantics.json')
    note='pickup_selection_semantics phase153 executes realrawCreature/Object loaders andcomplete44DEC0cursorfallback. DefaultfirsteligibleCreaturewins; nearesteligibleObjectisfallback; mode1choosesnearestObject. StrictapproximateQ12thresholds800/400,YthenXthenlinkedlist order,sourceGold/HeartID13/Key17eligibility confirmed.34native/8Ghidra/328edits. CreaturePriority/Jump,ObjectPriority remainunreadinthishelper; primarygraphics572820 notexecuted andglobalpurpose unresolved. Preserveallfields/effectiveviews.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
