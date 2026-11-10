"""Projection of DX44ACE0 with explicit world/time/orientation providers.

Does not simulate interpolation, rendering or mode11 kind6 random placement.
Runtime manager offsets below are never raw CD field offsets.
"""
import struct
from native_cameras import clamp_exclusive

U32=0xFFFFFFFF
GROUP2={3,4,5,9,13,14,15}
SPECIAL={2,10,11}
SUPPORTED={1,2,3,4,5,7,9,10,11,13,14,15,18}

def get(b,o):return struct.unpack_from('<I',b,o)[0]
def put(b,o,v):struct.pack_into('<I',b,o,v&U32)
def word(b,o):return struct.unpack_from('<H',b,o)[0]
def setw(b,o,v):struct.pack_into('<H',b,o,v&65535)
def group(n):return 1 if n in {1,2,10,11} else 2 if n in GROUP2 else 3 if n==7 else 4 if n==18 else 0

def activate(manager,mode,node=0,*,now=123456,dimensions=(32,48),unit=4096,
             orientations=bytes(190),entity=None,definition=None,ground=768,node_valid=False):
    """Return full projected manager, explicit provider observations and entity.

    Models valid bank indices0..19, with0/19 rejected after source copy.
    Input node ID is truncated to WORD. Mode11 kind6 is deliberately excluded.
    Empty/reversed scalar bounds retain native signed exclusive-clamp behavior.
    Providers do not modify manager except where this projection says so.
    """
    if len(manager)!=0xF00 or not 0<=mode<=19 or len(orientations)<160:
        raise ValueError('Expected manager0xF00, bounded mode0..19 and orientations')
    b=bytearray(manager);ent=bytearray(entity) if entity is not None else None
    calls=dict(time=0,hook=[],reset=0,matrix=0,dimensions=0,ground=0,events=[])
    glob=0;node&=65535
    def tick():calls['time']+=1;return now&U32
    def mode_hook():calls['hook'].append(glob)
    old=get(b,0xDA8);pending=get(b,0xDD4)
    if (old in SPECIAL)!=(pending in SPECIAL):
        glob=int(pending in SPECIAL);mode_hook()
    if old in GROUP2:put(b,0xDE0,get(b,0xD80))
    duration=500 if group(old)==group(mode) else 2000
    if old==7 and mode!=7 or get(b,0xE3C)&64:duration=0
    src=0xE92 if mode in {3,4,5,7} else 0xE82
    if b[src:src+16]!=b[0xE72:0xE82]:
        put(b,0xE4A,tick());put(b,0xE4E,duration)
        b[0xE52:0xE62]=b[0xE62:0xE72];b[0xE72:0xE82]=b[src:src+16]
    put(b,0xDAC,old);setw(b,0xDEC,node)
    if get(b,0xD92)&128 or mode not in SUPPORTED:
        return dict(manager=bytes(b),result=0,global_mode=glob,calls=calls,entity=bytes(ent) if ent is not None else None,duration=duration)
    if mode in {7,18}:
        put(b,0xDA8,mode);put(b,0xDD4,mode)
        return dict(manager=bytes(b),result=1,global_mode=glob,calls=calls,entity=bytes(ent) if ent is not None else None,duration=duration)
    slot=9 if mode in {13,14,15} else mode
    p=b[40+slot*152:40+(slot+1)*152]
    calls['dimensions']+=1
    for off,v in [(0xD38,0),(0xD3C,0),(0xD40,get(p,88)),(0xD44,dimensions[0]<<12),
                  (0xD48,dimensions[1]<<12),(0xD4C,get(p,100)),
                  (0xD50,get(p,120)),(0xD54,get(p,124)),(0xD60,0 if mode==11 else get(p,108)),
                  (0xD64,get(p,112)),(0xD58,get(p,132)),
                  (0xD5C,get(p,132 if mode in {13,14,15} else 136)),(0xD92,get(p,140))]:put(b,off,v)
    # Default target uses activeXY and presetZ; the raw X/Y extents are ignored.
    b[0xDB6:0xDBE]=b[0xD68:0xD70];put(b,0xDBE,get(p,76))
    put(b,0xDCC,get(p,104));put(b,0xDD0,get(p,128))
    lens_lower=get(b,0xD58)
    if mode in GROUP2:
        put(b,0xDC2,get(p,116) if mode in {3,4,5,9} and get(b,0xE3C)&64 else get(b,0xDE0))
        if mode in {13,14,15}:b[0xDC6:0xDCC]=orientations[mode*10:mode*10+6]
        else:
            setw(b,0xDC6,(word(b,0xD84)+4)&0xFFF8)
            setw(b,0xDC8,word(p,148)&2047);setw(b,0xDCA,word(p,146)&2047)
            if get(b,0xE3C)&64:put(b,0xD8E,get(p,128))
        if old in GROUP2:
            if not unit or not get(b,0xD8E):raise ValueError('Native unsigned DIV denominator zero')
            zoom=((get(b,0xD80)*unit)&U32)//get(b,0xD8E)
            put(b,0xDC2,((zoom*get(b,0xDD0))&U32)//unit)
    elif mode==11:
        if node_valid and ent is not None and get(ent,0x12)==6:
            raise ValueError('Mode11 kind6 random placement is not projected')
        calls['ground']+=1;put(b,0xDBE,ground+256)
        setw(b,0xDC6,word(b,0xD84)&2047);setw(b,0xDC8,400);setw(b,0xDCA,200)
        put(b,0xDC2,get(p,116));put(b,0xDCC,0);put(b,0xDD0,get(p,132))
        calls['events']=[(9,tick(),1500),(7,1500)]
    else:
        put(b,0xDC2,get(p,116));setw(b,0xDC6,0);setw(b,0xDC8,512);setw(b,0xDCA,word(p,148)&2047)
        if node:
            if ent is None:raise ValueError('Native path dereferences nonzero node without validity check')
            kind=get(ent,0x12)
            if kind in {0,2}:
                if definition is None:raise ValueError('Expected entity definition fixture')
                b[0xDB6:0xDBE]=ent[0x1A:0x22]
                height=word(definition,0xFA if kind==2 else 0x65C)
                put(b,0xDBE,(ground if kind==2 else get(ent,0x22))+height)
                if kind==2:calls['ground']+=1;put(b,0xE34,0)
                else:
                    put(b,0xE38,0)
                    if ent[0x36F]==0x2D:
                        # Signed truncation toward zero for lower bound; lens value unsigned>>1.
                        n=lens_lower if lens_lower<0x80000000 else lens_lower-0x100000000
                        lens_lower=(abs(n)//2)*(1 if n>=0 else -1)&U32
                        put(b,0xDD0,get(b,0xDD0)>>1)
                setw(b,0xDC6,word(ent,0xF4)&2047)
    for off,lo,hi in [(0xDCC,get(b,0xD60),get(b,0xD64)),(0xDC2,get(b,0xD50),get(b,0xD54)),(0xDD0,lens_lower,get(b,0xD5C))]:put(b,off,clamp_exclusive(get(b,off),lo,hi))
    put(b,0xDD4,mode)
    if mode in GROUP2 or mode==11:put(b,0xDA8,mode)
    else:put(b,0xE3C,get(b,0xE3C)|256)
    if mode==11:put(b,0xE3C,get(b,0xE3C)|256);duration=3000
    # Full44BD70 is the transition setup helper; duration0 applies immediately.
    if duration:
        t=tick();put(b,0xDDC,t);put(b,0xDD8,t+duration)
    else:
        glob=0;put(b,0xE3C,get(b,0xE3C)&~256);calls['reset']+=1
        if (get(b,0xDA8) in SPECIAL)!=(get(b,0xDD4) in SPECIAL):
            glob=int(get(b,0xDD4) in SPECIAL);mode_hook()
            if glob:put(b,0xE3C,get(b,0xE3C)|256)
        b[0xD68:0xD74]=b[0xDB6:0xDC2];put(b,0xD80,get(b,0xDC2))
        b[0xD84:0xD8A]=b[0xDC6:0xDCC];calls['matrix']+=1
        put(b,0xD8A,get(b,0xDCC));put(b,0xD8E,get(b,0xDD0))
        put(b,0xDA8,get(b,0xDD4));put(b,0xDD8,0)
        if node_valid:
            if ent is None:raise ValueError('Expected valid node entity')
            ent[0xF3]&=254
    return dict(manager=bytes(b),result=1,global_mode=glob,calls=calls,entity=bytes(ent) if ent is not None else None,duration=duration)
