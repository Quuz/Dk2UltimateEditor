"""Phase172 accepted XP7/D7 and pretargeted Instant34/D2,D7 compositions.

Full original raw loaders, 288 unchanged type65 source records for XP scenes,
original targeted Trap fire for Instant scenes, complete collision/Shot ticks.
The diagnostic Trap17 template and prepared snapshots are explicit context.
"""
from pathlib import Path
import json,math,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_instant_experience as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE=probe.EXE
sha=probe.sha
p=probe.target.p

def cases():
    xp=[('xp-source-mask1',{}),('xp-source-mask1-whole-tick',dict(root='tick')),
        ('xp-collision-level1',dict(mask=4)),('xp-full-health',dict(mask=4,HP=2500)),
        ('xp-no-normal-damage',dict(mask=4,mode_override=0)),('xp-unsigned-max',dict(mask=4,data1=0xFFFFFFFF)),
        ('xp-selector2',dict(mask=4,data1=2)),('xp-selector1-subtract',dict(mask=4,data1=1,XP=1000)),
        ('xp-selector0-subtract',dict(mask=4,data1=0,XP=1000)),
        ('xp-level2-subtract',dict(mask=4,data1=1,level=2,XP=5000)),
        ('xp-level2-gain',dict(mask=4,level=2,XP=1000)),('xp-level9-gain',dict(mask=4,level=9,HP=10000)),
        ('xp-level10-cap',dict(mask=4,level=10)),('xp-invulnerable445',dict(mask=4,HP=2500,invulnerable=1)),
        ('xp-tick2-abort',dict(mask=2,root='tick')),('xp-start64-no-target',dict(mask=64,root='tick'))]
    instant=[('instant-source-area',dict(mode=2)),('instant-area80',dict(mode=2,data1=80,data2=2)),
        ('instant-area-no-process',dict(mode=2,data1=80,data2=2,mask=0)),
        ('instant-area-no-target',dict(mode=2,data1=80,data2=2,no_fire=True)),
        ('instant-area-moved',dict(mode=2,data1=80,data2=2,target_dx=4096)),
        ('instant-area-moved-no-target',dict(mode=2,data1=80,data2=2,target_dx=4096,no_fire=True)),
        ('instant-area-invulnerable',dict(mode=2,data1=80,data2=2,invulnerable=1)),
        ('instant-area-repeat',dict(mode=2,data1=80,data2=2,pre_ticks=1)),
        ('instant-no-normal-damage',dict(mode=2,data1=80,data2=2,mode_override=0)),
        ('instant-source-hit-zero',dict(mode=7)),('instant-hit-collision-mask',dict(mode=7,data1=80)),
        ('instant-hit-tick-mask',dict(mode=7,data1=80,mask=2)),
        ('instant-hit-invulnerable',dict(mode=7,data1=80,mask=2,invulnerable=1)),
        ('instant-hit-no-target',dict(mode=7,data1=80,mask=2,no_fire=True)),
        ('instant-hit-dead-second-tick',dict(mode=7,data1=80,mask=2,pre_ticks=1)),
        ('instant-hit-live-repeat',dict(mode=7,data1=80,mask=2,flags=4,pre_ticks=1))]
    return [dict(process=7,HP=1000,**kw,name=name) for name,kw in xp if 'HP' not in kw]+[
        dict(process=7,**kw,name=name) for name,kw in xp if 'HP' in kw]+[dict(process=34,**kw,name=name) for name,kw in instant]

def execute(c):
    args,checks,node,actors=probe.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_instant_experience.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase172-instant-xp-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')
def signed(v):return v if v<0x80000000 else v-0x100000000

