"""Physical Creature-job patching;expose exact-profile alias without normalizing."""
import struct
from native_creature_extension import check
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
PRO='989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
RC3='7368d200cc5ac7e0cdb28c9852d9aa51b960fd718d9eea8dd927c4084add102c'
PROFILES={DX:dict(name='DX',role='game',mid_bytes=6457,mid_hate=0x1264),PRO:dict(name='Pro',role='editor',mid_bytes=6561,mid_hate=0x1274,writer_bytes=5537),RC3:dict(name='RC3',role='editor',mid_bytes=6457,mid_hate=0x1264,writer_bytes=5449)}
FIELDS={'hate1':(0xE74,4),'hate2':(0xE78,4),'extra3':(0x14E1,4),'extra4':(0x14E5,4),'alternative_job':(0xE7C,4),'alternative_mood':(0xE80,2),'alternative_mana':(0xE82,2)}

def patch_field(raw,field,value):
    check(raw)
    if field not in FIELDS:raise ValueError('Unknown physical job field')
    offset,width=FIELDS[field]
    if type(value) is not int or not 0<=value<(1<<(width*8)):raise ValueError('Unsigned value does not fit physical field width')
    result=bytearray(raw);result[offset:offset+width]=value.to_bytes(width,'little');return bytes(result)

def view(raw,executable_sha256):
    check(raw)
    if executable_sha256 not in PROFILES:raise ValueError('Exact inspected executable SHA256 required')
    p=PROFILES[executable_sha256];h1,h2=struct.unpack_from('<2I',raw,0xE74);x1,x2=struct.unpack_from('<2I',raw,0x14E1);job,mood,mana=struct.unpack_from('<IHH',raw,0xE7C)
    dwords=[h1,h2,job,mood|(mana<<16)]
    result=dict(profile=p['name'],record_bytes=len(raw),physical_hate_jobs=[h1,h2],physical_extra_jobs=[x1,x2],alternative0=dict(job=job,mood=mood,mana=mana),
      intermediate_bytes=p['mid_bytes'],intermediate_hate_start=p['mid_hate'],final_intermediate_hate_dwords=dwords,extra_jobs_shadowed_by_alternative0=True)
    if p['role']=='game':result.update(runtime_hate_bytes=[v&255 for v in dwords],runtime_count=4,membership_rule='Nonzero fullDWORD query equals any storedBYTE;query is not narrowed')
    else:result.update(native_writer_record_bytes=p['writer_bytes'],native_writer_preserves_independent_hate3_4=False)
    return result
