"""Publish mode8 cellHP, Q12 square traversal and original Terrain/CMap/NAV change."""
from pathlib import Path
import json
import emulate_tremor_damage as native
import crosscheck_tremor_damage as controls
import prepare_tremor_primary as primary
import native_tremor as api
from validate_tremor_api import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase151-tremor-primary.json')
    manifest=load('phase151-tremor-emulations.json');rows=manifest['replays'];assert len(rows)==24 and len({r['file'] for r in rows})==24
    for c,row in zip(native.cases(),rows):
        args,checks,saved,path=native.execute(c);post,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(path.relative_to(ROOT)) and row['sha256']==sha(path.read_bytes())
        for index,tid in enumerate([1,2,3]):
            raw=post[native.p.RAW0+index*0x20000];definition=post[native.p.TABLE][tid*460:(tid+1)*460]
            assert int.from_bytes(definition[0x17B:0x17F],'little')==int.from_bytes(raw[0x190:0x194],'little')
            assert int.from_bytes(definition[0x1AB:0x1AD],'little')==int.from_bytes(raw[0x1D7:0x1D9],'little')
            assert int.from_bytes(definition[0x1AD:0x1AF],'little')==int.from_bytes(raw[0x1FF:0x201],'little')
            assert definition[0x1C6]==raw[0x1DA]
    cross=load('phase151-tremor-crosschecks.json')['replays'];assert len(cross)==6 and [r['case'] for r in cross]==controls.CONTROLS
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase151-tremor-api-validation.json')
    preservation=load('room-resource-loader-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    known={str(Path(r['file']).resolve()).casefold():r['sha256'] for r in preservation['source_files']}
    for row in edits['files']:assert known[str(Path(row['file']).resolve()).casefold()]==row['sha256']
    corpus=load('area-damage-semantics.json')['corpus'];assert str(8) not in corpus['damage_mode_counts']
    before=ROOT/'phase151-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==129
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==129
    outcomes={r['case']['name']:r['outcome'] for r in rows}
    return dict(phase=151,date='2026-10-31',families=['Shots','Terrain'],profile=attested['profile'],primary=attested,
        source_usage=dict(source_Shot_records=5551,mode8_records=0,meaning='Diagnosticexecutedmode; no shippedspellusingmode8 foundin82pinnedShotcontainers.'),
        raw_fields=dict(Shot_record_bytes=239,Shot_Data1_u32=0x98,Shot_Data2_u32=0x9C,Shot_DamageType_u8=0xC5,Shot_ProcessType_u8=0xC7,
            Terrain_record_variants=[548,552],Terrain_flags_u32=0x190,Terrain_StartingHealth_u16=0x1D7,Terrain_DestroyedType_u8=0x1DA,Terrain_MaxHealth_u16=0x1FF),
        runtime_cell=dict(bytes=16,HP_u16_offset=6,owner_and_flags_u16_offset=8,TerrainID_u8_offset=10,
            scope='RuntimeCMapcell,notserialized4-byteKLDcell; rawKLDbyte3 remainsunresolved.'),
        dispatch=dict(whole_tick=0x4AB180,dispatcher=0x4AC3C0,table=0x4AC3F4,mode8_stub=0x4AC3ED,handler=0x4ACCA0,
            mechanism='4AC3C0 jump table selectsinteriorJMPstub4AC3ED -> actualfunction4ACCA0. Stubremains ownedbydispatcher; no functioncreatedattheinterioraddress.',
            timing='Onepacket perselectedwholeShot tick; optionalfirstbeforetick+secondroot demonstraterepeateddamage. No completeWorldscheduler claim.'),
        geometry=dict(Data2='signed32(instanceData2)>>12',cell_origin='arithmeticshiftShotXY>>12',shape='Filledhalf-opensquare [cx-R,cx+R)x[cy-R,cy+R)',
            order='Xouter ascending,Yinner ascending. No Euclideandistance/annulus check.',bounds_getter=0x509090,
            clipping='Eachcandidate goes throughoriginalWorldvslot1FC; invalidmapcoordinatesareskipped. Selected32x32edgecases return3and8eligibleRockcells withR2 andcenterleftopen.',
            zero_negative='0,1,4095 rawData2 -> R0 andnorequests. RawFFFFF000 ->R-1 andnorequests inselectedcase.'),
        eligibility=dict(predicate=0x453D10,flags_definition_offset=0x17B,required_mask=1,excluded_mask=2,
            examples='OriginalImpenetrableRock1 skipped; Rock2hit; DirtPath3skipped.',
            owners='452F60 receives cellWORD8&FFF at4ACD24/30, independentlyofcreatorowner7. Diagnosticowner7 andF007 bothforward7; coherentunclaimedowner0 usedfordestruction.',
            Creature='No genericCreatureHPrequests; bothpreparedCreaturetargets retain1000. AttackCategory/resistancepairs are not consumers inthispath.'),
        damage=dict(cell_HP_method=0x452F60,delta_negation=0x4ACD2E,negative_branch='Fullsigned32delta<0: low16(-delta) iscomparedtounsignedcurrentCellHP; subtractifsmaller,elseset0.',
            nonnegative_branch='Addlow16(delta),withnative32000precap; selectedpositiveHPresultsremainbelowRockMaxHealth660. Highermaxhealth/type-changehealing notclaimed.',
            samples=[dict(raw_Data1=80,delta=-80,initial=165,final=85),dict(raw_Data1=65536,delta=-65536,initial=165,final=165),
                dict(raw_Data1=65537,delta=-65537,initial=165,final=164),dict(raw_Data1=0xFFFFFFB0,delta=80,initial=165,final=245),
                dict(raw_Data1=0x80000000,delta=-2147483648,initial=165,final=165)],
            storage='RawData1/Data2remainu32; interpretation ismode-specific and shares storagewithProcessType. Allnewscenarios explicitlyuseProcessNONE0.'),
        destruction=dict(path='Mode8 ->452F60 setsRockHP0 ->44FC40 CMap change -> realnavigationupdate; originalnativecode throughout.',
            source_TerrainID=2,source_DestroyedType=3,new_TerrainID=3,new_StartingHealth=1,old_nav_byte=8,new_nav_byte=0,
            full_tick='destroy-full-tick completeswhole4AB180 afterthreeRock->Dirtchanges; ShotHP31,Creaturesunharmed inselectedscene.',
            self_type_control='DiagnosticrawTerrain1DA=2 loadsdefinition1C6=2. ThreeRockcells reachHP0 butretainTerrain2/nav8; noCMapchangecall.',
            breadth='SourceRock2 only; not allTerrains/gold/rooms/doors/allownershipchanges/drop/effectsuccess.'),
        native_environment=dict(World_and_CMap='Prepared32x32 CWorld/CMap withoriginalvtables, actualcellowner0 andNULLtag0 terminator; sourceTerrain1/2/3 go throughoriginalrawloaders.',
            NAV='Original4CE690/4CE740/4C8F30/4CEF20 runbeforeShot. Realtrianglebuffer2048records, copiedinitialNAVglobals andcorrectbitmap8solid/0floor; destructionrebuildexecutesagainstinitializedgraph.',
            assets='Explicit6-argumentTerrainresourceIO returnsfailure; originalCPCEngineD4/64/80 implementations selectedfromoriginalvtable. Audioadaptersretainopaquehandles/logposition. No GPU/actualassetfiles.',
            actors='PreparednativeCreature/World snapshots; nativeShotfactory executes,notnewCreaturefactories/normalAI.',
            excluded_trials='InitialNULLTerrainservice/CMap,invalidowner0objectalias,missinggraphicsD4 anduninitializedNAV trials excluded. Low-pagechecks rejectedaccidentalNULLgraphaccess despite mechanicalreturn. Longerbudgetneededfor32x32rebuild; acceptedtracesallfinishandlowreadsareonlyFS:0SEH.'),
        effective_shot_parameters=api.effective_parameters(),native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
        counts=dict(native_scenarios=24,complete_Shot_tick_scenarios=sum(bool(r['case'].get('tick')) for r in rows),
            complete_terrain_transition_scenarios=sum(bool(r['outcome']['destroyed']) for r in rows),
            native_Terrain_HP_calls=sum(len(r['outcome']['requests']) for r in rows),native_cells_destroyed=sum(len(r['outcome']['destroyed']) for r in rows),
            independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_function_extents=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
            new_exact_container_edits=edits['new_exact_container_edits'],public_decode_rebuild_controls=edits['public_decode_rebuild_controls'],
            source_Shot_records=edits['counts']['Shot_records'],source_Terrain_records=edits['counts']['Terrain_records'],
            observed_source_mode8_records=0,newly_named_opaque_fields=0,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0),
        helpers=[link(n) for n in ['probe_tremor_damage.py','native_unicorn_tremortrace.py','emulate_tremor_damage.py','crosscheck_tremor_damage.py','prepare_tremor_primary.py','native_tremor.py','validate_tremor_api.py']],
        limitations=['ExactsuppliedDX. OtherEXE/FlameDLL/RC3gamebehavior notnewlyverified.',
            'No mode8 in5551sourceShotrecords. SourceRock/Impenetrable/Dirt definitions anddiagnosticShot17 mode8/ProcessNONE; not a shippedTremorspellclaim.',
            'PreparedCWorld/CMap/actors/resources; originalNAVinitialization andactualterraintransition execute, but no liveWorldscheduler/GPU/realmodels/audio ornewCreaturefactory.',
            'CompleteRock->Dirt path checkedinunclaimedowner0scene; nonlethalowner7/highbits arediagnostics,not alllegalownership/reachability proof.',
            'HPwordarithmetic samples andR0/1/2/bounds tested; not exhaustiveoverflow/hugeextent/otherterrain/max-healthtransformation/drop/effectsuccess.',
            'RuntimecellHP+6 doesnotdecodeunknownserializedKLDbyte3. Terrain198224,CreatureJump/Priority,Traps204,Doors236,AP0408/KeeperC8 andothermajorquestions remainopen; readinessabout75%.'])

if __name__=='__main__':
    d=build();(ROOT/'tremor-damage-semantics.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(d['counts']))
