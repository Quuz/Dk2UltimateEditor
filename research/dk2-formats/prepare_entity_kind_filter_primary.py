"""Post-emulation full-body capture, including filtered and untyped callers."""
from pathlib import Path
import json
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    dx = ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
    old = ROOT/'source-binaries/DKII-local13.exe'
    entries = [('DKII-DX.exe',dx,a,f'phase69-DX-{a:x}-assembly.txt',a==0x453700)
               for a in [0x453700,0x4A0930,0x458F60,0x4A7D90,0x4A7E70,0x513730]]
    entries += [('DKII-DX.exe',dx,a,f'phase68-DKII-DX.exe-{a:x}-assembly.txt',False) for a in [0x4A8E70,0x4A80E0]]
    entries += [('DKII-local13.exe',old,0x44C250,'phase69-old-44c250-assembly.txt',True)]
    rows,queries = [],[]
    for program,exe,address,before,executed in entries:
        aa = [int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        assert aa[0] == address
        last = next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]))
        assert last.mnemonic.startswith('ret'),(before,last.mnemonic)
        n = aa[-1]+last.size-address
        key = f'phase69-final-{program}-{address:x}'
        rows.append(dict(program=program,exe=str(exe),address=address,length=n,before=before,
                         after=key+'-assembly.txt',memory=key+'-memory.txt',executed=executed))
        queries.extend([
            dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(address)),print_limit=5),
            dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(address),length=n),print_limit=5),
        ])
    block = dict(program='DKII-DX.exe',exe=str(dx),address=0x4A0E14,length=20,
                 memory='phase69-final-DX-kind-switch-memory.txt',label='kind-switch-five-targets')
    queries.append(dict(id='phase69-final-DX-kind-switch-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address='0x4a0e14',length=20),print_limit=5))
    (ROOT/'phase69-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=[block]),indent=2),encoding='utf8')
    (ROOT/'phase69-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print('9 full functions;2 executed;1 kind-dispatch data range')


if __name__=='__main__':
    main()
