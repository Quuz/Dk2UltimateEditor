"""Native ctor→portal spawn→waiting callback→native animation reset→exit callback.

Original functions/vtables and animation/AI initializers execute. Explicit
runtime Creature definition and room grid are fixtures. Native4966A0 is invoked
by the synthetic driver to isolate completion; actual animation ticking is not
yet composed. External null GFX/fixed sound/range OS probe providers only.
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
from spell_state_tables import BASE as SB,initialized_states
from creature_category_tables import BASE as CB,initialized_categories
from portal_animation_tables import BASE as AB,initialized_animations
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
ROOM=0x10100000;ROOMDEF=0x10110000;STOCK=0x10120000
def cases():
    base=dict(phase='done',offset=[0,0,0],local_owner=7)
    return [dict(base,**v,name=n) for n,v in [('spawn',dict(phase='spawn')),('spawn-nonzero',dict(phase='spawn',offset=[4096,-4096,0x7fffffff])),
      ('waiting',dict(phase='waiting')),('waiting-nonzero',dict(phase='waiting',offset=[4096,-4096,0x7fffffff])),
      ('done-zero',{}),('done-x',dict(offset=[4096,0,0])),('done-y-negative',dict(offset=[0,-4096,0])),
      ('done-z',dict(offset=[0,0,0x7fffffff])),('done-fraction',dict(offset=[512,-256,123])),
      ('done-negative-xy',dict(offset=[-4096,-4096,-0x7fffffff])),('done-other-local-owner',dict(local_owner=8)),
      ('tick-x',dict(offset=[4096,0,0],completion='native-tick')),
      ('ai-waiting',dict(phase='waiting',offset=[4096,0,0],dispatch=True)),
      ('ai-tick-x',dict(offset=[4096,0,0],completion='native-tick',dispatch=True))]]
def build(c,page):
    initial=[0x6800,0x7800,0];args,e=cf.build(dict(cf.cases()[0],position=initial),page)
    allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};at=0x11000000
    for row in e['checks']:checks[int(row['address'],16)]=allmem[at];at+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if a<0x11000000 and a not in [f.OBS,cf.DRIVER,0x7ffee000]}
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    both(f.PLAYER,0x19a,1);both(f.PLAYER,0x76,0x400);f.put(checks[f.PLAYER],0x76,0x600)
    checks[f.PLAYER][0xb58+8*0x12]=1
    both(f.PLAYER,0x4e,2);both(f.PLAYER,0x1109+12,1);both(f.PLAYER,0x1109+16,STOCK)
    stats=bytearray(26);stats[0]=4;stats[13]=8;wanted_stats=bytearray(stats);f.put(wanted_stats,22,1)
    memory[cf.STATS]=stats;checks[cf.STATS]=wanted_stats
    stock=bytearray(bytes([8])+pack32(5));wanted_stock=bytearray(bytes([8])+pack32(4));memory[STOCK]=stock;checks[STOCK]=wanted_stock
    both(f.WORLD,0x7243,c['local_owner'],2)
    if c.get('dispatch'):
        memory[0x6f2548]=bytearray(struct.pack('<H',7));checks[0x6f2548]=bytearray(struct.pack('<H',7))
    memory[0x75ad4b]=bytearray(pack32(3));checks[0x75ad4b]=bytearray(pack32(3))
    room=bytearray(0xc0);f.put(room,4,200,2);room[0x18]=3;f.put(room,0x19,ROOMDEF);f.put(room,0x1d,7,2);room[0x2d]=5;room[0x2e]=6
    for a,b in [(ROOM,room),(ROOMDEF,bytearray(0x500))]:memory[a]=bytearray(b);checks[a]=bytearray(b)
    both(0x6ee540,200*4,ROOM);both(0x6ed540,200,1,1)
    for y in range(6,9):
        for x in range(5,8):both(f.MAP,(y*32+x)*16+2,200,2)
    both(f.MAP,(7*32+6)*16+12,4,1)  # native portal floor0 central cell
    offset=c['offset']
    for i,v in enumerate(offset):both(cf.DEFINITION,0x6ec+4*i,v)
    if c['phase']=='done':
        # 494650 lazily registers its static null-vector destructor with atexit.
        # Model the ordinary already-initialized process state, as in phase85;
        # no game code is replaced and no CRT shutdown path is being tested.
        for a,b in [(0x6e4144,pack32(1)),(0x6e4148,bytes(12))]:memory[a]=bytearray(b);checks[a]=bytearray(b)
    final=list(initial)
    if c['phase']=='done':
        final[0]=(initial[0]+offset[0])&0xffffffff;final[1]=(initial[1]+offset[1])&0xffffffff
        final[2]=0 if (final[0]>>12,final[1]>>12)==(6,7) else 4096
        oldcell=(7*32+6)*16;newcell=((final[1]>>12)*32+(final[0]>>12))*16
        if oldcell!=newcell:f.put(checks[f.MAP],oldcell,0,2);f.put(checks[f.MAP],newcell,100,2)
    states,_=initialized_states();cats,_=initialized_categories();animations=initialized_animations()
    pe=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    for a,b,w in [(SB,bytes([0xa5])*len(states),states),(CB,pe_read(pe,CB,len(cats)),cats),(AB,bytes([0xa5])*len(animations),animations)]:memory[a]=bytearray(b);checks[a]=bytearray(w)
    calls=int(c['phase']=='done' and c['local_owner']==7)
    memory[f.OUT]=bytearray(20);checks[f.OUT]=bytearray(struct.pack('<5I',1,cf.ACTOR,calls,1,int(c['phase']!='spawn' and not c.get('dispatch'))))
    driver=bytearray()
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(cf.DRIVER+len(driver)+5)))
    for target in [0x4798f0,0x47bbe0,0x4950c0]:call(target)
    emit(b'\x68'+pack32(0)+b'\x68'+pack32(f.OUT+4)+b'\x68'+pack32(8)+b'\xb9'+pack32(ROOM));call(0x4dc9a0);emit(b'\xa3'+pack32(f.OUT))
    if c['phase']!='spawn':
        if c['phase']=='done':
            tick=c.get('completion')=='native-tick';emit(b'\xb9'+pack32(cf.ACTOR if tick else cf.ACTOR+0x38d));call(0x496aa0 if tick else 0x4966a0)
        emit(b'\xb9'+pack32(cf.ACTOR+0x24e));call(0x476fc0 if c.get('dispatch') else 0x459c20);emit(b'\xa3'+pack32(f.OUT+16))
    emit(b'\x68'+pack32(f.OBS)+b'\xc3');memory[cf.DRIVER]=driver
    stack=bytearray([0xa5])*16384;f.put(stack,8192,cf.DRIVER);memory[0x7ffee000]=stack
    spans=probe.allowed_ranges(pe,[region(a,b) for a,b in memory.items()]);memory[probe.ADDRESS]=bytearray(probe.adapter(spans))
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=readout.code(cf.ACTOR+256*page)+bytes.fromhex('8b15')+pack32(probe.COUNTER)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args.update(memory=json.dumps(dict(regions=mem)))
    e.update(position=final,state=1 if c['phase']=='done' else 193,animation=0 if c['phase']=='done' else 49,
      initial_position=initial,checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])
    return args,e
def verify(c,b,e):
    u=lambda o,n=4:int.from_bytes(b[o:o+n],'little')
    assert len(b)==1343 and u(0)==0x66d524 and u(4,2)==100 and u(0x36f,1)==8 and u(0x370)==cf.DEFINITION
    assert u(0x40)==1000 and u(0x28,2)==7 and u(0x2a)==f.PLAYER and u(0x403,1)==1
    assert list(struct.unpack_from('<3I',b,0x1a))==e['position'] and u(0x25e)==e['state'] and u(0x3a9)==e['animation']
    assert u(0x4eb,2)==200 and u(0xf4,2)==0x400 and u(0x3e6)&2
    assert [u(o,2) for o in [0x95,0x97,0x99]]==([0,0,0] if c['phase']=='done' else [0,0,4096])
    if c['phase']=='done':assert list(struct.unpack_from('<3I',b,0xf6))==e['position']
    if c.get('dispatch'):assert u(0x24e+0x14)==100 and u(0x24e+0x1c)==int(c['phase']=='waiting')
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                chunks=[]
                for page in range(6):
                    args,e=build(c,page);path=ROOT/'ghidra'/f'phase91-portal-{c["name"]}-page{page}.json'
                    if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                    else:
                        response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                        saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                    r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c,page,r,e)
                    chunks.append(readout.decode(r['registers']));rows.append(dict(case=c['name'],page=page,file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],page,'verified',flush=True)
                b=b''.join(chunks)[:1343];verify(c,b,e);(ROOT/'ghidra'/f'phase91-portal-{c["name"]}.bin').write_bytes(b)
    if len(selected)==len(cases()):(ROOT/'phase91-portal-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
