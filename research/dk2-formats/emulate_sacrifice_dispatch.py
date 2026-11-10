"""Full native raw loaders→spell sacrifice handler→matcher→reward dispatcher.

Original World vtable, map/room/PRNG/mana/notification helpers remain intact.
Two tiles form a finite room list; notifications disabled via owner+19A.
Creature/object factories and renderer effects are not exercised successfully.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_sacrifice import *
ROOT=Path(__file__).resolve().parent
WORLD=0x10000000;ROOM=0x10010000;RAW=0x10014000;OUT=0x10018000;ACTION=0x10019000
DRIVER=0x10020000;OBS=0x10028000;MAP=0x10040000;ROOMDEF=0x10050000;OWNER=0x10070000;SHADOW=0x10100000;STACK=0x7fff0000

def call(at,target):return b'\xe8'+pack32(target-at-5)
def put(b,o,v):struct.pack_into('<I',b,o,v&0xffffffff)
def s32(v):return (v&0x7fffffff)-(v&0x80000000)

def cases():
    base=dict(prior=[(1,8,2)],spell=9,mana=50,cap=100,counter=0,room=True)
    one=raw_recipe([(3,9)],4,5)
    two=raw_recipe([(1,8),(3,9)],4,40)
    rows=[dict(base,name='first-shorter',raws=[one.hex(),two.hex()]),
      dict(base,name='first-longer',raws=[two.hex(),one.hex()]),
      dict(base,name='ordered-mixed',raws=[two.hex()]),
      dict(base,name='reversed-mixed',raws=[raw_recipe([(3,9),(1,8)],4,40).hex()]),
      dict(base,name='no-room',room=False,raws=[one.hex()]),
      dict(base,name='empty-first',raws=[raw_recipe([],4,7).hex(),one.hex()]),
      dict(base,name='zero-gap-compacted',raws=[raw_recipe([(1,8),(0,255),(3,9)],4,40).hex()])]
    for kind in [0,5,6,7,8,9,255]:rows.append(dict(base,name=f'unsupported-reward-{kind}',raws=[raw_recipe([(3,9)],kind,1234).hex()]))
    for name,mana,cap,value in [('zero',50,100,0),('capped',90,100,30),('negative',50,100,0xffffffff),('negative-clamp',50,100,0xffffff9c),
                               ('overflow',50,100,0x7fffffff),('cap-high',50,0xffffffff,30)]:
        rows.append(dict(base,name='mana-'+name,mana=mana,cap=cap,raws=[raw_recipe([(3,9)],4,value).hex()]))
    rows.append(dict(base,name='older-history-cleared',prior=[(2,7,0),(1,8,2)],raws=[one.hex()]))
    rows.append(dict(base,name='speech-suppressed',raws=[raw_recipe([(3,9)],4,5,23).hex()]))
    return rows

def build(c):
    world=bytearray(0xa3d0);put(world,0,0x66e3ec);put(world,0x9e6,2);put(world,0x9ee,MAP);put(world,0x15a0,1)
    room=bytearray((i*37+13)&255 for i in range(0xc0));put(room,0x19,ROOMDEF);struct.pack_into('<H',room,0x1d,2)
    room[0x18]=13;room[0x2d:0x2f]=bytes(2);struct.pack_into('<H',room,0xa7,2);room[0x5b:0x5e]=bytes([255,0,0])
    owner=bytearray((i*19+7)&255 for i in range(0x1100));put(owner,0x82,c['mana']);put(owner,0x10ea,c['cap']);put(owner,0x10ee,c['counter']);put(owner,0x19a,1)
    definition=bytes(0x500);tiles=bytearray(32)
    struct.pack_into('<H',tiles,2,1 if c['room'] else 0);tiles[4]=1;tiles[12]=255
    struct.pack_into('<H',tiles,18,1);tiles[28]=0
    action=bytearray(16);action[0]=c['spell'];put(action,4,0x800);put(action,8,0x800)
    regions=[region(WORLD,world),region(ROOM,room),region(ROOMDEF,definition),region(MAP,tiles),region(OWNER,owner),region(ACTION,action),
      region(0x6ed484,pack32(WORLD)),region(0x6ee544,pack32(ROOM)),region(0x6ee548,pack32(OWNER)),region(0x75b2f4,pack32(0)),region(OUT,bytes(20))]
    wanted_world=bytearray(world);wanted_room=bytes(room);wanted_owner=bytearray(owner);wanted_out=bytearray(20)
    driver=bytearray();expected=[(ROOMDEF,definition),(MAP,bytes(tiles)),(ACTION,bytes(action))]
    def emit(b):driver.extend(b)
    def native(a):emit(call(DRIVER+len(driver),a))
    recipes=[bytes.fromhex(v) for v in c['raws']]
    for i,raw in enumerate(recipes):
        arg=bytes(16)+raw;ptr=RAW+32*i;regions.append(region(ptr,arg));expected.append((ptr,arg))
        if i:emit(b'\x68'+pack32(ptr)+b'\xb9'+pack32(WORLD));native(0x51db30)
        wanted_world[0x2e70+34*i:0x2e70+34*(i+1)]=runtime_recipe(raw)
    wanted_world[0x731b]=len(recipes)
    for event in c['prior']:
        kind,ident,level=event
        emit(b'\x68'+pack32(level)+b'\x68'+pack32(ident)+b'\x68'+pack32(kind)+b'\xb9'+pack32(ROOM));native(0x4f1600)
        wanted_room=history_append(wanted_room,event)
    emit(b'\x68'+pack32(ACTION)+b'\xb9'+pack32(WORLD));native(0x5133a0)
    emit(b'\xa3'+pack32(OUT));put(wanted_out,0,1)
    selected=None;reward=None
    if c['room']:
        wanted_room=history_append(wanted_room,(3,c['spell'],0))
        for i,raw in enumerate(recipes):
            result=match(wanted_room,runtime_recipe(raw))
            if result['matched']:selected=i;reward=struct.unpack('<III',result['output']);break
        if selected is not None:
            # Complete native executor chooses a room tile before clearing the history.
            put(wanted_world,0x15a0,(1*0x19660d+0x3c6ef35f)&0xffffffff)
            wanted_room=bytearray(wanted_room);wanted_room[0x5b:0x5e]=bytes([255,0,0]);wanted_room=bytes(wanted_room)
            kind,value,speech=reward
            assert kind not in (1,2,3),'Successful factory and gold cases require a separate native fixture'
            if kind==4:
                new=s32((c['mana']+value)&0xffffffff)
                if s32(value)<0:
                    if new<0:new=0
                elif s32(c['cap'])<new:new=s32(c['cap'])
                put(wanted_owner,0x82,new);put(wanted_owner,0x10ee,c['counter']+value)
            for i in range(3):
                emit(b'\xa1'+pack32(STACK-12+4*i)+b'\xa3'+pack32(OUT+4+4*i));put(wanted_out,4+4*i,reward[i])
    expected.extend([(WORLD,bytes(wanted_world)),(ROOM,wanted_room),(OWNER,bytes(wanted_owner)),(OUT,bytes(wanted_out))])
    emit(b'\x68'+pack32(OBS)+b'\xc3')
    observer=bytearray(b'\xbd'+pack32(1));shadow=SHADOW
    for actual,data in expected:
        regions.append(region(shadow,data));observer.extend(compare_code(actual,shadow,len(data))+bytes.fromhex('21c5'));shadow+=((len(data)+255)//256)*256
    observer.extend(b'\x68'+pack32(0xdeadbeef)+b'\xc3')
    regions.extend([region(DRIVER,driver),region(OBS,observer),region(STACK,pack32(DRIVER)+pack32(RAW))])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in regions)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args=dict(program='DKII-DX.exe',address='0x51db30',max_steps=500000,registers=json.dumps(dict(ECX=hex(WORLD))),
      memory=json.dumps(dict(regions=regions)),return_registers='EAX,EBP,ECX,EIP,ESP')
    details=dict(selected_recipe=selected,reward=None if reward is None else list(reward),final_mana=int.from_bytes(wanted_owner[0x82:0x86],'little'),
      final_awarded_counter=int.from_bytes(wanted_owner[0x10ee:0x10f2],'little'),history_hex=wanted_room[0x49:0x5e].hex(),
      final_prng_seed=int.from_bytes(wanted_world[0x15a0:0x15a4],'little'))
    return args,details

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[];selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,details=build(c);path=ROOT/'ghidra'/f'phase88-sacrifice-dispatch-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==details
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected=details,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,ECX=0).items()),(c['name'],r,details)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    (ROOT/('phase88-sacrifice-dispatch-emulations.json' if len(sys.argv)==1 else 'phase88-sacrifice-dispatch-one-case.json')).write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
