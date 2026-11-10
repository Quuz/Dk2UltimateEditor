"""DX anger block arithmetic and raw threshold/treat bit-preserving helpers."""
import struct
import copy
def u32(v):return v&0xFFFFFFFF
def s32(v):return (v&0xFFFFFFFF)-(0x100000000 if v&0x80000000 else 0)
def tz(a,b):return (abs(a)//abs(b))*(-1 if (a<0)!=(b<0) else 1)
def threshold(raw_percent):
    if type(raw_percent) is not int or not 0<=raw_percent<=255:raise ValueError('Expected BYTE percent')
    return ((raw_percent*32767)//100)&65535
def mood_action_gate(previous,available):
    if any(type(v) is not int or not 0<=v<=255 for v in [previous,available]):raise ValueError('Expected BYTE gate and available')
    return (previous&254)|int(available==1)
def apply_action_override(layouts):
    row=layouts['0xD6']['44'];row['baseline_fields']=copy.deepcopy(row['fields']);row['fields']=copy.deepcopy(row['fields']);row['confidence']='native_DX_SET_CREATURE_MOODS_gate_verified';row['native_evidence']='creature-anger-semantics.json;DX518AD0→519704';row['native_note']='AvailableBYTE0==1 setsCWorldBYTE72A1bit01;all othervalues clearit,preservingother7bits. This gates anger-channel writes,not unconditional state recomputation. Seven reserved bytes preserved.'
    return layouts
def state(values,unhappy_at):
    vals=[s32(v) for v in values];mx=max(vals)
    return 0 if mx<s32(unhappy_at) else 2 if mx>32767 else 1
def project(block,mode,*,index=5,value=0,gate=1):
    if not isinstance(block,(bytes,bytearray)) or len(block)!=32:raise ValueError('Expected32-byte anger block')
    if mode not in ['init','reset_all','add_65534_all','reset_one','add','percent_add']:raise ValueError('Unknown operation')
    if type(index) is not int or not 0<=index<6 or type(value) is not int or not 0<=value<=0xFFFFFFFF or type(gate) is not int or not 0<=gate<=255:raise ValueError('Expected index0..5,DWORD value,BYTE gate')
    if mode=='init':return bytes(24)+struct.pack('<II',1,0)
    vals=list(struct.unpack('<6I',block[:24]));thr=struct.unpack_from('<I',block,24)[0]
    if gate&1:
        targets=range(6) if mode in ['reset_all','add_65534_all'] else [index]
        for i in targets:
            delta=-s32(vals[i]) if mode in ['reset_all','reset_one'] else 65534 if mode=='add_65534_all' else tz(s32(u32(vals[i]*value)),100) if mode=='percent_add' else s32(value)
            vals[i]=max(0,min(65533,s32(u32(vals[i]+delta))))
    return struct.pack('<8I',*vals,thr,state(vals,thr))
def mean(block):
    if not isinstance(block,(bytes,bytearray)) or len(block)!=32:raise ValueError('Expected32-byte anger block')
    return tz(s32(u32(sum(struct.unpack('<6I',block[:24])))),6)
def max_index(block):
    if not isinstance(block,(bytes,bytearray)) or len(block)!=32:raise ValueError('Expected32-byte anger block')
    vals=[s32(v) for v in struct.unpack('<6I',block[:24])];return vals.index(max(vals))
def patch_raw(raw,offset,value):
    if not isinstance(raw,(bytes,bytearray)) or len(raw) not in [5449,5537] or offset not in [0xF4F,0xFCB]:raise ValueError('Expected known creature variant and threshold/treat offset')
    n=1 if offset==0xF4F else 2
    if type(value) is not int or not 0<=value<1<<(n*8):raise ValueError('Value outside raw storage width')
    return bytes(raw[:offset])+value.to_bytes(n,'little')+bytes(raw[offset+n:])
