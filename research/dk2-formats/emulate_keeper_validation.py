"""Original Keeper raw loaders, rule initializers and complete pre-cast validators.

Prepared World/Player/Actor/NAV, original vtables and helpers. Only external
sound-handle lookup is an explicit adapter. No code/vtable substitution.
"""
from pathlib import Path
import hashlib,json,struct,sys
import dk2_formats as fmt
import native_unicorn_keepertrace as cpu
from emulate_element_colors import region,pack32
from emulate_resistance import post_context
from native_keeper_pipeline import intermediate,runtime_projection
from native_keeper_array import default_definition
from build_effect_semantics import EXE
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/KeeperSpells.kwd'
WORLD,ARRAY,MID,RAW,SESSION=0x10000000,0x10010000,0x10012000,0x10014000,0x10018000
PLAYER,STATE,ACTOR,CDEF=0x10030000,0x10032000,0x10034000,0x10036000
OTHER_PLAYER=0x10038000
MAP,NAV,BITS=0x10040000,0x10042000,0x10043000
SOUND,SV,SCB,IO=0x10050000,0x10051000,0x10052000,0x10053000
OBS=0x10060000
CAST=[0,0x517B80,0x517CB0,0x517DE0,0x517F10,0x517F60]
TARGET=[0,0x517800,0x517930,0x517990,0x5179D0,0x517F60,0x517AB0]
def sha(b):return hashlib.sha256(b).hexdigest()
def put(b,o,v,n=4):b[o:o+n]=int(v).to_bytes(n,'little',signed=False)
def cases():
    rows=[dict(name='sight-point',spell=1),dict(name='possession-creature',spell=2,kind='creature')]
    rows.append(dict(name='call-to-arms-point',spell=3))
    rows.append(dict(name='secret1-Heal-own-creature',spell=10,kind='creature',source_file=str(SOURCE.parent/'maps/Secret1Globals.kld')))
    for v in [0,1,0x7FFFFFFF,0x80000000,0xFE0C0000,0xFFFFFFFF]:
        rows.append(dict(name=f'sight-c8-{v:08x}',spell=1,c8=v))
    for name,c in [('mana-below',dict(mana=4999)),('mana-equal',dict(mana=5000)),
        ('cooldown-before',dict(last=90,recharge=4096)),('cooldown-boundary',dict(last=80,recharge=4096)),
        ('timestamp-zero',dict(last=0,recharge=0xFFFFFFFF)),
        ('cast-none',dict(cast=0)),('cast-out-of-range',dict(cast=6)),('cast255',dict(cast=255)),
        ('cast-any-land',dict(cast=4)),('cast-any-land-solid',dict(cast=4,solid=True)),
        ('target-none',dict(target=0)),('target-all',dict(target=5)),('target-out-of-range',dict(target=7)),('target255',dict(target=255))]:
        rows.append(dict(name=name,spell=1,**c))
    for name,c in [('enemy-owner',dict(owner=8)),('neutral-owner',dict(owner=0)),
        ('Horny',dict(def_flags=0x4000)),('actor-flag8',dict(actor_flags=8)),
        ('actor-flag8000',dict(actor_flags=0x8000)),('word3cb',dict(word3cb=1)),('word44b',dict(word44b=1)),
        ('state-d1',dict(state=0xD1)),('state-ff',dict(state=0xFF)),('state-100',dict(state=0x100)),('state-101',dict(state=0x101)),
        ('state-c1',dict(state=0xC1)),('state-116',dict(state=0x116)),('state-117',dict(state=0x117)),('state-118',dict(state=0x118)),
        ('ordinary-state',dict(state=17)),('missing-tag',dict(exists=False)),('wrong-type',dict(actor_type=2))]:
        rows.append(dict(name='possession-'+name,spell=2,kind='creature',**c))
    for target in [1,2,3,5]:
        for owner in [0,7,8]:
            rows.append(dict(name=f'target{target}-owner{owner}',spell=2,kind='creature',target=target,owner=owner))
    for target in [1,2]:
        for owner in [7,8]:rows.append(dict(name=f'target{target}-inverted-owner{owner}',spell=2,kind='creature',target=target,owner=owner,flags=4))
    for v in [0,1,0xFE0C0000,0xFFFFFFFF]:rows.append(dict(name=f'possession-c8-{v:08x}',spell=2,kind='creature',c8=v))
    for cast in [1,2,3,4]:
        for owner,allied in [(7,False),(8,False),(8,True)]:
            rows.append(dict(name=f'cast{cast}-land{owner}-allied{int(allied)}',spell=1,cast=cast,cell_owner=owner,allied=allied))
    for cast in [1,2,3,4]:
        for marked in [False,True]:
            rows.append(dict(name=f'cast{cast}-flag2-marked{int(marked)}',spell=1,cast=cast,cell_owner=8 if cast==3 else 7,flags=2,marked=marked))
    return rows

