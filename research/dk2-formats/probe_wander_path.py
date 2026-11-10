"""State87 with original native AI tables and full32x32 NAV initialization."""
from pathlib import Path
import json,struct
import probe_wander_target as target
from emulate_element_colors import region,pack32
from spell_state_tables import BASE as STATEBASE,initialized_states
from creature_category_tables import BASE as CATBASE,initialized_categories

ROOT=Path(__file__).resolve().parent
TRIANGLES=0x10B10000
NTRIANGLES=2048
TRIANGLE_BYTES=4+29*NTRIANGLES
NAVGLOBALS,NAVSIZE=0x6E5AC0,0x7520
cpu=target.cpu;f=target.f;cf=target.cf

def build(c):
    args,_=target.build(dict(c,callback=True))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    actor=mem[cf.ACTOR]
    for o,v,n in [(0x256,cf.ACTOR,4),(0x25A,f.WORLD,4),(0x25E,87,4),(0x26A,0,4),(0x3B9,0,2)]:f.put(actor,o,v,n)
    args['registers']=json.dumps(dict(ECX=hex(cf.ACTOR+0x24E)))
    base,image=cpu.pe_image(target.EXE.read_bytes());globals_=bytearray(image[NAVGLOBALS-base:NAVGLOBALS-base+NAVSIZE])
    for a in list(mem):
        if NAVGLOBALS<=a<NAVGLOBALS+NAVSIZE:
            b=mem.pop(a);globals_[a-NAVGLOBALS:a-NAVGLOBALS+len(b)]=b
    mem[NAVGLOBALS]=globals_
    mem[TRIANGLES]=bytearray(TRIANGLE_BYTES)+bytes([165])*64
    mem[STATEBASE]=bytearray([165])*len(initialized_states()[0])
    mem[CATBASE]=bytearray(target.pe_read(target.EXE.read_bytes(),CATBASE,len(initialized_categories()[0])))
    args['before_calls']=[dict(address='0x4798f0',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x47bbe0',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x4ce690',registers=dict(ECX='0'),stack_args=[TRIANGLES,TRIANGLE_BYTES]),
        dict(address='0x4ce740',registers=dict(ECX='0'),stack_args=[target.source.nb.BITS,32,32,32]),
        dict(address='0x4c8f30',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x4cef20',registers=dict(ECX='0'),stack_args=[])]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]));args['max_steps']=4000000
    args['observe_writes'] += [[TRIANGLES,TRIANGLE_BYTES+64],[NAVGLOBALS,NAVSIZE]]
    checks={a:bytes(b) for a,b in mem.items() if a!=0x7FFEF000}
    for a,n in [(0x66E3EC,0x3A0),(0x66D524,0x130)]:checks[a]=target.pe_read(target.EXE.read_bytes(),a,n)
    return args,checks

def execute(c,verbose=True):
    args,checks=build(c);tag=cpu.sha((json.dumps(args,sort_keys=True)+cpu.sha((ROOT/'native_unicorn_wandertrace.py').read_bytes())+json.dumps({hex(a):cpu.sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase144-wander-path-native-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(target.EXE,args,list(checks.items()),target.STOP,cf.ACTOR,len(checks[cf.ACTOR]));saved=dict(case=c,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if verbose:print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],registers=r['registers'],tail=r['trace_tail'],before_calls=r['before_calls'])),flush=True)
    return args,checks,saved,path

if __name__=='__main__':execute(dict(name='open-native-nav',seed=1,radius_seed=1))
