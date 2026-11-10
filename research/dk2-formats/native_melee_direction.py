"""Exact safe-range integer angle and the observed original melee modifier."""
import struct
from build_effect_semantics import pe_read


def table(data,address=0x65FC94):return struct.unpack('<257H',pe_read(data,address,514))


def angle(dx,dy,lookup):
    # The original SHL8/DIV can overflow for extreme coordinates. This API
    # intentionally certifies only the nonoverflowing prepared range.
    if type(dx) is not int or type(dy) is not int or max(abs(dx),abs(dy))>0x7FFFFF:raise ValueError('Outside checked coordinate range')
    x,y=abs(dx),abs(dy)
    if x==y==0:return 0
    if x>=y:
        q=lookup[(y<<8)//x]
        a=512+q if dx>=0 and dy>=0 else 512-q if dx>=0 else 1536-q if dy>=0 else 1536+q
    else:
        q=lookup[(x<<8)//y]
        a=1024-q if dx>=0 and dy>=0 else q if dx>=0 else 1024+q if dy>=0 else 2048-q
    return a&2047


def modifier(a):
    if type(a) is not int or not 0<=a<2048:raise ValueError('Expected normalized original angle')
    folded=2046-a if a>1023 else a
    p=(folded&1023)//20
    return dict(angle=a,folded_word=folded&65535,percent_candidate=p,applied_percent=p if p>15 else 0)


def damage(base,a):
    if type(base) is not int or not 0<=base<=65535:raise ValueError('Expected physical u16 base damage')
    m=modifier(a);return base+(base*m['applied_percent'])//100
