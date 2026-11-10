"""Whole original melee/raw loader controls for absolute versus facing angle."""
from pathlib import Path
import json,struct,math
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_melee_animation_flag as fixture
import native_unicorn_mapreader as cpu
import native_melee_direction as api
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXE=fixture.prior.EXE
sha=cpu.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
ACTOR,TARGET=fixture.cf.ACTOR,fixture.attack.TARGET


def cases():
    out=[]
    directions=[(0,-4096),(4096,-4096),(4096,0),(4096,4096),(0,4096),(-4096,4096),(-4096,0),(-4096,-4096)]
    for i,(dx,dy) in enumerate(directions):
        for facing in [0,512,1024,1536]:
            out.append(dict(name=f'd{i}-victim{facing}',dx=dx,dy=dy,target_heading=facing,attacker_heading=0))
    for i,(dx,dy) in enumerate([directions[0],directions[4]]):
        for facing in [512,1024,1536]:out.append(dict(name=f'attacker-d{i}-{facing}',dx=dx,dy=dy,target_heading=0,attacker_heading=facing))
    # Exact native table inputs straddling the wrap seam and activation gate.
    lookup=api.table(EXE.read_bytes());vectors=[(x*16,y*16) for x,y in [(1,-256),(-1,-256),(-2,-256),(-3,-256)]]
    for wanted in [319,320,321,1725,1726,1727,2045,2046,2047]:
        for i in range(257):
            candidates=[(i*16,-4096),(4096,-i*16),(-i*16,-4096),(-4096,-i*16)]
            match=next((p for p in candidates if api.angle(*p,lookup)==wanted),None)
            if match is not None:vectors.append(match);break
    for i,(dx,dy) in enumerate(dict.fromkeys(vectors)):
        out.append(dict(name=f'boundary-{i}',dx=dx,dy=dy,target_heading=0,attacker_heading=0))
    for i,(dx,dy) in enumerate([directions[0],directions[2],directions[4]]):
        out.append(dict(name=f'translated-{i}',dx=dx,dy=dy,target_heading=512,attacker_heading=1024,translate=[8192,4096]))
        out.append(dict(name=f'scaled-{i}',dx=dx*2,dy=dy*2,target_heading=1536,attacker_heading=512))
    return out


def build(c):
    args,_=fixture.build(dict(enabled=False,initial_runtime=False))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    put=fixture.f.put;actor,target=mem[ACTOR],mem[TARGET]
    # The complete original loader receives this deliberately edited copy.
    put(mem[fixture.prior.RAW],0xF14,100,2)
    put(target,0x40,10000);put(actor,0x403,1,1)
    put(actor,0xF4,c['attacker_heading'],2);put(target,0xF4,c['target_heading'],2)
    tx,ty=c.get('translate',[0,0])
    x,y=0x5800+tx,0x6800+ty
    for obj,px,py in [(target,x,y),(actor,x+c['dx'],y+c['dy'])]:put(obj,0x1A,px);put(obj,0x1E,py)
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[ACTOR+0xF4,2],[TARGET+0xF4,2],[ACTOR+0x1A,8],[TARGET+0x1A,8],[0,4096]]
    args['observe_writes'] += [[TARGET+0x40,4],[0,4096]]
    excluded=[0x7FFEE000,fixture.f.GFX_CB,fixture.f.SOUND_CB,fixture.attack.EVENT,fixture.prior.LOAD_GFX,fixture.prior.LOAD_SOUND]
    ch={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for a,n in [(0x66D524,0x130),(0x66D654,0x80),(0x66E3EC,0x3A0)]:ch[a]=pe_read(EXE.read_bytes(),a,n)
    return args,ch


def execute(c):
    args,ch=build(c);deps=sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_mapreader.py').read_bytes())
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12];file=ROOT/'ghidra'/f'phase194-direction-native-{c["name"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(ch.items()),fixture.f.OBS,TARGET,len(ch[TARGET]));saved=dict(case=c,input=args,result=r);dump(str(file.relative_to(ROOT)),saved)
    return args,ch,saved,file


def verify(c,args,ch,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==7 and all(x['returned'] for x in r['before_calls'])
    post=post_context(ch,r);a=api.angle(c['dx'],c['dy'],api.table(EXE.read_bytes()));m=api.modifier(a);d=api.damage(100,a)
    assert int.from_bytes(post[TARGET][0x40:0x44],'little')==10000-d,(c['name'],a,d,int.from_bytes(post[TARGET][0x40:0x44],'little'))
    assert int.from_bytes(post[fixture.cf.DEFINITION][0x5C4:0x5C6],'little')==100
    assert post[fixture.prior.RAW]==ch[fixture.prior.RAW]
    for obj,h in [(ACTOR,c['attacker_heading']),(TARGET,c['target_heading'])]:
        assert int.from_bytes(post[obj][0xF4:0xF6],'little')==h
        assert post[obj][0x1A:0x22]==ch[obj][0x1A:0x22]
    reads=[x for x in r['observed_reads'] if int(x['address'],16) in [ACTOR+0xF4,TARGET+0xF4]]
    assert not reads,(c['name'],reads)
    hp=[x for x in r['observed_writes'] if int(x['address'],16)==TARGET+0x40]
    assert len(hp)==1 and hp[0]['pc']=='0x4b2f03' and hp[0]['value']==10000-d
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(data,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    assert r['visited_instructions'].get('0x498e30')==r['visited_instructions'].get('0x65fb54')==r['visited_instructions'].get('0x4b2ce0')==1
    return post,dict(m,damage_HP=d,HP=10000-d,facing_reads=0,headings_and_positions_unchanged=True,
                     full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])


def main():
    rows=[]
    for c in cases():
        args,ch,saved,file=execute(c);_,out=verify(c,args,ch,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['angle'],out['damage_HP'],flush=True)
    dump('phase194-direction-emulations.json',dict(phase=194,replays=rows))


if __name__=='__main__':main()
