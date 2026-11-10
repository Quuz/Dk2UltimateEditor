"""Frozen native CPU backend with an explicitly validated IsBadReadPtr import.

Original game/RTTI instructions, IAT bytes and vtables remain unchanged. Only
the existing outside-image import target and declared graphics/sound providers
may execute outside the original PE. Readability is bounded to declared inputs
and readable PE sections, not unconditionally granted.
"""
import json
import struct
import fixture_read_probe as imported
import emulate_object_factory as f
import native_unicorn_factorytrace as backend
from build_effect_semantics import pe_read


def run(exe,args,checks,stop,capture_address,capture_size):
    data=exe.read_bytes()
    assert pe_read(data,0x66C1B8,4)==struct.pack('<I',imported.ADDRESS)
    regions=json.loads(args['memory'])['regions']
    memory={int(r['address'],16):bytes.fromhex(r['hex']) for r in regions}
    bounds=imported.allowed_ranges(data,[r for r in regions if int(r['address'],16)!=imported.ADDRESS])
    assert memory[imported.ADDRESS]==imported.adapter(bounds)
    allowed={(a,n) for a,n in args['allowed_external_IO_code']}
    assert allowed=={(a,len(memory[a])) for a in [f.GFX_CB,f.SOUND_CB,imported.ADDRESS]}
    assert memory[f.GFX_CB]==bytes.fromhex('ff05')+struct.pack('<I',f.OUT+12)+bytes.fromhex('33c0c21400')
    assert memory[f.SOUND_CB]==bytes.fromhex('ff05')+struct.pack('<I',f.OUT+8)+b'\xB8'+bytes(4)+bytes.fromhex('c21000')
    result=backend.run(exe,args,checks,stop,capture_address,capture_size)
    result['declared_import']=dict(name='IsBadReadPtr',IAT=0x66C1B8,target=imported.ADDRESS,
                                   implementation='Bounded known fixture/readable-PE ranges; zero length accepted; overflow rejected.',
                                   readable_ranges=bounds,IAT_patches=0)
    return result
