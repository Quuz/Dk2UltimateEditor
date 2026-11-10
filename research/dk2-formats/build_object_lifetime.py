"""Publish fullObject tick lifetime semantics without closing unknownManaGen."""
from pathlib import Path
import hashlib
import json
import struct
import emulate_object_lifetime as native
import crosscheck_object_lifetime as cross
from build_effect_semantics import EXE, pe_read
from build_possession_damage import source_executables

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT/name).read_bytes()))


def primary():
    data, result = EXE.read_bytes(), {}
    for key, rows in load('phase108-primary-manifest.json').items():
        result[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == row['length'] == value['length']
            assert blob == pe_read(data, row['address'], row['length'])
            result[key].append(dict(row, evidence_sha256=digest(path.read_bytes()), code_or_data_sha256=digest(blob)))
    assert len(result['functions']) == 7 and sum(r['executed'] for r in result['functions']) == 6
    assert len(result['blocks']) == 3
    assert pe_read(data, 0x66D654+0x14, 4) == struct.pack('<I', 0x49BD20)
    assert b'RoomId' in pe_read(data, 0x69D714, 16) and b'InHand' in pe_read(data, 0x69D5DC, 16)
    return result


def replays():
    cases = {c['name']: c for c in native.cases()}
    manifest = load('phase108-object-lifetime-emulations.json')
    assert manifest['scope'] == native.__doc__
    output, visits = [], set()
    for row in manifest['replays']:
        value = load('ghidra/'+row['file'])
        c = cases[row['case']]
        args, expected, checks, wanted = native.build(c)
        assert value['case'] == c and value['input'] == args and value['expected'] == expected
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        native.verify(c, args, expected, checks, wanted, value['result'])
        visits.update(value['result']['visited_instructions'])
        output.append(dict(row, inputs=c, expected=expected, instruction_count=value['result']['instruction_count'],
                           observed_definition_reads=value['result']['observed_reads'],
                           compared_contexts=len(checks), compared_bytes=sum(map(len, checks.values()))))
    assert len(output) == len(cases) == 19
    return output, visits


def build():
    p = primary()
    rows, visits = replays()
    assert all(hex(r['address']) in visits for r in p['functions'] if r['executed'])
    prior = ROOT/'phase108-prior-components.json'
    if not prior.exists():
        parts = load('editor-format-package.json')['components']
        assert len(parts) == 86
        prior.write_text(json.dumps(parts, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 86
    api = load('phase108-object-lifetime-api-validation.json')
    assert api['source_files'] == 82 and api['source_record_identity_views'] == 11756
    assert api['exact_container_rebuilds'] == 574 and api['changed_containers'] == 492 and api['identity_containers'] == 82
    for row in api['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    controls = load('phase108-object-crosschecks.json')['replays']
    assert len(controls) == 2
    for row in controls:
        value = load('ghidra/'+row['file'])
        args, expected = cross.build(row['case'])
        assert value['input'] == args and value['expected'] == expected
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        r = value['result']
        assert r['success'] and r['hit_return']
        assert all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
    # Memory-read instrumentation is a derivative; published backend107 intact.
    old = next(r for r in load('crypt-tick-semantics.json')['helpers'] if r['file'] == 'native_unicorn_tick.py')
    assert digest((ROOT/old['file']).read_bytes()) == old['sha256']
    diagnostic = load('ghidra/phase108-creature-unknown-field-reads.json')
    assert diagnostic['result']['success'] and diagnostic['result']['hit_return']
    assert not diagnostic['result']['observed_reads']
    flags = load('object-property-semantics.json')['flags']
    assert flags['raw_offset'] == 0x344 and flags['runtime_offset'] == 0xF4
    return dict(phase=108, date='2026-10-29', family='Objects runtime lifetime/Flags/Hp',
        profile=dict(file=str(EXE), sha256=digest(EXE.read_bytes())), primary=p,
        native_replays=rows, Ghidra_crosschecks=controls, corpus=api,
        unchanged_source_executables=source_executables(),
        helpers=[link(n) for n in ['native_object_lifetime.py', 'emulate_object_lifetime.py',
                                   'validate_object_lifetime_api.py', 'native_unicorn_fieldtrace.py',
                                   'probe_object_tick.py', 'crosscheck_object_lifetime.py', 'prepare_object_lifetime_primary.py']],
        dependencies=[link(n) for n in ['object-property-semantics.json', 'object-loading-semantics.json',
                                       'crypt-tick-semantics.json', 'native_unicorn_tick.py',
                                       'ghidra/phase89-object-factory-base.bin', 'probe_creature_tick.py']],
        storage=[dict(raw_offset=0x344, bytes=4, runtime_definition_offset=0xF4, meaning='Flags;new APIchangesmask3only'),
                 dict(raw_offset=0x348, bytes=2, runtime_definition_offset=0x104, instance_offset=0x40,
                      meaning='UnsignedrawHP;ordinary runtimeHP is signedDWORDandalsodecaycounterwhenenabled')],
        flags=[dict(mask=1, name='DIE_OVER_TIME', native_effect='DecrementpositiveinstanceHPby1perObjecttick,exceptInHand.'),
               dict(mask=2, name='DIE_OVER_TIME_IF_NOT_IN_ROOM', native_effect='DecrementpositiveHPonlywhenRoomIdWORD138==0,exceptInHand.')],
        formula='If not(actorBYTE146&10h) and(flags&1 or(flags&2 andactorWORD138==0)) andsignedHP>0:HP-=1. '
                'Mask1always-decaytakespriorityoverconditionalmask2;positiveHPneverdecaysbelow0.',
        runtime_fields=dict(HP_DWORD=0x40, RoomId_WORD=0x138, InHand_flags_DWORD=0x146, InHand_mask=0x10,
                            native_diagnostic=0x49D310, observed_renderinfo_counter_WORD=0x6A,
                            scope='InHand is runtime Objectstate;not rawFlags344/bit10.'),
        HP_damage_interaction=dict(native_damage=0x4B2CE0, damageable_mask=0x200000,
            examples={'hp10_damage5_decay': 4, 'hp10_damage5_withoutDamageable_thenDecay': 9,
                      'hp10_damage5_withoutDecay': 5},
            source_owner='WORD36 written7evenwhendamageisrefused;noCreaturetimestamps forordinaryObject.'),
        lifecycle=dict(ticks='Repeatedcomplete49BD20 calls inoneCPU memory;hp3→2→1→0→0→0.',
            registration='Existence/pointer/owner/map tables remainunchanged evenHP0 afterthismethod.',
            limitation='ActualWorld scheduling/removal/destruction andDeathEffect/particle paths notexecuted. '
                       'Do not infer persistentimmortalityormemoryleak;laterconsumer separate.',
            time_units='ExactlyoneHPperObjecttick here;do not labelrawHPseconds without schedulerfrequencyproof.'),
        unknown_mana_gen=dict(raw_offset=0x34C, runtime_definition_offset=0x106, meaning_resolved=False,
            observed='WholeObjectdefinition285bytes traced;nooverlapping106/107reads in19accepted ordinaryState0 scenarios, '
                     'includingvalues0/100/FFFFh. Otherstates/tickconsumersnotruledout.',
            raw_preserved=True),
        creature_unknown_read_probe=dict(evidence=link('ghidra/phase108-creature-unknown-field-reads.json'),
            watched=diagnostic['fields'], observed_reads=0,
            scope='OnepreviouspreparedCreaturetick executeswithnewreadobservations;no reads ofsixwatchedfields. '
                  'Not a globalnonuse proof,newsemanticassignment ornewCreaturefixturecoverage.'),
        API=dict(file='native_object_lifetime.py', functions=['patch_decay_flags(raw,mode0..3)', 'patch_hp(raw,unsigned16)'],
                 preserve='Allotherflagbits includingDAMAGEABLE,typebitsandunsupportedbits;unknownManaGen andallotherbytes/headers/order retained.',
                 record_bytes=894),
        counts=dict(native_object_cases=19, complete_Object_tick_calls=sum(c['ticks'] for c in native.cases()),
                    damage_then_tick_cases=sum('damage' in c for c in native.cases()), Ghidra_crosschecks=2,
                    whole_code_spans=7, executed_path_spans=6, PE_blocks=3,
                    source_files=82, source_record_identity_views=11756,
                    exact_container_rebuilds=574, changed_containers=492, identity_containers=82, API_refusals=6,
                    unknown_semantic_fields_closed=0),
        limitations=['ExactDX only;otherversions notnewlyexecuted.',
                     'PreparedactualObjectclasssnapshot/definition/world,no newrawObjectloader/factorycomposition.',
                     'ObjectState0,nomodel,activeaudiohandles0,noDeathEffect/particleattachment,stationaryposition.',
                     'RoomId0/200 preparedwithoutnewroomconstruction;conditionisactualWORDcheck,notroomgeometricalrevalidation.',
                     'NativeObjecttick(s) complete;World scheduler/deferreddelete andeffectcreation remainopen.',
                     'NoObject106semanticnameassigned,noGlobalManaeffectfound,noglobalunusedclaim.',
                     'CreatureJump/Priority/otherwatched fields unobservedinonefixture only;notclosed.',
                     'Significantunknownsall15families remain;thisisconfirmedconsumer/APIprogress,not completegoal.'])


def main():
    schema = build()
    (ROOT/'object-lifetime-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
