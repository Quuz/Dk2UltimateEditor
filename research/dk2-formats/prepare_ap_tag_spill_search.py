"""Apply the new call-lifetime rules to tag-table leads omitted by lookup-only pass.

Numeric global tag entries remain untyped. New candidates require AP identity
and native instruction-boundary verification;not a globalabsence proof.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
source=(ROOT/'scan_ap_stack_spills.py').read_text(encoding='utf8')
old=(ROOT/'scan_ap_pointer_arguments.py').read_text(encoding='utf8')
start=old.index("  if x.mnemonic=='mov' and len(op)==2")
end=old.index("  if family:seeds.append",start)
body=old[start:end]
start=source.index("  if x.mnemonic=='call':\n   if op[0].type==X86_OP_IMM and op[0].imm in getters")
end=source.index("  if family:seeds.append",start)
source=source[:start]+body+source[end:]
source=repr(__doc__)+'\n'+source[source.index('from pathlib import Path'):]
source=source.replace('phase76-ap-spill-leads.json','phase76-ap-tag-spill-leads.json')
(ROOT/'scan_ap_tag_spills.py').write_text(source,encoding='utf8')
print('Prepared new full tag-table AP candidate pass with known-call lifetime rules')
