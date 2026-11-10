"""Attest newly executed MFC/CRT resource helpers and inherited Room spans."""
from pathlib import Path
import hashlib, json, re, sys
import capstone
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent
EXE = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
FRESH = [0x4D8A63, 0x4AC4CB, 0x4AC4DD, 0x4AC509, 0x4AC3C8, 0x4AC457,
    0x4D854C, 0x4DDE57, 0x4DDC56, 0x4DDCD8, 0x4DDDE9, 0x41BB40,
    0x41C720, 0x41CAC0, 0x4DDF22, 0x4D5FB4, 0x4D60E9, 0x4D6183, 0x4D6191, 0x4DDF72]
INHERITED = {121: [0x41BB90, 0x41C0B0, 0x41CE60],
    122: [0x42DF90, 0x49FC30, 0x424780, 0x4A7AC0, 0x4AB320, 0x4AB3B0, 0x424B40, 0x4ACDA8]}


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def seed():
    blob = EXE.read_bytes()
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    rows = []
    for address in FRESH:
        name = f'ghidra/phase127-model-{address:06x}-disassemble_function.txt'
        addresses = [int(match[1], 16) for match in re.finditer(r'^([0-9a-fA-F]{8}):', (ROOT / name).read_text(encoding='utf8'), re.M)]
        assert addresses and addresses[0] == address
        last = next(md.disasm(pe_read(blob, addresses[-1], 15), addresses[-1]))
        length = addresses[-1] + last.size - address
        assert [ins.address for ins in md.disasm(pe_read(blob, address, length), address)] == addresses, hex(address)
        rows.append(dict(address=address, length=length, assembly=link(name), instructions=len(addresses)))
    return rows


def build():
    blob = EXE.read_bytes()
    fresh, inherited, blocks = [], [], []
    for row in seed():
        name = f'ghidra/phase127-primary-model-{row["address"]:06x}.txt'
        value = load(name)
        data = bytes(value['data'])
        assert int(value['address'], 16) == row['address'] and len(data) == row['length']
        assert data == pe_read(blob, row['address'], row['length'])
        fresh.append(dict(row, memory=link(name), original_bytes_sha256=sha(data)))
    for phase, addresses in INHERITED.items():
        previous = load(f'phase{phase}-primary-manifest.json')
        for address in addresses:
            row = next(row for row in previous['functions'] if row['address'] == address and row.get('profile', 'Pro') == 'Pro')
            name = row['memory']
            value = load(name)
            data = bytes(value['data'])
            assert len(data) == row['length'] and data == pe_read(blob, address, row['length'])
            inherited.append(dict(address=address, length=row['length'], inherited_phase=phase,
                memory=link(name), original_bytes_sha256=sha(data)))
    for address, length, name in [(0x50AC44, 4, 'phase127-model-resource-vtable.txt'),
            (0x53A7B4, 24, 'phase127-model-empty-string.txt')]:
        file = 'ghidra/' + name
        value = load(file)
        data = bytes(value['data'])
        assert len(data) == length and data == pe_read(blob, address, length)
        blocks.append(dict(address=address, length=length, memory=link(file), original_bytes_sha256=sha(data)))
    return dict(phase=127, executable=str(EXE), sha256=sha(blob), fresh_functions=fresh,
        inherited_functions=inherited, fresh_blocks=blocks,
        scope='31key whole native function spans,20new and11inherited;2new static data blocks. Doesnot claim all callees independently semantically decoded.')


if __name__ == '__main__':
    if '--queries' in sys.argv:
        rows = seed()
        queries = [dict(id=f'phase127-primary-model-{row["address"]:06x}', tool='read_memory',
            args=dict(program='DK2 Editor pro.exe', address=hex(row['address']), length=row['length']), print_limit=0) for row in rows]
        (ROOT / 'phase127-primary-model-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
        print('Prepared', len(rows), 'fresh whole function reads')
    else:
        value = build()
        (ROOT / 'phase127-Room-model-primary-manifest.json').write_text(json.dumps(value, indent=2), encoding='utf8')
        print('Key spans:', len(value['fresh_functions']), 'fresh/', len(value['inherited_functions']), 'inherited')
