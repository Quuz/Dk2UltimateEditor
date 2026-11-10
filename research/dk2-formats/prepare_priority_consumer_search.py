"""Search a known Object pickup field as a control for Creature priority leads."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
source=(ROOT/'scan_current_door_word236_arguments.py').read_text(encoding='utf8')
first=source.index('from pathlib import Path')
source=__doc__.__repr__()+'\n'+source[first:]
source=source.replace("TARGETS={'Doors':[(0x99,2,'unknownWord236'),(0x97,2,'controlHeight'),(0x91,2,'controlHealth'),(0x95,2,'controlManufacture')]}",
    "TARGETS={'Objects':[(0x112,1,'ObjectPickUpPriority'),(0x106,2,'unknown34C'),(0x104,2,'controlHealth')]}" )
source=source.replace("getters={0x50D650:'Doors'};slots={0x138:'Doors'}","getters={};slots={0x188:'Objects'}")
source=source.replace("op[1].mem.disp==0xF7", "op[1].mem.disp==0x12D")
source=source.replace("family='Doors'", "family='Objects'")
source=source.replace('phase67-DX-door-word236-leads.json','phase73-object-priority-pointer-leads.json')
assert "'Doors'" not in source and '0xF7' not in source
(ROOT/'scan_object_priority_arguments.py').write_text(source,encoding='utf8')
print('PreparedObjectpriority/unknown34C/controlHealthpointersearch')
