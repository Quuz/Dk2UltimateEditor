"""Raw Creature/Shot loaders, native Shot factory and multi-target area damage.

Extra targets are explicit copies of the prepared native Creature snapshot,
with tags/positions and a valid doubly linked cell list. They share the loaded
Creature definition; new Creature factories/full world scheduler are not run.
Optional lifetime changes use original generic HP method4B2CE0 on the Shot.
"""
from pathlib import Path
import hashlib,json,math,struct,sys
import probe_shot_hit as base
import native_aoe_trace as cpu
from emulate_resistance import post_context
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
EXTRAS=[0x1B000000,0x1B010000]
TARGETS=[base.target.attack.TARGET]+EXTRAS
TAGS=[101,103,104]
SESSION,GFX=0x1B020000,0x1B030000
LOG,LOG_TEMP,LOG_OBJECT=0x740C20,0x7426A0,0x7428A0

def sha(b):return hashlib.sha256(b).hexdigest()
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(n*8))-1)).to_bytes(n,'little')


def cases():
    seed=dict(mode=2,damage=80,data2=2,positions=[[0,0],[1024,0]],order=[0,1],value=100,
        kind='creature',owners=[8,8],invulnerable=[0,0],health_delta=0,repeats=1)
    variations=[('range-centre-first',{}),('range-centre-last',dict(order=[1,0])),
        ('constant-centre-first',dict(mode=1)),('constant-centre-last',dict(mode=1,order=[1,0])),
        ('time-half-health',dict(mode=3,data2=8192,health_delta=-16)),
        ('ring-half-health',dict(mode=4,data2=8192,health_delta=-16)),
        ('range-three-far-first',dict(positions=[[0,0],[1024,0],[1536,0]],order=[1,2,0])),
        ('range-three-centre-first',dict(positions=[[0,0],[1024,0],[1536,0]],order=[0,2,1])),
        ('range-opposite-cells',dict(positions=[[-4096,0],[4096,0]],order=[1,0])),
        ('range-cell-scan-y-first',dict(positions=[[-4096,4096],[4096,-4096],[0,0]],order=[1,2,0])),
        ('range-repeat-dispatch',dict(repeats=2)),
        ('range-friendly-first',dict(positions=[[1024,0],[0,0]],owners=[7,8])),
        ('range-invulnerable-first',dict(positions=[[1024,0],[0,0]],invulnerable=[1,0])),
        ('range-resistance-zero',dict(value=0,order=[1,0])),
        ('range-radius-zero',dict(data2=0)),
        ('range-outside-circle',dict(positions=[[-7168,-7168],[0,0]])),
        ('constant-outside-circle',dict(mode=1,positions=[[-7168,-7168],[0,0]])),
        ('range-radius-boundary',dict(positions=[[8192,0],[0,0]])),
        ('time-full-health',dict(mode=3,data2=8192)),
        ('time-quarter-health',dict(mode=3,data2=8192,health_delta=-24)),
        ('time-subcell-data2',dict(mode=3,data2=4095,health_delta=-16)),
        ('ring-initial-health',dict(mode=4,data2=8192)),
        ('ring-subcell-data2',dict(mode=4,data2=2,health_delta=-16)),
        ('ring-filled-square',dict(mode=4,data2=16384,health_delta=-16,positions=[[-7168,-7168],[0,0]])),
        ('boulder-range',dict(kind='boulder')),
        ('boulder-range-below-four',dict(kind='boulder',damage=3)),
        ('boulder-constant',dict(kind='boulder',mode=1))]
    out=[]
    for name,kw in variations:
        case=dict(seed,**kw,name=name);n=len(case['positions'])
        if 'owners' not in kw:case['owners']=[8]*n
        if 'invulnerable' not in kw:case['invulnerable']=[0]*n
        out.append(case)
    return out


