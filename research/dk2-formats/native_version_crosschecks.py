"""Exact-EXE CreatureSpell loader and AP runtime-state views.

AP runtime-state spans are NOT the C5 map record layout.
"""
import struct
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
OLD='93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342'
PROFILES={DX:dict(program='DKII-DX.exe',spell_loader=0x4F7860,spell_converter=0x56A560,spell_init=0x56A490,tps_global=0x6F2590,checksum_gate=0x6FCDCF,ap_state_writer=0x4B0460,ap_state_reader=0x4B0550),
          OLD:dict(program='DKII-local13.exe',spell_loader=0x4EC440,spell_converter=0x5608A0,spell_init=0x5607D0,tps_global=0x6DD618,checksum_gate=None,ap_state_writer=0x4A6FB0,ap_state_reader=0x4A70A0)}
def profile(sha):
    if not isinstance(sha,str) or sha not in PROFILES:raise ValueError('Expected exact inspected executable SHA256')
    return PROFILES[sha]
def spell_projection(mid,previous,tps,sha,gate=0):
    from native_spell_alternatives import runtime_projection
    p=profile(sha)
    if type(gate) is not int or not 0<=gate<=0xFFFFFFFF:raise ValueError('Expected DWORD gate')
    return runtime_projection(mid,previous,tps,gate if p['checksum_gate'] is not None else 0)
def ap_state_segments(node,sha):
    profile(sha)
    if not isinstance(node,(bytes,bytearray)) or len(node)!=64:raise ValueError('Expected64-byte runtime AP')
    return [bytes(node[0x37:0x38]),bytes(node[0x38:0x3C]),bytes(node[0x3C:0x40]),bytes(node[0x2E:0x37])]
def ap_loaded_local_state(previous,state9,sha):
    ap_state_segments(previous,sha)
    if not isinstance(state9,(bytes,bytearray)) or len(state9)!=9:raise ValueError('Expected9-byte AP local runtime state')
    out=bytearray(previous);out[0x2E:0x37]=state9;return bytes(out)
