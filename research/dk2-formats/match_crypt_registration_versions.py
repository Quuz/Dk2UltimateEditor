"""Bounded old-build lead: preserve fields/control flow, mask relocations only.

This locates a possible homologous Crypt registration routine. It does not
establish counter semantics or whole-executable equivalence.
"""
from pathlib import Path
import json,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_GRP_CALL,CS_GRP_JUMP
from capstone.x86 import X86_OP_IMM,X86_OP_MEM
from compare_rc3_converters import code_region
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def main():
    baseline=json.loads((ROOT/'phase165-source-baseline.json').read_text())['executable_paths']
    dx=Path(baseline[0]['file']);old=ROOT/'source-binaries/DKII-local13.exe'
    a=dx.read_bytes();b=old.read_bytes()
    assert sha(a)=='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
    assert sha(b)=='93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342'
    row=next(r for r in json.loads((ROOT/'phase94-primary-manifest.json').read_text())['functions'] if r['address']==0x4D8E60)
    start,n=row['address'],row['length'];raw=pe_read(a,start,n);mask=bytearray([255])*n;relocs=[]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    for ins in md.disasm(raw,start):
        if not ins.id:continue
        for op in ins.operands:
            if op.type==X86_OP_IMM:
                value=op.imm&0xFFFFFFFF;branch=ins.group(CS_GRP_CALL) or ins.group(CS_GRP_JUMP)
                if (branch and not start<=value<start+n) or (not branch and 0x400000<=value<0x800000):
                    at=ins.address-start+ins.imm_offset;mask[at:at+ins.imm_size]=bytes(ins.imm_size)
                    relocs.append(dict(pc=hex(ins.address),bytes=ins.imm_size,offset=at,kind='external_branch_or_image_address',value=hex(value)))
            elif op.type==X86_OP_MEM and not op.mem.base and 0x400000<=op.mem.disp<0x800000:
                at=ins.address-start+ins.disp_offset;mask[at:at+ins.disp_size]=bytes(ins.disp_size)
                relocs.append(dict(pc=hex(ins.address),bytes=ins.disp_size,offset=at,kind='absolute_image_memory',value=hex(op.mem.disp)))
    spans=[];i=0
    while i<n:
        if not mask[i]:i+=1;continue
        end=i
        while end<n and mask[end]:end+=1
        spans.append((i,end));i=end
    left,right=max(spans,key=lambda p:p[1]-p[0]);origin,code=code_region(b);matches=[]
    pos=code.find(raw[left:right])
    while pos!=-1:
        offset=pos-left
        if 0<=offset<=len(code)-n and all(not m or x==y for x,y,m in zip(raw,code[offset:offset+n],mask)):
            matches.append(origin+offset)
        pos=code.find(raw[left:right],pos+1)
    prefix_leads=[]
    for size in [32,64,96,128,256]:
        prefix_spans=[(s,min(e,size)) for s,e in spans if s<size]
        s,e=max(prefix_spans,key=lambda p:p[1]-p[0]);found=[];pos=code.find(raw[s:e])
        while pos!=-1:
            offset=pos-s
            if 0<=offset<=len(code)-size and all(not m or x==y for x,y,m in zip(raw[:size],code[offset:offset+size],mask[:size])):found.append(hex(origin+offset))
            pos=code.find(raw[s:e],pos+1)
        prefix_leads.append(dict(bytes=size,candidates=found))
    result=dict(phase=189,status='static_matching_lead_only',DX=dict(file=str(dx),sha256=sha(a),function=start,length=n,bytes_sha256=sha(raw)),
        old=dict(file=str(old),sha256=sha(b)),mask_hex=mask.hex(),unmasked_bytes=sum(bool(x) for x in mask),relocations=relocs,
        old_candidates=[dict(address=hex(x),bytes_sha256=sha(pe_read(b,x,n))) for x in matches],
        partial_prefix_leads=prefix_leads,
        scope='Allunmaskedbytesincludinginstancefieldoffsets/localbranches/speciesdispatchmustmatch. Newconsumersemanticsrequireprimaryandtypedverification.',
        previous_goal_turn=dict(classification='progress',reason='Phase188publishedandauditedraw/sourceJob28entry; remaining10ownercountersarecurrentpriority.'),
        limitations=['Oneprovidedoldhashisnotcertificationofallretail1.3builds.','Noabsence-of-useclaim; do nottransferoldconsumersautomaticallytoDX.'])
    (ROOT/'phase189-crypt-registration-version-match.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('DXsetterbytes',n,'fixed',result['unmasked_bytes'],'oldcandidates',[hex(x) for x in matches])
    print('partialprefixes',prefix_leads)

if __name__=='__main__':main()
