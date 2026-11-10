"""Whole mode18 -> original CBridge wrappers/factory -> native Effect instances.

Parent/world connection prepared; child raw loaders and full native creation run.
Explicit connected-engine providers: visible, valid position, tile flags20,
floor0 and declined light request. No original code/vtable substitutions.
"""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import probe_effect_generation_scale as scaffold
from emulate_element_colors import region,pack32
from build_effect_semantics import EXE,pe_read
from emulate_resistance import post_context
import native_unicorn_effecttrace as cpu

ROOT=Path(__file__).resolve().parent
base=scaffold.base
DEFS,CHILD_RAW,CHILD_MID,TILE,LIGHT=0x10500000,0x10600000,0x10700000,0x10800000,0x10012400

def build(c):
    args,_=scaffold.build(dict(name='scaffold',count=c.get('count',4),flags=c.get('flags',0x40),scale=255,misc=c.get('misc',[0,0])))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    mem[scaffold.ERAW][0xF2]=18
    struct.pack_into('<8H',mem[scaffold.ERAW],0xCA,*c.get('slots',[1,2,3,4,65535,65535,65535,65535]))
    mem[base.BRIDGE]+=bytes(0x2700-len(mem[base.BRIDGE]))
    ids=[1,2,3,4,143,221] # complete original NextEffectId closure of1..4
    base.put(mem[base.BRIDGE],0x86,DEFS);base.put(mem[base.BRIDGE],0x8E,len(ids))
    base.put(mem[base.PARENT],0,0x66CBB4)
    mem[base.PARENT][0x10B:0x10F]=pack32(c.get('life',100))
    base.put(mem[base.SESSION],0x10,base.ENGINE)
    base.put(mem[base.EV],0xA0,LIGHT)
    mem[TILE]=bytearray(16);mem[TILE][9]=0x20
    mem[base.TOUCH]=bytearray(base.callback(16,TILE,4))
    mem[LIGHT]=bytearray(base.callback(0,0,20))
    mem[DEFS]=bytearray([165])*(len(ids)*148+64)
    mem[CHILD_MID]=bytearray([165])*(426+64)
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Effects.kwd'
    records=next(s.records for s in fmt.parse(source.read_bytes()) if s.signature==270)
    # Drop unused Element loading; keep its initialized scaffold buffers opaque.
    calls=args['before_calls'][4:8]
    for index,ident in enumerate(ids):
        raw=bytearray(next(r for r in records if int.from_bytes(r[0xB8:0xBA],'little')==ident))
        if 'child_misc' in c:struct.pack_into('<2H',raw,0xEE,*c['child_misc'])
        if ident in c.get('next_ids',{}):struct.pack_into('<H',raw,0xC0,c['next_ids'][ident])
        address=CHILD_RAW+index*0x1000;mem[address]=raw+bytes([165])*64
        calls += [dict(address='0x4357e0',registers=dict(ECX=hex(DEFS+148*index)),stack_args=[]),
            dict(address='0x56aec0',registers=dict(ECX=hex(CHILD_MID)),stack_args=[]),
            dict(address='0x56afe0',registers=dict(ECX=hex(CHILD_MID)),stack_args=[address]),
            dict(address='0x4358c0',registers=dict(ECX=hex(DEFS+148*index)),stack_args=[CHILD_MID,0])]
    if 'pool_size' in c:base.put(mem[scaffold.POOL],0,c['pool_size'])
    calls.append(dict(address='0x439100',registers=dict(ECX=hex(base.PARENT)),stack_args=[base.PARENT_DEF,1]))
    args.update(before_calls=calls,memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=2000000,
        observe_reads=[[scaffold.ERAW,246],[scaffold.EMID,426],[base.PARENT_DEF,148],[DEFS,len(ids)*148],[0,4096]],
        observe_writes=[[scaffold.POOL,scaffold.POOL_SIZE+64],[DEFS,len(ids)*148+64],[0x66CBB4,0x20],[0,4096]],
        allowed_external_IO_code=[[a,len(mem[a])] for a in [base.ALLOW,base.BLOCKED,base.TOUCH,base.FLOOR,scaffold.element.GCB,LIGHT]])
    checks={a:bytes(b) for a,b in mem.items() if a not in [base.ALLOW,base.BLOCKED,base.TOUCH,base.FLOOR,scaffold.element.GCB,LIGHT,0x7FFEF000]}
    for a,n in [(0x66CC94,0x190),(0x66CBB4,0x20),(0x66D0C4,0x40)]:checks[a]=pe_read(EXE.read_bytes(),a,n)
    return args,checks

def execute(c,verbose=True):
    args,checks=build(c);tag=cpu.sha((json.dumps(args,sort_keys=True)+cpu.sha((ROOT/'native_unicorn_effecttrace.py').read_bytes())+json.dumps({hex(a):cpu.sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase143-mode18-native-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(EXE,args,list(checks.items()),scaffold.element.STOP,base.PARENT,345)
        saved=dict(case=c,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if verbose:print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],registers=r['registers'],tail=r['trace_tail'],before_calls=r['before_calls'],created=r['visited_instructions'].get('0x4347e0',0))),flush=True)
    return args,checks,saved,path

if __name__=='__main__':execute(dict(name='four-effects',count=4))
