"""Exact narrow494D00 projection and physical stat4/scalar225 record edits."""
import struct
from native_possession_recharge import u32, s32, tz, qmul, qdiv


def project(base_damage, percent_bits, *, level=1, coefficient_bits=125,
            possessed=False, secondary=False, pit_active=False, pit225_bits=0):
    for value, maximum in [(base_damage, 65535), (percent_bits, 0xFFFFFFFF),
                           (level, 255), (coefficient_bits, 0xFFFFFFFF), (pit225_bits, 0xFFFFFFFF)]:
        if type(value) is not int or not 0 <= value <= maximum:
            raise ValueError('Expected physical unsigned field bits')
    if any(type(flag) is not bool for flag in [possessed, secondary, pit_active]):
        raise ValueError('Expected boolean flags')
    percent = percent_bits & 65535
    percent = percent if percent < 32768 else percent-65536
    coefficient = coefficient_bits & 65535
    coefficient = coefficient if coefficient < 32768 else coefficient-65536
    damage = base_damage if level == 1 else tz(s32(coefficient*base_damage), 100)
    factor = 4096
    if possessed or secondary:
        factor = qdiv(u32((100+percent) << 12), (100 if possessed else 200) << 12)
    elif pit_active:
        factor = qdiv((100+(pit225_bits & 255)) << 12, 100 << 12)
    return dict(base_after_level=u32(damage), factor_Q12=factor,
                result_bits=u32(s32(qmul(factor, u32(damage << 12))) >> 12))


def _record(raw, value_bits):
    if not isinstance(raw, (bytes, bytearray)) or len(raw) != 16:
        raise ValueError('Expected full16-byte Variables record')
    if type(value_bits) is not int or not 0 <= value_bits <= 0xFFFFFFFF:
        raise ValueError('Expected physical DWORD bits')


def patch_stat4_value(raw, value_bits):
    _record(raw, value_bits)
    kind, stat = struct.unpack_from('<2I', raw)
    if kind not in (65, 224) or stat != 4:
        raise ValueError('Expected type65/224 StatId4')
    out = bytearray(raw)
    struct.pack_into('<I', out, 8, value_bits)
    return bytes(out)


def patch_scalar225(raw, value_bits):
    _record(raw, value_bits)
    if struct.unpack_from('<I', raw)[0] != 225:
        raise ValueError('Expected scalar225')
    out = bytearray(raw)
    struct.pack_into('<I', out, 4, value_bits)
    return bytes(out)
