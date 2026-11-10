"""Verify entry boundaries, capture full contexts and locate table-pointer writes."""
from pathlib import Path
import json
from collections import deque
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    exe = ROOT/'source-binaries/DKII-local13.exe'
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    md.detail = True
    md.skipdata = True
    base,code = code_region(data)
    history = deque(maxlen=18)
    writes = []
    for x in md.disasm(code,base):
        if not x.id:
            continue
        if x.mnemonic=='mov' and x.operands[0].type==X86_OP_MEM and not x.operands[0].mem.base and not x.operands[0].mem.index and x.operands[0].mem.disp==0x6CF048:
            writes.append(dict(address=x.address,context=list(history)+[f'{x.address:08x}: {x.mnemonic} {x.op_str}']))
        history.append(f'{x.address:08x}: {x.mnemonic} {x.op_str}')
    (ROOT/'phase70-old-growth-pointer-writes.json').write_text(json.dumps(writes,indent=2),encoding='utf8')
    for r in writes:
        print('Global pointer write:','\n'.join(r['context']))
    contexts = json.loads((ROOT/'phase70-old-indexed-contexts.json').read_text())
    entries = sorted({r['function_hint'] for r in contexts})
    queries,boundaries = [],[]
    for address in entries:
        previous = pe_read(data,address-16,16)
        first = pe_read(data,address,16)
        padded = previous[-1] == 0x90
        immediate_ret = previous[-1] == 0xC3 or (previous[-3] == 0xC2 and int.from_bytes(previous[-2:],'little') <= 1024)
        assert padded or immediate_ret,(hex(address),previous.hex())
        boundaries.append(dict(address=address,previous16=previous.hex(),first16=first.hex(),origin='Direct-call target in PE linear pass,16-byte aligned,preceded by NOP padding or an immediately ending RET. Actual read must also occur at the expected boundary in complete Ghidra body.'))
        key = f'phase70-old-{address:x}'
        queries.extend([
            dict(id=key+'-create',tool='create_function',args=dict(program='DKII-local13.exe',address=hex(address),disassemble_first=True),print_limit=10),
            dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-local13.exe',address=hex(address)),print_limit=10),
        ])
    (ROOT/'phase70-indexed-boundaries.json').write_text(json.dumps(boundaries,indent=2),encoding='utf8')
    (ROOT/'phase70-indexed-context-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print(len(entries),'candidate complete function entries')


if __name__=='__main__':
    main()
