"""Exact Threat WORD edits in complete Creature containers; sources read-only."""
from pathlib import Path
import hashlib
import json
import struct
import dk2_formats as fmt
from native_threat import patch_threat

ROOT = Path(__file__).resolve().parent


def main():
    sources = json.loads((ROOT/'creature-lookup-review.json').read_text(encoding='utf8'))['corpus']['files']
    rows, views, patches, changed = [], 0, 0, 0
    aggregate = hashlib.sha256()
    for row in sources:
        data = Path(row['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['sha256']
        parts = fmt.parse(data)
        assert b''.join(s.pack() for s in parts) == data
        hits = [(i, s) for i, s in enumerate(parts) if s.signature == 170]
        assert hits
        for _, section in hits:
            for raw in section.records:
                assert len(raw) in [5449, 5537]
                value = int.from_bytes(raw[0xF12:0xF14], 'little')
                assert patch_threat(raw, value) == raw
                views += 1
        si, section = hits[0]
        raw = section.records[0]
        for value in [0, 100, 65535, int.from_bytes(raw[0xF12:0xF14], 'little')]:
            edited = patch_threat(raw, value)
            assert edited[:0xF12] == raw[:0xF12] and edited[0xF14:] == raw[0xF14:]
            records = list(section.records)
            records[0] = edited
            clone = fmt.Section(section.offset, section.signature, section.header, list(section.records), section.suffix)
            clone.replace_records(records)
            packed = b''.join(clone.pack() if i == si else s.pack() for i, s in enumerate(parts))
            want = bytearray(data)
            struct.pack_into('<H', want, section.offset+len(section.header)+0xF12, value)
            assert packed == want and fmt.parse(packed)[si].records == records
            aggregate.update(hashlib.sha256(packed).digest())
            patches += 1
            changed += int(packed != data)
        rows.append(dict(file=row['file'], sha256=row['sha256'], records=sum(len(s.records) for _, s in hits)))
    refusals = 0
    for fn in [lambda: patch_threat(bytes(5448), 1), lambda: patch_threat(bytes(5449), -1),
               lambda: patch_threat(bytes(5449), 65536), lambda: patch_threat(bytes(5449), True),
               lambda: patch_threat(bytearray(5449), 1)]:
        try:
            fn()
        except ValueError:
            refusals += 1
        else:
            raise AssertionError('Expected API refusal')
    assert len(rows) == 82 and views == 3734 and patches == 328 and refusals == 5
    output = dict(files=rows, source_files=82, identity_record_views=views,
                  exact_container_rebuilds=patches, changed_containers=changed,
                  identity_containers=patches-changed, API_refusals=refusals,
                  ordered_patch_digest=aggregate.hexdigest(),
                  scope='Four edits onfirstCreature record per82files,includingidentity;3734records unchangedidentity views. '
                        'Allheaders,order,otherrecords/sections andopaqueextension bytes preserved. No sourcewrites.')
    (ROOT/'phase106-threat-api-validation.json').write_text(json.dumps(output, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in output.items() if k != 'files'}))


if __name__ == '__main__':
    main()
