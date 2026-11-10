"""Locate canonical linear instructions containing six remaining byte anchors.

This is preparation only. Full native function/body confirmation is required
before using a canonical-instruction guess as evidence to reject a lead.
"""
from pathlib import Path
from collections import deque
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from compare_rc3_converters import code_region

ROOT = Path(__file__).resolve().parent


def main():
    s = json.loads((ROOT/'object-indexed-consumer-review.json').read_text())
    wanted = {int(r['address'],16) for r in s['remaining_anchored_candidates']}
    data = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    base,code = code_region(data)
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    md.skipdata = True
    history,targets,rows = deque(maxlen=10),set([base]),[]
    for x in md.disasm(code,base):
        if not x.id:
            continue
        if x.mnemonic=='call' and x.op_str.startswith('0x'):
            targets.add(int(x.op_str,16))
        for a in wanted:
            if x.address<=a<x.address+x.size:
                rows.append(dict(anchor=a,canonical_guess=x.address,canonical_bytes=x.bytes.hex(),canonical_text=x.mnemonic+' '+x.op_str,
                                 context=list(history)))
        history.append(f'{x.address:08x}: {x.mnemonic} {x.op_str}')
    assert len(rows)==6
    for r in rows:
        r['function_hint'] = max(t for t in targets if t<=r['canonical_guess'])
        print(hex(r['anchor']),'inside',hex(r['canonical_guess']),r['canonical_text'],'hint',hex(r['function_hint']))
    (ROOT/'phase71-anchored-boundary-preparation.json').write_text(json.dumps(rows,indent=2),encoding='utf8')


if __name__=='__main__':
    main()