def build(case):
    basic=dict(next(c for c in base.cases() if c['name']=='range-res0'))
    basic.update(damage_type=case['mode'],damage=case['damage'],radius=case['data2'],category=6,res_type=6,value=case['value'],shot_dx=0,kind=case['kind'])
    args,_=base.build(basic)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # Frozen combat fixture extends World past its early Session/GFX buffers.
    # Move those two objects and update their actual pointers/loader argument;
    # native code, vtables and all object contents stay unchanged.
    mem[SESSION]=mem.pop(base.f.SESSION);mem[GFX]=mem.pop(base.f.GFX)
    put(mem[base.f.WORLD],4,SESSION);put(mem[SESSION],0x10,GFX)
    count=len(case['positions']);assert count in [2,3] and sorted(case['order'])==list(range(count))
    origin=struct.unpack_from('<3i',mem[base.f.ARGS],0)
    initial=bytes(mem[TARGETS[0]])
    for i in range(count):
        mem[TARGETS[i]]=bytearray(initial)
        put(mem[TARGETS[i]],4,TAGS[i]);put(mem[TARGETS[i]],0x28,case['owners'][i],2)
        put(mem[TARGETS[i]],0x2A,base.OTHEROWNER if case['owners'][i]==8 else base.f.PLAYER)
        put(mem[TARGETS[i]],0x445,case['invulnerable'][i],2)
        struct.pack_into('<3i',mem[TARGETS[i]],0x1A,origin[0]+case['positions'][i][0],origin[1]+case['positions'][i][1],origin[2])
        put(mem[0x6EE540],TAGS[i]*4,TARGETS[i]);mem[0x6ED540][TAGS[i]]=1
    for o in range(0,len(mem[base.f.MAP]),16):put(mem[base.f.MAP],o,0,2)
    entities=[(base.cf.ACTOR,100)]+[(TARGETS[i],TAGS[i]) for i in case['order']]
    cell_tails={}
    for address,tag in entities:
        put(mem[address],0xA,0,2);put(mem[address],0xC,0,2)
        x,y=struct.unpack_from('<2i',mem[address],0x1A);x>>=12;y>>=12
        assert 0<=x<32 and 0<=y<32
        cell=(y*32+x)*16
        if cell not in cell_tails:put(mem[base.f.MAP],cell,tag,2)
        else:
            previous,prevtag=cell_tails[cell];put(mem[previous],0xC,tag,2);put(mem[address],0xA,prevtag,2)
        cell_tails[cell]=(address,tag)
    mem[LOG]=bytearray(512);mem[LOG_TEMP]=bytearray(512);mem[LOG_OBJECT]=bytearray(0x401C)
    calls=args['before_calls'][:9]
    assert calls[7]['address']=='0x4acdf0' and calls[7]['stack_args'][1]==base.f.GFX
    calls[7]=dict(calls[7],stack_args=[base.MID,GFX])
    if case['health_delta']:
        calls.append(dict(address='0x4b2ce0',registers={'ECX':hex(base.NODE)},stack_args=[case['health_delta']&0xFFFFFFFF,7,0]))
    calls += [dict(address='0x4ac3c0',registers={'ECX':hex(base.NODE)},stack_args=[]) for _ in range(case['repeats'])]
    args['before_calls']=calls
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['max_steps']=1000000
    args['observe_reads'] += [[a,len(mem[a])] for a in TARGETS[:count]]
    args['observe_writes'] += [[a,len(mem[a])] for a in TARGETS[:count]]+[[LOG,512],[LOG_TEMP,512],[LOG_OBJECT,0x401C]]
    spans=sorted((a,len(b)) for a,b in mem.items())
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])), [(hex(a),n,hex(b),m) for (a,n),(b,m) in zip(spans,spans[1:]) if a+n>b]
    excluded=[0x7FFEE000,base.f.GFX_CB,base.f.SOUND_CB,base.target.attack.EVENT,base.target.LOAD_GFX,base.target.LOAD_SOUND]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    checks[base.SHOT_VTABLE]=pe_read(EXE.read_bytes(),base.SHOT_VTABLE,0x100)
    return args,checks


def path_for(args):
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_aoe_trace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase131-area-native-{tag}.json'


