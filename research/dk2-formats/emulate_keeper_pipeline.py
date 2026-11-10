"""Native Keeper full writers/converters/loaders; bounded x87 model fragment.

File writes, date, virtual scalar getters and sound handles are explicit fixtures.
Real resources, flags, string copies and ftol execute. No GUI/gameplay runs.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_keeper_pipeline import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
BOUNDARY=[0,1,0x7FFFFFFF,0x80000000,0xFE0C0000,0xFFFFFFFF]

def cases():
    rows=[]
    for seed in [0,165,255,'pattern']:rows.append(dict(name=f'writer-seed-{seed}',kind='writer',seed=seed,c8=0xFE0C0000,slots=[1,2],flags=0xFFFFFFFF,label=32))
    for c8 in BOUNDARY:rows.append(dict(name=f'writer-c8-{c8}',kind='writer',seed=165,c8=c8,slots=[1],flags=0x80000000,label=5))
    for slots in [[],[0],[1,0,2],[0,2],[2,1]]:rows.append(dict(name='writer-slots-'+''.join(map(str,slots)),kind='writer',seed='pattern',c8=1,slots=slots,flags=6,label=0))
    for mask in [0,2,4,8,16]:rows.append(dict(name=f'writer-flags-{mask}',kind='writer',seed=255,c8=1,slots=[1],flags=mask,label=31))
    for length in [225,226]:rows.append(dict(name=f'writer-long-name-{length}',kind='writer',seed=165,c8=1,slots=[1],flags=30,label=length))
    for program in ['game','editor']:
        for seed in [0,165,255]:rows.append(dict(name=f'init-{program}-{seed}',kind='init',program=program,seed=seed))
        for seed in [0,165]:
            for gap in [0,165,255]:rows.append(dict(name=f'converter-{program}-{seed}-{gap}',kind='converter',program=program,seed=seed,gap=gap))
    for c8 in BOUNDARY:
        for presence in [0,1]:rows.append(dict(name=f'model-{c8}-{presence}',kind='model',c8=c8,presence=presence))
    for gate in [0,1]:
        for c8 in BOUNDARY:rows.append(dict(name=f'loader-{gate}-{c8}',kind='loader',gate=gate,c8=c8,presence=1,tps=20,seed=165))
    for p in [0,1]:
        for tps in [0,0xFFFFFFFF]:rows.append(dict(name=f'loader-presence-tps-{p}-{tps}',kind='loader',gate=0,c8=0xFE0C0000,presence=p,tps=tps,seed=255))
    return rows

def raw_record(gap=0,c8=0xFE0C0000):
    raw=bytearray((i*31+7)&255 for i in range(406));raw[:32]=b'NativeKeeper\0'+bytes(19)
    raw[0xF1:0x111]=b'NativeSound\0'+bytes(20);raw[0x174:0x194]=b'NativeCategory\0'+bytes(17)
    struct.pack_into('<I',raw,0xC8,c8);raw[0xE0:0xE2]=bytes([gap])*2
    assert len(raw)==406
    return bytes(raw)

def fixture_model(n,c):
    node=bytearray(0x220);struct.pack_into('<I',node,0,0x10008000)
    for o,bias in [(0x14,n),(0x70,n+55),(0x190,n+111)]:node[o:o+84]=bytes((i+bias)&255 for i in range(84))
    for o,v in [(0x124,0x1234+n),(0x126,0x2345+n),(0x128,0x3456+n),(0x12A,0x4567+n),(0x12C,0x5678+n),(0x170,0x6789+n),(0x158,0x789A+n),(0x1E8,0x89AB+n)]:struct.pack_into('<H',node,o,v)
    for o,v in [(0x168,n),(0x188,4+n),(0x189,6+n),(0x15A,8+n),(0x1F8,10+n)]:node[o]=v
    for o,v in [(0x16C,0x11223344+n),(0x1FC,0x12345678+n),(0x200,0x89ABCDEF+n),(0x1F0,12+n),(0x1F4,14+n)]:struct.pack_into('<I',node,o,v)
    struct.pack_into('<d',node,0x178,c['c8']/4096);struct.pack_into('<d',node,0x180,0x87654321/4096)
    for b,o in FLAGS:struct.pack_into('<I',node,o,0x80000000 if c['flags']&b else 0)
    strings=[bytes([64+n])*c['label'],b'Sound'+bytes([64+n])*27,b'Group'+bytes([64+n])*27]
    for o,i in [(8,0),(0x154,1),(0x1EC,2)]:struct.pack_into('<I',node,o,0x10020008+(n-1)*0x1000+i*0x200)
    return node,strings

def expected_record(node,strings,c,gap):
    raw=bytearray(406)
    for d,s in [(0x20,0x14),(0x74,0x70),(0x120,0x190)]:raw[d:d+84]=node[s:s+84]
    for d,s,n in [(0xEE,0x168,1),(0xE4,0x124,10),(0x11C,0x16C,4),(0xE2,0x170,2),
                  (0xEF,0x188,1),(0xDA,0x189,1),(0xD8,0x158,2),(0x111,0x1E8,2),
                  (0x113,0x1F8,1),(0x114,0x1FC,4),(0x118,0x200,4),(0xDB,0x15A,1),
                  (0x194,0x1F0,1),(0x195,0x1F4,1)]:raw[d:d+n]=node[s:s+n]
    struct.pack_into('<4I',raw,0xC8,c['c8'],0x87654321,0x12345678,0x89ABCDEF)
    struct.pack_into('<I',raw,0xDC,c['flags']&30);raw[0xF0]=21;raw[0xE0:0xE2]=gap
    for o,s in zip([0,0xF1,0x174],strings):raw[o:o+max(32,len(s))]=s.ljust(32,b'\0')
    assert len(raw)==406
    return bytes(raw)

def build_case(c):
    mem=[];regs={};program='DK2 Editor pro.exe';kind=c['kind']
    if kind in ['init','converter']:
        game=c['program']=='game';program='DKII-DX.exe' if game else program;init=0x56B990 if game else 0x42B600;conv=0x56BAB0 if game else 0x42B720
        entry=init if kind=='init' else conv
        wantbuf=bytes(526) if kind=='init' else intermediate(raw_record(c['gap']),c['seed'])
        mem=[region(0x10001000,bytes([c['seed']])*526),region(0x10002000,wantbuf),region(0x10003000,compare_code(0x10001000,0x10002000,526)+END),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))]
        if kind=='converter':mem.append(region(0x10000000,raw_record(c['gap'])))
        regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    elif kind=='loader':
        program='DKII-DX.exe';entry=0x4F7CE0;mid=bytearray(intermediate(raw_record(c8=c['c8'])));struct.pack_into('<I',mid,0x18,c['presence']);previous=bytes([c['seed']])*122;expected=runtime_projection(mid,previous,c['tps'],c['gate'])
        vt=bytearray(32);struct.pack_into('<I',vt,0x1C,0x10006000)
        mem=[region(0x10000000,mid),region(0x10001000,previous),region(0x10002000,expected),region(0x10003000,compare_code(0x10001000,0x10002000,122)+END),region(0x10004000,pack32(0x10005000)),region(0x10005000,vt),region(0x10006000,return_const(0x11223344,8)),region(0x75B468,pack32(0x10004000)),region(0x6F2590,pack32(c['tps'])),region(0x6FCDCF,pack32(c['gate'])),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000)+pack32(0))]
        regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    elif kind=='model':
        entry=0x4A25E0;mid=bytearray(526);struct.pack_into('<I',mid,0x18,c['presence']);struct.pack_into('<I',mid,0x1C,c['presence']);struct.pack_into('<2I',mid,0x140,c['c8'],0x87654321)
        node=bytes([165])*0x220;expected=bytearray(node)
        if c['presence']:struct.pack_into('<2d',expected,0x178,c['c8']/4096,0x87654321/4096)
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,expected),region(0x10003000,compare_code(0x10001000,0x10002000,len(node))+END),region(entry,jump(entry,0x4A273B)),region(0x4A2781,jump(0x4A2781,0x10003000))]
        regs=dict(ESI='0x10000000',EBX='0x10001000',EDI='0x0');want=dict(EAX=1,ECX=0)
    else:
        entry=0x4A99B0;stack=bytes((i*73+19)&255 for i in range(0x400)) if c['seed']=='pattern' else bytes([c['seed']])*0x400
        context=bytearray(0x600);struct.pack_into('<2I',context,0x560,0x10001000,len(c['slots']))
        ptrs=b''.join(pack32(0 if not n else 0x10002000+(n-1)*0x1000) for n in c['slots']) or bytes(4)
        vt=bytearray(0x44)
        for o,a in [(0x2C,0x10009000),(0x38,0x10009020),(0x40,0x10009040)]:struct.pack_into('<I',vt,o,a)
        fvt=bytearray(0x40);struct.pack_into('<I',fvt,0x38,0x1000A000)
        capture=bytes.fromhex('56578b74240c8b4c2410ff05')+pack32(0x10011000)+bytes.fromhex('8b3d')+pack32(0x10011004)+bytes.fromhex('01cf893d')+pack32(0x10011004)+bytes.fromhex('29cff3a45f5eb801000000c20800')
        date=bytes.fromhex('ea070a1a000c2238');date_stub=bytes.fromhex('8b442404c700')+date[:4]+bytes.fromhex('c74004')+date[4:]+bytes.fromhex('c20400')
        header=struct.pack('<7I',150,4,56+406*len(c['slots']),151,28,len(c['slots']),1)+bytes.fromhex('112233445566778899aa')+date+stack[0x25E:0x260]+struct.pack('<2I',152,406*len(c['slots']))
        models={};names={}
        for n in [1,2]:models[n],names[n]=fixture_model(n,c)
        expected=header+b''.join(expected_record(models[n],names[n],c,stack[0x340:0x342]) for n in c['slots'] if n)
        readout=bytes.fromhex('0fb6d0')+compare_code(0x10012000,0x10014000,len(expected))+load_abs(3,0x10011000)+load_abs(5,0x10011004)+END
        mem=[region(0x10000000,context),region(0x10001000,ptrs),region(0x10002000,models[1]),region(0x10003000,models[2]),region(0x10008000,vt),region(0x10009000,return_const(21)),region(0x10009020,return_const(0x12345678)),region(0x10009040,return_const(0x89ABCDEF)),region(0x1000A000,capture),region(0x1000C000,pack32(0x1000D000)),region(0x1000D000,fvt),region(0x10011000,pack32(0)+pack32(0x10012000)),region(0x10012000,bytes(0x1000)),region(0x10014000,expected),region(0x10016000,readout),region(0x550968,bytes.fromhex('112233445566778899aa')),region(0x4AB320,date_stub),region(0x7FFEFC00,stack),region(0x7FFF0000,pack32(0x10016000)+pack32(0x1000C000))]
        for n in [1,2]:
            for i,s in enumerate(names[n]):mem.append(region(0x10020000+(n-1)*0x1000+i*0x200,pack32(len(s))+pack32(0)+s+b'\0'))
        regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBX=3+sum(bool(n) for n in c['slots']),EBP=0x10012000+len(expected),EDX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=40000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase44-emulation-keeper-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase44-keeper-pipeline-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
