"""Raw Creature/recipe loaders -> original complete Creature sacrifice entry.

Prepared post-factory actor with real embedded AIcontext. Full rawloader output
compared to prior Ghidra definition2199 and a declared measured baseline for
opaque intermediate bytes; no invented semantic interpretation of those bytes.
Original code/vtables retained; declared audio lookup/play external providers.
"""
from pathlib import Path
import json,struct,sys,hashlib
from functools import cache
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from spell_state_tables import BASE as STATEBASE,initialized_states
from creature_category_tables import BASE as CATBASE,initialized_categories
import dk2_formats as fmt
import append_temple_recipe as recipe_append
import native_unicorn_factorytrace as cpu
from native_sacrifice import raw_recipe,runtime_recipe,history_append,match
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
WORLD,ROOM,ACTOR,DEFINITION,OWNER,MAP,ROOMDEF,RAW,MID,RECIPE,SOUND,OUT,STOP=[0x18000000+i*0x10000 for i in range(13)]
SESSION,VARS=0x180D0000,0x180E0000
TRIANGLES,VERTICES=0x180F0000,0x180F0080
NAV_BSS,NAV_BSS_BYTES=0x6EB980,0x1690
SV,SCB,PLAY=SOUND+0x1000,SOUND+0x2000,SOUND+0x2100
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd'
CONTEXT=ACTOR+0x24E
LOOKUP_TOKEN,PLAY_TOKEN=0x11223344,0x76543210

def sha(b):return hashlib.sha256(b).hexdigest()
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')

def cases():
    base=dict(room=True,room_kind=13,cell_kind=255,level=3,z=0x1000,owner=2,entry=False,reward=False,room_capacity=100,source_table=False,initial_state=0x65,member=True,prior=[])
    return [dict(base,**x,name=name) for name,x in [
        ('no-recipe',{}),('matching',dict(reward=True)),('level10',dict(level=10,reward=True)),
        ('nonzero-height',dict(z=0x8000,reward=True)),('foreign-owner',dict(owner=3,reward=True)),
        ('no-room',dict(room=False,reward=True)),('other-room',dict(room_kind=12,reward=True)),
        ('entry-preparation',dict(entry=True,reward=True)),
        ('nonfloor-foreign',dict(cell_kind=0,owner=3,reward=True)),
        ('nonfloor-own',dict(cell_kind=0,reward=True)),
        ('nonfloor-room-full',dict(cell_kind=0,room_capacity=0,reward=True)),
        ('appended-source-recipe',dict(reward=True,source_table=True)),
        ('direct-from-state1-room-full',dict(cell_kind=0,room_capacity=0,reward=True,initial_state=1,member=False)),
        ('direct-from-state1-room-available',dict(cell_kind=0,reward=True,initial_state=1,member=False)),
    ]]

def recipes_for_case(c):
    if not c['reward']:return []
    raw=raw_recipe([(1,5)],4,5)
    if not c['source_table']:return [raw]
    source=(SOURCE.parent/'GlobalVariables.kwd').read_bytes()
    return recipe_append.recipes(recipe_append.append(source,raw))

