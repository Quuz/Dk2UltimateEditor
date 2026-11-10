"""Placed Object C2 native projections for the two inspected executables.

Raw-preserving patching is independent of original editor normalization.
Creation success, definition flags and initial instance fields are inputs;
this module does not simulate object creation or assert catalogue references exist.
"""
import struct
from native_placed_structures import checked,center_q12

SPELL_OBJECT_IDS={2:117,6:118,7:119,8:120,10:121,11:122,12:123,13:124,14:125,15:126,16:127,17:129,18:128}
TRAP_CRATE_OBJECT_IDS={5,*range(36,45),92,138}
DOOR_CRATE_OBJECT_IDS=set(range(52,58))

def game_created_id(object_id,spell_bits):
    checked(object_id,8);checked(spell_bits,32)
    if object_id==28:return None
    return SPELL_OBJECT_IDS.get(spell_bits,7) if object_id==7 else object_id

def gold_add(initial_bits,amount_bits):
    checked(initial_bits,32);checked(amount_bits,32)
    total=(initial_bits+amount_bits)&0xFFFFFFFF
    return 0 if total&0x80000000 else total

def game_payload(flags,actual_id,spell_bits,money_bits,initial_type=0,initial_value=0):
    for value in [flags,spell_bits,money_bits,initial_type]:checked(value,32)
    checked(actual_id,8);checked(initial_value,8)
    if flags&0x40:return dict(branch='gold',type_or_amount=gold_add(initial_type,money_bits),value=initial_value)
    if flags&8:return dict(branch='spellbook',type_or_amount=4,value=spell_bits&255)
    if flags&0x10:
        if actual_id in TRAP_CRATE_OBJECT_IDS:return dict(branch='trap_crate',type_or_amount=1,value=actual_id)
        if actual_id in DOOR_CRATE_OBJECT_IDS:return dict(branch='door_crate',type_or_amount=0,value=actual_id)
    return dict(branch='unchanged',type_or_amount=initial_type,value=initial_value)

def editor_getters(flags,model_spell_byte,model_money_bits):
    checked(flags,32);checked(model_spell_byte,8);checked(model_money_bits,32)
    return dict(spell_bits=model_spell_byte if not flags&0x40 and flags&8 else 0,
                money_bits=model_money_bits if flags&0x44 else 0)

def editor_load_projection(object_id,flags,spell_bits,money_bits,initial_spell=0,initial_money=0):
    checked(object_id,8)
    for v in [flags,spell_bits,money_bits,initial_money]:checked(v,32)
    checked(initial_spell,8)
    spell=spell_bits&255 if not flags&0x40 and flags&0x18 else initial_spell
    money=money_bits if flags&0x44 else initial_money
    # The original callback remaps using420610, a byte getter, after setters.
    selector=spell if not flags&0x40 and flags&8 else 0
    return dict(object_id=SPELL_OBJECT_IDS.get(selector,7) if object_id==7 else object_id,
                model_spell_byte=spell,model_money_bits=money)

def record_view(envelope):
    if not isinstance(envelope,(bytes,bytearray)) or len(envelope)!=32 or struct.unpack_from('<II',envelope)!=(194,24):raise ValueError('Expected Object C2 envelope and24-byte payload')
    x,y,z,spell,money,trigger,oid,owner=struct.unpack('<5IHBB',envelope[8:])
    return dict(tag=194,x_bits=x,y_bits=y,opaque_position_word=z,spell_bits=spell,money_bits=money,trigger_id=trigger,object_id=oid,player_id=owner,
                created_id=game_created_id(oid,spell),position_q12=[center_q12(x),center_q12(y),0])

def patch_raw(envelope,payload_offset,value):
    record_view(envelope);widths={0:4,4:4,8:4,12:4,16:4,20:2,22:1,23:1}
    if payload_offset not in widths:raise ValueError('Unsupported Object field')
    n=widths[payload_offset];checked(value,n*8);off=8+payload_offset
    return bytes(envelope[:off])+value.to_bytes(n,'little')+bytes(envelope[off+n:])
