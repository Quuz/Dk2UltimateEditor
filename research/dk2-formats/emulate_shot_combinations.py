"""Phase168: original loaded ProcessType/DamageType pairs through real collision.

Prepared Creature snapshots, owners, map and original resource adapters reuse
the accepted phase115 fixture. Original loaders, spell wrapper, collision,
process table initialization, HP/status methods and optional Creature ticks run
unchanged. Diagnostic pairs are explicitly distinguished from source usage.
"""
from pathlib import Path
import hashlib,json,struct,sys
import probe_poison as p
import native_aoe_trace as cpu
from emulate_element_colors import region
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
EXE=p.EXE
sha=lambda b:hashlib.sha256(b).hexdigest()

def cases():
    out=[]
    seed=dict(process=0,mode=7,data1=80,data2=5,mask=4,ticks=0,armour=0,invulnerable=0,initial_HP=1000)
    rows=[('hit-only',{}),('no-process-no-damage',dict(mode=0)),
        ('armour-only',dict(process=5,mode=0)),('armour-and-hit',dict(process=5)),
        ('armour-mask-miss',dict(process=5,mask=2)),('armour-zero-and-hit',dict(process=5,data1=0)),
        ('invulnerable-only',dict(process=10,mode=0)),('invulnerable-and-hit',dict(process=10)),
        ('invulnerable-mask-miss',dict(process=10,mask=2)),
        ('invulnerable-wrap-and-hit',dict(process=10,data1=0x4000,initial_HP=100000)),
        ('freeze-only',dict(process=11,mode=0)),('freeze-and-hit',dict(process=11)),
        ('freeze-mask-miss',dict(process=11,mask=2)),
        ('poison-only',dict(process=38,mode=0)),('poison-and-hit',dict(process=38)),
        ('poison-mask-miss',dict(process=38,mask=2)),
        ('poison-and-hit-tick',dict(process=38,ticks=1)),
        ('poison-and-hit-armoured',dict(process=38,armour=2,ticks=1)),
        ('poison-and-hit-blocked',dict(process=38,invulnerable=2,ticks=1)),
        ('poison-wide-and-hit',dict(process=38,data1=0x10002,data2=0x10005,initial_HP=100000)),
        ('poison-negative-hit',dict(process=38,data1=0xFFFFFFFF)),
        ('noop-fear-and-hit',dict(process=21)),
        ('armour-preexisting-hit',dict(armour=2)),
        ('invulnerable-preexisting-hit',dict(invulnerable=2)),
    ]
    for n,kw in rows:out.append(dict(seed,**kw,name=n))
    return out

def build(c):
    args,_=p.build(ticks=c['ticks'],duration=c['data1'],severity=c['data2'],
        armour=c['armour'],invulnerable=c['invulnerable'])
    mem={int(x['address'],16):bytearray.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
    raw=mem[p.shot.RAW]
    raw[0xC7]=c['process'];raw[0xC5]=c['mode'];p.f.put(raw,0xA0,c['mask'])
    p.f.put(mem[p.target.attack.TARGET],0x40,c['initial_HP'])
    # Keep the original three raw loading chains and caster override semantics.
    assert int.from_bytes(raw[0x98:0x9C],'little')==c['data1']
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[p.PROCESS,39*17],[p.shot.NODE,371],[p.shot.DEFINITION,118]]
    args['observe_writes'] += [[p.target.attack.TARGET+0x40,4],[p.target.attack.TARGET+0x443,0x20]]
    excluded=[0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,0x28CFA4]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    checks[p.shot.SHOT_VTABLE]=p.pe_read(EXE.read_bytes(),p.shot.SHOT_VTABLE,0x100)
    return args,checks

def execute(c):
    args,checks=build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['emulate_shot_combinations.py','probe_poison.py','native_aoe_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    path=ROOT/'ghidra'/f'phase168-shot-combinations-{c["name"]}-{tag}.json'
    if path.exists():
        saved=json.loads(path.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),p.f.OBS,p.target.attack.TARGET,1599)
        saved=dict(case=c,input=args,result=r)
        path.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,path

def inspect(c,checks,r):
    post=post_context(checks,r);b=post[p.target.attack.TARGET]
    u=lambda o,n=2:int.from_bytes(b[o:o+n],'little')
    return dict(HP=u(0x40,4),armour=u(0x443),invulnerable=u(0x445),freeze=u(0x447),
        poison=u(0x45D),severity=u(0x45F),poison_owner=u(0x461),
        requests=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']],
        callbacks={hex(a):r['visited_instructions'].get(hex(a),0) for a in [0x4ABD60,0x4AD730,0x4AE0D0,0x4AE180,0x4AE1D0,0x484940,0x4B0270,0x4AC420,0x488930]})

