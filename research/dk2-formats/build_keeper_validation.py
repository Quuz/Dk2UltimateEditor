"""Publish bounded original pre-cast validation evidence without renaming C8."""
from pathlib import Path
import json
import emulate_keeper_validation as native
import prepare_keeper_validation_primary as primary
import crosscheck_keeper_validation as controls
from validate_keeper_validation_api import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase152-keeper-validation-primary.json')
    manifest=load('phase152-keeper-validation-emulations.json');rows=manifest['replays'];assert len(rows)==82
    for c,row in zip(native.cases(),rows):
        args,checks,raw,d,p=native.execute(c);_,out=native.verify(c,args,checks,raw,d['result'])
        assert c==row['case'] and out==row['outcome'] and row['sha256']==sha(p.read_bytes())
    cross=load('phase152-keeper-validation-crosschecks.json')['replays'];assert len(cross)==10 and [r['case'] for r in cross]==controls.CONTROLS
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase152-keeper-validation-api-validation.json')
    preservation=load('tremor-damage-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=load('phase152-prior-components.json');assert len(prior)==130
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    return dict(phase=152,date='2026-10-31',family='KeeperSpells',profile=attested['profile'],primary=attested,
        raw_fields=dict(record_bytes=406,CastRule_u8=0xEF,TargetRule_u8=0xDA,ManaCost_u32=0x11C,RechargeTime_u32=0xCC,unknown_C8_u32=0xC8),
        runtime_fields=dict(definition_bytes=122,CastRule_u8=0x76,TargetRule_u8=0x77,ManaCost=0x58,RechargeTime=0x3E,unknown_C8=0x3A),
        native_chain=dict(loaders=[0x4F7C70,0x56B990,0x56BAB0,0x4F7CE0],rule_initializers=[0x5174A0,0x517580],
            full_coordinate_validator=0x4BB8A0,full_creature_validator=0x4BBA40,
            CastRule_dispatcher=0x5176B0,TargetRule_dispatcher=0x517730,
            catalogue='Prepared preallocated single-record array; original50DE50 existence and50DD60 lookup execute through original World vtable66E3EC.',
            IO='Only external sound-handle lookup v1C adapter returns11223344. Null graphics provider. No native code or original vtable substitution.'),
        rule_tables=dict(cast=dict(address=0x6FCDD8,rows=6,row_bytes=17,callbacks=native.CAST),target=dict(address=0x6FCE40,rows=7,row_bytes=18,callbacks=native.TARGET),
            initialization='Original runtime initializers overwrite preparedA5 buffers. Every callback pointer is checked after execution; image/BSS contents are not assumed to already contain callbacks.'),
        cast_rules={
            '0':dict(selector=0,callback=0,native_dispatch_return=0,meaning='No registered predicate; validation fails'),
            '1':dict(selector=1,callback=0x517B80,meaning='Own or allied land, excluding configured neutral owner; not solid by native NAV bit3',
                verified='Own7 and allied8 accepted, nonallied8 rejected. Callflag2 does not impose cell2000 requirement in this rule.',scope='RoomID0 scenes; room-specific branches and neutral-player relation setup not dynamically covered'),
            '2':dict(selector=2,callback=0x517CB0,meaning='Land whose owner passes native Player4BF3D0 relation, and native NAV bit3 clear',
                verified='Own7 and allied8 accepted, nonallied8 rejected. Callflag2 additionally requires cellWORD8 bit2000.',
                neutral='Unlike rule1 there is no explicit neutral-owner rejection; actual neutral relation depends on player alliance-index state, not inferred from enum label alone.'),
            '3':dict(selector=3,callback=0x517DE0,meaning='Nonallied land by native Player4BF390 relation, and native NAV bit3 clear',
                verified='Nonallied8 accepted, own7/allied8 rejected. Callflag2 additionally requires cellWORD8 bit2000.',scope='RoomID0 scenes; all special room branches remain open'),
            '4':dict(selector=4,callback=0x517F10,meaning='Land with native NAV bit3 clear, independent of tested ownership/alliance',
                verified='Own/allied/nonallied accepted; solid case rejected. Callflag2 additionally requires cellWORD8 bit2000.'),
            '5':dict(selector=5,callback=0x517F60,meaning='Predicate unconditionally returns1',scope='Outer existence/mana/cooldown/target gates still apply')},
        target_rules={
            '0':dict(selector=0,callback=0,native_dispatch_return=0,meaning='No registered predicate; validation fails'),
            '1':dict(selector=1,callback=0x517800,meaning='Own creature in tested ordinary branch; excludes definition flag4000',
                call_flag4='Selects nonown, nonneutral creature branch instead',
                scope='Tagged type0 Actor; carried/prison room-owner branches statically visible but not executed'),
            '2':dict(selector=2,callback=0x517930,meaning='Nonown, nonneutral creature; predicate compares owners, not Player alliance relation',
                call_flag4='Calls rule1 with bit4 removed, selecting own-creature branch',scope='Type0 Actor and owner comparisons covered; allied Creature owner not separately tested'),
            '3':dict(selector=3,callback=0x517990,meaning='Any tagged type0 creature whose owner is not configured neutral',verified='Owners7/8 accepted; configured neutral0 rejected'),
            '4':dict(selector=4,callback=0x5179D0,meaning='Coordinate/room predicate; selected RoomID0 point returns1',scope='Not a global unconditional predicate; special owned-room branches not executed'),
            '5':dict(selector=5,callback=0x517F60,meaning='Predicate unconditionally returns1',scope='Full creature wrapper still checks tag presence and type0 before dispatch'),
            '6':dict(selector=6,callback=0x517AB0,meaning='Possession eligibility',
                tested_required='Tag present, Actor type0, owner equals casting Player owner',
                tested_rejections=dict(definition_flags_mask=0x4000,Actor_flags_mask=0x8008,Actor_word_offsets=[0x3CB,0x44B],
                    Actor_state_u32_offset=0x25E,rejected_states=[0xD1,0xFF,0x100,0x101,0xC1,0x116,0x117,0x118]),
                scope='Actual source Possession cast5/target6; object-type2 specialID9 branch is statically visible but full creature wrapper rejects type2 before reaching it')} ,
        caller_flags=dict(argument='Third full-validator argument, not raw Keeper flags atDC',
            bit2='For CastRule2/3/4 requires runtime cellWORD8&2000; not newly labelled fog-of-war/visibility flag',
            bit4='Inverts TargetRule1/2 own-vs-nonown branches; other rule effects not inferred',
            persistence='No claim that these caller bits correspond to any stored KWD field'),
        mana_cooldown=dict(mana_helper=0x4C1740,mana='Original helper accepts cost0; otherwise signed cost<=PlayerDWORD82. Selected positive source costs tested below/equal.',
            cooldown='Timestamp Player spell-stateDWORD5==0 bypasses cooldown. Otherwise native Sessiontick - last compared to loaded definitionDWORD3E.',
            tested='TPS20/rawRecharge4096 ->20 ticks. Elapsed10 rejects; elapsed20 accepts. Timestamp0 accepts despite diagnostic rawRechargeFFFFFFFF.',
            loader='Existing low32(rawRecharge*TPS)>>12 conversion preserved; no new global signedness/range rule'),
        C8=dict(raw_offset=0xC8,definition_offset=0x3A,purpose='unresolved',
            new_scope='Actual full coordinate/creature validators after raw loaders and real rule initializers;10 C8 boundary scenarios across Sight/Possession. No read overlapping definition3A..3D in any of82 accepted traces.',
            rejected_interpretations='No evidence here that C8 is a cast-permission, mana or cooldown parameter. These verified gates read other fields.',global_nonuse_proved=False),
        source_examples=[dict(file=str(native.SOURCE),sha256=sha(native.SOURCE.read_bytes()),records=['Sight Of Evil ID1','Possession ID2','Call To Arms ID3']),
            dict(file=str(native.SOURCE.parent/'maps/Secret1Globals.kld'),sha256=sha((native.SOURCE.parent/'maps/Secret1Globals.kld').read_bytes()),record='Heal ID10: actual CastRule2/TargetRule1, only CastRule2 in1066-source-record corpus')],
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
        counts=dict(native_scenarios=82,full_coordinate_validator_scenarios=sum(c.get('kind','point')=='point' for c in native.cases()),
            full_creature_validator_scenarios=sum(c.get('kind')=='creature' for c in native.cases()),C8_boundary_scenarios=sum('c8' in c for c in native.cases()),
            independent_Ghidra_CPU_controls=10,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_code_extents=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
            source_files=82,source_Keeper_records=1066,new_exact_container_edits=246,public_decode_rebuild_controls=246,
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        limitations=['Exact DX executable; prepared scene state, not live World/AI scheduler or new factory/Shot application.',
            'RoomID0 scenes only; room-dependent cast/target branches, carried/prison ownership paths and actual neutral-alliance state remain open.',
            'CreateImpID4 cost multiplier, SummonHornyID5 actor-list gate and OneShotReaperID14 gate are statically visible but not executed here.',
            'Only full coordinate4BB8A0 and creature4BBA40 wrappers executed; object4BBBC0 inspected statically. Do not call a coordinate TargetRule using an Actor tag as pointer.',
            'Caller flags2/4 are runtime arguments, not rawKeeperDC flags. Runtime cell data does not name rawKLD opaque byte3.',
            attested['listing_limitation'],
            'C8 and all substantive remaining unknowns stay unresolved; no global non-use or full-format completion claim.'],
        helpers=[link(n) for n in ['probe_keeper_validation.py','emulate_keeper_validation.py','native_unicorn_keepertrace.py','prepare_keeper_validation_primary.py',
            'crosscheck_keeper_validation.py','validate_keeper_validation_api.py','native_keeper_validation.py']])
def main():
    s=build();(ROOT/'keeper-validation-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase152-prior-components.json')
    assert set(p['components']) in [set(old),set(old)|{'keeper_validation_semantics'}]
    for k,row in old.items():assert p['components'][k]==row
    p['components']['keeper_validation_semantics']=link('keeper-validation-semantics.json');p['keeper_validation_semantics']=s
    note='keeper_validation_semantics phase152 executes original rawKeeper loaders, rule initializers and complete coordinate/creature validators. OWN_LAND includes tested allies; Creature target1 compares own owner, callflag4 inverts target1/2. Possession rejects selected states/Actor flags/IS_HORNY. Callerflags are not rawKeeperDC.82native/10Ghidra/246edits; C8 no reads in bounded paths, remains unresolved. Existing effective physical/enum/Shot views unchanged.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
