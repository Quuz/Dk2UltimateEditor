from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent;EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    old=json.loads((ROOT/'phase97-primary-manifest.json').read_text(encoding='utf8'));functions={r['address']:r for r in old['functions']};q=[]
    addresses=[0x4d27d0,0x4d10d0,0x4d16b0,0x4c8bd0,0x4d56e0,0x4d5ac0,0x4d5af0,0x4d3be0,0x4d1e10,0x4d1ec0,0x4d1e40,0x4d1f10,0x4d5060,0x4d1e70,0x4d4550,0x509090,0x4b35c0,0x48ebf0,0x4d0e60,0x4d0f60]
    # Rawflagstoragebindingusesoriginalconverter/loader;notnewexecutioncoverage.
    for a in [0x568e30,0x492320]:
        if a not in functions:addresses.append(a)
    for a in addresses:
        stem=f'phase98-navigation-{a:x}'
        if not (ROOT/'ghidra'/(stem+'-disassemble_function.txt')).exists():
            for tool in ['create_function','disassemble_function','force_decompile']:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=stem+'-'+tool,tool=tool,args=args,print_limit=5))
    if q:
        (ROOT/'phase98-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print('Needfunctions:',len(q));return
    md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a in addresses:
        file=f'phase98-navigation-{a:x}-disassemble_function.txt';aa=[int(x[:8],16) for x in (ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
        assert aa[0]==a;last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase98-primary-{a:x}'
        functions[a]=dict(exe=str(EXE),address=a,length=n,assembly=file,memory=key+'.txt',scope='Wholeoriginalspan;static/unexecutedbranchesincluded,notcoverageclaim')
        if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    blocks=list(old['blocks']);key='phase98-data-original-collision-pointer';blocks.append(dict(exe=str(EXE),address=0x6a11d0,length=4,label='original-collision-pointer',memory=key+'.txt'))
    if not (ROOT/'ghidra'/(key+'.txt')).exists():q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address='0x6a11d0',length=4),print_limit=5))
    (ROOT/'phase98-primary-manifest.json').write_text(json.dumps(dict(functions=[functions[a] for a in sorted(functions)],blocks=blocks),indent=2),encoding='utf8')
    q.append(dict(id='phase98-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase98-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'spans',len(blocks),'blocks;',len(q),'newqueries')

if __name__=='__main__':main()
