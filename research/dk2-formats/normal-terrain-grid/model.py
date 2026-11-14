"""Independent bounded 20-point normal terrain projection; uniform Terrain fixture.
Normal height mode; raw grid is renderer coordinates (Z down), not editor Z.
"""
import struct,math
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def rand(mod,seed):
 seed=(seed*0x19660d+0x3c6ef35f)&0xffffffff
 return (((seed>>13)|(seed<<19))&0xffffffff)%mod,seed

def grid(x,y,occupied,h,v,wh=0,wv=0):
 import native_terrain_lean as lean
 samples=[]
 for z in range(3):
  for iy in range(3):
   for ix in range(3):
    wave=f32(-.15-.125*math.sin((2*x+ix+2*y+iy)*.5)) if z==0 else 0
    samples.append(struct.pack('<5f',0,0,h[z]/512,v[z]/512,wave))
 shape,_=lean.project(bytes(324),struct.pack('<16I',*occupied),b''.join(samples))
 delta=[list(p) for p in struct.iter_unpack('<3f',shape)]
 for z in range(3):
  for iy in range(2):
   for ix in range(2):
    seed=(2*x+ix+65536*(2*y+iy)+16*z)&0xffffffff
    nz=ny=nx=0
    if wv:nz,seed=rand(wv<<7,seed)
    if wh:
     ny,seed=rand(wh<<7,seed);ny-=wh<<6
     nx,seed=rand(wh<<7,seed);nx-=wh<<6
    p=delta[z*9+iy*3+ix]
    p[0]=f32(p[0]+nx/131072)
    p[1]=f32(p[1]+ny/131072)
    p[2]=f32(p[2]+nz/131072)
  # Native Wibble adds an occupancy-dependent inward horizontal bias.
  a,b,c,d=[occupied[i] for i in [0,1,4,5]]
  bias=f32((wh<<7)/131072*.5)
  corner=lean.corner(bias,0,a,b,c,d)
  for axis in [0,1]:delta[z*9][axis]=f32(delta[z*9][axis]+corner[axis])
  edge=lean.edge(bias,0,occupied[1],occupied[5],1)
  delta[z*9+1][1]=f32(delta[z*9+1][1]+edge[1])
  edge=lean.edge(bias,0,occupied[4],occupied[5],0)
  delta[z*9+3][0]=f32(delta[z*9+3][0]+edge[0])
 result=[]
 for ix in range(2):
  for iy in range(2):
   for z in range(5):
    p=[x+ix*.5,y+iy*.5,-1.75+z*.25]
    if 1<=z<=3:
     correction=delta[(z-1)*9+iy*3+ix]
     p=[f32(a+b) for a,b in zip(p,correction)]
    result.append(p)
 return b''.join(struct.pack('<3f',*p) for p in result)

if __name__=='__main__':
 import sys,json,argparse
 from pathlib import Path
 ap=argparse.ArgumentParser()
 ap.add_argument('--research-root',type=Path,default=Path(__file__).resolve().parents[1])
 ap.add_argument('--cases',type=Path,default=Path(__file__).with_name('cases.json'))
 cli=ap.parse_args()
 sys.path.insert(0,str(cli.research_root))
 import emulate_terrain_lean as setup
 path=cli.cases
 report=json.loads(path.read_text(encoding='utf8'))
 for row in report['cases']:
  mode=row['mode']
  case=dict(topology=row['topology'],nav_block=8)
  expected=grid(2,3,setup.occupancy_grid(case),[128 if mode in ['H','HV'] else 0]*3,
      [128 if mode in ['V','HV'] else 0]*3,128 if mode=='W' else 0,128 if mode=='W' else 0)
  actual=bytes.fromhex(row['grid_hex'])
  if actual!=expected:
   for i,(a,b) in enumerate(zip(struct.iter_unpack('<3f',actual),struct.iter_unpack('<3f',expected))):
    if a!=b:print(row['name'],i,'native',a,'model',b)
   raise AssertionError(row['name'])
 print('Independent model matched all 30 native grids: 600 points / 7200 bytes')

