"""Exact editor-profile extraFlags view;noimplicit cross-format conversion."""
import struct
from native_creature_extension import check
RC3='7368d200cc5ac7e0cdb28c9852d9aa51b960fd718d9eea8dd927c4084add102c'
PRO='989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
PROFILES={RC3:dict(name='RC3',raw_offset=0x1545,writer_bytes=5449,mid_offset=0x1935,presence=0x260),PRO:dict(name='Pro',raw_offset=0x1599,writer_bytes=5537,mid_offset=0x1999,presence=0x264)}
def profile(sha):
    if not isinstance(sha,str) or sha not in PROFILES:raise ValueError('Exactinspectededitor SHA256required')
    return PROFILES[sha]
def extra_flags_view(raw,sha):
    check(raw);p=profile(sha);off=p['raw_offset'];v=struct.unpack_from('<I',raw,off)[0] if off+4<=len(raw) else 0
    return dict(editor=p['name'],field_physically_present=off+4<=len(raw),raw_offset=off,raw_flags=v,native_model_flags=v&15,normal_writer_record_bytes=p['writer_bytes'],game_flags=struct.unpack_from('<I',raw,0x1545)[0])
def patch_extra_flags(raw,sha,mask,enabled):
    check(raw);p=profile(sha);off=p['raw_offset']
    if type(mask) is not int or not 0<=mask<=0xFFFFFFFF or type(enabled) is not bool:raise ValueError('ExpectedDWORDmask andbool')
    if off+4>len(raw):raise ValueError('Selectededitorflag slotabsent;implicitextensionforbidden')
    b=bytearray(raw);prior=struct.unpack_from('<I',b,off)[0];struct.pack_into('<I',b,off,prior|mask if enabled else prior&~mask);return bytes(b)
