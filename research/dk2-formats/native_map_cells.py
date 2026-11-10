"""Lossless physical map cells and explicit native writer projections."""
import struct
def checked(v,bits=8):
    if type(v) is not int or not 0<=v<1<<bits:raise ValueError(f'Expected unsigned{bits} integer')
    return v
def decode_cell(cell):
    if not isinstance(cell,(bytes,bytearray)) or len(cell)!=4:raise ValueError('Expected four cell bytes')
    return dict(terrain_id=cell[0],owner_id=cell[1],native_flags=cell[2],opaque_fourth=cell[3],game_substrate_bit1000=bool(cell[2]&2))
def patch_cell(cell,offset,value):
    decode_cell(cell);checked(value)
    if type(offset) is not int or offset not in range(4):raise ValueError('Invalid cell byte offset')
    return bytes(cell[:offset])+bytes([value])+bytes(cell[offset+1:])
def intermediate_cell(cell,initial_flags=0):
    decode_cell(cell);checked(initial_flags)
    return struct.pack('<III4B',1,1,1,cell[0],cell[1],initial_flags|cell[2],0)
def runtime_substrate_flags(old_flags,cell_flags):
    checked(old_flags,16);checked(cell_flags)
    return (old_flags&0xEFFF)|(0x1000 if cell_flags&2 else 0)
def writer_cells(width,height,terrain,owners,initial_flags=0,opaque_fourth=0,guard=0):
    """Explicit stack-byte seeds and uniform exterior guard for native unchecked reads.

    Native ±1 uses linear memory even across row edges; no bounds/clamping added.
    Caller supplies ordinary packed terrain and owner arrays, not tile objects.
    """
    checked(width,32);checked(height,32);checked(initial_flags);checked(opaque_fourth);checked(guard)
    if len(terrain)!=width*height or len(owners)!=len(terrain):raise ValueError('Dimensions/array lengths disagree')
    output=bytearray();flags=initial_flags
    def at(i):return terrain[i] if 0<=i<len(terrain) else guard
    for i,t in enumerate(terrain):
        if t not in [17,26,36]:flags=1
        else:
            values=[at(i+1),at(i-1),at(i+width),at(i-width)]
            if 4 in values:flags=1
            if 5 in values:flags=2
        output+=bytes([t,owners[i],flags,opaque_fourth])
    return bytes(output)