def memory_and_calls(c):
    raw=next(r for s in fmt.parse(SOURCE.read_bytes()) if fmt.FAMILIES.get(s.signature)=='Creatures' for r in s.records if r[0xF4B]==5)
    actor=bytearray((ROOT/'ghidra/phase90-creature-factory-base.bin').read_bytes());assert len(actor)==1343
    for o,v,n in [(4,100,2),(0x1A,0x800,4),(0x1E,0x800,4),(0x22,c['z'],4),(0x28,c['owner'],2),
        (0x2A,OWNER if c['owner']==2 else OWNER+0x2000,4),(0x2E,0,4),(0x32,0,4),(0x40,1000,4),
        (0x256,ACTOR,4),(0x25A,WORLD,4),(0x25E,c['initial_state'],4),(0x26A,int(c['entry']),4),(0x3B9,int(c['member']),2),
        (0x36F,5,1),(0x370,DEFINITION,4),(0x3E6,0x12360020,4),(0x403,c['level'],1)]:put(actor,o,v,n)
    world=bytearray(0xA3D0)
    for o,v in [(0,0x66E3EC),(4,SESSION),(0x9E6,2),(0x9EA,1),(0x9EE,MAP),(0x15A0,1)]:put(world,o,v)
    session=bytearray(0x18);put(session,0x14,WORLD)
    room=bytearray(0xC0);put(room,4,1,2);room[0x18]=c['room_kind'];put(room,0x19,ROOMDEF);put(room,0x1D,2,2);put(room,0xA7,2,2);put(room,0x67,c['room_capacity'],2)
    if c['member']:
        put(room,0xA,100,2);put(room,0x6B,1,2);put(room,0x6D,1,2)
    room[0x5B:0x5E]=bytes([255,0,0])
    for event in c['prior']:room=bytearray(history_append(room,event))
    owner=bytearray(0x1200);put(owner,0x19A,1);put(owner,0x82,50);put(owner,0x10EA,100)
    cells=bytearray(32);put(cells,0,100,2);put(cells,2,int(c['room']),2);cells[4]=1;put(cells,8,2,2);cells[12]=c['cell_kind']
    put(cells,18,1,2);put(cells,24,2,2)
    tagptr=bytearray(4096*4);alive=bytearray(4096)
    for tag,ptr in [(1,ROOM),(2,OWNER),(3,OWNER+0x2000),(100,ACTOR)]:put(tagptr,tag*4,ptr);alive[tag]=1
    vt=bytearray(0x24);put(vt,0x1C,SCB);put(vt,0x20,PLAY)
    cb=bytes.fromhex('ff05')+struct.pack('<I',OUT)+b'\xB8'+struct.pack('<I',LOOKUP_TOKEN)+bytes.fromhex('c20800')
    play=bytearray(bytes.fromhex('ff05')+struct.pack('<I',OUT+4))
    for index in range(4):play+=bytes.fromhex('8b4424')+bytes([4+4*index])+b'\xA3'+struct.pack('<I',OUT+8+4*index)
    play+=b'\xB8'+struct.pack('<I',PLAY_TOKEN)+bytes.fromhex('c21000')
    vars_=bytearray(0x400);put(vars_,0x40,20)
    nav=bytearray(236);struct.pack_into('<8H',nav,0,0,1,2,0,0,0,0,0)
    struct.pack_into('<6h',nav,128,0,0,0,32,32,0)
    memory={WORLD:world,ROOM:room,ACTOR:actor+bytes([165])*64,DEFINITION:bytes([165])*(2199+64),
        OWNER:owner,OWNER+0x2000:bytes(owner),MAP:cells,ROOMDEF:bytes(0x500),RAW:raw+bytes([165])*64,
        MID:bytes([165])*(6457+64),SOUND:struct.pack('<I',SV),SV:vt,SCB:cb,PLAY:play,OUT:bytes(24),SESSION:session,VARS:vars_,
        0x6EE540:tagptr,0x6ED540:alive,0x6E5050:struct.pack('<I',WORLD),0x6ED484:struct.pack('<I',WORLD),
        0x6DA490:struct.pack('<I',2),0x6DA49C:struct.pack('<I',MAP),0x6DA4A4:struct.pack('<I',WORLD),
        0x75B2F4:bytes(4),0x75B468:struct.pack('<I',SOUND),0x6E4154:struct.pack('<I',VARS),
        0x6E4144:b'\x01',0x6E4148:bytes(12),0:bytes(4),0x7FFEE000:bytes([165])*0x4000,
        TRIANGLES:nav+bytes([165])*64,0x6E5AC0:bytes(64),NAV_BSS:bytes(NAV_BSS_BYTES),
        STATEBASE:bytes([165])*len(initialized_states()[0]),CATBASE:pe_read(EXE.read_bytes(),CATBASE,len(initialized_categories()[0]))}
    calls=[dict(address='0x4798f0',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x47bbe0',registers=dict(ECX='0'),stack_args=[]),
        dict(address='0x4ce690',registers=dict(ECX='0'),stack_args=[TRIANGLES,236]),
        dict(address='0x491dc0',registers=dict(ECX=hex(DEFINITION)),stack_args=[]),
        dict(address='0x568820',registers=dict(ECX=hex(MID)),stack_args=[]),
        dict(address='0x568e30',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
        dict(address='0x492320',registers=dict(ECX=hex(DEFINITION)),stack_args=[MID,WORLD,0])]
    recipes=recipes_for_case(c)
    if recipes:
        memory[RECIPE]=b''.join(bytes(16)+r for r in recipes)+bytes([165])*64
        for index in range(len(recipes)):
            calls.append(dict(address='0x51db30',registers=dict(ECX=hex(WORLD)),stack_args=[RECIPE+32*index]))
    return memory,calls

def build(c,baseline=False):
    memory,calls=memory_and_calls(c)
    args=dict(program='DKII-DX.exe',address='0x508df0' if baseline else '0x472930',
        registers=json.dumps(dict(ECX=hex(WORLD if baseline else CONTEXT))),before_calls=calls,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),max_steps=600000,
        observe_reads=[[DEFINITION,2199],[ACTOR,1343],[ROOM,0xC0],[0,4096]],
        observe_writes=[[ACTOR,1343],[ROOM,0xC0],[OWNER,0x1200],[NAV_BSS,NAV_BSS_BYTES],[TRIANGLES,300],[0,4096]],
        allowed_external_IO_code=[[SCB,len(memory[SCB])],[PLAY,len(memory[PLAY])]])
    spans=sorted((a,len(b)) for a,b in memory.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    checks={a:bytes(b) for a,b in memory.items() if a not in [SCB,PLAY,0x7FFEE000]}
    return args,checks

def path_for(args):
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase136-temple-creature-native-{tag}.json'

def execute(c,baseline=False):
    args,checks=build(c,baseline);path=path_for(args)
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),STOP,ACTOR,1343+64)
        saved=dict(case=c,baseline=baseline,input=args,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if not r['success']:print(c['name'],baseline,r['emulation_error'],r['registers'],r['trace_tail'],flush=True)
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    return args,checks,saved,path

def reference():
    c=cases()[0];args,checks,saved,path=execute(c,True);r=saved['result'];post=post_context(checks,r)
    old=ROOT/'ghidra/phase85-raw-creature-definition-raw-warlock.bin'
    assert post[DEFINITION]==old.read_bytes()+bytes([165])*64
    assert post[OUT]==struct.pack('<6I',2,0,0,0,0,0)
    assert all(post[a]==checks[a] for a in checks if a not in [MID,DEFINITION,OUT,NAV_BSS,STATEBASE,CATBASE])
    assert post[STATEBASE]==initialized_states()[0] and post[CATBASE]==initialized_categories()[0]
    expected_nav=bytearray(checks[NAV_BSS])
    for a,v in [(0x6EC9FC,8),(0x6ECA00,8),(0x6ECA04,4),(0x6ECA08,2),(0x6ECA0C,TRIANGLES),
        (0x6ECA10,VERTICES),(0x6ECA14,TRIANGLES+144),(0x6ECA18,TRIANGLES+152),
        (0x6ECA1C,TRIANGLES+184),(0x6ECA20,TRIANGLES+220),(0x6ECA24,TRIANGLES+228),
        (0x6ECA40,1),(0x6ECA5C,236)]:put(expected_nav,a-NAV_BSS,v)
    assert post[NAV_BSS]==expected_nav
    return post,dict(file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),
        prior_Ghidra_definition=dict(file=str(old.relative_to(ROOT)),sha256=sha(old.read_bytes())),
        opaque_intermediate_scope='Entire6457byte output measured from native init/converter, not independently named or decoded here.',
        full_definition_matches_prior_Ghidra2199=True)

