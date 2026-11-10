"""Exact inspected KeeperSpells flag/AI-selection fragments; no complete AI simulation."""
def checked(v,bits=32):
    if type(v) is not int or not 0<=v<1<<bits:raise ValueError(f'Expected unsigned{bits} integer')
    return v
def game_flags(raw_flags,intermediate_initial=0):
    return checked(raw_flags)|checked(intermediate_initial)
def editor_saved_flags(raw_flags):return checked(raw_flags)&0x1E
def eligible_flags(raw_flags,mode):
    checked(raw_flags)
    if mode not in ['defensive','attacking']:raise ValueError('Unknown selection mode')
    return bool(raw_flags&(8 if mode=='defensive' else 4)) and not bool(raw_flags&16)
def random_bound(cost_bits,mode):
    checked(cost_bits)
    if mode not in ['defensive','attacking']:raise ValueError('Unknown selection mode')
    # Preserve original MUL-high/SHR expression, independently audited against /2499.
    return ((cost_bits*0xD1CC931B)>>43)+(1 if mode=='defensive' else 2)
def select_spell(mode,spells,power,draws):
    """spells[i-1] supplies helpers/definition for enumerated integer ID i.

    Caller gates, PRNG, target search and cast validation/application are outside
    this projection. Draws are explicit helper results, not a simulated seed.
    """
    checked(power,3)
    if mode not in ['defensive','attacking']:raise ValueError('Unknown selection mode')
    selected=0;used=0;trace=[];threshold=(4-power)&0xFFFFFFFF
    for i,spell in enumerate(spells,1):
        flags=checked(spell['flags']);cost=checked(spell['cost'])
        if spell.get('availability',3)!=3 or spell.get('cooldown',0)!=0 or not eligible_flags(flags,mode):continue
        if used>=len(draws):raise ValueError('Missing explicit random result')
        bound=random_bound(cost,mode);draw=checked(draws[used]);used+=1
        if not draw<bound:raise ValueError('Random result outside requested bound')
        passed=draw>=threshold
        # Defensive fallback is assigned BEFORE comparing the random result.
        if mode=='defensive' or passed:selected=i&255
        trace.append(dict(id=i,bound=bound,draw=draw,passed=passed))
        if passed:break
    return dict(selected_id=selected,draw_count=used,unsigned_threshold=threshold,trace=trace)
def record_view(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=406:raise ValueError('Expected406-byte KeeperSpells record')
    flags=int.from_bytes(record[0xDC:0xE0],'little');cost=int.from_bytes(record[0x11C:0x120],'little')
    return dict(raw_flags=flags,cost_bits=cost,defensive_flag_gate=eligible_flags(flags,'defensive'),attacking_flag_gate=eligible_flags(flags,'attacking'),editor_saved_flags=editor_saved_flags(flags),opaque_c8=int.from_bytes(record[0xC8:0xCC],'little'))
def patch_flags(record,raw_flags):
    record_view(record);checked(raw_flags)
    return bytes(record[:0xDC])+raw_flags.to_bytes(4,'little')+bytes(record[0xE0:])
