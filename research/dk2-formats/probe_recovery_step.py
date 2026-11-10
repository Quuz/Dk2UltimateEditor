"""Phase184 discovery: causal raw128B recovery displacement, no host XY writes.

Reuse the exact native post-recovery257->481200->room assignment boundary.
Only supplied raw coordinate components vary before original converters.
"""
from pathlib import Path
import json,struct,sys
import probe_crypt_room_assignment as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u

def build(c):
    base=dict(species=5,owner=7,entry_state=274,entry_path='post_recovery',register=False,activate=False,tick=False,exit=False,
        native_nav=True,HP=900,x=22528,room_area=True,source_trig=True,null_renderer_cache=True,direction=c.get('direction',0x600),movement_steps=1)
    args,ch,ofs=old.build(base)
    # The recovery handler's481400 reads the actual Warlock spell catalogue.
    # Request the inherited full native1/2/14/41 loaders, but omit its extra
    # movement update: this probe ends at room assignment, before registration.
    calls=args['before_calls'];indices=[i for i,x in enumerate(calls) if x['address']=='0x476fc0']
    assert len(indices)==1
    del calls[indices[0]]
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    for field,off in [('raw_x',0x1287),('raw_y',0x128B),('raw_z',0x128F)]:
        if field in c:p.f.put(mem[p.target.RAW],off,c[field])
    ch[p.target.RAW]=bytes(mem[p.target.RAW])
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    return args,ch,ofs

def execute(c):
    args,ch,ofs=build(c);deps=sha(Path(__file__).read_bytes())+sha(Path(old.__file__).read_bytes())
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    for attempt in range(3):
        suffix='' if attempt==0 else f'-attempt{attempt+1}'
        f=ROOT/'ghidra'/f'phase184-recovery-step-{tag}{suffix}.json'
        if f.exists():s=json.loads(f.read_text(encoding='utf-8'));assert s['input']==args
        else:
            r=old.old.old.prior.old.base.cpu.run(EXE,args,list(ch.items()),p.f.OBS,old.ACTOR,len(ch[old.ACTOR]))
            s=dict(case=c,input=args,result=r);f.write_text(json.dumps(s,indent=2),encoding='utf-8')
        r=s['result']
        # Completed terminal CPU run without a fault can hit the bounded
        # wall-time inside an observation copy; retain it and retry separately.
        # Real invalid reads/execution faults are never retried as successes.
        if r['success'] or r['emulation_error'] or r['protected_write_or_execution_failures']:break
    return args,ch,s,f,ofs

def describe(c):
    args,ch,s,f,ofs=execute(c);r=s['result'];post=post_context(ch,r);o,n,_=ofs['actor'];a=post[old.LOG][o:o+n]
    definition=u(post[old.ACTOR],0x370)
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],state=u(a,0x25E),
        position=[u(a,o) for o in [0x1A,0x1E,0x22]],native_slot2=list(struct.unpack_from('<3i',post[definition],0x704)),
        file=str(f.relative_to(ROOT)))),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
