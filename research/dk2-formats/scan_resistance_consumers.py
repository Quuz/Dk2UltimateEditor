"""Reproduce finite direct displacement leads and bind canonical Ghidra contexts.

This is not a proof against indexed/adjusted pointers, copies, other binaries or
all possible consumers. Non-gameplay matches are classified by complete context.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import EXE,pe_read
from audit_targeted_spell import executable_ranges

ROOT=Path(__file__).resolve().parent
CLASSIFICATION={0x491DC0:'Creature definition constructor',0x492230:'Creature definition destructor',
    0x492320:'Creature definition loader',0x51F3B0:'Creature definition copy, not damage calculation',
    0x55C020:'Other resource/path manager initialization',0x55DF40:'Other resource manager subobject constructor',
    0x5A8980:'Other MovieRenderer context',0x5A8B20:'Other MovieRenderer context',
    0x64B43E:'Creature array exception-unwind cleanup',0x64B51E:'Creature array exception-unwind cleanup',
    0x65C120:'Other video pixel-buffer fill'}


def main():
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;rows=[]
    primary=json.loads((ROOT/'phase113-primary-manifest.json').read_text(encoding='utf8'))['functions']
    for start,n in executable_ranges(data):
        for ins in md.disasm(pe_read(data,start,n),start):
            if not ins.id:continue
            for op in ins.operands:
                selected=op.type==X86_OP_MEM and ((op.mem.disp<0x624 and op.mem.disp+max(1,op.size)>0x61C) or op.mem.disp==0x78A)
                selected|=op.type==X86_OP_IMM and op.imm in [0x61C,0x61D,0x78A]
                if not selected:continue
                owner=next(r for r in primary if r['address']<=ins.address<r['address']+r['length'])
                assert owner['address'] in CLASSIFICATION
                assembly=(ROOT/'ghidra'/owner['assembly']).read_text(encoding='utf8')
                assert f'{ins.address:08x}:' in assembly.lower()
                rows.append(dict(address=ins.address,hex=ins.bytes.hex(),text=ins.mnemonic+' '+ins.op_str,
                                 function=owner['address'],classification=CLASSIFICATION[owner['address']],assembly=owner['assembly']))
    assert len(rows)==20
    result=dict(profile=dict(path=str(EXE),sha256=hashlib.sha256(data).hexdigest()),leads=rows,
        coverage='Finite linear executable-section operands for61C..623/78A and exact immediates61C/61D/78A; canonical instruction addresses verified.',
        conclusion='No damage-use candidate among these20direct leads. Native matched melee separately confirms no application.',
        limitations='Not proof of global unuse: adjusted/indexed pointers, dynamic copies, other binaries and all spell paths not exhausted.')
    (ROOT/'phase113-resistance-consumer-review.json').write_text(json.dumps(result,indent=2),encoding='utf8')
    print('20 canonical direct leads classified; no global absence claim')


if __name__=='__main__':main()
