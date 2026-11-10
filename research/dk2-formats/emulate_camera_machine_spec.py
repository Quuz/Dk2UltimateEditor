"""Full Machine Spec Level setter -> full camera preset store.

No registry operations occur: registry open and imported RegSetValueExA/
RegCloseKey are EmulatorHelper memory-only fixtures. Native setter, DWORD
registry wrapper, destructor, zero-init and camera store execute unchanged.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from emulate_cameras import raw_record
from native_cameras import intermediate,preset_projection
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    rows=[]
    for spec in [0,2,3,4,5,10,0x80000000,0xFFFFFFFF]:
        for opened in [False,True]:rows.append(dict(name=f'spec-{spec}-open-{opened}',spec=spec,opened=opened,status=0,id=1,lo=3000,hi=12000))
    for status in [5,0xFFFFFFFF]:rows.append(dict(name=f'registry-status-{status}',spec=4,opened=True,status=status,id=1,lo=3000,hi=12000))
    for ident in [0,18,19,255]:rows.append(dict(name=f'camera-id-{ident}',spec=4,opened=True,status=0,id=ident,lo=3000,hi=12000))
    for lo,hi in [(12000,3000),(0xFFFFFFFF,0),(0x80000000,0x7FFFFFFF),(0x7FFFFFFF,0x80000000),(0,1),(0,2)]:rows.append(dict(name=f'zoom-bounds-{lo}-{hi}',spec=4,opened=False,status=0,id=18,lo=lo,hi=hi))
    for spec in [*range(11),11,0xFFFFFFFF]:rows.append(dict(name=f'profile-{spec}',kind='profile',spec=spec))
    return rows

def build_case(c):
    if c.get('kind')=='profile':return profile_case(c)
    settings=bytearray([165])*0xB4;wanted=bytearray(settings);struct.pack_into('<I',wanted,0x74,c['spec'])
    raw=raw_record(ident=c['id'],zoom_min=c['lo'],zoom_max=c['hi']);mid=intermediate(raw);bank=bytes([165])*0xC00;wantbank=bytearray(bank);v=preset_projection(raw,c['spec'])
    if v['accepted']:wantbank[v['slot_offset']:v['slot_offset']+152]=v['slot']
    # RegSetValueExA fixture captures handle/name/reserved/type/value/cb in28bytes.
    regset=b''
    for src,dst in [(4,0),(8,4),(12,8),(16,12),(24,20)]:regset+=bytes.fromhex('8b4424')+bytes([src])+b'\xA3'+pack32(0x10007000+dst)
    regset+=bytes.fromhex('8b4424148b00a3')+pack32(0x10007010)+bytes.fromhex('ff05')+pack32(0x10007018)+b'\xB8'+pack32(c['status'])+bytes.fromhex('c21800')
    close=bytes.fromhex('ff05')+pack32(0x1000701C)+bytes.fromhex('33c0c20400')
    opener=bytes.fromhex('8b442404c700')+pack32(0x12345678 if c['opened'] else 0)+b'\xB8'+pack32(int(c['opened']))+bytes.fromhex('c20400')
    wanted_capture=struct.pack('<8I',0x12345678,0x6BDA30,0,4,c['spec'],4,1,1) if c['opened'] else bytes(32)
    readout=compare_code(0x10000000,0x10003000,0xC00)+b'\x50'+compare_code(0x759A78,0x10004000,0xB4)+bytes.fromhex('5a21d050')+compare_code(0x10007000,0x10008000,32)+bytes.fromhex('5a21d0')+END
    # Native setter returns to adapter; adapter executes full449A30 normally.
    adapter=b'\xB9'+pack32(0x10000000)+b'\x68'+pack32(0x10002000)+b'\x68'+pack32(0x10005000)+jump(0x10006000+15,0x449A30)
    mem=[region(0x759A78,settings),region(0x10000000,bank),region(0x10002000,mid),region(0x10003000,wantbank),region(0x10004000,wanted),region(0x10005000,readout),region(0x10006000,adapter),region(0x10007000,bytes(32)),region(0x10008000,wanted_capture),region(0x10009000,regset),region(0x1000A000,close),region(0x66C010,pack32(0x10009000)),region(0x66C018,pack32(0x1000A000)),region(0x563E30,opener),region(0x7FFF0000,pack32(0x10006000)+pack32(c['spec']))]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address='0x566520',max_steps=20000,registers=json.dumps(dict(ECX='0x759A78')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,ESP,EIP'),dict(EAX=1)

def profile_case(c):
    from build_effect_semantics import pe_read
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();settings=bytearray([165])*0xB4;want=bytearray(settings)
    if c['spec']<11:
        values=struct.unpack('<8I',pe_read(data,0x66F5A0+36*c['spec'],32))
        for off,value in zip([0x44,0x4C,0x50,0x54,0x60,0x58,0x5C,0x68],values):struct.pack_into('<I',want,off,value)
    opener=bytes.fromhex('8b442404c7000000000033c0c20400')
    mem=[region(0x759A78,settings),region(0x10000000,want),region(0x10001000,compare_code(0x759A78,0x10000000,0xB4)+END),region(0x563E30,opener),region(0x7FFF0000,pack32(0x10001000)+pack32(c['spec']))]
    return dict(program='DKII-DX.exe',address='0x565CF0',max_steps=20000,registers=json.dumps(dict(ECX='0x759A78')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,ESP,EIP'),dict(EAX=1)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase46-emulation-machine-spec-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase46-camera-machine-spec-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
