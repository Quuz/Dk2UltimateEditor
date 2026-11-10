"""Original first-person fragments. Memory adapters skip callers and capture queued actions.

No adapters modify files or game processes. Native slot getter uses its initialized
zero fallback; native world spell lookup deliberately has an empty catalogue.
The camera getter is original. Action capture replaces enqueueing, not rendering.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_creature_first_person import *

ROOT=Path(__file__).resolve().parent
MOTION=[('input',0x40EC46,0x40EC98,-0x10),('osc-a',0x40F12D,0x40F521,-0x50),
        ('osc-b',0x40F1B1,0x40F209,-0x50),('action80',0x40F25F,0x40F2BA,-0x78)]
def cases():
    rows=[]
    for key,*_ in MOTION:
        for i,(w,o,c,p,half,base) in enumerate([(4096,2457,4,1,False,0),(4096,4096,4,2,False,0),
            (4096,4096,4,6,True,0),(0,0,4,1,False,123),(4096,4096,0,0xFFFFFFFF,False,123),
            (0xFFFFFFFF,0xFFFFFFFF,255,0xFFFFFFFF,True,0x7FFFFFFF),(0x80000000,0x80000000,12,13,False,0),
            (4097,4097,3,5,True,0xFFFFFFF0),(1,1,2,17,False,1),(4096,4096,4,14,True,0)]):
            rows.append(dict(name=f'{key}-{i}',kind='motion',kernel=key,values=dict(raw_waddle=w,raw_oscillate=o,raw_cycle=c,phase=p,half_oscillation=half),base=base))
    defaults=dict(raw_zoom=8192,level=2,slots=[[0,0],[7,2],[9,1]],slot_count=3,camera_base=4096,state_flags=0,player_tag=0x1234)
    variants=[dict(raw_zoom=v) for v in [0,4096,6144,8191,8192,11878,12288,0xFFFFFFFF]]
    variants += [dict(state_flags=0x80000000),dict(state_flags=0xFFFFFFFF,camera_base=12345),
        dict(slots=[[0,0],[7,3],[9,2]]),dict(slots=[[5,2],[7,1],[9,0]]),dict(level=0,slots=[[1,1],[2,2],[3,3]]),
        dict(slot_count=0),dict(slot_count=1),dict(slot_count=2),dict(slot_count=255),dict(camera_base=0xFFFFFFFF),
        dict(raw_zoom=8191,state_flags=0x80000000),dict(slots=[[0,0],[0,0],[0,0]]),dict(level=255,slots=[[5,255],[6,0],[7,0]])]
    for i,changes in enumerate(variants):rows.append(dict(name=f'sniper-{i}',kind='sniper',values={**defaults,**changes}))
    for i,(flags,value,present) in enumerate([(0xA5A5A5A5,1,True),(0xFFFFFFFF,2,True),(0,3,True),(0x80000000,0,False)]):
        rows.append(dict(name=f'flag-{i}',kind='flag',values=dict(state_flags=flags,action_value=value,possessed_creature_present=present)))
    return rows

def load_abs(reg,address):return bytes([0x8B,0x05+(reg<<3)])+pack32(address)
def build_case(case):
    v=case['values'];memory=[];registers={};node=bytearray(0x900);definition=bytearray(0x1000);frame=0x7FFF0800
    if case['kind']=='motion':
        entry=0x40E910;_,start,end,slot=next(r for r in MOTION if r[0]==case['kernel'])
        struct.pack_into('<I',node,0x370,0x10001000)
        for off,k in [(0x863,'raw_waddle'),(0x867,'raw_oscillate'),(0x86B,'raw_cycle')]:struct.pack_into('<I',definition,off,v[k])
        scratch=bytearray(0x200);struct.pack_into('<I',scratch,0x100+slot,case['base']);struct.pack_into('<I',scratch,0x100-0x2C,int(v['half_oscillation']))
        struct.pack_into('<I',scratch,0x100-0x30,0x10003000)
        values=motion_terms(**v);term=values['input_waddle'] if case['kernel']=='input' else values['action80_waddle'] if case['kernel']=='action80' else values['oscillate']
        expected=dict(EAX=u32(term if case['kernel']=='action80' else case['base']+term))
        readout=b'\x8B\x85'+pack32(slot&0xFFFFFFFF)+b'\xC3'
        memory=[region(0x10000000,node),region(0x10001000,definition),region(0x10003000,bytes(0x100)),region(frame-0x100,scratch),
            region(0x6CE968,pack32(v['phase'])),region(entry,jump(entry,start)),region(end,readout)]
        registers=dict(EDI='0x10000000',EBP=hex(frame))
    elif case['kind']=='sniper':
        entry=0x40FDF0;interface=bytearray(0x100);world=bytearray(0x2000);bridge=bytearray(0x2000);wv=bytearray(0x300);bv=bytearray(0x100)
        struct.pack_into('<I',node,0x370,0x10001000);struct.pack_into('<I',node,0x3E6,v['state_flags']);node[0x403]=v['level']
        struct.pack_into('<I',definition,0x79B,v['raw_zoom']);definition[0x789]=v['slot_count']
        for i,(spell,available) in enumerate(v['slots']):definition[0x5E1+i*19:0x5E3+i*19]=bytes([spell,available])
        struct.pack_into('<H',interface,8,v['player_tag']);struct.pack_into('<I',interface,0x53,0x10002000)
        struct.pack_into('<I',world,0,0x10007000);struct.pack_into('<I',wv,0xE8,0x50DFA0)
        struct.pack_into('<I',bridge,0,0x10008000);struct.pack_into('<I',bridge,0x3E5+0x1D8,v['camera_base']);struct.pack_into('<I',bv,0xD0,0x43A980)
        stack=bytearray(0x100);struct.pack_into('<I',stack,0x14,0x10004000)
        # Save caller-preserved registers, copy20-byte packet, increment capture count, stdcall cleanup.
        capture=bytes.fromhex('56578b74240c')+load_abs(0,0x1000A000)+bytes.fromhex('c1e0058db8')+pack32(0x1000A100)+bytes.fromhex('b905000000f3a5ff05')+pack32(0x1000A000)+bytes.fromhex('5f5ec20400')
        readout=load_abs(0,0x1000A000)+load_abs(3,0x1000A100)+load_abs(1,0x1000A104)+load_abs(2,0x1000A10C)
        readout+=bytes.fromhex('0fb73d')+pack32(0x1000A110)+bytes.fromhex('c1e7100bd7')
        readout+=load_abs(6,0x1000A120)+load_abs(7,0x1000A12C)+bytes.fromhex('0fb72d')+pack32(0x1000A130)+bytes.fromhex('c1e5100bfd0fb66c2410')
        readout+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,node),region(0x10001000,definition),region(0x10002000,world),region(0x10004000,bridge),region(0x10006000,interface),
            region(0x10007000,wv),region(0x10008000,bv),region(0x1000A000,bytes(0x200)),region(0x7FFF0000,stack),region(0x6E4168,b'\x01'),region(0x6E4178,bytes(19)),
            region(entry,jump(entry,0x40FE38)),region(0x4346E0,capture),region(0x40FE83,jump(0x40FE83,0x1000B000)),region(0x40FF98,jump(0x40FF98,0x1000B000)),region(0x1000B000,readout)]
        registers=dict(EBX='0x10000000',ESI='0x10006000')
        projected=sniper_commands(**v);cmd=projected['commands'];selected=projected['selected_spell'] or 0
        expected=dict(EAX=len(cmd),EBX=cmd[0]['value'] if cmd else 0,ECX=300 if cmd else 0,
            EDX=((v['player_tag']<<16)|0xE) if cmd else 0,ESI=cmd[1]['value'] if cmd else 0,
            EDI=((v['player_tag']<<16)|0x27) if cmd else 0,EBP=selected)
    else:
        entry=0x5171D0;action=bytearray(0x20);player=bytearray(0x1400);pointers=bytearray(16)
        struct.pack_into('<I',action,0,v['action_value']);struct.pack_into('<H',action,0x10,1)
        struct.pack_into('<H',player,0x134,2 if v['possessed_creature_present'] else 0);struct.pack_into('<I',node,0x3E6,v['state_flags'])
        struct.pack_into('<I',pointers,4,0x10002000);struct.pack_into('<I',pointers,8,0x10000000)
        readout=load_abs(0,0x100003E6)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,node),region(0x10002000,player),region(0x10004000,action),region(0x6EE540,pointers),
            region(0x7FFF0000,pack32(0x10005000)+pack32(0x10004000)),region(0x10005000,readout)]
        expected=dict(EAX=sniper_flag_after_action(**v))
    return dict(program='DKII-DX.exe',address=hex(entry),max_steps=3000,registers=json.dumps(registers),memory=json.dumps(dict(regions=memory)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase25-emulation-first-person-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']))
                print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase25-first-person-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
