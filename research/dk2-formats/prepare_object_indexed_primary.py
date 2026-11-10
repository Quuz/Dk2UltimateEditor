"""Attest old consumer code extents, recovered switch data and pointer setup."""
from pathlib import Path
import json
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    exe = ROOT/'source-binaries/DKII-local13.exe'
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    review = json.loads((ROOT/'phase70-indexed-origin-review.json').read_text())
    rows,blocks,queries = [],[],[]
    for r in review['functions']:
        a = r['function']
        addresses = [int(line[:8],16) for line in (ROOT/'ghidra'/r['assembly']).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        last = next(md.disasm(pe_read(data,addresses[-1],15),addresses[-1]))
        assert addresses[0] == a
        if not last.mnemonic.startswith('ret'):
            assert a == 0x4EFB70 and last.mnemonic == 'jmp' and last.op_str == '0x4efc46',(hex(a),last.mnemonic,last.op_str)
        n = last.address+last.size-a
        key = f'phase70-final-old-{a:x}'
        rows.append(dict(program='DKII-local13.exe',exe=str(exe),address=a,length=n,before=r['assembly'],
                         after=key+'-assembly.txt',memory=key+'-memory.txt',executed=False,
                         scope='Native function extent. Computed-switch blocks separately reconstructed and covered by these PE/live bytes.' if r['switches'] else 'Complete statically inspected native function body.',
                         recovered_instruction_count=r['recovered_instruction_count']))
        queries.extend([
            dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-local13.exe',address=hex(a)),print_limit=5),
            dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-local13.exe',address=hex(a),length=n),print_limit=5),
        ])
    for label,a,n,scope in [
        ('two-switch-tables',0x4F0A08,32,'Two four-target jump tables;not executable code.'),
        ('table-initializer-fragment',0x4FE54F,80,'Code fragment only;sets defaults100 and installs pointer6CF048=6DD5D8. Full constructor not executed.'),
        ('table-reset-fragment',0x4FEDA8,88,'Code fragment only;EDI is cleared before pointer6CF048=EDI. Calls obey callee-saved ABI. Full destructor not executed.'),
    ]:
        key = 'phase70-final-old-'+label
        blocks.append(dict(program='DKII-local13.exe',exe=str(exe),address=a,length=n,memory=key+'-memory.txt',label=label,scope=scope))
        queries.append(dict(id=key+'-memory',tool='read_memory',args=dict(program='DKII-local13.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase70-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase70-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print(len(rows),'native code extents;',len(blocks),'explicit code/data ranges;no native executions')


if __name__=='__main__':
    main()
