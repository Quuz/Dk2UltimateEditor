"""Locate native resistance/Shot category dialogs from original resources and code."""
from pathlib import Path
import hashlib, json
import capstone, pefile
from extract_editor_dialogs import extract

ROOT = Path(__file__).resolve().parent
EXE = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'


def main():
    blob = EXE.read_bytes()
    assert hashlib.sha256(blob).hexdigest() == '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
    dialogs = extract(blob)
    selected = [d for d in dialogs if d['resource_path'][1] in (233, 259, 330)]
    assert len(selected) == 3
    targets = {233, 259, 330, 1484}
    for d in selected:
        for index, c in enumerate(d['controls']):
            if c['title'] == 'Attack Category:':
                after = d['controls'][index + 1]
                assert after['window_class'] == {'ordinal': 133}
                targets.add(after['id'])
    pe = pefile.PE(data=blob)
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    md.detail = True
    md.skipdata = True
    rows = []
    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue
        for ins in md.disasm(section.get_data(), pe.OPTIONAL_HEADER.ImageBase + section.VirtualAddress):
            if ins.id == 0:
                continue
            if any(op.type == capstone.x86.X86_OP_IMM and op.imm in targets for op in ins.operands):
                rows.append(dict(address=ins.address, mnemonic=ins.mnemonic, operands=ins.op_str,
                    bytes=bytes(ins.bytes).hex()))
    result = dict(executable=str(EXE), sha256=hashlib.sha256(blob).hexdigest(),
        dialogs=selected, searched_immediates=sorted(targets), literal_code_leads=rows,
        scope='Native resource and immediate leads only; receiver identity and field binding need canonical function review.')
    (ROOT / 'phase125-combat-editor-leads.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    queries = [dict(id=f'phase125-editor-owner-{r["address"]:06x}', tool='get_function_by_address',
        args=dict(program='DK2 Editor pro.exe', address=hex(r['address'])), print_limit=0) for r in rows]
    (ROOT / 'phase125-editor-owner-queries.json').write_text(json.dumps(queries, indent=2), encoding='utf8')
    print('Dialog/control immediate leads:', len(rows), 'targets:', sorted(targets))
    for row in rows:
        print(hex(row['address']), row['mnemonic'], row['operands'])


if __name__ == '__main__':
    main()
