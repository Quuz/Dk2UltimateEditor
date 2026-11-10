"""Full native Terrain cache initialization and tile-light definition selection.

Cache compositions execute real enumeration/getter/count functions. Selection
executes real getter38 but world-vslot21C is an explicit capture/result fixture.
No native instructions are overwritten; observer starts after native return.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_terrain_cache import enumerate_id,cache_ids,light_selection
ROOT=Path(__file__).resolve().parent
PROFILES={
 'DX':dict(program='DKII-DX.exe',init=0x455D40,select=0x452DC0,enum=0x50CEB0,getter=0x508DD0,count=0x508DF0,cache=0x6DA4A8,global_world=0x6DA4A4,vtable=0x66E3EC),
 'old':dict(program='DKII-local13.exe',init=0x44E7E0,select=0x44B910,enum=0x501900,getter=0x4FD7F0,count=0x4FD810,cache=0x6CD1A0,global_world=0x6CD19C,vtable=0x65F31C),
}
WORLD=0x10000000;OTHER=0x10003000;TABLE=0x10010000;TABLE2=0x10030000;OUTPUT=0x10050000;TILE=0x10051000;VTABLE=0x10052000;OBS=0x10080000;QUERY=0x10090000;STACK=0x7FFF0000

def cache_cases():
    return [
     dict(name='empty',capacity=1,loaded=0,ids=[0]),
     dict(name='basic',capacity=4,loaded=3,ids=[0,1,2,3]),
     dict(name='sparse',capacity=8,loaded=2,ids=[0,1,0,0,0,0,7,0]),
     dict(name='slot0-skipped',capacity=4,loaded=2,ids=[9,1,0,3]),
     dict(name='extra-count-zero',capacity=4,loaded=4,ids=[0,1,0,3]),
     dict(name='id-points-elsewhere',capacity=8,loaded=2,ids=[0,7,0,0,0,0,0,5]),
     dict(name='capacity-one',capacity=1,loaded=2,ids=[0,1]),
     dict(name='id255',capacity=256,loaded=1,ids=[0]*255+[255]),
     dict(name='global-other-world',capacity=8,loaded=2,ids=[0,1,0,3,0,0,0,0],other_ids=[0,7,0,0,0,0,0,5]),
    ]

def select_cases():
    variants=[
      ('occupied-disabled',1,0,0,255,0,0,0),
      ('occupied-enabled',1,0,0,255,0x10000000,0,0),
      ('occupied-byteC-blocked',1,0,0,0,0x10000000,0,0),
      ('current-writes-false',0,0,0x40,255,0x01000000,0,0),
      ('current-true',0,0,0x40,255,0x11000000,0,0),
      ('fallback38-true',0,0,0x40,255,0,0x10000000,0),
      ('fallback38-writes-false',0,0,0x40,255,0,0,0),
      ('geometry-clear',0,0x20,0,255,0x10000000,0,0),
      ('geometry-blocked',0,0x20,0,255,0x10000000,0,1),
      ('geometry-negative-result',0,0x20,0,255,0x10000000,0,0xFFFFFFFF),
      ('geometry-no-flag',0,0x20,0,255,0,0,0),
      ('high-word-ignored',0xF000,0x20,0,255,0x10000000,0,0),
      ('byte9-forces-query',0,0x20,0x40,255,0x11000000,0,0),
    ]
    out=[]
    for name,w2,b9,b11,b12,flags,fallback,result in variants:
        tile=bytearray((i*19+3)&255 for i in range(16));struct.pack_into('<H',tile,2,w2);tile[9]=b9;tile[10]=7;tile[11]=b11;tile[12]=b12
        out.append(dict(name=name,tile=tile.hex(),flags=flags,fallback_flags=fallback,query_result=result,x=0x12345678,y=0xFFFFFFF9))
    return out

def world(p,ids,capacity,loaded,table=TABLE):
    b=bytearray((i*13+7)&255 for i in range(0x1700));struct.pack_into('<I',b,0,VTABLE)
    for a,v in [(0x15A4,table),(0x15A8,capacity),(0x15AC,loaded)]:struct.pack_into('<I',b,a,v)
    t=bytearray((i*37+11)&255 for i in range(max(len(ids),8)*460))
    for i in range(len(t)//460):t[460*i+0x1C4]=ids[i] if i<len(ids) else 0
    return b,t

def vtable(p):
    b=bytearray(0x220)
    for a,v in [(0x60,p['enum']),(0x70,p['getter']),(0x74,p['count']),(0x21C,QUERY)]:struct.pack_into('<I',b,a,v)
    return b

def finish(p,entry,mem,checks,observer,stack,steps=180000):
    expected_at=0x11000000
    for at,want in checks:
        mem.append(region(expected_at,want));observer+=compare_code(at,expected_at,len(want))+bytes.fromhex('21c5');expected_at+=((len(want)+15)//16)*16
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    mem.extend([region(OBS,observer),region(VTABLE,vtable(p)),region(STACK,stack)])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=p['program'],address=hex(entry),max_steps=steps,registers=json.dumps(dict(ECX=hex(TILE))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP')

def build_cache(profile,c):
    p=PROFILES[profile];w,t=world(p,c['ids'],c['capacity'],c['loaded']);glob=WORLD;ids=c.get('other_ids',c['ids']);cache=bytearray(1024)
    for ident in cache_ids(ids,c['capacity'],c['loaded']):struct.pack_into('<I',cache,4*ident,TABLE+460*ident)
    mem=[region(WORLD,w),region(TABLE,t),region(p['cache'],bytes([0xA5])*1024),region(p['global_world'],pack32(glob))]
    checks=[(WORLD,w),(TABLE,t),(p['cache'],cache),(VTABLE,vtable(p))]
    if 'other_ids' in c:
        ow,ot=world(p,ids,c['capacity'],c['loaded'],TABLE2);glob=OTHER;mem[-1]=region(p['global_world'],pack32(glob));mem.extend([region(OTHER,ow),region(TABLE2,ot)]);checks.extend([(OTHER,ow),(TABLE2,ot)])
    queries=[0,1,2,c['loaded'],0xFFFFFFFF];answers=bytes().join(pack32(enumerate_id(ids,c['capacity'],q)) for q in queries)
    mem.append(region(OUTPUT,bytes(len(answers))));checks.append((OUTPUT,answers));checks.append((p['global_world'],pack32(glob)))
    observer=b'\xBD'+pack32(1)
    for i,q in enumerate(queries):observer+=b'\xB9'+pack32(glob)+b'\x68'+pack32(q)+b'\xB8'+pack32(p['enum'])+bytes.fromhex('ffd00fb6c0a3')+pack32(OUTPUT+4*i)
    return finish(p,p['init'],mem,checks,observer,pack32(OBS)+pack32(WORLD),steps=700000),dict(cache_slots=sorted(cache_ids(ids,c['capacity'],c['loaded'])),enum_queries=queries,enum_low8=[enumerate_id(ids,c['capacity'],q) for q in queries])

def build_select(profile,c):
    p=PROFILES[profile];ids=[0]*39;ids[7]=7;ids[38]=38;w,t=world(p,ids,39,2)
    struct.pack_into('<I',t,7*460+0x17B,c['flags']);struct.pack_into('<I',t,38*460+0x17B,c['fallback_flags'])
    cache=bytearray(1024);struct.pack_into('<I',cache,7*4,TABLE+7*460);struct.pack_into('<I',cache,38*4,TABLE+38*460)
    tile=bytes.fromhex(c['tile']);ret,selected,called=light_selection(tile,c['flags'],c['fallback_flags'],c['query_result'])
    out=bytearray(32);struct.pack_into('<I',out,0,0xCAFEBABE)
    want=bytearray(out);struct.pack_into('<I',want,4,ret)
    if selected:struct.pack_into('<I',want,0,TABLE+460*(7 if selected=='current' else 38))
    if called:
        for a,v in [(8,1),(12,WORLD),(16,c['x']),(20,c['y']),(24,0)]:struct.pack_into('<I',want,a,v)
    # Explicit adapter: count, ECX, and all three DWORD arguments; RET0C.
    adapter=b'\xFF\x05'+pack32(OUTPUT+8)+b'\x89\x0D'+pack32(OUTPUT+12)
    for n in range(3):adapter+=bytes.fromhex('8b4424')+bytes([4+4*n])+b'\xA3'+pack32(OUTPUT+16+4*n)
    adapter+=b'\xB8'+pack32(c['query_result'])+bytes.fromhex('c20c00')
    mem=[region(WORLD,w),region(TABLE,t),region(TILE,tile),region(OUTPUT,out),region(QUERY,adapter),region(p['cache'],cache),region(p['global_world'],pack32(WORLD))]
    checks=[(OUTPUT,want),(WORLD,w),(TABLE,t),(TILE,tile),(p['cache'],cache),(VTABLE,vtable(p)),(p['global_world'],pack32(WORLD))]
    observer=b'\xA3'+pack32(OUTPUT+4)+b'\xBD'+pack32(1)
    stack=pack32(OBS)+pack32(c['x'])+pack32(c['y'])+pack32(OUTPUT)
    return finish(p,p['select'],mem,checks,observer,stack),dict(return_dword=ret,output_selector=selected,query_called=called)

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for profile in PROFILES:
          for kind,cases,build in [('cache',cache_cases(),build_cache),('select',select_cases(),build_select)]:
            for c in cases:
                args,expected=build(profile,c);path=ROOT/'ghidra'/f'phase74-emulation-terrain-{profile}-{kind}-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==expected
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(profile=profile,kind=kind,case=c,input=args,expected=expected,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(profile,kind,c['name'],r,expected)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),profile=profile,kind=kind,case=c['name']));print(profile,kind,c['name'],'verified',flush=True)
    (ROOT/'phase74-terrain-cache-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
