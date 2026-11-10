"""Raw attacker Creature flags -> whole original loader and native melee.

Only raw bit29 is varied; no new raw field name inferred from template labels.
Prepared Actor/target/world, native class vtables, explicit resource IO adapters.
"""
from pathlib import Path
import json,struct,sys
import probe_creature_resistance as prior
import native_unicorn_factorytrace as cpu
from emulate_element_colors import region
from emulate_resistance import post_context
from portal_animation_tables import BASE,SIZE
ROOT=Path(__file__).resolve().parent
f,cf,sp,attack=prior.f,prior.cf,prior.sp,prior.attack
def coalesce_world(mem):
    # The inherited loader scene extended World across prepared pool views.
    # Preserve exact ordered last-write-wins input bytes in one physical
    # region, rather than retaining overlapping named views in the fixture.
    lo=f.WORLD;hi=lo+len(mem[lo]);parts=[(a,b) for a,b in mem.items() if lo<=a and a+len(b)<=hi]
    merged=bytearray(hi-lo)
    for a,b in parts:merged[a-lo:a-lo+len(b)]=b
    for a,_ in parts:
        if a!=lo:del mem[a]
    mem[lo]=merged
def build(c):
    args,_,_=prior.build(0,1)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=next(r for s in prior.fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd').read_bytes()) if s.signature==0xAA for r in s.records if r[0xF4B]==8)
    raw=bytearray(raw);flags=int.from_bytes(raw[0xEFC:0xF00],'little');f.put(raw,0xEFC,(flags&~0x20000000)|(0x20000000 if c['enabled'] else 0))
    mem[prior.RAW]=raw
    actor=mem[cf.ACTOR]
    flags2=int.from_bytes(actor[0x3EA:0x3EE],'little');f.put(actor,0x3EA,(flags2&~0x1000)|(0x1000 if c.get('initial_runtime',False) else 0))
    f.put(actor,0x3A9,17);f.put(actor,0x3BB,0)
    f.put(mem[f.SESSION],4,0 if c.get('cooldown') else 10000)
    if c.get('missing_target'):mem[0x6ED540][101]=0
    for row in args['before_calls']:
        if row['address'] in ['0x491dc0','0x492320']:row['registers']['ECX']=hex(cf.DEFINITION)
    mem[BASE]=bytearray([165])*SIZE
    args['before_calls'].append(dict(address='0x4950c0',registers=dict(ECX='0'),stack_args=[]))
    coalesce_world(mem)
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[cf.DEFINITION,2199],[cf.ACTOR,len(actor)],[prior.RAW,len(raw)]]
    args['observe_writes'] += [[cf.ACTOR,len(actor)],[cf.DEFINITION,2199]]
    checks={a:bytes(b) for a,b in mem.items() if a not in [0x7FFEE000,f.GFX_CB,f.SOUND_CB,attack.EVENT,prior.LOAD_GFX,prior.LOAD_SOUND]}
    return args,checks
def execute(c,verbose=True):
    args,checks=build(c);tag=cpu.sha(json.dumps(args,sort_keys=True).encode())[:12];p=ROOT/'ghidra'/f'phase147-melee-animation-native-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(prior.EXE,args,list(checks.items()),f.OBS,cf.ACTOR,len(checks[cf.ACTOR]));saved=dict(case=c,input=args,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    if verbose:
        r=saved['result'];post=post_context(checks,r);u=lambda a,o:int.from_bytes(post[a][o:o+4],'little')
        print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],before_calls=r['before_calls'],flags=hex(u(cf.DEFINITION,0x686)),runtime=hex(u(cf.ACTOR,0x3EA)),HP=u(attack.TARGET,0x40),return_value=r['registers']['EAX'],tail=r['trace_tail'])),flush=True)
    return args,checks,saved,p
if __name__=='__main__':execute(dict(enabled='on' in sys.argv,initial_runtime='initial' in sys.argv))
