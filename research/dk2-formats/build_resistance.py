"""Publish resistance storage and exact matched melee nonapplication evidence."""
import hashlib
import json
from pathlib import Path
import emulate_resistance as native
import crosscheck_resistance as cross
from build_effect_semantics import EXE,pe_read
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))


def build():
    data=EXE.read_bytes();primary={};visits=set();replays=[]
    manifest=load('phase113-resistance-emulations.json');assert manifest['scope']==native.__doc__
    assert [r['value'] for r in manifest['replays']]==native.VALUES
    for row in manifest['replays']:
        args,e,checks=native.probe.build(row['value']);wanted=native.expected(row['value'])
        p=native.path_for(args);assert p.name==row['file'] and digest(p.read_bytes())==row['sha256']
        value=load('ghidra/'+p.name);assert value['input']==args and value['expected_attack']==e
        native.verify(row['value'],args,checks,wanted,value['result']);visits.update(value['result']['visited_instructions'])
        replays.append(dict(row,instruction_count=value['result']['instruction_count'],baseline_measured=row['value']==0,
                            scope='Full rawtarget loader then whole498E30/4B2CE0; attacker prepared; matched type1; default native null graphics.'))
    for k,rows in load('phase113-primary-manifest.json').items():
        primary[k]=[]
        for r in rows:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text(encoding='utf8'));b=bytes(v['data'])
            assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address']
            assert b==pe_read(data,r['address'],r['length'])
            if r.get('executed'):assert hex(r['address']) in visits
            primary[k].append(dict(r,evidence_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    assert len(primary['functions'])==24 and sum(r['executed'] for r in primary['functions'])==15
    controls=load('phase113-resistance-crosschecks.json')['replays'];assert [r['value'] for r in controls]==cross.CONTROLS
    for row in controls:
        v=load('ghidra/'+row['file']);args,e=cross.build(row['value']);assert v['input']==args and v['expected']==e
        assert not v['tool_is_error'] and v['result']['success'] and v['result']['hit_return']
        assert all(int(v['result']['registers'][k],16)==n for k,n in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert digest((ROOT/'ghidra'/row['file']).read_bytes())==row['sha256']
    api=load('phase113-resistance-api-validation.json')
    for r in api['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
    review=load('phase113-resistance-consumer-review.json');assert len(review['leads'])==20
    frozen=load('vault-map-semantics.json')
    for r in frozen['helpers']+frozen['dependencies']:assert digest((ROOT/r['file']).read_bytes())==r['sha256']
    prior=ROOT/'phase113-prior-components.json'
    if not prior.exists():
        rows=load('editor-format-package.json')['components'];assert len(rows)==91
        prior.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==91
    return dict(phase=113,date='2026-10-29',family='Creature four resistance slots; raw loading and matched melee nonapplication',
        profile=dict(path=str(EXE),sha256=digest(data)),primary=primary,native_replays=replays,Ghidra_controls=controls,
        physical=dict(raw_offset=0xE0C,slots=4,stride=2,fields=['AttackType:u8','ValueBits:u8'],record_sizes=[5449,5537],
            intermediate_offset=0x11FC,intermediate_presence_offset=0x284,loaded_offset=0x61C,loaded_count_offset=0x78A,
            loaded_count=4,preserved_value_range=[0,255],percentage_formula_established=False),
        load_chain=dict(functions=[0x491DC0,0x568820,0x568E30,0x492320],presence_getter=0x569D70,type_getter=0x569D90,value_getter=0x569DB0,
            result='All4type/value pairs arrive unchanged in definition61C..623 with count4. Ability to read these file bytes confirmed.'),
        melee=dict(function=0x498E30,HP_handler=0x4B2CE0,target_species=8,attacker_runtime_type_offset=0x77A,
            attacker_raw_type_lineage='Creature rawF50 -> mid1340 -> definition77A; matching runtime type1 explicitly prepared.',
            resistance_types=[1,1,1,1],values_tested=[0,100,255],HP_before=1000,HP_after=[980,980,980],
            target_definition_reads='Resistance count read only in loader4936B4/4936C8; neither loaded pairs nor count read by selected attack/HP path.',
            result='Table is loaded but not applied in this exact matched-type melee path.',
            inference='Suggests an unconnected/unused resistance mechanism in supplied DX; not proof against all spell paths or other versions.'),
        direct_consumer_review=link('phase113-resistance-consumer-review.json'),
        verification=dict(baseline='Native value0 output measured as complete baseline; not independent decoding of every2199byte.',
            paired_expectations='Only corresponding raw/mid/definition value bytes differ for100/255; all other prepared bytes agree with baseline.',
            independent_checks='Matching type1,all4loadedpairs/count,HP1000->980,oneoriginalHPstore,all6beforecalls,originalentries/FS0/protectedcode checked.',
            Ghidra='Two independent CPU controls compare all prepared contexts for0/100; not proof of gameplay intent.'),
        helpers=[link(n) for n in ['native_creature_resistance.py','probe_creature_resistance.py','emulate_resistance.py',
                                   'crosscheck_resistance.py','scan_resistance_consumers.py','validate_resistance_api.py','prepare_resistance_primary.py']],
        dependencies=[link(n) for n in ['native_unicorn_factorytrace.py','emulate_melee_attack.py','creature-subrecords.json',
            'creature-definition-semantics.json','melee-attack-semantics.json','ghidra/phase90-creature-factory-base.bin']],
        external_IO=dict(gameplay_replacements=0,gameplay_class_vtable_replacements=0,
            provided='Null graphics resource loader21bytes/null sound loader11bytes; inherited null query11/fixed sound14/session-event34 IO captures.',
            original='CPCEngineInterface slot90=4430D0 executes its original null effect request; original game/RTTI/native class methods retained.',
            graphics='Absent-model render fallback; no live image or audio.',
            checksum='Prepared WorldA3C7=1 disables checksum refresh; unchanged inputs except resistance bytes are compared.'),
        corpus=dict(evidence=link('phase113-resistance-api-validation.json'),files=82,records=3734,
                    exact_container_rebuilds=api['exact_container_rebuilds'],changed=api['changed_containers'],
                    identity=api['identity_containers'],refusals=api['API_refusals']),
        unchanged_source_executables=source_executables(),
        counts=dict(native_raw_loader_attack_chains=3,native_damage_calls=3,Ghidra_controls=2,
                    primary_functions=24,executed_path_functions=15,primary_blocks=2,classified_direct_leads=20,
                    exact_container_rebuilds=410,source_data_writes=0),
        limitations=['Only supplied exact DX. Original1.3/1.7/Flame behavior not universally established.',
                     'TargetVampire rawdefinition loads fully; attacker/world/instances prepared. No fullgame orGUI session.',
                     'Matched melee1 and rawvalues0/100/255; not all attacks/spells/everyotherstate.',
                     'Direct linear candidate scan cannot exclude adjusted/indexed pointer uses,copies,othermodules.',
                     'No universal percentage formula asserted; ValueBits preserve u8,including unknown inputs.',
                     'Separate Lightning immunity flag1545/mask1 is not this table and remains independently documented.',
                     'WorkEnhancement followup queries are WIP,not evidence of research/manufacture output.',
                     'Substantial unknowns in all15family scope remain; goal incomplete.'])


def main():
    s=build();(ROOT/'creature-resistance-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print('resistance schema:',s['counts'])


if __name__=='__main__':main()
