"""Original Object factory, pool, constructor, init, tag/map/owner registration.

Original World/Object vtables and native code remain intact. Only external
GFX-null and sound vtable providers are synthetic. Two deterministic YMM
replays capture the complete 393-byte object; not an atomic live-game dump.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import memory_readout_ymm as readout
import fixture_read_probe as probe
ROOT=Path(__file__).resolve().parent
WORLD=0x10000000;SESSION=0x10009000;GFX=0x1000a000;GV=0x1000b000;SOUND=0x1000c000;SV=0x1000d000;DEFINITION=0x10010000
FH=0x10011000;FT=0x10012000;UH=0x10013000;UT=0x10014000;PLAYER=0x10018000;BLOCK=0x10016000;MAP=0x10020000;TERRAIN=0x10030000
OUT=0x10040000;ARGS=0x10050000;OBS=0x10060000;GFX_CB=0x10070000;SOUND_CB=0x10071000;KINDS=0x10072000
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')
def cases():
    base=dict(ident=17,seed=165,block_size=512,empty=False,gate=4094,next_tag=100,owner=7,position=[0x5800,0x6800,0x2000])
    return [dict(base,**kw,name=n) for n,kw in [('base',{}),('seed-zero',dict(seed=0)),('gold-bag',dict(ident=1)),('z-zero',dict(position=[0x5800,0x6800,0])),
      ('pool-exact',dict(block_size=408)),('pool-small',dict(block_size=404)),('pool-empty',dict(empty=True)),
      ('gate-zero',dict(gate=0)),('id-zero',dict(ident=0))]]
def build(c,page):
    world=bytearray(0x8000);session=bytearray(0x14);gv=bytearray(0xa4);sv=bytearray(0x24);player=bytearray(0x1200);definition=bytearray(285);cells=bytearray(32*32*16);terrain=bytearray(460)
    for o,v in [(0,0x66e3ec),(4,SESSION),(0x1c,WORLD),(0x36,5),(0x6e,UT),(0x72,FH),(0x76,0),(0x9e6,32),(0x9ee,MAP),(0x15f4,DEFINITION),(0x15fc,1)]:put(world,o,v)
    put(session,0x10,GFX);put(gv,0xa0,GFX_CB);put(sv,0x20,SOUND_CB)
    definition[0x113]=c['ident'];put(definition,0xfe,65535,2);put(definition,0x100,0x1234,2);put(definition,0x104,1000,2);put(definition,0x119,3,2)
    fh=bytearray(12);ft=bytearray(12);uh=bytearray(12);ut=bytearray(12);block=bytearray([c['seed']])*768
    struct.pack_into('<III',fh,0,0,0,FT if c['empty'] else BLOCK);struct.pack_into('<III',ft,0,0,FH if c['empty'] else BLOCK,0)
    struct.pack_into('<III',uh,0,0,0,UT);struct.pack_into('<III',ut,0,0,UH,0);struct.pack_into('<III',block,0,c['block_size'],FH,FT)
    tagptr=bytearray(4096*4);alive=bytearray(4096);put(tagptr,c['owner']*4,PLAYER);alive[c['owner']]=1
    globals_=pack32(c['gate'])+pack32(c['next_tag'])+pack32(200);kinds=bytes(20);forced=pack32(0)
    requested=408;success=bool(c['gate'] and not c['empty'] and c['block_size']>=requested and c['ident'])
    offset=c['block_size']-requested if c['block_size']-requested>12 else 0;node=BLOCK+offset+12;ident=c['next_tag']
    wants=[bytearray(x) for x in [world,player,cells,fh,ft,uh,ut,tagptr,alive,globals_,forced,kinds,block]]
    if success:
        put(wants[0],0x76,requested);put(wants[0],0x2a,1);put(wants[1],10,ident,2);put(wants[1],0x1e,1)
        cell=((c['position'][1]>>12)*32+(c['position'][0]>>12))*16;put(wants[2],cell,ident,2)
        if offset:put(wants[12],0,offset)
        else:put(wants[3],8,FT);put(wants[4],4,FH)
        struct.pack_into('<III',wants[12],offset,requested if offset else c['block_size'],UH,UT);put(wants[5],8,BLOCK+offset);put(wants[6],4,BLOCK+offset)
        put(wants[7],ident*4,node);wants[8][ident]=1;put(wants[9],0,c['gate']-1);put(wants[9],4,ident+1);put(wants[11],4,ident,2)
    out=pack32(0)+pack32(0xcafebabe)+bytes(8);want_out=pack32(int(success))+pack32(node if success else 0xcafebabe)+pack32(2 if success else 0)+pack32(int(success))
    buffers=[(WORLD,world,wants[0]),(PLAYER,player,wants[1]),(MAP,cells,wants[2]),(FH,fh,wants[3]),(FT,ft,wants[4]),(UH,uh,wants[5]),(UT,ut,wants[6]),
      (0x6ee540,tagptr,wants[7]),(0x6ed540,alive,wants[8]),(0x6a4818,globals_,wants[9]),(0x6f2540,forced,wants[10]),(KINDS,kinds,wants[11]),
      (BLOCK,block,None),(SESSION,session,session),(GFX,pack32(GV),pack32(GV)),(GV,gv,gv),(SOUND,pack32(SV),pack32(SV)),(SV,sv,sv),
      (DEFINITION,definition,definition),(TERRAIN,terrain,terrain),(OUT,out,want_out),(ARGS,struct.pack('<3I',*c['position']),struct.pack('<3I',*c['position'])),
      (0,bytes(4),bytes(4)),(0x6e5050,pack32(WORLD),pack32(WORLD)),(0x6e504c,pack32(KINDS),pack32(KINDS)),(0x6da4a8,pack32(TERRAIN),pack32(TERRAIN)),(0x75b468,pack32(SOUND),pack32(SOUND))]
    sound=bytes.fromhex('ff05')+pack32(OUT+8)+b'\xb8'+pack32(0x13572468)+bytes.fromhex('c21000')
    gfx=bytes.fromhex('ff05')+pack32(OUT+12)+bytes.fromhex('33c0c21400')
    stack=bytearray([0xa5])*8192;struct.pack_into('<5I',stack,4096,OBS,c['ident'],c['owner'],ARGS,OUT+4)
    mem=[region(a,b) for a,b,_ in buffers]+[region(GFX_CB,gfx),region(SOUND_CB,sound),region(0x7ffef000,stack)]
    rtti=bool(success and c['position'][2]<0x1000)
    if rtti:
        spans=probe.allowed_ranges((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes(),mem)
        mem.extend([region(probe.ADDRESS,probe.adapter(spans)),region(probe.COUNTER,bytes(8))])
    observer=b'\xa3'+pack32(OUT)+b'\xbd'+pack32(0);checks=[(a,w) for a,_,w in buffers if w is not None]
    if rtti:checks.append((probe.COUNTER+4,bytes(4)))
    if success:checks.extend([(BLOCK,wants[12][:offset+12]),(node+393,wants[12][offset+12+393:])])
    else:checks.append((BLOCK,block))
    at=0x11000000
    for i,(address,data) in enumerate(checks):
        assert i<32;mem.append(region(at,data));observer+=compare_code(address,at,len(data))+bytes.fromhex('85c0750681cd')+pack32(1<<i);at+=((len(data)+255)//256)*256
    observer+=readout.code(node+256*page)
    if rtti:observer+=bytes.fromhex('8b1d')+pack32(probe.COUNTER)
    observer+=b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    expected=dict(success=success,node=node if success else None,entity_id=ident if success else None,checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks])
    args=dict(program='DKII-DX.exe',address='0x4b7a90',max_steps=300000,registers=json.dumps(dict(ECX=hex(WORLD+8),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,ECX,EIP,ESP,'+','.join('YMM'+str(i) for i in range(8)))
    if rtti:args['return_registers']+=',EBX'
    return args,expected
def verify_node(c,b,e):
    if not e['success']:return
    u=lambda o,n=4:int.from_bytes(b[o:o+n],'little')
    assert len(b)==393 and u(0)==0x66d654 and u(4,2)==e['entity_id'] and u(0x12)==2
    assert u(0x12c,1)==c['ident'] and u(0x12d)==DEFINITION and u(0x28,2)==c['owner'] and u(0x2a)==PLAYER
    assert u(0x40)==1000 and u(0x11e,2)==65535 and u(0x11c,2)==0x1234
    position=list(c['position']);position[2]=max(0x1000,position[2])
    assert list(struct.unpack_from('<3I',b,0x1a))==position
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
                for page in range(2):
                    args,expected=build(c,page);p=ROOT/'ghidra'/f'phase89-object-factory-{c["name"]}-page{page}.json'
                    if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==expected
                    else:
                        response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                        if response.isError or 'error' in r:raise RuntimeError(str(r))
                        saved=dict(case=c,page=page,input=args,expected=expected,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                    r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=0).items()),(c['name'],page,r,expected)
                    chunks.append(readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page))
                b=b''.join(chunks)[:393];verify_node(c,b,expected);(ROOT/'ghidra'/f'phase89-object-factory-{c["name"]}.bin').write_bytes(b);print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase89-object-factory-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
