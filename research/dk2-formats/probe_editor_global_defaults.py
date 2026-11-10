"""Investigate whole original numerical-default initializer, not Terrain model."""
from pathlib import Path
import json,struct
import emulate_terrain_text_stat as old
import native_editor_text_executor as executor
ROOT=Path(__file__).resolve().parent
CTX=0x54F550

def build():
    f=old.p.build(dict(name='global-default-discovery',text='0'))
    mem={int(v['address'],16):bytes.fromhex(v['hex']) for v in f['memory']}
    mem[CTX+0x6A0]=bytes(0x24)
    f['memory']=[old.engine.region(a,b) for a,b in sorted(mem.items())]
    f['calls']=[dict(address=0x47FFB0,ecx=CTX,stack=[])]
    return f

def main():
    f=build();backend=old.sha(Path(executor.__file__).read_bytes());tag=old.sha(json.dumps(f,sort_keys=True).encode()+backend.encode())[:12]
    path=ROOT/'ghidra'/f'phase166-global-defaults-{tag}.json'
    if path.exists():
        d=json.loads(path.read_text(encoding='utf-8'));assert d['input']==f and d['backend_sha256']==backend
    else:
        d=dict(input=f,backend_sha256=backend,result=executor.run(f));path.write_text(json.dumps(d,indent=2),encoding='utf-8')
    r=d['result'];print(path.name,{k:r[k] for k in ['success','error','failures','calls']})
    if not r['success']:return
    post={int(v['address'],16):bytes.fromhex(v['hex']) for v in r['output']}
    p,n,cap,grow,unused,ids,count,ids_cap,ids_grow=struct.unpack('<9I',post[CTX+0x6A0]);heap=post[old.engine.HEAP]
    print('ARRAYS',[hex(x) for x in [p,n,cap,grow,unused,ids,count,ids_cap,ids_grow]])
    print('DOUBLE100',struct.unpack_from('<d',heap,p-old.engine.HEAP+100*8)[0])
    print('LOW',[r['low_reads'],r['low_writes']])

if __name__=='__main__':main()
