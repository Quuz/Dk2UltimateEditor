"""Full4611D0 good-player/AP branch and real517490 time getter.

Navigation,state transitions,movement,post-success update and sound are explicit
memory-only providers whose arguments/results are compared. Other owner branch,
real pathfinding,state mutation,rendering and GUI are not executed.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_ap_retreat import project,d,setd,sw
ROOT=Path(__file__).resolve().parent
B=0x10000000;CAP=B+0x10000
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    rows=[dict(name=f'flags-{f}',flags=[f]) for f in [0,4,8,12,1,5,9,13,0x3D]]
    rows.extend([dict(name='empty-list',flags=[]),dict(name='creature-gate-off',flags=[1],gate=False),dict(name='first-reachable',flags=[1,1],nav=[0,1]),dict(name='first-reachable-order',flags=[1,1],nav=[1,1]),dict(name='unreachable-all',flags=[1,1],nav=[0,0]),dict(name='skip-unflagged',flags=[12,1],nav=[1]),dict(name='state-change-rejected',flags=[1],state=0,state_ok=0),dict(name='state-change-accepted',flags=[1],state=0,state_ok=1),dict(name='second-move-success',flags=[1],moves=[0,1]),dict(name='all-moves-fail',flags=[1],moves=[0,0]),dict(name='creature-id1',flags=[1],creature_id=1),dict(name='end-bounds-ignored',flags=[1],end_bounds=(65535,65535)),dict(name='high-start-coordinates',flags=[1],start=(65535,32768)),dict(name='negative-entity-position',flags=[1],entity_x=0xFFFFE123,entity_y=0x12345678)])
    return rows

def build_case(c):
    controller=bytearray([165])*16;setd(controller,8,B+0x1000);setd(controller,12,B+0x3000)
    entity=bytearray([165])*0x600;sw(entity,0x28,4);entity[0x46F]=16 if c.get('gate',True) else 0;entity[0x36F]=c.get('creature_id',12)
    setd(entity,0x370,B+0x4000);setd(entity,0x1A,c.get('entity_x',0x5123));setd(entity,0x1E,c.get('entity_y',0xA456));setd(entity,0x22,0x7654);setd(entity,0x2E,0xABCDEF)
    definition=bytearray([165])*0x900;setd(definition,0x6E3,0x1234)
    nodes={};aps=bytearray();table=bytearray(16)
    for i,flags in enumerate(c['flags'],1):
        ap=bytearray([165])*64;sw(ap,8,i+1 if i<len(c['flags']) else 0);ap[0x32]=flags
        x,y=c.get('start',(3+i,7+i));ex,ey=c.get('end_bounds',((x+5)&65535,(y+6)&65535));struct.pack_into('<4H',ap,0x38,x,y,ex,ey)
        nodes[i]=bytes(ap);setd(table,i*4,B+0x6000+(i-1)*64);aps+=ap
    world=bytearray([165])*32;setd(world,4,B+0x3800);sw(world,0x12,1 if nodes else 0)
    timer=bytearray([165])*16;setd(timer,4,123456)
    want=project(entity,nodes,1 if nodes else 0,nav=c.get('nav',(1,)),state=c.get('state',22),state_ok=c.get('state_ok',1),moves=c.get('moves',(1,)))
    cap=bytearray(512);calls=want['calls']
    for o,value in [(0,len(calls['nav'])),(4,calls['state_query']),(8,len(calls['state_change'])),(12,len(calls['moves'])),(16,len(calls['post'])),(20,len(calls['audio'])),(24,want['result'])]:setd(cap,o,value)
    for i,p in enumerate(calls['nav']):struct.pack_into('<6I',cap,64+i*24,*p,B+0x1000,0,B+0x1124)
    for i,(p,state,action) in enumerate(calls['moves']):struct.pack_into('<6I',cap,192+i*24,*p,state,action,B)
    if calls['state_query']:setd(cap,328,B+0x124E)
    if calls['state_change']:struct.pack_into('<2I',cap,320,*calls['state_change'][0])
    if calls['post']:struct.pack_into('<3I',cap,336,1,0,B+0x1000)
    if calls['audio']:struct.pack_into('<5I',cap,352,*calls['audio'][0],B+0x101A,B+0x8000)
    def inc(off):return bytes.fromhex('ff05')+pack32(CAP+off)
    def record(counter,start,pointarg):
        # Preserve native callee-saved ESI/EDI; shifted arguments after pushes.
        code=bytes.fromhex('56578b3d')+pack32(CAP+counter)+bytes.fromhex('6bff1881c7')+pack32(CAP+start)+bytes.fromhex('894f148b7424')+bytes([pointarg+8])
        for off in [0,4,8]:code+=bytes.fromhex('8b46')+bytes([off])+bytes.fromhex('8947')+bytes([off])
        for src,dst in ([(12,12),(20,16)] if pointarg==8 else [(16,12),(20,16)]):code+=bytes.fromhex('8b4424')+bytes([src])+bytes.fromhex('8947')+bytes([dst])
        code+=inc(counter)+b'\xA1'+pack32(CAP+counter)+bytes.fromhex('488b0485')+pack32(B+(0xA000 if counter==0 else 0xA100))+bytes.fromhex('5f5ec20c00')
        return code
    def pair(counter,where,result,capture_ecx=None):
        code=inc(counter)
        for src,dst in [(4,where),(8,where+4)]:code+=bytes.fromhex('8b4424')+bytes([src])+b'\xA3'+pack32(CAP+dst)
        if capture_ecx is not None:code+=bytes.fromhex('890d')+pack32(CAP+capture_ecx)
        return code+b'\xB8'+pack32(result)+bytes.fromhex('c20800')
    query=inc(4)+bytes.fromhex('890d')+pack32(CAP+328)+b'\xB8'+pack32(c.get('state',22))+b'\xC3'
    audio=inc(20)+bytes.fromhex('890d')+pack32(CAP+368)
    for i in range(4):audio+=bytes.fromhex('8b4424')+bytes([4+4*i])+b'\xA3'+pack32(CAP+352+4*i)
    audio+=b'\xB8'+pack32(0x777)+bytes.fromhex('c21000')
    def results(values):return b''.join(pack32(v) for v in list(values)+[0]*(4-len(values)))
    readout=b'\xA3'+pack32(CAP+24)+compare_code(B+0x1000,B+0x2000,len(entity))+bytes.fromhex('89c589ca')
    for actual,expected,n in [(B,B+0xC000,16),(B+0x3000,B+0xC100,32),(B+0x3800,B+0xC200,16),(B+0x4000,B+0xD000,len(definition)),(CAP,B+0x11000,len(cap)),(B+0x6000,B+0xC300,len(aps))]:
        if n:readout+=compare_code(actual,expected,n)+bytes.fromhex('21c5')
    readout+=bytes.fromhex('89e8')+END
    vt=bytearray(0x24);setd(vt,0x20,B+0x8200)
    mem=[region(B,controller),region(B+0x1000,entity),region(B+0x2000,want['entity']),region(B+0x3000,world),region(B+0x3800,timer),region(B+0x4000,definition),region(B+0x6000,aps),region(B+0x8000,pack32(B+0x8100)),region(B+0x8100,vt),region(B+0x8200,audio),region(B+0x9000,readout),region(B+0xA000,results(c.get('nav',(1,)))),region(B+0xA100,results(c.get('moves',(1,)))),region(B+0xC000,controller),region(B+0xC100,world),region(B+0xC200,timer),region(B+0xC300,aps),region(B+0xD000,definition),region(CAP,bytes(512)),region(B+0x11000,cap),region(0x6EE540,table),region(0x6F254C,struct.pack('<H',4)),region(0x75B468,pack32(B+0x8000)),region(0x4D0E60,record(0,64,8)),region(0x478050,query),region(0x476D30,pair(8,320,c.get('state_ok',1))),region(0x471890,record(12,192,4)),region(0x4887D0,pair(16,336,0,344)),region(0x7FFF0000,pack32(B+0x9000))]
    mem=[r for r in mem if r['hex']]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address='0x4611D0',max_steps=20000,registers=json.dumps(dict(ECX=hex(B))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,EDX,ESP,EIP'),dict(EAX=1,EBP=1,EDX=0)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as s:
            await s.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase64-emulation-ap-retreat-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await s.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want)
                rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase64-ap-retreat-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
