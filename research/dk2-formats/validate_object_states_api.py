"""Exact StartState edits and state census over every known Object container."""
import hashlib
import json
from pathlib import Path
import dk2_formats as fmt
from native_object_states import patch_start_state, supplied_dx_state_status

ROOT = Path(__file__).resolve().parent


def main():
    source = json.loads((ROOT/'object-loading-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    rows, counts, views, patches, changed = [], {}, 0, 0, 0
    aggregate = hashlib.sha256()
    values = [0, 2, 16, 39, 40, 58, 59, 63, 255]
    for row in source:
        data = Path(row['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['sha256']
        parts = fmt.parse(data)
        assert b''.join(s.pack() for s in parts) == data
        hits = [(i, s) for i, s in enumerate(parts) if s.signature == 0xF0]
        if not hits:
            continue
        file_counts = {}
        for _, s in hits:
            for raw in s.records:
                value = raw[0x35B]
                assert patch_start_state(raw, value) == raw
                counts[str(value)] = counts.get(str(value), 0) + 1
                file_counts[str(value)] = file_counts.get(str(value), 0) + 1
                views += 1
        si, section = hits[0]
        raw = section.records[0]
        for value in values:
            edited = patch_start_state(raw, value)
            assert edited[:0x35B] == raw[:0x35B] and edited[0x35C:] == raw[0x35C:]
            want = bytearray(data)
            at = section.offset + len(section.header) + 0x35B
            want[at] = value
            records = list(section.records)
            records[0] = edited
            clone = fmt.Section(section.offset, section.signature, section.header, list(section.records), section.suffix)
            clone.replace_records(records)
            packed = b''.join(clone.pack() if i == si else s.pack() for i, s in enumerate(parts))
            assert packed == want and fmt.parse(packed)[si].records == records
            aggregate.update(hashlib.sha256(packed).digest())
            patches += 1
            changed += int(packed != data)
        rows.append(dict(file=row['file'], sha256=row['sha256'], records=sum(file_counts.values()), start_state_counts=file_counts))
    refused = 0
    for call in [lambda: patch_start_state(bytes(893), 0), lambda: patch_start_state(bytes(895), 0),
                 lambda: patch_start_state(bytes(894), -1), lambda: patch_start_state(bytes(894), 256),
                 lambda: patch_start_state(bytes(894), True), lambda: patch_start_state(bytearray(894), 0)]:
        try:
            call()
        except ValueError:
            refused += 1
        else:
            raise AssertionError('Expected API refusal')
    assert len(rows) == 82 and views == 11756 and patches == 738 and refused == 6
    assert not any(int(k) >= 59 for k in counts)
    assert len({supplied_dx_state_status(v) for v in range(256)}) == 5
    result = dict(files=rows, source_files=82, record_identity_views=views,
                  start_state_counts=counts, out_of_initialized_registry_records=0,
                  exact_container_rebuilds=patches, changed_containers=changed,
                  identity_containers=patches-changed, patch_values=values, API_refusals=refused,
                  ordered_patch_digest=aggregate.hexdigest(),
                  scope='All record identities; first Object record per source patched at35B only. '
                        'Low-level API preserves every byte value including unsupported states; classification is exact-DX only. '
                        'All other bytes/headers/order/ManaGen preserved; no source writes.')
    (ROOT/'phase110-object-state-api-validation.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in result.items() if k not in ['files', 'scope']}))


if __name__ == '__main__':
    main()
