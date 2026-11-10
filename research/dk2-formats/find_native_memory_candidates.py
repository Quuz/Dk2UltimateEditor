"""Instruction-level memory candidates, not proof of object identity or semantics.

Decode only Flame-mapped spans within .text. Embedded data, overlapping mappings,
unrelated vtables and reused displacements still require manual verification.
"""
from pathlib import Path
import argparse, hashlib, json, re, struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32, CS_GRP_CALL
from capstone.x86 import X86_OP_MEM

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def find(offsets,call_offsets):
    data=EXE.read_bytes();pe=struct.unpack_from('<I',data,60)[0]
    n=struct.unpack_from('<H',data,pe+6)[0];optional=struct.unpack_from('<H',data,pe+20)[0]
    base=struct.unpack_from('<I',data,pe+52)[0]
    for i in range(n):
        p=pe+24+optional+40*i
        if data[p:p+8].rstrip(b'\0')==b'.text':
            _,rva,size,raw=struct.unpack_from('<4I',data,p+8)
            origin=base+rva;code=data[raw:raw+size];break
    else:raise ValueError('No .text')
    mapping=(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(encoding='utf8')
    functions=[(int(a,16),name,int(size)) for a,name,size in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',mapping,re.M)]
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    result=[];scanned=0
    for address,name,size in functions:
        if not (origin<=address<origin+len(code)) or address+size>origin+len(code):continue
        scanned+=1;hits=[];calls=[]
        for ins in md.disasm(code[address-origin:address-origin+size],address):
            if ins.id==0:continue
            for operand in ins.operands:
                if operand.type!=X86_OP_MEM:continue
                disp=operand.mem.disp
                is_call=ins.group(CS_GRP_CALL)
                if (is_call and disp in call_offsets) or (not is_call and disp in offsets):
                    row=dict(address=hex(ins.address),text=ins.mnemonic+' '+ins.op_str,
                        displacement=hex(disp),memory_bytes=operand.size)
                    (calls if is_call else hits).append(row)
        if (not call_offsets or calls) and hits:
            result.append(dict(function=hex(address),name=name,size=size,matching_virtual_calls=calls,field_candidates=hits))
    return dict(program_sha256=hashlib.sha256(data).hexdigest(),warning=__doc__,mapped_spans_scanned=scanned,functions=result)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('offsets',nargs='+',type=lambda s:int(s,0));parser.add_argument('--call-offsets',nargs='*',default=[],type=lambda s:int(s,0))
    parser.add_argument('--output',required=True,type=Path);args=parser.parse_args()
    result=find(set(args.offsets),set(args.call_offsets));args.output.write_text(json.dumps(result,indent=2),encoding='utf8')
    print('Mapped spans:',result['mapped_spans_scanned'],'candidate functions:',len(result['functions']))
    for row in result['functions']:
        print(row['function'],row['name'],'calls',len(row['matching_virtual_calls']),'fields',len(row['field_candidates']))

if __name__=='__main__':main()
