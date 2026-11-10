"""Publish native resistance taxonomy and replace the disproved spell-list binding."""
from pathlib import Path
import copy, hashlib, json, struct
import native_combat_categories as api
import emulate_matched_categories as game
import emulate_editor_combat_categories as editor
import prepare_combat_type_primary as primary
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def table(blob, address):
    rows = []
    for index in range(64):
        ident, pointer = struct.unpack('<iI', pe_read(blob, address + 8 * index, 8))
        if ident == -1:
            return dict(address=address, rows=rows, terminator_address=address + 8 * index)
        text = bytearray()
        for char in range(128):
            pair = pe_read(blob, pointer + 2 * char, 2)
            if pair == b'\0\0':
                break
            text.extend(pair)
        else:
            raise AssertionError('Unterminated native category label')
        rows.append(dict(id=ident, label=text.decode('utf-16le'), label_pointer=pointer))
    raise AssertionError('Unterminated native type table')


def effective_layouts():
    result = {}
    original = load('layouts.json')
    for family in ['Creatures', 'Creatures_5537']:
        layout = copy.deepcopy(original[family])
        types = [field for field in layout['fields'] if field['name'].startswith('resistances[') and field['name'].endswith('.AttackType')]
        values = [field for field in layout['fields'] if field['name'].startswith('resistances[') and field['name'].endswith('.Value')]
        assert len(types) == len(values) == 4
        for index, field in enumerate(types):
            assert field['offset'] == 0xE0C + 2 * index
            field.update(enum='Creature.ResistanceCategory', semantics='Native Editor category table512D28 and dialog233; IDs0..15 labelled, other raw u8 preserved.',
                native_evidence_component='combat_category_semantics')
        for field in values:
            field.update(semantics='Physical u8; native dialog validation0..100. No runtime percentage formula proved.',
                editor_validation_range=[0, 100], native_evidence_component='combat_category_semantics')
        result[family] = layout
    return result


