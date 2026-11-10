"""Inspect indexed candidate context without assigning an Objects type."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM
from compare_rc3_converters import code_region
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    rows = json.loads((ROOT/'phase70-old-object-field-candidates-v2.json').read_text())['novel_address_candidates']
    wanted = {int(r['address'],16) for r in rows if r['bytes']==2 and r['index'] and r['base'] not in ['esp','ebp']}
    data = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    base,code = code_region(data)
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    md.detail = True
    md.skipdata = True
    history,contexts,targets = [],[],set()
    for x in md.disasm(code,base):
        if not x.id:
            continue
        if x.mnemonic=='call' and x.operands[0].type==X86_OP_IMM:
            targets.add(x.operands[0].imm)
        if x.address in wanted:
            context = history[-14:]+[f'{x.address:08x}: {x.mnemonic} {x.op_str}']
            contexts.append(dict(read=x.address,lines=context))
            print('\n'.join(context))
        history.append(f'{x.address:08x}: {x.mnemonic} {x.op_str}')
    queries = []
    for r in contexts:
        left = max(t for t in targets if t<=r['read'])
        r['function_hint'] = left
        # The nearest direct-call target is only a hint. Complete Ghidra bodies
        # showed that these two reads belong to following indirect-entry methods.
        corrected = {0x4710BA:0x470E50,0x49FA90:0x49F510}.get(r['read'])
        if corrected is not None:
            r['superseded_function_hint'] = left
            r['function_hint'] = corrected
        queries.append(dict(id=f'phase70-old-{r["read"]:x}-owner',tool='get_function_by_address',args=dict(program='DKII-local13.exe',address=hex(r['read'])),print_limit=80))
    (ROOT/'phase70-old-indexed-contexts.json').write_text(json.dumps(contexts,indent=2),encoding='utf8')
    (ROOT/'phase70-indexed-owner-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')


if __name__=='__main__':
    main()
