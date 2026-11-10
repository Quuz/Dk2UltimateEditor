"""New read-policy/indexed/anchored pass over old ObjectWORD106 candidates.

All results are untyped instruction leads. Anchored decodes can start inside
another instruction and must be verified against real function boundaries.
The historical phase61 output is retained; this pass does not overwrite it.
"""
from pathlib import Path
import json
import hashlib
from collections import Counter
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
from native_memory_access import reads_memory

ROOT = Path(__file__).resolve().parent


def main():
    exe = ROOT/'source-binaries/DKII-local13.exe'
    data = exe.read_bytes()
    base,code = code_region(data)
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    md.detail = True
    md.skipdata = True
    rows = {}
    count = 0
    def add(x,o,source):
        key = (x.address,o.mem.disp,o.size,x.reg_name(o.mem.base),x.reg_name(o.mem.index))
        row = rows.setdefault(key,dict(address=hex(x.address),hex=x.bytes.hex(),text=x.mnemonic+' '+x.op_str,
            displacement=o.mem.disp,bytes=o.size,base=x.reg_name(o.mem.base),index=x.reg_name(o.mem.index),
            legacy_read_bit=bool(o.access&CS_AC_READ),sources=[]))
        if source not in row['sources']:
            row['sources'].append(source)
    for x in md.disasm(code,base):
        if not x.id:
            continue
        count += 1
        for o in x.operands:
            if reads_memory(x,o) and o.mem.base and o.mem.disp < 0x108 and o.mem.disp+o.size > 0x106:
                add(x,o,'linear-fixed-read-policy')
    for disp in range(0xFF,0x108):
        pattern = disp.to_bytes(4,'little')
        at = code.find(pattern)
        while at >= 0:
            for backwards in range(2,13):
                start = at-backwards
                if start < 0:
                    continue
                x = next(md.disasm(code[start:start+15],base+start,count=1),None)
                if x is None or not x.id:
                    continue
                for o in x.operands:
                    if reads_memory(x,o) and o.mem.base and x.disp_size==4 and start+x.disp_offset==at and o.mem.disp < 0x108 and o.mem.disp+o.size > 0x106:
                        add(x,o,'displacement-anchored')
            at = code.find(pattern,at+1)
    result = sorted(rows.values(),key=lambda r:(int(r['address'],16),r['bytes']))
    old = json.loads((ROOT/'phase61-old-structure-field-leads.json').read_text())
    previous = {r['address'] for r in old['candidates'] if 'ObjectUnknownRuntime' in r['targets']}
    novel = [r for r in result if r['address'] not in previous]
    out = dict(scope=__doc__,exe=str(exe),sha256=hashlib.sha256(data).hexdigest(),
               decoded_instructions=count,field=dict(offset=0x106,bytes=2),candidates=result,novel_address_candidates=novel,
               policy=dict(file='memory-read-policy.json',sha256=hashlib.sha256((ROOT/'memory-read-policy.json').read_bytes()).hexdigest()),
               baseline='phase61-old-structure-field-leads.json',warning='No pointer identity or global absence proof;stack/index/prefix false leads included intentionally.')
    (ROOT/'phase70-old-object-field-candidates-v2.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    print('decoded',count,'candidates',len(result),'new addresses',len(novel),'widths',dict(Counter(r['bytes'] for r in novel)))
    for r in novel:
        if r['base'] not in ['esp','ebp']:
            print(r['address'],r['text'],r['sources'],'legacyREAD',r['legacy_read_bit'])


if __name__=='__main__':
    main()
