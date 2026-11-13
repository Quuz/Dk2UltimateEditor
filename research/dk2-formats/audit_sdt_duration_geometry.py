"""Correlate SFX stored duration with SDT nominal PCM geometry; no runtime claims."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import struct

from dk2_sound_index import read_bank, read_sfx, read_sdt, text, SoundFormatError


def inspect(root, length_baseline):
    files = {p.relative_to(root).as_posix().casefold(): p for p in root.rglob('*') if p.is_file() and p.suffix.lower() in ('.map', '.sdt')}
    hashes, banks, maps, clips, issues = {}, {}, {}, {}, []
    known = {r['file'].casefold(): r['sha256'] for r in length_baseline['files']}
    corrected = []
    for name, path in sorted(files.items()):
        raw = path.read_bytes()
        hashes[name] = hashlib.sha256(raw).hexdigest()
        try:
            if name.endswith('.sdt'):
                try:
                    clips[name] = read_sdt(raw)
                except SoundFormatError:
                    if hashes[name] != known.get(name):
                        raise
                    candidate = bytearray(raw)
                    n, = struct.unpack_from('<I', raw)
                    offsets = struct.unpack_from(f'<{n}I', raw, 4)
                    assert n > 0 and offsets[0] >= 4 + 4*n
                    for start, end in zip(offsets, offsets[1:] + (len(raw),)):
                        assert start + 40 <= end
                        size, declared = struct.unpack_from('<II', raw, start)
                        assert size == 40 and declared == end - start
                        struct.pack_into('<I', candidate, start + 4, end - start - size)
                    clips[name] = read_sdt(candidate)
                    corrected.append(name)
            elif name.endswith('bank.map'):
                _, entries = read_bank(raw)
                banks[name[:-8]] = [text(label).replace('\\', '/').casefold() for _, label in entries]
            elif name.endswith('sfx.map'):
                maps[name[:-7]] = read_sfx(raw)[2]
        except SoundFormatError as e:
            issues.append(dict(file=name, error=str(e)))
    totals, groups, examples = Counter(), defaultdict(Counter), defaultdict(list)
    for category, entries in sorted(maps.items()):
        names = banks.get(category)
        for group in entries:
            for item_index, item in enumerate(group.items, 1):
                for sound_index, row in enumerate(item.sounds, 1):
                    index, weight, duration, bank = struct.unpack('<4I', row)
                    totals['sound_rows'] += 1
                    for suffix in ('hd', 'hw'):
                        totals['reference_attempts'] += 1
                        if names is None or not 1 <= bank <= len(names):
                            totals['missing_or_out_of_range_bank'] += 1
                            continue
                        archive = names[bank - 1] + suffix + '.sdt'
                        if archive not in files:
                            totals['archive_missing'] += 1
                            continue
                        if archive not in clips:
                            totals['archive_unparsed'] += 1
                            continue
                        if not 1 <= index <= len(clips[archive]):
                            totals['clip_index_out_of_range'] += 1
                            continue
                        clip = clips[archive][index - 1]
                        totals['resolved'] += 1
                        key = suffix + '/type_' + hex(clip.type)
                        stat = groups[key]
                        stat['resolved'] += 1
                        if clip.type not in (0x02, 0x03, 0x24, 0x25) or not clip.sample_rate or not clip.sample_bits:
                            stat['not_comparable_type_or_geometry'] += 1
                            continue
                        channels = 2 if clip.type in (0x03, 0x25) else 1
                        # Correlation hypothesis: +20 stores total interleaved PCM bytes.
                        numerator = 1000 * clip.pcm_bytes * 8
                        denominator = clip.sample_rate * clip.sample_bits * channels
                        floor = numerator // denominator
                        nearest = (numerator * 2 + denominator) // (2 * denominator)
                        delta = duration - floor
                        stat['comparable'] += 1
                        stat['stored_duration_zero'] += int(duration == 0)
                        stat['pcm_bytes_zero'] += int(clip.pcm_bytes == 0)
                        stat['exact_rational'] += int(duration * denominator == numerator)
                        stat['equals_floor_ms'] += int(duration == floor)
                        stat['equals_nearest_half_up_ms'] += int(duration == nearest)
                        stat['within_1ms_of_floor'] += int(abs(delta) <= 1)
                        stat['delta_below_minus1'] += int(delta < -1)
                        stat['delta_above_plus1'] += int(delta > 1)
                        if clip.type in (0x02, 0x03):
                            data_numerator = 1000 * clip.data_size * 8
                            data_rounded = (data_numerator * 2 + denominator) // (2 * denominator)
                            stat['pcm_payload_bytes_equals_nearest_ms'] += int(duration == data_rounded)
                        bucket = key + ('/within_1ms' if abs(delta) <= 1 else '/mismatch')
                        if len(examples[bucket]) < 3:
                            examples[bucket].append(dict(sfx_file=category+'sfx.map', group_id=group.id,
                                item_index=item_index, sound_index=sound_index, bank_index=bank,
                                archive=archive, clip_index=index, duration_field=duration,
                                pcm_bytes=clip.pcm_bytes, payload_bytes=clip.data_size, nominal_rate=clip.sample_rate,
                                sample_bits=clip.sample_bits, assumed_channels=channels,
                                nominal_geometry_floor_ms=floor, delta_from_floor_ms=delta))
    return dict(schema_version=1, scope='Stored SFX duration versus nominal SDT geometry; references counted per HD/HW resolution, not unique clips',
        hypothesis='pcmBytes is total interleaved PCM bytes; type02/24 mono,03/25 stereo; nominal rate and bits as stored',
        game_files_written=0, audio_decoded=False, native_timing_tested=False,
        totals=dict(totals), by_suffix_and_type={k: dict(v) for k,v in sorted(groups.items())},
        examples=dict(examples), in_memory_length40_files=corrected, parse_issues=issues, file_sha256=hashes)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('root', type=Path)
    p.add_argument('--baseline', type=Path, required=True)
    p.add_argument('--report', type=Path, required=True)
    args = p.parse_args()
    if args.report.resolve().is_relative_to(args.root.resolve()) or args.report.suffix.lower() != '.json':
        p.error('--report must be a .json outside the input root')
    result = inspect(args.root, json.loads(args.baseline.read_text(encoding='utf8')))
    args.report.write_text(json.dumps(result, indent=2)+'\n', encoding='utf8')
    print(json.dumps({k:result[k] for k in ('totals','by_suffix_and_type','parse_issues')}))
