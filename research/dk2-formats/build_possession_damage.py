"""Publish exact stat4 possession damage; distinguish getter from full attack."""
from pathlib import Path
import hashlib
import json
from build_effect_semantics import EXE, pe_read
from audit_targeted_spell import executable_ranges
import emulate_possession_damage as native

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT/name).read_bytes()))


def primary():
    data = EXE.read_bytes()
    out = {}
    for key, rows in load('phase104-primary-manifest.json').items():
        out[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            out[key].append(dict(row, memory_sha256=digest(path.read_bytes()),
                                  program_bytes_sha256=digest(blob)))
    assert len(out['functions']) == 11 and sum(r['executed'] for r in out['functions']) == 8
    assert len(out['blocks']) == 1
    return out


def replays():
    manifest = load('phase104-possession-damage-emulations.json')
    assert manifest['scope'] == native.__doc__
    cases = {c['name']: c for c in native.cases()}
    rows = []
    protected = executable_ranges(EXE.read_bytes()) + [(0x51D740, 960), (0x66D524, 0xD0)]
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
                   dict(EAX=1, EBP=1, EBX=0, ECX=0, EDX=expected['outcome']['result_bits']).items())
        regions = [(int(r['address'], 16), bytes.fromhex(r['hex']))
                   for r in json.loads(args['memory'])['regions']]
        assert all(a+len(b) <= p or a >= p+n for a, b in regions for p, n in protected)
        rows.append(dict(row, case_inputs=case, expected=expected))
    assert len(rows) == len(cases) == 27
    accepted = {r['file'] for r in rows}
    diagnostic = [p for p in (ROOT/'ghidra').glob('phase104-damage-pit150-*.json') if p.name not in accepted]
    assert len(diagnostic) == 1
    value = json.loads(diagnostic[0].read_text(encoding='utf8'))
    assert value['result']['success'] and value['result']['hit_return']
    assert int(value['result']['registers']['EBP'], 16) == 0
    assert int(value['result']['registers']['EDX'], 16) == 50
    return rows, dict(**link(str(diagnostic[0].relative_to(ROOT))),
                     reason='Rejected initial expected pit factor omitted100. Correct native result50 vsoldexpected30. '
                            'Not an accepted verification,not a game crash;new observer inputs rechecked whole contexts.')


def source_executables():
    checks = [(str(ROOT.parent.parent/path), sha) for path, sha in load('executable-hash-verification.json').items()]
    rc3 = load('rc3-job-overlap-semantics.json')['profile']
    old = load('creature-version-layout-semantics.json')['old_source']
    checks += [(rc3['original_file'], rc3['sha256']), (old['original_path'], old['sha256'])]
    for path, sha in checks:
        assert digest(Path(path).read_bytes()) == sha
    return [dict(file=p, sha256=s) for p, s in checks]


def current_records():
    doc = load('decoded/GlobalVariables.kwd.json')
    data = Path(doc['source']).read_bytes()
    assert digest(data) == doc['sha256']
    rows = []
    for row in doc['sections'][0]['records']:
        words = row['words_u32']
        if words[:2] not in [[224, 4], [65, 4]] and words[0] != 225:
            continue
        blob = bytes.fromhex(row['raw_hex'])
        assert data[row['offset']:row['offset']+16] == blob
        rows.append(dict(offset=row['offset'], words=words, raw_hex=blob.hex()))
    assert len(rows) == 11
    return dict(file=doc['source'], sha256=doc['sha256'], records=rows,
                scope='Offsets only for this exact hash;select records by type/stat/level in other containers.')


