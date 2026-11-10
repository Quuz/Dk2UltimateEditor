"""Primary native Terrain composition spans, reusing the frozen common MFC manifest."""
from pathlib import Path
import hashlib, json, re, sys
import capstone
from build_effect_semantics import pe_read
import prepare_room_model_primary as common

ROOT = Path(__file__).resolve().parent
EXE = common.EXE
FRESH = [0x4A7690, 0x49F5E0, 0x4250D0, 0x425400, 0x425420, 0x4254C0, 0x425640]


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def seed():
    blob = EXE.read_bytes()
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    rows = []
    for address in FRESH:
        name = f'ghidra/phase128-terrain-{address:06x}-disassemble_function.txt'
        addresses = [int(match[1], 16) for match in re.finditer(r'^([0-9a-fA-F]{8}):', (ROOT / name).read_text(encoding='utf8'), re.M)]
        assert addresses and addresses[0] == address
        last = next(md.disasm(pe_read(blob, addresses[-1], 15), addresses[-1]))
        length = addresses[-1] + last.size - address
        assert [ins.address for ins in md.disasm(pe_read(blob, address, length), address)] == addresses
        rows.append(dict(address=address, length=length, assembly=name, assembly_sha256=sha((ROOT / name).read_bytes())))
    return rows


def build():
    blob = EXE.read_bytes()
    rows = []
    for row in seed():
        name = f'ghidra/phase128-primary-terrain-{row["address"]:06x}.txt'
        saved = load(name)
        data = bytes(saved['data'])
        assert int(saved['address'], 16) == row['address'] and len(data) == row['length']
        assert data == pe_read(blob, row['address'], row['length'])
        rows.append(dict(row, memory=name, memory_sha256=sha((ROOT / name).read_bytes()), original_bytes_sha256=sha(data)))
    converter = next(row for row in load('phase50-primary-manifest.json') if row['program'] == 'DK2 Editor pro.exe' and row['address'] == 0x42ED90)
    name = 'ghidra/' + converter['memory']
    data = bytes(load(name)['data'])
    assert len(data) == converter['length'] and data == pe_read(blob, 0x42ED90, converter['length'])
    return dict(phase=128, executable=str(EXE), sha256=sha(blob), fresh_functions=rows,
        inherited_converter=dict(address=0x42ED90, length=converter['length'], memory=name,
            original_bytes_sha256=sha(data), inherited_from='phase50-primary-manifest.json'),
        common_MFC_CRT=common.build(),
        scope='7fresh Terrain spans, exact inherited full converter and re-attested common MFC/CRT manifest. Some Room-specific inherited spans are dependency history, not newly executed Terrain code.')


if __name__ == '__main__':
    if '--queries' in sys.argv:
        qs = [dict(id=f'phase128-primary-terrain-{row["address"]:06x}', tool='read_memory',
            args=dict(program='DK2 Editor pro.exe', address=hex(row['address']), length=row['length']), print_limit=0) for row in seed()]
        (ROOT / 'phase128-primary-terrain-queries.json').write_text(json.dumps(qs, indent=2), encoding='utf8')
        print('Prepared', len(qs), 'whole Terrain spans')
    else:
        value = build()
        (ROOT / 'phase128-terrain-model-primary.json').write_text(json.dumps(value, indent=2), encoding='utf8')
        print('7fresh Terrain spans and inherited converter/common helpers attested')
