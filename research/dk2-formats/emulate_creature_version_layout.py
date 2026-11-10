"""Old/local versus current C6/C7/C8 converters and diagnostic field identity.

All converter instructions execute; diagnostic tests execute only3 print calls
from each dump. Logger is a capture fixture. No game process or data writes.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from native_placed_creatures import SIZES,MID,to_intermediate
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    rows=[]
    for old in [False,True]:
        for tag in [198,199,200]:
            for seed in [0,165]:
                for pattern in [0,255,'pattern']:rows.append(dict(name=f'converter-{old}-{tag}-{seed}-{pattern}',kind='converter',old=old,tag=tag,seed=seed,pattern=pattern))
        for party,wander,trigger in [(0,0,0),(255,255,65535),(7,40,65526),(1,128,32767),(128,1,32768),(2,254,4660)]:rows.append(dict(name=f'dump-{old}-{party}-{wander}-{trigger}',kind='dump',old=old,party=party,wander=wander,trigger=trigger))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-local13.exe' if c['old'] else 'DKII-DX.exe'
    if c['kind']=='converter':
        t=c['tag'];raw=bytes((i*73+19)&255 for i in range(SIZES[t])) if c['pattern']=='pattern' else bytes([c['pattern']])*SIZES[t];envelope=struct.pack('<2I',t,len(raw))+raw;expected=bytearray(to_intermediate(envelope));m=MID[t]
        expected[m['flags']]|=c['seed']
        if t==199:expected[m['flags2']]|=c['seed']
        entry=({198:0x565630,199:0x565730,200:0x565840} if c['old'] else {198:0x56F330,199:0x56F430,200:0x56F540})[t]
        mem=[region(0x10000000,raw),region(0x10001000,bytes([c['seed']])*m['size']),region(0x10002000,expected),region(0x10003000,compare_code(0x10001000,0x10002000,len(expected))+END),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    else:
        entry=0x488270 if c['old'] else 0x490240;start,end=(0x488CAB,0x488CF6) if c['old'] else (0x490CD8,0x490D23);logger=0x550F00 if c['old'] else 0x5595C0
        offsets=[0x416,0x417,0x418] if c['old'] else [0x417,0x418,0x419];labels=[0x68F6D8,0x68F6C0,0x68F6AC] if c['old'] else [0x69CF54,0x69CF3C,0x69CF28]
        node=bytearray([165])*0x600;node[offsets[0]]=c['party'];node[offsets[1]]=c['wander'];struct.pack_into('<H',node,offsets[2],c['trigger']);trigger=c['trigger'] if c['trigger']<32768 else c['trigger']-65536;wanted=b''.join(struct.pack('<2I',p,v&0xFFFFFFFF) for p,v in zip(labels,[c['party'],c['wander'],trigger]))
        capture=bytes.fromhex('8b0d')+pack32(0x10005000)+bytes.fromhex('8b44240889018b44240c89410483c108890d')+pack32(0x10005000)+b'\xC3'
        mem=[region(0x10000000,node),region(0x10001000,bytes(24)),region(0x10002000,wanted),region(0x10003000,compare_code(0x10001000,0x10002000,24)+END),region(0x10005000,pack32(0x10001000)),region(entry,jump(entry,start)),region(end,jump(end,0x10003000)),region(logger,capture)];regs=dict(ESI='0x10000000',EDX='0x0');want=dict(EAX=1,ECX=0)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=10000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase47-emulation-creature-version-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase47-creature-version-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
