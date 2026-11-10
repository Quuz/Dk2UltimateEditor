"""Survey all known Creature resistance bytes and test exact category-only rebuilds."""
from pathlib import Path
from collections import Counter
import hashlib, json
import dk2_formats as fmt
import native_combat_categories as api

ROOT = Path(__file__).resolve().parent


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def main():
    sources = json.loads((ROOT / 'creature-lookup-review.json').read_text(encoding='utf8'))['corpus']['files']
    types, values, pairs = Counter(), Counter(), Counter()
    records_seen = rebuilds = changed = 0
    aggregate = hashlib.sha256()
    for source in sources:
        data = Path(source['file']).read_bytes()
        assert sha(data) == source['sha256']
        sections = fmt.parse(data)
        assert b''.join(s.pack() for s in sections) == data
        hits = [(i, s) for i, s in enumerate(sections) if s.signature == 0xAA]
        for _, section in hits:
            for raw in section.records:
                for row in api.view(raw):
                    types[row['attack_type']] += 1
                    values[row['value_bits']] += 1
                    pairs[(row['attack_type'], row['value_bits'])] += 1
                    assert api.patch(raw, row['slot'], category=row['attack_type'], value=row['value_bits']) == raw
                records_seen += 1
        si, section = hits[0]
        raw = section.records[0]
        for slot in range(5):
            if slot < 4:
                category = [4, 6, 8, 15][slot]
                if raw[0xE0C + 2 * slot] == category:
                    category = 1
                edited = api.patch(raw, slot, category=category)
            else:
                edited = api.patch(raw, 0)
            records = list(section.records)
            records[0] = edited
            clone = fmt.Section(section.offset, section.signature, section.header, list(section.records), section.suffix)
            clone.replace_records(records)
            packed = b''.join(clone.pack() if index == si else s.pack() for index, s in enumerate(sections))
            wanted = bytearray(data)
            if slot < 4:
                wanted[section.offset + len(section.header) + 0xE0C + 2 * slot] = category
            assert packed == wanted and fmt.parse(packed)[si].records == records
            rebuilds += 1
            changed += packed != data
            aggregate.update(hashlib.sha256(packed).digest())
        assert sha(Path(source['file']).read_bytes()) == source['sha256']
    assert len(sources) == 82 and records_seen == 3734 and rebuilds == 410 and changed == 328
    assert sum(types.values()) == records_seen * 4
    result = dict(phase=125, files=sources, records=records_seen, resistance_slots=records_seen * 4,
        type_counts={str(k): v for k, v in sorted(types.items())},
        value_counts={str(k): v for k, v in sorted(values.items())},
        pair_counts=[dict(category=k[0], value=k[1], count=v) for k, v in sorted(pairs.items())],
        exact_container_rebuilds=rebuilds, changed_containers=changed, identity_containers=rebuilds - changed,
        ordered_patch_digest=aggregate.hexdigest(), source_writes=0,
        scope='Four category-only single-byte changes and one identity per source; no new field sizes or runtime formula inferred.')
    (ROOT / 'phase125-category-api-validation.json').write_text(json.dumps(result, indent=2), encoding='utf8')
    print('Sources/records/rebuilds:', len(sources), records_seen, rebuilds)
    print('Category counts:', dict(sorted(types.items())))
    print('Value counts:', dict(sorted(values.items())))


if __name__ == '__main__':
    main()
