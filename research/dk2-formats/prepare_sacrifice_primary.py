"""Attest full native functions and original dispatch tables after emulation."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXE=Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe')
FUNCTIONS={0x51db30:'phase88-sacrifice-DX-51db30-assembly.txt',0x50f7e0:'readiness-DX-50f7e0-assembly.txt',
  0x4f1600:'phase88-sacrifice-DX-4f1600-assembly.txt',0x4f1660:'phase88-sacrifice-DX-4f1660-assembly.txt',
  0x4f1780:'phase88-sacrifice-DX-4f1780-assembly.txt',0x5133a0:'phase88-sacrifice-DX-5133a0-assembly.txt',
  0x49f600:'phase88-sacrifice-DX-49f600-assembly.txt',0x472930:'phase88-sacrifice-DX-472930-assembly.txt',
  **{a:f'phase88-sacrifice-context-{a:x}-assembly.txt' for a in [0x4e4d90,0x4e52f0,0x4559b0,0x508fe0]},
  **{a:f'phase88-sacrifice-executor-{a:x}-assembly.txt' for a in [0x454110,0x5558d0,0x4c19c0,0x4c4600]}}
def main():
    tasks=[];rows=[];md=Cs(CS_ARCH_X86,CS_MODE_32);data=EXE.read_bytes()
    for a,file in FUNCTIONS.items():
        if not (ROOT/'ghidra'/file).exists():
            for tool,suffix in [('create_function','create'),('disassemble_function','assembly'),('force_decompile','decompile')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                tasks.append(dict(id=f'phase88-sacrifice-DX-{a:x}-{suffix}',tool=tool,args=args,print_limit=5))
            continue
        lines=(ROOT/'ghidra'/file).read_text().splitlines();aa=[int(s[:8],16) for s in lines if re.match(r'^[0-9a-fA-F]{8}:',s)];assert aa and aa[0]==a
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a
        name=f'phase88-sacrifice-primary-{a:x}'
        rows.append(dict(exe=str(EXE),program='DKII-DX.exe',address=a,length=n,assembly=file,memory=name+'.txt',
          executed=a not in (0x49f600,0x472930),scope='Whole function bytes; not full instruction/branch coverage'))
        if not (ROOT/'ghidra'/(name+'.txt')).exists():
            tasks.append(dict(id=name,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    if len(rows)==len(FUNCTIONS):
        blocks=[]
        for a,n,label in [(0x66e3ec,0x400,'World-vtable'),(0x4f1928,16,'reward-switch')]:
            name=f'phase88-sacrifice-primary-{label}'
            blocks.append(dict(exe=str(EXE),program='DKII-DX.exe',address=a,length=n,memory=name+'.txt'))
            tasks.append(dict(id=name,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
        tasks.append(dict(id='phase88-sacrifice-save',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
        (ROOT/'phase88-sacrifice-primary-manifest.json').write_text(json.dumps(dict(functions=rows,data=blocks),indent=2),encoding='utf8')
    filename='phase88-sacrifice-primary-queries.json'
    (ROOT/filename).write_text(json.dumps(tasks,indent=2),encoding='utf8');print(len(rows),'complete functions;',len(tasks),'queries')
if __name__=='__main__':main()
