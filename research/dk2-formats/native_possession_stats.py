"""Physical type224 records and the verified final DX speed-modifier stage.

Other stat IDs are preserved; their gameplay effects are not implied by names.
"""
import struct
DX='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
def check(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw)!=16 or struct.unpack_from('<I',raw)[0]!=224:raise ValueError('Expected complete16-byte type224 record')
def view(raw):
    check(raw);_,index,value,ignored=struct.unpack('<4I',raw);word=value&65535
    return dict(stat_id_bits=index,value_bits=value,loaded_signed16=word if word<32768 else word-65536,ignored_bits=ignored,
      writer_address=(0x6f2863+2*index)&0xffffffff,writer_index_in_array=index<32,
      known_consumer='Final speed modifier for every movement mode' if index==21 else None)
def patch_value_bits(raw,value):
    check(raw)
    if type(value) is not int or not 0<=value<=0xffffffff:raise ValueError('Expected unsigned32 physical value')
    out=bytearray(raw);struct.pack_into('<I',out,8,value);return bytes(out)
def speed_modifier(base,value_bits,possessed=False,secondary_flag=False):
    if type(base) is not int or not 0<=base<=0xffffffff or type(value_bits) is not int or not 0<=value_bits<=0xffffffff:raise ValueError('Expected unsigned32 inputs')
    if type(possessed) is not bool or type(secondary_flag) is not bool:raise ValueError('Expected boolean state flags')
    p=value_bits&65535;p=p if p<32768 else p-65536
    if possessed:return ((base*(100+p))&0xffffffff)//100
    if secondary_flag:return ((base*(100+p))&0xffffffff)//200
    return base
