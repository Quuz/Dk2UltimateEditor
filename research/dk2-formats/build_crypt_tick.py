"""Publish full native tick, temporary Fairy speed andWizard WorkEnhancement."""
from pathlib import Path
import hashlib
import json
import struct
import emulate_crypt_tick as native
import crosscheck_creature_tick as cross
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
    data, output = EXE.read_bytes(), {}
    for key, rows in load('phase107-primary-manifest.json').items():
        output[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            output[key].append(dict(row, evidence_sha256=digest(path.read_bytes()), code_or_data_sha256=digest(blob)))
    assert len(output['functions']) == 20 and sum(r['executed'] for r in output['functions']) == 19
    assert len(output['blocks']) == 6
    assert b'WorkEnhancement' in pe_read(data, 0x69D734, 28)
    assert pe_read(data, 0x670574+0x80, 4) == struct.pack('<I', 0x59C140)
    return output


def initializers():
    out = []
    for name in ['phase107-initialize-47bbe0.json', 'phase107-initialize-4798f0-pe-seed.json',
                 'phase107-initialize-4950c0.json', 'phase107-initialize-navigation.json']:
        value = load('ghidra/'+name)
        r = value['result']
        assert r['success'] and r['hit_return']
        assert not r['protected_write_or_execution_failures']
        out.append(dict(**link('ghidra/'+name), function=value['input']['address'],
                        captured_bytes=r['capture_size'], code_visits=list(r['visited_instructions'])))
    return out


def replays():
    manifest = load('phase107-crypt-tick-emulations.json')
    assert manifest['scope'] == native.__doc__
    cases = {c['name']: c for c in native.cases()}
    result = []
    reference = {}
    for row in manifest['replays']:
        value = load('ghidra/'+row['file'])
        c = cases[row['case']]
        args, expected, checks = native.build(c)
        assert value['input'] == args and value['expected'] == expected and value['case'] == c
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        r = value['result']
        native.verify(c, r, expected)
        post = {a: bytearray(b) for a, b in checks.items()}
        for block in r['context_mismatches']:
            a = int(block['address'], 16)
            assert len(post[a]) == block['length']
            for d in block['differences']:
                assert post[a][d['offset']] == d['expected']
                post[a][d['offset']] = d['actual']
        assert post[native.cf.ACTOR].hex() == r['captured_hex']
        reference[c['name']] = (c, expected, checks, post, r)
        result.append(dict(row, inputs=c, expected=expected, instruction_count=r['instruction_count'],
                           observed_writes=r['observed_writes'], compared_contexts=len(checks),
                           compared_bytes=sum(map(len, checks.values()))))
    assert len(result) == len(cases) == 22
    # The complete baseline body is measured; all pair differences are predicted.
    # This does not pretend to independently name every side effect of a tick.
    for name, (c, expected, checks, post, _) in reference.items():
        control = {1000: 'base', 4000: 'base4000', 0: 'base-zero-speed'}[c['cache']]
        wanted_actor = bytearray(reference[control][3][native.cf.ACTOR])
        native.f.put(wanted_actor, 0x3B3, expected['work_enhancement'])
        assert post[native.cf.ACTOR] == wanted_actor, name
        allowed = {native.cf.ACTOR}
        wanted = {a: bytearray(b) for a, b in checks.items()}
        wanted[native.cf.ACTOR] = wanted_actor
        if 'crypt' in expected:
            owner = native.f.PLAYER if c['crypt_owner'] == 7 else native.OTHEROWNER
            native.f.put(wanted[owner], expected['crypt']['slot'], expected['crypt']['count'], 1)
            native.f.put(wanted[native.ROOM], 0x53, expected['crypt']['tag'], 2)
            native.f.put(wanted[native.ROOM], 0x55, expected['crypt']['room55'])
            allowed.update([owner, native.ROOM])
        assert post == wanted, name
        assert all(int(r['address'], 16) in allowed for r in reference[name][4]['context_mismatches'])
    return result, reference


def crosschecks():
    rows = load('phase107-tick-crosschecks.json')['replays']
    assert len(rows) == 2
    for row in rows:
        value = load('ghidra/'+row['file'])
        args, expected = cross.build(row['case'])
        assert value['input'] == args and value['expected'] == expected
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        r = value['result']
        assert r['success'] and r['hit_return']
        assert all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
        assert expected['compared_contexts'] == 42 and expected['compared_bytes'] == 95542
    return rows


def build():
    p, init = primary(), initializers()
    rows, reference = replays()
    controls = crosschecks()
    visited = set()
    for _, _, _, _, r in reference.values():
        visited.update(r['visited_instructions'])
    for row in init:
        visited.update(row['code_visits'])
    assert all(hex(r['address']) in visited for r in p['functions'] if r['executed'])
    prior = ROOT/'phase107-prior-components.json'
    if not prior.exists():
        parts = load('editor-format-package.json')['components']
        assert len(parts) == 85
        prior.write_text(json.dumps(parts, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 85
    # Frozen phase103 backend remains unchanged despite the instrumented derivative.
    old_backend = next(r for r in load('element-model-semantics.json')['helpers'] if r['file'] == 'native_unicorn.py')
    assert digest((ROOT/old_backend['file']).read_bytes()) == old_backend['sha256']
    diagnostic = []
    for path in (ROOT/'ghidra').glob('phase107-whole-tick-diagnostic*.json'):
        value = load(str(path.relative_to(ROOT)))
        diagnostic.append(dict(**link(str(path.relative_to(ROOT))), success=value['result']['success'],
                               final_pc=value['result']['registers']['EIP'], accepted_tick_case=False))
    old_fairy = [p for p in (ROOT/'ghidra').glob('phase107-tick-fairy1-*.json')
                 if p.name not in {r['file'] for r in rows}]
    assert len(old_fairy) == 1
    bad = load(str(old_fairy[0].relative_to(ROOT)))
    assert bad['result']['success'] and bad['expected']['speed'] == 1100
    assert int.from_bytes(bytes.fromhex(bad['result']['captured_hex'])[0x11C:0x11E], 'little') == 1000
    groups = load('room-species-bonus-semantics.json')['species_groups']
    assert next(r for r in groups if r['owner_byte_offset'] == 0x108C)['creature_ids'] == [20]
    assert next(r for r in groups if r['owner_byte_offset'] == 0x1094)['creature_ids'] == [16]
    return dict(phase=107, date='2026-10-29', family='Creatures/Rooms gameplay relationships',
        profile=dict(file=str(EXE), sha256=digest(EXE.read_bytes())), primary=p,
        initializers=init, native_replays=rows, crosschecks=controls,
        unchanged_source_executables=source_executables(),
        helpers=[link(n) for n in ['emulate_crypt_tick.py', 'probe_creature_tick.py', 'native_unicorn_tick.py',
                                   'crosscheck_creature_tick.py', 'prepare_crypt_tick_primary.py']],
        dependencies=[link(n) for n in ['crypt-threat-semantics.json', 'room-species-bonus-semantics.json',
                                       'navigation-speed-semantics.json', 'native_unicorn.py',
                                       'ghidra/phase90-creature-factory-base.bin', 'portal_animation_tables.py',
                                       'spell_state_tables.py', 'creature_category_tables.py']],
        scope='Complete original488930 fromentrytoreturn;prepared post-factory source and initializedtables/emptyforces. '
              'Not full livegame/worldstartup oralltickbranches. Whole-context pairedcomparisons;baseline sideeffects measured,not all independentlynamed.',
        fairy=dict(species=20, owner_byte=0x108C, count_interpretation='signed8;applies onlypositive',
                   saved_speed_getter=0x48D000, setter=0x48D010, actor_speed_WORD=0x11C,
                   controller_max_DWORD=0x206, original_branch=0x4898B3,
                   requested='unsigned32(low32(cachedSpeed*(count+10)*10))/100',
                   applied='Setter clamps unsignedrequest to4095;controller max becomes speed<<4.',
                   restore='Later nativecaller restores savedcachedspeed;observedwrite sequence1000->1100->1000 and16000->17600->16000.',
                   meaning='Temporary modifier inside complete tick,not permanentincrement ofKWD orcachedspeed.',
                   examples={'base1000_count1_peak': 1100, 'count2_peak': 1200, 'count3_peak': 1300,
                             'base4000_count1_peak': 4095, 'final_base1000': 1000}),
        wizard=dict(species=16, owner_byte=0x1094, count_interpretation='signed8;applies onlypositive',
                    actor_DWORD=0x3B3, native_name='WorkEnhancement', addend_per_count=205,
                    assignment='Base contributions409 pernonzeroWORD4D7 andconditionalcarry-relatedfield;then add205*positivecount. '
                               'Newcases have those othercontributors zero.',
                    examples={'count1': 205, 'count2': 410, 'count127': 26035, 'count128': 0},
                    downstream_static='496E00 forms Q12sum withactor3AF andactor3B3 through43D750 thenQmul4363F0; '
                                      'actual job/dig/research/manufacture output notnewlyexecuted. Do not claim universalexact5percentworkrate.'),
        crypt=dict(native_setters=6, same_CPU_memory_as_tick=True,
                   tests='Insert/clear Fairy20 andWizard16;registration roomowner8 vsactorowner7.',
                   prepared='Minimal memberandempty-listroom,notnewfactory orautomaticCrypt entry.',
                   new_raw_percentage_fields=0, groups_assigned_total=8, groups_remaining=10),
        state_coverage=dict(start_state=1, end_state=89, HP=1000, XYZ=[0x5800, 0x6800, 0x2000],
                            motion='No actualdisplacement inacceptedcases;temporaryspeed writes/realnavcall execute,butwalkingdistancebonus notnewlyproved.'),
        graphics=dict(interface64=0x5671D0, interface80=0x59C140, matrix=0x594E10, clip=0x575D70,
                      binding='Prepared external interfacevtable points toexactoriginalCPCEngineInterface entries;originalPEvtables retained.',
                      state='Actor graphics resourceNULL;camera/frustum usePEinitial zero globals. ActualCPUgraphicsmath executes;notconfiguredviewport/GPU/livevisibilityproof.'),
        verification=dict(paired_whole_context='For eachcache0/1000/4000 control,captureentireactor1599bytes;predictallcase differences '
                                              'onlyWorkEnhancement andCrypt room/owner mutations. Otherpreparedcontextbytes unchanged.',
                          independent_numeric='Finalspeed/max,temporarywrite sequences,WorkEnhancement,HP,XYZ,state89 andoriginalPCcoverage checked.',
                          backend_agreement='Ghidra andUnicorn matchall42preparedregions/95542bytes forbaseandFairy1. ThisisCPUagreement,not designerintent proof.',
                          protected='OriginalPEcode,Creature/Object/World/graphicsvtables protectedfrominput writes andduringCPUrun;all codeexecution insideoriginalPE.',
                          OS_or_IO_replacements_executed=0),
        rejected_fairy_expectation=dict(**link(str(old_fairy[0].relative_to(ROOT))),
            reason='Initialprediction expectedfinal1100;nativecorrectlyrestored1000. Rejectedexpectation,not crash. '
                   'Instrumentedwrite tracingthenprovedtemporaryincrease/restoration.'),
        exploratory_diagnostics=diagnostic,
        counts=dict(native_tick_cases=22, Ghidra_crosschecks=2, native_initializers=4,
                    crypt_setters=6, full_code_spans=20, executed_path_spans=19, PE_blocks=6,
                    independent_gameplay_coverage='Scoped numericproperties andpairedwholecontexts;notallfields/allbranches.',
                    raw_container_edits=0, crypt_groups_assigned_total=8, crypt_groups_remaining=10),
        limitations=['Exact suppliedDX;otherversions notnewlyverified.',
                     'Complete488930 inpreparedstate,not completeWorld initializationorlivegame.',
                     'AI/categories/actions/nav initialization areseparate verifiednativepreparation;Cryptsetter/tick shareoneCPU.',
                     'Emptyforces,stationarysource,nomodel;camera/frustumPEinitial/unconfigured. No displacementorGPUproof.',
                     'Cachedspeed preparedcoherently byWORD11C/controller206;not newrawSpeed→loader→gettercomposition.',
                     'Wizardwrites WorkEnhancement205/count;actualjoboutput/recharge impact remainsstaticonly.',
                     'Fullbaseline tick sideeffects measured;paired differences fullychecked,not everybaselinefieldindependentlydecoded.',
                     'No newKWDpercentfield/API/raweditcoverage;previousschemas retainedbyteexact.',
                     'Other10Cryptgroups andmajorunknownsJump/Priority/scalar113114/Objects34C/Traps204/Doors236/Terrain198224 remainopen.'])


def main():
    schema = build()
    (ROOT/'crypt-tick-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
