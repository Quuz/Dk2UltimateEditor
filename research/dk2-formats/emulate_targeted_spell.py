"""Actual third CreatureSpell wrapper498870 including sprintf,target getters and factory.

Original code/vtables are retained. Engine GFX/sound use the phase80 external
providers;prepared cardinal coefficients,entity lists and definitions bound
the claim. Each371-byte Shot snapshot is assembled from3 fresh native runs.
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
from emulate_creature_shot_request import SOURCE_RAW
from native_targeted_spell import prediction,u32,trunc
import memory_readout
ROOT=Path(__file__).resolve().parent
ACTOR=0x10080000;ACTOR_DEF=0x10090000;SPELL=0x10098000;TARGET=0x10099000;TARGET_DEF=0x1009A000;VARIABLES=0x1009B000

def cases():
    base=dict(alternative=[36,4],flags=0,shot=17,speed=0x1234,target_kind=0,target_alive=1,allbits=False,
      target_position=[0x6800,0x6800,0x2000],delta=[0,0,0],height=2048,level=1,height_percent=150,gate=4093,pool_size=512,tick=100,offsets=[0,0,0])
    variants=[('base',{}),('moving-x',dict(delta=[256,0,0])),('moving-y',dict(delta=[0,-256,0])),('moving-xy',dict(delta=[256,-256,0])),
      ('moving-z',dict(delta=[0,0,4096])),('distance-two-tiles',dict(target_position=[0x7800,0x6800,0x2000],delta=[256,0,0])),
      ('distance-half-tile',dict(target_position=[0x6000,0x6800,0x2000],delta=[256,0,0])),
      ('coincident',dict(target_position=[0x5800,0x6800,0x2000],delta=[256,0,0])),('height-odd',dict(height=4095)),
      ('height-level2',dict(level=2)),('height-max',dict(height=65535)),('target-object',dict(target_kind=2)),('target-shot',dict(target_kind=1)),
      ('target-kind9',dict(target_kind=9)),('target-dead',dict(target_alive=0)),('shot-zero',dict(shot=0)),('pool-small',dict(pool_size=380)),
      ('factory-gate-zero',dict(gate=0)),('waiting',dict(tick=5)),('flag80-bypasses-wait',dict(flags=0x80,tick=5,offsets=[256,128,64])),
      ('origin-offsets',dict(offsets=[256,128,64])),('alt-zero',dict(alternative=[0,0])),('alt68',dict(alternative=[68,0])),
      ('alt255',dict(alternative=[255,255])),('shot30',dict(shot=30)),('data-allbits',dict(allbits=True)),('speed-max',dict(speed=65535))]
    return [dict(base,**kw,name=name) for name,kw in variants]

def build(c,page):
    waiting=not(c['flags']&0x80) and c['tick']<=5
    attempt=not waiting and c['shot']!=0 and c['target_alive']!=0 and c['target_kind']!=9
    success=attempt and c['gate']!=0 and c['pool_size']>=384
    f=dict(factory_cases()[0],gate=c['gate'] if attempt else 0,block_size=c['pool_size'],shot=c['shot'],angle=512,speed=c['speed'])
    args,expect,parts=factory_build(f,page,True);node=parts['node']
    inputs={int(r['address'],16):bytearray.fromhex(r['hex']) for r in parts['memory'] if int(r['address'],16)<0x11000000 and int(r['address'],16) not in [OBS,0x7FFEF000]}
    checks={a:bytearray(b) for a,b in parts['checks']}
    def both(a,o,v,n=4):put(inputs[a],o,v,n);put(checks[a],o,v,n)
    both(0x6A4818,0,c['gate']);put(checks[0x6A4818],0,c['gate']-int(success))
    both(WORLD,0x1614,SPELL);both(WORLD,0x161C,1);both(WV,0xE4,0x50E080);both(WV,0xE8,0x50DFA0);both(SESSION,4,c['tick'])
    both(0x6EE540,10*4,ACTOR);both(0x6ED540,10,1,1);both(KINDS,0,10,2);both(PLAYER,6,10,2);both(PLAYER,0x16,1)
    cell=((f['position'][1]>>12)*32+(f['position'][0]>>12))*16;put(inputs[MAP],cell,10,2)
    if not success:put(checks[MAP],cell,10,2)
    actor=bytearray(0x600);definition=bytearray(2199)
    for o,v,n in [(4,10,2),(0x12,0,4),(0x16,0,4),(0x26,0x1000,2),(0x28,7,2),(0x2A,PLAYER,4),(0xF4,512,2),
      (0x370,ACTOR_DEF,4),(0x399,1,4),(0x39D,20,2),(0x3A1,0xAABBCCDD,4),(0x3A5,0,4),(0x3E6,c['flags'],4),(0x3EA,0xF00D0234,4)]:put(actor,o,v,n)
    for i,v in enumerate(f['position']):put(actor,0x1A+4*i,v)
    definition[0x789]=3
    for i,ident in enumerate([8,12,20]):
        at=0x5E1+19*i;definition[at]=ident;put(definition,at+2,5);definition[at+18]=int(i==1)
        if i==1:
            for j,v in enumerate(c['offsets']):put(definition,at+6+4*j,v)
    raw=bytearray(SOURCE_RAW);raw[0xE7]=c['shot'];raw[0xE8:0xEA]=bytes(c['alternative'])
    if c['allbits']:put(raw,0xC8,0xFFFFFFFF);put(raw,0xCC,0x80000000)
    spell=runtime_projection(full_intermediate(raw));want_actor=bytearray(actor);put(want_actor,0x3EA,0xF00D1234)
    if success:put(want_actor,0xA,100,2);put(want_actor,0x2E,0x13572468)
    both(0x6EE540,20*4,TARGET);both(0x6ED540,20,c['target_alive'],1)
    vt={0:0x66D524,1:0x66D86C,2:0x66D654,9:0x66D654}[c['target_kind']]
    target=bytearray(0x410);targetdef=bytearray(2199);variables=bytearray(0x300)
    for o,v,n in [(0,vt,4),(4,20,2),(0x12,c['target_kind'],4),(0x125,TARGET_DEF,4),(0x370,TARGET_DEF,4),(0x403,c['level'],1)]:put(target,o,v,n)
    previous=[u32(c['target_position'][i]-c['delta'][i]) for i in range(3)]
    for i,v in enumerate(c['target_position']):put(target,0x1A+4*i,v)
    for i,v in enumerate(previous):put(target,0xF6+4*i,v)
    put(targetdef,0x648,c['height'],2);put(targetdef,0x4F,0x1800,2);put(variables,0xCF+2*c['level'],c['height_percent'],2)
    height=(c['height'] if c['level']==1 else trunc(c['height']*c['height_percent'],100)) if c['target_kind']==0 else (0x3000 if c['target_kind']==1 else 0)
    aim=prediction(f['position'],c['target_position'],previous,height)
    coeff=bytearray(2560*4);put(coeff,512*4,65536);put(coeff,1536*4,0xFFFF0000)
    formatted=bytearray([0xCC])*128;wanted_format=bytearray(formatted)
    if not waiting:
        message=b'Creature Spell 12 has no Shot Type associated\0';wanted_format[:len(message)]=message
    for a,b,w in [(ACTOR,actor,want_actor),(ACTOR_DEF,definition,definition),(SPELL,spell,spell),(TARGET,target,target),(TARGET_DEF,targetdef,targetdef),
      (VARIABLES,variables,variables),(0x6E4154,pack32(VARIABLES),pack32(VARIABLES)),(0x740C20,formatted,wanted_format),
      (0x6E4168,b'\x01',b'\x01'),(0x6E4178,bytes(19),bytes(19)),(0x6D6458,bytes(4),bytes(4)),(0x6C1488,coeff,coeff)]:inputs[a]=bytearray(b);checks[a]=bytearray(w)
    put(checks[OUT],0,int(waiting));put(checks[OUT],4,node if success else 0);put(checks[OUT],8,3 if success else 0)
    stack=bytearray([0xA5])*8192;put(stack,4096,OBS);inputs[0x7FFEF000]=stack
    observer=b'\xA3'+pack32(OUT)+b'\xA1'+pack32(0x6EE540+100*4)+b'\xA3'+pack32(OUT+4)+b'\xBD'+pack32(1)+b'\x31\xDB'
    mem=[region(a,b) for a,b in inputs.items()];at=0x11000000
    for i,(address,b) in enumerate(checks.items()):
        mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(node+128*page)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args.update(address='0x498870',memory=json.dumps(dict(regions=mem)),registers=json.dumps(dict(ECX=hex(ACTOR),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')))
    origin=[u32(f['position'][0]-(0 if c['flags']&0x80 else c['offsets'][1])),u32(f['position'][1]+(0 if c['flags']&0x80 else c['offsets'][0])),u32(f['position'][2]+c['offsets'][2])]
    return args,dict(success=success,node=node,waiting=waiting,prediction=aim,origin=origin,target_height=height,
      data1=int.from_bytes(spell[0x1F:0x23],'little'),data2=int.from_bytes(spell[0x23:0x27],'little'),checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])

def verify_node(c,b,e):
    assert len(b)==371
    if not e['success']:assert b==bytes([165])*371;return
    u=lambda o,n=4:int.from_bytes(b[o:o+n],'little')
    assert u(0)==0x66D86C and u(4,2)==100 and u(0x12)==1 and u(0x28,2)==7 and u(0x125)==DEFINITION
    assert u(0x124,1)==c['shot'] and u(0x13D)==e['data1'] and u(0x141)==e['data2'] and u(0x145,2)==10 and u(0x147,2)==20 and u(0x16D,2)==10
    assert list(struct.unpack_from('<3I',b,0x131))==e['prediction']['aim'] and list(struct.unpack_from('<3I',b,0xF6))==e['origin']
    assert u(0x11C,2)==c['speed']

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
                args,e=build(c,page);p=ROOT/'ghidra'/f'phase82-emulation-targeted-spell-v2-{c["name"]}-page{page}.json'
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c['name'],page,r,e)
                chunks.append(memory_readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page))
            node=b''.join(chunks)[:371];verify_node(c,node,e);(ROOT/'ghidra'/f'phase82-targeted-spell-{c["name"]}.bin').write_bytes(node);print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase82-targeted-spell-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
