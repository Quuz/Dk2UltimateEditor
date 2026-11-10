"""Explicit IsBadReadPtr import fixture over known emulated readable ranges.

This is not a replacement for any native game/RTTI function. The PE IAT
66C1B8 resolves to this outside-image import address in the inspected file.
"""
import struct
import pefile
from emulate_element_colors import pack32
ADDRESS=0x28cfa4
COUNTER=0x10074000
def allowed_ranges(exe,regions):
    pe=pefile.PE(data=exe);base=pe.OPTIONAL_HEADER.ImageBase
    spans=[(base,base+pe.OPTIONAL_HEADER.SizeOfHeaders)]
    spans += [(base+s.VirtualAddress,base+s.VirtualAddress+max(s.Misc_VirtualSize,s.SizeOfRawData)) for s in pe.sections if s.Characteristics&0x40000000]
    spans += [(int(r['address'],16),int(r['address'],16)+len(bytes.fromhex(r['hex']))) for r in regions]
    merged=[]
    for lo,hi in sorted(spans):
        if merged and lo<=merged[-1][1]:merged[-1]=(merged[-1][0],max(hi,merged[-1][1]))
        else:merged.append((lo,hi))
    return merged
def adapter(ranges):
    code=bytearray(bytes.fromhex('ff05')+pack32(COUNTER)+bytes.fromhex('8b4424048b54240885d2'));labels={};fixups=[]
    def branch(op,label):code.extend(op);fixups.append((len(code),label));code.extend(bytes(4))
    branch(bytes.fromhex('0f84'),'good');code.extend(bytes.fromhex('89c101d1'));branch(bytes.fromhex('0f82'),'bad')
    for i,(lo,hi) in enumerate(ranges):
        code.extend(b'\x3d'+pack32(lo));branch(bytes.fromhex('0f82'),f'next{i}')
        code.extend(bytes.fromhex('81f9')+pack32(hi));branch(bytes.fromhex('0f86'),'good');labels[f'next{i}']=len(code)
    labels['bad']=len(code);code.extend(bytes.fromhex('ff05')+pack32(COUNTER+4)+b'\xb8'+pack32(1)+bytes.fromhex('c20800'))
    labels['good']=len(code);code.extend(bytes.fromhex('33c0c20800'))
    for off,label in fixups:struct.pack_into('<i',code,off,labels[label]-(off+4));assert labels[label]>=off+4
    return bytes(code)
