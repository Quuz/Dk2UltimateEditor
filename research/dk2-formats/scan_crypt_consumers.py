"""Bounded linear-operand leads for all18 Crypt owner counters, not proof of use."""
from pathlib import Path
import hashlib
import json
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
from build_effect_semantics import EXE

ROOT = Path(__file__).resolve().parent


def main():
    data = EXE.read_bytes()
    base, code = code_region(data)
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail, md.skipdata = True, True
    rows, recent, pending = [], [], []
    for ins in md.disasm(code, base):
        if not ins.id:
            continue
        row = dict(address=ins.address, instruction=ins.mnemonic+' '+ins.op_str, bytes=ins.bytes.hex())
        pending = [r for r in pending if ins.address-r['address'] < 60]
        for r in pending:
            r['after'].append(row)
        for op in ins.operands:
            if op.type == X86_OP_MEM and op.mem.base and 0x108A <= op.mem.disp <= 0x109B:
                lead = dict(row, displacement=op.mem.disp, width=op.size, before=list(recent), after=[])
                rows.append(lead)
                pending.append(lead)
        recent = (recent+[row])[-8:]
    out = dict(executable=str(EXE), sha256=hashlib.sha256(data).hexdigest(), leads=rows,
               scope='Linear-disassembly displacement leads only. Receiver owner/dataflow and instruction boundaries '
                     'must be checked in whole original functions. Absence is not global nonuse.')
    (ROOT/'phase106-crypt-consumer-leads.json').write_text(json.dumps(out, indent=2), encoding='utf8')
    for offset in range(0x108A, 0x109C):
        print(hex(offset), [(hex(r['address']), r['instruction']) for r in rows
                            if r['displacement'] == offset and not 0x4D8E60 <= r['address'] < 0x4D9271])


if __name__ == '__main__':
    main()
