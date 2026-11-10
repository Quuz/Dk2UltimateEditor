"""Publish supplied-DX Object StartState and bounded periodic-effect consumers."""
import hashlib
import json
import struct
from pathlib import Path
import emulate_object_states as native
import crosscheck_object_states as cross
from object_state_tables import BASE, CALLBACK_BASE, SIZE, COUNT, initialized_table
from build_effect_semantics import EXE, pe_read
from build_possession_damage import source_executables

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(blob):
    return hashlib.sha256(blob).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT/name).read_bytes()))


def primary():
    out, data = {}, EXE.read_bytes()
    for key, rows in load('phase110-primary-manifest.json').items():
        out[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            out[key].append(dict(row, evidence_sha256=digest(path.read_bytes()), program_bytes_sha256=digest(blob)))
    assert len(out['functions']) == 19 and sum(r['executed'] for r in out['functions']) == 13
    assert len(out['blocks']) == 2
    return out


def replay_evidence():
    cases = {c['name']: c for c in native.cases()}
    manifest = load('phase110-object-state-emulations.json')
    assert manifest['scope'] == native.__doc__ and len(manifest['replays']) == len(cases) == 26
    rows, visits = [], set()
    for row in manifest['replays']:
        value = load('ghidra/'+row['file'])
        c = cases[row['case']]
        args, expected, checks, wanted = native.build(c)
        assert value['case'] == c and value['input'] == args and value['expected'] == expected
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        native.verify(c, args, expected, checks, wanted, value['result'])
        result = value['result']
        visits.update(result['visited_instructions'])
        reads = [r for r in result['observed_reads'] if native.f.DEFINITION <= int(r['address'], 16) < native.f.DEFINITION+285]
        rows.append(dict(row, inputs=c, expected=expected, observed_definition_reads=reads,
                         instruction_count=result['instruction_count'], expected_null_target_rejection=c['fault'],
                         compared_contexts=len(wanted), compared_bytes=sum(map(len, wanted.values()))))
    controls = load('phase110-object-state-crosschecks.json')['replays']
    assert [r['case'] for r in controls] == cross.CONTROL_CASES
    for row in controls:
        value = load('ghidra/'+row['file'])
        args, expected = cross.build(row['case'])
        assert value['input'] == args and value['expected'] == expected
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        r = value['result']
        assert not value['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
    return rows, visits, controls


def build():
    p = primary()
    rows, visits, controls = replay_evidence()
    assert all(hex(r['address']) in visits for r in p['functions'] if r['executed'])
    table = initialized_table()
    baseline = load('enums.json')['GameObject.State']
    registry = []
    for i in range(COUNT):
        tag, callback, a, b, c = struct.unpack_from('<5I', table, i*20)
        registry.append(dict(index=i, ordinal_tag=tag, callback=callback,
                             other_words=[a, b, c], baseline_name=baseline.get(str(i)),
                             name_authority='baseline display name; native initializer proves binding, not all handler behavior'))
    api = load('phase110-object-state-api-validation.json')
    assert api['source_files'] == 82 and api['record_identity_views'] == 11756
    assert api['exact_container_rebuilds'] == 738 and api['changed_containers'] == 738
    for row in api['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    source_samples = []
    for source_id in [17, 101, 108]:
        raw = native.source_record(source_id)
        source_samples.append(dict(id=source_id, name=raw[:32].split(b'\0')[0].decode('ascii'),
                                   start_state=raw[0x35B], misc_effect_id=int.from_bytes(raw[0x358:0x35A], 'little'),
                                   mana_gen_bits=int.from_bytes(raw[0x34C:0x34E], 'little')))
    assert [(r['id'], r['start_state']) for r in source_samples] == [(17, 0), (101, 0), (108, 0)]
    frozen = next(r for r in load('object-lifetime-semantics.json')['helpers'] if r['file'] == 'native_unicorn_fieldtrace.py')
    assert digest((ROOT/frozen['file']).read_bytes()) == frozen['sha256']
    prior = ROOT/'phase110-prior-components.json'
    if not prior.exists():
        parts = load('editor-format-package.json')['components']
        assert len(parts) == 88
        prior.write_text(json.dumps(parts, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 88
    return dict(
        phase=110, date='2026-10-29', family='Objects StartState and periodic effect requests',
        profile=dict(file=str(EXE), sha256=digest(EXE.read_bytes())), primary=p,
        native_replays=rows, Ghidra_crosschecks=controls,
        initializer_replay=link('ghidra/phase110-object-state-initializer-v2.json'),
        helpers=[link(n) for n in ['object_state_tables.py', 'native_object_states.py', 'emulate_object_states.py',
                                   'crosscheck_object_states.py', 'prepare_object_states_primary.py', 'validate_object_states_api.py']],
        dependencies=[link(n) for n in ['native_unicorn_fieldtrace.py', 'probe_object_tick.py', 'native_object_loading.py',
                                       'object-loading-semantics.json', 'object-lifetime-semantics.json', 'enums.json']],
        unchanged_source_executables=source_executables(),
        raw_field=dict(offset=0x35B, bytes=1, kind='u8', intermediate_offset=0x3DB,
                       runtime_definition_offset=0x11B, instance_active_state=0x13A, instance_state_age=0x13E,
                       native_chain=[0x56D4F0, 0x56D620, 0x49DD60, 0x49BD20, 0x49EED0]),
        registry=dict(initializer=0x49E600, record_base=BASE, callback_base=CALLBACK_BASE, stride=20,
                      initialized_indices=[0, 58], record_count=COUNT, bytes=SIZE, entries=registry,
                      first_ordinal='Initial PE/BSS zero; initializer writes bytes4..1179 and preserves first DWORD.',
                      descriptor_words='Index-like ordinal and callback binding established; roles of other metadata words not inferred.',
                      zero_callback_states=[2, 7, 57, 58], persistent_trivial_callback_states=[16, 22, 51, 56]),
        dispatch=dict(
            inactive='If active state0 and definition StartState nonzero, copy it, zero age, return1 without invoking callback in that update.',
            inactive_zero='If both active and definition state0, leave existing age unchanged and return1.',
            active='Increment DWORD age modulo2^32, invoke callback at6E4B3C+20*active; callback0 result clears active state only, leaving incremented age.',
            no_index_validation=True,
            expanded_baseline_enum=dict(values=list(range(59, 64)), native_initialization='No entries initialized by this exact routine; baseline enum alone is not native support.',
                                        tested_values=[59, 63, 255], outcome='First update copies value; second attempts callback address0 in prepared PE/BSS state.',
                                        observation='Protected backend rejects execution at0; no Windows crash/live game observed. Other runtime tables or patched versions not excluded.'),
            diagnostic_policy='Keep original bytes; report exact-profile registry discrepancy separately. Low-level patch API accepts allu8 values.'),
        periodic_effects=dict(
            state39=dict(baseline_name='MANA_VAULT', callback=0x4A3400, effect_id=288,
                         trigger='After dispatcher age increment, unsignedage modulo low32(3*TPS) equals0.',
                         checked_TPS=20, checked_age_transition=[59, 60], nonperiod_transition=[58, 59]),
            state40=dict(baseline_name='TORTURE_COALS', callback=0x4A3450,
                         effect_source=dict(raw_offset=0x358, raw_bytes=2, runtime_offset=0x117),
                         trigger='Same period; request only if loaded WORD MiscEffectId is nonzero.', tested_effect_id=123),
            requests='Original World509580->50E490->original CPCEngineInterface4430D0; six argument values observed in original bridge stack reads.',
            positions='Start/end pointers both address instanceXYZ+1A; owner low16 is7 in checked fixtures.',
            graphics='Prepared interface slot90 equals original PE670604 value4430D0, which returns0. No injected provider code and no actual effect/renderer created.',
            source_samples=source_samples,
            activation='Active39/40 supplied by controlled caller; source ManaVault101/ClaimedManaVault108 default StartState0. Automatic activation/claiming and complete factory for these types not executed.'),
        unknown_mana_gen=dict(raw_offset=0x34C, runtime_definition_offset=0x106,
                              tested_raw_values=[0, 100, 65535], observed_reads=0, meaning_resolved=False,
                              conclusion='No reads in selected original loading/state/tick paths including periodic39; not a global unused claim.'),
        mana_writer_review=dict(
            candidate_source=link('phase23-player-mana-candidates.json'),
            fresh_static_receivers=[dict(address=a, evidence=link(f'ghidra/phase110-mana-{a:x}-force_decompile.txt'))
                                    for a in [0x4B9250, 0x4B8640, 0x426070, 0x568E30, 0x4C1AE0, 0x4C1B20]],
            conclusion='UI reset/Creature conversion share displacements; Player reset clears accumulators; helpers1AE0/1B20 use external definition5C and Player10D2/10E2, not Object106. No new Object ManaGen consumer established.',
            limits='Selected existing mapped candidates only; no exhaustive alias/indexed/global nonuse proof.'),
        corpus=api,
        counts=dict(native_compositions=26, normally_returning_compositions=23, expected_null_target_rejections=3,
                    Ghidra_compositions=3, native_Object_tick_entries=sum(c['ticks'] for c in native.cases()),
                    native_Object_ticks_returned=sum(c['ticks']-int(c['fault']) for c in native.cases()),
                    native_raw_Object_load_chains=26, standalone_registry_initializers=1,
                    registry_entries=59, selected_whole_primary_functions=19, executed_primary_functions=13,
                    exact_container_rebuilds=738, source_record_identity_views=11756, source_Object_files_unchanged=82,
                    newly_named_unknown_raw_fields=0),
        limitations=[
            'Exact supplied unpatched DX; no native state-table compatibility claim for old/RC3/Flame runtime patches.',
            '26 prepared raw-loader/instance/tick compositions:23normal returns,3deliberate null-target rejections; not26 successful game updates.',
            'All285definition/1022intermediate/894raw bytes and complete actor649 plus other fixture regions compared; selected branch coverage only.',
            'Loading gate1 skips graphical resources/sound/checksum refresh; existing scalar loader executes unchanged.',
            'Full original tick on prepared existing object, not complete Objectfactory or World scheduler/startup.',
            'Two typed ManaVault definitions loaded; active39 controlled, not proof of natural vault activation/claim reward.',
            'Effect request reaches original null graphics implementation; no visible particles, actual renderer or effect lifetime proved.',
            'Initial misaligned table read and missing slot90 diagnostic retained as excluded discovery artifacts, not accepted game failures.',
            'ManaGen, Terrain198/224, Trap204, Door236, Jump/Priority/113/114 and other significant unknowns remain unresolved; goal incomplete.',
        ])


def main():
    schema = build()
    (ROOT/'object-state-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
