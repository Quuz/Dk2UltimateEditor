"""New raw-array roots at World1604,not previously covered getter-return seeds.

Every displacement hit remains untyped until native context proves World.
Linear text decoding is bounded evidence,not proof of global non-use.
"""
from pathlib import Path
import json,re,hashlib,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_REG
from compare_rc3_converters import code_region
from native_memory_access import reads_memory
ROOT=Path(__file__).resolve().parent

def main():
    labels=[(int(a,16),name,int(size)) for a,name,size in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(),re.M)]
    labels.sort();starts=[r[0] for r in labels];out=[];queries=[];functions=set()
    for profile,path,program in [('DX',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe','DKII-DX.exe'),('old',ROOT/'source-binaries/DKII-local13.exe','DKII-local13.exe')]:
        data=path.read_bytes();base,code=code_region(data);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;md.skipdata=True;hits=[];decoded=0
        for x in md.disasm(code,base):
            if not x.id:continue
            decoded+=1
            for o in x.operands:
                if o.type!=X86_OP_MEM or o.mem.disp!=0x1604:continue
                k=bisect.bisect_right(starts,x.address)-1;hint=None
                if profile=='DX' and k>=0 and labels[k][0]<=x.address<labels[k][0]+labels[k][2]:hint=dict(address=labels[k][0],name=labels[k][1],size=labels[k][2])
                rows=[f'{i.address:08x}: {i.mnemonic} {i.op_str}' for i in md.disasm(code[x.address-base:x.address-base+100],x.address,count=16)]
                hits.append(dict(address=x.address,hex=hex(x.address),instruction=x.mnemonic+' '+x.op_str,bytes=x.bytes.hex(),memory_bytes=o.size,
                  read=reads_memory(x,o),address_only=x.mnemonic=='lea',function_hint=hint,following=rows))
                if hint:functions.add(hint['address'])
                if profile=='old':queries.append(dict(id=f'phase83-old-array1604-{x.address:x}-memory',tool='read_memory',args=dict(program=program,address=hex(x.address),length=100),print_limit=5))
        out.append(dict(profile=profile,program=program,file=str(path),sha256=hashlib.sha256(data).hexdigest(),decoded=decoded,hits=hits))
    for a in sorted(functions):
        for label,tool in [('create','create_function'),('assembly','disassemble_function'),('decompile','force_decompile')]:
            args=dict(program='DKII-DX.exe',address=hex(a))
            if tool=='create_function':args['disassemble_first']=True
            queries.append(dict(id=f'phase83-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
    (ROOT/'phase83-array-root-candidates.json').write_text(json.dumps(dict(scope=__doc__,profiles=out),indent=2),encoding='utf8')
    (ROOT/'phase83-array-root-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    for r in out:
        print(r['profile'],r['decoded'],'decoded;',len(r['hits']),'untyped array-displacement hits')
        for h in r['hits']:print(h['hex'],h['instruction'],h['function_hint']['name'] if h['function_hint'] else '')

if __name__=='__main__':main()
