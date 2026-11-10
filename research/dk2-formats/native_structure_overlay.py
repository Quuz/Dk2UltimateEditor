"""DX structure-height consumer in entity state-overlay placement.

This does not define physical collision height or every renderer consumer.
"""
import struct
FAMILIES={'Traps':dict(raw_bytes=579,raw_height=0x1C4,mid_bytes=747,mid_height=0x26C,presence=0x20,definition_bytes=209,definition_height=0xA0,kind=3,converter=0x570D10,loader=0x4AA440,start=0x4AA4D6,stop=0x4AA4EA),
 'Doors':dict(raw_bytes=616,raw_height=0x218,mid_bytes=732,mid_height=0x28C,presence=0x24,definition_bytes=181,definition_height=0x97,kind=4,converter=0x56AA40,loader=0x4A7100,start=0x4A71A6,stop=0x4A71BA)}

def checked(v,bits):
 if type(v) is not int or not 0<=v<1<<bits:raise ValueError(f'Expected unsigned{bits} bit pattern')
 return v

def height_offset(kind,height_word,door_state=0):
 checked(height_word,16);checked(door_state,32)
 if type(kind) is not int or kind not in [3,4]:raise ValueError('Expected entity kind3 or4')
 return height_word+(0x200 if kind==3 else 0x1098 if door_state else 0x800)

def project(context,kind,height_word,door_state=0):
 if not isinstance(context,(bytes,bytearray)) or len(context)!=300:raise ValueError('Expected300-byte overlay context')
 out=bytearray(context);n=height_offset(kind,height_word,door_state);struct.pack_into('<I',out,0x14,n)
 for o in [0x2D,0x39]:struct.pack_into('<I',out,o,(struct.unpack_from('<I',out,o)[0]+n)&0xFFFFFFFF)
 return bytes(out)

def patch_height(raw,family,value):
 if not isinstance(family,str) or family not in FAMILIES:raise ValueError('ExpectedTraps orDoors')
 f=FAMILIES[family]
 if not isinstance(raw,(bytes,bytearray)) or len(raw)!=f['raw_bytes']:raise ValueError('Unexpected catalogue record size')
 checked(value,32);out=bytearray(raw);struct.pack_into('<I',out,f['raw_height'],value);return bytes(out)
