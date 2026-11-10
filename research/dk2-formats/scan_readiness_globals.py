"""Direct operand inventory; zero hits is not proof of no indirect consumer."""
from pathlib import Path
import json, hashlib
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region

ROOT = Path(__file__).resolve().parent
data = Path('D:/Games/DK2/Dungeon Keeper 2/DKII-DX.exe').read_bytes()
base, code = code_region(data)
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True
rows = []
relative_rows = []
contexts = []
recent = []
interesting = [0x4598b0,0x459cb2,0x472f31,0x48124f,0x47bb4e]
for ins in md.disasm(code, base):
    if not ins.id: continue
    row = dict(address=hex(ins.address), instruction=ins.mnemonic+' '+ins.op_str)
    recent.append(row)
    if len(recent)>100: recent.pop(0)
    for context in contexts:
        if 0 < ins.address-context['anchor'] < 100:
            context['after'].append(row)
    if ins.address in interesting:
        contexts.append(dict(anchor=ins.address, before=list(recent), after=[]))
    for op in ins.operands:
        if op.type == X86_OP_MEM and op.mem.base and not op.mem.index and op.mem.disp in (0x3a2,0x3a4):
            relative_rows.append(dict(row, bytes=ins.bytes.hex()))
        if op.type == X86_OP_MEM and not op.mem.base and not op.mem.index:
            if any(op.mem.disp <= a < op.mem.disp+op.size for a in (0x6f28f2,0x6f28f4)):
                rows.append(dict(address=hex(ins.address), bytes=ins.bytes.hex(),
                                 instruction=ins.mnemonic+' '+ins.op_str))
report = dict(exe_sha256=hashlib.sha256(data).hexdigest(), direct_absolute_operands=rows,
              candidate_relative_operands=relative_rows,
              limitation='Direct operands only. Pointer-relative or indexed reads are not excluded.')
(ROOT/'readiness-possession-direct-operands.json').write_text(json.dumps(report, indent=2), encoding='utf8')
(ROOT/'readiness-caller-contexts.json').write_text(json.dumps(contexts, indent=2), encoding='utf8')
print(json.dumps(report, indent=2))
for context in contexts:
    if context['anchor'] != 0x459cb2: continue
    print('\nContext',hex(context['anchor']))
    for row in context['before']+context['after']:
        print(row['address'], row['instruction'])
