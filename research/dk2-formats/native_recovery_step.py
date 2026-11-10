"""Lossless raw128B patching and exact-DX candidate XY after recovery257.

The consumer uses only Y of runtime XYZslot2. This predicts the candidate
coordinates before native terrain/cell/floor acceptance, not final collision,
Z, whole AI lifetime or action timing. Unknown raw components stay intact.
"""
import struct
from native_creature_extension import check
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
OFFSET=0x128B

def validate(raw,executable_sha256):
    check(raw)
    if executable_sha256!=DX:raise ValueError('Exact inspected DX executable SHA256 required')

def view(raw,executable_sha256):
    validate(raw,executable_sha256)
    return dict(raw_offset=OFFSET,width=4,signed_distance=struct.unpack_from('<i',raw,OFFSET)[0],physical_bits=struct.unpack_from('<I',raw,OFFSET)[0],
        units_per_cell=4096,consumer='481200/state257 after recovery animation, before generic room assignment',
        runtime_XYZ_index=2,runtime_Y_offset=0x708,candidate_only=True,
        unassigned_neighbours='raw1287 X and128F Z are preserved; this consumer does not use them for XY displacement')

def patch_bits(raw,value,executable_sha256):
    validate(raw,executable_sha256)
    if type(value) is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected unsigned32 physical bits')
    out=bytearray(raw);struct.pack_into('<I',out,OFFSET,value);return bytes(out)

def patch_signed(raw,value,executable_sha256):
    if type(value) is not int or not -(1<<31)<=value<(1<<31):raise ValueError('Expected signed32 distance')
    return patch_bits(raw,value&0xFFFFFFFF,executable_sha256)

def candidate_xy(raw,executable_sha256,position,direction,trig_bytes):
    validate(raw,executable_sha256)
    if len(position)!=2 or any(type(v) is not int or not -(1<<31)<=v<(1<<31) for v in position):raise ValueError('Expected signed32 XY position')
    if type(direction) is not int or not 0<=direction<2048:raise ValueError('Expected direction0..2047; invalid game angles are not normalized')
    if not isinstance(trig_bytes,(bytes,bytearray)) or len(trig_bytes)!=10240:raise ValueError('Expected explicit2560entry signed32 source trig table')
    signed=lambda v:((v&0xFFFFFFFF)^0x80000000)-0x80000000
    distance=struct.unpack_from('<i',raw,OFFSET)[0];sine=struct.unpack_from('<i',trig_bytes,4*direction)[0];cosine=struct.unpack_from('<i',trig_bytes,4*(direction+512))[0]
    minus=signed(-distance)
    dx=signed(sine*minus)>>16;dy=signed(cosine*minus)>>16
    return [signed(position[0]+dx),signed(position[1]-dy)]
