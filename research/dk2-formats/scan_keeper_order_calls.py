"""Find literal v120/direct50DE00 call leads;type each receiver separately."""
from pathlib import Path
from collections import deque
import json,re,bisect,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();base,code=code_region(data)
    labels=sorted((int(a,16),n,int(s)) for a,n,s in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M));starts=[r[0] for r in labels]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;previous=deque(maxlen=28);hits=[];decoded=0
    for x in md.disasm(code,base):
        if not x.id:continue
        decoded+=1;text=f'{x.address:08x}: {x.mnemonic} {x.op_str}';o=x.operands[0] if x.operands else None
        if x.mnemonic=='call' and ((o.type==X86_OP_MEM and o.mem.disp==0x120) or (o.type==X86_OP_IMM and o.imm==0x50DE00)):
            k=bisect.bisect_right(starts,x.address)-1;hint=None
            if k>=0 and labels[k][0]<=x.address<labels[k][0]+labels[k][2]:hint=dict(address=labels[k][0],name=labels[k][1],size=labels[k][2])
            hits.append(dict(address=x.address,instruction=text,bytes=x.bytes.hex(),previous=list(previous),following=[f'{y.address:08x}: {y.mnemonic} {y.op_str}' for y in md.disasm(code[x.address-base:x.address-base+100],x.address,count=18)],function_hint=hint))
        previous.append(text)
    out=dict(scope=__doc__,file=str(exe),sha256=hashlib.sha256(data).hexdigest(),decoded_instructions=decoded,candidates=hits)
    (ROOT/'phase84-order-call-candidates.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    for h in hits:print(hex(h['address']),h['function_hint']['name'] if h['function_hint'] else 'unmapped',h['function_hint']['size'] if h['function_hint'] else '')
    print(len(hits),'untyped call candidates')
if __name__=='__main__':main()
