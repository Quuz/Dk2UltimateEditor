"""Verified ordinary nonlethal native HP stage, not an entire combat simulation."""
import struct
def s32(value):
    value&=0xffffffff;return value if value<0x80000000 else value-0x100000000
def trunc_div(value,divisor):return abs(value)//divisor*(-1 if value<0 else 1)
def healing_ceiling(base_hp,level,coefficient_bits):
    if type(base_hp) is not int or not 0<=base_hp<=65535 or type(level) is not int or not 0<=level<=255 or type(coefficient_bits) is not int or not 0<=coefficient_bits<=0xffffffff:raise ValueError('Expected u16 HP,u8 level,u32 coefficient bits')
    p=coefficient_bits&65535;p=p if p<32768 else p-65536
    return base_hp if level==1 else trunc_div(p*base_hp,100)
def ordinary_stage(hp,delta,base_hp,level,coefficient_bits,possessed=False,word443=False,word447=False,owner1095=0,pit_word=None,period=0,tick=100):
    if any(type(v) is not int or not -(1<<31)<=v<(1<<31) for v in [hp,delta]):raise ValueError('Expected signed32 HP/delta')
    if any(type(v) is not bool for v in [possessed,word443,word447]) or type(owner1095) is not int or not 0<=owner1095<=255:raise ValueError('Expected flags and raw ownerBYTE')
    if any(type(v) is not int or not 0<=v<=0xffffffff for v in [period,tick]):raise ValueError('Expected u32 period/tick')
    cap=healing_ceiling(base_hp,level,coefficient_bits);early=delta==0
    if delta>0 and s32(hp+delta)>cap:delta=s32(cap-hp);early=delta==0
    scale=128
    if delta<0:
        if word447:scale=160
        if word443 or possessed:scale-=64
        owner=owner1095 if owner1095<128 else owner1095-256
        if owner>0:scale-=8*owner
        if pit_word is not None:
            if type(pit_word) is not int or not 0<=pit_word<=65535:raise ValueError('Expected loaded u16 pit factor')
            scale+=pit_word-128
        scale=max(0,scale)
    applied=trunc_div(s32(scale*delta),128);result=s32(hp+applied)
    if result<=0:raise ValueError('Lethal result requires native death branches outside this ordinary-stage projection')
    return dict(hp=result,cap=cap,early=early,event=int(possessed and delta<0),timestamp=(tick+((period*3&0xffffffff)>>2))&0xffffffff)
def patch_level_value(raw,value_bits):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=16 or struct.unpack_from('<I',raw)[0]!=65:raise ValueError('Expected complete16-byte type65 record')
    if type(value_bits) is not int or not 0<=value_bits<=0xffffffff:raise ValueError('Expected unsigned32 physical value')
    out=bytearray(raw);struct.pack_into('<I',out,8,value_bits);return bytes(out)
