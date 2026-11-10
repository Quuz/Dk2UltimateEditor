"""Publish complete original object-sacrifice callback and its raw Gold flag boundary."""
from pathlib import Path
import json
import emulate_temple_object_entry as native
import crosscheck_temple_object_entry as controls
import prepare_temple_object_primary as primary
from validate_temple_object_edits import validate as validate_edits

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase135-temple-object-primary.json')
    runs=load('phase135-temple-object-emulations.json')['replays'];assert len(runs)==len(native.cases())==19
    for c,row in zip(native.cases(),runs):
        args,checks,wanted,expected=native.build(c);path=native.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==args and saved['expected']==expected and saved['case']==json.loads(json.dumps(c)) and sha(path.read_bytes())==row['sha256']
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
        assert native.verify(c,args,checks,wanted,expected,saved['result'])==row['outcome']
    cross=load('phase135-temple-object-crosschecks.json')['replays'];assert len(cross)==len(controls.CONTROLS)==5
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    edits=validate_edits();assert edits==load('phase135-temple-object-api-validation.json')
    assert edits['current_ingredient_type_counts']=={'1':92,'3':9} and not edits['original_gold_recipe_ingredient_occurrences']
    inherited=load('terrain-opaque-table-semantics.json')['preservation'];sources={}
    for row in inherited['source_files']+edits['files']+edits['source_catalogues']:
        k=str(Path(row['file']).resolve()).lower()
        if k in sources:assert sources[k]['sha256']==row['sha256']
        sources[k]=dict(file=row['file'],sha256=row['sha256'])
    preservation=dict(source_files=list(sources.values()),executable_paths=inherited['executable_paths'])
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase135-prior-components.json'
    if not prior.exists():
        v=load('editor-format-package.json')['components'];assert len(v)==113
        prior.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==113
    radio=load('object-property-semantics.json')['flags']['object_type_radio']['choices']
    gold=next(r for r in radio if r['mask']==0x40);assert gold['label']=='&Gold' and gold['control_id']==1401
    by_name={r['case']:r['outcome'] for r in runs}
    return dict(phase=135,date='2026-10-30',families=['Objects','GlobalVariables type43','Temple room instance'],
        profile=attested['profile'],primary=attested,
        replaces_coverage=dict(component='sacrifice_semantics',historical_limitation='Object sacrifice entry statically reviewed only',
            current='Complete49F600entry now executed with rawObjects definition and rawtype43recipes in sameCPU. Creature472930entry remains static-only; live scheduler/physicalfall/fullWorld removal not covered.'),
        fields=dict(raw_Object_Flags=0x344,physical_width=4,intermediate=0x3C4,definition=0xF4,
            tested_mask=0x40,native_editor_label='Gold',native_editor_control_id=gold['control_id'],
            predicate='At already invoked49F600callback: currentmapcell resolves nonzero roomtag; roominstance BYTE18==0D; cellBYTE0C==FF; ObjectDefinitionDWORD F4 has40hclear.',
            meaning='Gold classification excludes object from this sacrifice route even when a matching object recipe is loaded. Other flags remain independent and must be preserved.',
            no_raw_object_ID_whitelist=True,instance_flags_distinct='ActorDWORD146 bit0 is cleared only on accepted sacrifice; it is a separate instance field, not rawObjectFlags344.'),
        input_chain=dict(raw_Object_init=0x56D4F0,convert=0x56D620,load=0x49DD60,
            raw_recipe_load=0x51DB30,append_recipe=0x50F7E0,entry=0x49F600,
            actor='Prepared complete393byte post-factory Object snapshot with explicitly matched ID,owner,position,definition pointer,HP1000 and instanceflags13579BDF. Factory is not newly executed.',
            world_cell='Original Worldvslot1D4→508FE0 returns cell from actorXY.453A20checks actual resolved roomcategory;4559B0resolves roompointer.',
            callback_registration_static_only=dict(function=0x49E600,store_PC=0x49E62B,destination=0x6E4B8C,
                note='Original initializer assigns49F600 into callback table. Registration, table dispatcher, physical drop and normal object tick activation not newly executed. No stateID inferred from table index.')),
        accepted=dict(history='4F1600 appends(type2,actorBYTE12C,level0), including older history and normal three-entry eviction rules.',
            lifecycle='Clear instanceDWORD146 bit0; original Object vslot20→4B2AD0 sets actorHP40=0. Tag100 and alive/cell registries remain unchanged in all passing cases. Not a completed delete/free.',
            matcher='Walk World raw-loaded recipes in order through4F1660; first match calls4F1780 with original actor-position pointer.',
            no_match='Accepted object still has HP0 and remains appended tohistory; no requirement that a recipe match before consumption.',
            match='Reward dispatch clears all history indices before award, including older entries outside a one-object recipe. Prepared mana reward5 reaches roomowner2.',
            owner='Objectowner3 does not block this callback. Mana reward goes to roomowner2; itemowner field/pointer remain unchanged by callback.',
            return_value='Entire49F600returns EAX0 for both acceptance and rejection. It is not a success Boolean.'),
        rejected=dict(gold='Branch40h skips history, HP-zeroing, recipe matching and reward; olderhistory retained. Gold with a matching loaded rawrecipe is still skipped.',
            map='Absentroom, different roomcategory or cellBYTE0C!=FF returns without accepting item.',
            notification='Gold attempts call message47/type7; accepted nonmatch calls message47/type1. Actual4C4600 stops via owner19A=1 in fixture; live speech/event delivery not covered.'),
        visual_height=dict(function=0x4545C0,helper=0x4F1940,condition='unsigned32(actorZ-surfaceZ)<4096 gates visual helper only; it does not gate history orHP0.',
            tested_surface=0,actorZ=[0,4095,4096,8192,4294967295],
            result='0/4095callhelper;4096/8192/FFFFFFFFskip. AllnonGoldotherwiseeligible objects accepted in each tested height case.',
            effect_boundary='RoomDefinitionWORD429=0 makes4F1940return before actualeffect/audio. No visual/gfx claim, no newly named rawRoom field.'),
        examples=dict(source_Gold=by_name['source-gold'],source_DoorKey=by_name['source-door-key'],
            mana_cap=by_name['mana-cap'],foreign_owner=by_name['foreign-object-owner']),
        source_recipe_scope='Current44original recipes contain92Creature and9Spell ingredient occurrences, noObject ingredients. Object→mana5 test recipes are diagnostic engine support, not originaltable behavior.',
        native_executions=runs,independent_Ghidra_controls=cross,corpus=edits,preservation=preservation,
        counts=dict(full_native_object_entry_paths=19,full_raw_Object_definition_loads=19,
            raw_type43_recipe_loader_paths=sum(c['reward'] for c in native.cases()),
            executed_function_spans=attested['executed_function_spans'],static_only_registration_spans=1,
            unique_executed_original_PCs=attested['unique_executed_original_PCs'],original_vtable_blocks=2,
            full_prepared_bytes_independently_predicted=sum(r['outcome']['complete_prepared_bytes'] for r in runs),
            independent_Ghidra_CPU_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),new_exact_container_rebuilds=82,
            original_Object_source_containers=82,original_recipes_inspected=44,
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=7,newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['emulate_temple_object_entry.py','crosscheck_temple_object_entry.py',
            'prepare_temple_object_primary.py','native_temple_object_entry.py','validate_temple_object_edits.py',
            'native_unicorn_factorytrace.py','native_object_loading.py','native_sacrifice.py',
            'ghidra/phase89-object-factory-base.bin']],
        limitations=['ExactDXhash only. No oldEXE/RC3/Flame/patchDLL behavior established.',
            'Object actor is a prepared post-factory snapshot. RawObject definition and rawrecipe loaders plus full49F600entry execute; no freshfactory, physical drop, callbackscheduler or wholeWorld bootstrap.',
            'Callback registration49E600is whole-function static evidence only. Activationstate number and normalplayer/drop reachability not newly proved.',
            'HP0 and instanceflagbit clear are exact; final unlink/delete/free and subsequentObjecttick not executed. Do not tell editor users the item is immediately removed from every registry.',
            'Room effect429=0 andowner19A=1 suppress realvisual/audio/notifications. Height gates only entry to a helper, not visible animation proof.',
            'One supplied DoorKey17raw base for diagnosticmask tests, plus sourceGold1/sourceDoorKey17unchangedflags cases. RecipeObject→mana5 is diagnostic, absent44originalrecipes.',
            'Map/room/owner/tag state supplied explicitly. Callback accepts a foreign object in fixture; no claim that any live hand/drop route permits every such state.',
            'Terrain198/224/16WORDpurpose, CreatureJump/Priority,Traps204,Doors236,otherformat unknowns and fullCreatureTemple entry remain unresolved.'])

if __name__=='__main__':
    v=build();(ROOT/'temple-object-entry-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(v['counts']))
