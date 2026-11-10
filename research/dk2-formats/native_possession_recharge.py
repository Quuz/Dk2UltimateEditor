"""Exact DX fixed-point recharge arithmetic; narrow consumer,not combat simulator."""
import struct

def u32(n):return n&0xffffffff
def s32(n):return (n&0x7fffffff)-(n&0x80000000)
def tz(n,d):
    if not d:raise ZeroDivisionError('Native division byzero')
    return (abs(n)//abs(d))*(-1 if (n<0)!=(d<0) else 1)

def qmul(a,b):return u32((s32(a)*s32(b))>>12)

def qdiv(a,b):
    # Native441990 zeroesEDX,thenSHLD/SHL andIDIV: no signed extension ofa.
    q=tz(u32(a)<<12,s32(b))
    if not -0x80000000<=q<=0x7fffffff:raise OverflowError('Native signed quotient overflow')
    return u32(q)

def interval(base_ticks,value_bits,scalar210_bits,*,possessed=False,secondary=False):
    for x in [base_ticks,value_bits,scalar210_bits]:
        if type(x) is not int or not 0<=x<=0xffffffff:raise ValueError('Expected physical DWORD inputs')
    p=value_bits&65535;p=p if p<32768 else p-65536
    if not possessed and not secondary:return base_ticks
    ratio=qdiv(u32((100+p)<<12),(100 if possessed else 200)<<12)
    if possessed:ratio=qmul(ratio,qdiv((scalar210_bits&255)<<12,100<<12))
    return u32(s32(qmul(ratio,u32(base_ticks<<12)))>>12)

def readiness_progress(adjusted_ticks,last_use,tick,denominator=4096,*,ability_present=True):
    for x in [adjusted_ticks,last_use,tick,denominator]:
        if type(x) is not int or not 0<=x<=0xffffffff:raise ValueError('Expected physical DWORD inputs')
    if not ability_present:return dict(ready=0,progress=0)
    ready=1
    if last_use:
        if not denominator:raise ZeroDivisionError('Native readinessDIV0')
        ready=int(u32(last_use+u32(adjusted_ticks<<12)//denominator)<tick)
    elapsed=s32(u32(tick-last_use))
    progress=100 if elapsed>=s32(adjusted_ticks) else qmul(qdiv(100<<12,u32(adjusted_ticks<<12)),u32(elapsed))
    return dict(ready=ready,progress=progress)

def patch_scalar210(raw,value_bits):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=16 or struct.unpack_from('<I',raw)[0]!=210:raise ValueError('Expected16-byte scalar210 record')
    if type(value_bits) is not int or not 0<=value_bits<=0xffffffff:raise ValueError('Expected physical DWORD value')
    out=bytearray(raw);struct.pack_into('<I',out,4,value_bits);return bytes(out)
