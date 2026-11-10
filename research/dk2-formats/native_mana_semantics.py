"""Read-only projections of inspected DKII-DX.exe mana branches.

Raw WORDs retain their unsigned bit pattern; semantic consumers sign-extend them.
These functions model selected handlers, not the complete world/player simulation.
"""

def integer(value,low,high):
    if type(value) is not int or not low<=value<=high:raise ValueError('Integer outside native input range')
    return value

def u32(value):return value&0xFFFFFFFF
def s32(value):return (value+0x80000000)%0x100000000-0x80000000
def s16(bits):
    integer(bits,0,0xFFFF)
    return bits if bits<0x8000 else bits-0x10000

def ownership_deltas(owner_word,requested_owner,world_flags,gain_word,ceiling_word):
    """0x4559D0 field deltas only; other callbacks and mode4 side effects excluded."""
    integer(owner_word,0,0xFFFF);integer(requested_owner,0,0xFFFFFFFF);integer(world_flags,0,255)
    gain=s16(gain_word);ceiling=s16(ceiling_word);old=owner_word&0xFFF;new=requested_owner&0xFFF
    updates=[];changed=old!=(requested_owner&0xFFFF)
    if changed:
        if old:updates.append(dict(owner=old,gain=-gain,ceiling=-ceiling))
        updates.append(dict(owner=new,gain=gain,ceiling=ceiling))
        owner_word=(owner_word&0xF000)|new
    elif old and world_flags&0x80:updates.append(dict(owner=old,gain=gain,ceiling=ceiling))
    return dict(owner_word=owner_word,updates=updates,changed_branch=changed)

def terrain_change_delta(old_id,new_id,old_flags,new_flags,owner_word,world_flags,old_gain_word,new_gain_word):
    """0x455B90 only. Its branch changes gain; it does not update ceiling."""
    for value in [old_id,new_id,world_flags]:integer(value,0,255)
    for value in [old_flags,new_flags]:integer(value,0,0xFFFFFFFF)
    integer(owner_word,0,0xFFFF);old_gain=s16(old_gain_word);new_gain=s16(new_gain_word)
    delta=0;branch='same type'
    if old_id!=new_id:
        if not(old_flags&0x10 or new_flags&0x10):
            branch='ordinary skipped'
            if owner_word&0xFFF and not world_flags&0x40:
                delta=new_gain-(old_gain if old_id else 0);branch='ordinary adjusted'
        elif new_id in [0x11,0x1A]:delta=1;branch='special new type'
        elif old_id in [0x11,0x1A]:delta=-1;branch='special old type'
        else:branch='room skipped'
    return dict(terrain_id=new_id,gain_delta=delta,ceiling_delta=0,owner=owner_word&0xFFF,branch=branch)

def gross_income(extra,basic,helper_cost,one_shot_gain,ceiling):
    """0x4C1770 uses signed32 sums and signed MIN with the per-second ceiling."""
    for value in [extra,basic,helper_cost,one_shot_gain,ceiling]:integer(value,-0x80000000,0x7FFFFFFF)
    subtotal=s32(extra+basic+helper_cost+one_shot_gain)
    return min(subtotal,ceiling)

def income_due(last_tick,current_tick,ticks_per_second):
    """Unsigned deadline comparison at0x4C1109..0x4C1121; overflow wraps."""
    for value in [last_tick,current_tick,ticks_per_second]:integer(value,0,0xFFFFFFFF)
    return current_tick>=u32(last_tick+ticks_per_second)

def tick_arithmetic(extra,basic,helper_before,helper_after,ceiling,multiplier,current,max_mana,mode=0,setting=1):
    """0x4C1127..0x4C11EC. Both one-shot fields reset; helper results supplied.

    Clock gate, full cost helper, warning logic and end-of-function tick update
    are outside this projection. Two helper calls may return different values.
    """
    for value in [extra,basic,helper_before,helper_after,ceiling,multiplier,current,max_mana]:integer(value,-0x80000000,0x7FFFFFFF)
    integer(mode,0,0xFFFFFFFF);integer(setting,0,0xFFFF)
    gross=gross_income(extra,basic,helper_before,0,ceiling)
    delta=s32(gross-helper_after)
    if delta>0:delta=s32(delta*multiplier)
    if mode==3:
        if setting==0:
            doubled=s32(delta*2)
            delta=s32(doubled+(3 if doubled<0 else 0))>>2
        elif setting==2:delta=s32(delta*2)
    result=s32(current+delta)
    if delta<0:
        if result<0:result=0
    elif result>max_mana:result=max_mana
    return dict(gross=gross,delta=delta,mana=result,one_shot_gain=0,one_shot_loss=0)

def trap_upkeep(usage_word,maintain,extra,*,adding=True):
    """0x4C1B60 /0x4C1B90: signed trap definition+0x96, raw+0x202."""
    usage=s16(usage_word)
    for value in [maintain,extra]:integer(value,-0x80000000,0x7FFFFFFF)
    if type(adding) is not bool:raise ValueError('Expected boolean adding')
    amount=usage if adding else -usage
    return dict(maintain=s32(maintain+amount),extra=s32(extra-amount))
