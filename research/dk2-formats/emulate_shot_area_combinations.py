"""Accepted phase169 area pairs through original complete Shot updates.

Wind17, Inferno30, Gas31 and bounded friendly-target Jack39 paths. Full
source records and explicit diagnostic variants; no instruction replacement.
"""
from pathlib import Path
import json,math,struct,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_area_combinations as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha
EXE=p.EXE
HANDLERS={17:0x4AE990,30:0x4AF660,31:0x4AF8E0,39:0x4B02E0}

def cases():
    seed=dict(process=30,mode=2,data1=80,data2=2,mask=2,pre_ticks=0,
        positions=[[0,0],[1024,0]],owners=[8,8],invulnerable=[0,0],order=[0,1])
    rows=[('none-range',dict(process=0)),('none-constant',dict(process=0,mode=1)),
        ('inferno-only',dict(mode=0)),('inferno-constant',dict(mode=1)),('inferno-range',{}),
        ('inferno-mask-miss',dict(mask=4)),('inferno-second-tick',dict(pre_ticks=1)),
        ('inferno-start-only',dict(mask=64)),('inferno-start-and-tick',dict(mask=66)),
        ('inferno-friendly',dict(owners=[7,8])),('inferno-invulnerable',dict(invulnerable=[1,0])),
        ('inferno-navigation-block',dict(blocked_cells=[[0,0]])),
        ('inferno-flying-excluded',dict(target_flag_set=0x20,target_flag_clear=0x2000)),
        ('inferno-flying-override',dict(target_flag_set=0x2020)),
        ('inferno-first-radius',dict(positions=[[6000,0],[4096,0]])),
        ('inferno-later-radius',dict(positions=[[6000,0],[4096,0]],pre_ticks=1)),
        ('gas-range',dict(process=31)),('gas-only',dict(process=31,mode=0)),
        ('gas-constant',dict(process=31,mode=1)),('gas-mask-miss',dict(process=31,mask=4)),
        ('gas-second-tick',dict(process=31,pre_ticks=1)),('gas-zero-damage',dict(process=31,data1=0)),
        ('wind-range',dict(process=17,data1=2)),('wind-only',dict(process=17,mode=0,data1=2)),
        ('wind-mask-miss',dict(process=17,data1=2,mask=4)),
        ('wind-zero-radius',dict(process=17,data1=0)),
        ('wind-invulnerable',dict(process=17,data1=2,invulnerable=[1,0])),
        ('wind-friendly',dict(process=17,data1=2,owners=[7,8])),
        ('wind-second-tick',dict(process=17,data1=2,pre_ticks=1)),
        ('jack-start-return-zero',dict(process=39,mask=64,jack_target=True,owners=[7,8])),
        ('jack-tick-return-zero',dict(process=39,mask=2,jack_target=True,owners=[7,8])),
        ('jack-both-events-zero',dict(process=39,mask=66,jack_target=True,owners=[7,8])),
        ('jack-repeated-tick-zero',dict(process=39,mask=2,jack_target=True,owners=[7,8],pre_ticks=1))]
    out=[dict(seed,**kw,name=name) for name,kw in rows]
    inventory=json.loads((ROOT/'phase168-shot-combinations-storage.json').read_text(encoding='utf-8'))
    for pair,index,label in [(17,0,'source-wind-zero'),(30,0,'source-inferno-constant'),(30,0,'source-inferno-start'),(30,1,'source-inferno-range'),(31,0,'source-gas-fart'),(31,1,'source-gas-missile'),(31,2,'source-gas-trap')]:
        mode=1 if label=='source-inferno-constant' else 2
        x=next(x for x in inventory['observed_pairs'] if (x['ProcessType'],x['DamageType'])==(pair,mode))['variants'][index]
        out.append(dict(seed,process=pair,mode=mode,data1=x['Data1'],data2=x['Data2'],mask=x['ProcessFlags'],name=label,
            source_record=dict(file=x['first_source'],record_index=x['first_record_index'])))
    return out

