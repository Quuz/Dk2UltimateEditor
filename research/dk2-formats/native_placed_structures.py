"""Native placed Trap/Door projections, not full world/entity initialization."""
SIZES={195:16,196:20}
def checked(v,bits):
    if type(v) is not int or not 0<=v<1<<bits:raise ValueError(f'Expected unsigned{bits} integer')
    return v
def trap_placement(raw_shots):
    checked(raw_shots,8)
    return dict(activate=bool(raw_shots),queue_manufacture=not bool(raw_shots),quantity_forwarded=False)
def door_placement(raw_flags,initial_state=0):
    checked(raw_flags,8);checked(initial_state,32)
    blueprint=(raw_flags>>1)&1;request_lock=bool(raw_flags&1)
    return dict(blueprint=blueprint,lock_requested=request_lock,lock_state_set=request_lock and not blueprint,
        projected_state=3 if request_lock and not blueprint else initial_state,queue_manufacture=bool(blueprint),editor_flags=raw_flags&3)
def center_q12(raw_tile_bits):
    checked(raw_tile_bits,32)
    return ((raw_tile_bits<<12)+0x800)&0xFFFFFFFF
def record_view(envelope):
    if not isinstance(envelope,(bytes,bytearray)) or len(envelope)<8:raise ValueError('Expected Things envelope')
    tag=int.from_bytes(envelope[:4],'little');n=int.from_bytes(envelope[4:8],'little')
    if tag not in SIZES or n!=SIZES[tag] or len(envelope)!=n+8:raise ValueError('Expected Trap16 orDoor20 payload')
    p=envelope[8:];result=dict(tag=tag,x_bits=int.from_bytes(p[:4],'little'),y_bits=int.from_bytes(p[4:8],'little'),opaque_position_word=int.from_bytes(p[8:12],'little'))
    result['position_q12']=[center_q12(result['x_bits']),center_q12(result['y_bits']),0]
    if tag==195:result.update(raw_shots=p[12],trap_id=p[13],player_id=p[14],opaque_flags=p[15],native=trap_placement(p[12]))
    else:result.update(trigger_id=int.from_bytes(p[12:14],'little'),door_id=p[14],player_id=p[15],raw_flags=p[16],opaque_tail=p[17:20].hex(),native=door_placement(p[16]))
    return result
def patch_raw(envelope,payload_offset,value):
    v=record_view(envelope);allowed={195:{8:4,12:1,15:1},196:{8:4,16:1}}
    if payload_offset not in allowed[v['tag']]:raise ValueError('Unsupported raw field')
    width=allowed[v['tag']][payload_offset];checked(value,width*8);off=8+payload_offset
    return bytes(envelope[:off])+value.to_bytes(width,'little')+bytes(envelope[off+width:])
