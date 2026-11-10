"""Pointer-return/field-getter leads missed by forward-only owner taint.

No function is created here. Matches are candidates until actual entry/type
and consumers are checked. Also records one bounded old-code Monk anchor.
"""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from compare_rc3_converters import code_region
import emulate_crypt_job_selection as native
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=native.sha

def main():
    dx=native.EXE.read_bytes();base,code=code_region(dx);md=Cs(CS_ARCH_X86,CS_MODE_32)
    a,ch,s,file=native.execute(next(c for c in native.cases() if c['name']=='direct-job28'));post=post_context(ch,s['result'])
    actor=native.probe.old.ACTOR;vt=native.u(post[actor],0);vbytes=native.p.pe_read(dx,vt,0x130)
    candidates=[]
    patterns=[('entity_owner_pointer',re.compile(re.escape(bytes.fromhex('8b412ac3')),re.S)),
        ('this_relative_pointer',re.compile(b'\x8d\x81(.{4})\xc3',re.S)),
        ('signed_byte_value',re.compile(b'\x0f\xbe\x81(.{4})\xc3',re.S)),
        ('unsigned_byte_value',re.compile(b'\x0f\xb6\x81(.{4})\xc3',re.S)),
        ('byte_value',re.compile(b'\x8a\x81(.{4})\xc3',re.S))]
    for kind,pattern in patterns:
        for m in pattern.finditer(code):
            disp=0x2A if kind=='entity_owner_pointer' else int.from_bytes(m.group(1),'little',signed=True)
            if kind!='entity_owner_pointer' and not 0x1000<=disp<=0x109B:continue
            address=base+m.start();raw=m.group();ins=list(md.disasm(raw,address))
            assert sum(x.size for x in ins)==len(raw) and ins[-1].mnemonic=='ret'
            slots=[hex(i) for i in range(0,len(vbytes),4) if int.from_bytes(vbytes[i:i+4],'little')==address]
            candidates.append(dict(address=hex(address),kind=kind,displacement=hex(disp),hex=raw.hex(),
                instructions=[dict(address=hex(x.address),text=x.mnemonic+' '+x.op_str) for x in ins],
                actual_Creature_vtable_slots=slots))
    old=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();ob,oc=code_region(old)
    # This exact8-byte arithmetic tail is a narrow anchor, not a semantic
    # absence proof. Register allocation/compiler differences can defeat it.
    anchor=bytes.fromhex('0fbec0f7d88d3cc7');oldhits=[];pos=oc.find(anchor)
    while pos!=-1:oldhits.append(hex(ob+pos));pos=oc.find(anchor,pos+1)
    assert native.p.pe_read(dx,0x4B2E9F,8)==anchor
    out=dict(phase=189,status='untyped_pointer_return_leads',program_sha256=sha(dx),candidates=candidates,
        actual_Creature_vtable=dict(address=hex(vt),bytes=len(vbytes),sha256=sha(vbytes),
            native_reference=dict(file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()))),
        old_Monk_anchor=dict(old_sha256=sha(old),DX_address='0x4b2e9f',anchor_hex=anchor.hex(),old_literal_matches=oldhits,
            limitation='Exactregister/opcodetailonly; zeromatchesdoesnotproveoldCryptorMonkabsent.'),
        scope='Leafreturnshapesonly;memoryoffsetsarehints.Caller/VtableentryandtypedreceiverproofrequiredbeforeassigningOwnersemantics.')
    (ROOT/'phase189-owner-return-producer-leads.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    print('Actorvtable',hex(vt),'return/getterleads',[(x['address'],x['kind'],x['displacement'],x['actual_Creature_vtable_slots']) for x in candidates])
    print('oldnarrowMonktail',oldhits)

if __name__=='__main__':main()
