"""Exact Pro/RC3 Trap model→raw projection; not a gameplay specification.

GUI labels and commits can be faulty. Lossless patching remains separate from
this original-writer projection. Synthetic virtual getters supplied explicitly.
"""
import struct
PROFILES={'Pro':dict(program='DK2 Editor pro.exe',writer=0x4A8F40,array=0x4F0,count=0x4F4,date_global=0x550954,date_helper=0x4AB320,ddx=0x470740,fill=0x471930,commit=0x471E00,model=0x4A1770),
 'RC3':dict(program='DK2-Editor-rc3.exe',writer=0x4AE750,array=0x4EC,count=0x4F0,date_global=0x5680BC,date_helper=0x4B0B30,ddx=0x475900,fill=0x476AF0,commit=0x476FC0,model=0x4A6EB0)}
DIRECT=[(0x20C,0x168,1),(0x206,0x124,2),(0x208,0x126,2),(0x1FC,0x128,2),(0x1FE,0x12A,2),(0x200,0x12C,2),(0x1F2,0x230,2),(0x202,0x236,2),(0x204,0x238,2),(0x1D4,0x23C,4),(0x1F0,0x232,2),(0x241,0x234,2),(0x1FA,0x240,2),(0x20A,0x242,1),(0x1EA,0x258,2),(0x1F6,0x278,2),(0x1F4,0x27C,2),(0x1F8,0x27A,2),(0x20E,0x268,1),(0x20B,0x27E,1),(0x1E8,0x158,2),(0x22F,0x27F,1),(0x230,0x15A,1)]
SCALED=[(0x1C4,0x228),(0x1C8,0x248),(0x1CC,0x250),(0x1D0,0x260),(0x1D8,0x270),(0x231,0x280),(0x235,0x288),(0x239,0x290),(0x23D,0x298)]
RESOURCES=[(0x20,0xCC),(0x74,0x14),(0xC8,0x70),(0x11C,0x170),(0x170,0x1CC)]
COMMIT_COPY=[(0x168,0x14C,1),(0x232,0x70,2),(0x234,0x74,2),(0x228,0x68,8),(0x230,0x7C,2),(0x236,0x60,2),(0x240,0x78,2),(0x250,0xA0,8),(0x242,0x98,1),(0x248,0xA8,8),(0x258,0x88,2),(0x260,0x90,8),(0x270,0xB0,8),(0x158,0x64,2),(0x280,0xE0,8),(0x288,0xE8,8),(0x290,0xF0,8),(0x298,0xF8,8),(0x15A,0x100,1)]
COMMIT_COPY += [(0x2A0+4*i,0x104+4*i,4) for i in range(5)]
COMMIT_COPY += [(0x2D4,0x138,4)]+[(0x2B4+4*i,0x118+4*i,4) for i in range(8)]+[(0x2D8+4*i,0x13C+4*i,4) for i in range(3)]

def commit_projection(model,panel,manufacture):
 """Memory-fixture branch projection;virtual setters captured separately.

 CString copy fixture leaves existing string handle unchanged;selection arrays
 contain the fixed fixture values below. Not a general live dialog emulator.
 """
 if len(model)!=0x300 or len(panel)!=0x200 or type(manufacture) is not int or not 0<=manufacture<=0xFFFFFFFF:raise ValueError('Expected fixture model/panel andDWORD')
 p=bytearray(panel);struct.pack_into('<I',p,0x78,manufacture);out=bytearray(model)
 for dst,src,n in COMMIT_COPY:out[dst:dst+n]=p[src:src+n]
 out[0x23C:0x240]=p[0x84:0x88] if struct.unpack_from('<I',p,0x120)[0] else bytes(4)
 for dst in [0x278,0x27C,0x27A]:struct.pack_into('<H',out,dst,0x1234)
 out[0x268]=7;out[0x27E]=3;out[0x27F]=9;out[0x2E4:0x2E8]=p[0xD8:0xDC]
 return bytes(out),bytes(p)

def padded_string(value):
 if not isinstance(value,bytes) or len(value)>32:raise ValueError('Expected at most32 string bytes')
 return value+bytes(32-len(value))

def record(model,label=b'',sound=b'',virtual=(21,0x12345678,0x89ABCDEF),flag_fields=()):
 if not isinstance(model,(bytes,bytearray)) or len(model)!=0x300:raise ValueError('Expected300hex model bytes')
 out=bytearray(579);covered=bytearray(579)
 def copy(dst,b):out[dst:dst+len(b)]=b;covered[dst:dst+len(b)]=bytes([1])*len(b)
 copy(0,padded_string(label));copy(0x20F,padded_string(sound))
 for dst,src,n in DIRECT:copy(dst,model[src:src+n])
 for dst,src in SCALED:copy(dst,struct.pack('<I',int(struct.unpack_from('<d',model,src)[0]*4096)&0xFFFFFFFF))
 for dst,src in RESOURCES:copy(dst,model[src:src+84])
 for dst,v,n in [(0x20D,virtual[0],1),(0x1E0,virtual[1],4),(0x1E4,virtual[2],4)]:copy(dst,int(v).to_bytes(n,'little'))
 copy(0x1DC,model[0x2E4:0x2E8]);flags=0
 for bit,off in flag_fields:
  if struct.unpack_from('<I',model,off)[0]:flags|=bit
 copy(0x1EC,struct.pack('<I',flags));assert all(covered),[hex(i) for i,v in enumerate(covered) if not v]
 return bytes(out)

def patch_unknown(raw,value):
 if not isinstance(raw,(bytes,bytearray)) or len(raw)!=579 or type(value) is not int or not 0<=value<=65535:raise ValueError('Expected579-byte Trap andWORD bitpattern')
 out=bytearray(raw);struct.pack_into('<H',out,0x204,value);return bytes(out)
