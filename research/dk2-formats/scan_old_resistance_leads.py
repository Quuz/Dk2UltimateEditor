"""Fresh bounded direct-operand review in the supplied old game PE, not DX."""
from pathlib import Path
import hashlib, json
import capstone, pefile
from capstone.x86 import X86_OP_MEM, X86_OP_IMM

ROOT = Path(__file__).resolve().parent
EXE = ROOT / 'source-binaries/DKII-local13.exe'


def main():
    blob = EXE.read_bytes()
    assert hashlib.sha256(blob).hexdigest() == '93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342'
    pe = pefile.PE(data=blob)
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    md.detail = True
    md.skipdata = True
    rows = []
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        for ins in md.disasm(section.get_data(), pe.OPTIONAL_HEADER.ImageBase + section.VirtualAddress):
            if not ins.id:
                continue
            match = False
            for operand in ins.operands:
                match |= operand.type == X86_OP_MEM and (
                    operand.mem.disp < 0x624 and operand.mem.disp + max(1, operand.size) > 0x61C
                    or operand.mem.disp == 0x78A)
                match |= operand.type == X86_OP_IMM and operand.imm in (0x61C, 0x61D, 0x78A)
            if match:
                rows.append(dict(address=ins.address, bytes=bytes(ins.bytes).hex(), mnemonic=ins.mnemonic, operands=ins.op_str))
    result = dict(executable=str(EXE), sha256=hashlib.sha256(blob).hexdigest(), literal_leads=rows,
        scope='Only direct displacement overlaps61C..623/count78A and exact immediates61C/61D/78A; receiver identity requires canonical review.')
    (ROOT / 'phase126-old-resistance-literal-leads.json').write_text(json.dumps(result, indent=2), encoding='utf8')
    queries = [dict(id=f'phase126-old-resist-owner-{row["address"]:06x}', tool='get_function_by_address',
        args=dict(program='DKII-local13.exe', address=hex(row['address'])), print_limit=0) for row in rows]
    (ROOT / 'phase126-old-resistance-owner-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print('Old PE direct resistance leads:', len(rows))
    for row in rows:
        print(hex(row['address']), row['mnemonic'], row['operands'])


if __name__ == '__main__':
    main()
