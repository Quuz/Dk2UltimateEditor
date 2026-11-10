"""Original11 Creeping weight branches, selected game/editor pipelines and action.

All adaptations are EmulatorHelper memory only. Kernel entry TEST supplies the
same Creeping condition as the skipped flag read; precomputed metric supplied.
Game pipeline chains converter/loader fragments; editor chains callback/writer.
No resources, GUI float conversion, full AI scan or live game is emulated.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_creature_creep import *

ROOT=Path(__file__).resolve().parent
# key, original function entry, JZ at Creeping gate, exit, distance register
KERNELS=[
 ('a',0x45B140,0x45B28B,0x45B2EA,'EBX'),('b1',0x45BDF0,0x45BFD5,0x45C033,'EBX'),
 ('b2',0x45BDF0,0x45C1FE,0x45C25C,'EBX'),('c',0x45B500,0x45B64B,0x45B6A9,'EBX'),
 ('d',0x45B800,0x45B920,0x45B97E,'EBX'),('e',0x45BAD0,0x45BC18,0x45BC74,'EDI'),
 ('f1',0x45C690,0x45C774,0x45C7D3,'EBX'),('f2',0x45C690,0x45C962,0x45C9C1,'EBX'),
 ('g1',0x45CB00,0x45D1F8,0x45D254,'EBX'),('g2',0x45CB00,0x45D406,0x45D462,'EBX'),
 ('target',0x48EE90,0x48EFB1,0x48F00D,'EBX')]

def cases():
    rows=[]
    values=[(0xFFFFFFFF,0xFFFFFFFF,255,0x8000,0),(409600,50,1,0xFFFF,0x1000),
        (409600,50,2,105,0x1000),(409600,0,1,100,0x1000),(0xFFFFFFFF,300,1,100,0x1000),
        (123456,0xFFFFFFFF,2,0xFFFF,0x1000),(0xFFFFFFFF,0x80000000,255,32767,0x1000),
        (123456,51,2,0xFF97,0x1000)]
    for key,*_ in KERNELS:
        for i,v in enumerate(values):rows.append(dict(name=f'kernel-{key}-{i}',kind='kernel',kernel=key,values=dict(zip(['distance_q12','raw_percentage','level','level_multiplier_bits','state_flags'],v))))
    for raw in [0,50,100,255,256,300,32000,0xFFFFFFFF]:
        for kind in ['game','editor']:rows.append(dict(name=f'{kind}-{raw}',kind=kind,values=dict(raw_percentage=raw)))
    for i,(flags,value,present) in enumerate([(0xA5A5A5A5,1,True),(0xFFFFFFFF,2,True),(0,3,True),(0x1000,0,False)]):
        rows.append(dict(name=f'action-{i}',kind='action',values=dict(state_flags=flags,action_value=value,possessed_creature_present=present)))
    for i,values in enumerate([(0,0,40960,0),(0,0,4096,4096),(-4096,4096,8192,-8192),(-0x80000000,0,0x7FFFFFFF,1)]):
        rows.append(dict(name=f'squared-{i}',kind='squared',values=dict(zip(['source_x','source_y','target_x','target_y'],values))))
    for raw in [50,300,0x80000000,0xFFFFFFFF]:rows.append(dict(name=f'stat26-{raw}',kind='setter',values=dict(raw_percentage=raw)))
    return rows

def build_case(case):
    kind=case['kind'];v=case['values'];node=bytearray(0x900);definition=bytearray(0x900);frame=0x7FFF0800
    memory=[];registers={};program='DKII-DX.exe'
    if kind=='kernel':
        _,entry,start,end,distance_reg=next(r for r in KERNELS if r[0]==case['kernel'])
        struct.pack_into('<I',node,0x370,0x10001000);struct.pack_into('<I',node,0x3E6,v['state_flags']);node[0x403]=v['level']
        struct.pack_into('<I',definition,0x760,v['raw_percentage']);growth=bytearray(0x1000)
        struct.pack_into('<H',growth,0x2A3+2*v['level'],v['level_multiplier_bits'])
        scratch=bytearray(pack32(0x10000000)*80)
        if case['kernel'] in ['f1','f2']:
            # These variants saved the metric immediately before our entry JZ.
            struct.pack_into('<I',scratch,0x100-0x24,v['distance_q12'])
        registers={r:'0x10000000' for r in ['EAX','EBX','ECX','EDX','ESI','EDI']};registers.update(EBP=hex(frame));registers[distance_reg]=hex(v['distance_q12'])
        adapter=bytes.fromhex('f705')+pack32(0x100003E6)+pack32(0x1000)+jump(entry+10,start)
        readout=bytes.fromhex('8bc3c3') if distance_reg=='EBX' else bytes.fromhex('8bc7c3')
        memory=[region(0x10000000,node),region(0x10001000,definition),region(0x10004000,growth),region(0x6E4154,pack32(0x10004000)),
            region(frame-0x100,scratch),region(entry,adapter),region(end,readout)]
        expected=weighted_distance(**v)['distance']
    elif kind=='game':
        entry=0x568E30;raw=bytearray(5537);mid=bytearray(0x2000);struct.pack_into('<I',raw,0xEE4,v['raw_percentage'])
        registers=dict(EBX='0x10000000',EBP='0x10002000',ESI='0x1')
        bridge=b'\xBB'+pack32(0x10005000)+b'\xBD'+pack32(0x10002000)+jump(0x5694B5+10,0x493BDD)
        memory=[region(0x10000000,raw),region(0x10002000,mid),region(0x10005000,definition),
            region(entry,jump(entry,0x5694A3)),region(0x5694B5,bridge),region(0x493BF3,bytes.fromhex('8b8360070000c3'))]
        expected=v['raw_percentage']
    elif kind=='editor':
        program='DK2 Editor pro.exe';entry=0x4A01B0;mid=bytearray(0x2000);model=bytearray(0x2000)
        struct.pack_into('<I',mid,0x208,1);struct.pack_into('<I',mid,0x12E4,v['raw_percentage'])
        registers=dict(EBX='0x10000000',EBP='0x10003000',EDI='0x0')
        bridge=bytes.fromhex('8bdd')+jump(0x4A0F68+2,0x4A84B4)
        memory=[region(0x10000000,mid),region(0x10003000,model),region(entry,jump(entry,0x4A0F54)),region(0x4A0F68,bridge),
            region(0x4A84CA,bytes.fromhex('8b8424780f0000c3')),region(0x7FFF0000,pack32(0xDEADBEEF)+bytes(0x1800))]
        expected=native_editor_projection(v['raw_percentage'])
    elif kind=='action':
        entry=0x517310;action=bytearray(0x20);player=bytearray(0x1400);pointers=bytearray(16)
        struct.pack_into('<I',action,0,v['action_value']);struct.pack_into('<H',action,0x10,1)
        struct.pack_into('<H',player,0x134,2 if v['possessed_creature_present'] else 0);struct.pack_into('<I',node,0x3E6,v['state_flags'])
        struct.pack_into('<I',pointers,4,0x10002000);struct.pack_into('<I',pointers,8,0x10000000)
        readout=bytes.fromhex('a1')+pack32(0x100003E6)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,node),region(0x10002000,player),region(0x10004000,action),region(0x6EE540,pointers),
            region(0x7FFF0000,pack32(0x10005000)+pack32(0x10004000)),region(0x10005000,readout)]
        expected=creep_flag_after_action(**v)
    elif kind=='squared':
        entry=0x45B140;other=bytearray(0x900);struct.pack_into('<ii',node,0x1A,v['target_x'],v['target_y']);struct.pack_into('<ii',other,0x1A,v['source_x'],v['source_y'])
        registers=dict(ESI='0x10000000',EBX='0x10001000',EBP=hex(frame))
        memory=[region(0x10000000,node),region(0x10001000,other),region(entry,jump(entry,0x45B232)),region(0x45B282,bytes.fromhex('8bc3c3')),region(frame-0x100,bytes(0x200))]
        expected=squared_distance_q12(**v)
    else:
        entry=0x494A70;registers=dict(ECX='0x10000000')
        readout=bytes.fromhex('a1')+pack32(0x10000760)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,definition),region(0x7FFF0000,pack32(0x10002000)+pack32(26)+pack32(v['raw_percentage'])),region(0x10002000,readout)]
        expected=v['raw_percentage']
    return dict(program=program,address=hex(entry),max_steps=1000,registers=json.dumps(registers),memory=json.dumps(dict(regions=memory)),return_registers='EAX,ESP,EIP'),dict(EAX=expected)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase24-emulation-creep-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);raw='\n'.join(c.text for c in response.content if c.type=='text');result=json.loads(raw)
                    if response.isError or 'error' in result:raise RuntimeError(raw)
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,ensure_ascii=False,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert int(result['registers']['EAX'],16)==expected['EAX'],(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']))
                print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase24-creep-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
