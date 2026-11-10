"""Old-profile scalar neighbourhood leads; expected offsets remain hypotheses."""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from compare_rc3_converters import code_region
ROOT=Path(__file__).resolve().parent
def main():
    data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    recent=[];pending=[];rows=[];targets=set()
    for x in md.disasm(code,base):
        if not x.id:continue
        row=dict(address=hex(x.address),text=x.mnemonic+' '+x.op_str,hex=x.bytes.hex())
        for r in pending:
            if x.address-int(r['address'],16)<80:r['after'].append(row)
        recent.append(row);recent=recent[-13:]
        if x.mnemonic=='call' and x.operands[0].type==X86_OP_IMM:targets.add(x.operands[0].imm)
        matches=[]
        for o in x.operands:
            if o.type==X86_OP_MEM and not o.mem.base and 0x6dd948<=o.mem.disp<0x6dd9a0:matches.append('absolute/indexedscalar-neighbourhood')
            elif o.type==X86_OP_IMM and 0x6dd948<=o.imm<0x6dd9a0:matches.append('immediatescalar-neighbourhood')
        if matches:r=dict(row,matches=matches,before=list(recent),after=[],nearest_direct_call_hint=hex(max((a for a in targets if a<=x.address),default=base)));rows.append(r);pending.append(r)
        pending=[r for r in pending if x.address-int(r['address'],16)<80]
    v=dict(exe_sha256=hashlib.sha256(data).hexdigest(),candidate_base=0x6dd5d8,hypothesized113=0x6dd97a,hypothesized114=0x6dd97c,
      scope=__doc__,candidates=rows)
    (ROOT/'phase101-old-possession-scalar-leads.json').write_text(json.dumps(v,indent=2),encoding='utf8');print('Scalarneighbourhood leads',len(rows))
    for r in rows:
        if '6dd97' in r['text']:print(r['address'],r['text'],r['nearest_direct_call_hint'])
if __name__=='__main__':main()
