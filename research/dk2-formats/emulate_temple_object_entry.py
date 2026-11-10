"""Original rawObject loader and rawrecipe loader -> complete object sacrifice callback.

Actor is an explicit post-factory snapshot, not newly allocated here. Original
World/Object vtables and methods execute. Null graphics/audio lookup only.
No live scheduler, physical falling, final entity unlink or visual effects.
"""
from pathlib import Path
import json,struct,sys,hashlib
import dk2_formats as fmt
import native_object_loading as object_layout
import native_unicorn_factorytrace as cpu
import native_temple_object_entry as rules
from native_sacrifice import raw_recipe,runtime_recipe,history_append,match
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE

ROOT=Path(__file__).resolve().parent
WORLD,ROOM,ACTOR,DEFINITION,OWNER,MAP,ROOMDEF,RAW,MID,RECIPE,SOUND,OUT,STOP=[0x18000000+i*0x10000 for i in range(13)]
SV,SCB=SOUND+0x1000,SOUND+0x2000
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd'
DX=hashlib.sha256(EXE.read_bytes()).hexdigest()

def sha(b):return hashlib.sha256(b).hexdigest()
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')

def cases():
    base=dict(ident=17,flags=0,source_flags=False,room=True,room_kind=13,cell_kind=255,z=0x2000,reward=False,mana=50,capacity=100,object_owner=2,prior=[])
    return [dict(base,**x,name=name) for name,x in [
        ('ordinary-no-recipe',{}),('ordinary-matching',dict(reward=True)),
        ('gold-matching',dict(flags=0x40,reward=True)),('gold-opaque',dict(flags=0x80000040,reward=True)),
        ('food-matching',dict(flags=0x80,reward=True)),('normal-opaque',dict(flags=0x80000000,reward=True)),
        ('no-room',dict(room=False,reward=True)),('other-room',dict(room_kind=12,reward=True)),
        ('nonfloor-cell',dict(cell_kind=0x7F,reward=True)),
        ('at-floor',dict(z=0,reward=True)),('below-one-cell',dict(z=0xFFF,reward=True)),
        ('one-cell-above',dict(z=0x1000,reward=True)),('below-floor',dict(z=0xFFFFFFFF,reward=True)),
        ('foreign-object-owner',dict(object_owner=3,reward=True)),
        ('mana-cap',dict(mana=99,reward=True)),
        ('gold-keeps-history',dict(flags=0x40,reward=True,prior=[(1,8,3)])),
        ('matching-clears-older-history',dict(reward=True,prior=[(1,8,3)])),
        ('source-gold',dict(ident=1,source_flags=True,reward=True)),
        ('source-door-key',dict(source_flags=True,reward=True)),
    ]]

