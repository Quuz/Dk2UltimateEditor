"""Direct-call cleanup from frozen, byte-attested whole DX function extents.

RET immediates describe listed return sites, not inferred C++ signatures. Reject
functions with indirect/out-of-range JMPs or inconsistent return cleanup. Zero-
pop callees with unknown arguments invalidate tracked stack values. No indirect
callsite binding or memory-value/type inference is supplied by this module.
"""
from pathlib import Path
from functools import cache
import json,re,hashlib,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import EXE,pe_read
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
INPUTS='phase161-ABI-input-manifests.json'
EXTRA=['phase161-trap-creation-primary.json']

def discover():
    if (ROOT/INPUTS).exists():return json.loads((ROOT/INPUTS).read_text(encoding='utf8'))
    inputs=[];digest=sha(EXE.read_bytes())
    for path in sorted(ROOT.glob('*primary*.json')):
        try:d=json.loads(path.read_text(encoding='utf8'))
        except (ValueError,UnicodeError):continue
        if not isinstance(d,dict) or not isinstance(d.get('profile'),dict) or d['profile'].get('sha256')!=digest:continue
        if not isinstance(d.get('functions'),list) or not all(isinstance(x,dict) and all(k in x for k in ['address','length','assembly','memory','bytes_sha256']) for x in d['functions']):continue
        inputs.append(dict(file=path.name,sha256=sha(path.read_bytes())))
    assert inputs
    p=ROOT/INPUTS
    if p.exists():assert json.loads(p.read_text(encoding='utf8'))==inputs
    else:p.write_text(json.dumps(inputs,indent=2),encoding='utf8')
    return inputs

@cache
def evidence():
    blob=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;rows={};rejected=[]
    sources=json.loads((ROOT/INPUTS).read_text(encoding='utf8'))+[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in EXTRA]
    for source in sources:
        p=ROOT/source['file'];assert sha(p.read_bytes())==source['sha256'];d=json.loads(p.read_text(encoding='utf8'))
        assert d['profile']['sha256']==sha(blob)
        for r in d['functions']:
            a=r['address'];a=int(a,0) if isinstance(a,str) else a
            if not 0x400000<=a<0x670000:continue
            text=(ROOT/r['assembly']).read_text(encoding='utf8');b=bytes(json.loads((ROOT/r['memory']).read_text(encoding='utf8'))['data'])
            assert len(b)==r['length'] and b==pe_read(blob,a,r['length']) and sha(b)==r['bytes_sha256']
            if 'assembly_sha256' in r:assert sha((ROOT/r['assembly']).read_bytes())==r['assembly_sha256']
            if 'memory_sha256' in r:assert sha((ROOT/r['memory']).read_bytes())==r['memory_sha256']
            addresses=[int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',text,re.M)]
            if not addresses or addresses[0]!=a:
                rejected.append(dict(target=a,source_manifest=source['file'],reason='Savedlistingdoesnotstartatrequestedentry; notacceptedforABI'));continue
            ins=[next(md.disasm(pe_read(blob,pc,15),pc)) for pc in addresses]
            returns=[x for x in ins if x.mnemonic.startswith('ret')];pops={int(x.op_str,0) if x.op_str else 0 for x in returns}
            outgoing=[x for x in ins if x.mnemonic=='jmp' and (x.operands[0].type!=X86_OP_IMM or not a<=x.operands[0].imm<a+r['length'])]
            if len(pops)!=1 or outgoing:
                rejected.append(dict(target=a,source_manifest=source['file'],return_pops=sorted(pops),outgoing_or_indirect_jumps=[hex(x.address) for x in outgoing]));continue
            pop=pops.pop();args=pop if pop else None
            if not pop and not any(x.mnemonic=='call' or any(o.type==X86_OP_MEM and x.reg_name(o.mem.base) in ['esp','ebp'] for o in x.operands) for x in ins):args=0
            row=dict(target=a,callee_pop=pop,argument_bytes=args,source_manifest=source['file'],assembly=r['assembly'],memory=r['memory'],bytes_sha256=sha(b),
                return_instructions=[dict(address=x.address,bytes=x.bytes.hex(),instruction=x.mnemonic+' '+x.op_str) for x in returns])
            if a in rows:assert (rows[a]['callee_pop'],rows[a]['argument_bytes'])==(pop,args)
            rows[a]=row
    return rows,rejected

def resolve(site,direct_target):
    row=evidence()[0].get(direct_target) if direct_target is not None else None
    return None if row is None else dict(row,callsite=site,binding='directCALL-only')

if __name__=='__main__':
    sources=discover()+[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in EXTRA];rows,rejected=evidence();blob=EXE.read_bytes()
    values={hex(slot):hex(struct.unpack('<I',pe_read(blob,0x66E3EC+slot,4))[0]) for slot in [0x138,0x160,0x188]}
    d=dict(scope=__doc__,program_sha256=sha(blob),input_manifests=sources,functions=list(rows.values()),rejected_extents=rejected,
        original_World_lookup_slots=values,indirect_sites={},limitations='No globalABI/type proof; only consistent listedRETcleanups, ordinaryABI, no stackpointee aliaswrites model.')
    (ROOT/'phase161-catalogue-call-ABI.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(len(sources),'manifests',len(rows),'accepted directcleanup records',len(rejected),'rejected extents','lookup slots',values)
