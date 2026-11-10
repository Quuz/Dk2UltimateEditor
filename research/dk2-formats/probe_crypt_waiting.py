"""Phase187: original occupied-Crypt waiting, action76 and participant exit.

Frozen182 source/NAV/renderer scene. Explicit original entry for a second living
participant and optional native Dancer construction are declared conditions.
No callback, state after entry, action, or vtable is replaced.
"""
from pathlib import Path
import json,struct
import probe_crypt_movement as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u
ACTOR,OTHER,CONTEXT,ROOM,LOG,STRIDE=old.ACTOR,old.old.RECIPIENT,old.CONTEXT,old.ROOM,old.LOG,old.STRIDE

def cases():
    return [dict(name='occupied-no-dancers',occupied=True,dancers=0,ai_steps=1),
        dict(name='occupied-dancer',occupied=True,dancers=1,ai_steps=2),
        dict(name='occupied-dancer-repeat',occupied=True,dancers=1,ai_steps=8),
        dict(name='occupied-dancer-release',occupied=True,dancers=1,ai_steps=2,release_other=True),
        dict(name='prepared-free276',occupied=False,dancers=0,ai_steps=1,entry_state=276)]

def build(c):
    cfg=dict(species=5,owner=7,entry_state=c.get('entry_state',274),register=False,activate=False,tick=c['ai_steps']>1,
        exit=False,native_nav=True,HP=900,room_area=True,null_renderer_cache=True,
        registration='ai',register_dispatch=True,movement_steps=c['ai_steps']-1,spare_blocks=24)
    args,checks,offsets=old.build(cfg)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    calls=args['before_calls'];at=next(i for i,x in enumerate(calls) if x['address']=='0x476d30' and x['registers']['ECX']==hex(CONTEXT) and x['stack_args']==[cfg['entry_state'],0])
    before=[]
    if c['occupied']:
        # Recipient100 is a real initialized Vampire8; attach through its
        # original category entry rather than forging room links/tag fields.
        before += [dict(address='0x476d30',registers=dict(ECX=hex(OTHER+0x24E)),stack_args=[275,0]),
            dict(address='0x4d8e60',registers=dict(ECX=hex(ROOM)),stack_args=[100])]
    if c['dancers']:
        # Explicit count1 is a branch condition, not a Vampire-type bonus.
        # Full original factory/room lists/pool allocation produce Object143.
        before.append(dict(address='0x4d9410',registers=dict(ECX=hex(ROOM)),stack_args=[c['dancers']]))
    calls[at:at]=before
    if c.get('release_other'):
        # After action snapshot3, exit the active participant, not the waiter.
        at=next(i for i,x in enumerate(calls) if x['address']==hex(old.CONTROL+256*3))
        calls.insert(at+1,dict(address='0x476d30',registers=dict(ECX=hex(OTHER+0x24E)),stack_args=[1,0]))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    return args,checks,offsets

def execute(c):
    args,ch,offsets=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha(Path(old.__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase187-crypt-waiting-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.old.prior.old.base.cpu.run(EXE,args,list(ch.items()),p.f.OBS,ACTOR,len(ch[ACTOR]))
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,ch,saved,file,offsets

def describe(c):
    args,ch,s,f,offsets=execute(c);r=s['result'];post=post_context(ch,r);rows=[]
    for i in range(6):
        b=post[LOG][i*STRIDE:(i+1)*STRIDE]
        at=lambda name:b[offsets[name][0]:offsets[name][0]+offsets[name][1]]
        a,other,room,owner=at('actor'),at('recipient'),at('room'),at('owner')
        rows.append(dict(index=i,state=u(a,0x25E),action=u(a,0x3A9),HP=u(a,0x40),roomtag=u(a,0x3B9,2),
            other_state=u(other,0x25E),other_action=u(other,0x3A9),otherHP=u(other,0x40),other_roomtag=u(other,0x3B9,2),
            registered=u(room,0x53,2),dancers=u(room,0x55),roomhead=u(room,0xA,2),counters=list(owner[0x108A:0x109C])))
    out=dict(case=c,success=r['success'],error=r['emulation_error'],failures=r['protected_write_or_execution_failures'],snapshots=rows,
        calls={hex(a):r['visited_instructions'].get(hex(a),0) for a in [0x483790,0x4839E0,0x496700,0x4D8E60,0x4D9570,0x4D9410]},
        tail=r['trace_tail'][-20:],file=str(f.relative_to(ROOT)))
    print(json.dumps(out),flush=True);return out

def main():
    rows=[describe(c) for c in cases()]
    (ROOT/'phase187-crypt-waiting-discovery.json').write_text(json.dumps(dict(replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
