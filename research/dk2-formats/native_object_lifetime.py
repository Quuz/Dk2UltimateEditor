"""Lossless raw Object lifetime/HP edits; exact ordinary DX tick semantics."""
import struct


def check(raw):
    if not isinstance(raw, bytes) or len(raw) != 894:
        raise ValueError('Expected complete known894-byte Object record')


def patch_decay_flags(raw, mode):
    check(raw)
    if type(mode) is not int or not 0 <= mode <= 3:
        raise ValueError('Decay flag combination must be0..3')
    value = struct.unpack_from('<I', raw, 0x344)[0]
    return raw[:0x344]+struct.pack('<I', (value & ~3)|mode)+raw[0x348:]


def patch_hp(raw, value):
    check(raw)
    if type(value) is not int or not 0 <= value <= 65535:
        raise ValueError('HP must fitunsigned WORD')
    return raw[:0x348]+struct.pack('<H', value)+raw[0x34A:]


def after_ticks(hp, flags, room_id=0, in_hand=False, ticks=1):
    active = not in_hand and bool(flags & 1 or flags & 2 and room_id == 0)
    return hp-min(max(0, hp), ticks) if active else hp
