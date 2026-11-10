"""Untyped runtime nav-pointer leads; address matches alone are not semantics."""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent

def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();base,code=code_region(data)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;recent=[];pending=[];rows=[]
    for x in md.disasm(code,base):
        if not x.id:continue
        row=dict(address=hex(x.address),instruction=x.mnemonic+' '+x.op_str,bytes=x.bytes.hex())
        for r in pending:
            if x.address-int(r['address'],16)<100:r['after'].append(row)
        recent.append(row);recent=recent[-16:]
        if any(o.type==X86_OP_MEM and o.mem.base and o.mem.disp==0x9f2 for o in x.operands) or any(o.type==X86_OP_IMM and o.imm==0x9f2 for o in x.operands):
            r=dict(row,before=list(recent),after=[]);rows.append(r);pending.append(r)
        pending=[r for r in pending if x.address-int(r['address'],16)<100]
    (ROOT/'phase99-navigation-map-producer-leads.json').write_text(json.dumps(dict(exe_sha256=hashlib.sha256(data).hexdigest(),scope=__doc__,candidates=rows),indent=2),encoding='utf8')
    print('Nav pointer displacement leads',len(rows))
    for r in rows:print(r['address'],r['instruction'])

if __name__=='__main__':main()
