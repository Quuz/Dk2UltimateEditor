"""DX recipe storage and matching, derived from original x86 functions.

Names of enum categories are deliberately separate from physical raw storage.
Faults describe the inspected native function, not a recommendation to emit them.
"""
import struct

DX_SHA256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
FIELDS={**{f'input{i}_{part}':(4+2*i+j,1) for i in range(3) for j,part in enumerate(['type','id'])},
        'reward_type':(10,1),'speech_id':(11,1),'reward_value_bits':(12,4)}

def raw_recipe(inputs, reward_type=0, value=0, speech=0):
    if len(inputs)>3:raise ValueError('At most three physical input pairs')
    raw=bytearray(struct.pack('<I',43)+bytes(12))
    for i,(kind,ident) in enumerate(inputs):
        raw=bytearray(patch_field(raw,f'input{i}_type',kind))
        raw=bytearray(patch_field(raw,f'input{i}_id',ident))
    for name,v in [('reward_type',reward_type),('speech_id',speech),('reward_value_bits',value)]:raw=bytearray(patch_field(raw,name,v))
    return bytes(raw)

def validate_raw(raw):
    if len(raw)!=16 or struct.unpack_from('<I',raw)[0]!=43:raise ValueError('Expected a complete type43 16-byte record')

def patch_field(raw,field,value):
    validate_raw(raw)
    if field not in FIELDS:raise ValueError('Unknown physical field')
    off,n=FIELDS[field]
    if isinstance(value,bool) or not isinstance(value,int) or not 0<=value<1<<(8*n):raise ValueError('Unsigned physical range exceeded')
    out=bytearray(raw);out[off:off+n]=value.to_bytes(n,'little');return bytes(out)

def runtime_recipe(raw):
    """51DB30 compacts nonzero types in physical order; 50F7E0 stores 34 bytes."""
    validate_raw(raw)
    entries=[struct.pack('<IBB',raw[4+2*i],raw[5+2*i],0) for i in range(3) if raw[4+2*i]]
    return b''.join(entries)+bytes(6*(3-len(entries)))+struct.pack('<IIII',len(entries),raw[10],int.from_bytes(raw[12:16],'little'),raw[11])

def history_append(room,event):
    """4F1600: event=(DWORD type,BYTE ID,BYTE level), three decreasing ring slots."""
    if len(room)<0x5e:raise ValueError('Room history is truncated')
    kind,ident,level=event
    if not 0<=kind<1<<32 or not 0<=ident<=255 or not 0<=level<=255:raise ValueError('Physical event range exceeded')
    slot=room[0x5c]
    if slot>2:raise ValueError('Corrupt signed next-slot index is outside this safe model')
    out=bytearray(room);struct.pack_into('<IBB',out,0x49+slot*6,kind,ident,level)
    out[0x5b]=slot;out[0x5c]=(slot-1)%3;out[0x5d]=min(room[0x5d]+1,3)
    return bytes(out)

def match(room,recipe,previous_output=bytes([0x66])*12):
    """4F1660: ordered suffix; BYTE level sum wraps before integer averaging."""
    if len(room)<0x5e or len(recipe)!=34 or len(previous_output)!=12:raise ValueError('Incomplete input')
    count=struct.unpack_from('<I',recipe,18)[0]
    if count>room[0x5d]:return dict(matched=False,output=bytes(previous_output))
    if count>3:raise ValueError('Corrupt runtime count is outside this safe model')
    last=room[0x5b]
    if count and last>2:raise ValueError('Corrupt last-slot index is outside this safe model')
    creatures=level_sum=0;matches=True
    for i in range(count):
        slot=(last+i)%3;want_type,want_id,_=struct.unpack_from('<IBB',recipe,6*(count-i-1))
        actual_type,actual_id,level=struct.unpack_from('<IBB',room,0x49+6*slot)
        if (actual_type,actual_id)!=(want_type,want_id):matches=False
        elif want_type==1:creatures=(creatures+1)&255;level_sum=(level_sum+level)&255
    if not matches:return dict(matched=False,output=bytes(previous_output))
    kind,value,speech=struct.unpack_from('<III',recipe,22)
    if kind==1:
        if creatures==0:raise ZeroDivisionError('Original 4F1749 divides by the number of creature inputs')
        average=level_sum//creatures;value=(value+(average<<16))&0xffffffff
    else:average=None
    return dict(matched=True,output=struct.pack('<III',kind,value,speech),average_level=average)
