"""Inspect the 32 SHA-pinned Russian SDTs. No decoding or game-file writes."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct


def inspect(root, baseline, reference_root=None):
    rows, rates, deltas = [], Counter(), Counter()
    for source in baseline['files']:
        raw = (root / source['file']).read_bytes()
        assert hashlib.sha256(raw).hexdigest() == source['sha256']
        count, = struct.unpack_from('<I', raw)
        offsets = struct.unpack_from(f'<{count}I', raw, 4)
        assert count == source['records']
        assert all(a < b for a, b in zip(offsets, offsets[1:]))
        assert offsets[0] >= 4 + 4 * count
        clips = []
        comparison = None
        if reference_root is not None and (reference_root / source['file']).is_file():
            stock = (reference_root / source['file']).read_bytes()
            stock_count, = struct.unpack_from('<I', stock)
            if stock_count == count:
                stock_offsets = struct.unpack_from(f'<{count}I', stock, 4)
                comparison = Counter()
        for index, (start, end) in enumerate(zip(offsets, offsets[1:] + (len(raw),)), 1):
            size, declared, name, rate, bits, kind, w1, pcm, w2 = struct.unpack_from('<II16sHBBIII', raw, start)
            assert size == 40 and declared == end - start and kind == 36 and bits == 16
            payload = raw[start + size:end]
            assert payload[-1:] == b'\0'
            assert (len(payload) - 1) % 720 == 0
            frames = (len(payload) - 1) // 720
            emphases = Counter()
            for pos in range(0, len(payload) - 1, 720):
                h = int.from_bytes(payload[pos:pos+4], 'big')
                # MPEG-1, Layer II, no CRC, 160 kbps, 32000 Hz, no padding, mono.
                assert h >> 9 == 0xfffd98c4 >> 9 and (h >> 6) & 3 == 3
                emphases[h & 3] += 1
            # Header-implied untrimmed PCM size, not decoder output.
            implied_pcm = frames * 1152 * 2
            delta = pcm - implied_pcm
            rates[rate] += 1
            deltas[delta] += 1
            clips.append(dict(index=index, frames=frames, pcm_bytes=pcm,
                              untrimmed_header_implied_pcm_bytes=implied_pcm,
                              pcm_delta=delta, nominal_rate=rate,
                              emphasis_counts=dict(emphases)))
            if comparison is not None:
                other = struct.unpack_from('<II16sHBBIII', stock, stock_offsets[index-1])
                comparison['compared_by_same_index'] += 1
                comparison['identical_raw_name'] += int(name == other[2])
                comparison['identical_pcm_bytes'] += int(pcm == other[7])
                comparison['identical_nominal_rate'] += int(rate == other[3])
        row = dict(file=source['file'], sha256=source['sha256'], clips=clips)
        if comparison is not None:
            row['same_path_same_index_reference'] = dict(sha256=hashlib.sha256(stock).hexdigest(), **comparison)
        rows.append(row)
    assert len(rows) == 32 and sum(rates.values()) == 3385
    reference_totals = Counter()
    for row in rows:
        for key, value in row.get('same_path_same_index_reference', {}).items():
            if key != 'sha256':
                reference_totals[key] += value
    return dict(scope='Only 32 SHA-pinned Russian SDTs from prior +40 audit',
                audio_decoded=False, game_playback_tested=False, game_files_written=0,
                frames_are_header_positions_not_decoded_audio=True,
                mpeg_header_rate_hz=32000, mpeg_header_samples_per_frame=1152,
                nominal_rate_counts=dict(rates),
                pcm_delta_summary=dict(equal=deltas[0],
                    positive=sum(n for d, n in deltas.items() if d > 0),
                    negative=sum(n for d, n in deltas.items() if d < 0),
                    min=min(deltas), max=max(deltas), most_common=deltas.most_common(8)),
                reference_comparison_totals=dict(reference_totals), files=rows)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path)
    p.add_argument('--baseline', type=Path, required=True)
    p.add_argument('--report', type=Path, required=True)
    p.add_argument('--reference-root', type=Path)
    args = p.parse_args()
    target = args.report.resolve()
    if target.suffix.lower() != '.json' or target.is_relative_to(args.root.resolve()) or (
            args.reference_root and target.is_relative_to(args.reference_root.resolve())):
        p.error('--report must be a .json path outside both input roots')
    report = inspect(args.root, json.loads(args.baseline.read_text(encoding='utf8')), args.reference_root)
    target.write_text(json.dumps(report, indent=2) + '\n', encoding='utf8')
    print(json.dumps({k: v for k, v in report.items() if k != 'files'}))
