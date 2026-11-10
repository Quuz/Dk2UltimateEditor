"""Scoped caller-stack cleanup evidence from byte-attested Effect functions.

No guessed function boundaries. Existing full Ghidra function listings and
their exact PE bytes supply RET cleanup. Six indirect sites are tied to the
original Effect/CBridge vtables in the previously verified native fixtures;
these associations are scoped hints, not arbitrary-world dispatch proofs.
"""
from pathlib import Path
from functools import cache
import json,re,hashlib,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from build_effect_semantics import EXE,pe_read
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
SITES={0x438A35:0x434790,0x438A40:0x43A8E0,0x438BDD:0x434790,0x438BE8:0x43A8E0,0x4391C6:0x434790,0x4391D1:0x43A8E0}

@cache
def evidence():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;rows={}
    for file in ['phase141-effect-scale-primary.json','phase143-effect-mode18-primary.json']:
        d=json.loads((ROOT/file).read_text(encoding='utf8'));assert d['profile']['sha256']==sha(blob)
        for r in d['functions']:
            text=(ROOT/r['assembly']).read_text(encoding='utf8');assert sha((ROOT/r['assembly']).read_bytes())==r['assembly_sha256']
            m=json.loads((ROOT/r['memory']).read_text(encoding='utf8'));b=bytes(m['data']);assert b==pe_read(blob,r['address'],r['length']) and sha(b)==r['bytes_sha256']
            addresses=[int(a,16) for a in re.findall(r'^([0-9a-fA-F]{8}):',text,re.M)];ins=[next(md.disasm(pe_read(blob,a,15),a)) for a in addresses]
            returns=[x for x in ins if x.mnemonic.startswith('ret')];pops={int(x.op_str,0) if x.op_str else 0 for x in returns}
            if len(pops)!=1:continue
            pop=pops.pop();args=pop if pop else None
            no_stack_access=not any(o.type==X86_OP_MEM and x.reg_name(o.mem.base) in ['esp','ebp'] for x in ins for o in x.operands)
            if not pop and no_stack_access and not any(x.mnemonic=='call' for x in ins):args=0
            if r['address']==0x5558D0:args=16 # actual four-argument native PRNG, phase76/141/143
            row=dict(target=r['address'],callee_pop=pop,argument_bytes=args,source_manifest=file,assembly=r['assembly'],memory=r['memory'],bytes_sha256=sha(b),
                return_instructions=[dict(address=x.address,bytes=x.bytes.hex(),instruction=x.mnemonic+' '+x.op_str) for x in returns])
            if r['address'] in rows:assert (rows[r['address']]['callee_pop'],rows[r['address']]['argument_bytes'])==(pop,args)
            rows[r['address']]=row
    assert struct.unpack('<I',pe_read(blob,0x66CBB4+12,4))[0]==0x434790
    assert struct.unpack('<I',pe_read(blob,0x66CC94+0x94,4))[0]==0x43A8E0
    assert rows[0x434790]['callee_pop']==0 and rows[0x434790]['argument_bytes']==0
    assert rows[0x43A8E0]['callee_pop']==20
    return rows

def resolve(site,direct_target):
    target=direct_target if direct_target is not None else SITES.get(site)
    row=evidence().get(target)
    return None if row is None else dict(row,callsite=site,binding='directCALL' if direct_target is not None else 'scoped-original-vtable-fixture')

if __name__=='__main__':
    d=dict(functions=list(evidence().values()),indirect_sites={hex(k):hex(v) for k,v in SITES.items()},scope=__doc__)
    (ROOT/'phase158-effect-call-ABI.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(len(d['functions']),'byte-attested cleanup records')
