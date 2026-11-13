"""Audit the declared lengths of the alternate Russian SDT corpus in memory."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct

from dk2_sound_index import SDT_HEADER, read_sdt

ROOT = Path(__file__).resolve().parent


def inspect(root):
    baseline = json.loads((ROOT / 'sound-index-validation.json').read_text(encoding='utf8'))['alternate']
    results = []
    rates = Counter()
    for issue in baseline['issues']:
        name = issue['file']
        if not name.endswith('.sdt'):
            continue
        raw = (root / name).read_bytes()
        sha = hashlib.sha256(raw).hexdigest()
        assert sha == baseline['file_sha256'][name], name
        count, = struct.unpack_from('<I', raw)
        offsets = struct.unpack_from(f'<{count}I', raw, 4)
        ends = offsets[1:] + (len(raw),)
        candidate = bytearray(raw)
        frames = 0
        for offset, end in zip(offsets, ends):
            header_size, declared, _, rate, bits, kind, _, _, _ = SDT_HEADER.unpack_from(raw, offset)
            payload = raw[offset + header_size:end]
            assert (header_size, bits, kind) == (40, 16, 36)
            assert declared == len(payload) + 40
            count_frames, trailer = divmod(len(payload), 720)
            assert count_frames > 0 and trailer == 1 and payload[-1:] == b'\0'
            for start in range(0, count_frames * 720, 720):
                header = int.from_bytes(payload[start:start + 4], 'big')
                assert header >> 9 == 0xfffd98c4 >> 9 and (header >> 6) & 3 == 3
            rates[rate] += 1
            frames += count_frames
            struct.pack_into('<I', candidate, offset + 4, len(payload))
        clips = read_sdt(candidate)
        assert len(clips) == count
        for clip, end in zip(clips, ends):
            assert clip.offset + clip.header_size + clip.data_size == end
            begin = clip.offset + 4
            candidate[begin:begin + 4] = raw[begin:begin + 4]
        assert candidate == raw
        results.append(dict(file=name, sha256=sha, records=count, full_frames=frames))
    assert len(results) == 32 and sum(x['records'] for x in results) == 3385
    return dict(
        scope='Russian SDTs from phase 204, header-only correction and inverse tested in memory',
        MPEG_version=1, MPEG_layer=2, MPEG_sample_rate_hz=32000, MPEG_bitrate_kbps=160,
        SDT_nominal_sample_rate_counts=dict(rates), size_field_overstatement_bytes=40,
        trailing_zero_bytes_per_payload=1, total_records=sum(x['records'] for x in results),
        full_frames=sum(x['full_frames'] for x in results), exact_inverse=True,
        files=results, game_playback_tested=False, game_files_written=0)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path, help='Data/Sound/Sfx  rus directory')
    args = parser.parse_args()
    report = inspect(args.root)
    path = ROOT / 'sound-russian-sdt-length-audit.json'
    path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf8')
    print(len(report['files']), 'archives,', report['total_records'], 'records with exact inverse')


if __name__ == '__main__':
    main()