def build():
    p = primary()
    rows, diagnostic = replays()
    corpus = load('phase104-damage-api-validation.json')
    for row in corpus['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    assert corpus['file_count'] == 557 and corpus['exact_container_rebuilds'] == 1452
    assert corpus['records_by_kind'] == {'65/4': 9, '224/4': 1, '225': 474}
    prior = ROOT/'phase104-prior-components.json'
    if not prior.exists():
        components = load('editor-format-package.json')['components']
        assert len(components) == 82
        prior.write_text(json.dumps(components, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 82
    cases = native.cases()
    pit_label = pe_read(EXE.read_bytes(), 0x69D470, 32).split(bytes(1))[0]
    assert pit_label.strip().startswith(b'CombatPitFighter')
    return dict(phase=104, date='2026-10-29', family='Variables/Creatures',
        profile=dict(exe=str(EXE), sha256=digest(EXE.read_bytes())), primary=p,
        native_replays=rows, rejected_projection_diagnostic=diagnostic,
        corpus=corpus, current_records=current_records(), unchanged_source_executables=source_executables(),
        helpers=[link(n) for n in ['native_possession_damage.py', 'native_possession_recharge.py',
                 'emulate_possession_damage.py', 'validate_possession_damage.py',
                 'prepare_possession_damage_primary.py']],
        dependencies=[link(n) for n in ['creature-stat-runtime-map.json', 'variable-runtime-map.json',
                      'possession-speed-semantics.json', 'ghidra/phase90-creature-factory-base.bin']],
        storage=[dict(kind=224, stat=4, value_DWORD_offset=8, loaded='low16', consumer='signed16',
                      runtime_offset_from_variables=0x31B, meaning='possession/secondary melee damage modifier'),
                 dict(kind=65, stat=4, value_DWORD_offset=8, level_DWORD_offset=12,
                      loaded='low16', consumer='signed16', runtime_offset='117h+2*actorBYTElevel',
                      meaning='melee damage level coefficient;level1 bypasses coefficient'),
                 dict(kind=225, value_DWORD_offset=4, loaded='low8', runtime_offset_from_variables=0x482,
                      meaning='CombatPitFighter melee modifier;add100 before Q12 division')],
        base_damage=dict(raw_creature_offset=0xF14, raw_bytes=2, definition_offset=0x5C4,
                         consumer_width='unsigned16', actor_level_offset=0x403,
                         scope='Raw binding from prior verified stat mapping. New tests prepare runtime definition;no new rawCreature loader.'),
        native_paths=dict(callback224=0x51DC70, callback65=0x51DC40, callback225=0x51CD90,
                          possession_setter=0x48DEA0, complete_getter=0x494D00,
                          qadd=0x43D750, qdiv=0x441990, qmul=0x4363F0,
                          static_game_caller=0x498E30),
        formula=dict(priority=['actor3E6/80 ->(100+signedP)/100',
                               'else actor3EA/08 ->(100+signedP)/200',
                               'else actor3E6/8000 ->(100+low8(scalar225))/100',
                               'else factor4096'],
                     exact='Level!=1 uses signed32 low32(coefficientSigned16*baseUnsigned16) /100,towardzero. '
                           'Ratios use native unsigned32 numerator<<12 andsigned denominator;factorQ12 truncates. '
                           'Then signed64 Q12 multiply,low32,and arithmetic>>12. SeeAPI;not a floatingpercentage formula.',
                     examples={'base20_P60_possessed': 31, 'base20_P60_normal': 20,
                               'base20_P60_secondary': 15, 'base20_P100_possessed': 40,
                               'base20_level2_coeff125_P60': 39, 'base20_pit225150': 50}),
        static_caller=dict(call=0x498EC8, negate=0x498ED3, HP_calls=[0x49924D, 0x49926C],
                           explanation='Original498E30 calls494D00 withattacker definition andactor,negatesresult, '
                                       'then has facing/owner/target adjustments andcalls4B2CE0 onthetarget. '
                                       'Wholecaller attested statically,not executed;getter value not promised as finalHP delta.'),
        jump_review=dict(raw_offset=0xEE0, definition_offset=0x75C, meaning_resolved=False,
                         evidence='51F3B0 copies definition75C tosameoffset;494D00 reads5C4,494EF0reads5C6. '
                                  'Neitheris a discoveredJump gameplay getter. No renewed broadnegative scan/globalnonuseclaim.'),
        scalar_boundaries='98/113/114 values0/6/30000 do not change checked494D00 result;not global unused proof.',
        pit_flag_name=dict(mask=0x8000, actor_flags_offset=0x3E6, native_label='CombatPitFighter',
                           diagnostic_function=0x490240, string_address=0x69D470,
                           source_string_hex=pit_label.hex(),
                           diagnostic_evidence=link('ghidra/phase24-creature-state-dump.txt'),
                           scope='Native diagnostic labels bit15;entry/exit ofCombatPit not executed.'),
        counts=dict(native_cases=len(cases), raw_callback_executions=2*len(cases)+sum('pit_percent' in c for c in cases),
                    possession_setter_executions=sum(c['possessed'] for c in cases), getter_executions=len(cases),
                    whole_code_extents=11, executed_path_code_extents=8, file_backed_blocks=1,
                    variable_source_files=557, selected_API_records=484,
                    exact_container_rebuilds=1452, changed_containers=1447, identity_containers=5),
        limitations=['Exact DX hash;not automatically otherversions.',
                     'Prefix+raw16 callbacks run;whole Variables file parser not in newnative composition.',
                     'Prepared phase90 actor/world/definition;no newfactory,rawCreature loader orlivegame.',
                     'No original game function/vtable patch. Inherited external fixture services not exercised by this complete getter path.',
                     'Whole498E30 caller is static only;no actualhit/targetHP/animation/facing/ownerbonus composition executed.',
                     'Pit bit prepared;fullCombatPit lifecycle/registration not executed.',
                     'Q12 overflow and signed boundary outcomes are storage diagnostics,not recommended UI ranges.',
                     '27 accepted cases;one rejected expected-pit projection retained separately,not counted as a passing check.',
                     'Earlier24 strict Variables refusals retained;not repaired or declared damaged.',
                     'Jump,113/114,otherstats andremaining15-family unknowns remain open. Goalnot achieved.'])


def main():
    value = build()
    (ROOT/'possession-damage-semantics.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(value['counts']))


if __name__ == '__main__':
    main()