def expected(c):
    """Independent bounded integer model from original collision/status/HP code.

    Only the explicitly prepared ordinary Creature path and these test inputs.
    Timer narrowing and signed-positive HIT_THING gate are distinct operations.
    No inference about ordinary scheduling, lethality cleanup or other targets.
    """
    armour=c['armour'];inv=c['invulnerable'];freeze=poison=severity=owner=0
    applied=bool(c['mask']&4)
    if applied:
        if c['process']==5:armour=c['data1']&65535
        if c['process']==10:inv=(c['data1']*20)&65535
        if c['process']==11:freeze=(c['data1']*20)&65535
        if c['process']==38:poison=c['data1']&65535;severity=c['data2']&65535;owner=7
    hp=c['initial_HP'];requests=[];writes=[]
    def request(delta,caller):
        nonlocal hp
        requests.append(dict(target=p.target.attack.TARGET,delta=delta,before=hp,caller=caller))
        coefficient=(160 if freeze else 128)-(64 if armour else 0)
        product=delta*coefficient
        scaled=abs(product)//128*(-1 if product<0 else 1)
        hp=(hp+scaled)&0xFFFFFFFF
        if delta:writes.append(hp)
    if c['mode']==7 and 0<c['data1']<0x80000000 and not inv:
        request(-c['data1'],'0x4ac58e')
    for _ in range(c['ticks']):
        armour=max(0,armour-1)
        if freeze:freeze-=1
        elif poison:
            poison-=1;request(-severity,'0x48e364')
        inv=max(0,inv-1)
    return dict(HP=hp,armour=armour,invulnerable=inv,freeze=freeze,poison=poison,
        severity=severity,poison_owner=owner,requests=requests,HP_writes=writes)

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);out=inspect(c,checks,r)
    for a in [p.shot.RAW,p.target.RAW,p.RAWSPELL,p.shot.SHOT_VTABLE]:assert post[a]==checks[a]
    d=post[p.shot.DEFINITION]
    assert d[0x6C]==c['process'] and d[0x6B]==c['mode'] and int.from_bytes(d[0x3F:0x43],'little')==c['mask']
    node=post[p.f.BLOCK][p.shot.NODE-p.f.BLOCK:p.shot.NODE-p.f.BLOCK+371]
    assert int.from_bytes(node[0x147:0x149],'little')==101
    assert int.from_bytes(node[0x145:0x147],'little')==100
    assert int.from_bytes(node[0x13D:0x141],'little')==c['data1'] and int.from_bytes(node[0x141:0x145],'little')==c['data2']
    assert int.from_bytes(node[0x40:0x44],'little')==0 and post[0x6ED540][102]==1
    for a in [0x491DC0,0x568820,0x568E30,0x492320,0x4ACD60,0x56E540,0x56E690,0x4ACDF0,0x56A490,0x56A560,0x4F7860,0x4984D0,0x4B7CD0,0x4AAC90,0x4ABCA0,0x4AD1B0,0x4ABD60]:
        assert r['visited_instructions'].get(hex(a),0)==1,(c['name'],hex(a))
    assert r['visited_instructions'].get('0x4ac420',0)==int(c['mode']==7)
    assert r['visited_instructions'].get('0x488930',0)==c['ticks']
    applied=bool(c['mask']&4 and c['process'])
    handlers={5:0x4AE0D0,10:0x4AE180,11:0x4AE1D0,21:0x484940,38:0x4B0270}
    for process,a in handlers.items():assert r['visited_instructions'].get(hex(a),0)==int(applied and c['process']==process)
    e=expected(c)
    assert all(out[k]==v for k,v in e.items() if k!='HP_writes'),(c['name'],out,e)
    hp_writes=[x['value'] for x in r['observed_writes'] if int(x['address'],16)==p.target.attack.TARGET+0x40]
    assert hp_writes==e['HP_writes'],(c['name'],hp_writes,e['HP_writes'])
    return post,out

def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,checks,saved,path=execute(c);r=saved['result']
        out=inspect(c,checks,r)
        print(c['name'],r['success'],r['emulation_error'],json.dumps(out),flush=True)
        if not r['success']:print(r['trace_tail'],flush=True)
        _,out=verify(c,args,checks,r)
        rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out))
    if len(selected)==len(cases()):
        (ROOT/'phase168-shot-combinations-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
