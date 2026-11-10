"""Search Terrain pointers produced by the separate global definition cache.

Cache roots are numeric leads until its writer is verified to populate these
entries using native CWorld Terrain lookup. Not a global non-use proof.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
for label,table in [('DX',0x6DA4A8),('old',0x6CD1A0)]:
    source=(ROOT/f'scan_{label.lower()}_terrain_arguments.py').read_text(encoding='utf8')
    a=source.index("  if x.mnemonic=='call':")
    b=source.index("  if family:seeds.append",a)
    source=source[:a]+f'''  if x.mnemonic=='mov' and len(op)==2 and op[0].type==X86_OP_REG and op[0].size==4 and op[1].type==X86_OP_MEM and not op[1].mem.base and op[1].mem.disp=={table}:
   family='Terrain';dest=x.reg_name(op[0].reg);how='global-cache-return'
'''+source[b:]
    source=source.replace(f'phase74-{label}-terrain-argument-leads.json',f'phase74-{label}-terrain-cache-leads.json')
    source=source.replace("(0x181,2,'controlRaw196')", "(0x181,2,'controlRaw196'),(0x185,2,'controlRaw19A'),(0x1AF,6,'controlTerrainColour'),(0x1B9,6,'controlAmbientColour')")
    (ROOT/f'scan_{label.lower()}_terrain_cache.py').write_text(source,encoding='utf8')
print('Generated DX/old global Terrain-cache return searches')
