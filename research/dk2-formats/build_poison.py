"""Correct PoisonSpit parameter inversion using original stack and tick evidence."""
import copy
import hashlib
import json
from pathlib import Path
import emulate_poison as native
import prepare_poison_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))


def corrected_process():
    baseline=load('shot-semantics.json')['processes'][38]
    assert baseline['process_type']==38 and baseline['name']=='POISON_SPIT'
    return dict(baseline,
        data1='Poison duration: low u16 update count; stored directly in Creature45D without multiplication by TPS.',
        data2='Poison damage magnitude per eligible Creature update: low u16; stored in Creature45F.',
        note='Corrected phase115:4B0270 pushes Data1 first and Data2 second;48CC50 reads ESP+8 into timer45D,ESP+4 into severity45F. Whole native cast/collision/tick confirms50/5 -> timer50,damage5.',
        evidence='phase115-primary-manifest.json;phase115-poison-emulations.json',
        supersedes_phase='Initial process38 parameter meanings in shot-semantics.json were reversed; retain that source solely as an auditable historical component.')


def effective_shot_parameters():
    s=copy.deepcopy(load('shot-semantics.json'));s['processes'][38]=corrected_process();return s


def build():
    v=load('phase115-poison-emulations.json');assert v['scope']==native.__doc__
    cases=native.cases();assert len(v['replays'])==len(cases)==16;posts={}
    for c,row in zip(cases,v['replays']):
        args,checks=native.probe.build(**native.inputs(c));p=native.path_for(args);r=load('ghidra/'+p.name)
        assert row['case']==c['name'] and row['file']==p.name and row['sha256']==digest(p.read_bytes())
        assert r['case']==c and r['input']==args
        posts[c['name']]=native.verify(c,args,checks,r['result'])
    for name in v['paired_full_context_cases']:
        c=next(c for c in cases if c['name']==name)
        assert native.paired_expected(c,posts['tick1'])==posts[name]
    assert load('phase115-primary-manifest.json')==primary.build()
    corpus=load('phase115-poison-api-validation.json')
    for row in corpus['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    for name in ['shot-damage-semantics.json','crypt-tick-semantics.json']:
        frozen=load(name)
        for row in frozen['helpers']+frozen.get('dependencies',[]):assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase115-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==93
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==93
    totals={k:sum(row['outcome'][k] for row in v['replays']) for k in ['ticks','HP_calls','applications']}
    return dict(phase=115,date='2026-10-29',families=['Shots','CreatureSpells','Creatures'],
        executable=primary.build()['executable'],executable_sha256=primary.build()['sha256'],
        correction=dict(component='shot_parameters',json_pointer='/processes/38',
            before=dict(data1='severity',data2='timer'),after=dict(data1='duration updates low16',data2='damage/update low16'),
            cause='Earlier report assigned names to call arguments without checking their stack order and callee stores.',
            confirmed=True,original_editor_bug_established=False),
        corrected_process38=corrected_process(),
        package_contract='Use top-level effective_shot_parameters for Shot process UI. It derives from historical shot_parameters and applies this exact replacement to process38; all other process rows remain byte-equivalent JSON values.',
        primary_evidence=link('phase115-primary-manifest.json'),native_replays=v['replays'],
        load_cast_collision=dict(raw_creature_source='Vampire8 target with explicit resistance type41/value0,100,255.',
            raw_shot_source='Maiden Poison Spit62,239bytes,DamageType0,ProcessType38,category8.',
            raw_creature_spell_source='Maiden Poison Spit41,266bytes,ShotID62; complete346byte intermediate and70byte definition verified.',
            loaders=[0x491DC0,0x568820,0x568E30,0x492320,0x4ACD60,0x56E540,0x56E690,0x4ACDF0,0x56A490,0x56A560,0x4F7860],
            cast=0x4984D0,factory=[0x4B7CD0,0x4AAC90],collision=0x4ABD60,dispatch=0x4AD730,poison_callback=0x4B0270,setter=0x48CC50,
            collision_tables_initializer=0x4ABCA0,process_tables_initializer=0x4AD1B0,
            identity='Native Creature cast sets Shot145=caster100; actual cell/bounds/enemy collision selects target101 and setsShot147. No synthetic hit-tag setter.',
            snapshot='Prepared caster/target overlap in one valid cell; actual original Creature/Object/World/Shot vtables,normal factory pool/tag/map registration.',
            allocation_scope='On hit ShotHP becomes0 but tag remains alive; scheduler deletion and physical flight are not executed.'),
        parameters=dict(duration=dict(Shot_raw=0x98,CreatureSpell_raw=0xC8,CreatureSpell_definition=0x1F,
                                      Shot_instance=0x13D,Creature_state=0x45D,runtime_width=2,unit='eligible update calls remaining',TPS_multiplication=False),
            damage=dict(Shot_raw=0x9C,CreatureSpell_raw=0xCC,CreatureSpell_definition=0x23,
                        Shot_instance=0x141,Creature_state=0x45F,runtime_width=2,unit='requested HP damage per eligible update'),
            override='Creature wrapper copies loaded CreatureSpellData1/2 over Shotdefaults. SourceShotdefaults123/9 stillproduce poison50/5 whenCreatureSpell50/5.',
            full_raw_width=4,serialization='Preserve all32rawbits; low16 runtime narrowing does not authorize deleting upper16 file bits.'),
        delayed_state=dict(timer_offset=0x45D,damage_offset=0x45F,player_offset=0x461,
            source='Only player owner7 explicitly stored by4B02CD; this six-byte state has no ShotID,SpellID orAttackCategory.',
            update=0x48E160,enclosing_tick=0x488930,HP_handler=0x4B2CE0,
            behavior='When timer nonzero and freezeTimer447 zero: decrement timer once,request -unsigned16severity with savedplayer andcontrol0. A timer1 stilldealsdamage and becomes0.',
            no_global_claim='No proof against source recovery through all possible external/global bookkeeping; the six bytes themselves do not distinguish attacks.'),
        tested=dict(base='Data1=50,Data2=5 -> no immediate HP loss; after one full updateHP995/timer49; aftertwoHP990/timer48.',
            narrowing='Data1=10002h,Data2=10005h -> timer2,severity5; afteroneHP995/timer1.',
            zero_duration='Timer0 disables poison request; severity remains5.',
            zero_severity='Timer still decrements; commonHP handler called withdelta0,HP unchanged.',
            freeze='FreezeTimer1 pauses both poison timer and damage for firstupdate; secondupdate resumesdamage5.',
            invulnerability='Prepared InvulnerableTimer445=2 does not prevent selected poison:HP995,timer445=1 afteroneupdate.',
            armour='ArmourTimer443=2 decrements to1 before HP calculation; incoming5 becomes2 with integer truncation.',
            reapplication='Additional direct original process-dispatch callback afterfirstupdate overwrites49->50; nextupdate49,HP990. This is not a second physical collision/new projectile.',
            resistance='Type41 fourpairs with values0/100/255 allgiveHP995. Count78A reads onlywithinrawloader; no pairs/count reads incast/collision/status/HP selected paths.'),
        verification=dict(native_scenarios=16,full_Creature_updates=totals['ticks'],HP_handler_calls=totals['HP_calls'],
            poison_applications=totals['applications'],HP_stores=sum(len(r['outcome']['HP_writes']) for r in v['replays']),
            complete_context_pairs=v['paired_full_context_cases'],
            independently_checked='Entire346byte CreatureSpell intermediate and70byte definition,loaded parameters,cast/collision identity,state,timer/HP stores,original entry counts,FS-only zero-page accesses andShotvtable.',
            new_original_PE_ranges=11,inherited_Ghidra_byte_ranges=12,independent_Ghidra_CPU_controls=0,
            pending_review='Prior automatic approval review failed because usage limit; no Ghidra request dispatched this phase,no bypass.',
            clock='Prepared World tick counter fixed; consecutive explicit Creature update calls,not advancing realWorld scheduler or measured wall time.'),
        corpus=dict(evidence=link('phase115-poison-api-validation.json'),unique_sources=83,family_source_checks=164,
            exact_rebuilds=820,changed=656,identity=164,identity_records=9086,high_bits_preserved=True,source_writes=0),
        pending_independent_controls=link('phase115-pending-crosschecks.json'),
        helpers=[link(n) for n in ['probe_poison.py','emulate_poison.py','validate_poison_parameters.py','prepare_poison_primary.py','crosscheck_poison.py']],
        dependencies=[link(n) for n in ['probe_shot_hit.py','probe_creature_resistance.py','probe_creature_tick.py',
            'native_unicorn_factorytrace.py','native_spell_alternatives.py','native_spell_writer.py','native_shot_damage.py',
            'shot-semantics.json','shot-damage-semantics.json','crypt-tick-semantics.json']],
        unchanged_source_executables=source_executables(),
        limitations=['Exact supplied DX; no livegame,renderer,audio or original editor GUI commit/save executed.',
            'Actual native cast and overlap collision in prepared scene; no flight or World scheduling/deletion,AI recipient alert list intentionally empty.',
            'Genuine game code/vtables retained; external absent-resource/audio providers explicit; sound provider returns0 to avoid a synthetic livehandle.',
            'RawCreature/rawShot/rawCreatureSpell load in one CPU; CreatureSpell70byte definition starts with resource pointers0 and nullGFX loader.',
            'Prepared cardinal trig/nav/state tables reused from verified initializers; no real trigtable/renderer initialization.',
            'Four whole-context pairs from measured baseline; remaining scenarios use independent semantic/state/store checks and full captures,not a full independent opaque-byte model.',
            'Delayed source cannot be identified from timer/severity/player triple alone; no exhaustive global source-tracking absence proof.',
            'No general resistance formula or mod implemented; otherDamageTypes/specialProcesses and major remaining15family unknowns stayopen.',
            'Independent Ghidra CPU controls0; previousphase114 five controls also remain pending.'])


if __name__=='__main__':
    s=build();(ROOT/'poison-process-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print(s['verification'])
