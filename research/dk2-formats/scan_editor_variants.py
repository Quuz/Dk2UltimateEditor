"""Compare the exact supplied stock 2.0 and fan Pro editor PE files.

Read-only source analysis. Resource leaves are compared by type/name/language,
not by their relocated file offsets. Code differences are exhaustively bounded;
unchanged code is not treated as an independent implementation or a bug fix.
"""
from pathlib import Path
from collections import Counter
import hashlib
import itertools
import json
import struct
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import pe_read
from extract_editor_dialogs import extract

ROOT = Path(__file__).resolve().parent
EDITOR = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor'
PROFILES = {
    'stock': ('DK2 Editor 2000.exe', 'f5cbbb05b3f78a1d13d0ff0c9600e6e4b231f7a81c3817cafd22368dcb175125'),
    'stock_alias': ('DK2 Editor.exe', 'f5cbbb05b3f78a1d13d0ff0c9600e6e4b231f7a81c3817cafd22368dcb175125'),
    'pro': ('DK2 Editor pro.exe', '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'),
}
# Original instruction starts, not byte-offset search guesses.
DIALOGS = [
    ('Creature attacks', 0x434E98, 323, 231),
    ('Creature first person', 0x4392A8, 324, 310),
    ('Creatures', 0x43CD41, 322, 246),
    ('CreatureSpells', 0x442C48, 328, 257),
    ('Doors', 0x44621F, 325, 254),
    ('EffectElements', 0x44B738, 332, 260),
    ('Effects', 0x44DE88, 331, 261),
    ('KeeperSpells', 0x450A30, 329, 258),
    ('Objects', 0x459296, 327, 256),
    ('Rooms', 0x465FC5, 321, 251),
    ('Shots', 0x4695E6, 330, 259),
    ('Terrain', 0x46CE10, 320, 143),
    ('Traps', 0x470046, 326, 255),
]


def sha(b):
    return hashlib.sha256(b).hexdigest()


def read_profiles():
    out = {}
    for key, (filename, expected) in PROFILES.items():
        blob = (EDITOR / filename).read_bytes()
        assert sha(blob) == expected, filename
        out[key] = blob
    assert out['stock_alias'] == out['stock']
    return out


def resource_leaves(pe):
    out = {}
    def walk(node, path):
        for entry in node.entries:
            key = str(entry.name) if entry.name is not None else entry.id
            child = path + (key,)
            if hasattr(entry, 'directory'):
                walk(entry.directory, child)
            else:
                value = entry.data.struct
                out[child] = pe.get_data(value.OffsetToData, value.Size)
                assert len(out[child]) == value.Size
    walk(pe.DIRECTORY_ENTRY_RESOURCE, ())
    return out


def instructions(blob, address):
    rows = list(Cs(CS_ARCH_X86, CS_MODE_32).disasm(blob, address))
    assert sum(i.size for i in rows) == len(blob)
    return [dict(address=i.address, hex=i.bytes.hex(), text=i.mnemonic+' '+i.op_str) for i in rows]


def dialog_summary(value):
    controls = value['controls']
    return dict(title=value['title'], controls=len(controls),
                template_visible=sum(c['template_visible'] for c in controls),
                template_disabled=sum(c['template_disabled'] for c in controls))