def signed(v):return v if v<0x80000000 else v-0x100000000
def trunc_div(a,b):return (abs(a)//abs(b))*(-1 if (a<0)!=(b<0) else 1)


def reference(case,checks):
    """Independent integer model for selected bounded positive damage inputs.

    Includes the observed native shared accumulator for mode2, not the intended
    independent-per-target falloff. No overflow/negative-radius generalization.
    """
    origin=struct.unpack_from('<3i',checks[base.f.ARGS]);cx,cy=origin[0]>>12,origin[1]>>12
    hp=[1000]*len(case['positions']);requests=[];health=32+case['health_delta'];logs=[]
    assert case['damage']>0 and 0<health<=32
    if case['health_delta']:requests.append((base.NODE,case['health_delta'],32))
    mode=case['mode'];radius=case['data2'] if mode in [1,2] else case['data2']>>12
    if mode==4:radius=(32-health)*radius//32
    order_index={i:n for n,i in enumerate(case['order'])}
    axis=(1,0) if mode==2 else (0,1)
    order=sorted(range(len(hp)),key=lambda i:tuple((origin[a]+case['positions'][i][a])>>12 for a in axis)+(order_index[i],))
    for _ in range(case['repeats']):
        working=case['damage']
        for i in order:
            dx,dy=case['positions'][i];x=(origin[0]+dx)>>12;y=(origin[1]+dy)>>12
            if mode==2:
                if not max(0,cx-radius)<=x<=min(31,cx+radius) or not max(0,cy-radius)<=y<=min(31,cy+radius):continue
                if case['owners'][i]==7 or (case['kind']=='creature' and case['invulnerable'][i]):continue
                candidate=working if case['kind']=='creature' else trunc_div(working,4)
                if candidate==0:continue
                distance_sq_q12=(dx*dx>>12)+(dy*dy>>12)
                if not distance_sq_q12 < ((radius*4096)**2>>12):continue
                distance=math.isqrt(distance_sq_q12*4096)
                working=trunc_div((radius*4096-distance)*working,radius*4096)
                damage=working
            else:
                if not cx-radius<=x<cx+radius or not cy-radius<=y<cy+radius:continue
                if case['kind']=='creature' and (case['owners'][i]==7 or case['invulnerable'][i]):continue
                if case['kind']=='boulder' and mode in [3,4] and case['owners'][i]==7:continue
                damage=case['damage']
                if mode==1 and case['kind']=='boulder':damage=trunc_div(damage,2)
                if mode==3:damage=trunc_div(damage*health,32)
            requests.append((TARGETS[i],-damage,hp[i]));hp[i]-=damage
            if mode in [3,4] and case['kind']=='creature':logs += [dict(damage=damage),dict(health=hp[i])]
    return dict(HP=hp,requests=requests,shot_health=health,log_lines=logs,extent_cells=radius)


def verify(case,args,checks,result):
    assert result['success'] and result['hit_return'] and not result['emulation_error']
    assert not result['protected_write_or_execution_failures'] and result['function_or_vtable_substitutions']==0
    assert len(result['before_calls'])==len(args['before_calls']) and all(c['returned'] for c in result['before_calls'])
    post=post_context(checks,result);expected=reference(case,checks)
    assert [(r['target'],r['delta'],r['HP_before']) for r in result['hp_requests']]==expected['requests'],case['name']
    assert all(r['owner_u16']==7 and r['mode']==0 for r in result['hp_requests'])
    actual_hp=[int.from_bytes(post[a][0x40:0x44],'little',signed=True) for a in TARGETS[:len(case['positions'])]]
    assert actual_hp==expected['HP']
    assert post[base.RAW]==checks[base.RAW] and post[base.target.RAW]==checks[base.target.RAW]
    mid=bytearray(b'\1\0\0\0'*34+checks[base.RAW]);mid[0xA7]=mid[0x170]=0
    assert post[base.MID]==mid
    definition=post[base.DEFINITION]
    assert definition[0x51]==6 and definition[0x6B]==case['mode'] and int.from_bytes(definition[0x33:0x37],'little')==sum(mid)
    assert int.from_bytes(definition[0x37:0x3B],'little')==case['damage'] and int.from_bytes(definition[0x3B:0x3F],'little')==case['data2']
    node=post[base.f.BLOCK][base.NODE-base.f.BLOCK:base.NODE-base.f.BLOCK+371]
    assert node[:4]==base.SHOT_VTABLE.to_bytes(4,'little') and int.from_bytes(node[0x125:0x129],'little')==base.DEFINITION
    assert int.from_bytes(node[0x13D:0x141],'little')==case['damage'] and int.from_bytes(node[0x141:0x145],'little')==case['data2']
    assert int.from_bytes(node[0x40:0x44],'little',signed=True)==expected['shot_health']
    assert post[base.SHOT_VTABLE]==checks[base.SHOT_VTABLE]
    defs=post[base.target.attack.TARGET_DEF]
    assert defs[0x61C:0x624]==bytes([6,case['value']])*4 and defs[0x78A]==4
    res_start=base.target.attack.TARGET_DEF+0x61C
    assert not [r for r in result['observed_reads'] if int(r['address'],16)<res_start+8 and int(r['address'],16)+r['size']>res_start]
    assert all(int(r['address'],16)==0 and r['size']==4 and pe_read(EXE.read_bytes(),int(r['pc'],16),2)==bytes.fromhex('64a1')
        for r in result['observed_reads'] if int(r['address'],16)<4096)
    for a in [0x491DC0,0x568820,0x568E30,0x492320,0x4ACD60,0x56E540,0x56E690,0x4ACDF0,0x4B7CD0,0x4AAC90]:
        assert result['visited_instructions'].get(hex(a),0)==1
    assert not result['visited_instructions'].get('0x4ac420',0)
    handlers={1:0x4AC5A0,2:0x4AC6D0,3:0x4AC940,4:0x4ACAF0}
    assert result['visited_instructions'].get('0x4ac3c0',0)==case['repeats']
    assert result['visited_instructions'].get(hex(handlers[case['mode']]),0)==case['repeats']
    assert result['visited_instructions'].get('0x4b2ce0',0)==len(expected['requests'])
    assert int.from_bytes(post[LOG_OBJECT][0x4018:0x401C],'little')==len(expected['log_lines'])
    tags_seen=set();pointers={100:base.cf.ACTOR,102:base.NODE,**dict(zip(TAGS[:len(case['positions'])],TARGETS[:len(case['positions'])]))}
    for cell in range(32*32):
        tag=int.from_bytes(post[base.f.MAP][cell*16:cell*16+2],'little');previous=0
        while tag:
            assert tag in pointers and tag not in tags_seen;tags_seen.add(tag)
            address=pointers[tag]
            data=node if tag==102 else post[address]
            assert int.from_bytes(data[4:6],'little')==tag and int.from_bytes(data[10:12],'little')==previous
            x,y=struct.unpack_from('<2i',data,0x1A);assert ((y>>12)*32+(x>>12))==cell
            previous,tag=tag,int.from_bytes(data[12:14],'little')
    assert tags_seen==set(pointers)
    return dict(HP=actual_hp,requests=[dict(target=i,delta=d,HP_before=h) for i,d,h in expected['requests']],
        shot_health=expected['shot_health'],extent_cells=expected['extent_cells'],debug_log_lines=len(expected['log_lines']),
        damage_type=case['mode'],category=6,resistance_value=case['value'],resistance_pairs_read=False,
        instance_Data1_preserved=True,cell_lists_valid=True,instructions=result['instruction_count'],
        complete_context_regions=len(post),complete_context_bytes=sum(map(len,post.values())))


if __name__=='__main__':
    rows=[];selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for case in selected:
        args,checks=build(case);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            result=cpu.run(EXE,args,list(checks.items()),base.f.OBS,TARGETS[0],len(checks[TARGETS[0]]))
            saved=dict(case=case,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        result=saved['result'];post=post_context(checks,result)
        if not result['success']:print(result['emulation_error'],result['trace_tail'],flush=True)
        outcome=verify(case,args,checks,result)
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified HP',outcome['HP'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase131-area-native-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
