"""Complete Creature shot wrappers with real factory/pool/registration/motion.

Null GFX and external sound adapters remain explicit. All original native
functions/vtables are intact. Trig data is a prepared cardinal-only fixture;
actual game table population and later Shot process ticks are not executed.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_native_shot_success import build as factory_build,cases as factory_cases,put
from emulate_native_shot_success import WORLD,WV,SESSION,PLAYER,MAP,OUT,OBS,KINDS,DEFINITION
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_spell_alternatives import full_intermediate,runtime_projection
from emulate_creature_shot_request import SOURCE,SOURCE_RAW
import memory_readout
ROOT=Path(__file__).resolve().parent
ACTOR=0x10080000;ACTOR_DEF=0x10090000;SPELL=0x10098000;TARGET=0x10099000
def cases():
    base=dict(alternative=[36,4],flags=0,shot=17,speed=0x1234,target_kind=None,allbits=False)
    variants=[('base',{}),('alt-zero',dict(alternative=[0,0])),('alt68',dict(alternative=[68,0])),('alt255',dict(alternative=[255,255])),
      ('flag80',dict(flags=0x80)),('flag-high-and80',dict(flags=0x80000080)),('speed-max',dict(speed=65535)),('spell-data-allbits',dict(allbits=True)),
      ('shot30-target-none',dict(shot=30)),('shot30-target-creature',dict(shot=30,target_kind=0)),('shot30-target-other-kind',dict(shot=30,target_kind=2))]
    return [dict(base,**kw,name=f'{a:x}-{n}',root=a) for a in [0x4982B0,0x4984D0] for n,kw in variants]
def build(c,page):
    f=dict(factory_cases()[0],gate=4093,shot=c['shot'],angle=512,speed=c['speed'])
    args,expect,parts=factory_build(f,page,True);node=parts['node'];inputs={int(r['address'],16):bytearray.fromhex(r['hex']) for r in parts['memory'] if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [OBS,0x7FFEF000]};checks={a:bytearray(b) for a,b in parts['checks']}
    def both(a,o,v,n=4):put(inputs[a],o,v,n);put(checks[a],o,v,n)
    both(WORLD,0x1614,SPELL);both(WORLD,0x161C,1);both(WV,0xE4,0x50E080);both(WV,0xE8,0x50DFA0);both(SESSION,4,100)
    both(0x6EE540,10*4,ACTOR);both(0x6ED540,10,1,1);both(KINDS,0,10,2);both(PLAYER,6,10,2);both(PLAYER,0x16,1)
    cell=((f['position'][1]>>12)*32+(f['position'][0]>>12))*16;put(inputs[MAP],cell,10,2)
    actor=bytearray(0x600);definition=bytearray(2199)
    for o,v,n in [(4,10,2),(0x12,0,4),(0x16,0,4),(0x26,0x1000,2),(0x28,7,2),(0x2A,PLAYER,4),(0xF4,512,2),
      (0x370,ACTOR_DEF,4),(0x399,1,4),(0x3A1,0xAABBCCDD,4),(0x3A5,0,4),(0x3E6,c['flags'],4),(0x3EA,0xF00D0234,4)]:put(actor,o,v,n)
    for i,v in enumerate(f['position']):put(actor,0x1A+4*i,v)
    target_position=[f['position'][0]+4096,f['position'][1],f['position'][2]]
    for i,v in enumerate(target_position):put(actor,0x38D+4*i,v)
    definition[0x789]=3
    for i,ident in enumerate([8,12,20]):definition[0x5E1+19*i]=ident;put(definition,0x5E1+19*i+2,5);definition[0x5E1+19*i+18]=1 if i==1 else 0
    raw=bytearray(SOURCE_RAW);raw[0xE7]=c['shot'];raw[0xE8:0xEA]=bytes(c['alternative'])
    if c['allbits']:put(raw,0xC8,0xFFFFFFFF);put(raw,0xCC,0x80000000)
    spell=runtime_projection(full_intermediate(raw));want_actor=bytearray(actor);put(want_actor,0xA,100,2);put(want_actor,0x2E,0x13572468);put(want_actor,0x3EA,0xF00D1234)
    if c['target_kind'] is not None:
        put(actor,0x39D,20,2);put(want_actor,0x39D,20,2);both(0x6EE540,20*4,TARGET);both(0x6ED540,20,1,1)
        target=bytearray(0x30);put(target,4,20,2);put(target,0x12,c['target_kind'])
        for i,v in enumerate(target_position):put(target,0x1A+4*i,v)
        inputs[TARGET]=target;checks[TARGET]=target
    coeff=bytearray(2560*4);put(coeff,512*4,65536);put(coeff,1536*4,0xFFFF0000)
    for a,b,w in [(ACTOR,actor,want_actor),(ACTOR_DEF,definition,definition),(SPELL,spell,spell),(0x6E4168,b'\x01',b'\x01'),(0x6E4178,bytes(19),bytes(19)),(0x6D6458,bytes(4),bytes(4)),(0x6C1488,coeff,coeff)]:inputs[a]=bytearray(b);checks[a]=bytearray(w)
    put(checks[OUT],0,0);put(checks[OUT],8,3)
    stack=bytearray([0xA5])*8192;put(stack,4096,OBS);inputs[0x7FFEF000]=stack
    observer=b'\xA3'+pack32(OUT)+b'\xA1'+pack32(0x6EE540+100*4)+b'\xA3'+pack32(OUT+4)+b'\xBD'+pack32(1)
    mem=[region(a,b) for a,b in inputs.items()];at=0x11000000
    for address,b in checks.items():mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(node+128*page)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args.update(address=hex(c['root']),memory=json.dumps(dict(regions=mem)),registers=json.dumps(dict(ECX=hex(ACTOR),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')))
    expected=dict(node=node,entity_id=100,native_wrapper_return=0,primary_shot=c['shot'],data1=int.from_bytes(spell[0x1F:0x23],'little'),data2=int.from_bytes(spell[0x23:0x27],'little'),
      target=target_position,origin=f['position'],creator=10,speed=c['speed'],owner=7,
      homing_short_branch=c['root']==0x4984D0 and c['shot']==30,valid_homing_target=c['target_kind']==0,
      checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])
    return args,expected
def verify_node(c,b,e):
    assert len(b)==371
    u=lambda at,n=4:int.from_bytes(b[at:at+n],'little')
    assert u(0)==0x66D86C and u(4,2)==100 and u(0x12)==1 and u(0x28,2)==7 and u(0x125)==DEFINITION
    assert u(0x124,1)==c['shot'] and u(0x13D)==e['data1'] and u(0x141)==e['data2'] and u(0x145,2)==10
    assert u(0xC,2)==10 and u(0x11C,2)==c['speed']
    assert u(0x151)==int(c['root']==0x4984D0 and bool(c['flags']&0x80))
    assert u(0x165)==int(c['root']==0x4984D0 and bool(c['flags']&0x80) and bool(c['flags']&0x80000000))
    if e['homing_short_branch']:
        assert u(0x147,2)==(20 if e['valid_homing_target'] else 0)
        assert list(struct.unpack_from('<3I',b,0x131))==(e['target'] if e['valid_homing_target'] else [0,0,0])
        assert list(struct.unpack_from('<3I',b,0x104))==[0,0,0]
    else:
        assert list(struct.unpack_from('<3I',b,0x131))==e['target']
        assert list(struct.unpack_from('<3I',b,0x104))==[c['speed']<<4,0,0]
        assert u(0x102,2)==u(0xF4,2)==512
    return True
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
            for page in range(3):
                args,expected=build(c,page);p=ROOT/'ghidra'/f'phase80-emulation-native-creature-shot-{c["name"]}-page{page}.json'
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==expected
                else:
                    response=await session.call_tool('emulate_function',args);value='\n'.join(x.text for x in response.content if x.type=='text');r=json.loads(value)
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,page=page,input=args,expected=expected,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],page,r,expected)
                chunks.append(memory_readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page))
            node=b''.join(chunks)[:371];verify_node(c,node,expected);(ROOT/'ghidra'/f'phase80-native-creature-shot-{c["name"]}.bin').write_bytes(node);print(c['name'],'verified;full native-created Shot captured',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase80-native-creature-shot-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