def expected(c,checks,base):
    out={a:bytearray(b) for a,b in checks.items()}
    for a in [MID,DEFINITION,OUT,NAV_BSS,STATEBASE,CATBASE]:out[a][:]=base[a]
    accepted=c['room'] and c['room_kind']==13 and c['cell_kind']==255
    recipes=recipes_for_case(c)
    for index,raw in enumerate(recipes):out[WORLD][0x2E70+34*index:0x2E70+34*(index+1)]=runtime_recipe(raw)
    out[WORLD][0x731B]=len(recipes)
    if c['entry']:
        put(out[ACTOR],0x3E6,0x12300020)
        # Selected same-position search produces one destination waypoint.
        # These are bounded fixture outputs, not a general navmesh serializer.
        for o,v in [(0x1A8,0x8000),(0x1AC,0x8000),(0x1B0,0),(0x1B4,0),
            (0x1C8,0x8000),(0x1CC,0x8000),(0x1D8,0x8000),(0x1DC,0x8000)]:put(out[ACTOR],o,v)
        put(out[ACTOR],0x230,1,1);put(out[ACTOR],0x232,1,1)
        for a,v in [(0x6EB9A0,8),(0x6EC9B4,1),(0x6EC9E4,(int.from_bytes(base[DEFINITION][0x686:0x68A],'little')>>4)&1),
            (0x6ECA44,1),(0x6ECA48,1),(0x6ECB18,128),(0x6ECB1C,128),(0x6ECB20,128),(0x6ECB24,128),
            (0x6ECB28,1),(0x6ECB2C,128),(0x6ECB30,128)]:put(out[NAV_BSS],a-NAV_BSS,v)
        put(out[TRIANGLES],14,127,2);put(out[TRIANGLES],144,2,1)
        put(out[TRIANGLES],184,1);put(out[TRIANGLES],216,0xFFFFFFFF)
    transition=bool(c['room'] and c['room_kind']==13 and c['cell_kind']!=255 and c['owner']==2)
    requirement=base[DEFINITION][0x672]
    if transition:put(out[ACTOR],0x266,0)
    old_category=initialized_states()[1][c['initial_state']]['category']
    same_category=old_category==12
    if transition and (same_category or c['room_capacity']>=requirement):
        put(out[ACTOR],0x25E,0x67)
        if not same_category:
            put(out[ROOM],0xA,100,2);put(out[ROOM],0x6B,requirement,2);put(out[ROOM],0x6D,requirement,2)
            put(out[ACTOR],0x3B9,1,2)
    if accepted:
        out[ROOM][:]=history_append(out[ROOM],(1,5,c['level']))
        put(out[ACTOR],0x40,0);put(out[ACTOR],0x2E,PLAY_TOKEN)
        put(out[ACTOR],0x3E6,(int.from_bytes(out[ACTOR][0x3E6:0x3EA],'little')&~0x20)|0x40)
        out[OUT][:]=struct.pack('<6I',2,1,0,LOOKUP_TOKEN,0xE2,ACTOR+0x1A)
        if c['reward']:
            matches=[i for i,raw in enumerate(recipes) if match(out[ROOM],runtime_recipe(raw))['matched']]
            assert matches==[len(recipes)-1]
            out[ROOM][0x5B:0x5E]=bytes([255,0,0]);put(out[WORLD],0x15A0,0x3C88596C)
            put(out[OWNER],0x82,55);put(out[OWNER],0x10EE,5)
    return {a:bytes(b) for a,b in out.items()},bool(accepted)

