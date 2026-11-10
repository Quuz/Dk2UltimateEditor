"""Locate candidate instruction immediates and label ranges from Flame sgmap.
Matches are byte-pattern candidates, not disassembly proof.
"""
from pathlib import Path
import bisect,json,re,struct
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
pe=struct.unpack_from('<I',data,0x3c)[0]
n=struct.unpack_from('<H',data,pe+6)[0]
optsize=struct.unpack_from('<H',data,pe+20)[0]
base=struct.unpack_from('<I',data,pe+24+28)[0]
sections=[]
for i in range(n):
 p=pe+24+optsize+i*40
 name=data[p:p+8].split(b'\0')[0].decode()
 vsize,rva,size,raw=struct.unpack_from('<4I',data,p+8)
 if name=='.text':sections.append((rva,size,raw))
functions=[]
for m in re.finditer(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M):
 functions.append((int(m[1],16),int(m[3]),m[2]))
functions.sort();starts=[f[0] for f in functions]
def candidates(value):
 results=[]
 for rva,size,raw in sections:
  p=raw
  while True:
   p=data.find(struct.pack('<I',value),p,raw+size)
   if p<0:break
   va=base+rva+p-raw;idx=bisect.bisect_right(starts,va)-1
   f=functions[idx] if idx>=0 else None
   results.append(dict(address=f'0x{va:X}',nearby_hex=data[max(raw,p-4):p+8].hex(' '),
                       function=f'0x{f[0]:X}' if f and va<f[0]+f[1] else None,
                       function_name=f[2] if f and va<f[0]+f[1] else None))
   p+=1
 return results
if __name__=='__main__':
 out={}
 for value in [0x2344,0x2034,0x1545,0x1599,0xF20]:
  results=candidates(value);out[f'0x{value:X}']=results
  print(hex(value),len(results))
  for r in results[:30]:print(r)
 (ROOT/'game-immediate-candidates.json').write_text(json.dumps(out,indent=2))
