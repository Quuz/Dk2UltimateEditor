"""Exact stat4/scalar225 edits across accepted Variables containers; no writes."""
from pathlib import Path
import hashlib
import json
import struct
import dk2_formats as fmt
from native_possession_damage import patch_stat4_value, patch_scalar225

ROOT = Path(__file__).resolve().parent


def main():
    sources = json.loads((ROOT/'phase88-sacrifice-corpus.json').read_text(encoding='utf8'))['files']
    counts = {'65/4': 0, '224/4': 0, '225': 0}
    files, patches, changed = [], 0, 0
    for row in sources:
        data = Path(row['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['sha256']
        sections = fmt.parse(data)
        assert b''.join(s.pack() for s in sections) == data
        selected = 0
        for si, section in enumerate(sections):
            if section.signature != 0xE6:
                continue
            for ri, raw in enumerate(section.records):
                kind, word1 = struct.unpack_from('<2I', raw)
                if kind in (65, 224) and word1 == 4:
                    key, offset, patch = f'{kind}/4', 8, patch_stat4_value
                elif kind == 225:
                    key, offset, patch = '225', 4, patch_scalar225
                else:
                    continue
                counts[key] += 1
                selected += 1
                for value in [0, 0xABCD003C, 0xFFFFFFFF]:
                    edited = patch(raw, value)
                    assert edited[:offset] == raw[:offset] and edited[offset+4:] == raw[offset+4:]
                    want = bytearray(data)
                    position = section.offset+len(section.header)+ri*16+offset
                    struct.pack_into('<I', want, position, value)
                    records = list(section.records)
                    records[ri] = edited
                    clone = fmt.Section(section.offset, section.signature, section.header,
                                        list(section.records), section.suffix)
                    clone.replace_records(records)
                    packed = b''.join(clone.pack() if i == si else s.pack() for i, s in enumerate(sections))
                    assert packed == bytes(want) and fmt.parse(packed)[si].records == records
                    patches += 1
                    changed += int(packed != data)
        files.append(dict(file=row['file'], sha256=row['sha256'], selected_records=selected))
    refusals = 0
    for fn in [lambda: patch_stat4_value(bytes(15), 0),
               lambda: patch_stat4_value(struct.pack('<4I', 224, 21, 1, 2), 0),
               lambda: patch_stat4_value(struct.pack('<4I', 65, 4, 125, 2), -1),
               lambda: patch_stat4_value(struct.pack('<4I', 224, 4, 60, 10), 1 << 32),
               lambda: patch_scalar225(struct.pack('<4I', 210, 100, 1, 2), 0),
               lambda: patch_scalar225(struct.pack('<4I', 225, 100, 1, 2), True)]:
        try:
            fn()
        except ValueError:
            refusals += 1
        else:
            raise AssertionError('Expected refusal')
    assert len(files) == 557 and patches == 3*sum(counts.values()) and refusals == 6
    out = dict(phase=104, files=files, file_count=len(files), selected_records=sum(counts.values()),
               records_by_kind=counts, exact_container_rebuilds=patches,
               changed_containers=changed, identity_containers=patches-changed,
               strict_API_refusals=refusals,
               scope='Three DWORDvalue patches per selected record;complete containers/headers/order/opaque bytes preserved. '
                     'Earlier24 strict-refusal files not repaired or declared damaged. No source writes.')
    (ROOT/'phase104-damage-api-validation.json').write_text(json.dumps(out, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in out.items() if k != 'files'}))


if __name__ == '__main__':
    main()
