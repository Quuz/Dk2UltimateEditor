"""Discovery: original RenderInfo->nativeheap->dynamicprimitive->primarypick.

RenderInfo/cache/geometry inputs explicit. No allocator/constructor replacement.
Not published until native inputs/outputs/integrity and primary bytes checked.
"""
from pathlib import Path
import json,struct,sys
import emulate_primary_pick as parent
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
put,sha=parent.put,parent.sha
POOL,GLOBALS,TABLE,ENTRY0,XYZ0,ANGLES,ENGINE=0x10700000,0x765B08,0x10710000,0x10720000,0x10730000,0x10740000,0x10741000
def build(c):
    args,checks=parent.build(c);mem={a:bytearray(b) for a,b in checks.items()};mem[0x7FFEE000]=bytearray([165])*0x4000
    heap=bytearray(0x400);put(heap,4,30);put(heap,0x98+30*4,POOL);mem[GLOBALS]=heap
    pool=bytearray([165])*(0x180+64)
    for i in range(2):
        at=i*0xC0;put(pool,at,POOL+(i+1)*0xC0 if i==0 else 0);put(pool,at+4,POOL if i else 0);put(pool,at+8,0);put(pool,at+12,0xC0);put(pool,at+16,30,2);pool[at+18]=0;pool[at+19]=1
    mem[POOL]=pool;table=bytearray(24);mem[TABLE]=table;mem[0x766A6C]=bytearray(struct.pack('<I',TABLE))
    # Merge model resource-map global into the existing prepared cache block.
    v=mem.pop(0x766A6C);mem[parent.CACHE_GLOBALS][0x766A6C-parent.CACHE_GLOBALS:0x766A70-parent.CACHE_GLOBALS]=v
    mem[ANGLES]=bytearray(4);mem[ENGINE]=bytearray(8);mem[0x759ACC]=bytearray(4);mem[0x760B7C]=bytearray(struct.pack('<f',0.5))
    calls=[];tokens=[]
    for i in range(2):
        old=parent.BASE+parent.STEP*i;mem.pop(old);actor=parent.prior.ACTORS[i];ri=actor+0x44;entry=ENTRY0+0x100*i;xyz=XYZ0+0x100*i
        b=mem[actor];off=0x44
        for o,v,n in [(4,c.get('width',4096),4),(8,c.get('height',4096),4),(0x20,0,2),(0x24,0,2),(0x26,0,2),(0x28,entry,4),(0x48,255,1),(0x49,255,1),(0x4A,255,1),(0x4B,c.get('alpha',255),1),(0x5B,c.get('flags',0),4),(0xAC,c.get('bank',0),1)]:put(b,off+o,v,n)
        for j,v in enumerate(c.get('offset',[0,0,0])):put(b,off+0x51+2*j,v,2)
        en=bytearray(27);put(en,15,i);mem[entry]=en
        handle=parent.BASE+parent.STEP*i+0x1000;put(table,12*i+4,handle)
        mem[xyz]=bytearray(struct.pack('<3i',0,0,-4096*c.get('depths',[2,1])[i]))
        primitive=POOL+i*0xC0+20;tokens.append(primitive);put(mem[parent.REGISTRY],i*16+12,primitive)
        calls.append(dict(address='0x59ccf0',registers=dict(ECX=hex(ENGINE)),stack_args=[ri,xyz,xyz,ANGLES,ANGLES,actor]))
    if c.get('stage','pick')=='creation':
        args['before_calls']+=calls[:-1];args['address']=calls[-1]['address'];args['registers']=json.dumps(calls[-1]['registers']);args['stack_args']=calls[-1]['stack_args']
    else:args['before_calls']+=calls
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]));args['max_steps']=2000000
    args['observe_reads'] += [[POOL,len(pool)],[GLOBALS,len(heap)],[TABLE,len(table)],[ENTRY0,0x200]]
    args['observe_writes'] += [[POOL,len(pool)],[GLOBALS,len(heap)]]
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,{a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000},tokens
def execute(c):
    args,checks,tokens=build(c);backend=sha(Path(parent.cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];file=ROOT/'ghidra'/f'phase155-renderinfo-discovery-{tag}.json'
    if file.exists():d=json.loads(file.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=parent.cpu.run(parent.EXE,args,list(checks.items()),parent.prior.p.OBS,parent.FLAG,4));file.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,tokens,d,file
if __name__=='__main__':
    a,ch,t,d,p=execute(dict(name='base'));r=d['result'];print(json.dumps({k:r[k] for k in ['success','emulation_error','registers','before_calls','trace_tail']}));print('CHANGES',[(x['address'],len(x['differences'])) for x in r['context_mismatches']])
    if r['success']:
        post=post_context(ch,r)
        for at in t:print('PRIM',hex(at),post[POOL][at-POOL:at-POOL+141].hex())
