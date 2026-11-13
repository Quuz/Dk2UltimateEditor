"""Lossless SDT structure audit; never writes input archives.
Strict increasing/disjoint records are conservative writer policy, not native rules.
"""
import argparse
from collections import Counter
from dataclasses import dataclass
import hashlib
import json
from pathlib import Path
import struct

HEADER = struct.Struct('<II16sHBBIII')
U32 = struct.Struct('<I')

class LayoutError(ValueError):
    pass

@dataclass(frozen=True)
class Record:
    header: bytes
    payload: bytes
    following_gap: bytes

@dataclass(frozen=True)
class Archive:
    leading_gap: bytes
    records: tuple

    def rebuild(self):
        """Regenerate directory, then concatenate parsed components, not raw file."""
        count = len(self.records)
        offset = 4 + count * 4 + len(self.leading_gap)
        offsets, parts = [], [self.leading_gap]
        for record in self.records:
            offsets.append(U32.pack(offset))
            part = record.header + record.payload + record.following_gap
            parts.append(part)
            offset += len(part)
        return U32.pack(count) + b''.join(offsets) + b''.join(parts)

def directory(raw):
    if len(raw) < 4:
        raise LayoutError('truncated_count')
    count = U32.unpack_from(raw)[0]
    if count > (len(raw) - 4) // 4:
        raise LayoutError('truncated_directory')
    end = 4 + count * 4
    offsets = tuple(U32.unpack_from(raw, 4 + i * 4)[0] for i in range(count))
    previous = end - 1
    for offset in offsets:
        if offset < end:
            raise LayoutError('header_inside_directory')
        if offset <= previous:
            raise LayoutError('duplicate_or_unordered_offsets')
        if offset > len(raw) - HEADER.size:
            raise LayoutError('truncated_fixed_header')
        previous = offset
    return end, offsets

def parse(raw):
    end, offsets = directory(raw)
    first = offsets[0] if offsets else len(raw)
    records = []
    for offset, boundary in zip(offsets, offsets[1:] + (len(raw),)):
        if offset + HEADER.size > boundary:
            raise LayoutError('fixed_header_crosses_record_boundary')
        size, payload_size = struct.unpack_from('<II', raw, offset)
        if size < HEADER.size:
            raise LayoutError('header_size_below_40')
        if offset + size > boundary:
            raise LayoutError('extended_header_crosses_record_boundary')
        payload_end = offset + size + payload_size
        if payload_end > boundary:
            raise LayoutError('payload_crosses_record_boundary')
        records.append(Record(raw[offset:offset + size], raw[offset + size:payload_end], raw[payload_end:boundary]))
    return Archive(raw[end:first], tuple(records))

def inspect_length40_pattern(raw):
    """Recognize prior Russian packaging pattern, not general repair.
    MPEG header slots and complete fixed sizes checked; not audio decoding/CRC QA.
    """
    _, offsets = directory(raw)
    if not offsets:
        return None
    candidate = bytearray(raw)
    frames = 0
    for offset, boundary in zip(offsets, offsets[1:] + (len(raw),)):
        if offset + HEADER.size > boundary:
            return None
        size, declared, _, _, bits, kind, _, _, _ = HEADER.unpack_from(raw, offset)
        physical_size = boundary - offset - HEADER.size
        if (size, bits, kind) != (40, 16, 36) or declared != physical_size + 40:
            return None
        payload = raw[offset + size:boundary]
        count, remainder = divmod(len(payload), 720)
        if count == 0 or remainder != 1 or payload[-1:] != b'\0':
            return None
        for start in range(0, count * 720, 720):
            header = int.from_bytes(payload[start:start + 4], 'big')
            if header >> 9 != 0xfffd98c4 >> 9 or (header >> 6) & 3 != 3:
                return None
        frames += count
        U32.pack_into(candidate, offset + 4, physical_size)
    rebuilt = parse(candidate).rebuild()
    if rebuilt != candidate:
        raise RuntimeError('corrected candidate roundtrip mismatch')
    inverse = bytearray(rebuilt)
    for offset in offsets:
        inverse[offset + 4:offset + 8] = raw[offset + 4:offset + 8]
    if inverse != raw:
        raise RuntimeError('length correction inverse mismatch')
    return dict(records=len(offsets), full_frame_header_slots=frames,
                corrected_candidate_sha256=hashlib.sha256(candidate).hexdigest(),
                corrected_candidate_exact_roundtrip=True, exact_inverse=True,
                overstatement_bytes_per_record=40)

def inspect_file(path, root):
    raw = path.read_bytes()
    result = dict(file=path.relative_to(root).as_posix(), bytes=len(raw), source_sha256=hashlib.sha256(raw).hexdigest())
    try:
        archive = parse(raw)
    except LayoutError as error:
        result.update(status='rejected_by_strict_policy', reason=str(error))
        try:
            pattern = inspect_length40_pattern(raw)
        except LayoutError:
            pattern = None
        if pattern:
            result.update(status='length40_packaging_pattern', **pattern)
        return result
    rebuilt = archive.rebuild()
    if rebuilt != raw:
        raise RuntimeError(f'{path}: identity roundtrip mismatch')
    result.update(status='exact_roundtrip', records=len(archive.records), rebuilt_sha256=hashlib.sha256(rebuilt).hexdigest(), exact_roundtrip=True,
                  extended_headers=sum(len(r.header) > 40 for r in archive.records),
                  retained_gap_bytes=len(archive.leading_gap) + sum(len(r.following_gap) for r in archive.records))
    return result

def audit(root):
    if not root.is_dir():
        raise ValueError(f'Not a directory: {root}')
    files = sorted((p for p in root.rglob('*') if p.is_file() and p.suffix.lower() == '.sdt'), key=lambda p: p.relative_to(root).as_posix().casefold())
    if not files:
        raise ValueError(f'No SDT files under: {root}')
    rows = [inspect_file(path, root) for path in files]
    return dict(schema_version=1,
                scope='Structural identity rebuild; known length40 pattern corrected/inverted only in memory',
                policy='Strict increasing disjoint records; conservative writer policy, not native acceptance specification',
                totals=dict(files=len(rows), records=sum(r.get('records', 0) for r in rows), statuses=dict(Counter(r['status'] for r in rows))),
                limitations=['No audio decoding, CRC verification, playback, replacement encoding or native loader execution',
                             'Length40 recognition uses prior corpus fixed MPEG header-slot pattern, not a general repair heuristic',
                             'Headers, unknown bytes, extensions, gaps and audio preserved; metadata semantics not validated'],
                game_files_written=0, files=rows)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('root', type=Path)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    root, report_path = args.root.resolve(), args.report.resolve()
    if report_path.suffix.lower() != '.json' or report_path.is_relative_to(root):
        parser.error('--report must be a .json path outside the input root')
    report = audit(root)
    report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf8')
    print(json.dumps(report['totals']))
    return 2 if report['totals']['statuses'].get('rejected_by_strict_policy') else 0

if __name__ == '__main__':
    raise SystemExit(main())