"""Construction7 runtime-cell projection; NOT a four-byte KLD cell codec."""
import struct

DIRECTIONS=((0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1))

def construction7_mask(cells,width,height,x,y,terrain_id):
    if len(cells)!=16*width*height or not(0<=x<width and 0<=y<height):raise ValueError('Expected bounded 16-byte runtime cells')
    if type(terrain_id) is not int or not 0<=terrain_id<=255:raise ValueError('Expected byte TerrainId')
    owner=struct.unpack_from('<H',cells,16*(y*width+x)+8)[0]&0xFFF
    mask=0
    for bit,(dx,dy) in enumerate(DIRECTIONS):
        xx,yy=x+dx,y+dy
        if not(0<=xx<width and 0<=yy<height):continue
        at=16*(yy*width+xx)
        if cells[at+10]==terrain_id and struct.unpack_from('<H',cells,at+8)[0]&0xFFF==owner:mask|=1<<bit
    return mask

def project_cell(cells,width,height,x,y,terrain_id):
    out=bytearray(cells);out[16*(y*width+x)+12]=construction7_mask(cells,width,height,x,y,terrain_id)
    return bytes(out)

def queue_request(x,y,width,height):
    """Exact selected in-map signed16 region request, including X/Y asymmetry."""
    if not(0<=x<width<=32767 and 0<=y<height<=32767):raise ValueError('Expected bounded signed16 map coordinates')
    xx,yy=x-2,y-2;nx=ny=5
    if xx<=1:xx=1;nx=3
    if xx>=width:nx-=3
    if yy<1:yy=1;ny=3
    if yy>=height:ny-=3
    return struct.pack('<4I',xx|(yy<<16),nx|(ny<<16),0,6)

def patch_room_construction(raw,value):
    if len(raw)!=1055 or type(value) is not int or not 0<=value<=255:raise ValueError('Expected 1055-byte Room and byte value')
    out=bytearray(raw);out[0x395]=value;return bytes(out)
