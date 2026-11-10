"""Phase180 discovery: original Crypt action80 callback and action dispatcher.

Prepared explicit owner lists and clock. Full loaded source participant
definition; runtime instance starts from a real initialized Creature snapshot.
No automatic job entry, visible animation or World scheduling claim.
"""
from pathlib import Path
import json,struct,sys
import probe_crypt_objects as prior
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=prior.p
EXE=prior.EXE
sha=prior.sha
PARTICIPANT=p.target.attack.TARGET
RECIPIENT=p.cf.ACTOR
UNRELATED=0x20E10000
UI,UIVT,UICB,UILOG=0x20E30000,0x20E31000,0x20E32000,0x20E33000
VARIABLE=0x20E40000
KEEPER_RAW,KEEPER_MID,KEEPER_DEF=0x20E50000,0x20E51000,0x20E52000
SOURCE=prior.SOURCE.parent/'Creatures.kwd'
u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')

def build(c):
    args,_,_,_=prior.build(dict(count=0,spare_blocks=c.get('spare_blocks',4),old_registered=False))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    species=c.get('species',5)
    raw=next(r for s in prior.fmt.parse(SOURCE.read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==species)
    assert len(raw)==5449
    mem[p.target.RAW]=bytearray(raw)
    mem[PARTICIPANT][0x36F]=species
    owner=u(mem[PARTICIPANT],0x2A);outside=u(mem[RECIPIENT],0x2A)
    # The player-linked list uses CreatureWORD10, independent of room list.
    # Both explicit listed recipients belong to owner8; a third full snapshot
    # belongs to owner7 and is not linked to owner8.
    mem[UNRELATED]=bytearray(mem[RECIPIENT]);p.f.put(mem[UNRELATED],4,303,2)
    p.f.put(mem[UNRELATED],0x256,UNRELATED)
    p.f.put(mem[UNRELATED],0x10,0,2);p.f.put(mem[UNRELATED],0xE,0,2)
    p.f.put(mem[0x6EE540],303*4,UNRELATED);mem[0x6ED540][303]=1
    p.f.put(mem[outside],6,303,2)
    p.f.put(mem[RECIPIENT],0x28,8,2);p.f.put(mem[RECIPIENT],0x2A,owner)
    p.f.put(mem[PARTICIPANT],0x10,100,2);p.f.put(mem[PARTICIPANT],0xE,0,2)
    p.f.put(mem[RECIPIENT],0x10,0,2);p.f.put(mem[RECIPIENT],0xE,101,2)
    p.f.put(mem[owner],6,101 if c.get('listed',True) else 0,2)
    for a in [PARTICIPANT,RECIPIENT,UNRELATED]:
        p.f.put(mem[a],0x40,c.get('HP',100))
        for off in [0x443,0x445,0x447,0x44B,0x45D]:p.f.put(mem[a],off,0,2)
        # Six native accumulation values, threshold and category. Explicit
        # high threshold isolates numerical updates from angry-state events.
        mem[a][0x26E:0x28E]=bytes(32)
        for i in range(6):p.f.put(mem[a],0x26E+4*i,c.get('initial_anger',1000))
        p.f.put(mem[a],0x286,30000)
    world=mem[p.f.WORLD]
    config=u(mem[0x6DC424],0);assert config==p.f.WORLD
    world[0x72A1]=(world[0x72A1]&254)|int(c.get('accumulation_enabled',True))
    variable=next(r for s in prior.fmt.parse((SOURCE.parent/'GlobalVariables.kwd').read_bytes()) if s.signature==230 for r in s.records if u(r,0)==102)
    assert len(variable)==16
    variable=bytearray(variable)
    if 'unit' in c:p.f.put(variable,4,c['unit']&0xFFFFFFFF)
    mem[VARIABLE]=bytearray(16)+variable
    mem[0x6F28E8]=bytearray([165,165])
    p.f.put(mem[PARTICIPANT],0x3A5,c.get('last',0))
    p.f.put(mem[PARTICIPANT],0x3A9,80)
    session=u(mem[p.f.WORLD],4);p.f.put(mem[session],4,c.get('clock',5))
    assert u(mem[0x6E5050],0)==p.f.WORLD
    calls=args['before_calls'];assert calls[-1]['address']=='0x4d9410';calls=calls[:-1]
    # Extend the actual raw Object table with Chicken9 and Rat11, not a
    # fabricated Object payload. Allfive definitions use original converters.
    ids=prior.IDS+[9,11]
    raws={r[0x35A]:r for s in prior.fmt.parse(prior.SOURCE.read_bytes()) if s.signature==240 for r in s.records}
    mem[prior.DEFS]=bytearray(285*5)+bytearray([165])*64
    mem[prior.RAWS]=bytearray(b''.join(raws[i] for i in ids))+bytearray([165])*64
    p.f.put(world,0x15F8,5);p.f.put(world,0x15FC,5)
    for i in [3,4]:
        calls += [dict(address='0x56d4f0',registers=dict(ECX=hex(prior.MID)),stack_args=[]),
            dict(address='0x56d620',registers=dict(ECX=hex(prior.MID)),stack_args=[prior.RAWS+894*i]),
            dict(address='0x49dd60',registers=dict(ECX=hex(prior.DEFS+285*i)),stack_args=[prior.MID,0])]
    calls.append(dict(address='0x51cd90',registers=dict(ECX='0'),stack_args=[VARIABLE]))
    # Quake notification requests original KeeperSpell8 (Tremor) sound data
    # through World+110 ->50DD60. Supply the real source definition instead
    # of allowing a null lookup followed by an invalid read of address6D.
    keeper=next(r for s in prior.fmt.parse((SOURCE.parent/'KeeperSpells.kwd').read_bytes()) if s.signature==150 for r in s.records if r[0xEE]==8)
    mem[KEEPER_RAW]=bytearray(keeper)+bytearray([165])*64
    mem[KEEPER_MID]=bytearray([165])*(526+64);mem[KEEPER_DEF]=bytearray(122)+bytearray([165])*64
    for off,value in [(0x1604,KEEPER_DEF),(0x1608,1),(0x160C,1)]:p.f.put(world,off,value)
    calls += [dict(address='0x4f7c70',registers=dict(ECX=hex(KEEPER_DEF)),stack_args=[]),
        dict(address='0x56b990',registers=dict(ECX=hex(KEEPER_MID)),stack_args=[]),
        dict(address='0x56bab0',registers=dict(ECX=hex(KEEPER_MID)),stack_args=[KEEPER_RAW]),
        dict(address='0x4f7ce0',registers=dict(ECX=hex(KEEPER_DEF)),stack_args=[KEEPER_MID,0])]
    # Only the external floating notification endpoint is provided here.
    # Log four arguments and preserve original gameplay/money methods.
    provider=u(mem[session],0x10);vtable=u(mem[provider],0)
    # Preserve inherited resource/Shot notification endpoints used earlier in
    # the composition; extend only the explicit external provider's slot64.
    mem[UI]=bytearray(mem[provider]);mem[UIVT]=bytearray(mem[vtable])
    mem[UIVT].extend(bytes(max(0,0x68-len(mem[UIVT]))))
    p.f.put(mem[UI],0,UIVT);p.f.put(mem[UIVT],0x64,UICB)
    code=bytearray(bytes.fromhex('ff05')+struct.pack('<I',UILOG))
    for i in range(4):code+=bytes.fromhex('8b4424')+bytes([4+4*i])+b'\xa3'+struct.pack('<I',UILOG+4+4*i)
    code+=bytes.fromhex('33c0c21000');mem[UICB]=code;mem[UILOG]=bytearray(20)
    p.f.put(mem[session],0x10,UI)
    args['allowed_external_IO_code'] += [[UICB,len(code)]]
    for _ in range(c.get('repeat',1)-1):calls.append(dict(address='0x499ca0',registers=dict(ECX=hex(PARTICIPANT)),stack_args=[]))
    args['before_calls']=calls
    args['address']='0x496aa0' if c.get('dispatcher') else '0x499ca0'
    args['registers']=json.dumps(dict(ECX=hex(PARTICIPANT)))
    args['observe_reads'] += [[owner,len(mem[owner])],[outside,len(mem[outside])],[PARTICIPANT,len(mem[PARTICIPANT])],[RECIPIENT,len(mem[RECIPIENT])],[session,len(mem[session])]]
    args['observe_writes'] += [[UNRELATED,len(mem[UNRELATED])],[owner,len(mem[owner])],[outside,len(mem[outside])],[UILOG,20],[p.f.WORLD,len(world)]]
    args['observe_reads'] += [[prior.DEFS,len(mem[prior.DEFS])],[prior.RAWS,len(mem[prior.RAWS])],[0x6F28E8,2]]
    args['max_steps']=3000000
    # Regenerate only the declared imported readability IO for the new region.
    mem[prior.readable.ADDRESS]=bytearray(prior.readable.adapter(prior.readable.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=prior.readable.ADDRESS])))
    args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=prior.readable.ADDRESS]+[[prior.readable.ADDRESS,len(mem[prior.readable.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    excluded={0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,prior.readable.ADDRESS,prior.SOUND_END,UICB}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D654,0x80),(0x66D754,0x100),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:checks[a]=p.pe_read(EXE.read_bytes(),a,n)
    return args,checks

def execute(c):
    args,checks=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    for attempt in range(3):
        suffix='' if attempt==0 else f'-attempt{attempt+1}'
        file=ROOT/'ghidra'/f'phase180-crypt-action-discovery-{tag}{suffix}.json'
        if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
        else:
            r=prior.old.base.cpu.run(EXE,args,list(checks.items()),p.f.OBS,PARTICIPANT,len(checks[PARTICIPANT]))
            saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
        r=saved['result']
        if r['success']:return args,checks,saved,file
        # Frozen backend has a5s per-call wall limit. A terminal incomplete
        # original-code trace can be retried identically; retain every failed
        # trace, never relabel it as success or restart a live process. Real
        # errors/protectedwrites terminate immediately. Only a successful
        # complete replay can enter the accepted manifest.
        if r['emulation_error'] or r['protected_write_or_execution_failures']:break
    return args,checks,saved,file

def describe(c):
    args,ch,s,file=execute(c);r=s['result'];post=post_context(ch,r)
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],EAX=r['registers']['EAX'],
        HP=[u(post[a],0x40) for a in [PARTICIPANT,RECIPIENT,UNRELATED]],last=u(post[PARTICIPANT],0x3A5),action=u(post[PARTICIPANT],0x3A9),
        accumulations=[[u(post[a],0x26E+4*i) for i in range(6)] for a in [PARTICIPANT,RECIPIENT,UNRELATED]],
        money=u(post[u(ch[PARTICIPANT],0x2A)],0x7E),UI=post[UILOG].hex(),
        requests=r['hp_requests'],before=r['before_calls'][-3:],tail=r['trace_tail'][-20:],file=str(file.relative_to(ROOT)))),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
