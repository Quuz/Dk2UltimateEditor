"""Reattest five executed functions and five static identity bodies separately."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);old=json.loads((ROOT/'phase63-primary-manifest.json').read_text());funcs=[];blocks=[];queries=[]
    for a in [0x44ACE0,0x44BD30,0x44BD70,0x56F850,0x5558D0,0x4B1DF0,0x4B28D0,0x4B4A10,0x4B0F20,0x4B8050]:
        if a in [0x44ACE0,0x44BD30,0x44BD70,0x56F850]:before=next(r['after'] for r in old['functions'] if r['address']==a)
        elif a in [0x4B0F20,0x4B8050]:before='phase38-'+('initialize' if a==0x4B0F20 else 'constructor')+'-disassemble_function.txt'
        else:before=f'phase65-game-{a:x}-disassemble_function.txt'
        aa=[int(l[:8],16) for l in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',l)];assert aa[0]==a;last=aa[-1];end=next(md.disasm(pe_read(data,last,15),last));n=last+end.size-a;key=f'phase65-final-game-{a:x}'
        funcs.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',execution_scope='full function executed in specifiedbranch/providercases' if a in [0x44ACE0,0x44BD30,0x44BD70,0x56F850,0x5558D0] else 'static identity path only;not newlyexecuted inphase65'))
        queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=10),dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10)])
    rtti=json.loads((ROOT/'phase65-body-rtti.json').read_text());assert rtti['native_name']=='.?AVCDeadBody@@'
    ranges=[('trig',0x6C1488,0x2800,'Initialized overlapping trig tables read bynativecamera;not recomputed sin/cos.'),('dispatch',0x44BB8C,0x1A2,'Initialized activation jump/index tables.'),('eye-converter-fragment',0x568E9D,18,'Three-instruction staticrawEB0→mid12A0fragment;not fullnewconverterexecution.'),('eye-loader-fragment',0x4923C1,21,'PresenceF4 gateandmid12A0→definition65Clow16;21-bytefragmentexecuted inthreechains,restofloaderbypassed.'),('body-vtable',rtti['vtable']-4,36,'RTTI locator pointer andfirst8vslots.'),('body-locator',rtti['locator'],20,'Complete object locator.'),('body-type',rtti['type_descriptor'],rtti['type_bytes'],'Native CDeadBody type descriptor.')]
    for name,a,n,scope in ranges:
        key='phase65-final-data-'+name;blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',scope=scope,label=name));queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=10))
    (ROOT/'phase65-primary-manifest.json').write_text(json.dumps(dict(functions=funcs,blocks=blocks),indent=2),encoding='utf8');(ROOT/'phase65-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8');print('Wholefunctions',len(funcs),'executed5/static5;explicitranges',len(blocks))
if __name__=='__main__':main()