def verify(c,args,checks,wanted,accepted,r):
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==7+len(recipes_for_case(c))
    post=post_context(checks,r)
    if post!=wanted:
        dif=[dict(address=hex(a),bytes_different=sum(x!=y for x,y in zip(post[a],wanted[a])),
            first=[hex(i) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:16]) for a in wanted if post[a]!=wanted[a]]
        raise AssertionError((c['name'],dif))
    transition=bool(c['room'] and c['room_kind']==13 and c['cell_kind']!=255 and c['owner']==2)
    assert int(r['registers']['EAX'],16)==int(transition)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        assert int(x['address'],16)==0 and x['size']==4
        ins=next(md.disasm(pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x,ins.mnemonic,ins.op_str)
    pcs=r['visited_instructions'];assert (hex(0x4F1600) in pcs)==accepted and (hex(0x4B2AD0) in pcs)==accepted
    assert (hex(0x4F1780) in pcs)==(accepted and c['reward'])
    assert (hex(0x476D30) in pcs)==transition
    assert (hex(0x4E53B0) in pcs)==(transition and initialized_states()[1][c['initial_state']]['category']!=12)
    return dict(accepted=accepted,level_appended=c['level'] if accepted else None,
        HP=int.from_bytes(post[ACTOR][0x40:0x44],'little'),sound_plays=int.from_bytes(post[OUT][4:8],'little'),
        mana=int.from_bytes(post[OWNER][0x82:0x86],'little'),creature_remains_registered=post[0x6ED540][100]==1,
        transition_requested=transition,callback_return=int(r['registers']['EAX'],16),
        final_AI_state=int.from_bytes(post[ACTOR][0x25E:0x262],'little'),
        final_room_tag=int.from_bytes(post[ACTOR][0x3B9:0x3BB],'little'),
        initial_AI_state=c['initial_state'],room_capacity_checked=hex(0x4E53B0) in pcs,
        complete_prepared_regions=len(post),complete_prepared_bytes=sum(map(len,post.values())),
        preparation_helper_called=hex(0x48ECE0) in pcs,original_steps=r['instruction_count'])

if __name__=='__main__':
    base,baseline=reference();rows=[]
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for c in selected:
        args,checks,saved,path=execute(c);wanted,accepted=expected(c,checks,base)
        out=verify(c,args,checks,wanted,accepted,saved['result'])
        rows.append(dict(case=c['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out))
        print(c['name'],out,flush=True)
    if len(selected)==len(cases()):(ROOT/'phase136-temple-creature-emulations.json').write_text(json.dumps(dict(scope=__doc__,baseline=baseline,replays=rows),indent=2),encoding='utf8')
