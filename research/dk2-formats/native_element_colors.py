"""Read-only EffectElements color/fade projections for the inspected game build."""
import dk2_formats as fmt

def integer(value,minimum,maximum,label):
    if type(value) is not int or not minimum<=value<=maximum:raise ValueError(label)
    return value

def u32(value):return value&0xFFFFFFFF
def s32(value):
    value&=0xFFFFFFFF
    return value-(1<<32) if value&(1<<31) else value
def trunc_div(n,d):
    if d==0:raise ValueError('Division by zero')
    q=abs(n)//abs(d)
    return -q if (n<0)!=(d<0) else q

def channels(value,count):
    if not isinstance(value,(tuple,list)) or len(value)!=count:raise ValueError('Expected explicit channel sequence')
    return tuple(integer(v,0,255,'Channel must be a byte') for v in value)

def initial_color(resource_type,resource_flags,raw_rgb,*,resolved_player_rgb=None):
    integer(resource_type,0,255,'Resource type must be a byte');integer(resource_flags,0,0xFFFFFFFF,'Flags must be unsigned DWORD')
    raw_rgb=channels(raw_rgb,3)
    if resource_type==1:rgb=(255,255,255);source='sprite_white'
    elif resource_flags&2:
        if resolved_player_rgb is None:raise ValueError('Supply externally resolved player color')
        rgb=tuple(max(c,48) for c in channels(resolved_player_rgb,3));source='resolved_player_minimum48'
    else:rgb=raw_rgb;source='raw_color'
    return dict(channels=rgb+(255,),packed=rgb[0]|rgb[1]<<8|rgb[2]<<16|0xFF000000,source=source)

def alpha_threshold(initial_lifetime,fade_percentage):
    integer(initial_lifetime,0,65535,'Initial lifetime must be the selected unsigned WORD')
    integer(fade_percentage,0,255,'FadePercentage must be a byte')
    signed=initial_lifetime-(initial_lifetime*fade_percentage//100)
    return dict(unsigned=u32(signed),signed=signed)

def fade_after_lifetime_update(rgba,remaining_lifetime,threshold_bits,element_flags,resource_type,renderer_flags=0):
    rgba=channels(rgba,4);integer(remaining_lifetime,-(1<<31),(1<<31)-1,'Remaining lifetime must be signed32')
    integer(threshold_bits,0,0xFFFFFFFF,'Threshold must be raw DWORD');integer(element_flags,0,0xFFFFFFFF,'Element flags must be DWORD')
    integer(resource_type,0,255,'Resource type must be a byte');integer(renderer_flags,0,0xFFFFFFFF,'Renderer flags must be DWORD')
    result=list(rgba);rgb_changed=False;alpha_written=False;h=remaining_lifetime
    if element_flags&8 and h!=0:
        if h<=max(rgba[:3]):
            result[:3]=[trunc_div(s32(u32(h-1)*c),h)&255 for c in rgba[:3]];rgb_changed=True
        if threshold_bits!=0 and h<s32(threshold_bits):
            result[3]=(u32(h*255)//threshold_bits)&255;alpha_written=True
            if resource_type in [2,3,5]:renderer_flags|=1
    return dict(channels=tuple(result),packed=sum(c<<(8*i) for i,c in enumerate(result)),renderer_flags=renderer_flags,
        rgb_branch_taken=rgb_changed,alpha_written=alpha_written,remaining_lifetime=h)

def update_projection(rgba,lifetime_before,elapsed_ticks,threshold_bits,element_flags,resource_type,renderer_flags=0):
    integer(lifetime_before,-(1<<31),(1<<31)-1,'Lifetime must be signed32')
    integer(elapsed_ticks,0,(1<<31)-1,'Elapsed ticks must be a nonnegative signed32 integer')
    return fade_after_lifetime_update(rgba,s32(lifetime_before-elapsed_ticks),threshold_bits,element_flags,resource_type,renderer_flags)

def record_view(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=182:raise ValueError('Expected one182-byte EffectElements record')
    return dict(id=fmt.u16(record,0xA0),name=fmt.string(record[:32]),raw_rgb=list(record[0xAE:0xB1]),
        resource_type=record[0x70],resource_flags=fmt.u32(record,0x60),element_flags=fmt.u32(record,0x9C),
        fade_percentage=record[0xB3],min_hp=fmt.u16(record,0xA2),max_hp=fmt.u16(record,0xA4),
        random_color_index=record[0xB1],table_color_index=record[0xB2],palette_index_meaning='unresolved')
