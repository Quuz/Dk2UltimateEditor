"""Explicit native Effects save projections; preserve raw by default."""
FLAG_MASK=0xFFF7
FLAG_MODEL_BITS=[0,1,2,4,5,6,7,8,9,10,11,12,13,14,15]
def checked(value,bits):
    if type(value) is not int or not 0<=value<1<<bits:raise ValueError(f'Expected unsigned{bits} integer')
    return value
def flag_encode_decode(raw_flags):return checked(raw_flags,32)&FLAG_MASK
def native_loaded_flags(raw_flags):
    value=flag_encode_decode(raw_flags)
    return value if value&0xC0 else value|0x80
def tail_update(previous,incoming):
    if len(previous)!=12 or len(incoming)!=12:raise ValueError('Expected twelve WORD values')
    for v in [*previous,*incoming]:checked(v,16)
    return [n if n else old for old,n in zip(previous,incoming)]
def record_view(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=246:raise ValueError('Expected246-byte Effects record')
    flags=int.from_bytes(record[0xB4:0xB8],'little')
    return dict(raw_flags=flags,native_loaded_saved_flags=native_loaded_flags(flags),
        tail_words=[int.from_bytes(record[o:o+2],'little') for o in range(0xDA,0xF2,2)],
        opaque_f4=int.from_bytes(record[0xF4:0xF6],'little'),
        native_writer_f4='not assigned by inspected writer; depends on incoming stack contents for ordinary <=32-byte names')
def patch_raw(record,offset,value):
    record_view(record)
    if offset==0xB4:width=4
    elif offset==0xF4 or offset in range(0xDA,0xF2,2):width=2
    else:raise ValueError('Unsupported patch offset')
    checked(value,width*8)
    return bytes(record[:offset])+value.to_bytes(width,'little')+bytes(record[offset+width:])
