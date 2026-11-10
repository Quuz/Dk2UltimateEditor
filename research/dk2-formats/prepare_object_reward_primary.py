"""Whole-function and original RTTI/vtable/import data attestation."""
from pathlib import Path
import json,re,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
ADDRESSES=[0x4b7a90,0x4b7190,0x4b5ef0,0x49b1c0,0x50dc10,0x50db20,0x4b7260,0x4b7280,0x4ba420,0x4c6e70,0x4bab80,0x49c760,0x634f30,
 0x635120,0x635000,0x6352b0,0x6352d0,0x554b30,0x507850,0x4b3610,0x402ae0,0x4b1df0,0x4b28d0,0x4b4a10,0x4b4120,0x4b4c30,0x4b4d40,
 0x4b5560,0x4b5020,0x4b4e70,0x4b4f60,0x4545c0,0x4b5f30,0x4b37d0,0x4b49d0,0x4b41e0,0x4f1780,0x4e4d90,0x4e52f0,0x5558d0,
 0x4f1600,0x4f1660,0x51db30,0x50f7e0]
def main():
    mapping={}
    for file in ['phase80-primary-manifest.json','phase88-sacrifice-primary-manifest.json']:
        for r in json.loads((ROOT/file).read_text())['functions']:mapping[r['address']]=r['assembly']
    for prefix in ['phase89-sacrifice','phase89-creation','phase89-object','phase89-rtti']:
        for p in (ROOT/'ghidra').glob(prefix+'-*-assembly.txt'):
            first=p.read_text().splitlines()[0]
            if re.match('^[0-9a-fA-F]{8}:',first):mapping[int(first[:8],16)]=p.name
    q=[];functions=[];md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a in ADDRESSES:
        if a not in mapping:
            for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase89-rtti-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
            continue
        file=mapping[a];aa=[int(s[:8],16) for s in (ROOT/'ghidra'/file).read_text().splitlines() if re.match('^[0-9a-fA-F]{8}:',s)]
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a;n=last.address+last.size-a;key=f'phase89-primary-{a:x}'
        functions.append(dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Whole byte span; not a branch coverage claim'))
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    if len(functions)==len(ADDRESSES):
        blocks=[]
        def block(a,n,label):
            key='phase89-data-'+label;blocks.append(dict(exe=str(EXE),address=a,length=n,memory=key+'.txt',label=label))
            if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
        for a,n,label in [(0x66e3ec,0x400,'World-vtable'),(0x66d650,0x104,'Object-vtable-with-locator'),(0x66c1b8,4,'IsBadReadPtr-IAT'),(0x66d428,8,'Gold-scale-coefficient')]:block(a,n,label)
        u=lambda a:struct.unpack('<I',pe_read(data,a,4))[0]
        locator=u(0x66d650);hier=u(locator+16);array=u(hier+12);count=u(hier+8);assert 0<count<32
        block(locator,20,'Object-COL');block(hier,16,'Object-hierarchy');block(array,4*count,'Object-base-array');bases=[]
        for i in range(count):
            desc=u(array+4*i);typ=u(desc);name=pe_read(data,typ+8,80).split(b'\0')[0].decode('ascii')
            block(desc,28,f'Object-base-{i}');block(typ,9+len(name),f'Object-type-{i}')
            bases.append(dict(descriptor=desc,type_descriptor=typ,name=name,member_displacement=struct.unpack('<i',pe_read(data,desc+8,4))[0]))
        assert any(r['type_descriptor']==0x69efd8 and r['name']=='.?AVCMovingThing@@' for r in bases)
        q.append(dict(id='phase89-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
        (ROOT/'phase89-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks,object_rtti=bases),indent=2),encoding='utf8')
    (ROOT/'phase89-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'of',len(ADDRESSES),'whole functions;',len(q),'queries')
if __name__=='__main__':main()
