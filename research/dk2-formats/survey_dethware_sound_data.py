"""Record the installed background graphs and sound-field value distributions."""
from collections import Counter
from dataclasses import asdict
import gzip
import hashlib
import json
from pathlib import Path
import struct

from dk2_sound_index import read_bank, read_sdt, read_sfx, text

ROOT = Path(__file__).resolve().parent
SFX = ROOT.parents[1] / 'Dungeon Keeper 2/Data/Sound/Sfx'
CATEGORIES = ('music', 'ambience', 'one_shot_atmos', 'options_music')


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def group_record(group):
    data = dict(offset=group.offset, raw=group.raw.hex(), id=group.id, items=[])
    for item in group.items:
        data['items'].append(dict(
            offset=item.offset, raw=item.raw.hex(),
            sounds=[dict(zip(('index', 'cumulative_weight_DWORD', 'duration_ms', 'bank'),
                             struct.unpack('<4I', raw))) for raw in item.sounds],
            links=[dict(zip(('target', 'opaque_4', 'opaque_5', 'min_intensity', 'max_intensity'),
                            struct.unpack('<I4B', raw))) for raw in item.links],
        ))
    return data


def survey():
    graphs = {}
    archives = {}
    for category in CATEGORIES:
        bank_path = SFX / 'Global' / (category + 'BANK.map')
        map_path = SFX / 'Global' / (category + 'SFX.map')
        bank_raw, map_raw = bank_path.read_bytes(), map_path.read_bytes()
        header, bank_rows = read_bank(bank_raw)
        effect_header, entries, groups = read_sfx(map_raw)
        names = [text(raw).replace('\\', '/') for _, raw in bank_rows]
        graphs[category] = dict(
            BANK_sha256=digest(bank_raw), SFX_sha256=digest(map_raw),
            BANK_header=header.hex(), BANK_opaque_rows=[raw.hex() for raw, _ in bank_rows],
            BANK_name_bytes=[raw.hex() for _, raw in bank_rows],
            bank_names=names, SFX_header=effect_header.hex(),
            entries=[raw.hex() for raw in entries], groups=[group_record(group) for group in groups],
        )
        packed_bank = header + b''.join(row for row, _ in bank_rows)
        packed_bank += b''.join(struct.pack('<I', len(name)) + name for _, name in bank_rows)
        assert packed_bank == bank_raw
        packed_sfx = effect_header + b''.join(entries) + b''.join(group.raw for group in groups)
        for group in groups:
            packed_sfx += b''.join(item.raw for item in group.items)
            for item in group.items:
                packed_sfx += b''.join(item.sounds) + b''.join(item.links)
        assert packed_sfx == map_raw
        for base in names:
            archive_name = base + 'HD.sdt'
            if archive_name in archives:
                continue
            raw = (SFX / archive_name).read_bytes()
            clips = read_sdt(raw)
            archives[archive_name] = dict(sha256=digest(raw), clips=[asdict(clip) for clip in clips])
    stats = Counter()
    group_flags = Counter()
    range_examples = []
    opaque = {'entry4_12': Counter(), 'group_priority': Counter(), 'group_slot0': Counter(),
              'item14_16': Counter(), 'item22_float': Counter(), 'item20_WORD': Counter()}
    for path in sorted(SFX.rglob('*SFX.map')):
        _, entries, groups = read_sfx(path.read_bytes())
        for raw in entries:
            opaque['entry4_12'][raw[4:12].hex()] += 1
        for group in groups:
            group_flags[struct.unpack_from('<H', group.raw, 16)[0]] += 1
            opaque['group_priority'][struct.unpack_from('<I', group.raw, 12)[0]] += 1
            opaque['group_slot0'][group.raw[18]] += 1
            stats['groups'] += 1
            for index, item in enumerate(group.items, 1):
                stats['items'] += 1
                opaque['item14_16'][item.raw[20:22].hex()] += 1
                opaque['item22_float'][item.raw[34:38].hex()] += 1
                opaque['item20_WORD'][struct.unpack_from('<H', item.raw, 32)[0]] += 1
                if item.raw[12] > item.raw[13]:
                    stats['reversed_volume_range'] += 1
                low, high = struct.unpack_from('<2b', item.raw, 14)
                if low > high:
                    stats['reversed_signed_range'] += 1
                    range_examples.append(dict(file=path.relative_to(SFX).as_posix(), group=group.id, item=index, minimum=low, maximum=high))
                weights = [struct.unpack_from('<I', raw, 4)[0] for raw in item.sounds]
                stats['sounds'] += len(weights)
                stats['sound_weights_with_high16'] += sum(weight > 65535 for weight in weights)
                if len(weights) > 1:
                    stats['multi_sound_items'] += 1
                    if weights[-1] < 65535:
                        stats['multi_sound_final_weight_below65535'] += 1
                stats['links'] += len(item.links)
    summary = dict(
        category_names=list(CATEGORIES), graph_groups=sum(len(value['groups']) for value in graphs.values()),
        graph_items=sum(len(group['items']) for value in graphs.values() for group in value['groups']),
        background_archives=len(archives), exact_background_map_roundtrips=8, all_SFX_counts=dict(stats), reversed_signed_ranges=range_examples,
        group_flag_counts={f'0x{k:04x}': v for k, v in sorted(group_flags.items())},
        opaque_value_counts={key: {str(k): v for k, v in sorted(counts.items())}
                             for key, counts in opaque.items()},
    )
    return dict(graphs=graphs, archives=archives, summary=summary)


def main():
    report = survey()
    raw = json.dumps(report, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode('utf8')
    path = ROOT / 'sound-background-graphs.json.gz'
    path.write_bytes(gzip.compress(raw, compresslevel=9, mtime=0))
    assert json.loads(gzip.decompress(path.read_bytes())) == report
    summary = report['summary']
    (ROOT / 'dethware-local-sound-survey.json').write_text(
        json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print(json.dumps({k: v for k, v in summary.items() if k not in ('opaque_value_counts', 'group_flag_counts')}))
    print(path.name, len(path.read_bytes()), 'bytes')


if __name__ == '__main__':
    main()

