"""DX room-owned species counters; hardcoded EXE mapping, not raw KWD fields."""
GROUPS={
 0x108a:[5],0x108b:[34],0x108c:[20],0x108d:[45],0x108e:[8],0x108f:[42],
 0x1090:[23],0x1091:[44],0x1092:[13,37],0x1093:[2,21,26,29,31],0x1094:[16],
 0x1095:[19],0x1096:[3,15],0x1097:[41],0x1098:[18,24],0x1099:[40],
 0x109a:[17,25,30],0x109b:[4,6,7,9,10,11,12,22,32,33,35,36,38,39,43]}
SLOTS={ident:slot for slot,ids in GROUPS.items() for ident in ids}

def counter_slot(creature_id):
    if not isinstance(creature_id,int) or not 0<=creature_id<=255:raise ValueError('Creature ID must fit a BYTE')
    return SLOTS.get(creature_id)

def change_counter(value,delta):
    if not isinstance(value,int) or not 0<=value<=255:raise ValueError('Stored count must fit a BYTE')
    if not isinstance(delta,int):raise ValueError('Delta must be integral')
    result=(value+delta)&255
    return 0 if result&128 else result
