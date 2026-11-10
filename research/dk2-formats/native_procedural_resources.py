"""Lossless ArtResource type7 view/edit. Unknown procedure IDs and DWORDs survive.

Offsets are relative to the complete 84-byte ArtResource, not a file record.
Only procedure_id has a native consumer name; opaque_1/2 remain unknown data.
"""
import struct


def checked(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=84 or raw[80]!=7:
        raise ValueError('Expected complete 84-byte ArtResource with type7')
    return bytes(raw)


def view_art(raw):
    raw=checked(raw)
    selector,one,two=struct.unpack_from('<3I',raw,68)
    return dict(procedure_id=selector,opaque_1=one,opaque_2=two,union_hex=raw[68:80].hex())


def patch_art(raw,*,procedure_id=None,opaque_1=None,opaque_2=None):
    out=bytearray(checked(raw))
    for offset,value in [(68,procedure_id),(72,opaque_1),(76,opaque_2)]:
        if value is None:continue
        if type(value) is not int or not 0<=value<=0xFFFFFFFF:
            raise ValueError('Expected unsigned32 integer; unknown IDs are allowed')
        struct.pack_into('<I',out,offset,value)
    return bytes(out)
