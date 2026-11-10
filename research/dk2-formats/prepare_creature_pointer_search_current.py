"""Expand current Creature pointer forwarding to the earlier3-level argument engine."""
from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
slots={slot:struct.unpack('<I',pe_read(data,0x66E3EC+slot,4))[0] for slot in [0xC0,0x1C8]}
source=(ROOT/'scan_creature_argument_read_fallback.py').read_text(encoding='utf8')
first=source.index('from pathlib import Path');source=__doc__.__repr__()+'\n'+source[first:]
source=source.replace("data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes()","data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()")
source=source.replace("getters={0x501DE0:'Creatures',0x502DE0:'Creatures'}", "getters={"+','.join(f"{a}:'Creatures'" for a in slots.values())+"}")
source=source.replace('op[1].mem.disp==0x36F','op[1].mem.disp==0x370')
assert 'phase64-old-creature-argument-leads-v2.json' in source
source=source.replace('phase64-old-creature-argument-leads-v2.json','phase73-DX-creature-argument-leads.json')
(ROOT/'scan_current_creature_arguments.py').write_text(source,encoding='utf8')
(ROOT/'phase73-creature-slot-seeds.json').write_text(json.dumps(dict(world_vtable=0x66E3EC,slots=slots,warning='ExactPEslotvalues;typeproofofeveryvirtualreturnstillrequired.'),indent=2),encoding='utf8')
print('Current Creature getter seeds',','.join(f'{slot:x}:{a:x}' for slot,a in slots.items()))
