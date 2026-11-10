"""Synthetic preservation controls; no copyrighted game data or local paths."""
from pathlib import Path
import sys,unittest
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'research/dk2-formats'))
import dk2_formats as fmt
import native_gold_margin as margin


class PreservationTests(unittest.TestCase):
    def test_door_manufacture_keeps_separate_unknown_word(self):
        raw=bytes((i*37+11)&255 for i in range(616))
        changed=fmt.set_scalar(raw,'Doors','ManufToBuild',65535)
        self.assertEqual(changed,raw[:0x234]+b'\xff\xff'+raw[0x236:])
        self.assertEqual(changed[0x236:0x238],raw[0x236:0x238])

    def test_trap_unknown_word_keeps_all_other_bytes(self):
        raw=bytes((i*17+3)&255 for i in range(579))
        changed=fmt.set_scalar(raw,'Traps','Unknown204',0xA55A)
        self.assertEqual(changed,raw[:0x204]+b'\x5a\xa5'+raw[0x206:])

    def test_variable_scalar_keeps_opaque_extra_words(self):
        raw=(74).to_bytes(4,'little')+(340).to_bytes(4,'little')+bytes.fromhex('1122334455667788')
        changed=margin.patch_bits(raw,0xFFFFFFFF)
        self.assertEqual(changed,raw[:4]+b'\xff'*4+raw[8:])
        self.assertEqual(margin.patch_bits(changed,340),raw)

    def test_gold_margin_unsigned_underflow_is_not_clamped(self):
        result=margin.project(100,1,100,100,340,1)
        self.assertEqual(result['threshold_bits'],0xFFFFFF10)
        self.assertFalse(result['accepted'])


if __name__=='__main__':unittest.main()
