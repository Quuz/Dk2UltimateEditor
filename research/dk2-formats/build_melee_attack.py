"""Publish executed melee-to-HP paths and two newly assigned Crypt effects."""
from pathlib import Path
import hashlib
import json
import struct
import emulate_melee_attack as native
from build_effect_semantics import EXE, pe_read
from audit_targeted_spell import executable_ranges
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
    for key, rows in load('phase105-primary-manifest.json').items():
        result[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            result[key].append(dict(row, evidence_sha256=digest(path.read_bytes()), code_or_data_sha256=digest(blob)))
    assert len(result['functions']) == 26 and len(result['blocks']) == 8
    assert pe_read(data, 0x66D524+0x34, 4) == struct.pack('<I', 0x628BC0)
    assert pe_read(data, 0x66D654+0x34, 4) == struct.pack('<I', 0x628BC0)
    assert pe_read(data, 0x66D524+0x74, 4) == struct.pack('<I', 0x4B5DE0)
    assert struct.unpack_from('<H', pe_read(data, 0x65FC94, 514), 0)[0] == 0
    return result


def replays():
    manifest = load('phase105-melee-attack-emulations.json')
    assert manifest['scope'] == native.__doc__
    cases = {c['name']: c for c in native.cases()}
    protected = executable_ranges(EXE.read_bytes()) + [
        (0x66D524, 0x130), (0x66D654, 0x80), (0x66E3EC, 0x3A0),
        (0x65FC94, 514), (0x69DA38, 149), (0x51D740, 960), (0x4D9274, 180), (0x4D9328, 180)]
    rows = []
    for row in manifest['replays']:
        path = ROOT/'ghidra'/row['file']
        value = json.loads(path.read_text(encoding='utf8'))
        case = cases[row['case']]
        args, expected = native.build(case)
        assert value['case'] == case and value['input'] == args and value['expected'] == expected
        assert digest(path.read_bytes()) == row['sha256']
        result = value['result']
        assert result['success'] and result['hit_return']
        assert all(int(result['registers'][k], 16) == v for k, v in
                   dict(EAX=1, EBP=1, EBX=0, ECX=0, EDX=expected['outcome']['hp']).items())
        regions = {int(r['address'], 16): bytes.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        assert all(a+len(b) <= p or a >= p+n for a, b in regions.items() for p, n in protected)
        assert regions[native.cf.ACTOR][:4] == struct.pack('<I', 0x66D524)
        assert regions[native.TARGET][:4] == struct.pack('<I', 0x66D524 if case['target_kind'] == 'creature' else 0x66D654)
        assert regions[native.f.WORLD][:4] == struct.pack('<I', 0x66E3EC)
        assert regions[native.f.GV][0x10C:0x110] == struct.pack('<I', 0x59D380)
        assert regions[native.cf.ACTOR][0x6C:0x70] == bytes(4)
        checked = {int(r['address'], 16): r['bytes'] for r in expected['checks']}
        assert checked[native.TARGET] == (1343 if case['target_kind'] == 'creature' else 393)+256
        assert checked[native.cf.ACTOR] == 1343+256
        assert checked[native.sp.VARS] == 4096
        assert all(a in checked for a in [native.cf.DEFINITION, native.TARGET_DEF, native.f.PLAYER,
                                         native.f.WORLD, native.f.SESSION, native.sp.RAW, native.RAW65,
                                         native.CAP, 0x6ED540, 0x6EE540])
        rows.append(dict(row, case_inputs=case, expected=expected))
    assert len(rows) == len(cases) == 40
    accepted = {r['file'] for r in rows}
    diagnostics = [p for p in (ROOT/'ghidra').glob('phase105-attack-object-possessed60-*.json') if p.name not in accepted]
    assert len(diagnostics) == 1
    bad = load(str(diagnostics[0].relative_to(ROOT)))
    assert bad['result']['success'] and bad['result']['hit_return']
    assert int(bad['result']['registers']['EDX'], 16) == 969
    assert int(bad['result']['registers']['EBP'], 16) == 0
    return rows, dict(**link(str(diagnostics[0].relative_to(ROOT))),
                     reason='Rejected expectation incorrectly gave Object Creature timestamps. Native HP969 correct; '
                            'Object skips timestamps. Corrected full-context observer then passed. Not a game crash.')


def build():
    p = primary()
    rows, diagnostic = replays()
    previous = ROOT/'phase105-prior-components.json'
    if not previous.exists():
        parts = load('editor-format-package.json')['components']
        assert len(parts) == 83
        previous.write_text(json.dumps(parts, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(previous.name)) == 83
    crypt = load('room-species-bonus-semantics.json')
    groups = {r['owner_byte_offset']: r for r in crypt['species_groups']}
    assert groups[0x1090]['creature_ids'] == [23] and groups[0x1091]['creature_ids'] == [44]
    record_source = load('possession-damage-semantics.json')['current_records']
    assert digest(Path(record_source['file']).read_bytes()) == record_source['sha256']
    cases = native.cases()
    outcomes = {r['case']: r['expected']['outcome'] for r in rows}
    counts = dict(native_cases=40, raw_callbacks=80, full_attack_executions=40,
                  possession_setters=sum(c['possessed']+c['target_possessed'] for c in cases),
                  HP_handler_executions=sum(r['damage_call'] for r in outcomes.values()),
                  HP_changed_cases=sum(r['applied_delta'] != 0 for r in outcomes.values()),
                  crypt_compositions=sum('crypt_species' in c for c in cases),
                  native_event_payloads=sum(r['event'] for r in outcomes.values()),
                  sound_IO_calls=sum(r['sound'] for r in outcomes.values()),
                  whole_code_spans=26, file_backed_blocks=8,
                  new_raw_container_edits=0, previously_verified_stat4_edits=1452)
    return dict(phase=105, date='2026-10-29', family='Creatures/Objects/GlobalVariables/Rooms',
        profile=dict(file=str(EXE), sha256=digest(EXE.read_bytes())),
        primary=p, native_replays=rows, outcomes=outcomes, rejected_projection_diagnostic=diagnostic,
        counts=counts, unchanged_source_executables=source_executables(),
        helpers=[link(n) for n in ['emulate_melee_attack.py', 'prepare_melee_primary.py',
                                  'native_possession_damage.py', 'native_possession_recharge.py', 'native_room_counters.py']],
        dependencies=[link(n) for n in ['possession-damage-semantics.json', 'possession-health-semantics.json',
                                       'room-species-bonus-semantics.json', 'emulate_possession_speed.py',
                                       'ghidra/phase90-creature-factory-base.bin', 'ghidra/phase89-object-factory-base.bin',
                                       'phase104-damage-api-validation.json']],
        editable_source=dict(file=record_source['file'], sha256=record_source['sha256'],
                             kind=224, stat_id=4, value_DWORD_offset=8, loaded='low16;consumer signed16',
                             API='native_possession_damage.patch_stat4_value',
                             scope='Existing lossless patch API,1452 prior checks;no new raw field or new patch count.'),
        chain=dict(callback224=0x51DC70, callback65=0x51DC40, possession_setter=0x48DEA0,
                   full_attack=0x498E30, getter=0x494D00, target_HP=0x4B2CE0,
                   target_lookup=dict(tag_WORD=0x399, existence=0x6ED540, pointers=0x6EE540),
                   target_vslot34=dict(Creature_vtable=0x66D524, Object_vtable=0x66D654, original_function=0x628BC0),
                   graphics_query=[0x59D380, 0x4B5D50, 0x4B5D40, 0x59D260],
                   notification=[0x48F2E0, 0x517480, 0x527150]),
        cooldown=dict(condition='If no possession:unsigned tick<=low32(lastAttack-1+definition5CA) returns1 before target lookup.',
                      tick_fixture=100, blocking_interval=101, passing_interval=100,
                      possessed='This specific gate bypassed;not a proof of unrestricted overall attack rate.'),
        direction=dict(helper=0x65FB54, inputs='signed32(attackerX-targetX),signed32(attackerY-targetY)',
                       fold='angle &= 7FF; if angle>3FF:angle=7FE-angle; p=(angle&3FF)//20',
                       apply='Only Creature targets; if p>15:delta += trunc0(s32(low32(p*delta))/100).',
                       confirmed_cardinals=[dict(dx=0, dy=-4096, angle=0, percent=0, loss31=31),
                                             dict(dx=0, dy=4096, angle=1024, percent=51, loss31=46),
                                             dict(dx=4096, dy=0, angle=512, percent=25, loss31=38),
                                             dict(dx=-4096, dy=0, angle=1536, percent=25, loss31=38)],
                       heading='Independent changes of attacker/target WORD F4 to1024 do not change checked outcome; '
                               'visible caller does not subtract headings. Do not label proven backstab/relative facing.',
                       scope='Four cardinal inputs;not all integer atan2 boundary/overflow inputs.'),
        owner_modifiers=[dict(owner='attacker pointerDWORD2A', byte_offset=0x1090, signed=True,
                              condition='count>0', formula='trunc0(s32(low32((count+10)*delta*10))/100)',
                              crypt_species=23, current_name=groups[0x1090]['current_names'][0], ordinary_percent_per_count=10),
                        dict(owner='attacker pointerDWORD2A', byte_offset=0x1091, signed=True,
                              condition='count>0', formula='trunc0(s32(low32((15*count+100)*delta))/100)',
                              crypt_species=44, current_name=groups[0x1091]['current_names'][0], ordinary_percent_per_count=15)],
        modifier_order=['native494D00 withtype224/stat4 andtype65/stat4', 'negate getter result to signed delta',
                        'Creature direction adjustment', 'attacker owner1090', 'attacker owner1091',
                        'target4B2CE0 protections/received-damage factor andHP'],
        crypt=dict(setter=0x4D8E60, cleanup=0x4D9570, room_type=26,
                   binding='Original Crypt.cpp/callers proved inphase94;new compositions execute setter andfullattack.',
                   prepared='Minimal member ID/tag and room with empty cleanup lists;not newly factory-created room/member.',
                   tests='Insertion/removal23/44,second23,normal attacker,androom owner8 whileattacker owner7.',
                   raw_percentage_field_found=False,
                   interpretation='Hardcoded EXE mapping/coefficient. Species/room IDs areexistingKWDfields,not new percentages.',
                   other_unassigned_groups=15),
        target_rules=dict(Object='Negative delta requires definitionDWORD_F4 bit200000;sourceownerWORD36 stillwritten on refusal. '
                                 'Objects do not get Creature timestamps.',
                          Creature='WORD445 stops attack before impact/HP/sound;normal damage followsphase93 factor. '
                                   'Possessed target innewcomposition gets31->15 damage,HP985,one exactSession payload.',
                          missing_or_state2='NoHP/sound;attacker3EA1000 isstill synchronized before earlyreturn.',
                          zero_damage='NoHP handler call;attack still issues its own sound.'),
        external_IO=dict(graphics='Prepared interfacevtable points10C tooriginal59D380;actor renderresourceNULL; '
                                  'native59D260 returns0 andcaller fallsback totargetposition.',
                         sound='Only explicit externalprovider increments OUT8,returns13572468;no actualaudio.',
                         session='Original event chain emits16bytes toexplicit captureprovider;no network/sessionconsumer.',
                         impact='Attacker definitionBYTE77A=0 frompreparedfactory definition;originalmaterial helper searches '
                                'originaltable thenreturns onFFsentinel without creating visuals/sounds.'),
        limitations=['Exact supplied DX only;not everygameversion.',
                     'Prepared post-factory classes/definitions/world. No new rawCreature/Object loader,allocation orlivegame.',
                     'RawVariables record callbacks execute;whole Variables container parser not executed innewcomposition.',
                     'Full498E30 returns,with ordinary nonlethalCreature/Object targets;not everybranch ofallfunctions.',
                     'No mounted/rescue/friendlyfighter/ID19 special branch,Door/Trap target orlethal/death path.',
                     'Only null-model graphic fallback andunmatched impact type;realmodel hitpoints/effects/GPU/audio not executed.',
                     'No animation/AI/physics/full combat tick;cooldown bypass doesnot meanunlimitedliveattackrate.',
                     'Direction isworldXY-based incheckedcaller;do not generalize to allangles/backstab withoutfurtherwork.',
                     'Scalar98/113/114,Jump,Priority andother significant15-familyunknowns remainunresolved.',
                     'No new serializationcoverage claimed;existingrawAPI retained andprior1452checks remainhistorical.'])


def main():
    schema = build()
    (ROOT/'melee-attack-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