def reference(c,checks):
    raw=checks[p.shot.RAW];data1=u(raw,0x98);data2=u(raw,0x9C);mask=u(raw,0xA0);flags=u(raw,0xA8);mode=raw[0xC5]
    requests=[]
    if c['process']==7:
        HP=c['HP'];XP=c.get('XP',0);level=c.get('level',1);callback=False
        active=(mask&4)!=0 and c.get('root','collision')=='collision'
        if active:
            callback=True;definition_raw=checks[p.target.RAW]
            # Native-confirmed projection: rawCreature HP F0C ->692 and XP F00 ->6B5.
            basehp=u(definition_raw,0xF0C,2);basexp=u(definition_raw,0xF00,2)
            assert basehp==2500 and basexp==3000
            scales={}
            for off in range(0,len(checks[probe.LEVELS]),32):
                typ,stat,value,lvl=struct.unpack_from('<4I',checks[probe.LEVELS],off+16);assert typ==65
                scales[stat,lvl]=(value&65535) if value&65535<32768 else (value&65535)-65536
            threshold=basexp if level==1 else min(65534,max(0,scales[13,level]*basexp//100))
            if data1>1:
                if threshold and XP+threshold>=threshold:
                    if level<10:
                        maximum=basehp if level==1 else scales[1,level]*basehp//100
                        newmax=scales[1,level+1]*basehp//100
                        nextHP=((HP*4096//maximum)*newmax)>>12
                        requests.append(dict(target=p.target.attack.TARGET,delta=nextHP-HP,before=HP,caller='0x48b294'))
                        HP=nextHP;level+=1
                    XP=0
            else:XP=max(0,XP-threshold)
        aborted=c.get('root','collision')=='tick' and bool(mask&2)
        if not aborted and mode==7 and signed(data1)>0 and not c.get('invulnerable',0):
            requests.append(dict(target=p.target.attack.TARGET,delta=-data1,before=HP,caller='0x4ac58e'));HP-=data1
        if c.get('root','collision')=='tick':
            callbacks=int(bool(mask&64))+int(bool(mask&2))+int(not aborted and bool(mask&4))
            shotHP=u(raw,0xBC,2) if aborted else (-int(bool(flags&4)) if flags&2 else u(raw,0xBC,2)-int(bool(flags&4)))
        else:
            callbacks=int(active);shotHP=0 if flags&2 else u(raw,0xBC,2)
        return dict(HP=[HP],levels=[level],XP=[XP],requests=requests,callback_calls=callbacks,
            Data1=data1,Data2=data2,ShotHP=shotHP&0xFFFFFFFF,ShotTarget=0 if aborted else 101,ShotForced=0)
    actors=[probe.trap.p.base.cf.ACTOR]+probe.trap.p.area.TARGETS[:2]
    HP=[1000,1000,1000];tick_count=1+c.get('pre_ticks',0);shotHP=u(raw,0xBC,2);callbacks=0;forced=0
    targeted=not c.get('no_fire');targettag=c.get('target_tag',2) if targeted else 0
    for tick in range(tick_count):
        if signed(shotHP)<=0:continue
        if tick==0 and mask&64:
            callbacks+=1
            if targeted:forced=1
            else:shotHP=0
        if mask&2:
            callbacks+=1
            if targeted:forced=1
            else:shotHP=0
        collision=targeted
        if collision:
            forced=0
            if mask&4:callbacks+=1;forced=1
            if mode==7 and signed(data1)>0 and not c.get('invulnerable',0):
                requests.append(dict(target=actors[1],delta=-data1,before=HP[1],caller='0x4ac58e'));HP[1]-=data1
            if flags&2:shotHP=0
        if mode==2:
            working=data1;radius=data2*4096
            for i in [1,2]:
                if i==1 and c.get('invulnerable',0):continue
                # Selected target is at the teleported origin when targeted.
                # Nulltarget keeps oldorigin, so scene target_dx enters distance.
                distance=(0 if targeted else c.get('target_dx',0))+(1024 if i==2 else 0)
                if radius<=0 or distance>=radius:continue
                working=(radius-distance)*working//radius
                requests.append(dict(target=actors[i],delta=-working,before=HP[i],caller='0x4ac8d2'));HP[i]-=working
        if flags&4:shotHP=(shotHP-1)&0xFFFFFFFF
    return dict(HP=HP,levels=[1,1,1],XP=[0,0,0],requests=requests,callback_calls=callbacks,
        Data1=data1,Data2=data2,ShotHP=shotHP,ShotTarget=targettag,ShotForced=forced)

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);e=reference(c,checks)
    actors=[p.target.attack.TARGET] if c['process']==7 else [probe.trap.p.base.cf.ACTOR]+probe.trap.p.area.TARGETS[:2]
    out=dict(HP=[u(post[a],0x40) for a in actors],levels=[u(post[a],0x403,1) for a in actors],XP=[u(post[a],0x3FF) for a in actors],
        requests=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']])
    node=p.shot.NODE if c['process']==7 else probe.trap.shot_node(dict(trap_fire=True))
    shot=next(b[node-a:node-a+371] for a,b in post.items() if a<=node<node+371<=a+len(b))
    out.update({k:u(shot,o,2 if k=='ShotTarget' else 4) for k,o in [('Data1',0x13D),('Data2',0x141),('ShotHP',0x40),('ShotTarget',0x147),('ShotForced',0x14D)]})
    callback=0x4AE630 if c['process']==7 else 0x4AFCB0
    out['callback_calls']=r['visited_instructions'].get(hex(callback),0)
    assert out==e,(c['name'],out,e)
    for a in [p.shot.RAW,p.target.RAW,p.RAWSPELL,0x66D524,0x66D754,0x66D86C,0x66E3EC,0x670574]:
        if a in checks:assert post[a]==checks[a],(c['name'],hex(a))
    if c['process']==7:
        assert post[probe.LEVELS]==checks[probe.LEVELS] and r['visited_instructions'].get('0x51dc40',0)==288
    else:
        assert post[probe.trap.TRAW]==checks[probe.trap.TRAW]
        assert r['visited_instructions'].get('0x4a7fd0',0)==int(not c.get('no_fire'))
        assert u(post[readable.COUNTER],4)==0
        if not c.get('no_fire') and u(checks[p.shot.RAW],0xA0)&2:
            # Real Instant callback writes forcedflag1; originalcollision clearsit.
            writes=[x for x in r['observed_writes'] if int(x['address'],16)==node+0x14D]
            assert any(x['value']==1 and 0x4AFCB0<=int(x['pc'],16)<0x4AFD20 for x in writes),c['name']
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    out.update(events=r['process_events'],full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])
    return post,out

# Explicit OS read probe from the source scene; its implementation is not gameplay.
readable=probe.readable

def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],out['levels'],out['XP'],'ShotHP',out['ShotHP'],flush=True)
    if len(selected)==len(cases()):(ROOT/'phase172-instant-xp-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