def build():
    profiles = read_profiles()
    stock, pro = profiles['stock'], profiles['pro']
    pa, pb = pefile.PE(data=stock), pefile.PE(data=pro)
    assert len(stock) == len(pro) == 1593344
    assert pa.OPTIONAL_HEADER.ImageBase == pb.OPTIONAL_HEADER.ImageBase == 0x400000
    assert pa.FILE_HEADER.TimeDateStamp == pb.FILE_HEADER.TimeDateStamp == 966424098
    assert pa.OPTIONAL_HEADER.AddressOfEntryPoint == pb.OPTIONAL_HEADER.AddressOfEntryPoint == 0xAD61C
    sections = []
    for a, b in zip(pa.sections, pb.sections):
        assert a.Name == b.Name and a.VirtualAddress == b.VirtualAddress
        assert a.PointerToRawData == b.PointerToRawData and a.SizeOfRawData == b.SizeOfRawData
        sections.append(dict(name=a.Name.rstrip(b'\0').decode('ascii'),
            address=0x400000+a.VirtualAddress, raw_offset=a.PointerToRawData,
            raw_bytes=a.SizeOfRawData, stock_sha256=sha(a.get_data()), pro_sha256=sha(b.get_data()),
            different_bytes=sum(x != y for x, y in zip(a.get_data(), b.get_data()))))
    changed = [i for i, (a, b) in enumerate(zip(stock, pro)) if a != b]
    runs = []
    for _, group in itertools.groupby(enumerate(changed), lambda row: row[1]-row[0]):
        offsets = [v for _, v in group]
        offset, n = offsets[0], len(offsets)
        section = next((s for s in pa.sections if s.PointerToRawData <= offset < s.PointerToRawData+s.SizeOfRawData), None)
        label = section.Name.rstrip(b'\0').decode('ascii') if section else 'headers'
        row = dict(file_offset=offset, length=n, section=label)
        if label != '.rsrc':
            row.update(stock_hex=stock[offset:offset+n].hex(), pro_hex=pro[offset:offset+n].hex())
            if section:
                row['address'] = 0x400000+section.VirtualAddress+offset-section.PointerToRawData
        runs.append(row)
    assert len(changed) == 161421 and len(runs) == 91184
    assert Counter(r['section'] for r in runs) == Counter({'.rsrc': 91166, '.text': 14, '.data': 2, 'headers': 2})
    leaves_a, leaves_b = resource_leaves(pa), resource_leaves(pb)
    assert leaves_a.keys() == leaves_b.keys() and len(leaves_a) == 281
    changed_leaves = [k for k in leaves_a if leaves_a[k] != leaves_b[k]]
    assert changed_leaves == [(5, 100, 2057)]
    dialogs_a = {tuple(d['resource_path']): d for d in extract(stock)}
    dialogs_b = {tuple(d['resource_path']): d for d in extract(pro)}
    assert dialogs_a.keys() == dialogs_b.keys() and len(dialogs_a) == 115
    assert [k for k in dialogs_a if dialogs_a[k] != dialogs_b[k]] == [(5, 100, 2057)]
    switches = []
    allowed_text_bytes = set()
    for family, address, old_id, new_id in DIALOGS:
        old, new = pe_read(stock, address, 5), pe_read(pro, address, 5)
        assert old == b'\x68'+struct.pack('<I', old_id)
        assert new == b'\x68'+struct.pack('<I', new_id)
        # Both alternative templates already exist unchanged in the stock file.
        old_path, new_path = (5, old_id, 2057), (5, new_id, 2057)
        assert leaves_a[old_path] == leaves_b[old_path]
        assert leaves_a[new_path] == leaves_b[new_path]
        switches.append(dict(family=family, instruction=address,
            stock_resource_id=old_id, pro_resource_id=new_id,
            stock_template=dialog_summary(dialogs_a[old_path]),
            pro_template=dialog_summary(dialogs_a[new_path]),
            stock_instruction=instructions(old, address), pro_instruction=instructions(new, address),
            selected_templates_already_in_stock=True))
        allowed_text_bytes.update(range(address+1, address+5))
    old = pe_read(stock, 0x492603, 9)
    new = pe_read(pro, 0x492603, 9)
    assert old.hex() == '6a008bcde80443ffff'
    assert new.hex() == 'e99100000090909090'
    allowed_text_bytes.update(range(0x492603, 0x49260C))
    text = next(s for s in pa.sections if s.Name.rstrip(b'\0') == b'.text')
    base = 0x400000+text.VirtualAddress
    actual_text_bytes = {base+i for i, (a, b) in enumerate(zip(text.get_data(), next(s for s in pb.sections if s.Name == text.Name).get_data())) if a != b}
    assert actual_text_bytes <= allowed_text_bytes and len(actual_text_bytes) == 28
    def cstring(data, address):
        raw = pe_read(data, address, 180).split(b'\0')[0]
        return raw.decode('cp1252')
    old_message, new_message = cstring(stock, 0x53A1E8), cstring(pro, 0x53A1E8)
    assert old_message == 'This filename is reserved for an Official DK2 Level.\n\nPlease select a different filename.'
    assert new_message == 'This filename is reserved for an Official DK2 Level.\n\nOriginal level will be overwritten.'
    allowed_data_bytes = set(range(0x53A21E, 0x53A240))
    assert all(set(range(r['address'], r['address']+r['length'])) <= allowed_data_bytes
               for r in runs if r['section'] == '.data')
    # Preserve original PE ranges: the surrounding branches establish that the
    # jump is after the warning, not a removal of the protected-name predicate.
    ranges = []
    for address, length, meaning in [(0x4925C3, 0xD6, 'Existing-name path through warning and abort arm'),
            (0x492699, 0x17, 'Shared continuation and subsequent independent check'),
            (0x48F6D0, 0xF0, 'Shared protected-name predicate; not dynamically exercised here')]:
        a, b = pe_read(stock, address, length), pe_read(pro, address, length)
        ranges.append(dict(address=address, length=length, description=meaning,
            stock_hex=a.hex(), pro_hex=b.hex(), stock_instructions=instructions(a, address),
            pro_instructions=instructions(b, address)))
    return dict(phase=117, profiles=[dict(key=k, file=str(EDITOR/n), sha256=h, bytes=len(profiles[k])) for k, (n,h) in PROFILES.items()],
        executable_metadata=dict(timestamp=966424098, image_base=0x400000, entry_point=0x4AD61C),
        sections=sections, different_file_bytes=len(changed), different_text_bytes=len(actual_text_bytes),
        raw_difference_runs=len(runs), raw_difference_runs_by_section=dict(Counter(r['section'] for r in runs)),
        nonresource_difference_runs=[r for r in runs if r['section'] != '.rsrc'],
        resource_leaves=[dict(path=list(k), stock_bytes=len(leaves_a[k]), pro_bytes=len(leaves_b[k]),
            stock_sha256=sha(leaves_a[k]), pro_sha256=sha(leaves_b[k]), equal=leaves_a[k]==leaves_b[k]) for k in leaves_a],
        dialogs=115, unchanged_dialogs=114, changed_resource_paths=[list(k) for k in changed_leaves],
        dialog_switches=switches,
        save_branch=dict(address=0x492603, stock=instructions(old, 0x492603), pro=instructions(new, 0x492603),
            predicate=0x48F6D0, warning_call=0x4925FE, shared_continuation=0x492699,
            stock_message=old_message, pro_message=new_message,
            scope='Static original instructions. On the warning arm Pro jumps to shared continuation instead of the stock return/cleanup arm. Warning still occurs. Later checks and actual saving not executed.'),
        primary_ranges=ranges,
        scope='Complete supplied PE byte comparison and all resource leaves; selected control-flow interpretation. No live GUI, native save, additional game execution or new raw-field meaning.')


if __name__ == '__main__':
    result = build()
    (ROOT/'phase117-editor-variant-evidence.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: result[k] for k in ['different_file_bytes','different_text_bytes','raw_difference_runs_by_section','dialogs','unchanged_dialogs']}, ensure_ascii=False))
