"""Narrow numeric projection for selected496E00 nonpossessed,nonlethal work path.

Inputaction is selected rawVariables8/9/10value bits. Original51CB80 signedTPS
division, nativeQ12sum/multiply andSAR12; no everyjob/Worldscheduler claim.
"""
def u32(n):return n&0xFFFFFFFF
def s32(n):n=u32(n);return n-(1<<32) if n&0x80000000 else n
def delta(action_value_bits,tps,modifier_bits,enhancement_bits,*,non_imp=False):
    if any(type(x) is not int or not 0<=x<=0xFFFFFFFF for x in [action_value_bits,modifier_bits,enhancement_bits]) or type(tps) is not int or not 1<=tps<=0x7FFFFFFF or type(non_imp) is not bool:
        raise ValueError('Expected u32bitpatterns, positive signedTPS and non_imp bool')
    n=s32(action_value_bits);action=(abs(n)//tps)*(-1 if n<0 else 1)
    base=s32(action*(6 if non_imp else 1)<<12);factor=s32(modifier_bits+enhancement_bits)
    product=s32((base*factor)>>12)
    return dict(action_per_tick=action,combined_factor_bits=u32(factor),Q12_product_bits=u32(product),signed_HP_delta=product>>12)

def patch_action_value(raw,value_bits):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=16 or int.from_bytes(raw[:4],'little') not in [8,9,10]:raise ValueError('Expected full16-byte scalarVariables8/9/10 record')
    if type(value_bits) is not int or not 0<=value_bits<=0xFFFFFFFF:raise ValueError('Expected u32value bits')
    return bytes(raw[:4])+value_bits.to_bytes(4,'little')+bytes(raw[8:])
