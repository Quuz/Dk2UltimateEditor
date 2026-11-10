"""Exact integer projections for Creatures+EE4, native Creeping distance weight.

The metric is a squared XY distance in Q12, not an ordinary movement speed.
Only selected arithmetic/serialization branches are modeled, not full AI or LOS.
"""
def checked(value,bits):
    if type(value) is not int or not 0<=value<(1<<bits):raise ValueError(f'Expected unsigned{bits} integer')
    return value

def u32(value):return value&0xFFFFFFFF
def s32(value):return (value+0x80000000)%0x100000000-0x80000000
def s16(value):
    checked(value,16)
    return value if value<0x8000 else value-0x10000
def trunc100(value):return value//100 if value>=0 else -((-value)//100)

def weighted_distance(distance_q12,raw_percentage,level,level_multiplier_bits,state_flags):
    checked(distance_q12,32);checked(raw_percentage,32);checked(level,8);checked(level_multiplier_bits,16);checked(state_flags,32)
    if not state_flags&0x1000:return dict(distance=distance_q12,effective_percentage=None,active=False)
    percentage=s32(raw_percentage) if level==1 else trunc100(s32(s16(level_multiplier_bits)*s32(raw_percentage)))
    result=u32(distance_q12*percentage)//100
    return dict(distance=result,effective_percentage=percentage,active=True)

def squared_distance_q12(source_x,source_y,target_x,target_y):
    for value in [source_x,source_y,target_x,target_y]:
        if type(value) is not int or not -0x80000000<=value<=0x7FFFFFFF:raise ValueError('Expected signed32 coordinate')
    dx=s32(target_x-source_x);dy=s32(target_y-source_y)
    return u32(((dx*dx)>>12)+((dy*dy)>>12))

def within_numeric_sight(weighted_q12,sight_radius_tiles_bits):
    checked(weighted_q12,32);checked(sight_radius_tiles_bits,32)
    threshold=u32(sight_radius_tiles_bits*sight_radius_tiles_bits*4096)
    return s32(weighted_q12)<s32(threshold)

def native_editor_projection(raw_percentage):
    """Present callback field -> model byte -> writer zero-extended DWORD."""
    return checked(raw_percentage,32)&0xFF

def editor_integer_update(converted_integer):
    """Only the integer result AFTER native __ftol; arbitrary GUI float parsing excluded."""
    if type(converted_integer) is not int or not -0x8000000000000000<=converted_integer<=0x7FFFFFFFFFFFFFFF:
        raise ValueError('Expected signed64 converted integer')
    return converted_integer&0xFF

def creep_flag_after_action(state_flags,action_value,possessed_creature_present=True):
    checked(state_flags,32);checked(action_value,32)
    if type(possessed_creature_present) is not bool:raise ValueError('Expected boolean presence')
    return ((state_flags&0xFFFFEFFF)|((action_value&1)<<12)) if possessed_creature_present else state_flags

def record_view(record):
    if not isinstance(record,(bytes,bytearray,memoryview)) or len(record) not in [5449,5537]:raise ValueError('Expected complete supported Creatures record')
    raw=int.from_bytes(record[0xEE4:0xEE8],'little')
    return dict(raw_percentage=raw,game_runtime_bits=raw,native_editor_saved_projection=native_editor_projection(raw))
