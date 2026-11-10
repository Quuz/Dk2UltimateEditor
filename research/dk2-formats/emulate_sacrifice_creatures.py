"""Native ctor→AI initializers→temple Creature reward, including real state entry.

All game functions/vtables remain original. Runtime definitions, owner, room,
grid and coefficients are prepared data; null GFX and fixed sound are external
providers. Six deterministic replays capture the complete1343-byte actor.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_creature_factory as cf
import emulate_object_factory as f
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import memory_readout_ymm as readout
import fixture_read_probe as probe
from spell_state_tables import BASE as STATEBASE,initialized_states
from creature_category_tables import BASE as CATBASE,initialized_categories
from build_effect_semantics import pe_read
from native_sacrifice import raw_recipe,runtime_recipe,history_append
ROOT=Path(__file__).resolve().parent
ROOM=0x10100000;ROOMDEF=0x10110000;GFX90=0x10120000
RAW=0x10130000;MATCH=0x10131000
def cases():
    return [dict(ident=8,**c) for c in [dict(name='level1',level=1),dict(name='level0',level=0),dict(name='level3',level=3),
      dict(name='level3-high-bits',level=3,value=0xfe03ab08),dict(name='level10',level=10),dict(name='level11',level=11),
      dict(name='level255',level=255),dict(name='level2-health150',level=2,health_coefficient=150),dict(name='raw-level3',level=3,raw_pipeline=True),
      dict(name='room-full',level=1,room_capacity=0)]]
def build(c,page):
    fc=dict(cf.cases()[0],position=[0x5800,0x6800,0x1000]);args,e=cf.build(fc,page)
    regions={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};at=0x11000000
    for r in e['checks']:checks[int(r['address'],16)]=regions[at];at+=((r['bytes']+255)//256)*256
    memory={a:b for a,b in regions.items() if a<0x11000000 and a not in [f.OBS,cf.DRIVER,0x7ffee000]}
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    both(f.PLAYER,0x19a,1)
    both(0x6ee540,200*4,ROOM);both(0x6ed540,200,1,1)
    cell=(6*32+5)*16;both(f.MAP,cell+2,200,2)
    # Constructor/factory consumes two world-PRNG draws; room selection adds one.
    seed=0
    for _ in range(3):seed=(seed*0x19660d+0x3c6ef35f)&0xffffffff
    f.put(checks[f.WORLD],0x15a0,seed)
    room=bytearray(0xc0);f.put(room,4,200,2);room[0x18]=13;f.put(room,0x19,ROOMDEF);f.put(room,0x1d,7,2)
    room[0x2d]=5;room[0x2e]=6;f.put(room,0xa7,1,2);f.put(room,0x67,c.get('room_capacity',100),2);room[0x5b:0x5e]=bytes([1,0,3])
    wanted=bytearray(room);wanted[0x5b:0x5e]=bytes([255,0,0]);f.put(wanted,10,100,2);f.put(wanted,0x6b,1,2);f.put(wanted,0x6d,1,2)
    if c.get('room_capacity',100)==0:
        for o in [10,0x6b,0x6d]:f.put(wanted,o,0,2)
    for a,b,w in [(ROOM,room,wanted),(ROOMDEF,bytes(0x500),bytes(0x500))]:memory[a]=bytearray(b);checks[a]=bytearray(w)
    both(cf.DEFINITION,0x672,1,1)
    # The health table is packed at an ODD address. A blanket aligned WORD fill
    # is not a 100% table: it would read 0x6400 at native E1+2*level.
    if c['level']>1:
        for level in range(11):both(cf.VARIABLES,0xe1+2*level,100,2)
    if 'health_coefficient' in c:both(cf.VARIABLES,0xe1+2*c['level'],c['health_coefficient'],2)
    # Independent data projections are compared AFTER original initializers run.
    state,_=initialized_states();cat,_=initialized_categories()
    memory[STATEBASE]=bytearray([0xa5])*len(state);checks[STATEBASE]=bytearray(state)
    memory[CATBASE]=bytearray(pe_read((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes(),CATBASE,len(cat)));checks[CATBASE]=bytearray(cat)
    arguments=bytearray(64);struct.pack_into('<3I',arguments,16,1,c.get('value',c['ident']+(c['level']<<16)),0);struct.pack_into('<3I',arguments,32,0x9800,0xa800,0x3000)
    memory[f.ARGS]=arguments;checks[f.ARGS]=bytearray(arguments)
    memory[f.OUT]=bytearray(20);checks[f.OUT]=bytearray(struct.pack('<5I',1,cf.ACTOR,max(0,min(10,c['level'])-1),1,max(0,min(10,c['level'])-1)))
    # GFX vslot90 has six args; original native509580→50E490 calls it.
    both(f.GV,0x90,GFX90)
    memory[GFX90]=bytearray(bytes.fromhex('ff05')+pack32(f.OUT+16)+bytes.fromhex('33c0c21800'))
    driver=bytearray()
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(cf.DRIVER+len(driver)+5)))
    call(0x4798f0);call(0x47bbe0)
    reward=f.ARGS+16
    if c.get('raw_pipeline'):
        raw=raw_recipe([(1,16)],1,c['ident']);memory[RAW]=bytearray(bytes(16)+raw);checks[RAW]=bytearray(memory[RAW])
        memory[MATCH]=bytearray(12);checks[MATCH]=bytearray(struct.pack('<3I',1,c['ident']+(c['level']<<16),0))
        checks[f.WORLD][0x2e70:0x2e70+34]=runtime_recipe(raw);f.put(checks[f.WORLD],0x731b,1,1)
        memory[ROOM][0x5b:0x5e]=bytes([255,0,0]);wanted=bytearray(history_append(memory[ROOM],(1,16,c['level'])));wanted[0x5b:0x5e]=bytes([255,0,0])
        f.put(wanted,10,100,2);f.put(wanted,0x6b,1,2);f.put(wanted,0x6d,1,2);checks[ROOM]=wanted
        emit(b'\x68'+pack32(RAW)+b'\xb9'+pack32(f.WORLD));call(0x51db30)
        emit(b'\x68'+pack32(c['level'])+b'\x68'+pack32(16)+b'\x68'+pack32(1)+b'\xb9'+pack32(ROOM));call(0x4f1600)
        emit(b'\x68'+pack32(MATCH)+b'\x68'+pack32(f.WORLD+0x2e70)+b'\xb9'+pack32(ROOM));call(0x4f1660);reward=MATCH
    emit(b'\x68'+pack32(f.ARGS+32)+b'\x68'+pack32(reward)+b'\xb9'+pack32(ROOM));call(0x4f1780)
    emit(b'\x68'+pack32(f.OBS)+b'\xc3');memory[cf.DRIVER]=driver
    stack=bytearray([0xa5])*16384;f.put(stack,8192,cf.DRIVER);memory[0x7ffee000]=stack
    # Observe registered entity and all side effects; dispatcher has void return.
    observer=bytearray(b'\xa1'+pack32(0x6ee540+400)+b'\xa3'+pack32(f.OUT+4)+bytes.fromhex('85c00f95c00fb6c0a3')+pack32(f.OUT)+b'\xbd'+pack32(1)+b'\xbb'+pack32(0))
    spans=probe.allowed_ranges((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes(),[region(a,b) for a,b in memory.items()]);memory[probe.ADDRESS]=bytearray(probe.adapter(spans))
    mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=readout.code(cf.ACTOR+256*page)+bytes.fromhex('8b15')+pack32(probe.COUNTER)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args.update(memory=json.dumps(dict(regions=mem)))
    e.update(level=min(10,max(1,c['level'])),state=0x6a,room_tag=200,argument_position=[0x9800,0xa800,0x3000],checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])
    if 'health_coefficient' in c:e['health']=1000*c['health_coefficient']//100
    if c.get('room_capacity',100)==0:e.update(state=1,room_tag=0)
    return args,e
def verify(c,b,e):
    u=lambda o,n=4:int.from_bytes(b[o:o+n],'little')
    assert len(b)==1343 and u(0)==0x66d524 and u(4,2)==100 and u(0x36f,1)==c['ident'] and u(0x403,1)==e['level']
    assert u(0x40)==e['health'] and u(0x28,2)==7 and u(0x2a)==f.PLAYER and u(0x370)==cf.DEFINITION
    assert list(struct.unpack_from('<3I',b,0x1a))==e['position'] and u(0x25e)==e['state'] and u(0x3b9,2)==e['room_tag']
    assert u(0x24e+0xaf,1)==20
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                chunks=[]
                for page in range(6):
                    args,e=build(c,page);path=ROOT/'ghidra'/f'phase90-sacrifice-creature-{c["name"]}-page{page}.json'
                    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                    else:
                        response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                        saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                    r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c,page,r,e)
                    chunks.append(readout.decode(r['registers']));rows.append(dict(case=c['name'],page=page,file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],page,'verified',flush=True)
                image=b''.join(chunks)[:1343];verify(c,image,e);(ROOT/'ghidra'/f'phase90-sacrifice-creature-{c["name"]}.bin').write_bytes(image)
    if len(selected)==len(cases()):(ROOT/'phase90-sacrifice-creature-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
