"""Initial literal-displacement inventory only; not an alias/type/nonuse proof."""
from pathlib import Path
import json,hashlib,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_READ
from capstone.x86 import X86_OP_MEM
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
def main():
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    source=ROOT/'sources/DKII_EXE_v170.sgmap'
    fs={int(a,16):dict(length=int(n),hint=name) for a,name,n in re.findall(r'^global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',source.read_text(),re.M)}
    table=json.loads((ROOT/'effect-semantics.json').read_text(encoding='utf8'))['generation_dispatch']['entries'];rows=[]
    for e in table:
        a=int(e['callback'],16)
        if not a:continue
        span=fs[a];raw=pe_read(blob,a,span['length']);reads=[]
        for ins in md.disasm(raw,a):
            for op in ins.operands:
                if op.type!=X86_OP_MEM or not op.access&CS_AC_READ or not op.mem.base or op.mem.index or ins.mnemonic in ['lea','call','jmp']:continue
                if op.mem.disp<0x4F and op.mem.disp+op.size>0x4B:
                    reads.append(dict(address=hex(ins.address),instruction=ins.mnemonic+' '+ins.op_str,
                        base=ins.reg_name(op.mem.base),size=op.size,classification='stack offset, not direct definition field' if ins.reg_name(op.mem.base)=='esp' else 'untyped candidate'))
        rows.append(dict(mode=e['value'],entry=a,mapped_bytes=span['length'],mapped_code_sha256=hashlib.sha256(raw).hexdigest(),literal_reads=reads))
    assert len(rows)==18 and all(x['base']=='esp' for r in rows for x in r['literal_reads'])
    out=dict(phase=142,scope=__doc__,profile=dict(file=str(EXE),sha256=hashlib.sha256(blob).hexdigest()),
        span_hint_source=dict(file=str(source.relative_to(ROOT)),sha256=hashlib.sha256(source.read_bytes()).hexdigest()),rows=rows,
        limitations='Only literal4B..4E overlaps in mapped callback spans, not adjusted/indexed/indirect helper reads. Map is a span hint; no full native absence proof and no new Misc2/Misc3 meaning.')
    (ROOT/'phase142-effect-misc-initial-review.json').write_text(json.dumps(out,indent=2),encoding='utf8');print('18 mode entries; all literal overlaps are stack offsets; aliases remain open')
if __name__=='__main__':main()
