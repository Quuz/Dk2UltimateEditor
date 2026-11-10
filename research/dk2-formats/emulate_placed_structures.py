"""Original Trap callback and Door flag/guard branches; explicit external fixtures.

Trap full callback uses create, checksum, manufacture/reset/neighbor/upkeep stubs.
Original4A96D0 activation body executes. Door starts at flag assignment after
creation; original4A66C0 and4A6800 execute with renderer/nav fixtures; refresh
helpers are skipped. Converter and Editor flag codecs execute original bodies.
No EXE, game files or game processes are changed.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_placed_structures import *
ROOT=Path(__file__).resolve().parent
def cases():
    rows=[]
    for i,n in enumerate([0,1,2,255]):
        for trigger in [1,3]:rows.append(dict(name=f'trap-{n}-{trigger}',kind='trap',shots=n,trigger=trigger,x_bits=0xFFFFFFFF if n==255 else 3,y_bits=4,opaque_z=0x12345678))
    for flags in [0,1,2,3,4,0x80,0xFF]:
        rows.append(dict(name=f'door-{flags}',kind='door',flags=flags))
        rows.append(dict(name=f'editor-{flags}',kind='editor',flags=flags))
    for tag in [195,196]:
        for opaque in [0,0xFFFFFFFF,0x12345678]:rows.append(dict(name=f'convert-{tag}-{opaque}',kind='converter',tag=tag,opaque=opaque))
    return rows
def load_abs(reg,address):return bytes([0x8B,0x05+(reg<<3)])+pack32(address)
def inc_stub(address,cleanup=0):return bytes.fromhex('ff05')+pack32(address)+(b'\xC2'+struct.pack('<H',cleanup) if cleanup else b'\xC3')
def build_case(c):
    program='DKII-DX.exe';registers={};memory=[]
    if c['kind']=='trap':
        entry=0x520E50;mid=bytearray(0x24);world=bytearray(0x100);player=bytearray(0x1200);node=bytearray(0x200);definition=bytearray(0xC0)
        struct.pack_into('<3I',mid,0x14,c['x_bits'],c['y_bits'],c['opaque_z']);mid[0x20:0x24]=bytes([c['shots'],7,1,0xFF])
        struct.pack_into('<I',world,0x84,0x10003000);struct.pack_into('<H',player,4,1);struct.pack_into('<I',node,0xF7,0x10006000);node[0x10C]=255;definition[0xBD]=c['trigger']
        create=bytes.fromhex('56578b742414bf')+pack32(0x1000501A)+bytes.fromhex('b903000000f3a58b442418c700')+pack32(0x10005000)+bytes.fromhex('5f5eb801000000c21000')
        readout=bytes.fromhex('0fb605')+pack32(0x1000510C)+load_abs(3,0x10007000)+load_abs(1,0x10007004)+load_abs(2,0x10007008)+load_abs(6,0x1000501A)+load_abs(7,0x1000501E)+load_abs(5,0x10005022)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,mid),region(0x10001000,world),region(0x10003000,player),region(0x10005000,node),region(0x10006000,definition),region(0x10007000,bytes(16)),region(0x10008000,readout),
            region(0x5108C0,bytes.fromhex('c20800')),region(0x4B7C10,create),region(0x4A9F10,b'\xC3'),region(0x4A9720,inc_stub(0x10007004)),region(0x4A9A00,inc_stub(0x10007000)),region(0x4C1B60,bytes.fromhex('c20400')),
            region(0x5066E0,inc_stub(0x10007008,24)),region(0x505680,bytes.fromhex('c20400')),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10000000)+pack32(0x10001000))]
        expected=dict(EAX=0xDF if c['shots'] else 0xFF,EBX=int(bool(c['shots']) and c['trigger']!=3),ECX=int(bool(c['shots']) and c['trigger']==3),EDX=int(not c['shots']),ESI=center_q12(c['x_bits']),EDI=center_q12(c['y_bits']),EBP=0)
    elif c['kind']=='door':
        entry=0x520F40;mid=bytearray(0x28);mid[0x24]=c['flags'];node=bytearray(0x200);definition=bytearray(0xB0);stack=bytearray(0x80)
        struct.pack_into('<I',node,0xF7,0x10002000);struct.pack_into('<I',node,0x111,0x77);struct.pack_into('<ii',node,0x1A,0x1800,0x1800)
        struct.pack_into('<I',stack,0,0xDEADBEEF);struct.pack_into('<I',stack,0x44,0x10001000)
        world=bytearray(0xA00);nav=bytearray(0x20);struct.pack_into('<I',world,0x9F2,0x10005000);struct.pack_into('<I',nav,8,3);struct.pack_into('<I',nav,0x10,0x10006000)
        resource_vtable=bytearray(0x24);struct.pack_into('<I',resource_vtable,0x20,0x10009000)
        readout=load_abs(0,0x10001109)+load_abs(3,0x10001111)+b'\xC3'
        memory=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,definition),region(0x10004000,world),region(0x10005000,nav),region(0x10006000,bytes(16)),region(0x10007000,pack32(0x10008000)),region(0x10008000,resource_vtable),
            region(0x10009000,bytes.fromhex('b834120000c21000')),region(0x75B468,pack32(0x10007000)),region(0x6E5050,pack32(0x10004000)),region(0x7FFF0000,stack),region(entry,jump(entry,0x521116)),region(0x521135,readout),
            region(0x4A66F0,bytes.fromhex('c20400')),region(0x4A6B70,b'\xC3'),region(0x4D1930,bytes.fromhex('c20c00'))]
        registers=dict(EDI='0x10000000',ECX='0x10001000');out=door_placement(c['flags'],0x77);expected=dict(EAX=out['blueprint'],EBX=out['projected_state'])
    elif c['kind']=='editor':
        program='DK2 Editor pro.exe';entry=0x41ED30;registers=dict(ECX='0x10000000')
        adapter=b'\xB9'+pack32(0x10000000)+b'\xE8'+struct.pack('<i',0x41ED10-(0x10002005+5))+bytes.fromhex('0fb6c0c3')
        memory=[region(0x10000000,bytes(0x40)),region(0x10002000,adapter),region(0x7FFF0000,pack32(0x10002000)+pack32(c['flags'])+pack32(0xDEADBEEF))];expected=dict(EAX=c['flags']&3)
    else:
        entry=0x56F690 if c['tag']==195 else 0x56F600;raw=bytearray(16 if c['tag']==195 else 20);struct.pack_into('<3I',raw,0,3,4,c['opaque'])
        if c['tag']==195:raw[12:16]=bytes([255,7,1,0xA5])
        else:struct.pack_into('<H',raw,12,0x1234);raw[14:20]=bytes([7,1,0xFF,0xA5,0x5A,0xCC])
        readout=load_abs(0,0x10001014)+load_abs(3,0x10001018)+load_abs(1,0x1000101C)+load_abs(2,0x10001020)+load_abs(6,0x10001024)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,raw),region(0x10001000,bytes(0x30)),region(0x10002000,readout),region(0x7FFF0000,pack32(0x10002000)+pack32(0x10000000))];registers=dict(ECX='0x10001000')
        expected=dict(EAX=3,EBX=4,ECX=c['opaque'],EDX=0xA50107FF if c['tag']==195 else 0x01071234,ESI=0 if c['tag']==195 else 255)
    return dict(program=program,address=hex(entry),max_steps=4000,registers=json.dumps(registers),memory=json.dumps(dict(regions=memory)),return_registers=','.join([*expected,'ESP','EIP'])),expected
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase29-emulation-placed-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']));print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase29-placed-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
