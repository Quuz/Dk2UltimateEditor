"""Exact version-specific entity pointer offsets; runtime is not a file format."""
import struct
from native_version_crosschecks import DX,OLD
FAMILIES={'Traps':dict(kind=3,lookup_old=0x502300,world_array=0x15E4,world_count=0x15EC,definition_bytes=209,definition_id=0xB4,old_pointer=0xF6,dx_pointer=0xF7,old_id=0xF5,dx_id=0xF6,raw_bytes=579,patch_offset=0x204,patch_bytes=2),
 'Doors':dict(kind=4,lookup_old=0x5020A0,world_array=0x15D4,world_count=0x15DC,definition_bytes=181,definition_id=0xAD,old_pointer=0xF6,dx_pointer=0xF7,old_id=0xF5,dx_id=0xF6,raw_bytes=616,patch_offset=0x218,patch_bytes=4),
 'Objects':dict(kind=2,lookup_old=0x502560,world_array=0x15F4,world_count=0x15FC,definition_bytes=285,definition_id=0x113,old_pointer=0x12C,dx_pointer=0x12D,old_id=0x12B,dx_id=0x12C,raw_bytes=894,patch_offset=0x34C,patch_bytes=2)}
def family(name):
 if not isinstance(name,str) or name not in FAMILIES:raise ValueError('Expected Traps,Doors orObjects')
 return FAMILIES[name]
def entity_offsets(sha,name):
 f=family(name)
 if not isinstance(sha,str) or sha not in [DX,OLD]:raise ValueError('Expected exact inspected executable SHA256')
 return dict(id=f['old_id'] if sha==OLD else f['dx_id'],definition=f['old_pointer'] if sha==OLD else f['dx_pointer'])
def lookup_index(ids,requested):
 if not isinstance(ids,(list,tuple,bytes,bytearray)) or any(type(n) is not int or not 0<=n<=255 for n in ids):raise ValueError('Expected byte IDs in physical runtime-array order')
 if type(requested) is not int or not 0<=requested<=0xFFFFFFFF:raise ValueError('Expected DWORD requested ID bits')
 key=requested&255;i=min(key-1,len(ids)-1)
 while i>=0:
  if ids[i]==key:return i
  if ids[i]<key:return None
  i-=1
 return None
def patch_raw(raw,name,value):
 f=family(name);n=f['patch_bytes'];o=f['patch_offset']
 if not isinstance(raw,(bytes,bytearray)) or len(raw)!=f['raw_bytes']:raise ValueError('Unexpected catalogue record size')
 if type(value) is not int or not 0<=value<1<<(8*n):raise ValueError('Unexpected raw bit width')
 out=bytearray(raw);out[o:o+n]=value.to_bytes(n,'little');return bytes(out)
