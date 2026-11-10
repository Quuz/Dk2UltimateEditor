"""Full native AP condition dispatcher+counters+filter+comparison+mark lookup.

Only world tile virtual1D8 is a coordinate→fixture-address helper. Runtime
tiles/player/mark/entity arrays are explicit; no renderer, gameplay or file I/O.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_ap_conditions import evaluate
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
CELLS=[(1,7),(1,9),(2,7),(3,7),(4,9),(1,7)]
TFLAGS={1:12,2:4,3:8,4:0}
ENTITIES=[dict(tag=1,cell=0,kind=0,ident=12,player=1,possessed=True,next=5),dict(tag=2,cell=1,kind=0,ident=24,player=2,possessed=False,next=0),dict(tag=3,cell=2,kind=2,ident=12,player=1,possessed=True,next=0),dict(tag=4,cell=3,kind=2,ident=24,player=2,possessed=False,next=0),dict(tag=5,cell=0,kind=0,ident=12,player=1,possessed=False,next=0)]
MARKS={1:[1,1,2,2,0,0],2:[0,3,0,0,1,2]}
def cases():
    out=[]
    for kind in [0x27,0x28,0x29,0x2A,0x3E,0x45,0x46]:
        for flags in [0,4,8,12]:out.append(dict(name=f'core-{kind:x}-{flags}',kind=kind,flags=flags,op=3,player=1,target=0,target_type=0,value=3))
    for op in [0,1,2,3,4,5,6,7,255]:
        for value in [3,0xFFFFFFFF]:out.append(dict(name=f'compare-{op}-{value}',kind=0x27,flags=0,op=op,player=0,target=0,target_type=0,value=value))
    for kind,player,target,target_type in [(0x27,1,12,0),(0x27,1,12,6),(0x27,2,24,255),(0x28,1,3,0),(0x28,0,1,0),(0x29,1,3,0),(0x3E,2,255,255),(0x45,2,255,255),(0x46,1,12,6),(0x46,2,24,0)]:out.append(dict(name=f'filter-{kind:x}-{player}-{target}-{target_type}',kind=kind,flags=255,op=1,player=player,target=target,target_type=target_type,value=2))
    out.append(dict(name='claim-all-65536',kind=0x29,flags=12,op=0,player=0,target=0,target_type=0,value=0,big=True))
    out.append(dict(name='claim-all-ignore-threshold',kind=0x29,flags=0,op=255,player=0,target=0,target_type=255,value=0xFFFFFFFF))
    out.append(dict(name='tag-all-ignore-threshold',kind=0x45,flags=0,op=255,player=1,target=0,target_type=255,value=0xFFFFFFFF))
    for no_taggable in [False,True]:out.append(dict(name=f'tag-all-empty-marks-{int(no_taggable)}',kind=0x45,flags=12,op=0,player=1,target=0,target_type=0,value=0,no_marks=True,no_taggable=no_taggable))
    return out
def expected(c):
    p,t=c['player'],c['target'];etype=2 if c['target_type']==6 else 0
    matches=[e for e in ENTITIES if e['kind']==etype and (not p or e['player']==p) and (not t or e['ident']==t)]
    owner={0:0,1:7,2:9}[p]
    if not p and not t:owned=65536 if c.get('big') else 6
    else:owned=sum((not t or terrain==t) and (not p or tileowner==owner) and (bool(t) or bool(TFLAGS[terrain]&4)) for terrain,tileowner in CELLS)
    kw=dict(entity_count=len(matches),owned_count=owned,marked_count=0 if c.get('no_marks') else sum(v in [1,2] for v in MARKS.get(p,[])),taggable_count=0 if c.get('no_taggable') else sum(bool(TFLAGS[t]&8) for t,_ in CELLS),area=65536 if c.get('big') else 6,possessed=any(e['possessed'] for e in matches))
    params=bytes([c['op'],p,t,c['target_type']])+pack32(c['value']);return int(evaluate(c['kind'],params,**kw)),kw
def build_case(c):
    result,counts=expected(c);world=bytearray(0x100);struct.pack_into('<I',world,0,0x10001000);struct.pack_into('<I',world,0x84,0x10004000);vt=bytearray(0x1DC);struct.pack_into('<I',vt,0x1D8,0x10070000)
    ap=bytearray((i*13+7)&255 for i in range(64));ap[0x32]=c['flags'];struct.pack_into('<4H',ap,0x38,0,0,255 if c.get('big') else 2,255 if c.get('big') else 1)
    cond=bytearray(20);cond[:8]=bytes([c['op'],c['player'],c['target'],c['target_type']])+pack32(c['value']);cond[14]=c['kind']
    players=bytearray(4389*2)
    for i,owner in enumerate([7,9]):struct.pack_into('<H',players,i*4389+4,owner);players[i*4389+0x10FC]=i+1;struct.pack_into('<I',players,i*4389+0x1105,0x10010000+i*0x10000)
    tiles=bytearray(96)
    for i,(terrain,owner) in enumerate(CELLS):struct.pack_into('<H',tiles,i*16+8,owner|0xA000);tiles[i*16+10]=terrain
    for e in ENTITIES:
        if e['tag']!=5:struct.pack_into('<H',tiles,e['cell']*16,e['tag'])
    markers=[]
    for p in [1,2]:
        b=bytearray(0x4700);chains={}
        for i,typ in enumerate([0]*6 if c.get('no_marks') else MARKS[p]):
            if not typ:continue
            index=i+1;struct.pack_into('<HH',b,index*36,i%3,i//3)
            if typ in chains:struct.pack_into('<H',b,chains[typ]*36+0x1F,index)
            else:struct.pack_into('<H',b,0x4656+2*typ,index)
            chains[typ]=index;struct.pack_into('<H',b,0x4678+2*typ,1)
        markers.append(bytes(b))
    mem=[region(0x10000000,world),region(0x10001000,vt),region(0x10002000,ap),region(0x10003000,cond),region(0x10004000,players),region(0x10010000,markers[0]),region(0x10020000,markers[1]),region(0x10030000,tiles)]
    terrains=[0]+[0x10031000+i*0x200 for i in range(4)]
    for i in range(4):b=bytearray(460);b[0x17B]=TFLAGS[i+1]&(~8 if c.get('no_taggable') else 255);mem.append(region(terrains[i+1],b))
    pointers=[0]*6
    for e in ENTITIES:
        a=0x10040000+e['tag']*0x1000;pointers[e['tag']]=a;b=bytearray(0x500);struct.pack_into('<H',b,0xC,e['next']);struct.pack_into('<I',b,0x12,e['kind']);struct.pack_into('<I',b,0x2A,0x10004000+(e['player']-1)*4389);b[0x36F if e['kind']==0 else 0x12C]=e['ident'];b[0x3E6 if e['kind']==0 else 0x146]=0x80 if e['kind']==0 and e['possessed'] else 8 if e['possessed'] else 0;mem.append(region(a,b))
    # Coordinate fixture: only ordered3x2 inputs,or the proven fast-area path.
    tile=bytes.fromhex('8b4424086bc00303442404c1e00405')+pack32(0x10030000)+bytes.fromhex('c20800')
    # Save original return separately; comparisons themselves set EAX=1.
    readout=b'\xA3'+pack32(0x10060000)+b'\xBD'+pack32(1)
    targets=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in mem if int(r['address'],16) not in [0x10001000]]
    for j,(a,b) in enumerate(targets):
        shadow=0x10100000+j*0x10000;mem.append(region(shadow,b));readout+=compare_code(a,shadow,len(b))+bytes.fromhex('21c5')
    readout+=compare_code(0x10060000,0x10061000,4)+bytes.fromhex('21c5')+END
    mem.extend([region(0x10060000,bytes(4)),region(0x10061000,pack32(result)),region(0x10062000,readout),region(0x10070000,tile),region(0x6E5050,pack32(0x10000000)),region(0x6DA490,pack32(3)),region(0x6DA4A8,struct.pack('<5I',*terrains)),region(0x6EE540,struct.pack('<6I',*pointers)),region(0x7FFF0000,pack32(0x10062000)+pack32(0x10003000))])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address='0x4b0d70',max_steps=150000,registers=json.dumps(dict(ECX='0x10002000')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase56-emulation-ap-condition-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase56-ap-condition-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
