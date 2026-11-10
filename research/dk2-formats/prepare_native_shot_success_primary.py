"""Capture complete post-execution code spans and original vtable/math data."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
FUNCTIONS=[0x4B7CD0,0x554B30,0x4B7D60,0x507850,0x402AE0,0x4B3610,0x41F900,0x4AAC90,0x4B1DF0,0x4B28D0,0x4B4A10,0x4B4120,0x4B4C30,0x4B4D40,0x4B5560,0x4B5020,0x4B4E70,0x4B4F60,0x4545C0,0x4B5F30,0x40CFD0,0x4AAF60,0x40D640,0x4B37D0,0x4B41E0,0x4B49D0,0x50E2D0,0x50E1E0,
 0x4982B0,0x4984D0,0x4AB830,0x4AB8A0,0x4ABA10,0x65FAE0,0x65FB54,0x4B7E30,0x4943F0,0x517490,0x50E080,0x50DFA0]
def main():
    q=[]
    if len(sys.argv)==1:
        for a in FUNCTIONS:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase80-final-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        (ROOT/'phase80-final-code-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');return
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[]
    for a in FUNCTIONS:
        assembly=f'phase80-final-DX-{a:x}-assembly.txt';aa=[int(line[:8],16) for line in (ROOT/'ghidra'/assembly).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));assert aa[0]==a and last.mnemonic in ['ret','jmp'];n=last.address+last.size-a;key=f'phase80-final-DX-{a:x}-memory'
        functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',executed=True))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    for a,n,label in [(0x66D86C+0x68,4,'Shot-native-trajectory-flag-vslot68'),(0x66D86C+0x8C,4,'Shot-native-speed-setter-vslot8C'),
      (0x66E3EC+0xE4,4,'World-CreatureSpell-exists'),(0x66E3EC+0xE8,4,'World-CreatureSpell-get'),(0x66E3EC+0x1AC,4,'World-Shot-exists'),(0x66E3EC+0x1B0,4,'World-Shot-get'),
      (0x65FB14,64,'Native-integer-sqrt-seeds'),(0x65FC94,514,'Native-angle-ratio-table')]:
        key=f'phase80-final-data-{a:x}';blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'.txt',label=label));q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase80-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8');(ROOT/'phase80-final-memory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(functions),'complete PE code spans;',len(blocks),'file-backed data ranges')
if __name__=='__main__':main()
