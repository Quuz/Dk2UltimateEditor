"""Attest full Editor combat-dialog spans and native category/string tables."""
from pathlib import Path
import hashlib, json, re, struct, sys
import capstone
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent
EXE = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
FUNCTIONS = [0x442940, 0x463D20, 0x463DA0, 0x463F30, 0x463FF0, 0x4641E0,
    0x464210, 0x464310, 0x41E440, 0x41E450, 0x469820, 0x4699D0,
    0x46A090, 0x46A2A0, 0x46A2D0, 0x4356B0]
BLOCKS = [(0x512D28, 128, 'resistance-types'), (0x5138D0, 104, 'shot-categories'),
    (5450788, 352, 'shared-type-labels'), (0x512DA8, 200, 'resistance-message-map-full'),
    (0x50C318, 128, 'creature-attack-table')]


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def seed():
    blob = EXE.read_bytes()
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    rows = []
    for address in FUNCTIONS:
        name = f'ghidra/phase125-editor-{address:06x}-disassemble_function.txt'
        addresses = [int(m[1], 16) for m in re.finditer(r'^([0-9a-fA-F]{8}):', (ROOT / name).read_text(encoding='utf8'), re.M)]
        assert addresses and addresses[0] == address
        last = next(md.disasm(pe_read(blob, addresses[-1], 15), addresses[-1]))
        size = addresses[-1] + last.size - address
        assert [i.address for i in md.disasm(pe_read(blob, address, size), address)] == addresses
        rows.append(dict(address=address, length=size, assembly=link(name), instructions=len(addresses)))
    return rows


def build():
    blob = EXE.read_bytes()
    rows = []
    for row in seed():
        name = f'ghidra/phase125-primary-editor-{row["address"]:06x}.txt'
        saved = json.loads((ROOT / name).read_text(encoding='utf8'))
        native = bytes(saved['data'])
        assert int(saved['address'], 16) == row['address'] and len(native) == row['length']
        assert native == pe_read(blob, row['address'], row['length'])
        rows.append(dict(row, memory=link(name), original_bytes_sha256=sha(native)))
    blocks = []
    for address, length, tag in BLOCKS:
        name = f'ghidra/phase125-editor-{tag}.txt'
        saved = json.loads((ROOT / name).read_text(encoding='utf8'))
        native = bytes(saved['data'])
        assert int(saved['address'], 16) == address and len(native) == length
        assert native == pe_read(blob, address, length)
        blocks.append(dict(address=address, length=length, memory=link(name), original_bytes_sha256=sha(native)))
    return dict(phase=125, program='DK2 Editor pro.exe', executable=str(EXE), executable_sha256=sha(blob),
        functions=rows, blocks=blocks, scope='16 selected whole function spans and 5 original blocks; not full MFC/Win32 UI execution.')


if __name__ == '__main__':
    if '--queries' in sys.argv:
        queries = [dict(id=f'phase125-primary-editor-{row["address"]:06x}', tool='read_memory',
            args=dict(program='DK2 Editor pro.exe', address=hex(row['address']), length=row['length']), print_limit=0) for row in seed()]
        (ROOT / 'phase125-primary-editor-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
        print('Prepared', len(queries), 'whole function reads')
    else:
        value = build()
        (ROOT / 'phase125-primary-manifest.json').write_text(json.dumps(value, indent=2), encoding='utf8')
        print('Primary spans/blocks verified:', len(value['functions']), len(value['blocks']))
