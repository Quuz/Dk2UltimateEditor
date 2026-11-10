"""Post-emulation code attestation and six canonical-boundary confirmations."""
from pathlib import Path
import json
import re
import struct
from collections import deque
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    md.detail = True
    dx = ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
    old = ROOT/'source-binaries/DKII-local13.exe'
    entries = [('DKII-DX.exe',dx,f'phase71-DX-{a:x}-assembly.txt',True) for a in [0x56D4F0,0x56D620,0x49DD60]]
    entries += [('DKII-local13.exe',old,f'phase71-old-{a:x}-assembly.txt',True) for a in [0x5637F0,0x563920,0x494E80]]
    entries += [('DKII-DX.exe',dx,'phase52-DKII-DX.exe-6361a0-disassemble_function.txt',True),
                ('DKII-local13.exe',old,'phase52-DKII-local13.exe-627cd0-disassemble_function.txt',True),
                ('DKII-DX.exe',dx,'game-catalogue-objects-parser-assembly.txt',False)]
    canonical = json.loads((ROOT/'phase71-anchored-boundary-preparation.json').read_text())
    for r in canonical:
        file = f'phase71-boundary-old-{r["function_hint"]:x}-assembly.txt'
        lines = (ROOT/'ghidra'/file).read_text().splitlines()
        matches = [line for line in lines if line.startswith(f'{r["canonical_guess"]:08x}:')]
        if not matches:
            assert r['function_hint']==0x4979B0 and r['canonical_guess']==0x497C61
            data = old.read_bytes()
            assert pe_read(data,0x497A15,3)==bytes.fromhex('83f804')
            switches = struct.unpack('<5I',pe_read(data,0x497E58,20))
            todo = deque([0x4979B0]);seen=set();nodes={}
            while todo:
                pc=todo.popleft()
                if pc in seen or not 0x4979B0<=pc<0x497E55:continue
                x=next(md.disasm(pe_read(data,pc,15),pc,count=1));seen.add(pc);nodes[pc]=x
                if x.mnemonic.startswith('ret'):continue
                if x.mnemonic.startswith('j'):
                    if pc==0x497A1E:todo.extend(switches)
                    else:
                        assert x.operands[0].type==X86_OP_IMM
                        todo.append(x.operands[0].imm)
                        if x.mnemonic!='jmp':todo.append(pc+x.size)
                else:todo.append(pc+x.size)
            assert r['canonical_guess'] in nodes
            x=nodes[r['canonical_guess']]
            assert x.bytes.hex()==r['canonical_bytes']
            matches=[f'{x.address:08x}: {x.mnemonic} {x.op_str}']
            recovered='phase71-old-4979b0-recovered-CFG.txt'
            (ROOT/'ghidra'/recovered).write_text('\n'.join(f'{a:08x}: {nodes[a].mnemonic} {nodes[a].op_str}' for a in sorted(nodes)),encoding='utf8')
            r['recovered_switch']=dict(jump=0x497A1E,table=0x497E58,count=5,targets=switches,instructions=len(nodes),assembly=recovered,
                bound='Native CMP EAX,4 / JA default guards five entries. Complete reachable instruction paths reconstructed from actual PE bytes;CALL bodies not inlined.')
        assert len(matches)==1,(hex(r['anchor']),hex(r['function_hint']))
        r['verified_canonical_instruction'] = matches[0]
        r['body_file'] = file
        entries.append(('DKII-local13.exe',old,file,False))
    rows,blocks,queries = [],[],[]
    for program,exe,before,executed in entries:
        aa = [int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        first = aa[0]
        last = next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]))
        if executed:
            assert last.mnemonic.startswith('ret'),(before,last.mnemonic)
        n = last.address+last.size-first
        key = f'phase71-final-{program}-{first:x}'
        rows.append(dict(program=program,exe=str(exe),address=first,length=n,before=before,after=key+'-assembly.txt',
                         memory=key+'-memory.txt',executed=executed,scope='Complete executed native function.' if executed else 'Static parser or canonical-boundary context;not newly executed.'))
        queries.extend([
            dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(first)),print_limit=5),
            dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(first),length=n),print_limit=5),
        ])
    for program,exe,a in [('DKII-DX.exe',dx,0x68ED34),('DKII-local13.exe',old,0x67FCCC)]:
        key = f'phase71-final-{program}-sound-prefix'
        blocks.append(dict(program=program,exe=str(exe),address=a,length=8,memory=key+'-memory.txt',label='sound-prefix'))
        queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=8),print_limit=5))
    blocks.append(dict(program='DKII-local13.exe',exe=str(old),address=0x497E58,length=20,memory='phase71-final-old-boundary-switch-memory.txt',label='five-target-kind-switch'))
    queries.append(dict(id='phase71-final-old-boundary-switch-memory',tool='read_memory',args=dict(program='DKII-local13.exe',address='0x497e58',length=20),print_limit=5))
    (ROOT/'phase71-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase71-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    (ROOT/'phase71-canonical-boundary-review.json').write_text(json.dumps(canonical,indent=2),encoding='utf8')
    print(len(rows),'code extents;',sum(r['executed'] for r in rows),'executed native functions;',len(blocks),'data ranges;6 anchors rejected')


if __name__=='__main__':
    main()
