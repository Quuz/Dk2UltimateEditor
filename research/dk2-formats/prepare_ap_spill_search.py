"""New AP lookup-rooted pass retaining local aliases across known ABI calls.

Only real DX PRNG5558D0,APgetter50FAE0,mapgetter508FE0 and tentative matching
CWorld virtual slots have contracts. Passed argument cells and dead lower
stack are invalidated. Other calls still erase stack/frame knowledge.
Virtual receiver identity and arbitrary pointee escapes are not proved by
this candidate engine. Positive control is APFlag40 read47205E after spills.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
source=(ROOT/'scan_ap_pointer_arguments.py').read_text(encoding='utf8')
source=repr(__doc__)+'\n'+source[source.index('from pathlib import Path'):]
start=source.index("  if x.mnemonic=='mov' and len(op)==2")
end=source.index("   if op[0].type==X86_OP_IMM and op[0].imm in getters",start)
source=source[:start]+"  if x.mnemonic=='call':\n"+source[end:]
old="""    for r in ['eax','ecx','edx']:aliases.pop(r,None)
    stack={};sp=0;frames={}"""
new="""    contract=None
    if op[0].type==X86_OP_IMM:contract={0x5558D0:(16,0),0x50FAE0:(4,4),0x508FE0:(4,4)}.get(op[0].imm)
    elif op[0].type==X86_OP_MEM:contract={0x1D4:(4,4),0x2C8:(4,4)}.get(op[0].mem.disp)
    for r in ['eax','ecx','edx']:aliases.pop(r,None);frames.pop(r,None)
    if contract is None:stack={};sp=0;frames={}
    else:
     argument_bytes,pop_bytes=contract
     stack={k:v for k,v in stack.items() if k>=sp+argument_bytes}
     sp+=pop_bytes"""
assert source.count(old)==1
source=source.replace(old,new)
source=source.replace('phase64-ap-pointer-leads-v2.json','phase76-ap-spill-leads.json')
source=source.replace("if not h['field'].startswith('control'):print", "if h['field']=='possibleAPFlags':print")
assert 'global-tag-table-untyped' not in source
(ROOT/'scan_ap_stack_spills.py').write_text(source,encoding='utf8')
print('Prepared new AP-only spill retention search with explicit known-call contracts')
