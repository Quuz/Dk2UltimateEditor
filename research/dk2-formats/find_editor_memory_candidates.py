"""Find exact memory displacement candidates in Ghidra-mapped editor functions.

Candidates are not object identities. Embedded data and pointer aliases require
manual verification; scanning between entry points can include alignment data.
"""
from pathlib import Path
import argparse,bisect,hashlib,json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'

def scan(offsets,immediates=()):
    data=EXE.read_bytes();pe=struct.unpack_from('<I',data,60)[0]
    n,opt=struct.unpack_from('<H',data,pe+6)[0],struct.unpack_from('<H',data,pe+20)[0]
    imagebase=struct.unpack_from('<I',data,pe+52)[0];regions=[]
    for i in range(n):
        p=pe+24+opt+40*i;vs,rva,size,raw=struct.unpack_from('<4I',data,p+8)
        flags=struct.unpack_from('<I',data,p+36)[0]
        if flags&0x20000000:regions.append((imagebase+rva,data[raw:raw+size]))
    funcs=sorted(int(a,16) for a in re.findall(r' at ([0-9a-fA-F]{8})',
        (ROOT/'ghidra/editor-phase8-functions.txt').read_text()))
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True
    results=[]
    for origin,code in regions:
        entries=[a for a in funcs if origin<=a<origin+len(code)]
        for index,start in enumerate(entries):
            end=entries[index+1] if index+1<len(entries) else origin+len(code)
            hits=[]
            for ins in md.disasm(code[start-origin:end-origin],start):
                if not ins.id:continue
                for op in ins.operands:
                    if op.type==X86_OP_MEM and op.mem.disp in offsets:
                        hits.append(dict(address=hex(ins.address),instruction=ins.mnemonic+' '+ins.op_str,
                            displacement=hex(op.mem.disp),memory_bytes=op.size))
                    elif op.type==X86_OP_IMM and op.imm in immediates:
                        hits.append(dict(address=hex(ins.address),instruction=ins.mnemonic+' '+ins.op_str,
                            immediate=hex(op.imm)))
            if hits:results.append(dict(function=hex(start),end_bound=hex(end),candidates=hits))
    return dict(program_sha256=hashlib.sha256(data).hexdigest(),warning=__doc__,functions=results)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('offsets',nargs='+',type=lambda v:int(v,0))
    parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    r=scan(set(args.offsets));args.output.write_text(json.dumps(r,indent=2),encoding='utf8')
    print('Candidate functions:',len(r['functions']))
    for f in r['functions']:print(f['function'],len(f['candidates']))

if __name__=='__main__':main()