def build(c):
    raw=bytearray(next(r for s in fmt.parse(SOURCE.read_bytes()) if fmt.FAMILIES.get(s.signature)=='Objects' for r in s.records if r[0x35A]==c['ident']))
    flags=int.from_bytes(raw[0x344:0x348],'little') if c['source_flags'] else c['flags']
    put(raw,0x344,flags)
    actor=bytearray((ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes())
    assert len(actor)==393 and int.from_bytes(actor[:4],'little')==0x66D654
    for o,v,n in [(4,100,2),(0x1A,0x800,4),(0x1E,0x800,4),(0x22,c['z'],4),(0x28,c['object_owner'],2),
                   (0x2A,OWNER if c['object_owner']==2 else OWNER+0x2000,4),(0x2E,0,4),(0x32,0,4),
                   (0x40,1000,4),(0x12C,c['ident'],1),(0x12D,DEFINITION,4),(0x146,0x13579BDF,4)]:put(actor,o,v,n)
    world=bytearray(0xA3D0)
    for o,v in [(0,0x66E3EC),(0x9E6,2),(0x9EA,1),(0x9EE,MAP),(0x15A0,1)]:put(world,o,v)
    room=bytearray(0xC0);put(room,4,1,2);room[0x18]=c['room_kind'];put(room,0x19,ROOMDEF);put(room,0x1D,2,2)
    put(room,0xA7,2,2);room[0x5B:0x5E]=bytes([255,0,0])
    for event in c['prior']:room=bytearray(history_append(room,event))
    owner=bytearray(0x1200);put(owner,0x19A,1);put(owner,0x82,c['mana']);put(owner,0x10EA,c['capacity'])
    cells=bytearray(32);put(cells,0,100,2);put(cells,2,int(c['room']),2);cells[4]=1;cells[12]=c['cell_kind']
    put(cells,18,1,2);cells[28]=0
    tagptr=bytearray(4096*4);alive=bytearray(4096)
    for tag,ptr in [(1,ROOM),(2,OWNER),(3,OWNER+0x2000),(100,ACTOR)]:put(tagptr,tag*4,ptr);alive[tag]=1
    vt=bytearray(32);put(vt,0x1C,SCB)
    cb=bytes.fromhex('ff05')+struct.pack('<I',OUT)+bytes.fromhex('33c0c20800')
    memory={WORLD:world,ROOM:room,ACTOR:actor+bytes([165])*64,DEFINITION:bytes(285)+bytes([165])*64,
        OWNER:owner,OWNER+0x2000:bytes(owner),MAP:cells,ROOMDEF:bytes(0x500),RAW:raw+bytes([165])*64,
        MID:bytes([165])*(1022+64),SOUND:struct.pack('<I',SV),SV:vt,SCB:cb,OUT:bytes(8),
        0x6EE540:tagptr,0x6ED540:alive,0x6E5050:struct.pack('<I',WORLD),0x6ED484:struct.pack('<I',WORLD),
        0x6DA490:struct.pack('<I',2),0x6DA49C:struct.pack('<I',MAP),0x6DA4A4:struct.pack('<I',WORLD),
        0x75B2F4:bytes(4),0x75B468:struct.pack('<I',SOUND),0x740C20:bytes(32),
        0x6FCDCF:bytes(4),0x6F2590:struct.pack('<I',20),0x7FFEE000:bytes([165])*0x4000}
    calls=[dict(address='0x56d4f0',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x56d620',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
        dict(address='0x49dd60',registers=dict(ECX=hex(DEFINITION)),stack_args=[MID,0])]
    recipes=[raw_recipe([(2,c['ident'])],4,5)] if c['reward'] else []
    if recipes:
        memory[RECIPE]=bytes(16)+recipes[0]+bytes([165])*64
        calls.append(dict(address='0x51db30',registers=dict(ECX=hex(WORLD)),stack_args=[RECIPE]))
    args=dict(program='DKII-DX.exe',address='0x49f600',registers=json.dumps(dict(ECX=hex(ACTOR))),before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),max_steps=300000,
        observe_reads=[[DEFINITION,285],[ACTOR,393],[ROOM,0xC0],[0,4096]],
        observe_writes=[[ACTOR,393],[ROOM,0xC0],[OWNER,0x1200]],allowed_external_IO_code=[[SCB,len(cb)]])
    spans=sorted((a,len(b)) for a,b in memory.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    checks={a:bytes(b) for a,b in memory.items() if a not in [SCB,0x7FFEE000]}
    wanted={a:bytearray(b) for a,b in checks.items()}
    mid=object_layout.intermediate(raw,bytes(1022));definition=object_layout.definition(mid,bytes(285),DX,20,0,0)
    wanted[MID][:1022]=mid;wanted[DEFINITION][:285]=definition;put(wanted[OUT],0,1)
    wanted[0x740C20][:]=object_layout.string32(raw[0x35E:0x37E])
    for i,recipe in enumerate(recipes):wanted[WORLD][0x2E70+34*i:0x2E70+34*(i+1)]=runtime_recipe(recipe)
    wanted[WORLD][0x731B]=len(recipes)
    accepted=c['room'] and c['room_kind']==13 and c['cell_kind']==255 and not flags&0x40
    if accepted:
        put(wanted[ACTOR],0x40,0);put(wanted[ACTOR],0x146,0x13579BDE)
        expected_room=bytearray(history_append(room,(2,c['ident'],0)))
        if recipes:
            assert match(expected_room,runtime_recipe(recipes[0]))['matched']
            expected_room[0x5B:0x5E]=bytes([255,0,0]);put(wanted[WORLD],0x15A0,0x3C88596C)
            put(wanted[OWNER],0x82,min(c['mana']+5,c['capacity']));put(wanted[OWNER],0x10EE,5)
        wanted[ROOM][:]=expected_room
    expected=dict(accepted=bool(accepted),recipe_matched=bool(accepted and recipes),HP=0 if accepted else 1000,raw_flags=flags,
        final_mana=int.from_bytes(wanted[OWNER][0x82:0x86],'little'),object_registered=True,
        input_object_owner=c['object_owner'],reward_room_owner=2,
        note='HP0 is not completed World removal. Visual helper may be called but RoomDefinition429=0 gates its effect.')
    return args,checks,{a:bytes(b) for a,b in wanted.items()},expected

def path_for(args):
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase135-temple-object-native-{tag}.json'

def verify(c,args,checks,wanted,expected,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error']
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==3+int(c['reward'])
    post=post_context(checks,r);assert post==wanted,(c['name'],r['context_mismatches'])
    assert int(r['registers']['EAX'],16)==0
    assert not [x for x in r['observed_reads'] if int(x['address'],16)<4096]
    pcs=r['visited_instructions'];accepted=expected['accepted']
    assert accepted==rules.accepts(room_present=c['room'],room_category=c['room_kind'],cell_byte_C=c['cell_kind'],object_flags=expected['raw_flags'])
    assert (hex(0x4F1600) in pcs)==accepted and (hex(0x4B2AD0) in pcs)==accepted
    assert (hex(0x4F1780) in pcs)==expected['recipe_matched']
    assert (hex(0x4F1940) in pcs)==(accepted and rules.visual_helper_gate(c['z'],0))
    return dict(expected,complete_prepared_regions=len(post),complete_prepared_bytes=sum(map(len,post.values())),
        entire_actor_with_guard_sha256=sha(post[ACTOR]),entire_room_sha256=sha(post[ROOM]),
        visual_helper_called=hex(0x4F1940) in pcs,low_memory_reads=0,instruction_count=r['instruction_count'])

if __name__=='__main__':
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]];rows=[]
    for c in selected:
        args,checks,wanted,expected=build(c);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            r=cpu.run(EXE,args,list(checks.items()),STOP,ACTOR,393+64)
            saved=dict(case=c,input=args,expected=expected,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        r=saved['result']
        if not r['success']:print(c['name'],r['emulation_error'],r['registers'],r['trace_tail'],flush=True)
        outcome=verify(c,args,checks,wanted,expected,r)
        rows.append(dict(case=c['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(c['name'],'accepted',expected['accepted'],'mana',expected['final_mana'],'visual',outcome['visual_helper_called'],flush=True)
    if len(selected)==len(cases()):(ROOT/'phase135-temple-object-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
