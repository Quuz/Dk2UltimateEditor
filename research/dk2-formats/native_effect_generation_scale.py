"""Exact selected DX CUBE_GEN count rule; not a general effect simulation."""

def uint(v,n,maximum):
    if type(v) is not int or not 0<=v<=maximum:raise ValueError(n)

def s32(v):
    v&=0xFFFFFFFF
    return v-(1<<32) if v&0x80000000 else v

def scale_from_rate_dword(value):
    """43B28B..43B2C9 only. Input normally comes from frame timer575700."""
    uint(value,'rate DWORD',0xFFFFFFFF)
    product=s32(205*s32(value))
    quotient=abs(product)//14*(-1 if product<0 else 1)
    return max(50,min(255,quotient+50))

def cube_batches(elements_per_turn,flags,scale):
    """After successful randomness/lifetime gates, before slot iteration.

    The Effects target (bit40) bypasses this scale. This function reports
    batches, not successful creations, live objects, or a per-second rate.
    """
    uint(elements_per_turn,'ElementsPerTurn',255);uint(flags,'flags',0xFFFFFFFF);uint(scale,'scale',255)
    if flags&0x40 or not flags&0x4000:return elements_per_turn
    return max(int(elements_per_turn!=0),elements_per_turn*scale//255)

def patch_generation_count(record,count,*,use_scale=None):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=246:raise ValueError('246-byte Effects record required')
    uint(count,'ElementsPerTurn',255)
    if use_scale is not None and type(use_scale) is not bool:raise ValueError('Boolean use_scale required')
    out=bytearray(record);out[0xF3]=count
    if use_scale is not None:
        flags=int.from_bytes(out[0xB4:0xB8],'little')
        flags=(flags|0x4000) if use_scale else (flags&~0x4000)
        out[0xB4:0xB8]=flags.to_bytes(4,'little')
    return bytes(out)
