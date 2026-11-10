"""Search all PE .text, including code absent from the saved function map.

Linear disassembly and displacement-anchored decodes are only candidates.
Anchored results can start inside another instruction or embedded data. Verify
each candidate against a function body and object identity before using it.
"""
from pathlib import Path
import argparse,bisect,hashlib,json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM

ROOT=Path(__file__).resolve().parent

def scan(exe,offsets):
    data=exe.read_bytes();pe=struct.unpack_from('<I',data,60)[0];n=struct.unpack_from('<H',data,pe+6)[0];optional=struct.unpack_from('<H',data,pe+20)[0]
    base=struct.unpack_from('<I',data,pe+52)[0]
    for i in range(n):
        p=pe+24+optional+40*i
        if data[p:p+8].rstrip(b'\0')==b'.text':
            _,rva,size,raw=struct.unpack_from('<4I',data,p+8);origin=base+rva;code=data[raw:raw+size];break
    else:raise ValueError('No .text section')
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;found={}
    def accept(ins,source):
        if not ins.id:return
        for op in ins.operands:
            if op.type!=X86_OP_MEM or op.mem.disp not in offsets:continue
            key=(ins.address,op.mem.disp)
            if key not in found:
                found[key]=dict(address=hex(ins.address),instruction=ins.mnemonic+' '+ins.op_str,
                    displacement=hex(op.mem.disp),memory_bytes=op.size,base=ins.reg_name(op.mem.base),
                    index=ins.reg_name(op.mem.index),access=op.access,sources=[])
            if source not in found[key]['sources']:found[key]['sources'].append(source)
    for ins in md.disasm(code,origin):accept(ins,'linear')
    md.skipdata=False
    for disp in sorted(offsets):
        needle=struct.pack('<I',disp);position=code.find(needle)
        while position!=-1:
            for start in range(max(0,position-8),position):
                instructions=list(md.disasm(code[start:start+15],origin+start,count=1))
                if instructions and instructions[0].address+instructions[0].size>=origin+position+4:accept(instructions[0],'displacement-anchored')
            position=code.find(needle,position+1)
    if exe.name=='DKII-DX.exe':
        mapping=(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(encoding='utf8')
        funcs=sorted((int(a,16),name,int(size)) for a,name,size in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',mapping,re.M))
        starts=[f[0] for f in funcs]
        for row in found.values():
            address=int(row['address'],16);index=bisect.bisect_right(starts,address)-1
            if index>=0:
                start,name,size=funcs[index]
                if address<start+size:row['mapped_span']=dict(address=hex(start),name=name,size=size)
    return dict(program=str(exe),program_sha256=hashlib.sha256(data).hexdigest(),text_origin=hex(origin),text_bytes=len(code),
        offsets=[hex(v) for v in sorted(offsets)],warning=__doc__,candidates=sorted(found.values(),key=lambda r:int(r['address'],16)))

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('exe',type=Path);p.add_argument('offsets',nargs='+',type=lambda x:int(x,0));p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    result=scan(a.exe,set(a.offsets));a.output.write_text(json.dumps(result,indent=2),encoding='utf8')
    print('Candidate instructions:',len(result['candidates']))

if __name__=='__main__':main()
