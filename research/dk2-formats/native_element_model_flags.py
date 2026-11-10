"""Physical model-resource bit200 edit; preserve all other182 record bytes."""
import struct


def patch_resource_bit200(raw, enabled):
    if not isinstance(raw, (bytes, bytearray)) or len(raw) != 182:
        raise ValueError('Expected full182-byte EffectElements record')
    if type(enabled) is not bool or raw[0x70] not in (4, 5, 6):
        raise ValueError('Expected boolean and model resource type4/5/6')
    out = bytearray(raw)
    flags = struct.unpack_from('<I', raw, 0x60)[0]
    flags = flags | 0x200 if enabled else flags & ~0x200
    struct.pack_into('<I', out, 0x60, flags)
    return bytes(out)
