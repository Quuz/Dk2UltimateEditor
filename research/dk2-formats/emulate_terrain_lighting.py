"""Full native RGB setters/getters, Terrain flag codec, game color fragment.

Memory-only adapters bypass unrelated model construction and terrain resources.
No GUI, game process, renderer or original file changes.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from native_terrain_lighting import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
BOOL_MASKS=[*[1<<i for i in range(19)],1<<23,1<<24,1<<28,1<<29]

def cases():
    rows=[]
    for channel in range(3):
        for value in [0,255,256,511,512,65535,65536]:rows.append(dict(name=f'channel-{channel}-{value}',kind='channel',channel=channel,value=value))
    masks=sum(sum(g['masks']) for g in GROUPS.values())
    for flags in [0,0xFFFFFFFF,*[1<<i for i in range(32)],masks,masks|0x10000000,masks|0x20000000,masks|0x30000000]:rows.append(dict(name=f'flags-{flags}',kind='flags',flags=flags))
    for flags in [0,masks,*[m for g in GROUPS.values() for m in g['masks']],0x30000000,0xFFFFFFFF]:
        for present in [False,True]:
            for times in [1,2]:rows.append(dict(name=f'game-{flags}-{present}-{times}',kind='game',flags=flags,present=present,times=times))
    return rows

def build_case(c):
    mem=[];regs={};program='DK2 Editor pro.exe'
    if c['kind']=='channel':
        entry=[0x420320,0x420370,0x4203C0][c['channel']];getter=[0x420350,0x4203A0,0x4203F0][c['channel']];node=bytearray([165])*28;wantbuf=bytearray(node);v=c['value']&65535;mask=0x800<<c['channel'];old=struct.unpack_from('<I',node,20)[0];struct.pack_into('<I',wantbuf,20,old|mask if v>255 else old&~mask);wantbuf[24+c['channel']]=v&255;got=(v&255)+(256 if v>255 else 0)
        adapter=b'\xB9'+pack32(0x10000000)+b'\x68'+pack32(0x10003000)+jump(0x10004000+10,getter)
        readout=bytes.fromhex('0fb7d0')+compare_code(0x10000000,0x10002000,28)+END
        mem=[region(0x10000000,node),region(0x10002000,wantbuf),region(0x10003000,readout),region(0x10004000,adapter),region(0x7FFF0000,pack32(0x10004000)+pack32(c['value']))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EDX=got)
    elif c['kind']=='flags':
        entry=0x425640;node=bytearray([165])*0x3D4;wantbuf=bytearray(node)
        for i,mask in enumerate(BOOL_MASKS):struct.pack_into('<I',wantbuf,0x374+i*4,int(bool(c['flags']&mask)))
        for g in GROUPS.values():
            off=g['model']+20;prior=struct.unpack_from('<I',node,off)[0];v=prior&~0x3800
            for i,mask in enumerate(g['masks']):
                if c['flags']&mask:v|=0x800<<i
            struct.pack_into('<I',wantbuf,off,v)
        adapter=b'\xB9'+pack32(0x10000000)+b'\x68'+pack32(0x10003000)+jump(0x10004000+10,0x4254C0)
        readout=bytes.fromhex('89c2')+compare_code(0x10000000,0x10002000,len(node))+END
        mem=[region(0x10000000,node),region(0x10002000,wantbuf),region(0x10003000,readout),region(0x10004000,adapter),region(0x7FFF0000,pack32(0x10004000)+pack32(c['flags']))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EDX=flags_projection(c['flags']))
    else:
        program='DKII-DX.exe';entry=0x4F66E0;mid=bytearray(736);struct.pack_into('<I',mid,0xB0,1);struct.pack_into('<I',mid,0x248,c['flags']);node=bytearray([165])*460
        for gi,g in enumerate(GROUPS.values()):
            mid[g['mid']:g['mid']+3]=bytes([0,255,128])
            for i,off in enumerate(g['runtime']):struct.pack_into('<H',node,off,[65530,255,128][i]);struct.pack_into('<I',mid,g['presence']+i*4,int(c['present']))
        wantbuf=bytes(node)
        for _ in range(c['times']):wantbuf=game_colour_fragment(mid,wantbuf)
        loop=bytes.fromhex('ff0d')+pack32(0x10005000)+bytes.fromhex('0f85')+struct.pack('<i',0x4F69A0-(0x10004000+12))+jump(0x10004000+12,0x10003000)
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,wantbuf),region(0x10003000,compare_code(0x10001000,0x10002000,len(node))+END),region(0x10004000,loop),region(0x10005000,pack32(c['times'])),region(entry,jump(entry,0x4F69A0)),region(0x4F6AB7,jump(0x4F6AB7,0x10004000))];regs=dict(EBP='0x10000000',EBX='0x10001000',EDI='0x0');want=dict(EAX=1,ECX=0)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=20000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase48-emulation-terrain-lighting-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase48-terrain-lighting-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
