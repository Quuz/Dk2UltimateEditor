"""Whole raw Effects/Element loads followed by original CUBE_GEN and child creation.

The parent is a prepared effect instance, not a native factory result. Resource
lookup and connected-engine queries retain the phase140 explicit providers.
The effect loader uses a null graphics provider. No original code is replaced.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import probe_raw_element_palette as element
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
base=element.base;cpu=element.cpu
ERAW,EMID,POOL=0x10200000,0x10210000,0x10400000
POOL_SIZE=0x20000

def build(c):
    args,_=element.build(ident=294,spawn=False,render=False)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Effects.kwd'
    raw=bytearray(next(r for s in fmt.parse(source.read_bytes()) if s.signature==270 for r in s.records if int.from_bytes(r[0xB8:0xBA],'little')==1))
    # Controlled diagnostics start with an original raw record and patch only
    # explicitly listed fields. These are memory copies, never game-file edits.
    struct.pack_into('<I',raw,0xB4,c.get('flags',0x4000))
    struct.pack_into('<8H',raw,0xCA,*c.get('slots',[294,0,0,0,0,0,0,0]))
    raw[0xDA:0xF2]=bytes(24)
    struct.pack_into('<H',raw,0xEC,c.get('randomness',0))
    struct.pack_into('<2H',raw,0xEE,*c.get('misc',[0,0]))
    raw[0xF2:0xF4]=bytes([4,c.get('count',6)])
    mem[ERAW]=raw+bytes([165])*64;mem[EMID]=bytearray([165])*(426+64)
    mem[base.PARENT_DEF]=bytearray([165])*(148+64)
    parent=mem[base.PARENT]
    base.put(parent,0x10B,c.get('life',100));base.put(parent,0x113,0)
    parent[0xD6:0xE2]=mem[base.ARGS][:12]
    parent[0x117:0x11F]=mem[base.ARGS][:8]
    base.put(parent,0xE2,0,2)
    # Larger creation-only free list with exactly the original allocator's
    # header/sentinel convention, enough for the bounded controls.
    mem.pop(base.BLOCK)
    pool=bytearray([165])*(POOL_SIZE+64)
    struct.pack_into('<III',pool,0,POOL_SIZE,base.FH,base.FT)
    mem[POOL]=pool
    base.put(mem[base.FH],8,POOL);base.put(mem[base.FT],4,POOL)
    mem[0x69752C]=bytearray([c.get('scale',255)])
    calls=args['before_calls']+[
        dict(address='0x4357e0',registers=dict(ECX=hex(base.PARENT_DEF)),stack_args=[]),
        dict(address='0x56aec0',registers=dict(ECX=hex(EMID)),stack_args=[]),
        dict(address='0x56afe0',registers=dict(ECX=hex(EMID)),stack_args=[ERAW]),
        dict(address='0x4358c0',registers=dict(ECX=hex(base.PARENT_DEF)),stack_args=[EMID,0]),
        dict(address='0x4386f0',registers=dict(ECX=hex(base.PARENT)),stack_args=[base.PARENT_DEF,1])]
    args.update(before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),
        max_steps=1500000,observe_reads=[[ERAW,246],[EMID,426],[base.PARENT_DEF,148],[0x69752C,1],[0,4096]],
        observe_writes=[[POOL,POOL_SIZE+64],[base.PARENT_DEF,212],[0,4096]])
    checks={a:bytes(b) for a,b in mem.items() if a not in [base.ALLOW,base.BLOCKED,base.TOUCH,base.FLOOR,element.GCB,0x7FFEF000]}
    for a,n in [(0x66CC94,0x164),(0x66CBDC,0x20),(0x66D0C4,0x40)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    return args,checks

def execute(c,verbose=True):
    args,checks=build(c)
    tag=cpu.sha((json.dumps(args,sort_keys=True)+cpu.sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())+json.dumps({hex(a):cpu.sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase141-effect-scale-native-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),element.STOP,base.PARENT,345)
        saved=dict(case=c,input=args,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if verbose:print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],tail=r['trace_tail'],calls=r['before_calls'],creations=r['visited_instructions'].get('0x4352e0',0)),ensure_ascii=False),flush=True)
    return args,checks,saved,path

if __name__=='__main__':execute(dict(name='scale-127-count6',scale=127,count=6))
