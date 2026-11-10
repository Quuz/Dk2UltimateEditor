"""Whole old-PE instruction spans and bounded classification of20direct leads."""
from pathlib import Path
import hashlib, json, re, sys
import capstone
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent
EXE = ROOT / 'source-binaries/DKII-local13.exe'
FUNCTIONS = [0x489BF0, 0x48A060, 0x48A150, 0x512FA0, 0x5538A0, 0x555130,
    0x55F170, 0x5600B0, 0x5600D0, 0x5600F0, 0x59D8D0, 0x59DA70,
    0x59DEB0, 0x63D34E, 0x63D42E, 0x64C6A0]
ROLES = {0x489BF0: 'Creature definition constructor', 0x48A060: 'Creature definition destructor',
    0x48A150: 'Creature definition loader', 0x512FA0: 'Creature definition copy; no damage arithmetic',
    0x5538A0: 'Resource/path manager construction context, not Creature damage',
    0x555130: 'Other single-DWORD initialization at61E, no resistance-table iteration',
    0x59D8D0: 'Other threaded media context, not Creature definition',
    0x59DA70: 'Other media-buffer context, not Creature definition',
    0x63D34E: 'Exception-unwind cleanup of four two-byte resistance objects',
    0x63D42E: 'Exception-unwind cleanup of four two-byte resistance objects',
    0x64C6A0: 'Unrolled pixel/memory fill, not Creature definition'}


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def seed():
    blob = EXE.read_bytes()
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    rows = []
    for address in FUNCTIONS:
        name = f'ghidra/phase126-old-{address:06x}-disassemble_function.txt'
        addresses = [int(match[1], 16) for match in re.finditer(r'^([0-9a-fA-F]{8}):', (ROOT / name).read_text(encoding='utf8'), re.M)]
        assert addresses and addresses[0] == address, hex(address)
        last = next(md.disasm(pe_read(blob, addresses[-1], 15), addresses[-1]))
        size = addresses[-1] + last.size - address
        assert [ins.address for ins in md.disasm(pe_read(blob, address, size), address)] == addresses, hex(address)
        rows.append(dict(address=address, length=size, assembly=link(name), instructions=len(addresses)))
    return rows


def build():
    blob = EXE.read_bytes()
    rows = []
    for row in seed():
        name = f'ghidra/phase126-primary-old-{row["address"]:06x}.txt'
        saved = json.loads((ROOT / name).read_text(encoding='utf8'))
        data = bytes(saved['data'])
        assert int(saved['address'], 16) == row['address'] and len(data) == row['length']
        assert data == pe_read(blob, row['address'], row['length'])
        rows.append(dict(row, memory=link(name), original_bytes_sha256=sha(data)))
    inventory = json.loads((ROOT / 'phase126-old-resistance-literal-leads.json').read_text(encoding='utf8'))
    leads = []
    for row in inventory['literal_leads']:
        owner = next(fn for fn in rows if fn['address'] <= row['address'] < fn['address'] + fn['length'])
        assert owner['address'] in ROLES
        text = (ROOT / owner['assembly']['file']).read_text(encoding='utf8')
        assert f'{row["address"]:08x}:' in text.lower()
        leads.append(dict(row, canonical_function=owner['address'], classification=ROLES[owner['address']]))
    assert len(leads) == 20
    return dict(phase=126, executable=str(EXE), executable_sha256=sha(blob),
        program='DKII-local13.exe', functions=rows, direct_leads=leads,
        conclusion='No damage-use consumer among these20direct operands. Adjusted/indexed pointers, other modules and all gameplay paths remain outside this finite search.',
        copy='Original512FA0 copies all2199definition bytes, including unknown resistance IDs, in3preparedCPUcases.',
        scope='16whole spans/20canonical leads; full old rawCreature loader is static here, not executed as a World chain.')


if __name__ == '__main__':
    if '--queries' in sys.argv:
        rows = seed()
        queries = [dict(id=f'phase126-primary-old-{row["address"]:06x}', tool='read_memory',
            args=dict(program='DKII-local13.exe', address=hex(row['address']), length=row['length']), print_limit=0) for row in rows]
        (ROOT / 'phase126-primary-old-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
        print('Prepared', len(rows), 'whole old-PE spans')
    else:
        value = build()
        (ROOT / 'phase126-old-primary-manifest.json').write_text(json.dumps(value, indent=2), encoding='utf8')
        print('Whole old-PE ranges and classified leads:', len(value['functions']), len(value['direct_leads']))
