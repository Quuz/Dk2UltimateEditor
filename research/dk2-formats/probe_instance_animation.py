"""Original whole48BE50 from a saved post-melee Actor, with explicit resource cache.

This is a separate prepared renderer context, not an uninterrupted raw-load
pipeline. Descriptor presence/cache fields are explicit; no GPU assets loaded.
"""
from pathlib import Path
import json,struct,sys
import probe_melee_animation_flag as source
from portal_animation_tables import BASE,SIZE,initialized_animations
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
cpu=source.cpu;f,cf=source.f,source.cf
def build(c):
    args,checks,saved,_=source.execute(dict(enabled=c['enabled'],initial_runtime=False),False)
    assert saved['result']['success'];post=post_context(checks,saved['result'])
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    mem.update({a:bytearray(b) for a,b in post.items()})
    actor=mem[cf.ACTOR];definition=mem[cf.DEFINITION]
    for i in range(48):
        at=27*i;definition[at+11]|=1
        f.put(definition,at+15,0);f.put(definition,at+6,100,2)
    selector=initialized_animations()[c.get('action',17)*29+20] if c.get('action',17) else 39
    # All prepared model IDs coincide and current cached selector equals the
    # action selector: observe native descriptor selection without a swap IO.
    for o,v,n in [(0x3E6,0,4),(0x3A9,c.get('action',17),4),(0x3BB,0,4),(0x25E,87,4),
                  (0x6C,cf.DEFINITION+39*27,4),(0x70,0,4),(0xF2,selector,1),(0xF1,0,1),
                  (0x3C5,0,2),(0x4CE,0,2),(0x4DD,0,2),(0x4D6,0,1),(0x4E3,0,2),
                  (0x469,0,2),(0x44F,0,2),(0x45D,0,2),(0x447,0,2),(0x443,0,2),(0x44B,0,2),
                  (0x124+0x8C,4096 if c.get('moving') else 0,4),(0x124+0x90,0,4)]:f.put(actor,o,v,n)
    mem[BASE]=bytearray([165])*SIZE
    stack=bytearray([165])*16384;mem[0x7FFEE000]=stack
    args.update(address='0x48be50',registers=json.dumps(dict(ECX=hex(cf.ACTOR))),
        before_calls=[dict(address='0x4950c0',registers=dict(ECX='0'),stack_args=[])],
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=500000,
        observe_reads=[[cf.ACTOR,len(actor)],[cf.DEFINITION,len(definition)],[BASE,SIZE],[0,4096]],
        observe_writes=[[cf.ACTOR,len(actor)],[BASE,SIZE],[0,4096]])
    checks={a:bytes(b) for a,b in mem.items() if a not in [0x7FFEE000,f.GFX_CB,f.SOUND_CB,source.attack.EVENT,source.prior.LOAD_GFX,source.prior.LOAD_SOUND]}
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,checks
def execute(c,verbose=True):
    args,checks=build(c);tag=cpu.sha(json.dumps(args,sort_keys=True).encode())[:12];p=ROOT/'ghidra'/f'phase147-instance-animation-native-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(source.prior.EXE,args,list(checks.items()),f.OBS,cf.ACTOR,len(checks[cf.ACTOR]));saved=dict(case=c,input=args,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    if verbose:
        r=saved['result'];print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],registers=r['registers'],before_calls=r['before_calls'],tail=r['trace_tail'],
            selected_reads=[x for x in r['observed_reads'] if int(x['address'],16)>=cf.DEFINITION and int(x['address'],16)<cf.DEFINITION+48*27 and 0x48C540<=int(x['pc'],16)<=0x48C610])),flush=True)
    return args,checks,saved,p
if __name__=='__main__':execute(dict(enabled='on' in sys.argv,moving='moving' in sys.argv))
