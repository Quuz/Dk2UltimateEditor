"""DX Threat calculations and exact raw Creature Threat WORD edits.

Runtime formulas are bounded to the attested functions; no automatic repairs.
"""
from native_possession_recharge import s32, tz


def signed8(value):
    value &= 255
    return value if value < 128 else value-256


def emitted(base, level, coefficient_bits, entries, vampire_bits, unique_vampire_bits):
    value = base & 65535
    if level != 1:
        coefficient = coefficient_bits & 65535
        if coefficient >= 32768:
            coefficient -= 65536
        value = tz(s32(coefficient*value), 100)
    if not entries:
        return None
    value = tz(s32(value), entries)
    vampire, unique = signed8(vampire_bits), signed8(unique_vampire_bits)
    if vampire > 0:
        value = tz(s32((30*vampire+100)*value), 100)
    if unique > 0:
        # Original gate uses108F, but multiplier reuses BL loaded from108E.
        value = tz(s32((40*vampire+100)*value), 100)
    return value


def received(delta, maiden_bits):
    count = signed8(maiden_bits)
    if count > 0:
        delta = tz(s32((10-count)*delta*10), 100)
    return delta


def patch_threat(raw, value):
    if not isinstance(raw, bytes) or len(raw) not in [5449, 5537]:
        raise ValueError('Expected a complete known Creature record')
    if type(value) is not int or not 0 <= value <= 65535:
        raise ValueError('Threat must fit an unsigned WORD')
    return raw[:0xF12]+value.to_bytes(2, 'little')+raw[0xF14:]
