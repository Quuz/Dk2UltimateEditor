"""Full native Pro/RC3 Door writers with explicitdate/filememoryfixtures.

All616recordbytes/all56headerbytes/model/context/string/sourcearrayscompared.
Native flags/resource/CString/ftol helpers execute;no GUIor gameprocess.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_door_writer import PROFILES,DIRECT,RESOURCES,FLAGS,record
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
DATE=bytes.fromhex('ea070a1b000c2238');STAMP=bytes.fromhex('112233445566778899aa')

def cases():
    rows=[]
    for profile in PROFILES:
        for value in [0,1,0x7FFF,0x8000,0xFFFF]:rows.append(dict(name=f'{profile}-word236-{value}',profile=profile,value=value,seed=165,slots=[1]))
        for seed in [0,255,'pattern']:rows.append(dict(name=f'{profile}-scratch-{seed}',profile=profile,value=0x1234,seed=seed,slots=[1,2]))
        for slots in [[],[0],[1,0,2],[2,1]]:rows.append(dict(name=f'{profile}-slots-'+''.join(map(str,slots)),profile=profile,value=0x4321,seed='pattern',slots=slots))
        for mask in [0,0x20,0x40,0x80]:rows.append(dict(name=f'{profile}-flags-{mask}',profile=profile,value=123,seed='pattern',slots=[1],flags=mask))
        for length in [543,544,548,562]:rows.append(dict(name=f'{profile}-name-{length}',profile=profile,value=123,seed='pattern',slots=[1],label_length=length))
        rows.append(dict(name=f'{profile}-name-carry-to-next-record',profile=profile,value=123,seed='pattern',slots=[1,2],label_length=562))
    return rows

def model(n,c):
    b=bytearray(0x300);struct.pack_into('<I',b,8,0x1000B008+(n-1)*0x2000);struct.pack_into('<I',b,0x154,0x1000C008+(n-1)*0x2000)
    for i,(_,src,width) in enumerate(DIRECT):b[src:src+width]=((n*257+i*31)&((1<<(width*8))-1)).to_bytes(width,'little')
    for i,(_,src) in enumerate(RESOURCES):b[src:src+84]=bytes((j*7+i+n)&255 for j in range(84))
    struct.pack_into('<d',b,0x280,n+0.25);struct.pack_into('<H',b,0x28A,c['value']^(0xFFFF if n==2 else 0))
    for bit,off in FLAGS:struct.pack_into('<I',b,off,0x80000000 if c.get('flags',0xFFFFFFFF)&bit else 0)
    return bytes(b)

def capture(limit):
    b=bytearray(bytes.fromhex('56578b74240c8b4c2410ff05')+pack32(0x10011000)+bytes.fromhex('8b3d')+pack32(0x10011004)+bytes.fromhex('81f968020000'));j1=len(b);b+=bytes.fromhex('7700')
    b+=bytes.fromhex('89f801c881f8')+pack32(limit);j2=len(b);b+=bytes.fromhex('7700')
    b+=bytes.fromhex('a3')+pack32(0x10011004)+bytes.fromhex('f3a45f5eb801000000c20800');fail=len(b);b+=bytes.fromhex('5f5e31c0c20800')
    for j in [j1,j2]:b[j+1]=fail-j-2
    return bytes(b)

def build_case(c):
    f=PROFILES[c['profile']];ctx=bytearray(0x600);struct.pack_into('<II',ctx,f['array'],0x10001000,len(c['slots']));ptrs=b''.join(pack32(0x10002000+(n-1)*0x1000 if n else 0) for n in c['slots']) or bytes(4)
    models={n:model(n,c) for n in [1,2]};labels={1:b'Q'*c.get('label_length',5),2:b'R'*5};sounds={n:bytes([96+n])*7 for n in [1,2]}
    stack=bytes((i*73+19)&255 for i in range(0x400)) if c['seed']=='pattern' else bytes([c['seed']])*0x400
    scratch=stack[0x198:0x400];assert len(scratch)==616
    emitted=[]
    for n in c['slots']:
        if n:scratch=record(models[n],scratch,labels[n],sounds[n]);emitted.append(scratch)
    expected=b''.join(emitted)
    header=struct.pack('<7I',140,4,56+616*len(c['slots']),141,28,len(c['slots']),1)+STAMP+DATE+stack[0x196:0x198]+struct.pack('<II',142,616*len(c['slots']))
    filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x1000A000)
    date=bytes.fromhex('8b442404c700')+DATE[:4]+bytes.fromhex('c74004')+DATE[4:]+bytes.fromhex('c20400')
    readout=bytes.fromhex('0fb6c0a3')+pack32(0x10011008)+b'\xBD'+pack32(1)
    checks=[(0x10012000,0x10014000,56+len(expected)),(0x10000000,0x10015000,len(ctx)),(0x10002000,0x10016000,0x300),(0x10003000,0x10017000,0x300),(0x10001000,0x10018000,len(ptrs)),(0x10011000,0x10019000,12)]
    strings=[]
    for n in [1,2]:
        for delta,value in [(0,labels[n]),(0x1000,sounds[n])]:
            a=0x1000B000+(n-1)*0x2000+delta;content=pack32(len(value))+pack32(0)+value+b'\0';strings.append((a,content))
            checks.append((a,a+0x20000,len(content)))
    for a,b,n in checks:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
    readout+=END
    mem=[region(0x10000000,ctx),region(0x10001000,ptrs),region(0x10002000,models[1]),region(0x10003000,models[2]),region(0x1000A000,capture(0x10013000)),region(0x10010000,pack32(0x10010100)),region(0x10010100,filevt),region(0x10011000,struct.pack('<3I',0,0x10012000,0)),region(0x10012000,bytes(0x1000)),region(0x10014000,header+expected),region(0x10015000,ctx),region(0x10016000,models[1]),region(0x10017000,models[2]),region(0x10018000,ptrs),region(0x10019000,struct.pack('<3I',3+sum(bool(n) for n in c['slots']),0x10012000+56+len(expected),1)),region(0x1001A000,readout),region(f['date_global'],STAMP),region(f['date_helper'],date),region(0x7FFEFC00,stack),region(0x7FFF0000,pack32(0x1001A000)+pack32(0x10010000))]
    for a,b in strings:mem.extend([region(a,b),region(a+0x20000,b)])
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=f['program'],address=hex(f['writer']),max_steps=50000,registers=json.dumps(dict(ECX='0x10000000')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase66-emulation-door-writer-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want)
                rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase66-door-writer-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
