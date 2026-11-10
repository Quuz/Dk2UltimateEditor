"""Exact-profile physical job/weight edits; preserve mood/mana and padding."""
from native_creature_extension import check
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
ARRAYS={'happy':(0xE14,3),'unhappy':(0xE38,2)}
FIELDS={'job':(0,4),'weight':(8,1)}

def offset(raw,profile,array,index,field):
    check(raw)
    if profile!=DX:raise ValueError('Exact inspected DX executable SHA256 required')
    if array not in ARRAYS or field not in FIELDS:raise ValueError('Supported physical Happy/Unhappy job or weight field required')
    start,count=ARRAYS[array]
    if type(index) is not int or not 0<=index<count:raise ValueError('Physical preference index out of range')
    delta,width=FIELDS[field];return start+12*index+delta,width

def patch(raw,profile,array,index,field,value):
    at,width=offset(raw,profile,array,index,field)
    if type(value) is not int or not 0<=value<1<<(8*width):raise ValueError('Unsigned physical value does not fit')
    out=bytearray(raw);out[at:at+width]=value.to_bytes(width,'little');return bytes(out)

def view(raw,profile):
    out={}
    for array,(start,count) in ARRAYS.items():
        rows=[]
        for i in range(count):
            at,_=offset(raw,profile,array,i,'job');job=int.from_bytes(raw[at:at+4],'little')
            rows.append(dict(raw_offset=at,raw_job_u32=job,loaded_job_u8=job&255,weight_u8=raw[at+8],
                mood_raw_hex=raw[at+4:at+6].hex(),mana_raw_hex=raw[at+6:at+8].hex(),reserved3_hex=raw[at+9:at+12].hex()))
        out[array]=rows
    return dict(profile_sha256=profile,record_bytes=len(raw),arrays=out,Job28='Crypt',
        caution='Weight0doesnotdisablefallbackselection. RawDWORDhighbitsandallunmodifiedbytesarepreserved; loadedjobnarrows8bits.')
