"""Execute original world AP reveal fragment with real per-tile helper.

World fragment50C05D..50C170; full454B80 and517480 execute. Session event
append, room reveal and renderer update are explicit fixtures. No live game.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_hero_parties import compare_code
from native_action_point_visibility import reveal_projection,toolbox_id
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    rows=[]
    def add(name,points,**kwargs):
        c=dict(name=name,kind='reveal',points=points,world_flags=0,initial_revealed=[],room_type=None,room_tiles=[]);c.update(kwargs);rows.append(c)
    def p(bounds,flags=16,id=7):return dict(bounds=bounds,flags=flags,id=id)
    for b in [(1,1,1,1),(1,1,3,2),(1,1,1,3),(1,1,3,1),(3,2,1,1),(3,1,1,2),(1,3,2,1)]:add('bounds-'+''.join(map(str,b)),[p(b)])
    for flags in [0,1,4,8,16,20,24,32,64,255]:add(f'flags-{flags}',[p((1,1,3,2),flags)])
    add('empty',[]);add('world-gate',[p((1,1,3,2))],world_flags=64)
    add('already-revealed',[p((1,1,3,2))],initial_revealed=[(1,1),(3,2)])
    add('overlap',[p((1,1,3,2)),p((2,2,4,3),16,8)])
    add('same-area-twice',[p((1,1,3,2)),p((1,1,3,2),16,8)])
    for t in [1,2,3,4,5,8,9]:add(f'room-type-{t}',[p((1,1,3,2))],room_type=t,room_tiles=[(1,1),(2,1),(3,2)])
    add('special-room-already-revealed',[p((1,1,3,2))],room_type=3,room_tiles=[(1,1),(2,1)],initial_revealed=[(1,1)])
    for ps in [[],[p((0,0,0,0),0)],[p((0,0,0,0),32,7)],[p((0,0,0,0),32,7),p((0,0,0,0),32,8)],[p((0,0,0,0),32,8),p((0,0,0,0),32,0)],[p((0,0,0,0),32,255),p((0,0,0,0),16,8)]]:rows.append(dict(name='toolbox-'+str(len(rows)),kind='toolbox',points=ps))
    return rows

def build_case(c):
    W,H=6,5;world=bytearray(0x7300);ptrs=bytearray(256*4);nodes=[];mem=[]
    # Linked-list tags and AP IDs deliberately differ.
    for i,p in enumerate(c['points']):
        tag=20+i;address=0x10008000+i*0x100;struct.pack_into('<I',ptrs,tag*4,address);node=bytearray(64);struct.pack_into('<H',node,8,tag+1 if i+1<len(c['points']) else 0);node[0x32]=p['flags'];node[0x37]=p['id'];struct.pack_into('<4H',node,0x38,*p['bounds']);nodes.append(region(address,node))
    struct.pack_into('<H',world,0x12,20 if nodes else 0);mem=[region(0x10000000,world),region(0x6EE540,ptrs),*nodes]
    if c['kind']=='toolbox':
        entry=0x510E20;mem.append(region(0x7FFF0000,pack32(0x10031000)));mem.append(region(0x10031000,bytes.fromhex('0fb6c0')+END));regs=dict(ECX='0x10000000');want=dict(EAX=toolbox_id(c['points']))
    else:
        entry=0x50B5B0;world[0x7239]=c['world_flags'];struct.pack_into('<2I',world,0x9E6,W,H);struct.pack_into('<I',world,0x9EE,0x10010000);struct.pack_into('<I',world,4,0x10030000);mem[0]=region(0x10000000,world)
        tiles=bytearray((i*31+7)&255 for i in range(W*H*16))
        for i in range(W*H):struct.pack_into('<H',tiles,i*16+2,0);struct.pack_into('<H',tiles,i*16+8,0x8155)
        for x,y in c['initial_revealed']:struct.pack_into('<H',tiles,(y*W+x)*16+8,0xA155)
        for x,y in c['room_tiles']:struct.pack_into('<H',tiles,(y*W+x)*16+2,0x9005)
        room_types={5:c['room_type']} if c['room_type'] is not None else {}
        out,events,refresh,rooms=reveal_projection(W,H,tiles,c['points'],c['world_flags'],room_types)
        result=bytearray(0x4000);result[:len(tiles)]=tiles;expected=bytearray(result);expected[:len(out)]=out
        for i,e in enumerate(events):struct.pack_into('<4I',expected,0x1000+i*16,*e)
        for i,(x,y) in enumerate(refresh):struct.pack_into('<2I',expected,0x2000+i*8,x,y)
        struct.pack_into('<I',expected,0x3000,len(rooms))
        # Event callback observes actual517480 forwarding to session527150.
        event=bytes.fromhex('56578b74240c8b3d')+pack32(0x10015000)+bytes.fromhex('b910000000f3a4893d')+pack32(0x10015000)+bytes.fromhex('ff05')+pack32(0x10015008)+bytes.fromhex('5f5ec20400')
        update=bytes.fromhex('8b15')+pack32(0x10015004)+bytes.fromhex('8b44240489028b44240889420483c2088915')+pack32(0x10015004)+bytes.fromhex('ff05')+pack32(0x1001500C)+bytes.fromhex('c20800')
        room=b'\xFF\x05'+pack32(0x10013000)+b'\xC3'
        readout=compare_code(0x10010000,0x10020000,0x4000)+load_abs(3,0x10015008)+load_abs(5,0x1001500C)+load_abs(2,0x10013000)+END
        mem.extend([region(0x10030000,bytes(16)),region(0x10031000,readout),region(0x10010000,result),region(0x10015000,struct.pack('<4I',0x10011000,0x10012000,0,0)),region(0x10020000,expected),region(entry,jump(entry,0x50C05D)),region(0x50C171,jump(0x50C171,0x10031000)),region(0x527150,event),region(0x451C60,update),region(0x4E8B80,room),region(0x6DA490,pack32(W)),region(0x6DA49C,pack32(0x10010000)),region(0x6DA4A4,pack32(0x10000000)),region(0x7FFF0000,bytes(0x100))])
        if c['room_type'] is not None:
            struct.pack_into('<I',ptrs,5*4,0x10017000);mem[1]=region(0x6EE540,ptrs);r=bytearray(0x20);struct.pack_into('<I',r,0x19,0x10018000);definition=bytearray(0x460);definition[0x452]=c['room_type'];mem.extend([region(0x10017000,r),region(0x10018000,definition)])
        regs=dict(ESI='0x10000000');want=dict(EAX=1,ECX=0,EBX=len(events),EBP=len(refresh),EDX=len(rooms))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase45-emulation-ap-visibility-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase45-ap-visibility-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
