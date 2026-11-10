"""Complete original AP bind/tick/recursive trigger tree on prepared runtime arrays.

No function/vtable/IO substitution. Original getters, AP conditions and actions
execute. Arrays/active flags/AP/World are explicit prepared runtime data; this
does not execute the KLD parser, trigger loader, AP allocator or World scheduler.
"""
from pathlib import Path
import hashlib,json,struct,sys
import native_unicorn_keepertrace as cpu
import dk2_formats as fmt
from build_effect_semantics import EXE
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
WORLD,AP,CONDS,ACTIONS,OBS=0x10000000,0x10010000,0x10020000,0x10030000,0x10070000
SESSION=0x10040000
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/maps/3DLevelTriggers.kld'
put=lambda b,o,v,n=4:b.__setitem__(slice(o,o+n),(v&((1<<(n*8))-1)).to_bytes(n,'little'))
sha=lambda b:hashlib.sha256(b).hexdigest()

def node(ident,kind,repeat=0,child=0,nxt=0,active=1,param=1):
    b=bytearray(20);b[0]=param;put(b,8,ident,2);put(b,10,nxt,2);put(b,12,child,2);b[14]=kind;b[15]=repeat;put(b,16,active)
    return b

def cases():
    rows=[dict(name=f'flags{f:02x}-repeat{r}',flags=f,repeat=r) for f in [0,4,8,12] for r in [0,1,2,255]]
    rows += [dict(name='false-condition',false=True),dict(name='inactive-condition',inactive_condition=True),
        dict(name='inactive-action',inactive_action=True),dict(name='missing-root-falls-back',root=1777),
        dict(name='missing-action-falls-back',missing_action=True),dict(name='duplicate-condition-first',duplicate_condition=True),
        dict(name='duplicate-action-first',duplicate_action=True),dict(name='nested-actions',nested_actions=True),
        dict(name='nested-condition-repeat2',nested_condition=True),dict(name='direct-negative-root',root=0xFFF7),
        dict(name='zero-root',root=0),dict(name='zero-bind-preserves-old',root=0,old_trigger=7,old_cache=2),
        dict(name='action-repeat255-parent1',action_repeat=255),dict(name='action-repeat1-parent2',repeat=2,action_repeat=1)]
    rows += [dict(name=f'original-3DLevel-clock{v}',source_3D=True,clock=v,root=1,gate=0xA7,flags=12) for v in [0,1,0x80000000]]
    return rows

def initial(c):
    repeat=c.get('repeat',1)
    conds=[node(7,0x28 if c.get('false') else 0x29,repeat,0xFFF7,active=0xA4 if c.get('inactive_condition') else 0xA5,param=4)]
    # CLAIM_PART: operator4 means count > threshold; area6 > threshold6 is false.
    if c.get('false'):put(conds[0],4,6)
    actions=[node(0xFFF7,44,c.get('action_repeat',0),active=0xA4 if c.get('inactive_action') else 0xA5)]
    if c.get('missing_action'):put(conds[0],12,0xFFF0,2)
    if c.get('duplicate_condition'):conds.append(node(7,0x29,repeat,0xFFF6))
    if c.get('duplicate_action'):actions.append(node(0xFFF7,45,0))
    if c.get('nested_actions'):
        put(actions[0],12,0xFFF6,2);put(actions[0],10,0xFFF5,2)
        actions += [node(0xFFF6,45),node(0xFFF5,51)]
    if c.get('nested_condition'):
        put(conds[0],12,8,2);conds.append(node(8,0x29,2,0xFFF7))
    if c.get('source_3D'):
        s=next(s for s in fmt.parse(SOURCE.read_bytes()) if s.signature==0xD2)
        assert [r[8:].hex() for r in s.records]==['040000000000000001000000ffff2201','0000000000000000ffff000000002d00']
        conds=[bytearray(s.records[0][8:])+bytearray(struct.pack('<I',0xA5))]
        actions=[bytearray(s.records[1][8:])+bytearray(struct.pack('<I',0xA5))]
    world=bytearray(0x7400);put(world,0,0x66E3EC);put(world,0x7308,CONDS);put(world,0x730C,ACTIONS)
    put(world,0x7314,len(conds),2);put(world,0x7318,len(actions),2);world[0x72A1]=c.get('gate',0xA0)
    ap=bytearray(64);ap[0x32]=c.get('flags',0);put(ap,0x34,c.get('old_trigger',0),2);ap[0x36]=c.get('old_cache',0)
    struct.pack_into('<4H',ap,0x38,2,3,4,4)
    mem={WORLD:world,AP:ap,CONDS:b''.join(conds)+bytes([0xA5])*32,ACTIONS:b''.join(actions)+bytes([0xA5])*32,
        0x6E5050:struct.pack('<I',WORLD),0:bytes([0])*4+bytes([0xA5])*28,OBS:bytes([0xCC]),
        0x7FFEE000:bytes([0xA5])*0x4000}
    if c.get('source_3D'):
        put(world,4,SESSION);session=bytearray(16);put(session,4,c['clock']);mem[SESSION]=session
    return mem,conds,actions

