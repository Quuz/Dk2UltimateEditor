"""DX virtualsetter cap and original4D4F60 vector truncation arithmetic."""
import math

def u32(n):return n&0xffffffff
def s32(n):return (n&0x7fffffff)-(n&0x80000000)

def instance_cap(requested):
    if type(requested) is not int or not 0<=requested<=0xffffffff:raise ValueError('ExpectedunsignedDWORD')
    value=min(requested,4095);return dict(word11c=value,controller_max_speed=value<<4)

def truncate_vector(vector,limit):
    if len(vector)!=2 or any(type(x) is not int or not -0x80000000<=x<=0xffffffff for x in vector):raise ValueError('Expectedtwo32bitcomponents')
    if type(limit) is not int or not 0<=limit<=0xffffffff:raise ValueError('ExpectedDWORDlimit')
    x,y=map(s32,vector);ax,ay=abs(x),abs(y);flags=ax|ay
    shift=0 if flags&0xfffff000==0 else (8 if flags&0xffc00000==0 else 18)
    length=u32(math.isqrt(u32((ax>>shift)**2+(ay>>shift)**2))<<shift)
    if s32(length)<s32(limit):return [u32(x),u32(y)]
    if not length:raise ZeroDivisionError('NativevectorlimiterDIV0')
    numerator=s32(limit)<<16;denominator=s32(length)
    ratio=(abs(numerator)//abs(denominator))*(-1 if (numerator<0)!=(denominator<0) else 1)
    if not -0x80000000<=ratio<=0x7fffffff:raise OverflowError('Nativequotientoverflow')
    return [u32((x*ratio)>>16),u32((y*ratio)>>16)]
