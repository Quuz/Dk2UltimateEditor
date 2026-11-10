"""Shot constructor/initializer oracle under phase54 explicit helper contracts."""
import struct
def projected_node(seed,shot_id,definition,definition_address,owner=7,orientation=0x4321,position=(0x1200,0xFFFFDE00,0x3300),sound_enabled=1,sound_handle=0x13572468):
    if type(seed) is not int or not 0<=seed<=255 or len(definition)!=0x80:raise ValueError('Expected byte seed and128-byte definition')
    for name,value,maximum in [('ShotId',shot_id,255),('definition address',definition_address,0xFFFFFFFF),('owner',owner,65535),('orientation',orientation,65535),('sound flag',sound_enabled,0xFFFFFFFF),('sound handle',sound_handle,0xFFFFFFFF)]:
        if type(value) is not int or not 0<=value<=maximum:raise ValueError('Invalid '+name)
    if len(position)!=3 or any(type(v) is not int or not 0<=v<=0xFFFFFFFF for v in position):raise ValueError('Expected three unsigned position bit patterns')
    node=bytearray([seed])*0x173
    def u32(o,v):struct.pack_into('<I',node,o,v&0xFFFFFFFF)
    def u16(o,v):struct.pack_into('<H',node,o,v&65535)
    # Base constructor fixture assigns only entityID; native constructor follows.
    u16(4,0x1234)
    for o in [6,8,10,12,14,16]:u16(o,0)
    u32(0,0x66D86C);struct.pack_into('<3I',node,0x1A,*position);u16(0x28,owner)
    node[0x124]=shot_id&255;u32(0x125,definition_address)
    node[0xF6:0x102]=node[0x1A:0x26];u16(0x102,orientation);u16(0xF4,orientation)
    node[0x104:0x11C]=bytes(24);u16(0x11E,struct.unpack_from('<H',definition,0x4D)[0])
    u32(0x40,struct.unpack_from('<H',definition,0x47)[0])
    for d,s in [(0x13D,0x37),(0x141,0x3B)]:u32(d,struct.unpack_from('<I',definition,s)[0])
    for o in [0x12D,0x155,0x14D,0x131,0x135,0x139,0x149,0x169,0x151,0x159,0x15D,0x161,0x165,0x16F]:u32(o,0)
    for o in [0x16D,0x145,0x147,0x129,0x12B]:u16(o,0)
    # Actual4B49D0 runs on resource subobject44 after resource attach fixture.
    u16(0x93,0x1234);node[0x92]&=0xBF
    u32(0x2E,sound_handle if sound_enabled else 0);u32(0x32,sound_handle)
    return bytes(node)
