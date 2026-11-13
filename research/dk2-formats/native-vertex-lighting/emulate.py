from pathlib import Path
import sys, json, struct, math, hashlib, argparse
ap=argparse.ArgumentParser(description="Replay original DKII-DX vertex-light routines without launching Windows game")
ap.add_argument('--exe',type=Path,required=True)
ap.add_argument('--research-root',type=Path,default=Path(__file__).resolve().parents[1])
ap.add_argument('--output',type=Path,required=True)
cli=ap.parse_args()
exe=cli.exe.resolve();root=cli.research_root.resolve();out=cli.output.resolve()
expected=json.loads((Path(__file__).parent/'cases.json').read_text())['source_sha256']
before=hashlib.sha256(exe.read_bytes()).hexdigest()
if before!=expected:raise SystemExit('Unsupported executable fingerprint')
sys.path.insert(0,str(root))
from native_unicorn_factorytrace import run
out.mkdir(parents=True,exist_ok=True)
L,P,N,O=0x18000000,0x18001000,0x18002000,0x18003000
pack=lambda *v:struct.pack('<'+'f'*len(v),*v)
f32=lambda v:struct.unpack('<f',pack(v))[0]
rows=[]
for distance in [0,.125,.25,.5,.75,1,1.25,1.49,1.5,1.75]:
 for intensity in [0,64,128,255]:
  for facing in [1,-1]:
   # List count followed by the exact 40-byte lighting descriptor prepared by 57BCB0.
   data=struct.pack('<I',1)+pack(0,0,0,2.25,1/2.25,0,intensity/128,64,128,255)
   regions=[(L,data),(P,pack(0,0,-distance)),(N,pack(0,0,facing)),(O,bytes(12)),(0x7ffef000,bytes(8192))]
   args=dict(address='0x58f480',registers='{}',memory=json.dumps(dict(regions=[dict(address=hex(a),hex=b.hex()) for a,b in regions])),
    before_calls=[dict(address='0x58f480'),dict(address='0x57bf00',registers=dict(ECX=hex(L)),stack_args=[O,P,N])],
    observe_reads=[[0,4096]],max_steps=20000)
   r=run(exe,args,[],0x4aa310,O,12)
   assert r['success'] and all(c['returned'] for c in r['before_calls']) and not r['observed_reads'],r
   rgb=struct.unpack('<3f',bytes.fromhex(r['captured_hex']))
   d=f32(distance); d2=d*d
   index=struct.unpack('<I',pack(12582912-(f32(.49999)-16*d2)))[0]&0x7fffff
   index-=0x400000
   table=f32(.25+16/index) if index else float('inf')
   dot=d*facing*intensity/128
   weight=dot*table*(2.25-d2)*f32(1/2.25)
   negative_dot = facing < 0 and d > 0
   expected=0 if d2>=2.25 or negative_dot else (1 if math.isnan(weight) else min(1,weight))
   assert all(abs(c-v*expected)<.0002 for c,v in zip(rgb,[64,128,255])),(distance,intensity,facing,rgb,expected,index)
   rows.append(dict(distance=distance,intensity=intensity,facing=facing,table_index=index,weight=expected,rgb=rgb,instructions=r['instruction_count']))
report=dict(scope='Original 58F480 table initialization and 57BF00 vertex-light accumulation, prepared light descriptor and vertex; no Windows game or final rasterization',
 source_sha256=hashlib.sha256(exe.read_bytes()).hexdigest(),cases=rows)
(out/'vertex-light-cases.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(f'{len(rows)} native vertex-light cases passed; original reciprocal-distance table, radius cutoff, intensity and facing')

assert hashlib.sha256(exe.read_bytes()).hexdigest()==before,"Input EXE changed"
