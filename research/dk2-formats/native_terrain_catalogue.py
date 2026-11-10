"""Exact DX Terrain loading projection with null graphics provider.

Sound handles are supplied by the bounded test provider, not decoded from files.
This is a research oracle, not a normalization policy for an independent editor.
"""
import struct

# raw offset, intermediate offset, presence DWORD, native destination, copy bytes.
SCALARS=[(0x1D6,0x28E,4,0x1C4,1),(0x1D7,0x28F,0x74,0x1AB,2),
 (0x1FF,0x2B7,0x78,0x1AD,2),(0x188,0x240,0x24,0x17F,4),
 (0x194,0x24C,0x28,0x1A9,2),(0x196,0x24E,0x2C,0x181,2),
 (0x198,0x250,0x30,0x183,2),(0x1D9,0x291,0x7C,0x1C5,1),
 (0x1DA,0x292,0x80,0x1C6,1),(0x19C,0x254,0x34,0x187,2),
 (0x19E,0x256,0x38,0x189,2),(0x19A,0x252,0x3C,0x185,2),
 (0x1A0,0x258,0x40,0x18B,2),(0x1A2,0x25A,0x44,0x18D,2),
 (0x1A8,0x260,0x48,0x18F,2),(0x1AA,0x262,0x4C,0x191,2),
 (0x1AC,0x264,0x50,0x193,2),(0x1A4,0x25C,0x84,0x1C7,2),
 (0x1A6,0x25E,0x88,0x1C9,2),(0x1CE,0x286,0x54,0x1A1,1),
 (0x1D2,0x28A,0x64,0x1A5,1),(0x224,0x2DC,0x98,0x1B5,4),
 (0x18C,0x244,0xA8,0x1BF,4),(0x1DE,0x296,0xAC,0x1C3,1)]
for i in range(3):
 SCALARS.extend([(0x1CF+i,0x287+i,0x58+4*i,0x1A2+i,1),
                 (0x1D3+i,0x28B+i,0x68+4*i,0x1A6+i,1)])
COLOURS=[(0x1DB+i,0x293+i,0x8C+4*i,0x1AF+2*i,0x80000<<i) for i in range(3)]
COLOURS += [(0x201+i,0x2B9+i,0x9C+4*i,0x1B9+2*i,0x2000000<<i) for i in range(3)]

def u32(b,o):return struct.unpack_from('<I',b,o)[0]
def put(b,o,v):struct.pack_into('<I',b,o,v&0xFFFFFFFF)

def intermediate(raw):
 if not isinstance(raw,(bytes,bytearray)) or len(raw)!=552:raise ValueError('Expected552-byte Terrain')
 out=bytearray(736)
 for a,b,p,_,n in SCALARS:
  out[b:b+n]=raw[a:a+n];put(out,p,1)
 for a,b,p,_,_ in COLOURS:out[b]=raw[a];put(out,p,1)
 for a,b,n,p in [(0x20,0xD8,84,0x10),(0x74,0x12C,84,0x14),
                 (0xC8,0x180,84,0x18),(0x11C,0x1D4,84,0x1C),
                 (0x170,0x228,24,0x20),(0x1AE,0x266,32,0xB4)]:
  out[b:b+n]=raw[a:a+n];put(out,p,1)
 for a,b,p in [(0,0xB8,0),(0x1DF,0x297,8),(0x204,0x2BC,12)]:
  s=raw[a:a+32].split(b'\0',1)[0][:31];out[b:b+len(s)]=s;put(out,p,1)
 put(out,0x248,u32(raw,0x190));put(out,0xB0,1)
 return bytes(out)

def definition(mid,previous,gate,sound_calls=0):
 """Complete4F66E0 for null graphics provider and zero resource flags.

 The provider returns A100+one-based call number. Other provider implementations
 and nonzero model resource descriptors deliberately are outside this oracle.
 """
 if len(mid)!=736 or len(previous)!=460:raise ValueError('Expected736/460 bytes')
 out=bytearray(previous)
 if not gate:put(out,0x84,sum(mid))
 for _,a,p,b,n in SCALARS:
  if u32(mid,p):out[b:b+min(n,2 if b==0x17F else n)]=mid[a:a+min(n,2 if b==0x17F else n)]
 if not gate:
  if u32(mid,8):sound_calls+=1;put(out,0x195,0xA100+sound_calls)
  if out[0x1C4]==22:sound_calls+=1;put(out,0x199,0xA100+sound_calls)
  else:put(out,0x199,0)
  if u32(mid,12):sound_calls+=1;put(out,0x19D,0xA100+sound_calls)
 if u32(mid,0xB0):out[0x17B:0x17F]=mid[0x248:0x24C]
 flags=u32(out,0x17B)
 for _,a,p,b,m in COLOURS:
  v=mid[a] if u32(mid,p) else struct.unpack_from('<H',out,b)[0]
  struct.pack_into('<H',out,b,(v+(255 if flags&m else 0))&65535)
 if u32(mid,0x20):
  out[0x6C:0x84]=mid[0x228:0x240]
  if any(mid[0x23C:0x23F]):put(out,0x7C,u32(out,0x7C)|1)
 if not gate and flags&0x3000:raise ValueError('Special graphics resource branch not modeled')
 # Other null-provider branches retain resource objects. Final two derived bits.
 bit=int(not(flags&4) and struct.unpack_from('<H',out,0x1AB)[0]<struct.unpack_from('<H',out,0x1AD)[0] and out[0x1C4]!=out[0x1C5])
 out[0x1CB]=(out[0x1CB]&0xFC)|bit
 return bytes(out),sound_calls

def lookup_address(base,ident):
 if type(ident) is not int or not 0<=ident<=0xFFFFFFFF:raise ValueError('Expected DWORD selector')
 return base+460*(ident&255)

def patch_unknown(raw,offset,value):
 """Explicit raw198 WORD or raw224 DWORD patch; no guessed field meaning."""
 if len(raw)!=552 or type(offset) is not int or offset not in [0x198,0x224]:raise ValueError('Expected Terrain record and unknown offset')
 n=2 if offset==0x198 else 4
 if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Value outside raw width')
 out=bytearray(raw);out[offset:offset+n]=value.to_bytes(n,'little');return bytes(out)