def build(c):
    source=Path(c.get('source_file',SOURCE))
    raw=bytearray(next(r for s in fmt.parse(source.read_bytes()) if s.signature==150 for r in s.records if r[0xEE]==c['spell']))
    for k,o,n in [('c8',0xC8,4),('recharge',0xCC,4),('cast',0xEF,1),('target',0xDA,1)]:
        if k in c:put(raw,o,c[k],n)
    world=bytearray(0xA3D0)
    for o,v in [(0,0x66E3EC),(4,SESSION),(0x1604,ARRAY),(0x1608,1),(0x160C,1),(0x9E6,16),(0x9EA,16),(0x9EE,MAP),(0x9F2,NAV)]:put(world,o,v)
    player=bytearray(0x1100);put(player,4,7,2);put(player,0x6E,1);put(player,0x72,STATE);put(player,0x82,c.get('mana',1000000));player[0x10FC]=1
    other_player=bytearray(0x1100);put(other_player,4,8,2);other_player[0x10FC]=2;player[0x1AE+2]=int(c.get('allied',False))
    state=bytearray(17);state[0]=c['spell'];put(state,5,c.get('last',0))
    actor=bytearray(0x500);put(actor,0x12,c.get('actor_type',0));struct.pack_into('<3i',actor,0x1A,0x2800,0x3800,0x1000)
    put(actor,0x28,c.get('owner',7),2);put(actor,0x370,CDEF);put(actor,0x25E,c.get('state',0));put(actor,0x3E6,c.get('actor_flags',0))
    put(actor,0x3CB,c.get('word3cb',0),2);put(actor,0x44B,c.get('word44b',0),2)
    cdef=bytearray(0x800);put(cdef,0x686,c.get('def_flags',0))
    cells=bytearray(16*16*16)
    for i in range(256):put(cells,16*i+8,c.get('cell_owner',7)|(0x2000 if c.get('marked') else 0),2)
    nav=bytearray(20);put(nav,8,16);put(nav,12,16);put(nav,16,BITS)
    bits=bytes([8 if c.get('solid') else 0])*256
    tags=bytearray(64);put(tags,9*4,ACTOR);put(tags,7*4,PLAYER);put(tags,8*4,OTHER_PLAYER);valid=bytearray(16);valid[9]=int(c.get('exists',True))
    sv=bytearray(32);put(sv,0x1C,SCB)
    sound_cb=bytes.fromhex('ff05')+pack32(IO)+b'\xb8'+pack32(0x11223344)+bytes.fromhex('c20800')
    session=bytearray(20);put(session,4,100)
    mem={WORLD:world,ARRAY:bytes([165])*(122+64),MID:bytes([165])*(526+64),RAW:bytes(raw),SESSION:session,
        PLAYER:player,OTHER_PLAYER:other_player,STATE:state,ACTOR:actor,CDEF:cdef,MAP:cells,NAV:nav,BITS:bits,
        SOUND:pack32(SV),SV:sv,SCB:sound_cb,IO:bytes(4),
        0x6E5054:pack32(WORLD),0x6F2590:pack32(20),0x6F2548:bytes(2),0x6FCDCF:bytes(4),
        0x75B468:pack32(SOUND),0x6ED540:valid,0x6EE540:tags,
        0x6FCDD8:bytes([165])*(0x6FCEBE-0x6FCDD8),0x7FFEE000:bytes([165])*0x4000}
    calls=[dict(address=hex(a),registers=dict(ECX=hex(ecx)),stack_args=stack) for a,ecx,stack in [
        (0x4F7C70,ARRAY,[]),(0x56B990,MID,[]),(0x56BAB0,MID,[RAW]),(0x4F7CE0,ARRAY,[MID,0]),
        (0x5174A0,0,[]),(0x517580,0,[])]]
    kind=c.get('kind','point');entry=0x4BB8A0 if kind=='point' else 0x4BBA40
    arg=ACTOR+0x1A if kind=='point' else 9
    stack_args=[c['spell'],arg,c.get('flags',0)]
    args=dict(program='DKII-DX.exe',address=hex(entry),registers=json.dumps(dict(ECX=hex(PLAYER))),
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=200000,before_calls=calls,stack_args=stack_args,
        allowed_external_IO_code=[[SCB,len(sound_cb)]],observe_reads=[[ARRAY,122],[ACTOR,0x500],[CDEF,0x800],[0,4096]],
        observe_writes=[[ARRAY,122],[PLAYER,len(player)],[STATE,17],[ACTOR,0x500]])
    # Backend restores the root arguments after all native initializers.
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    checks={a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000}
    return args,checks,bytes(raw)