def build():
    manifest = primary.build()
    assert manifest == load('phase125-primary-manifest.json')
    blob = primary.EXE.read_bytes()
    tables = [table(blob, a) for a in [0x512D28, 0x5138D0, 0x50C318]]
    resistance, shot, attack = tables
    assert [row['id'] for row in resistance['rows']] == list(range(1, 16))
    assert [row['id'] for row in shot['rows']] == list(range(4, 16))
    assert resistance['rows'][3:] == shot['rows'] and resistance['rows'] == attack['rows']
    normalized = {str(row['id']): row['label'].upper().replace(' ', '_').replace('-', '_') for row in resistance['rows']}
    normalized = {'0': 'NONE', **normalized}
    assert normalized == {str(k): v for k, v in api.CATEGORIES.items()}
    for row in load('phase125-matched-category-emulations.json')['replays']:
        case = next(c for c in game.cases() if c['name'] == row['case'])
        args, checks = game.native.probe.build(case)
        path = game.path_for(args)
        saved = json.loads(path.read_text(encoding='utf8'))
        assert saved['input'] == args and row['sha256'] == sha(path.read_bytes())
        assert row['outcome'] == game.verify(case, args, checks, saved['result'])
    editor_replays = load('phase125-editor-combat-emulations.json')['replays']
    assert len(editor_replays) == 23
    for row in editor_replays:
        saved = load(row['file'])
        case = saved['input']
        assert case == editor.build(case['kind'], case['value'], case['slot'])
        assert row['outcome'] == editor.verify(case, saved['result']) and row['sha256'] == sha((ROOT / row['file']).read_bytes())
    controls = load('phase125-combat-type-crosschecks.json')['replays']
    assert len(controls) == 6
    import crosscheck_combat_categories as independent
    for row in controls:
        if row['port'] == 8089:
            args, count, size = independent.game_input(row['case'])
        else:
            kind, value, slot = row['case'].rsplit('-', 2)
            args, count, size = independent.editor_input(kind, int(value), int(slot))
        saved = load(row['file'])
        assert saved['input'] == args and row['sha256'] == sha((ROOT / row['file']).read_bytes())
        result = saved['result']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][key], 16) == value for key, value in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
        assert row['checked_regions'] == count and row['checked_bytes'] == size
    corpus = load('phase125-category-api-validation.json')
    for row in corpus['files']:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    assert corpus['exact_container_rebuilds'] == 410 and corpus['records'] == 3734
    sources = load('game-executable-compatibility.json')['original_executable_paths_preserved']
    for row in sources:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    previous = ROOT / 'phase125-prior-components.json'
    if not previous.exists():
        prior = load('editor-format-package.json')['components']
        assert len(prior) == 103
        previous.write_text(json.dumps(prior, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(previous.name)) == 103
    return dict(phase=125, date='2026-10-29', family='Creature resistance categories and Shot attack categories',
        editor=dict(program='DK2 Editor pro.exe', sha256=sha(blob), native_tables=tables,
            dialog_resource_id=233, constructor=0x463D20, modal_caller=0x442940,
            init=0x463DA0, DDX=0x463F30, list_builder=0x463FF0, lookup=0x4641E0,
            selection_map=dict(model_offset=0x78, count_offset=0x7C, entries=16, none_index=0, ids=list(range(16))),
            controls=dict(combos=[1766, 1769, 1772, 1775], values=[1767, 1770, 1773, 1776]),
            editor_value_validation=[0, 100], validation_is_runtime_percentage_formula=False,
            OK_message=dict(map_address=0x512DA8, button=12431, handler=0x464310),
            commit='After UpdateData, map-selected type0 forces value0; 41E450 writes four pairs into model1594..159B.',
            getter=0x41E440, setter=0x41E450,
            out_of_list='Native lookup returns-1 for selected tested unknown IDs18/41/255. Original files also contain16/30/32. Preserve them as opaque; no complete GUI handling of those values proved.',
            other_attack_table='4356B0 uses only native melee entries1/2/3 and15 then dynamically appends CreatureSpell catalogue IDs; do not globally reinterpret every other field sharing the historical enum name.'),
        native_resistance_categories=normalized,
        native_shot_categories={key: value for key, value in normalized.items() if int(key) >= 4},
        effective_creature_layouts=effective_layouts(),
        shared_IDs=list(range(4, 16)), shared_labels_and_string_pointers=True,
        corrected_interpretation=dict(field_scope='Only four Creature resistance AttackType fields, in5449/5537 layouts.',
            historical_enum='Creature.AttackType', replacement_enum='Creature.ResistanceCategory',
            old_reference_issue='The historical list names individual spells. Native resistance UI uses generic attack categories instead; e.g6Lightning,8Projectile,11CloseCombat.',
            prior_tests='Phase114types18/11 and phase115type41 were selected from the historical spell list. Their observed non-reading remains factual, but they were not proven matching resistance categories. Nine new executions explicitly match8/6.',
            not_a_game_patch=True),
        selected_native_paths=load('phase125-matched-category-emulations.json')['replays'],
        editor_native_cases=editor_replays, independent_Ghidra_controls=controls,
        primary=link('phase125-primary-manifest.json'), corpus=dict(evidence=link('phase125-category-api-validation.json'),
            files=82, records=3734, resistance_slots=14936, exact_rebuilds=410,
            changed=328, identity=82, observed_unknown_IDs=[16, 30, 32], source_writes=0),
        original_executable_paths_preserved=sources,
        helpers=[link(name) for name in ['native_combat_categories.py', 'emulate_matched_categories.py',
            'emulate_editor_combat_categories.py', 'crosscheck_combat_categories.py', 'validate_combat_categories.py',
            'prepare_combat_editor.py', 'prepare_combat_type_primary.py']],
        limitations=['Native UI taxonomy0..15 is not a universal proof of all runtime type IDs; observed16/30/32 remain unresolved.',
            'The actual native table is shared for IDs4..15; engine damage still doesnot read the four resistance pairs in the three tested direct/AoE paths.',
            'Full MFC dialog, UpdateData/Win32 window interaction and whole original Creature writer not executed together in thisphase; UI clearing and range are statically attested.',
            'Other fields using historical Creature.AttackType are not globally relabelled without field-specific evidence.',
            'No resistance percentage formula or mod patch implemented; unknown bytes and original sources preserved.',
            'Substantial remaining fields across15families remain unresolved; goalnotachieved.'])


if __name__ == '__main__':
    value = build()
    (ROOT / 'combat-category-semantics.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')
    print('Native categories0..15; shared Shot IDs4..15;9matched damage cases/23editor cases/6Ghidra controls.')
