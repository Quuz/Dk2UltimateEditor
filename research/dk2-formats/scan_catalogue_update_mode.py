"""Displacement leads for catalogue update mode; identity must be checked."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    raw=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;out=[]
    funcs=re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M)
    for address,name,size in funcs:
        a=int(address,16);n=int(size)
        try:b=pe_read(raw,a,n)
        except (ValueError,AssertionError):continue
        for x in md.disasm(b,a):
            if any(o.type==X86_OP_MEM and o.mem.disp==0xA3C7 for o in x.operands):out.append(dict(function=hex(a),name=name,address=hex(x.address),instruction=x.mnemonic+' '+x.op_str))
    (ROOT/'phase55-update-mode-leads.json').write_text(json.dumps(dict(scope=__doc__,mapped_ranges=len(funcs),rows=out),indent=2));print(json.dumps(out,indent=2))
if __name__=='__main__':main()
