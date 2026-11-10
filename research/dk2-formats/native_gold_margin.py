"""Observed DX Variables74 gold threshold; preserve physical scalar bytes."""
import struct
DX_SHA256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'


def view(raw):
    if len(raw)!=16 or int.from_bytes(raw[:4],'little')!=74:raise ValueError('Expected Variables74 record')
    t,v,a,b=struct.unpack('<4I',raw)
    return dict(type=t,value_bits=v,extra1=a,extra2=b,confirmed_alias='GOLD_CAPACITY_MARGIN',historical_alias='GAME_TICKS')


def patch_bits(raw,value):
    view(raw)
    if type(value) is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected physical DWORD bits')
    return raw[:4]+struct.pack('<I',value)+raw[8:]


def project(base_gold,level,coefficient,carried,margin,argument):
    if not 0<=base_gold<=65535 or not 0<=level<=255 or not -32768<=coefficient<=32767:raise ValueError('Invalid physical input')
    if not 0<=carried<=0xFFFFFFFF or not 0<=margin<=0xFFFFFFFF:raise ValueError('Invalid DWORD')
    if not argument:return dict(accepted=carried!=0,capacity_bits=None,threshold_bits=None,margin_read=False)
    product=base_gold*coefficient
    capacity=base_gold if level==1 else abs(product)//100*(-1 if product<0 else 1)
    threshold=(capacity-margin)&0xFFFFFFFF
    return dict(accepted=carried>=threshold,capacity_bits=capacity&0xFFFFFFFF,threshold_bits=threshold,margin_read=True)
