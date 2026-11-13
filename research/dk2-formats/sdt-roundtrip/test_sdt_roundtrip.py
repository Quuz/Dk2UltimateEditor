"""Boundary probes for conservative SDT writer model."""
import struct
import unittest
from audit_sdt_roundtrip import HEADER, LayoutError, inspect_length40_pattern, parse

def header(payload=0, size=40):
    return HEADER.pack(size, payload, b'opaque\0name\xff', 22050, 16, 36, 123, 456, 789)

class BoundaryChecks(unittest.TestCase):
    def rejects(self, raw, reason):
        with self.assertRaisesRegex(LayoutError, reason):
            parse(raw)

    def test_extended_headers_and_gaps(self):
        raw = struct.pack('<3I', 2, 15, 64) + b'pre' + header(3, 44) + b'EXT!' + b'abc' + b'xy' + header(2) + b'yz' + b'tail'
        model = parse(raw)
        self.assertEqual(raw, model.rebuild())
        self.assertEqual(model.records[0].header[-4:], b'EXT!')
        self.assertEqual(model.records[0].payload, b'abc')
        self.assertEqual(model.records[0].following_gap, b'xy')

    def test_empty_archive_tail(self):
        raw = struct.pack('<I', 0) + b'tail'
        self.assertEqual(parse(raw).rebuild(), raw)

    def test_empty_payload(self):
        raw = struct.pack('<2I', 1, 8) + header()
        self.assertEqual(parse(raw).rebuild(), raw)

    def test_truncated_count(self):
        self.rejects(b'123', 'truncated_count')

    def test_truncated_directory(self):
        self.rejects(struct.pack('<I', 0xffffffff), 'truncated_directory')

    def test_duplicate_offsets(self):
        self.rejects(struct.pack('<3I', 2, 12, 12) + header() * 2, 'duplicate_or_unordered')

    def test_unordered_offsets(self):
        self.rejects(struct.pack('<3I', 2, 52, 12) + header() * 2, 'duplicate_or_unordered')

    def test_offset_inside_directory(self):
        self.rejects(struct.pack('<2I', 1, 4) + header(), 'header_inside_directory')

    def test_offset_beyond_eof(self):
        self.rejects(struct.pack('<2I', 1, 999) + header(), 'truncated_fixed_header')

    def test_short_header(self):
        self.rejects(struct.pack('<2I', 1, 8) + header()[:-1], 'truncated_fixed_header')

    def test_fixed_header_overlap(self):
        self.rejects(struct.pack('<3I', 2, 12, 32) + header() * 2, 'fixed_header_crosses')

    def test_undersized_header(self):
        self.rejects(struct.pack('<2I', 1, 8) + header(size=39), 'header_size_below_40')

    def test_extension_truncated(self):
        self.rejects(struct.pack('<2I', 1, 8) + header(size=41), 'extended_header_crosses')

    def test_extension_overlap(self):
        self.rejects(struct.pack('<3I', 2, 12, 52) + header(size=41) + header(), 'extended_header_crosses')

    def test_payload_truncated(self):
        self.rejects(struct.pack('<2I', 1, 8) + header(payload=1), 'payload_crosses')

    def test_payload_overlap_inside_file(self):
        self.rejects(struct.pack('<3I', 2, 12, 52) + header(payload=1) + header(), 'payload_crosses')

    def test_length40_pattern_and_inverse(self):
        payload = bytes.fromhex('fffd98c4') + bytes(716) + b'\0'
        raw = struct.pack('<2I', 1, 8) + header(payload=len(payload) + 40) + payload
        result = inspect_length40_pattern(raw)
        self.assertEqual(result['records'], 1)
        self.assertTrue(result['exact_inverse'])
        self.assertTrue(result['corrected_candidate_exact_roundtrip'])
        self.rejects(raw, 'payload_crosses')

    def test_arbitrary_overrun_not_length40_pattern(self):
        raw = struct.pack('<2I', 1, 8) + header(payload=41) + b'\0'
        self.assertIsNone(inspect_length40_pattern(raw))

    def test_partial_frame_not_length40_pattern(self):
        payload = bytes.fromhex('fffd98c4') + bytes(715) + b'\0'
        raw = struct.pack('<2I', 1, 8) + header(payload=len(payload) + 40) + payload
        self.assertIsNone(inspect_length40_pattern(raw))

    def test_size_change_recalculates_next_offset(self):
        from dataclasses import replace
        # First payload grows by four bytes. Its own header field and every
        # later directory offset change; record order/opaque bytes do not.
        raw = struct.pack('<3I', 2, 15, 64) + b'pre' + header(3, 44) + b'EXT!' + b'abc' + b'xy' + header(2) + b'yz' + b'tail'
        original = parse(raw)
        first = original.records[0]
        new_payload = b'abcdefg'
        new_header = bytearray(first.header)
        struct.pack_into('<I', new_header, 4, len(new_payload))
        changed = replace(original, records=(replace(first, header=bytes(new_header), payload=new_payload), original.records[1]))
        rebuilt = changed.rebuild()
        self.assertEqual(struct.unpack_from('<3I', rebuilt), (2, 15, 68))
        self.assertEqual(len(rebuilt), len(raw) + 4)
        reparsed = parse(rebuilt)
        self.assertEqual(reparsed, changed)
        self.assertEqual(reparsed.records[1], original.records[1])
        self.assertEqual(reparsed.leading_gap, b'pre')
        self.assertEqual(reparsed.records[0].following_gap, b'xy')
        self.assertEqual(reparsed.records[0].header[8:], first.header[8:])
if __name__ == '__main__':
    unittest.main()