"""CreatureSpells rawEE eligibility; no alternate Shot/Room replacement rule.

Arguments describe a resolved spell and the creature on which it is checked.
No missing-definition safety is implied. Native state lookup has no bounds
check; this diagnostic helper rejects indices outside its supplied table.
"""
from native_spell_writer import patch_raw

def unsigned(value,bits):
    if type(value) is not int or not 0<=value<1<<bits:raise ValueError(f'Expected u{bits}')
    return value

def signed32(value):return (value&0x7FFFFFFF)-(value&0x80000000)

def effective_category(categories,current,backup):
    if not isinstance(categories,(bytes,bytearray)):raise ValueError('Expected category bytes')
    unsigned(current,32);unsigned(backup,32)
    if current>=len(categories):raise ValueError('Current state outside diagnostic table')
    category=categories[current]
    if category==2:
        if backup>=len(categories):raise ValueError('Backup state outside diagnostic table')
        category=categories[backup]
    return category

def eligible(rule,*,hp=49,base_hp=100,level=1,coefficient=100,timer=0,category=22):
    unsigned(rule,8);unsigned(hp,32);unsigned(base_hp,16);unsigned(level,8);unsigned(timer,16);unsigned(category,8)
    if type(coefficient) is not int or not -32768<=coefficient<=32767:raise ValueError('Expected s16 coefficient')
    if rule==1:return int(timer==0)
    if rule==2:
        if level==1:threshold=base_hp>>1
        else:
            product=coefficient*base_hp
            scaled=abs(product)//100*(-1 if product<0 else 1)
            threshold=(scaled&0xFFFFFFFF)>>1
        return int(signed32(hp)<threshold)
    if rule in (3,4,5):return int(category==22 and timer==0)
    return 0

def patch_predicate(raw,value):return patch_raw(raw,0xEE,value)
