"""Runtime Keeper insertion/overlay oracle;raw file order is never normalized."""
import struct
from native_keeper_pipeline import runtime_projection
from native_keeper_lookup import lookup_index
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')
def default_definition(seed=165):
    b=bytearray([seed])*122;b[:0x36]=bytes(0x36);b[0x3A:]=bytes(122-0x3A);return bytes(b)

def apply(array,count,capacity,aggregate,mid,overlay=False,gate=0,tps=20,null_previous=bytes([204])*122):
    rows=[bytes(array[i:i+122]) for i in range(0,len(array),122)];assert len(rows)==capacity
    key=mid[0x166];selected=None;null_result=None;sound_calls=0
    if overlay:
        selected=lookup_index([r[0x75] for r in rows[:count]],key)
        if selected is None:null_result=runtime_projection(mid,null_previous,tps,gate)
        else:rows[selected]=runtime_projection(mid,rows[selected],tps,gate)
        called=True
    elif count<capacity:
        i=count-1
        while i>=0 and rows[i][0x75]>=key:rows[i+1]=rows[i];i-=1
        selected=i+1;rows[selected]=runtime_projection(mid,rows[selected],tps,gate)
        aggregate=(aggregate+struct.unpack_from('<I',rows[selected],0x36)[0])&0xFFFFFFFF;count+=1;called=True
    else:called=False
    if called and gate==0:sound_calls=sum(bool(struct.unpack_from('<I',mid,p)[0]) for p in [8,12])
    return dict(array=b''.join(rows),count=count,aggregate=aggregate,selected=selected,null_result=null_result,sound_calls=sound_calls,loader_called=called)

def ids_result(ids,method,argument,orders=None):
    if method=='first':return ids[0] if ids else 0
    if method=='index':return ids[argument] if argument<len(ids) else 0
    if method=='order':
        return next((ids[i] for i,o in enumerate(orders) if o==argument),0)
    key=argument&255
    try:i=ids.index(key)
    except ValueError:return 0
    return ids[(i+(1 if method=='next' else -1))%len(ids)]
