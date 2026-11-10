"""Inventory original resource types and test exact selected container patches."""
from pathlib import Path
from collections import Counter
import hashlib
import json
import struct
import dk2_formats as fmt
from native_element_model_flags import patch_resource_bit200

ROOT = Path(__file__).resolve().parent


def main():
    rows = json.loads((ROOT / 'element-initialization-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    counts, file_counts, files, examples = Counter(), Counter(), [], {}
    patches = changed_count = 0
    for row in rows:
        data = Path(row['file']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['sha256']
        sections = fmt.parse(data)
        assert b''.join(s.pack() for s in sections) == data
        local, selected = Counter(), None
        for si, section in enumerate(sections):
            if section.signature != 0xFA:
                continue
            for ri, raw in enumerate(section.records):
                assert len(raw) == 182 and raw[0xB1:0xB3] == bytes(2)
                kind = raw[0x70]
                local[kind] += 1
                examples.setdefault(str(kind), dict(file=row['file'], record_index=ri,
                                    name=raw[:32].split(bytes(1))[0].decode('ascii', 'replace'),
                                    resource=raw[32:96].split(bytes(1))[0].decode('ascii', 'replace')))
                if selected is None and kind in (4, 5, 6):
                    selected = (si, ri, raw)
        assert selected is not None
        counts.update(local)
        file_counts.update(local.keys())
        si, ri, raw = selected
        section = sections[si]
        for enabled in [False, True]:
            patched = patch_resource_bit200(raw, enabled)
            want = bytearray(data)
            start = section.offset + len(section.header) + ri*182
            want[start:start+182] = patched
            records = list(section.records)
            records[ri] = patched
            clone = fmt.Section(section.offset, section.signature, section.header,
                                list(section.records), section.suffix)
            clone.replace_records(records)
            packed = b''.join(clone.pack() if i == si else s.pack() for i, s in enumerate(sections))
            assert packed == bytes(want) and fmt.parse(packed)[si].records == records
            assert patched[:0x60] == raw[:0x60] and patched[0x64:] == raw[0x64:]
            assert struct.unpack_from('<I', patched, 0x60)[0] ^ struct.unpack_from('<I', raw, 0x60)[0] in (0, 0x200)
            patches += 1
            changed_count += int(packed != data)
        files.append(dict(file=row['file'], sha256=row['sha256'], resource_type_counts=dict(local),
                          selected_section=si, selected_record=ri))
    assert sum(counts.values()) == 48416 and len(files) == 82 and patches == 164 and changed_count == 82
    synthetic = bytearray([0xA5])*182
    synthetic[0x70] = 4
    for enabled in [False, True]:
        patched = patch_resource_bit200(synthetic, enabled)
        assert patched[:0x60] == synthetic[:0x60] and patched[0x64:] == synthetic[0x64:]
        assert patched[0xB1:0xB3] == bytes([0xA5, 0xA5])
    failures = 0
    for raw, enabled in [(bytes(181), True), (bytes(182), True), (synthetic, 1)]:
        try:
            patch_resource_bit200(raw, enabled)
        except ValueError:
            failures += 1
        else:
            raise AssertionError('Expected refusal')
    value = dict(phase=103, files=files, records=sum(counts.values()),
                 resource_type_counts=dict(counts), resource_type_file_counts=dict(file_counts),
                 nonzero_palette_records=0, examples=examples,
                 exact_container_rebuild_cases=patches, changed_containers=changed_count,
                 identity_containers=patches-changed_count, synthetic_nonzero_opaque_checks=2,
                 strict_API_refusals=failures,
                 scope='Two bit200 states on first model record per82 file;164 rebuild cases include82 identity results. '
                       'Only selected raw bit is changed; all headers, other records and opaque bytes preserved. No source writes.')
    (ROOT / 'phase103-resource-corpus.json').write_text(json.dumps(value, indent=2), encoding='utf8')
    print(json.dumps({k: v for k, v in value.items() if k not in ['files', 'examples']}))


if __name__ == '__main__':
    main()
