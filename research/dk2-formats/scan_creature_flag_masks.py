"""Narrow new flag-mask leads from prior typed-pointer hints, not universal nonuse.

Only a short straight-line suffix after known686 reads. Masked register paths
and indirect dispatch need native verification; this pass does not name flags.
"""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read
ROOT=Path(__file__).resolve().parent
def main():
    prior=json.loads((ROOT/'phase85-creature-index-diagnostics.json').read_text(encoding='utf8'))
    rows={r['read']['address']:r for r in prior['candidates'] if r['field']=='controlCreatureFlags'}
    md=Cs(CS_ARCH_X86,CS_MODE_32);blob=EXE.read_bytes();out=[]
    for pc,row in rows.items():
        a=int(pc,16);instructions=list(md.disasm(pe_read(blob,a,96),a))[:20]
        context=[]
        for ins in instructions:
            context.append(f'{ins.address:x}: {ins.mnemonic} {ins.op_str}')
            if ins.mnemonic=='call':break
        hits=[s for s in context if re.search(r'(?<![0-9a-f])0x(?:800|1000|20000000)(?![0-9a-f])',s)
            or re.search(r'\b(?:test|and) [abcd]h, (?:8|0x10)\b',s)
            or re.search(r'\bbt \w+, (?:0xb|0xc|0x1d)\b',s)]
        if hits:out.append(dict(read=pc,root=row['root'],read_instruction=row['read']['instruction'],hits=hits,context=context))
    value=dict(scope=__doc__,source='phase85-creature-index-diagnostics.json',unique_prior_flag_reads=len(rows),leads=out)
    (ROOT/'phase147-creature-flag-mask-leads.json').write_text(json.dumps(value,indent=2),encoding='utf8')
    for r in out:print(r['read'],r['root']['function_hint'],r['read_instruction'],r['hits'])
    print(len(out),'new mask neighborhoods; candidates only')
if __name__=='__main__':main()
