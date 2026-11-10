"""Native spawnElement→pool→construct/register→full initialize.

Original CBridge/element vtables,actual exists/get,map geometry and PRNG run.
External connected-engine visibility/obstruction/tile-touch/floor callbacks
are explicit providers. Uniform coefficient data isolates range sampling;
no claim of real angle tables/renderer. Parent spread0,resource type0/1.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_element_initialization import plan
import memory_readout
ROOT=Path(__file__).resolve().parent
BRIDGE=0x10000000;SESSION=0x10002000;ENGINE=0x10003000;EV=0x10004000;PARENT=0x10005000;PARENT_DEF=0x10006000;DEF=0x10007000;CELLS=0x10008000
FH=0x10009000;FT=0x1000A000;UH=0x1000B000;UT=0x1000C000;BLOCK=0x1000D000;NODE=BLOCK+736;ARGS=0x1000E000;OUT=0x1000F000;OBS=0x10010000;ALLOW=0x10012000;BLOCKED=0x10012100;TOUCH=0x10012200;FLOOR=0x10012300
def cases():
    base=dict(seed=1,fill=165,id=1,id_argument=1,xy=[10,10],z=[0,0],hp=[100,100],scale=[4096,4096],fade=70,palette=[0,0],resource_type=0,resource_flags=1,
      flags=0,position=[0xA800,0xA800,0x2000],pool_size=1024,free_ids=4094)
    variants=[('base',{}),('palette-random',dict(palette=[255,0])),('palette-table',dict(palette=[0,255])),('palette-both',dict(palette=[255,255])),
      ('xy-negative-fixed',dict(xy=[0xFFFF,0xFFFF])),('z-negative-fixed',dict(z=[0x8000,0x8000])),('xy-signed-cross',dict(xy=[0xFFFE,2])),
      ('xy-reversed-sign',dict(xy=[0x7FFF,0x8000])),('z-reversed',dict(z=[5,2])),
      ('hp-variable',dict(hp=[10,12])),('hp-maximum',dict(hp=[65535,65535])),('hp-reversed',dict(hp=[12,10])),
      ('scale-variable',dict(scale=[10,12])),('scale-maximum',dict(scale=[65535,65535])),('scale-reversed',dict(scale=[12,10])),
      ('fade100',dict(fade=100)),('render-disabled',dict(resource_flags=0)),('sprite-white-priority',dict(resource_type=1,resource_flags=3)),
      ('id-high-slot-bits',dict(id_argument=0xABCD0001)),('id65535',dict(id=65535,id_argument=65535)),
      ('pool-small',dict(pool_size=296)),('no-free-id',dict(free_ids=0)),('outside-map',dict(position=[0xFFFFFFFF,0xA800,0x2000])),
      ('no-cull-flag',dict(flags=0x200))]
    result=[dict(base,**kw,name=n) for n,kw in variants]
    # Distinguishing upper-bound controls choose seeds using the independently
    # verified native LCG contract; actual complete native chain checks them.
    for field,pair,target,name in [('hp',[10,12],12,'hp-includes-upper'),('scale',[10,12],11,'scale-last-before-upper')]:
        for seed in range(1000):
            c=dict(base,seed=seed,**{field:pair});p=plan(c)
            if p['lifetime' if field=='hp' else 'scale']==target:result.append(dict(c,name=name));break
        else:raise AssertionError('No distinguishing seed')
    return result
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')
def callback(at,value,cleanup):return bytes.fromhex('ff05')+pack32(OUT+at)+b'\xB8'+pack32(value)+b'\xC2'+struct.pack('<H',cleanup)
def build(c,page):
    bridge=bytearray(0x1380);session=bytearray(0x18);engine=pack32(EV);ev=bytearray(0x210);parent=bytearray(0x160);parentdef=bytearray(209);definition=bytearray(79);cells=bytearray(32*32*2)
    for o,v in [(0,0x66CC94),(4,SESSION),(0x76,DEF),(0x7E,1),(0x9E,ENGINE),(0x32A,CELLS),(0x32E,32),(0x332,32),(0x1357,UT),(0x135B,FH)]:put(bridge,o,v)
    put(session,0x14,ENGINE)
    for o,fn in [(0x80,ALLOW),(0x20C,BLOCKED),(0x1D4,TOUCH),(0x204,FLOOR)]:put(ev,o,fn)
    put(parent,0xF2,2,2);put(parent,0x107,PARENT_DEF);put(parent,0x12D,7,2);put(parent,0x155,0xA1B2C3D4);put(parent,0x81,0x10203040)
    definition[0xA]=c['resource_type'];put(definition,0xB,c['resource_flags']);put(definition,0x1F,c['flags'])
    for at,pair in [(0x23,c['hp']),(0x2D,c['scale']),(0x31,c['xy']),(0x35,c['z'])]:
        put(definition,at,pair[0],2);put(definition,at+2,pair[1],2)
    definition[0x3D]=c['fade'];put(definition,0x3E,c['id'],2);definition[0x4A:0x4D]=bytes([10,20,30]);definition[0x4D:0x4F]=bytes(c['palette'])
    fh=bytearray(12);ft=bytearray(12);uh=bytearray(12);ut=bytearray(12);block=bytearray([c['fill']])*1152
    struct.pack_into('<III',fh,0,0,0,BLOCK);struct.pack_into('<III',ft,0,0,BLOCK,0);struct.pack_into('<III',uh,0,0,0,UT);struct.pack_into('<III',ut,0,0,UH,0);struct.pack_into('<III',block,0,c['pool_size'],FH,FT)
    table=bytearray(4096*4);put(table,2*4,PARENT)
    inside=all(0<=v<32*4096 for v in c['position'][:2]);success=inside and c['free_ids']!=0 and c['pool_size']>=300
    p=plan(c) if success else dict(seed=c['seed']);wanted=[bytearray(x) for x in [bridge,parent,cells,fh,ft,uh,ut,block,table]]
    if success:
        assert c['pool_size']==1024
        put(wanted[0],0x135F,300);put(wanted[1],0x113,NODE)
        cell=((c['position'][1]>>12)*32+(c['position'][0]>>12))*2;put(wanted[2],cell,1,2)
        put(wanted[5],8,BLOCK+724);put(wanted[6],4,BLOCK+724);put(wanted[7],0,724);struct.pack_into('<III',wanted[7],724,300,UH,UT);put(wanted[8],4,NODE)
    output=bytearray(24);put(output,0,int(success));put(output,4,NODE if success else 0xCAFEBABE);put(output,8,int(not(c['flags']&0x200)))
    put(output,12,int(inside));put(output,16,int(inside));put(output,20,int(success))
    fixed=[(BRIDGE,bridge,wanted[0]),(PARENT,parent,wanted[1]),(CELLS,cells,wanted[2]),(FH,fh,wanted[3]),(FT,ft,wanted[4]),(UH,uh,wanted[5]),(UT,ut,wanted[6]),(BLOCK,block,None),(0x6D6458,table,wanted[8]),
      (SESSION,session,session),(ENGINE,engine,engine),(EV,ev,ev),(PARENT_DEF,parentdef,parentdef),(DEF,definition,definition),
      (0x6DA458,pack32(BRIDGE),pack32(BRIDGE)),(0x6DA45C,pack32(ENGINE),pack32(ENGINE)),(0x6D6450,pack32(c['seed']),pack32(p['seed'])),
      (0x69793C,pack32(c['free_ids']),pack32(c['free_ids']-int(success))),(0,bytes(4),bytes(4)),(OUT,pack32(0)+pack32(0xCAFEBABE)+bytes(16),output),
      (ARGS,struct.pack('<3IHH',*c['position'],123,0),struct.pack('<3IHH',*c['position'],123,0)),(0x6C1488,pack32(65536)*2560,pack32(65536)*2560)]
    mem=[region(a,b) for a,b,_ in fixed]+[region(ALLOW,callback(8,1,8)),region(BLOCKED,callback(12,0,4)),region(TOUCH,callback(16,0,4)),region(FLOOR,callback(20,0,4))]
    stack=bytearray([0xA5])*8192;struct.pack_into('<5I',stack,4096,OBS,c['id_argument'],ARGS,ARGS+12,OUT+4);mem.append(region(0x7FFEF000,stack))
    observer=b'\xA3'+pack32(OUT)+b'\xBD'+pack32(1);checks=[(a,w) for a,_,w in fixed if w is not None]
    if success:checks.extend([(BLOCK,wanted[7][:736]),(NODE+287,wanted[7][736+287:])])
    else:checks.append((BLOCK,block))
    at=0x11000000
    for a,b in checks:mem.append(region(at,b));observer+=compare_code(a,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(NODE+128*page)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args=dict(program='DKII-DX.exe',address='0x4352e0',max_steps=100000,registers=json.dumps(dict(ECX=hex(PARENT),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,'+memory_readout.REGISTERS)
    return args,dict(success=success,plan=p,node=NODE if success else None)
def verify_node(c,b,e):
    if not e['success']:assert b==bytes([c['fill']])*287;return
    u=lambda o,n=4:int.from_bytes(b[o:o+n],'little');p=e['plan'];assert len(b)==287 and u(0)==0x66CBDC and u(0xF2,2)==1 and u(0x101,2)==c['id'] and u(0x103)==DEF
    assert u(0x107,2)==7 and u(0x111,2)==2 and u(0x113)==0xA1B2C3D4 and u(0x117)==u(0x11B)==0
    assert list(struct.unpack_from('<3I',b,8))==p['velocity'] and list(struct.unpack_from('<3I',b,0xD6))==c['position']
    assert u(0x109)==p['lifetime'] and u(0x10D)==p['alpha_threshold']
    if c['resource_flags']&1:
        assert u(0x2A)==u(0x2E)==p['scale'] and u(0x81)==0x10203040
        assert u(0x6E)==(0xFFFFFFFF if c['resource_type']==1 else 0xFF1E140A)
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
                args,e=build(c,page);path=ROOT/'ghidra'/f'phase81-emulation-element-{c["name"]}-page{page}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],page,r,e)
                chunks.append(memory_readout.decode(r['registers']));rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name'],page=page))
            node=b''.join(chunks)[:287];verify_node(c,node,e);(ROOT/'ghidra'/f'phase81-element-{c["name"]}.bin').write_bytes(node);print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase81-element-initialization-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
