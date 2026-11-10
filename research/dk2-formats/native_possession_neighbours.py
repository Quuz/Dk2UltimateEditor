"""Native DX bit08 update given an original XY-ray result; not a ray simulator."""
import struct

def flag_after(flags,*,source_possessed,is_source,line_clear):
    if not isinstance(flags,int) or not 0<=flags<=0xffffffff:raise ValueError('flags must be a DWORD')
    if not source_possessed:return flags&0xfffffff7
    if is_source:return flags
    return flags|8 if line_clear else flags&0xfffffff7

def patch_door_bit40(raw,enabled):
    """Preserve all other bytes/bits; this bit affects more than this ray path."""
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=616:raise ValueError('Expected complete616-byte Door record')
    if type(enabled) is not bool:raise ValueError('Expected boolean flag')
    out=bytearray(raw);flags=struct.unpack_from('<I',raw,0x228)[0]
    struct.pack_into('<I',out,0x228,(flags&~0x40)|(0x40 if enabled else 0));return bytes(out)
