"""Exact DX48ABB0 numeric projection under explicitly supplied runtime state.

No movement, world lookup or renderer. Native ordinary CC vcall88 is separate;
other class implementations must supply their actual vcall result.
"""
u32=lambda n:int(n)&0xffffffff
s32=lambda n:(int(n)&0x7fffffff)-(int(n)&0x80000000)
s16=lambda n:(int(n)&0x7fff)-(int(n)&0x8000)


def truncdiv(a,b):return (abs(a)//abs(b))*(-1 if (a<0)!=(b<0) else 1)


def load_base_speed(raw,tps):
    raw=u32(raw);tps=u32(tps)
    if not tps:raise ZeroDivisionError('Original loader DIV TPS has zero divisor')
    value=(raw//tps)&65535
    return value if value or not raw else 1


def regular_actor_v88(definition_flags,actor_flags):
    """Actual66D524+88 ->48ED40 profile, not every possible Actor subclass."""
    return bool(definition_flags&1 and actor_flags&0x100)


def speed_terms(*,walk,run,shuffle,mode,definition_HP,definition_flags,current_HP,level,health_growth_word,
                terrain_flags,terrain_v88_result,actor_flags,actor_flags2,stat21_word):
    base=u32(run if mode==1 else shuffle if mode==2 else walk)
    if (level&255)==1:maxhp=definition_HP&65535
    else:maxhp=truncdiv(s32(s16(health_growth_word)*(definition_HP&65535)),100)
    threshold=u32(maxhp*20)//100
    health_enabled=not bool(definition_flags&1)
    wounded=health_enabled and s32(current_HP)<s32(threshold)
    after_health=u32(base*20)//100 if wounded else base
    surface=0
    if not terrain_v88_result:
        if terrain_flags&128:surface=128
        elif terrain_flags&256:surface=256
    after_surface=u32(after_health*(50 if surface==128 else 150))//100 if surface else after_health
    divisor=100 if actor_flags&128 else 200 if actor_flags2&8 else None
    coefficient=s16(stat21_word)
    result=u32(after_surface*u32(100+coefficient))//divisor if divisor else after_surface
    return dict(loaded_base=base,maxHP_formula_value=maxhp,health_branch_enabled=health_enabled,health_threshold=threshold,
        wounded_branch=wounded,after_health=after_health,selected_surface_bit=surface,after_surface=after_surface,
        coefficient_signed16=coefficient,final_divisor=divisor,result=result)
