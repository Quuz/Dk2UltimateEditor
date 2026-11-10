"""AP getter call candidates; indirect slot alone is not verified type identity."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;rows=[]
    funcs=re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M)
    for a,name,size in funcs:
        a=int(a,16);n=int(size)
        if not 0x401000<=a<0x640000:continue
        try:ins=list(md.disasm(pe_read(data,a,n),a))
        except (ValueError,AssertionError):continue
        for i,x in enumerate(ins):
            if x.mnemonic=='call' and (x.op_str=='0x50fae0' or any(o.type==X86_OP_MEM and o.mem.disp==0x2C8 for o in x.operands)):
                rows.append(dict(function=hex(a),name=name,call=hex(x.address),direct=x.op_str=='0x50fae0',instructions=[f'{j.address:08x}: {j.mnemonic} {j.op_str}' for j in ins[max(0,i-8):i+24]]))
    (ROOT/'phase56-ap-getter-leads.json').write_text(json.dumps(dict(scope=__doc__,rows=rows),indent=2));print('Getter candidate calls:',len(rows))
    for r in rows:print(r['function'],r['name'],r['call'],'\n'+'\n'.join(r['instructions']))
if __name__=='__main__':main()
