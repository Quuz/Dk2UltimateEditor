"""Review pinned NovFR sources as external leads, without changing game schemas."""
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parent / 'DK2-Map-Editor-master'
REF = ROOT / 'source-references' / 'novfr-122d2ee3'
COMMIT = '122d2ee3ba65c87f9551cb13bd4ba1872b6edcba'
WIDTH = {'BYTE': 1, 'WORD': 2, 'short': 2, 'LONG': 4, 'DWORD': 4}
FAMILIES = {'Terrain': 'Terrain', 'Room': 'Rooms', 'Door': 'Doors',
            'Trap': 'Traps', 'Spell': 'KeeperSpells', 'Creature': 'Creatures',
            'CrSpell': 'CreatureSpells'}
READ_FAMILIES = {'Terrains': 'Terrain', 'Rooms': 'Rooms', 'Doors': 'Doors',
                 'Traps': 'Traps', 'Spells': 'KeeperSpells',
                 'Creatures': 'Creatures', 'CrSpells': 'CreatureSpells'}
POINTER = re.compile(r'\*\(\((\w+)\s*\*\)\((\w+)\+(0x[0-9a-fA-F]+)\)\)')
FUNCTION = re.compile(r'^(?:int|void|BOOL|BYTE\*|MAPINFO\*)\s+(\w+)\s*\(')


def read(path):
    b = (SOURCE / path).read_bytes()
    try:
        return b.decode('utf-8-sig')
    except UnicodeDecodeError:
        return b.decode('cp1252')


