"""Locate homologous converters by full instruction bytes with relocations masked.

Mask direct external branch immediates and absolute initialized-image pointers.
All otherbytes,including field offsets,branch structure andcopy counts mustmatch.
This is a candidate matcher,not a universal whole-executable equivalence proof.
"""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_GRP_CALL,CS_GRP_JUMP
from capstone.x86 import X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
PRO=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
RC3=ROOT/'source-binaries/DK2-Editor-rc3.exe'
def code_region(data):
    pe=struct.unpack_from('<I',data,60)[0];opt=struct.unpack_from('<H',data,pe+20)[0];base=struct.unpack_from('<I',data,pe+52)[0]
    for j in range(struct.unpack_from('<H',data,pe+6)[0]):
        o=pe+24+opt+40*j
        if data[o:o+8].rstrip(b'\0')==b'.text':
            _,rva,n,raw=struct.unpack_from('<4I',data,o+8);return base+rva,data[raw:raw+n]
    raise ValueError('No.text')
def main():
    a=PRO.read_bytes();b=RC3.read_bytes();origin,code=code_region(b);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;out=[];qs=[]
    for name in ['Terrain','Rooms','Creatures','Doors','Traps','Objects','CreatureSpells','KeeperSpells','Shots','Effects','EffectElements']:
        file=f'phase50-pro-converter-{name}'+('-actual' if name=='Shots' else '')+'.txt';lines=(ROOT/'ghidra'/file).read_text().splitlines();addresses=[int(l[:8],16) for l in lines if re.match(r'^[0-9a-f]{8}:',l)];start=addresses[0];last=addresses[-1];end=next(md.disasm(pe_read(a,last,15),last));n=last+end.size-start;raw=pe_read(a,start,n);mask=bytearray([255])*n;relocs=[]
        for ins in md.disasm(raw,start):
            i=ins.address-start
            for op in ins.operands:
                if op.type==X86_OP_IMM:
                    target=op.imm&0xFFFFFFFF
                    externalbranch=(ins.group(CS_GRP_CALL) or ins.group(CS_GRP_JUMP)) and not start<=target<start+n
                    absolute=0x400000<=target<0x580000 and not ins.group(CS_GRP_JUMP)
                    if externalbranch or absolute:
                        off=i+ins.imm_offset;mask[off:off+ins.imm_size]=bytes(ins.imm_size);relocs.append(dict(instruction=hex(ins.address),offset=off,bytes=ins.imm_size,target=hex(target),kind='branch' if externalbranch else 'absolute'))
                elif op.type==X86_OP_MEM and not op.mem.base and not op.mem.index and 0x400000<=op.mem.disp<0x580000:
                    off=i+ins.disp_offset;mask[off:off+ins.disp_size]=bytes(ins.disp_size);relocs.append(dict(instruction=hex(ins.address),offset=off,bytes=ins.disp_size,target=hex(op.mem.disp),kind='absolute_memory'))
        spans=[];s=0
        while s<n:
            if not mask[s]:s+=1;continue
            e=s
            while e<n and mask[e]:e+=1
            spans.append((s,e));s=e
        s,e=max(spans,key=lambda t:t[1]-t[0]);needle=raw[s:e];pos=code.find(needle);matches=[]
        while pos!=-1:
            index=pos-s
            if index>=0 and index+n<=len(code) and all(not m or x==y for x,y,m in zip(raw,code[index:index+n],mask)):matches.append(origin+index)
            pos=code.find(needle,pos+1)
        print(name,hex(start),n,'matches',list(map(hex,matches)))
        row=dict(family=name,pro_address=start,bytes=n,pro_assembly=file,masked_relocations=relocs,rc3_candidates=matches,unmasked_bytes=sum(bool(m) for m in mask),mask_hex=mask.hex());out.append(row)
        for rc in matches:
            for t in ['create_function','disassemble_function','force_decompile']:
                ar=dict(program='DK2-Editor-rc3.exe',address=hex(rc))
                if t=='create_function':ar['disassemble_first']=True
                qs.append(dict(id=f'phase50-rc3-converter-{name}-{t}',tool=t,args=ar,print_limit=15))
    (ROOT/'phase50-converter-match-candidates.json').write_text(json.dumps(out,indent=2),encoding='utf8');(ROOT/'phase50-rc3-converter-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8')
if __name__=='__main__':main()
