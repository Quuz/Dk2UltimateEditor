"""Lossless physical Creature XYZ patching; verified DX portal-exit consumer only.

Values are signed fixed-point positions at4096 units/cell. Preserve Z physically:
this consumer overwrites final Z with floor height, other consumers may differ.
"""
import struct
from native_creature_extension import check
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
FIELDS={'x':0xe94,'y':0xe98,'z':0xe9c}
def validate(raw,executable_sha256):
    check(raw)
    if executable_sha256!=DX:raise ValueError('Portal gameplay semantics require the exact inspected DX executable SHA256')
def view(raw,executable_sha256):
    validate(raw,executable_sha256)
    return dict(record_bytes=len(raw),xyz_signed=list(struct.unpack_from('<3i',raw,0xe94)),xyz_bits=list(struct.unpack_from('<3I',raw,0xe94)),
      units_per_cell=4096,role='Offset applied after portal appearance action finishes',initial_spawn='room anchorXY<<12 +1800h; not selected by this triple',
      final_z='Replaced by target-cell floor helper in verified459C20; original rawZ must still be preserved')
def patch_bits(raw,axis,value,executable_sha256):
    validate(raw,executable_sha256)
    if axis not in FIELDS:raise ValueError('Expected physical axis x/y/z')
    if type(value) is not int or not 0<=value<=0xffffffff:raise ValueError('Expected unsigned32 physical bits')
    result=bytearray(raw);struct.pack_into('<I',result,FIELDS[axis],value);return bytes(result)
def patch_signed(raw,axis,value,executable_sha256):
    if type(value) is not int or not -(1<<31)<=value<(1<<31):raise ValueError('Expected signed32 coordinate')
    return patch_bits(raw,axis,value&0xffffffff,executable_sha256)
