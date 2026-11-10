"""Bounded float32 projection of original59F4B0 Lean interpolation.

Inputs are its complete27point delta buffer,16DWORD occupancy and27derived
samples, not raw Terrain bytes. Caller-derived samples include baseline wave.
The raw→sample link is a separate native-loader proof. No original source writes.
"""
import struct

def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
CORNER_FACTOR=f32(1.4)


def corner(h,v,a,b,c,d):
    x=y=z=0.0;ab=a|b
    if (d|b)!=(c|a):
        if (d|c)!=ab:h=f32(h*CORNER_FACTOR)
        x=f32(x+(h if (c|a)==0 else -h));z=f32(z+v);v=0.0
    if (d|c)!=ab:y=f32(y+(h if ab==0 else -h));z=f32(z+v)
    return [x,y,z]


def edge(h,v,a,b,axis):
    out=[0.0,0.0,0.0]
    if a!=b:out[axis]=f32(out[axis]+(h if a==0 else -h));out[2]=f32(out[2]+v)
    return out


def project(initial,occupancy,samples):
    if len(initial)!=324 or len(occupancy)!=64 or len(samples)!=540:raise ValueError('Expected324/64/540byte buffers')
    out=[list(v) for v in struct.iter_unpack('<3f',initial)]
    occ=list(struct.unpack('<16I',occupancy));ss=list(struct.iter_unpack('<5f',samples))
    if any(x not in [0,1] for x in occ):raise ValueError('Projection covers Boolean occupancy only')
    temp=[]
    for z in range(3):
        rows=ss[z*9:z*9+9];plane=[[0.0]*3 for _ in range(9)]
        for index,indices in [(0,[0,1,4,5]),(2,[1,2,5,6]),(6,[4,5,8,9]),(8,[5,6,9,10])]:
            plane[index]=corner(rows[index][2],-rows[index][3],*[occ[i] for i in indices])
        for index,a,b,axis in [(1,1,5,1),(3,4,5,0),(5,5,6,0),(7,5,9,1)]:
            plane[index]=edge(rows[index][2],-rows[index][3],occ[a],occ[b],axis)
        # Native constant is -0.5; FSUBR therefore ADDS the corner average.
        for index,axis,a,b in [(1,0,2,0),(3,1,0,6),(5,1,8,2),(7,0,8,6)]:
            plane[index][axis]=f32(plane[index][axis]+(plane[a][axis]+plane[b][axis])*0.5)
        for axis in [0,1]:plane[4][axis]=f32(sum(plane[i][axis] for i in [5,3,1,7])*0.25)
        temp+=plane
    for i in range(27):
        for axis in range(3):out[i][axis]=f32(out[i][axis]+temp[i][axis])
    for i in range(9):out[i][2]=f32(out[i][2]+ss[i][4])
    return b''.join(struct.pack('<3f',*v) for v in out),b''.join(struct.pack('<3f',*v) for v in temp)
