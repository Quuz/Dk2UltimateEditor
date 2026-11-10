"""Current name and bit-preserving edit for native Creature melee animation bit29."""
from pathlib import Path
import copy,json
ROOT=Path(__file__).resolve().parent
MASK=0x20000000
NAME='PLAY_MELEE_INSTANCE_ANIMATION'
def effective_enums():
    enums=copy.deepcopy(json.loads((ROOT/'enums.json').read_text(encoding='utf8')))
    assert enums['Creature.CreatureFlag'][str(MASK)]=='UNKNOWN_5'
    enums['Creature.CreatureFlag'][str(MASK)]=NAME
    return enums
def patch(record,enabled):
    if len(record) not in [5449,5537]:raise ValueError('Known Creature record variant required')
    if type(enabled)is not bool:raise ValueError('Boolean flag value required')
    flags=int.from_bytes(record[0xEFC:0xF00],'little');flags=(flags&~MASK)|(MASK if enabled else 0)
    return bytes(record[:0xEFC])+flags.to_bytes(4,'little')+bytes(record[0xF00:])
