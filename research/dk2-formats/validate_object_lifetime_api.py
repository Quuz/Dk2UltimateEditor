"""Exact Object Flags0/1 andHP edits,includingopaqueFlags/unknownManaGen preservation."""
from pathlib import Path
import hashlib
import json
import struct
import dk2_formats as fmt
from native_object_lifetime import patch_decay_flags, patch_hp

ROOT = Path(__file__).resolve().parent


def main():
    source = json.loads((ROOT/'object-loading-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    rows, views, patches, changed = [], 0, 0, 0
    aggregate = hashlib.sha256()
    for row in source:
        data = Path(row['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['sha256']
        parts = fmt.parse(data)
        assert b''.join(s.pack() for s in parts) == data
        hits = [(i, s) for i, s in enumerate(parts) if s.signature == 0xF0]
        if not hits:
            continue
        for _, section in hits:
            for raw in section.records:
                assert len(raw) == 894
                flags, hp = struct.unpack_from('<IH', raw, 0x344)
                assert patch_decay_flags(raw, flags & 3) == patch_hp(raw, hp) == raw
                views += 1
        si, section = hits[0]
        raw = section.records[0]
        for fn, value, offset, width in [(patch_decay_flags, i, 0x344, 4) for i in range(4)]+[(patch_hp, i, 0x348, 2) for i in [0, 1, 65535]]:
            edited = fn(raw, value)
            assert edited[:offset] == raw[:offset] and edited[offset+width:] == raw[offset+width:]
            assert edited[0x34C:0x34E] == raw[0x34C:0x34E]
            if offset == 0x344:
                original = struct.unpack_from('<I', raw, offset)[0]
                assert struct.unpack_from('<I', edited, offset)[0] == (original&~3)|value
            want = bytearray(data)
            at = section.offset+len(section.header)+offset
            want[at:at+width] = edited[offset:offset+width]
            rr = list(section.records)
            rr[0] = edited
            clone = fmt.Section(section.offset, section.signature, section.header, list(section.records), section.suffix)
            clone.replace_records(rr)
            packed = b''.join(clone.pack() if i == si else s.pack() for i, s in enumerate(parts))
            assert packed == want and fmt.parse(packed)[si].records == rr
            aggregate.update(hashlib.sha256(packed).digest())
            patches += 1
            changed += int(packed != data)
        rows.append(dict(file=row['file'], sha256=row['sha256'], records=sum(len(s.records) for _, s in hits)))
    refuses = 0
    for fn in [lambda: patch_decay_flags(bytes(893), 1), lambda: patch_decay_flags(bytes(894), 4),
               lambda: patch_decay_flags(bytes(894), True), lambda: patch_hp(bytes(894), -1),
               lambda: patch_hp(bytes(894), 65536), lambda: patch_hp(bytearray(894), 1)]:
        try:
            fn()
        except ValueError:
            refuses += 1
        else:
            raise AssertionError('ExpectedAPIrefusal')
    assert len(rows) == 82 and views == 11756 and patches == 574 and refuses == 6
    out = dict(files=rows, source_files=82, source_record_identity_views=views, exact_container_rebuilds=patches,
               changed_containers=changed, identity_containers=patches-changed, API_refusals=refuses,
               ordered_patch_digest=aggregate.hexdigest(),
               scope='Flags mask3 only orHPWORD onlyonfirstObject record perfile;allotherFlags,unknownManaGen,opaque bytes/order/headers/sections preserved. No sourcewrites.')
    (ROOT/'phase108-object-lifetime-api-validation.json').write_text(json.dumps(out, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in out.items() if k != 'files'}))


if __name__ == '__main__':
    main()