def oracle(c):
    mem,condbytes,actionbytes=initial(c);conds=[bytearray(b) for b in condbytes];actions=[bytearray(b) for b in actionbytes]
    ap=mem[AP];root=c.get('root',7);events=[];conditions=0;calls=0
    def u16(b,o):return int.from_bytes(b[o:o+2],'little')
    def lookup(rows,key):return next((b for b in rows if u16(b,8)==key),rows[0])
    if root:
        put(ap,0x34,root,2);ap[0x36]=lookup(conds,root)[15]
    def walk(key,once):
        nonlocal conditions,calls
        assert calls<100;calls+=1
        while key:
            if key<0x8000:
                b=lookup(conds,key)
                if b[16]&1:
                    conditions+=1
                    condition_true=b[14]==0x29 or (b[14]==34 and 0<c['clock']<0x80000000)
                    if condition_true:walk(u16(b,12),b[15])
            else:
                b=lookup(actions,key)
                if b[16]&1:
                    mask={44:1,45:2,51:4}[b[14]]
                    mem[WORLD][0x72A1]=(mem[WORLD][0x72A1]&~mask)|(mask if b[0]==1 else 0)
                    events.append([b[14],mem[WORLD][0x72A1]])
                    if once==1:put(b,16,int.from_bytes(b[16:20],'little')&~1)
                    walk(u16(b,12),once)
            key=u16(b,10)
        # Native AP recursion returns0 for every ordinary return.
        return 0
    for _ in range(3):
        if u16(ap,0x34):walk(u16(ap,0x34),ap[0x36])
    mem[CONDS]=b''.join(conds)+bytes([0xA5])*32;mem[ACTIONS]=b''.join(actions)+bytes([0xA5])*32
    return {a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000},dict(world_gate=mem[WORLD][0x72A1],action_events=events,condition_calls=conditions,tree_calls=calls,
        root=u16(ap,0x34),cached_repeat=ap[0x36],condition_repeat=[b[15] for b in conds],action_repeat=[b[15] for b in actions],
        condition_active=[int.from_bytes(b[16:20],'little') for b in conds],action_active=[int.from_bytes(b[16:20],'little') for b in actions])

def build(c):
    mem,_,_=initial(c)
    before=[dict(address='0x4b0c90',registers=dict(ECX=hex(AP)),stack_args=[c.get('root',7)])]
    before += [dict(address='0x4b05e0',registers=dict(ECX=hex(AP)),stack_args=[]) for _ in range(2)]
    args=dict(program='DKII-DX.exe',address='0x4b05e0',registers=json.dumps(dict(ECX=hex(AP))),stack_args=[],before_calls=before,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=100000,
        observe_reads=[[AP,64],[CONDS,len(mem[CONDS])],[ACTIONS,len(mem[ACTIONS])],[0,4096]],
        observe_writes=[[WORLD+0x72A1,1],[AP,64],[CONDS,len(mem[CONDS])],[ACTIONS,len(mem[ACTIONS])]])
    return args,{a:bytes(b) for a,b in mem.items() if a!=0x7FFEE000}

def execute(c):
    args,checks=build(c);backend=sha(Path(cpu.__file__).read_bytes());tag=sha((json.dumps(args,sort_keys=True)+backend).encode())[:12];p=ROOT/'ghidra'/f'phase156-AP-tree-native-{tag}.json'
    if p.exists():d=json.loads(p.read_text(encoding='utf8'));assert d['input']==args and d['backend_sha256']==backend
    else:
        d=dict(case=c,input=args,backend_sha256=backend,result=cpu.run(EXE,args,list(checks.items()),OBS,WORLD+0x72A1,1));p.write_text(json.dumps(d,indent=2),encoding='utf8')
    return args,checks,d,p

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==3 and all(b['returned'] for b in r['before_calls'])
    assert int(r['registers']['EAX'],16)==1
    post=post_context(checks,r);wanted,out=oracle(c)
    assert post==wanted,[(hex(a),[(i,x,y) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:12]) for a in post if post[a]!=wanted[a]]
    v=r['visited_instructions'];assert v.get('0x4b05e0',0)==3 and v.get('0x4b0d70',0)==out['condition_calls'] and v.get('0x518ad0',0)==len(out['action_events'])
    assert v.get('0x4b0cc0',0)==out['tree_calls'] and v.get('0x508640',0)==0
    events=[w['value'] for w in r['observed_writes'] if int(w['address'],16)==WORLD+0x72A1]
    assert events==[v for _,v in out['action_events']]
    # No selected AP path reads the flags byte; writes likewise only bind fields.
    assert not any(int(x['address'],16)<=AP+0x32<int(x['address'],16)+x['size'] for x in r['observed_reads'])
    low=[x for x in r['observed_reads'] if int(x['address'],16)<4096];assert all(int(x['address'],16)==0 and x['size']==4 for x in low)
    out.update(compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'],flags_byte_reads=0,repeat_decrement_calls=0)
    return post,out

def main():
    selected=cases();rows=[]
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    for c in selected:
        args,ch,d,p=execute(c);_,out=verify(c,args,ch,d['result']);rows.append(dict(case=c,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),outcome=out));print(c['name'],out['world_gate'],len(out['action_events']),'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase156-AP-tree-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
