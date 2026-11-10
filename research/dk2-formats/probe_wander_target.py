"""Original state87/helper480A00 with real CWorld/CMap queries and native NAV.

Reuse the initialized phase98 runtime scene, not its driver or simulation. No
game function/vtable patch. Empty room list; explicit owner grid and RNG seeds.
"""
from pathlib import Path
import json,struct,sys
import emulate_navigation_step as source
import native_unicorn_wandertrace as cpu
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
CTX,TARGET,STOP=0x10B00000,0x10B01000,0x10B02000
f=source.f;cf=source.cf

def build(c):
    sc=dict(source.cases()[0],state='normal',position=c.get('position',[0xA800,0xA800,0x2000]),velocity=[0,0])
    args,_=source.build(sc)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions'] if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [f.OBS,source.nb.DRIVER]}
    actor=mem[cf.ACTOR];world=mem[f.WORLD]
    source.f.put(actor,0x28,1,2);source.f.put(actor,0x2A,f.PLAYER)
    source.f.put(actor,0x3A9,c.get('current_action',0));source.f.put(actor,0x3E6,c.get('actor_flags',0))
    actor[0x418]=c.get('wander',0);source.f.put(mem[f.PLAYER],0x36,0,2)
    # Decompiled pointer arithmetic World+568 scales by4: native RNG is15A0.
    source.f.put(world,0x15A0,c.get('seed',1))
    for y in range(32):
        for x in range(32):
            at=16*(y*32+x);mem[f.MAP][at:at+16]=bytes(16)
            source.f.put(mem[f.MAP],at+8,c.get('owner',1),2);mem[f.MAP][at+10]=1
    at=16*((sc['position'][1]>>12)*32+(sc['position'][0]>>12));source.f.put(mem[f.MAP],at,100,2)
    mem[source.nb.BITS][:]=bytes([8 if c.get('blocked',False) else 0])*1024
    for x,y,flag in c.get('nav',[]):mem[source.nb.BITS][y*32+x]=flag
    mem[0x6DA490]=bytearray(pack32(32));mem[0x6DA49C]=bytearray(pack32(f.MAP))
    ctx=bytearray(256);source.f.put(ctx,8,cf.ACTOR);source.f.put(ctx,12,f.WORLD)
    mem[CTX]=ctx;mem[TARGET]=bytearray([165])*76
    stack=bytearray([165])*8192
    if c.get('callback',False):entry=0x4808E0;ecx=CTX;words=[STOP]
    else:entry=0x480A00;ecx=cf.ACTOR;words=[STOP,TARGET,c.get('radius',4)]
    struct.pack_into('<'+'I'*len(words),stack,4096,*words);mem[0x7FFEF000]=stack
    args=dict(program='DKII-DX.exe',address=hex(entry),registers=json.dumps(dict(ECX=hex(ecx))),
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=2000000,
        observe_reads=[[cf.ACTOR,len(actor)],[f.MAP,len(mem[f.MAP])],[0,4096]],
        observe_writes=[[cf.ACTOR,len(actor)],[TARGET,76],[0,4096]])
    checks={a:bytes(b) for a,b in mem.items() if a!=0x7FFEF000}
    for a,n in [(0x66E3EC,0x3A0),(0x66D524,0x130)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    return args,checks

def execute(c,verbose=True):
    args,checks=build(c);tag=cpu.sha((json.dumps(args,sort_keys=True)+cpu.sha((ROOT/'native_unicorn_wandertrace.py').read_bytes())+json.dumps({hex(a):cpu.sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase144-wander-target-native-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(EXE,args,list(checks.items()),STOP,TARGET,76);saved=dict(case=c,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if verbose:print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],registers=r['registers'],tail=r['trace_tail'],target=r['captured_hex'][:24])),flush=True)
    return args,checks,saved,path

if __name__=='__main__':execute(dict(name='callback-open',callback='callback' in sys.argv,blocked='blocked' in sys.argv))
