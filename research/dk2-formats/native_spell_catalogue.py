"""DX CreatureSpell callback projection; not a general catalogue repair policy."""
import struct
from native_spell_alternatives import full_intermediate,runtime_projection

def lookup_index(ids,ident):
    if type(ident) is not int or not 0<=ident<=255 or any(type(v) is not int or not 0<=v<=255 for v in ids):raise ValueError('Expected byte IDs')
    i=min(len(ids)-1,ident-1)
    while i>=0:
        if ids[i]==ident:return i
        if ids[i]<ident:return None
        i-=1
    return None

def project(records,previous,capacity,tps=20,gate=0,update=False):
    """Full70-byte native loader projection with null resource provider.

    Existing buffers and checksum accumulator remain caller-owned. Return the
    checksum sum delta only for insertion; update callback does not add to it.
    """
    if type(capacity) is not int or capacity<0 or len(previous)>capacity or any(len(b)!=70 for b in previous):raise ValueError('Invalid preallocated definition storage')
    nodes=list(previous);delta=0
    for raw in records:
        mid=full_intermediate(raw,0);ident=mid[0x136]
        if update:
            i=lookup_index([n[0x43] for n in nodes],ident)
            if i is None:raise ValueError('Native update dereferences null for missing ID')
            nodes[i]=runtime_projection(mid,nodes[i],tps,gate)
        elif len(nodes)<capacity:
            i=len(nodes)
            while i>0 and nodes[i-1][0x43]>=ident:i-=1
            # Caller test fixtures seed every unused native destination with A5.
            old=nodes[i] if i<len(nodes) else bytes([165])*70
            new=runtime_projection(mid,old,tps,gate);nodes.insert(i,new);delta=(delta+struct.unpack_from('<I',new,0x1B)[0])&0xFFFFFFFF
    return nodes,delta
