"""Phase178 discovery: native Crypt setter with a nonempty Creature list.

Original initialized Creature snapshots and raw loaders; explicit room/member
records and correct bidirectional room links. No object destruction branch.
"""
from pathlib import Path
import json,sys
import probe_shot_target_combinations as base
import native_room_counters as counters
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE=base.EXE
p=base.p
sha=base.sha
ROOM,MEMBER,NEW_MEMBER=0x20800000,0x20810000,0x20820000

def build(c):
    args,_=base.build(dict(process=0,mode=0))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    roomtag,membertag,newtag=300,301,302
    actors=[p.target.attack.TARGET] if c.get('occupants',1) else []
    if c.get('occupants',1)==2:actors.append(p.cf.ACTOR)
    room=bytearray(0xC0)+bytearray([165])*64
    p.f.put(room,4,roomtag,2);room[0x18]=26;p.f.put(room,0x1D,c.get('room_owner',7),2)
    p.f.put(room,0x53,membertag if c.get('old_registered',True) else 0,2);p.f.put(room,0x55,1234);p.f.put(room,0x6B,100,2);p.f.put(room,0x6D,100,2)
    if actors:p.f.put(room,0xA,int.from_bytes(mem[actors[0]][4:6],'little'),2)
    mem[ROOM]=room
    for addr,tag,species in [(MEMBER,membertag,c.get('old_species',19)),(NEW_MEMBER,newtag,c.get('new_species',5))]:
        b=bytearray(0x380)+bytearray([165])*64;p.f.put(b,4,tag,2);b[0x36F]=species;mem[addr]=b
    for i,actor in enumerate(actors):
        p.f.put(mem[actor],0x236,int.from_bytes(mem[actors[i-1]][4:6],'little') if i else 0,2)
        p.f.put(mem[actor],0x238,int.from_bytes(mem[actors[i+1]][4:6],'little') if i+1<len(actors) else 0,2)
        p.f.put(mem[actor],0x3B9,roomtag,2)
    for addr,tag in [(ROOM,roomtag),(MEMBER,membertag),(NEW_MEMBER,newtag)]:
        p.f.put(mem[0x6EE540],tag*4,addr);mem[0x6ED540][tag]=1
    owner=int.from_bytes(mem[0x6EE540][c.get('room_owner',7)*4:c.get('room_owner',7)*4+4],'little')
    assert owner in mem
    mem[owner][0x108A:0x109C]=bytes(18)
    if c.get('old_registered',True):mem[owner][counters.counter_slot(c.get('old_species',19))]=1
    if c.get('enter_state'):
        args['before_calls'] += [dict(address='0x491830',registers=dict(ECX=hex(a)),stack_args=[c['enter_state']]) for a in actors]
    # Keep full original loaders/AI context; the inherited final collision is
    # harmless P0/D0 and returns before this original room-setter root.
    args['before_calls'].append(dict(address='0x4d8e60',registers=dict(ECX=hex(ROOM)),stack_args=[newtag if c.get('replace') else 0]))
    args['address']='0x4e52f0';args['registers']=json.dumps(dict(ECX=hex(ROOM)))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[ROOM,len(room)]]
    args['observe_writes'] += [[ROOM,len(room)]]+[[a,len(mem[a])] for a in actors]
    args['max_steps']=2000000
    excluded={0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,0x28CFA4}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D754,0x100),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:checks[a]=p.pe_read(EXE.read_bytes(),a,n)
    return args,checks,actors,owner

def execute(c):
    args,checks,actors,owner=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase178-crypt-cleanup-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=base.cpu.run(EXE,args,list(checks.items()),p.f.OBS,ROOM,0xC0);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file,post_context(checks,saved['result']),actors,owner

def describe(c):
    args,ch,s,f,post,actors,owner=execute(c);r=s['result'];u=lambda b,o,n=2:int.from_bytes(b[o:o+n],'little')
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],room_head=u(post[ROOM],0xA),room_tag=u(post[ROOM],0x53),room55=u(post[ROOM],0x55,4),
        counters=list(post[owner][0x108A:0x109C]),actors=[dict(address=a,state=u(post[a],0x25E,4),room=u(post[a],0x3B9),previous=u(post[a],0x236),next=u(post[a],0x238)) for a in actors],
        tail=r['trace_tail'][-16:],before=r['before_calls'][-2:])),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