def dump(path, obj):
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    tree = json.loads((REF / 'github-tree.json').read_text(encoding='utf-8'))
    files = [x for x in tree['tree'] if x['type'] == 'blob' and
             (x['path'].startswith('Source/') or x['path'] == 'README.md')]
    manifest = []
    for x in files:
        p = SOURCE / x['path']
        b = p.read_bytes()
        blob = hashlib.sha1(b'blob ' + str(len(b)).encode() + b'\0' + b).hexdigest()
        assert blob == x['sha'], p
        manifest.append({'path': x['path'], 'bytes': len(b), 'git_blob': blob,
                         'sha256': hashlib.sha256(b).hexdigest()})
    dump(REF / 'source-manifest.json', manifest)
    template_manifest = []
    templates = {}
    sizes = {'Terrain': 552, 'Rooms': 1055, 'Doors': 616, 'Traps': 579,
             'KeeperSpells': 406, 'Creatures': 5449, 'CreatureSpells': 266}
    tree_by_path = {x['path']: x for x in tree['tree'] if x['type'] == 'blob'}
    for family, size in sizes.items():
        rel = f'Binaries/Globals/Default{family}.dkg'
        b = (SOURCE / rel).read_bytes()
        blob = hashlib.sha1(b'blob ' + str(len(b)).encode() + b'\0' + b).hexdigest()
        assert blob == tree_by_path[rel]['sha'], rel
        assert len(b) % size == 0, rel
        templates[family] = [b[i:i+size] for i in range(0, len(b), size)]
        template_manifest.append({'family': family, 'path': rel, 'bytes': len(b),
                                  'record_size': size, 'records': len(b)//size,
                                  'git_blob': blob, 'sha256': hashlib.sha256(b).hexdigest()})

    writer_file = 'Source/Sources/Compiler/Compiler_Generate.c'
    writes = []
    function = None
    for no, line in enumerate(read(writer_file).splitlines(), 1):
        m = FUNCTION.match(line)
        if m:
            function = m[1]
        if line.lstrip().startswith('//'):
            continue
        for m in POINTER.finditer(line):
            kind, var, offset = m.groups()
            tail = line[m.end():].lstrip()
            if var not in FAMILIES or not re.match(r'[&|]?=(?!=)', tail):
                continue
            assert kind in WIDTH, (no, line)
            writes.append({'family': FAMILIES[var], 'function': function,
                           'offset': int(offset, 16), 'offset_hex': hex(int(offset, 16)),
                           'bytes': WIDTH[kind], 'cast': kind, 'line': no,
                           'source': writer_file,
                           'statement_sha256': hashlib.sha256(line.strip().encode('utf-8')).hexdigest()})

    reader_file = 'Source/Sources/Properties/GlobalVariables/Properties_Global_Functions.c'
    reads = []
    function = None
    for no, line in enumerate(read(reader_file).splitlines(), 1):
        fm = FUNCTION.match(line)
        if fm:
            function = fm[1]
        if function != 'Properties_GlobalReset':
            continue
        member = re.search(r'Variables->(\w+)\[Offset\]\.([^=]+)=', line)
        if not member:
            continue
        for m in POINTER.finditer(line):
            kind, var, offset = m.groups()
            if var != 'Default':
                continue
            assert kind in WIDTH, (no, line)
            reads.append({'family': READ_FAMILIES[member[1]],
                          'member': member[2].strip(), 'offset': int(offset, 16),
                          'offset_hex': hex(int(offset, 16)), 'bytes': WIDTH[kind],
                          'cast': kind, 'line': no, 'source': reader_file,
                          'statement_sha256': hashlib.sha256(line.strip().encode('utf-8')).hexdigest()})

    targets = [
        ('Creatures', 0xCEE, 4, 'Work Modifier Default'),
        ('Creatures', 0xCF2, 4, 'Work Modifier Level Increase'),
        ('Creatures', 0xEE0, 4, 'Jump'),
        ('Creatures', 0xEF0, 4, 'stat23 / opaque duration'),
        ('Creatures', 0xF55, 1, 'Pick-Up Priority'),
        ('Creatures', 0xEFC, 4, 'unknown flag bits 0x800/0x1000'),
        ('Objects', 0x34C, 2, 'ManaGen label / consumer unknown'),
        ('Terrain', 0x198, 2, 'Unknown198'),
        ('Terrain', 0x224, 4, 'Unknown224'),
        ('Terrain', 0x1AE, 32, 'opaque 16-WORD table'),
        ('Rooms', 0x1C4, 84, 'opaque ArtResource'),
        ('Traps', 0x204, 2, 'Unknown204'),
        ('Doors', 0x236, 2, 'Unknown236'),
        ('KeeperSpells', 0xC8, 4, 'UnknownC8'),
        ('CreatureSpells', 0xE8, 2, 'alternative Shot/Room'),
        ('EffectElements', 0xB1, 2, 'opaque B1/B2'),
        ('Effects', 0xEE, 4, 'opaque EE/F0'),
        ('Effects', 0xF4, 4, 'writer-unassigned F4'),
    ]
    review = []
    for family, offset, size, label in targets:
        overlap = lambda x: x['family'] == family and x['offset'] < offset+size and offset < x['offset']+x['bytes']
        review.append({'family': family, 'offset': offset, 'offset_hex': hex(offset),
                       'bytes': size, 'label': label,
                       'explicit_reads': [x for x in reads if overlap(x)],
                       'explicit_writes': [x for x in writes if overlap(x)],
                       'template_values': dict(Counter(str(int.from_bytes(b[offset:offset+size], 'little'))
                                                       for b in templates.get(family, []))) if size <= 4 else None,
                       'scope': 'Literal typed catalogue accesses only. Template copies are not semantic decoding.'})

    strings = {}
    for p in (SOURCE / 'Source/Sources/Datas').glob('Texts_*.c'):
        rel = p.relative_to(SOURCE).as_posix()
        for no, line in enumerate(read(rel).splitlines(), 1):
            m = re.search(r'char\s+(\w+)\[\]\s*=\s*"((?:[^"\\]|\\.)*)"', line)
            if m:
                strings[m[1]] = {'text': m[2], 'source': rel, 'line': no}
    runtime = json.loads((ROOT / 'variable-runtime-map.json').read_text(encoding='utf-8'))['rows']
    variable_file = 'Source/Sources/Datas/Datas_Map.c'
    variables = []
    for no, line in enumerate(read(variable_file).splitlines(), 1):
        m = re.search(r'\{(0x[0-9A-Fa-f]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),0,0,(szMapVar\w+),NULL\}', line)
        if not m or int(m[1], 16) == 0xFFFFFFFF:
            continue
        typ = int(m[1], 16)
        baseline = runtime.get(str(typ))
        variables.append({'type': typ, 'type_hex': hex(typ), 'default_expression': m[2].strip(),
                          'scaled_ui_flag': m[3].strip(), 'minimum_expression': m[4].strip(),
                          'maximum_expression': m[5].strip(), 'symbol': m[6],
                          'label': strings[m[6]], 'table_source': variable_file,
                          'table_line': no, 'current_runtime': baseline,
                          'status': 'External editor label and defaults; gameplay meaning not newly proved.'})

    code = []
    for p in (SOURCE/'Source').rglob('*'):
        if p.is_file() and p.suffix.lower() in {'.c', '.h', '.rc', '.htm', '.html'}:
            rel = p.relative_to(SOURCE).as_posix()
            code.append((rel, read(rel)))
    keywords = {}
    for label, pattern in {
        'crypt': r'\bcrypt\b', 'sacrifice': r'sacrif',
        'work_modifier': r'work.?mod|work.?enhanc',
        'jump': r'\bjump\b', 'pickup_priority': r'pick.?up.?priority',
        'lightning_unsupported': r'ImmuneLightning', 'emotionless_unsupported': r'Emotionless',
    }.items():
        hits = []
        for rel, content in code:
            for no, line in enumerate(content.splitlines(), 1):
                if re.search(pattern, line, flags=re.I):
                    hits.append({'source': rel, 'line': no,
                                 'line_sha256': hashlib.sha256(line.strip().encode('utf-8')).hexdigest()})
        keywords[label] = hits
    package = ROOT / 'editor-format-package.json'
    package_data = json.loads(package.read_text(encoding='utf-8'))
    exposed = {x['type'] for x in variables}
    runtime_not_exposed = [v for k, v in runtime.items() if int(k) not in exposed]
    disabled_variables = [x for x in variables if x['scaled_ui_flag'] != '0']
    width_differences = []
    for x in writes:
        layout = package_data['effective_catalogue_layouts'].get(x['family'])
        if not layout:
            continue
        fields = [f for f in layout['fields'] if f['offset'] == x['offset']]
        for f in fields:
            if f['size'] != x['bytes']:
                width_differences.append({**x, 'current_field': f['name'],
                                          'current_bytes': f['size'], 'current_kind': f['kind'],
                                          'status': 'Different literal access width; manual field/subrecord/bitmask review required.'})
    out = {'date': '2026-11-01', 'repository': 'https://github.com/NovFR/DK2-Map-Editor',
           'commit': COMMIT, 'source_root': str(SOURCE),
           'provenance': {'verified_blobs': len(manifest), 'verified_bytes': sum(x['bytes'] for x in manifest),
                          'manifest': 'source-references/novfr-122d2ee3/source-manifest.json'},
           'template_manifest': template_manifest,
           'counts': {'literal_typed_catalogue_writes': len(writes),
                      'literal_typed_reset_reads': len(reads),
                      'writer_families': dict(Counter(x['family'] for x in writes)),
                      'global_variable_ui_rows': len(variables),
                      'unknown_runtime_names_with_french_label': sum(bool(x['current_runtime']) and 'UNKNOWN' in x['current_runtime']['baseline_name'] for x in variables),
                      'text_files_keyword_scanned': len(code)},
           'catalogue_writes': writes, 'catalogue_reset_reads': reads,
           'width_differences_to_current_layout': width_differences,
           'open_field_review': review, 'global_variable_labels': variables,
           'runtime_variable_types_not_exposed': runtime_not_exposed,
           'display_only_variable_types': [x['type'] for x in disabled_variables],
           'trap_disarmable_export': {
               'template_flag_offset': 0x1EC, 'disarmable_mask': 0x10,
               'clear_mask': 0x01 | 0x02 | 0x20,
               'clear_statement_line': 2288, 'set_statement_line': 2294,
               'source': writer_file,
               'templates_with_flag_set': [
                   {'index': i, 'name': b[:32].split(b'\0', 1)[0].decode('ascii', errors='replace'),
                    'flags_byte': b[0x1EC]} for i, b in enumerate(templates['Traps']) if b[0x1EC] & 0x10],
               'conclusion': 'Static source: false Disarmable cannot clear pre-existing template bit0x10 in this writer; no compiled GUI replay.'},
           'keyword_evidence': keywords,
           'resistance_labels': [{'symbol': k, **v} for k, v in strings.items() if re.fullmatch(r'szResistance[0-9A-F]{2}', k)],
           'package_preserved': {'sha256': hashlib.sha256(package.read_bytes()).hexdigest(),
                                 'components': len(package_data['components'])},
           'limitations': ['Source interpretation is evidence for NovFR behavior only, not game runtime.',
                           'No game data, EXE, parallel editor, or authoritative schema changed.',
                           'No GUI execution and no newly composed native-game tests.',
                           'Literal offset extraction excludes dynamic-index accesses; reviewed separately.',
                           'Negative search applies only to this pinned source snapshot.']}
    dump(ROOT / 'novfr-source-review.json', out)
    print(json.dumps(out['counts'], ensure_ascii=False))
    for x in variables:
        if x['current_runtime'] and 'UNKNOWN' in x['current_runtime']['baseline_name']:
            print(x['type_hex'], x['current_runtime']['baseline_name'], x['label']['text'])
    for x in review:
        print(x['family'],x['offset_hex'],len(x['explicit_reads']),len(x['explicit_writes']))


if __name__ == '__main__':
    main()
