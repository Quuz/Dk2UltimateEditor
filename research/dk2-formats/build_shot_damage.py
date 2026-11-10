"""Publish exact raw Shot category consumers and three bounded damage paths."""
import hashlib
import json
from pathlib import Path
import emulate_shot_damage as native
import prepare_shot_damage_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(name):return dict(file=name,sha256=digest((ROOT/name).read_bytes()))


def build():
    manifest=load('phase114-shot-damage-emulations.json');cases=native.probe.cases()
    assert manifest['scope']==native.__doc__ and len(manifest['replays'])==len(cases)==18
    replays=[]
    for c,row in zip(cases,manifest['replays']):
        args,checks=native.probe.build(c);p=native.path_for(args);v=load('ghidra/'+p.name)
        assert v['case']==c and v['input']==args and row['file']==p.name and row['case']==c['name']
        assert row['sha256']==digest(p.read_bytes())
        post=native.verify(c,args,checks,v['result']);assert post==native.expected(c)
        replays.append(row)
    assert load('phase114-primary-manifest.json')==primary.build()
    api=load('phase114-shot-damage-api-validation.json')
    for row in api['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase114-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==92
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==92
    previous=load('creature-resistance-semantics.json')
    for row in previous['helpers']+previous['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    return dict(phase=114,date='2026-10-29',families=['Shots','Creatures','Objects'],
        scope='RawCreature target and rawShot definitions load in one CPU memory; real Shot factory followed by complete selected damage consumers.',
        executable=primary.build()['source_exe'],executable_sha256=primary.build()['sha256'],
        primary_evidence=link('phase114-primary-manifest.json'),native_replays=replays,
        physical=dict(record_bytes=239,signature=0x104,
            data1=dict(raw=0x98,intermediate=0x120,definition=0x37,instance=0x13D,width=4),
            data2=dict(raw=0x9C,intermediate=0x124,definition=0x3B,instance=0x141,width=4),
            attack_category=dict(raw=0xC8,intermediate=0x150,presence=0x80,definition=0x51,width=1),
            damage_algorithm=dict(raw=0xC5,intermediate=0x14D,presence=0x70,definition=0x6B,width=1),
            process=dict(raw=0xC7,intermediate=0x14F,definition=0x6C,width=1)),
        raw_load=dict(initializer=0x56E540,converter=0x56E690,constructor=0x4ACD60,loader=0x4ACDF0,
            intermediate_bytes=375,definition_bytes=118,
            full_intermediate='34 little-endian DWORD presence values1 then239raw bytes; name terminators atA7/170 forced0.',
            checksum='When PE-global6FCDCF==0, loaded definitionDWORD33 equals unsigned sum of all375 intermediate bytes.',
            native_pipeline='Four Creature load calls then four Shot load calls, original4B7CD0/4AAC90 creation then selected damage handler; no raw field substituted by gameplay code.'),
        direct=dict(handler=0x4AC420,algorithm=7,source_shot=11,source_name='Knives',
            configured_resistance_type=18,source_data1=20,
            HP_for_resistance_values={'0':980,'100':980,'255':980},HP_initial=1000,
            tested_gates=['Data1=0 and signednegativeFFFFFFFF ignored.','TargetWORD445!=0 prevents selected Creature hit.'],
            scope='Whole hit handler,not projectile movement/collision selection. Original target vslot34 and common HP handler executed.'),
        area=dict(dispatcher=0x4AC3C0,handler=0x4AC5A0,algorithm=1,
            scene='Shot11 Knives definition with selected algorithm1,Data1=20,Data2=1; targetCreature in enemy owner8 cell,original map/list lookup.',
            HP_for_resistance_values={'0':980,'100':980,'255':980},friendly_owner7_HP=1000,
            damage_source='InstanceData1 used directly; this handler bypasses HIT_THING.',
            geometry='IntegerData2 defines half-open cell scan [center-extent,center+extent); no Euclidean distance filter in this handler.'),
        range_area=dict(dispatcher=0x4AC3C0,handler=0x4AC6D0,algorithm=2,
            scene='Shot17 Mistress Hail Storm,category6,Data1=20,Data2=2; original map/list lookup.',
            configured_resistance_type=11,HP_for_resistance_values={'0':980,'100':980,'255':980},
            tested_distance=dict(center_HP=980,one_cell_of_two_cell_radius_HP=990),
            damage_source='Original fixed-point distance/sqrt calculation executed; only center and one-cell offset tested. No universal rounding model asserted.'),
        category_consumer=dict(handler=0x4AC420,target_kind=2,target_object_ID=74,
            prepared_target='Original Object snapshot,damageabledefinition flag200000,HP1000,owner8; rawObject loader and physical Boulder motion not executed.',
            category4=dict(Data1_20_HP=995,Data1_23_HP=995),category6=dict(Data1_20_HP=995),
            category8=dict(Data1_20_HP=1000),
            interpretation='For ObjectID74 only: categories4/6 use signeddivision4; other categories zero damage. Branch exists independently of Creature resistance table.',
            labels='FIRE/LIGHTNING/PROJECTILE are existing reference enum names,not inferred from numeric equality with Creature.AttackType.'),
        resistance_observation=dict(raw=0xE0C,loaded=0x61C,count=0x78A,
            values=[0,100,255],types=[18,11],
            result='All four pairs/count load correctly; only8loader count reads,none of the pairs/count in selected damage/HP paths.',
            type_scope='18/11 are chosen candidate Knives/HailStorm IDs; universal CreatureAttackType-to-Shot mapping is not established. Non-reading is observed regardless of that mapping.',
            bounds='Doesnot prove all spells/versions lack resistance. Poison/status persistence,otherDamageTypes and specialProcessTypes remain open.'),
        verification=dict(full_context_comparisons=18,measured_output_baselines=3,
            baseline_names=['hit-res0','range-res0','boulder-fire'],
            opaque_scope='Baselines measured from original CPU output; variants independently predict changed raw/mid/loaded/HP/map bytes. Not independent decoding of every opaque scene byte.',
            Ghidra_instruction_comparisons=6,inherited_Ghidra_byte_ranges=10,additional_PE_ranges=7,
            independent_Ghidra_CPU_controls=0,
            pending_control_names=['hit-res100','area-res100','range-half','boulder-fire','boulder-projectile'],
            pending_reason='Automatic approval review couldnotcomplete because of usage limit; request wasnotexecuted. No workaround dispatch.',
            original_code='No original PE instruction or native Creature/World/Shot vtable replacement; Shotvtable also checked whole and watched forwrites.'),
        corpus=dict(evidence=link('phase114-shot-damage-api-validation.json'),files=82,records=api['identity_records'],
            exact_rebuilds=410,changed=328,identity=82,refusals=8,source_writes=0),
        helpers=[link(n) for n in ['probe_shot_hit.py','emulate_shot_damage.py','crosscheck_shot_damage.py',
            'native_shot_damage.py','validate_shot_damage_api.py','prepare_shot_damage_primary.py']],
        dependencies=[link(n) for n in ['native_unicorn_factorytrace.py','probe_creature_resistance.py','emulate_resistance.py',
            'emulate_melee_attack.py','ghidra/phase89-object-factory-base.bin','creature-resistance-semantics.json']],
        unchanged_source_executables=source_executables(),
        limitations=['Exact supplied DX only; no live game,GUI,renderer,audio or original editor save path executed.',
            'Prepared World/instances with actual native class snapshots and declared null graphics/sound IO; actual projectile flight/collision scheduler not executed.',
            'Single target and selected center/offset AoE; no multi-target traversal,all geometry edges or all arithmetic extremes certified.',
            'CreatureAttackType and ShotAttackCategory are different enum spaces; do not compare their raw IDs as equivalent.',
            'No general percentage formula for resistance established; no game patch implemented.',
            'No independent Ghidra CPU replay in this phase; five prepared controls await review availability.',
            'OtherDamageTypes,Lightning/Inferno/poison status ticks and remaining unknown fields across15families remain unresolved.'])


if __name__=='__main__':
    s=build();(ROOT/'shot-damage-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print('phase114:',len(s['native_replays']),'native chains; independent Ghidra CPU controls0; rawShot corpus',s['corpus']['records'])
