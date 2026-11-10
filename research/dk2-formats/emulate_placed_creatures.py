"""Native creature converter, modern editor callback/writer and runtime fragments.

Original input normalization and all flag setters run. World creation, editor
constructors/array provider, virtual XY/trigger and objective operations use
explicit fixtures. Runtime fragment starts after health/gold arithmetic, stops
before dying actions. Gold helper is tested separately, without x87 placement.
"""
from pathlib import Path
from fractions import Fraction
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from emulate_placed_structures import load_abs,inc_stub
from emulate_placed_objects import return_const
from native_placed_creatures import *
ROOT=Path(__file__).resolve().parent

def raw_record(tag,**kw):
    v=dict(x=3,y=4,z=0x12345678,gold=300,level=0,flags=255,health=300,trigger=0x1234,creature=7,wander=0xA5,party=0x5A,flags2=255,ap=0x12345678,target=2,objective=0,owner=3);v.update(kw)
    p=bytearray(SIZES[tag]);struct.pack_into('<3IHBB',p,0,v['x'],v['y'],v['z'],v['gold'],v['level'],v['flags'])
    struct.pack_into('<I',p,20 if tag==199 else 16,v['health']);struct.pack_into('<H',p,20 if tag==198 else 24,v['trigger']);p[{198:22,199:28,200:26}[tag]]=v['creature']
    if tag==198:p[23]=v['wander']
    if tag==199:struct.pack_into('<I',p,16,v['ap']);p[26:28]=bytes([v['target'],v['objective']]);p[29:32]=bytes([v['wander'],v['party'],v['flags2']])
    if tag==200:struct.pack_into('<I',p,20,v['ap']);p[27]=v['owner']
    return struct.pack('<II',tag,len(p))+p

