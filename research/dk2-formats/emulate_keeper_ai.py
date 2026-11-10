"""Native AI spell selection loops with explicit helper/PRNG fixtures; flag pipelines.

Selection skips caller category/random branch choice and ends before target search.
Availability, cooldown, definition lookup/count and random source are memory stubs.
Original flag tests, scan order, cost arithmetic and fallback stores are executed.
All adapters exist only in Ghidra EmulatorHelper memory, never in EXE/game files.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_keeper_ai import *
ROOT=Path(__file__).resolve().parent
def cases():
    result=[]
    # Four candidates: mixed flags and a final target excluded by bit10.
    defaults=[dict(flags=12,cost=2499),dict(flags=12,cost=9996),dict(flags=12,cost=0),dict(flags=28,cost=0xFFFFFFFF)]
    for mode in ['defensive','attacking']:
        for p in range(8):result.append(dict(name=f'{mode}-power{p}',kind='selection',mode=mode,power=p,spells=defaults,draws=[0,0,0]))
        variants=[([dict(flags=8 if mode=='defensive' else 4,cost=9996),dict(flags=0,cost=0)],[4]),
            ([dict(flags=12,cost=0),dict(flags=12,cost=9996)],[0,4]),
            ([dict(flags=12,cost=2498),dict(flags=12,cost=2499)],[0,0]),
            ([dict(flags=12,cost=0xFFFFFFFF)],[1718674]),
            ([dict(flags=28,cost=0),dict(flags=2,cost=0)],[]),
            ([dict(flags=12,cost=0,availability=2),dict(flags=12,cost=0,cooldown=1),dict(flags=12,cost=0)],[0]),
            ([],[]),([dict(flags=0xFFFFFFEF,cost=4998)],[0])]
        for i,(spells,draws) in enumerate(variants):result.append(dict(name=f'{mode}-variant{i}',kind='selection',mode=mode,power=0,spells=spells,draws=draws))
    for raw in [0,1,2,4,8,16,0x1F,0xFFFFFFFF]:
        result.append(dict(name=f'editor-{raw}',kind='editor',flags=raw))
        result.append(dict(name=f'game-{raw}',kind='game',flags=raw,initial=0x80000001))
    return result
def load_abs(reg,address):return bytes([0x8B,0x05+(reg<<3)])+pack32(address)
def build_case(case):
    program='DKII-DX.exe';registers={};memory=[]
    if case['kind']=='selection':
        entry=0x503060;ai=bytearray(0x600);world=bytearray(0x100);vtable=bytearray(0x120);definitions=bytearray(0x1000)
        available=bytearray(0x100);cooldowns=bytearray(0x100);random_values=bytearray(0x100);frame=0x7FFF0800
        for i,s in enumerate(case['spells'],1):
            struct.pack_into('<I',definitions,i*128+0x54,s['flags']);struct.pack_into('<I',definitions,i*128+0x58,s['cost'])
            struct.pack_into('<I',available,i*4,s.get('availability',3));struct.pack_into('<I',cooldowns,i*4,s.get('cooldown',0))
        for i,d in enumerate(case['draws']):struct.pack_into('<I',random_values,i*4,d)
        struct.pack_into('<I',ai,0xA,0x10001000);struct.pack_into('<I',ai,0xE,0x10007000);struct.pack_into('<I',ai,0x4B6,case['power']<<23)
        struct.pack_into('<I',world,0,0x10002000);struct.pack_into('<I',vtable,0x110,0x10009000);struct.pack_into('<I',vtable,0x114,0x10009020)
        getter=bytes.fromhex('8b442404c1e00705')+pack32(0x10003000)+bytes.fromhex('c20400')
        count=b'\xB8'+pack32(len(case['spells']))+b'\xC3'
        def indexed(address):return bytes.fromhex('8b4424048b0485')+pack32(address)+bytes.fromhex('c20400')
        # Volatile EAX/ECX/EDX only. Capture requested bound before returning next fixture draw.
        random=load_abs(1,0x1000A000)+bytes.fromhex('8b54240489148d')+pack32(0x1000A010)+bytes.fromhex('8b048d')+pack32(0x10008000)+bytes.fromhex('ff05')+pack32(0x1000A000)+b'\xC3'
        readout=bytes.fromhex('0fb645f8')+load_abs(3,0x1000A000)
        for reg,off in [(1,0),(2,4),(6,8),(7,12)]:readout+=load_abs(reg,0x1000A010+off)
        readout+=b'\xC3'
        start,end=(0x5032CD,0x50335A) if case['mode']=='defensive' else (0x503480,0x503519)
        registers=dict(ECX='0x10001000',EDX='0x10002000',ESI='0x10000000',EBP=hex(frame))
        memory=[region(0x10000000,ai),region(0x10001000,world),region(0x10002000,vtable),region(0x10003000,definitions),
            region(0x10005000,available),region(0x10006000,cooldowns),region(0x10007000,bytes(0x100)),region(0x10008000,random_values),
            region(0x10009000,getter),region(0x10009020,count),region(0x1000A000,bytes(0x100)),region(frame-0x100,bytes(0x200)),
            region(0x4BAFA0,indexed(0x10005000)),region(0x4BE390,indexed(0x10006000)),region(0x5558D0,random),region(entry,jump(entry,start)),region(end,readout)]
        result=select_spell(case['mode'],case['spells'],case['power'],case['draws']);bounds=[r['bound'] for r in result['trace']]+[0]*4
        expected=dict(EAX=result['selected_id'],EBX=result['draw_count'],ECX=bounds[0],EDX=bounds[1],ESI=bounds[2],EDI=bounds[3])
    elif case['kind']=='editor':
        program='DK2 Editor pro.exe';entry=0x420270;registers=dict(ECX='0x10000000')
        adapter=b'\xB9'+pack32(0x10000000)+jump(0x10002005,0x420230)
        # Decoder RET4 reaches original encoder, encoder RET reaches sentinel.
        memory=[region(0x10000000,bytes(0x300)),region(0x10002000,adapter),region(0x7FFF0000,pack32(0x10002000)+pack32(case['flags'])+pack32(0xDEADBEEF))]
        expected=dict(EAX=editor_saved_flags(case['flags']))
    else:
        entry=0x56BAB0;raw=bytearray(406);mid=bytearray(0x300);definition=bytearray(0x100)
        struct.pack_into('<I',raw,0xDC,case['flags']);struct.pack_into('<I',mid,0x154,case['initial'])
        registers=dict(EBX='0x10000000',EBP='0x10001000',ESI='0x1',ESP='0x7FFEFFF4')
        # Original converter discards12 bytes of prior temporary arguments before end.
        bridge=bytes.fromhex('8bf5bf')+pack32(0x10002000)+jump(0x56BCE2+7,0x4F7EB6)
        memory=[region(0x10000000,raw),region(0x10001000,mid),region(0x10002000,definition),region(0x7FFEFFF4,bytes(12)+pack32(0xDEADBEEF)),
            region(entry,jump(entry,0x56BCC8)),region(0x56BCE2,bridge),region(0x4F7EC6,bytes.fromhex('8b4754c3'))]
        expected=dict(EAX=game_flags(case['flags'],case['initial']))
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
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase26-emulation-keeper-ai-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']));print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase26-keeper-ai-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
