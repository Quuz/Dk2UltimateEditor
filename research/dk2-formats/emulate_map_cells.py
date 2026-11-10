"""Native map writer, parser and selected cell/flag paths in emulator memory.

Full editor writer uses a read-only file-capture vcall; original row/cell loops
and unchecked neighbor accesses execute with explicit exterior guard bytes.
Full game parser uses a controlled file-read stub and callback capture instead
of constructing the world. Runtime flag fragment skips terrain/room setup.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_map_cells import *
ROOT=Path(__file__).resolve().parent
def cases():
    rows=[]
    for i,(terrain,guard,seed,fourth,w) in enumerate([([1,17,26,36,1,1],0,0xA5,0x5A,3),([17,26,36,1,1,1],0,0xA5,0x5A,3),
        ([1,4,1,1,26,1,1,1,1],0,0xA5,0,3),([1,5,1,4,26,1,1,1,1],0,0xA5,0xFF,3),
        ([17],4,0,7,1),([17],5,0,7,1),([17,17],0,2,0,2),([1,5,26,1,1,1],0,0,0,3)]):
        rows.append(dict(name=f'writer-{i}',kind='writer',width=w,height=len(terrain)//w,terrain=terrain,owners=list(range(1,len(terrain)+1)),guard=guard,initial_flags=seed,fourth=fourth))
    for i,fourth in enumerate([0,1,0xA5,0xFF]):
        rows.append(dict(name=f'parser-{i}',kind='parser',width=3,height=2,cells=[v for j in range(6) for v in [j+1,j+2,[0,1,2,3,0x80,0xFF][j],fourth]]))
    for i,cell in enumerate([[26,2,2,0xFF],[1,255,0x80,7],[17,1,0,0],[36,0,255,0xA5]]):
        rows.append(dict(name=f'converter-{i}',kind='converter',cell=cell,initial_flags=0x40))
    for i,(flags,old) in enumerate([(0,0xFFFF),(1,0xFFFF),(2,0),(3,0xA5A5),(0x80,0x1FFF),(0xFF,0xEFFF)]):
        rows.append(dict(name=f'runtime-{i}',kind='runtime',flags=flags,old=old))
    return rows
def load_abs(reg,address):return bytes([0x8B,0x05+(reg<<3)])+pack32(address)
def fnv(b):
    v=0x811C9DC5
    for x in b:v=((v^x)*0x1000193)&0xFFFFFFFF
    return v
def fnv_readout(address,length):
    # Returns EAX hash; uses ECX/EDX/ESI.
    return b'\xB8'+pack32(0x811C9DC5)+b'\xBE'+pack32(address)+b'\xB9'+pack32(length)+bytes.fromhex('0fb61633c269c093010001464975f1')
def build_case(case):
    kind=case['kind'];program='DKII-DX.exe';memory=[];registers={}
    if kind=='writer':
        program='DK2 Editor pro.exe';entry=0x494470;w=case['width'];h=case['height'];n=w*h;model=bytearray(0xA00);padding=w+2
        terrain=bytes([case['guard']])*padding+bytes(case['terrain'])+bytes([case['guard']])*padding
        struct.pack_into('<II',model,0x950,0x10002000+padding,0x10003000);struct.pack_into('<III',model,0x990,w,h,n)
        vtable=bytearray(0x40);struct.pack_into('<I',vtable,0x38,0x10005000)
        # Append every supplied write chunk, including outer/header/data prefix.
        capture=bytes.fromhex('56578b74240c8b4c2410')+load_abs(0,0x10008000)+bytes.fromhex('8db8')+pack32(0x10006000)+bytes.fromhex('010d')+pack32(0x10008000)+bytes.fromhex('f3a45f5ec20800')
        stack=bytearray(0x200);sp=0x7FFF0000;stack[0x100-0x2E]=case['initial_flags'];stack[0x100-0x2D]=case['fourth']
        struct.pack_into('<II',stack,0x100,0x10009000,0x10004000)
        expected_cells=writer_cells(w,h,bytes(case['terrain']),bytes(case['owners']),case['initial_flags'],case['fourth'],case['guard'])
        expected_file=struct.pack('<9I',100,4,n*4+36,101,8,w,h,102,n*4)+expected_cells
        readout=fnv_readout(0x10006000,len(expected_file))+load_abs(3,0x10008000)+load_abs(1,0x10006024)+load_abs(2,0x10006024+len(expected_cells)-4)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,model),region(0x10002000,terrain),region(0x10003000,bytes(case['owners'])),region(0x10004000,pack32(0x10004800)),region(0x10004800,vtable),
            region(0x10005000,capture),region(0x10006000,bytes(0x1000)),region(0x10008000,bytes(4)),region(0x10009000,readout),region(sp-0x100,stack)]
        registers=dict(ECX='0x10000000');expected=dict(EAX=fnv(expected_file),EBX=len(expected_file),ECX=int.from_bytes(expected_cells[:4],'little'),EDX=int.from_bytes(expected_cells[-4:],'little'))
    elif kind=='parser':
        entry=0x56D2D0;w=case['width'];h=case['height'];cells=bytes(case['cells']);file=struct.pack('<6I',101,8,w,h,102,len(cells))+cells
        cfg=bytearray(0x30);struct.pack_into('<I',cfg,0x18,0x10005000)
        # cdecl read(result*, handle, dest, count, status*), return result* inEAX.
        read=bytes.fromhex('56578b54240c')+load_abs(6,0x10008000)+b'\x81\xC6'+pack32(0x10002000)+bytes.fromhex('8b7c24148b4c2418010d')+pack32(0x10008000)+bytes.fromhex('f3a433c089028b4c241c89018bc25f5ec3')
        # Capture x,y and the16-byte intermediate for each cell in24-byte rows.
        callback=bytes.fromhex('5657')+load_abs(0,0x10008004)+bytes.fromhex('6bc0188db8')+pack32(0x10006000)+bytes.fromhex('8b44240c89078b4424108947048b74241483c708b910000000f3a4ff05')+pack32(0x10008004)+bytes.fromhex('b8010000005f5ec3')
        out=b''.join(struct.pack('<II',i%w,i//w)+intermediate_cell(cells[4*i:4*i+4]) for i in range(w*h))
        readout=fnv_readout(0x10006000,len(out))+load_abs(3,0x10008004)+load_abs(1,0x10006014)+load_abs(2,0x10006000+len(out)-4)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,pack32(0x1)),region(0x10001000,cfg),region(0x10002000,file),region(0x5B3EB0,read),region(0x10005000,callback),
            region(0x10006000,bytes(0x1000)),region(0x10008000,bytes(8)),region(0x10009000,readout),region(0x7FFF0000,pack32(0x10009000)+pack32(0x10000000)+pack32(0x10001000))]
        expected=dict(EAX=fnv(out),EBX=w*h,ECX=int.from_bytes(out[20:24],'little'),EDX=int.from_bytes(out[-4:],'little'))
    elif kind=='converter':
        program='DK2 Editor pro.exe';entry=0x42D1B0;mid=bytearray(16);mid[14]=case['initial_flags'];struct.pack_into('<I',mid,12,int.from_bytes(mid[12:16],'little'))
        readout=load_abs(0,0x1000000C)+load_abs(3,0x10000000)+load_abs(1,0x10000004)+load_abs(2,0x10000008)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,mid),region(0x10001000,bytes(case['cell'])),region(0x10002000,readout),region(0x7FFF0000,pack32(0x10002000)+pack32(0x10001000))]
        registers=dict(ECX='0x10000000');out=intermediate_cell(bytes(case['cell']),case['initial_flags']);expected=dict(EAX=int.from_bytes(out[12:16],'little'),EBX=1,ECX=1,EDX=1)
    else:
        entry=0x51EC60;grid=bytearray(0x30);tiles=bytearray(9*16);mid=bytearray(16);mid[14]=case['flags'];x=1;y=1
        struct.pack_into('<I',grid,0xC,3);struct.pack_into('<I',grid,0x14,0x10001000);struct.pack_into('<H',tiles,4*16+8,case['old']);stack=bytearray(0x40)
        struct.pack_into('<I',stack,0,0xDEADBEEF);struct.pack_into('<I',stack,0x24,0x10002000)
        readout=bytes.fromhex('0fb74008')+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,grid),region(0x10001000,tiles),region(0x10002000,mid),region(0x7FFF0000,stack),region(entry,jump(entry,0x51EDE4)),region(0x51EE06,readout),region(0x51EE27,readout)]
        registers=dict(EDI='0x10000000',EBP=hex(x),EBX=hex(y));expected=dict(EAX=runtime_substrate_flags(case['old'],case['flags']))
    return dict(program=program,address=hex(entry),max_steps=10000,registers=json.dumps(registers),memory=json.dumps(dict(regions=memory)),return_registers=','.join([*expected,'ESP','EIP'])),expected
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase28-emulation-map-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']));print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase28-map-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
