"""Real Shot factory/pool/tag/base/movement/resource setup; null GFX provider.

Original native code and original Shot vtable are untouched. Graphics request
and sound are external vtable adapters. Empty map/terrain,finite pool lists,
owner entity and tag arrays are explicit data. Three identical native replays
expose all371 object bytes via post-return XMM readout; opaque bytes stay raw.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import memory_readout
ROOT=Path(__file__).resolve().parent
WORLD=0x10000000;WV=0x10008000;SESSION=0x10009000;GFX=0x1000A000;GV=0x1000B000;SOUND=0x1000C000;SV=0x1000D000;DEFINITION=0x10010000
FH=0x10011000;FT=0x10012000;UH=0x10013000;UT=0x10014000;PLAYER=0x10015000;BLOCK=0x10016000;MAP=0x10020000;TERRAIN=0x10030000
OUT=0x10040000;ARGS=0x10050000;OBS=0x10060000;GFX_CB=0x10070000;SOUND_CB=0x10071000;KINDS=0x10072000
def cases():
    base=dict(shot=17,seed=165,block_size=512,empty=False,gate=4094,next_tag=100,occupied=False,forced=False,sound=1,position=[0x5800,0x6800,0x2000],angle=3,data1=0x11223344,data2=0x55667788,speed=0x1234)
    return [dict(base,**kw,name=n) for n,kw in [('base',{}),('seed-zero',dict(seed=0)),('sound-zero',dict(sound=0)),('id255',dict(shot=255)),
      ('pool-exact',dict(block_size=384)),('pool-absorbs12',dict(block_size=396)),('pool-small',dict(block_size=380)),('pool-empty',dict(empty=True)),
      ('gate-zero',dict(gate=0)),('last-tag',dict(gate=1)),('tag-occupied',dict(occupied=True)),('tag-forced',dict(forced=True)),
      ('z-high-clamp',dict(position=[0x5800,0x6800,0x4000])),('parameters-allbits',dict(data1=0xFFFFFFFF,data2=0x80000000,speed=0xFFFF))]]
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')
def build(c,page,return_parts=False):
    world=bytearray(0x7300);wv=bytearray(0x1B4);session=bytearray(0x14);gv=bytearray(0xA4);sv=bytearray(0x24);player=bytearray(512);definition=bytearray(118);cells=bytearray(32*32*16);terrain=bytearray(460)
    for o,v in [(0,WV),(4,SESSION),(0x1C,WORLD),(0x36,5),(0x6E,UT),(0x72,FH),(0x76,0),(0x9E6,32),(0x9EE,MAP),(0x1624,DEFINITION),(0x162C,1)]:put(world,o,v)
    put(wv,0x1AC,0x50E2D0);put(wv,0x1B0,0x50E1E0);put(session,0x10,GFX);put(gv,0xA0,GFX_CB);put(sv,0x20,SOUND_CB)
    definition[0x58]=c['shot'];put(definition,0x37,c['data1']);put(definition,0x3B,c['data2']);put(definition,0x47,65535,2);put(definition,0x4B,c['speed'],2);put(definition,0x4D,0xFEDC,2);put(definition,0x72,0x76543210)
    fh=bytearray(12);ft=bytearray(12);uh=bytearray(12);ut=bytearray(12);block=bytearray([c['seed']])*768
    struct.pack_into('<III',fh,0,0,0,FT if c['empty'] else BLOCK);struct.pack_into('<III',ft,0,0,FH if c['empty'] else BLOCK,0)
    struct.pack_into('<III',uh,0,0,0,UT);struct.pack_into('<III',ut,0,0,UH,0);struct.pack_into('<III',block,0,c['block_size'],FH,FT)
    tagptr=bytearray(4096*4);alive=bytearray(4096);put(tagptr,7*4,PLAYER);alive[7]=1
    if c['occupied']:put(tagptr,c['next_tag']*4,0x10073000);alive[c['next_tag']]=1
    globals_=pack32(c['gate'])+pack32(c['next_tag'])+pack32(200);forced=pack32(int(c['forced']));kinds=bytes(20)
    requested=384;success=c['gate']!=0 and not c['empty'] and c['block_size']>=requested and c['shot']!=0
    offset=c['block_size']-requested if c['block_size']-requested>12 else 0;node=BLOCK+offset+12
    ident=200 if c['forced'] else c['next_tag']+int(c['occupied'])
    wants=[bytearray(x) for x in [world,player,cells,fh,ft,uh,ut,tagptr,alive,globals_,forced,kinds,block]]
    if success:
        put(wants[0],0x76,requested);put(wants[0],0x36,6);put(wants[1],8,ident,2);put(wants[1],0x1A,1)
        cell=((c['position'][1]>>12)*32+(c['position'][0]>>12))*16;put(wants[2],cell,ident,2)
        if offset:put(wants[12],0,offset)
        else:put(wants[3],8,FT);put(wants[4],4,FH)
        struct.pack_into('<III',wants[12],offset,requested if offset else c['block_size'],UH,UT);put(wants[5],8,BLOCK+offset);put(wants[6],4,BLOCK+offset)
        put(wants[7],ident*4,node);wants[8][ident]=1;put(wants[9],0,c['gate']-1)
        if c['forced']:put(wants[10],0,0)
        else:put(wants[9],4,ident+1)
        put(wants[11],2,ident,2)
    wanted_out=pack32(int(success))+pack32(node if success else 0xCAFEBABE)+pack32((1+int(bool(c['sound']))) if success else 0)+pack32(int(success))
    buffers=[(WORLD,world,wants[0]),(PLAYER,player,wants[1]),(MAP,cells,wants[2]),(FH,fh,wants[3]),(FT,ft,wants[4]),(UH,uh,wants[5]),(UT,ut,wants[6]),
      (0x6EE540,tagptr,wants[7]),(0x6ED540,alive,wants[8]),(0x6A4818,globals_,wants[9]),(0x6F2540,forced,wants[10]),(KINDS,kinds,wants[11]),
      (BLOCK,block,None),(WV,wv,wv),(SESSION,session,session),(GFX,pack32(GV),pack32(GV)),(GV,gv,gv),(SOUND,pack32(SV),pack32(SV)),(SV,sv,sv),
      (DEFINITION,definition,definition),(TERRAIN,terrain,terrain),(OUT,pack32(0)+pack32(0xCAFEBABE)+bytes(8),wanted_out),
      (ARGS,struct.pack('<3IHH',*c['position'],c['angle'],0),struct.pack('<3IHH',*c['position'],c['angle'],0)),
      (0,bytes(4),bytes(4)),(0x6E5050,pack32(WORLD),pack32(WORLD)),(0x6E504C,pack32(KINDS),pack32(KINDS)),(0x6DA4A8,pack32(TERRAIN),pack32(TERRAIN)),(0x75B468,pack32(SOUND),pack32(SOUND))]
    sound=bytes.fromhex('ff05')+pack32(OUT+8)+b'\xB8'+pack32(0x13572468)+bytes.fromhex('c21000')
    gfx=bytes.fromhex('ff05')+pack32(OUT+12)+bytes.fromhex('33c0c21400')
    stack=bytearray([0xA5])*8192;struct.pack_into('<7I',stack,4096,OBS,c['shot'],7,ARGS,ARGS+12,OUT+4,c['sound'])
    mem=[region(a,b) for a,b,_ in buffers]+[region(GFX_CB,gfx),region(SOUND_CB,sound),region(0x7FFEF000,stack)]
    observer=b'\xA3'+pack32(OUT)+b'\xBD'+pack32(0);checks=[(a,w) for a,_,w in buffers if w is not None]
    if success:checks.extend([(BLOCK,wants[12][:offset+12]),(node+371,wants[12][offset+12+371:])])
    else:checks.append((BLOCK,block))
    at=0x11000000
    for i,(address,b) in enumerate(checks):
        assert i<32;mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('85c0750681cd')+pack32(1<<i);at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(node+128*page)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    expected=dict(success=success,node=node if success else None,entity_id=ident if success else None,checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks])
    args=dict(program='DKII-DX.exe',address='0x4b7cd0',max_steps=100000,registers=json.dumps(dict(ECX=hex(WORLD+8),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,'+memory_readout.REGISTERS)
    if return_parts:return args,expected,dict(memory=mem,checks=checks,node=node,observer=observer)
    return args,expected
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
                args,expected=build(c,page);p=ROOT/'ghidra'/f'phase80-emulation-native-shot-v2-{c["name"]}-page{page}.json'
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==expected
                else:
                    response=await session.call_tool('emulate_function',args);value='\n'.join(x.text for x in response.content if x.type=='text');r=json.loads(value)
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,page=page,input=args,expected=expected,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=0).items()),(c['name'],page,r,expected)
                chunks.append(memory_readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page))
            node=b''.join(chunks)[:371];(ROOT/'ghidra'/f'phase80-native-shot-{c["name"]}.bin').write_bytes(node);print(c['name'],'verified;raw node captured',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase80-native-shot-success-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
