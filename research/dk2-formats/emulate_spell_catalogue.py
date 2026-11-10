"""Full parser→initializer→converter→worldcallback→loader, memory stream fixture.

    File read fixture rejects copies exceeding266 bytes or stream end before REP.
    World arrays preallocated and seededA5; resource provider null. No real I/O.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from emulate_spell_alternatives import raw_record
from native_spell_catalogue import project,lookup_index
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
LOOKUP_IDS=[0,1,2,12,24,45,255]

def records(c):
    out=[]
    for j,ident in enumerate(c['ids']):
        r=bytearray(raw_record(c['shot'],c['room'],tail=c['tail']));r[0xE6]=ident;r[0xE7]=20;struct.pack_into('<I',r,0xC8,0x11223344+j);out.append(bytes(r))
    return out

def cases():
    out=[]
    for gate in [0,1]:
        for update in [False,True]:
            for shot,room in [(0,0),(36,4),(255,255)]:
                out.append(dict(name=f'pipeline-{gate}-{int(update)}-{shot}-{room}',gate=gate,update=update,shot=shot,room=room,tail=165,ids=[45,12,24],header=False,capacity=3))
    for ids in [[12,12,24],[0,1,2]]:out.append(dict(name='ids-'+'-'.join(map(str,ids)),gate=0,update=False,shot=36,room=4,tail=255,ids=ids,header=True,capacity=3))
    out.append(dict(name='capacity-drop',gate=0,update=False,shot=36,room=4,tail=0,ids=[45,12,24],header=False,capacity=2))
    out.append(dict(name='opaque-tail0',gate=0,update=False,shot=36,room=4,tail=0,ids=[45,12,24],header=True,capacity=3))
    for shot,room in [(36,4),(255,255)]:out.append(dict(name=f'scratch-reuse-{shot}-{room}',gate=0,update=False,shot=shot,room=room,tail=165,ids=[45,12,24],header=True,capacity=3,reuse=True))
    return out

def read_fixture(end):
    # cdecl(status*,handle,destination,length,bytesRead*) -> status*. No OS calls.
    b=bytearray(bytes.fromhex('5356578b7c24188b4c241c8b35')+pack32(0x10005000)+bytes.fromhex('81f90a010000'))
    j1=len(b);b+=bytes.fromhex('7700');b+=bytes.fromhex('89f201ca81fa')+pack32(end);j2=len(b);b+=bytes.fromhex('7700')
    b+=bytes.fromhex('89cbf3a48935')+pack32(0x10005000)+bytes.fromhex('ff05')+pack32(0x10005004)+bytes.fromhex('8b44242089188b442410c700000000005f5e5bc3')
    fail=len(b);b+=bytes.fromhex('8b442410c700ffffffff5f5e5bc3');b[j1+1]=fail-(j1+2);b[j2+1]=fail-(j2+2);return bytes(b)

def build_case(c):
    rr=records(c);n=len(rr);stream=bytearray(44);struct.pack_into('<II',stream,0,0xA1,28);struct.pack_into('<I',stream,8,n);struct.pack_into('<II',stream,36,0xA2,266*n);stream+=b''.join(rr)
    world=bytearray(0xA3CC);struct.pack_into('<I',world,0,0x10001000);struct.pack_into('<III',world,0x1614,0x10002000,0 if c['header'] else c['capacity'],3 if c['update'] else 0);struct.pack_into('<I',world,0x1620,0x1122);struct.pack_into('<I',world,0xA3C7,int(c['update']))
    initial=bytearray([165])*210
    if c['update']:
        for i,ident in enumerate([12,24,45]):initial[i*70+0x43]=ident
    previous=[bytes(initial[i*70:(i+1)*70]) for i in range(3)] if c['update'] else []
    nodes,delta=project(rr,previous,c['capacity'],gate=c['gate'],update=c['update']);expected=bytearray(initial);expected[:70*len(nodes)]=b''.join(nodes);ew=bytearray(world);struct.pack_into('<II',ew,0x1618,c['capacity'],len(nodes));struct.pack_into('<I',ew,0x1620,(0x1122+delta)&0xFFFFFFFF)
    vt=bytearray(0xEC);struct.pack_into('<I',vt,0xE8,0x50DFA0);callbacks=bytearray(0x60);struct.pack_into('<III',callbacks,0x50,0x51FDE0 if c['header'] else 0,0x51FE00,0x10010000)
    # Readout preserves parser return then compares arrays,world,inputstream,global intermediate.
    from native_spell_alternatives import full_intermediate
    readout=b'\xA3'+pack32(0x10005008)+b'\xBD'+pack32(1)
    for a,b,size in [(0x10002000,0x10003000,210),(0x10010000,0x10020000,len(world)),(0x10030000,0x10031000,len(stream)),(0x746FCA,0x10034000,346),(0x10005000,0x10035000,12)]:readout+=compare_code(a,b,size)+bytes.fromhex('21c5')
    # Also execute original lookup after native parser returns (input records remain in same order).
    for k,ident in enumerate(LOOKUP_IDS):
        readout+=b'\xB9'+pack32(0x10010000)+b'\x68'+pack32(ident)+b'\xB8'+pack32(0x50DFA0)+bytes.fromhex('ffd0a3')+pack32(0x10036000+4*k)
    readout+=compare_code(0x10036000,0x10037000,4*len(LOOKUP_IDS))+bytes.fromhex('21c5')
    if c.get('reuse'):
        readout+=b'\xB9'+pack32(0x747103)+b'\xB8'+pack32(0x570B60)+bytes.fromhex('ffd0')
        for a,b,size in [(0x747103,0x10038000,747),(0x746FCA,0x10039000,346),(0x10002000,0x10003000,210),(0x10010000,0x10020000,len(world))]:readout+=compare_code(a,b,size)+bytes.fromhex('21c5')
    readout+=END
    lookups=[]
    for ident in LOOKUP_IDS:
        i=lookup_index([b[0x43] for b in nodes],ident);lookups.append(0 if i is None else 0x10002000+70*i)
    mem=[region(0x10000000,pack32(123)),region(0x10001000,vt),region(0x10002000,initial),region(0x10003000,expected),region(0x10004000,callbacks),region(0x10005000,pack32(0x10030000)+bytes(8)),region(0x10006000,readout),region(0x10010000,world),region(0x10020000,ew),region(0x10030000,stream),region(0x10031000,stream),region(0x10034000,full_intermediate(rr[-1],0)),region(0x10035000,struct.pack('<3I',0x10030000+len(stream),2+n,1)),region(0x10036000,bytes(4*len(LOOKUP_IDS))),region(0x10037000,struct.pack('<'+'I'*len(lookups),*lookups)),region(0x5B3EB0,read_fixture(0x10030000+len(stream))),region(0x6F2590,pack32(20)),region(0x6FCDCF,pack32(c['gate'])),region(0x7FFF0000,pack32(0x10006000)+pack32(0x10000000)+pack32(0x10004000))]
    if c.get('reuse'):
        mid=full_intermediate(rr[-1],0);mem.extend([region(0x10038000,bytes(747)),region(0x10039000,mid[:313]+bytes(33))])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address='0x56a6d0',max_steps=50000,registers='{}',memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase55-emulation-spell-catalogue-v3-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase55-spell-catalogue-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