def expected(c,raw):
    if c.get('mana',1000000)<int.from_bytes(raw[0x11C:0x120],'little'):return 0
    if c.get('last',0) and 100-c['last'] < ((int.from_bytes(raw[0xCC:0xD0],'little')*20)&0xFFFFFFFF)>>12:return 0
    if raw[0xEF] in [0,6,255]:return 0
    cast=raw[0xEF];owner=c.get('cell_owner',7);allied=owner==7 or c.get('allied',False)
    if cast in [1,2,3,4] and c.get('solid'):return 0
    if cast in [1,2] and not allied:return 0
    if cast==3 and allied:return 0
    if cast in [2,3,4] and c.get('flags',0)&2 and not c.get('marked'):return 0
    t=raw[0xDA]
    if t in [0,7,255]:return 0
    if c.get('kind','point')=='point':return 1
    if not c.get('exists',True) or c.get('actor_type',0)!=0:return 0
    owner=c.get('owner',7);flags=c.get('flags',0)
    if t==1:return int(owner not in [0,7] if flags&4 else owner==7 and not c.get('def_flags',0)&0x4000)
    if t==2:return int(owner==7 and not c.get('def_flags',0)&0x4000 if flags&4 else owner not in [0,7])
    if t==3:return int(owner!=0)
    if t==5:return 1
    assert t==6
    return int(owner==7 and not(c.get('def_flags',0)&0x4000 or c.get('actor_flags',0)&0x8008 or c.get('word3cb',0) or c.get('word44b',0)
        or c.get('state',0) in [0xD1,0xFF,0x100,0x101,0xC1,0x116,0x117,0x118]))

def execute(c):
    args,checks,raw=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12]
    p=ROOT/'ghidra'/f'phase152-keeper-validation-native-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args and saved['backend_sha256']==backend
    else:
        saved=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(checks.items()),OBS,ARRAY,122))
        p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return args,checks,raw,saved,p

def verify(c,args,checks,raw,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'],r['registers'])
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==6
    assert int(r['registers']['EAX'],16)==expected(c,raw),(c,r['registers'],expected(c,raw))
    post=post_context(checks,r);want=runtime_projection(intermediate(raw),default_definition(165),20,0)
    assert post[ARRAY]==want+bytes([165])*64
    assert post[MID]==intermediate(raw)+bytes([165])*64
    # No normal gameplay mutation occurs during validation. Loader IO is
    # accounted for separately; raw bytes and every supplied scene stay exact.
    altered={ARRAY,MID,IO,0x6FCDD8}
    for a,b in checks.items():
        if a not in altered:assert post[a]==b,(c['name'],hex(a))
    table=post[0x6FCDD8]
    for i,fn in enumerate(CAST):assert int.from_bytes(table[17*i:17*i+4],'little')==fn
    for i,fn in enumerate(TARGET):assert int.from_bytes(table[0x68+18*i:0x68+18*i+4],'little')==fn
    # Distinguish loader writes/copies from gameplay definition reads.
    overlap=[x for x in r['observed_reads'] if int(x['address'],16)<ARRAY+0x3E and int(x['address'],16)+x['size']>ARRAY+0x3A]
    assert not overlap,(c['name'],overlap)
    assert not any(int(x['address'],16)<4096 for x in r['observed_reads'])
    return post,dict(return_value=expected(c,raw),raw_spell_id=raw[0xEE],CastRule=raw[0xEF],TargetRule=raw[0xDA],
        C8=int.from_bytes(raw[0xC8:0xCC],'little'),C8_definition_reads=0,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),original_instructions=r['instruction_count'])

def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    rows=[]
    for c in selected:
        args,checks,raw,saved,p=execute(c);_,out=verify(c,args,checks,raw,saved['result'])
        rows.append(dict(case=c,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),outcome=out));print(c['name'],out['return_value'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase152-keeper-validation-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