def cases():
    rows=[]
    for tag in SIZES:
        for name,kw in [('zero',dict(gold=0,health=0,level=0)),('99',dict(gold=99,health=99,level=1)),('100',dict(gold=100,health=100)),('255',dict(gold=255,health=255,level=255)),('256',dict(gold=256,health=256)),('300',dict(gold=300,health=300)),('max',dict(gold=65535,health=0xFFFFFFFF)),('high',dict(gold=0x1263,health=0x12345663))]:
            rows.append(dict(name=f'editor-{tag}-{name}',kind='editor',tag=tag,values=kw,major=3,minor=0))
        for program in ['game','editor']:
            for z in [0,0xFFFFFFFF]:rows.append(dict(name=f'convert-{program}-{tag}-{z}',kind='convert',tag=tag,z=z,program=program))
        for flags,flags2 in [(0,0),(255,255),(2,1),(4,2),(16,4),(128,8)]:rows.append(dict(name=f'flags-{tag}-{flags}',kind='runtime',tag=tag,flags=flags,flags2=flags2))
    for major,minor in [(2,49),(2,50),(3,0)]:rows.append(dict(name=f'editor-version-{major}-{minor}',kind='editor',tag=199,values={},major=major,minor=minor))
    for objective in [0,19,1]:
        for half in [0,1]:rows.append(dict(name=f'writer-{objective}-{half}',kind='writer',objective=objective,half=half))
    for level,growth in [(1,100),(0,150),(2,150),(10,-100)]:
        for initial,delta in [(0,55),(55,0xFFFFFFFF),(0xFFFFFFFF,1)]:rows.append(dict(name=f'gold-{level}-{growth}-{initial}-{delta}',kind='gold',level=level,growth=growth,initial=initial,delta=delta,base=500))
    for level,growth,percent in [(1,100,0),(1,100,50),(1,100,100),(2,150,300),(0,150,100),(10,-100,100),(1,100,0xFFFFFFFF)]:rows.append(dict(name=f'health-expression-{level}-{growth}-{percent}',kind='health_expression',level=level,growth=growth,percent=percent,base=1000))
    for party,other,leader,follower,state in [(3,3,1,1,222),(3,4,1,1,222),(255,255,1,1,222),(3,3,0,1,222),(3,3,1,0,222),(3,3,1,1,0)]:rows.append(dict(name=f'party-{party}-{other}-{leader}-{follower}-{state}',kind='party',party=party,other=other,leader=leader,follower=follower,state=state))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-DX.exe'
    if c['kind']=='convert':
        tag=c['tag'];m=MID[tag];raw=raw_record(tag,z=c['z']);mid=to_intermediate(raw);entry=CONVERTERS[tag]
        if c['program']=='editor':program='DK2 Editor pro.exe';entry=EDITOR_CONVERTERS[tag]
        offsets=[m['xyz'],m['xyz']+4,m['xyz']+8,m['gold'],m['health'],m['trigger']]
        readout=b''.join(load_abs(r,0x10001000+o) for r,o in zip([0,3,1,2,6,7],offsets))+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,raw[8:]),region(0x10001000,bytes(m['size'])),region(0x10002000,readout),region(0x7FFF0000,pack32(0x10002000)+pack32(0x10000000))];regs=dict(ECX='0x10001000')
        expected={r:int.from_bytes(mid[o:o+4],'little') for r,o in zip(['EAX','EBX','ECX','EDX','ESI','EDI'],offsets)}
    elif c['kind'] in ['editor','writer']:
        program='DK2 Editor pro.exe';model=bytearray(0x60);definition=bytearray(0x1900);vtable=bytearray(0x40);definition[0x168]=7;model[0x18]=7;struct.pack_into('<I',model,0,0x10003000)
        for off,address in [(0x14,0x10009000),(0x18,0x10009010),(0x28,0x10009020),(0x2C,0x10009030)]:struct.pack_into('<I',vtable,off,address)
        mem=[region(0x10001000,model),region(0x10010000,definition),region(0x10003000,vtable),region(0x10004000,pack32(0x10010000)),region(0x54F9D0,pack32(0x10004000)+pack32(1)),region(0x10009000,return_const(3)),region(0x10009010,return_const(4)),region(0x10009020,return_const(0x1234)),region(0x10009030,bytes.fromhex('c20400'))]
        if c['kind']=='editor':
            tag=c['tag'];entry={198:0x491120,199:0x490EB0,200:0x491340}[tag];raw=raw_record(tag,**c['values']);mid=to_intermediate(raw);mapmodel=bytearray(0x80);struct.pack_into('<II',mapmodel,0x60,c['major'],c['minor'])
            readout=load_abs(0,0x10001018)+load_abs(3,0x10001020)+load_abs(1,0x1000101C if tag==200 else 0x10001024)+load_abs(2,0x10001028)+load_abs(6,0x1000102C)+load_abs(7,0x10001030)+load_abs(5,0x10001038)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem += [region(0x10000000,mid),region(0x10005000,mapmodel),region(0x442C10,return_const(0x10010000,4)),region(0x488850,return_const(0x10006000,12)),region(0x4889D0,return_const(0x10001000,8)),region(0x10008000,readout),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10000000)+pack32(0x10005000))]
            out=modern_editor_projection(raw,c['major'],c['minor']);f=out['flags'];objective=out.get('objective',0);target=out.get('target',0)
            expected=dict(EAX=7|(out['level']<<8)|(out['health']<<16)|(out['gold']<<24),EBX=out['wander']|(255<<8)|(objective<<16)|(target<<24),ECX=out.get('ap',0),EDX=int(bool(f&1)),ESI=int(bool(f&2)),EDI=int(bool(f&4)),EBP=int(bool(f&16)))
        else:
            entry=0x4952F0;model[0x19:0x1C]=bytes([255,201,254]);model[0x20:0x25]=bytes([0xA5,0x5A,c['objective'],2,0xCC])
            for i in range(8):struct.pack_into('<I',model,0x28+4*i,1)
            for i in range(4):struct.pack_into('<I',model,0x48+4*i,1)
            mem[0]=region(0x10001000,model)
            expected_bytes=struct.pack('<3IHBBIIH6B',3,4,0,254,255,255,0xCC,201,0x1234,2,c['objective'] or 19,7,0xA5,0x5A,15)
            offsets=range(0,24,4) if not c['half'] else range(24,32,4)
            registers=[0,3,1,2,6,7];names=['EAX','EBX','ECX','EDX','ESI','EDI']
            readout=b''.join(load_abs(r,0x10005000+o) for r,o in zip(registers,offsets))+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem += [region(0x10005000,bytes([0xCC])*32),region(0x10008000,readout),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10001000)+pack32(0x10005000))]
            expected={r:int.from_bytes(expected_bytes[o:o+4],'little') for r,o in zip(names,offsets)}
    elif c['kind']=='runtime':
        tag=c['tag'];entry={198:0x520180,199:0x520460,200:0x5208B0}[tag];start={198:0x520376,199:0x520741,200:0x520AB9}[tag];end={198:0x5203FF,199:0x52085C,200:0x520B5B}[tag]
        mid=to_intermediate(raw_record(tag,flags=c['flags'],flags2=c['flags2']));node=bytearray(0x520);node[0x46F]=0xA6;node[0xF0]=0x77;struct.pack_into('<I',node,0x3EA,0xFFFFFFFF);node[0x417:0x419]=bytes([0x5A,0xCC]);stack=bytearray(0x80);struct.pack_into('<I',stack,0x34 if tag==199 else 0x38,0x10001000)
        readout=bytes.fromhex('0fb605')+pack32(0x1000146F)+load_abs(3,0x100013EA)+bytes.fromhex('0fb60d')+pack32(0x100010F0)+bytes.fromhex('0fb715')+pack32(0x10001417)+bytes.fromhex('0fb735')+pack32(0x10001419)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        trigger=bytearray(0x10);trigger[15]=0x66
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,trigger),region(0x518090,return_const(0x10002000,4)),region(0x48BE20,bytes.fromhex('c20c00')),region(0x486E20,b'\xC3'),region(0x478B80,bytes.fromhex('c20c00')),region(0x7FFF0000,stack),region(entry,jump(entry,start)),region(end,readout)]
        regs=dict(ESI='0x10000000');out=runtime_flags(tag,c['flags'],c['flags2'],0xA6,0xFFFFFFFF,0x77)
        expected=dict(EAX=out['flags'],EBX=out['extra'],ECX=out['f0'],EDX=0xA55A if tag in [198,199] else 0xCC5A,ESI=0x1234)
    elif c['kind']=='health_expression':
        entry=0x520180;mid=bytearray(56);mid[0x2E]=c['level'];struct.pack_into('<I',mid,0x30,c['percent']);node=bytearray(0x400);definition=bytearray(0x700);growth=bytearray(0x400);player=bytearray(8);stack=bytearray(0x80)
        struct.pack_into('<I',node,0x370,0x10002000);struct.pack_into('<H',definition,0x692,c['base']);struct.pack_into('<h',growth,0xE1+2*c['level'],c['growth']);struct.pack_into('<I',stack,0x38,0x10001000)
        readout=bytes.fromhex('dd1d')+pack32(0x10005000)+load_abs(0,0x10005000)+load_abs(3,0x10005004)+bytes.fromhex('89d1')+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,definition),region(0x10003000,growth),region(0x10004000,player),region(0x10005000,bytes(8)),region(0x6E4154,pack32(0x10003000)),region(0x7FFF0000,stack),region(entry,jump(entry,0x520255)),region(0x5202DB,readout)];regs=dict(ESI='0x10000000',EBX='0x10004000')
        base=scaled_base_bits(c['base'],c['level'],c['growth']);value=float(Fraction.from_float(0.01)*c['percent']*base-1);lo,hi=struct.unpack('<II',struct.pack('<d',value));expected=dict(EAX=lo,EBX=hi,ECX=base)
    elif c['kind']=='gold':
        entry=0x48AE70;node=bytearray(0x420);definition=bytearray(0x6A0);growth=bytearray(0x400)
        node[0x403]=c['level'];struct.pack_into('<I',node,0x370,0x10002000);struct.pack_into('<I',node,0x3FB,c['initial']);struct.pack_into('<H',definition,0x69C,c['base']);struct.pack_into('<h',growth,0x13B+2*c['level'],c['growth'])
        readout=load_abs(0,0x100013FB)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10001000,node),region(0x10002000,definition),region(0x10003000,growth),region(0x6E4154,pack32(0x10003000)),region(0x10008000,readout),region(0x7FFF0000,pack32(0x10008000)+pack32(c['delta']))];regs=dict(ECX='0x10001000')
        cap=scaled_base_bits(c['base'],c['level'],c['growth']);expected=dict(EAX=gold_add_cap(c['initial'],c['delta'],cap))
    else:
        entry=0x458C70;state=bytearray(16);leader=bytearray(0x480);follower=bytearray(0x480);owner=bytearray(16);lookup=bytearray(16)
        struct.pack_into('<I',state,8,0x10001000);leader[0x417]=c['party'];leader[0x46F]=2*c['leader'];struct.pack_into('<I',leader,0x2A,0x10003000);struct.pack_into('<H',owner,6,1);follower[0x417]=c['other'];follower[0x46F]=4*c['follower'];struct.pack_into('<I',follower,0x25E,c['state']);struct.pack_into('<II',lookup,0,0,0x10002000)
        readout=load_abs(0,0x10005000)+load_abs(3,0x10005004)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,state),region(0x10001000,leader),region(0x10002000,follower),region(0x10003000,owner),region(0x6EE540,lookup),region(0x10005000,bytes(8)),region(0x48BE20,inc_stub(0x10005000,12)),region(0x48AD30,inc_stub(0x10005004,4)),region(0x476D30,bytes.fromhex('c20800')),region(0x10008000,readout),region(0x7FFF0000,pack32(0x10008000))];regs=dict(ECX='0x10000000')
        hit=int(c['party']==c['other'] and c['leader'] and c['follower'] and c['state']==222);expected=dict(EAX=hit,EBX=hit)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=program,address=hex(entry),max_steps=6000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase31-emulation-creature-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']));print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase31-creature-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