def execute(c):
    args,checks=p.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_area_combinations.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase169-area-combination-{c["name"]}-{tag}.json'
    if file.exists():
        saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=p.cpu.run(p.EXE,args,list(checks.items()),p.p.f.OBS,p.p.base.NODE,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def reference(c,checks):
    """Bounded integer prediction; normal range accumulator retains phase131 bug.

    Both native systems share the same parameters but have different filters,
    geometry and damage formulas. No overflow/large-map generalization.
    """
    base=p.p.base;area=p.p.area
    actors=[base.cf.ACTOR]+area.TARGETS[:2];origin=struct.unpack_from('<3i',checks[p.p.f.ARGS])
    pos=[struct.unpack_from('<2i',checks[a],0x1A) for a in actors]
    rel=[(x-origin[0],y-origin[1]) for x,y in pos]
    hp={a:1000 for a in actors};requests=[];events=[];normal=0;procs=0;decrements=0
    angles={a:int.from_bytes(checks[a][0xF4:0xF6],'little') for a in actors};wind_affected=set()
    flags=int.from_bytes(checks[base.target.RAW][0xEFC:0xF00],'little')
    maximum=int.from_bytes(checks[base.target.RAW][0xF0C:0xF0E],'little')
    owners=[7]+c['owners'];iv=[0]+c['invulnerable'];order={0:-1,**{i+1:n for n,i in enumerate(c['order'])}}
    def sorted_indices(axis=(1,0)):
        return sorted(range(3),key=lambda i:tuple(pos[i][a]>>12 for a in axis)+(order[i],))
    def request(i,damage,caller):
        a=actors[i];requests.append(dict(target=a,delta=-damage,before=hp[a],caller=caller));hp[a]-=damage
    def inside(i,r,closed=False):
        x,y=pos[i][0]>>12,pos[i][1]>>12;cx,cy=origin[0]>>12,origin[1]>>12
        return cx-r<=x<cx+r+int(closed) and cy-r<=y<cy+r+int(closed)
    def process(tick):
        nonlocal procs
        if not c['process']:return 1
        procs+=1
        if c['process']==39:
            assert c.get('jack_target') and c['owners'][0]==7
            return 0
        if c['process']==30:
            r=c['data2'];threshold=r*r*4096//(2 if tick==1 else 1)
            blocked={((origin[0]+dx)>>12,(origin[1]+dy)>>12) for dx,dy in c.get('blocked_cells',[])}
            for i in sorted_indices():
                if not inside(i,r,True) or (pos[i][0]>>12,pos[i][1]>>12) in blocked:continue
                if i and flags&0x20 and not flags&0x2000:continue
                dx,dy=rel[i]
                if (dx*dx>>12)+(dy*dy>>12)<threshold:request(i,c['data1'],'0x4af83d')
        elif c['process']==17:
            r=c['data1']
            for i in sorted_indices():
                if not inside(i,r):continue
                dx,dy=rel[i];ds=(dx*dx>>12)+(dy*dy>>12)
                if ds>=r*r*4096:continue
                distance=math.isqrt(ds*4096)
                angles[actors[i]]=(angles[actors[i]]+(r*0x600000)//(distance+r*4096))&2047
                wind_affected.add(actors[i])
                if owners[i]!=7:
                    damage=(maximum*(distance+r*4096)//(r*8192))*c['data2']//100
                    request(i,damage,'0x4aeb40')
        return 1
    def event(tick,mask):
        result=process(tick) if c['mask']&mask else 1
        events.append(dict(tick=tick,event_mask=mask,return_EAX=result))
        return result
    raw=checks[base.RAW];shotHP=int.from_bytes(raw[0xBC:0xBE],'little');decay=bool(int.from_bytes(raw[0xA8:0xAC],'little')&4)
    for tick in range(1,c['pre_ticks']+2):
        if tick==1:event(tick,64)
        if not event(tick,2):continue
        normal+=1
        if c['mode'] in [1,2]:
            working=c['data1'];r=c['data2']
            for i in sorted_indices((1,0) if c['mode']==2 else (0,1)):
                if owners[i]==7 or iv[i]:continue
                if not inside(i,r,c['mode']==2):continue
                if c['mode']==2:
                    if working==0:continue
                    dx,dy=rel[i];ds=(dx*dx>>12)+(dy*dy>>12)
                    if ds>=r*r*4096:continue
                    distance=math.isqrt(ds*4096);working=(r*4096-distance)*working//(r*4096)
                request(i,working,'0x4ac8d2' if c['mode']==2 else '0x4ac666')
        if not event(tick,8):continue
        if decay:shotHP-=1;decrements+=1
        if shotHP<1:event(tick,128)
    return dict(HP=hp,requests=requests,events=events,normal_entries=normal,process_entries=procs,shotHP=shotHP,
        initial_shot_HP=int.from_bytes(raw[0xBC:0xBE],'little'),decrements=decrements,angles=angles,wind_affected=sorted(wind_affected))

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);e=reference(c,checks);base=p.p.base
    actual=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert actual==e['requests'],(c['name'],actual,e['requests'])
    assert {a:int.from_bytes(post[a][0x40:0x44],'little') for a in e['HP']}==e['HP'],(c['name'],e)
    assert all(x['owner_u16']==7 and x['mode']==0 for x in r['hp_requests'])
    ev=[{k:x[k] for k in ['tick','event_mask','return_EAX']} for x in r['process_events']]
    assert ev==e['events'],(c['name'],ev,e['events'])
    v=r['visited_instructions'];ticks=c['pre_ticks']+1
    assert v.get('0x4ab180',0)==ticks and v.get('0x4ad1b0',0)==1
    assert v.get('0x4ac3c0',0)==v.get('0x4abd60',0)==e['normal_entries']
    for process,entry in HANDLERS.items():assert v.get(hex(entry),0)==(e['process_entries'] if c['process']==process else 0),(c['name'],hex(entry))
    assert v.get('0x4ac5a0',0)==(e['normal_entries'] if c['mode']==1 else 0)
    assert v.get('0x4ac6d0',0)==(e['normal_entries'] if c['mode']==2 else 0)
    node=post[p.p.f.BLOCK][base.NODE-p.p.f.BLOCK:base.NODE-p.p.f.BLOCK+371]
    assert int.from_bytes(node[0x40:0x44],'little')==e['shotHP']
    assert int.from_bytes(node[0x12D:0x131],'little')==ticks and int.from_bytes(node[0x149:0x14D],'little')==1
    assert int.from_bytes(node[0x13D:0x141],'little')==c['data1'] and int.from_bytes(node[0x141:0x145],'little')==c['data2']
    assert node.hex()==r['captured_hex'] and post[base.RAW]==checks[base.RAW] and post[base.target.RAW]==checks[base.target.RAW]
    mid=bytearray(b'\1\0\0\0'*34+checks[base.RAW]);mid[0xA7]=mid[0x170]=0
    assert post[base.MID]==mid and int.from_bytes(post[base.DEFINITION][0x33:0x37],'little')==sum(mid)
    assert post[base.SHOT_VTABLE]==checks[base.SHOT_VTABLE]
    d=post[base.DEFINITION];assert d[0x6C]==c['process'] and d[0x6B]==c['mode'] and int.from_bytes(d[0x3F:0x43],'little')==c['mask']
    assert int.from_bytes(post[base.target.attack.TARGET_DEF][0x686:0x68A],'little')==int.from_bytes(checks[base.target.RAW][0xEFC:0xF00],'little')
    assert node[0x1A:0x22]==checks[p.p.f.ARGS][:8],(c['name'],'unexpected XY movement')
    for actor,angle in e['angles'].items():
        assert int.from_bytes(post[actor][0xF4:0xF6],'little')==angle,(c['name'],hex(actor),'angle')
        if actor in e['wind_affected']:
            assert int.from_bytes(post[actor][0x3E6:0x3EA],'little')&0x8000000
            assert post[actor][0x25E:0x262]==checks[actor][0x25E:0x262]
    for process,entry in HANDLERS.items():
        offset=(process-1)*17+1
        assert int.from_bytes(post[0x69E860][offset:offset+4],'little')==entry
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.EXE.read_bytes()
    for x in r['observed_reads']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(p.p.area.pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    return post,dict(HP={hex(a):v for a,v in e['HP'].items()},requests=e['requests'],process_events=ev,
        native_steps=r['instruction_count'],process_entries=e['process_entries'],normal_entries=e['normal_entries'],shotHP=e['shotHP'],
        full_regions=len(post),full_bytes=sum(map(len,post.values())),random_calls=v.get('0x5558d0',0),
        wind_angles={hex(a):e['angles'][a] for a in e['wind_affected']},wind_current_state_unchanged=True)

def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,checks,saved,file=execute(c)
        if not saved['result']['success']:print(c['name'],saved['result']['emulation_error'],saved['result']['trace_tail'],flush=True)
        _,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],'process/normal',out['process_entries'],out['normal_entries'],'HP',out['shotHP'],'RNG',out['random_calls'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase169-area-combinations-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
