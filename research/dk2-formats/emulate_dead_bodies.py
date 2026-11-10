"""Native CA parsers, game creation, editor callback/writer and scale fragment.

Providers and unrelated render/world helpers are explicit EmulatorHelper fixtures.
Byte comparisons check whole buffers; original PE/database bytes stay unchanged.
Legacy cases capture ST0 as double, with an explicit integer-return fixture;
the emulator does not honor the original ftol truncation mode for FISTP.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from party_writer_stack_audit import cleanups
from native_dead_bodies import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def raw_record(value=300,level=10,z=0x76543210,creature=7,x=3,y=4):
    return struct.pack('<II3IHBB',202,16,x,y,z,value,creature,level)

def cases():
    rows=[]
    for p in ['game','editor']:
        for declared,fail in [(16,False),(15,False),(17,False),(16,True)]:rows.append(dict(name=f'parser-{p}-{declared}-{fail}',kind='parser',program=p,declared=declared,fail=fail))
    for seed in [0,165,255]:rows.append(dict(name=f'converter-{seed}',kind='converter',seed=seed))
    for level in [0,1,10,255]:
        for value in [0,300,65535]:rows.append(dict(name=f'loader-{level}-{value}',kind='loader',level=level,value=value,seed=165,valid=True,enabled=True,allocate=True,tps=20,decay=60,angle=2047))
    for change in [dict(name='invalid',valid=False),dict(name='disabled',enabled=False),dict(name='allocation-failed',allocate=False),dict(name='overflow',tps=0xFFFFFFFF,decay=65535,angle=0)]:
        c=dict(kind='loader',level=10,value=300,seed=0,valid=True,enabled=True,allocate=True,tps=20,decay=60,angle=123);c.update(change);c['name']='loader-'+c['name'];rows.append(c)
    for z in [0,0xFFFFFFFF]:rows.append(dict(name=f'loader-z-{z}',kind='loader',level=10,value=300,seed=165,valid=True,enabled=True,allocate=True,tps=20,decay=60,angle=2047,z=z))
    for major in [1,2,3]:
        for value in [0,99,100,255,256,300,1000,2560,65535]:rows.append(dict(name=f'editor-{major}-{value}',kind='editor',major=major,value=value,level=0 if value<100 else 255,health=1000))
    for presence in [0,1]:rows.append(dict(name=f'editor-missing-{presence}',kind='editor',major=3,value=300,level=10,health=1000,missing=presence))
    for level,growth in [(0,100),(1,100),(10,100),(10,150),(255,100),(255,-100),(2,-32768)]:rows.append(dict(name=f'scale-{level}-{growth}',kind='scale',level=level,growth=growth,base=4096))
    for seed in [0,165,255]:
        for level,health in [(0,0),(255,44),(1,255)]:rows.append(dict(name=f'writer-{seed}-{level}-{health}',kind='writer',seed=seed,level=level,health=health))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-DX.exe'
    if c['kind']=='parser':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x570500 if c['program']=='game' else 0x430CD0;raw=raw_record();dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0x98,0x10004000)
        io=bytes.fromhex('5657be')+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10003008)+bytes.fromhex('f3a48b44240cc700')+pack32(0xFFFFFFFF if c['fail'] else 0)+bytes.fromhex('8b54241c8b0d')+pack32(0x10003008)+bytes.fromhex('890a5f5ec3')
        cb=bytes.fromhex('56578b74240cbf')+pack32(0x10001000)+b'\xB9'+pack32(32)+bytes.fromhex('f3a6b8000000000f94c0a3')+pack32(0x10003000)+bytes.fromhex('890d')+pack32(0x10003004)+bytes.fromhex('5f5eb801000000c3');readout=bytes.fromhex('89c2')+load_abs(0,0x10003000)+load_abs(3,0x10003008)+load_abs(1,0x10003004)+END
        mem=[region(0x10000000,raw[8:]),region(0x10001000,intermediate(raw)),region(0x10002000,dispatch),region(0x10003000,bytes(12)),region(0x10004000,cb),region(0x10005000,struct.pack('<II',202,c['declared'])),region(0x10006000,pack32(1)),region(0x10007000,readout),region(0x5B3EB0 if c['program']=='game' else 0x4B7F10,io),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10005000)+pack32(0x10006000)+pack32(0x10002000))]
        if c['program']=='editor' and c['fail']:mem.append(region(0x4E4D4A,return_const(0,12)))
        expected=dict(EAX=int(not c['fail']),EBX=16,ECX=0,EDX=int(not c['fail']))
    elif c['kind']=='converter':
        program='DK2 Editor pro.exe';entry=0x42FAA0;raw=raw_record();readout=compare_code(0x10001000,0x10002000,32)+END
        mem=[region(0x10000000,raw[8:]),region(0x10001000,bytes([c['seed']])*32),region(0x10002000,intermediate(raw)),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');expected=dict(EAX=1,ECX=0)
    elif c['kind']=='loader':
        entry=0x5212D0;raw=raw_record(value=c['value'],level=c['level'],z=c.get('z',0x76543210));v=runtime_projection(raw,c['tps'],c['decay'],c['angle']);node=bytearray([c['seed']]*320);want=bytearray(node);world=bytearray(0xA400);vt=bytearray(0xC4);variables=bytearray(0x1130);struct.pack_into('<H',variables,0x1129,0xBEEF);struct.pack_into('<I',world,0,0x10010000);struct.pack_into('<I',world,0x1C,0x10003000);struct.pack_into('<I',world,0x84,0x10014000)
        for o,va in [(0xBC,0x10011000),(0xC0,0x10011040)]:struct.pack_into('<I',vt,o,va)
        ok=c['valid'] and c['enabled'] and c['allocate']
        if ok:
            struct.pack_into('<I',want,0,0x66D904)
            for o in range(6,18,2):struct.pack_into('<H',want,o,0)
            struct.pack_into('<3I',want,0x1A,*v['position']);struct.pack_into('<I',want,0x16,0);struct.pack_into('<I',want,0x40,v['decay_counter']);struct.pack_into('<H',want,0xF4,v['angle']);struct.pack_into('<3I',want,0xF6,*v['position']);struct.pack_into('<H',want,0x102,v['angle']);struct.pack_into('<2H',want,0x124,0,0);want[0x128]=7;struct.pack_into('<I',want,0x129,0x10018000);want[0x12D]=c['level'];struct.pack_into('<I',want,0x12E,0);struct.pack_into('<HI',want,0x132,0,0);struct.pack_into('<II',want,0x138,0,0);want[0xF1]&=0xFE
            for o in [0x95,0x97,0x99]:struct.pack_into('<H',want,o,0)
        checksum=bytes.fromhex('ff05')+pack32(0x10019000)+bytes.fromhex('c20800');position=bytes.fromhex('56578b74240c8d791ab903000000f3a55f5ec20400');owner=bytes.fromhex('0fb7442404a3')+pack32(0x10019004)+bytes.fromhex('c20400');lookup=return_const(0x10018000 if c['valid'] else 0,4);definition=return_const(0x10018000,4)
        readout=bytes.fromhex('89c2')+compare_code(0x10001000,0x10002000,320)+load_abs(3,0x1000303E)+load_abs(5,0x10019000)+load_abs(6,0x10019004)+END
        mem=[region(0x10000000,intermediate(raw)),region(0x10001000,node),region(0x10002000,want),region(0x10003000,world),region(0x10010000,vt),region(0x10011000,lookup),region(0x10011040,definition),region(0x10014000,variables),region(0x10018000,bytes(0x800)),region(0x10019000,bytes(8)),region(0x1001A000,readout),region(0x6E5050,pack32(0x10003000)),region(0x6A4818,pack32(int(c['enabled']))),region(0x6F2548,struct.pack('<H',0x1234)),region(0x6F2590,pack32(c['tps'])),region(0x6F25A0,pack32(c['decay'])),region(0x5108C0,checksum),region(0x5558D0,return_const(c['angle'])),region(0x554B30,return_const(0x10001000 if c['allocate'] else 0,4)),region(0x507850,b'\xC3'),region(0x4B3610,b'\xC3'),region(0x4B1DF0,return_const(1,4)),region(0x4B5560,position),region(0x4B4E70,bytes.fromhex('8b442404894116c20400')),region(0x4B4C30,owner),region(0x494860,return_const(0,4)),region(0x4B37D0,return_const(1,16)),region(0x4B49D0,return_const(1,4)),region(0x4B45B0,return_const(1,12)),region(0x7FFF0000,pack32(0x1001A000)+pack32(0x10000000)+pack32(0x10003000))];expected=dict(EAX=1,ECX=0,EDX=int(ok),EBX=int(ok),EBP=1,ESI=0x1234 if ok else 0)
    elif c['kind']=='editor':
        program='DK2 Editor pro.exe';entry=0x491DE0;raw=raw_record(value=c['value'],level=c['level']);mid=bytearray(intermediate(raw));node=bytearray([165]*96);node[0x18]=7;want=bytearray(node);world=bytearray(0x68);struct.pack_into('<I',world,0x60,c['major']);definition=bytearray(0x1900);definition[0x168]=7;struct.pack_into('<H',definition,0x15EA,c['health']);skip='missing' in c
        if skip:struct.pack_into('<I',mid,4*c['missing'],0)
        else:want[0x21]=255;want[0x58]=1;want[0x19]=c['level'];want[0x1A]=editor_health(c['value'],c['major'],int(float(c['value'])/c['health']*100.0) if c['major']==1 else None)
        # Both providers count their calls; coordinates and definition lookup
        # remain covered by the original callback and actual41CE20 respectively.
        #488850 consumes only player ID; the coordinate/option arguments remain
        # on the stack for4889D0, whose four arguments consume16 bytes.
        player=bytes.fromhex('ff05')+pack32(0x10006000)+return_const(0x10008000,4);create=bytes.fromhex('ff05')+pack32(0x10006004)+return_const(0x10001000,16)
        readout=bytes.fromhex('89c2')+compare_code(0x10001000,0x10002000,96)+load_abs(3,0x10006000)+load_abs(5,0x10006004)+END
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,want),region(0x10003000,world),region(0x10010000,definition),region(0x10005000,pack32(0x10010000)),region(0x10006000,bytes(8)),region(0x10007000,readout),region(0x10008000,bytes(0x20)),region(0x54F9D0,pack32(0x10005000)+pack32(1)),region(0x488850,player),region(0x4889D0,create),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10000000)+pack32(0x10003000))];expected=dict(EAX=1,ECX=0,EDX=1,EBX=int(not skip),EBP=int(not skip))
        if c['major']==1:
            value=float(c['value'])/c['health']*100.0
            conversion=bytes.fromhex('dd1d')+pack32(0x10009000)+b'\xB8'+pack32(int(value))+bytes.fromhex('31d2c3')
            readout=bytes.fromhex('89c2')+compare_code(0x10001000,0x10002000,96,0x1000B000)+compare_code(0x10009000,0x1000A000,8)+load_abs(3,0x1000B000)+load_abs(5,0x10006004)+END
            mem=[r for r in mem if r['address']!='0x10007000']+[region(0x10007000,readout),region(0x4ACDA8,conversion),region(0x10009000,bytes(8)),region(0x1000A000,struct.pack('<d',value)),region(0x1000B000,bytes(8))];expected=dict(EAX=1,ECX=0,EDX=1,EBX=1,EBP=1)
    elif c['kind']=='scale':
        entry=0x4B12D0;node=bytearray([165]*320);node[0x12D]=c['level'];struct.pack_into('<I',node,0x129,0x10001000);definition=bytearray(0x800);struct.pack_into('<H',definition,0x626,c['base']);growth=bytearray(0x500);struct.pack_into('<h',growth,0x2D9+2*c['level'],c['growth']);value=body_scale_bits(c['level'],c['base'],c['growth']);readout=bytes.fromhex('83c404')+load_abs(0,0x10000048)+load_abs(3,0x1000004C)+load_abs(1,0x10000058)+load_abs(2,0x1000005C)+b'\xC3'
        mem=[region(0x10000000,node),region(0x10001000,definition),region(0x10002000,growth),region(0x6E4154,pack32(0x10002000)),region(entry,jump(entry,0x4B1540)),region(0x4B15E4,readout),region(0x7FFF0000,pack32(0xDEADBEEF)+bytes(0x80))];regs=dict(ESI='0x10000000');expected=dict(EAX=value,EBX=value,ECX=0,EDX=0)
    else:
        program='DK2 Editor pro.exe';entry=0x4947C0;world=bytearray(0xC00);struct.pack_into('<2I',world,0xB24,0x10001000,1);node=bytearray(96);struct.pack_into('<I',node,0,0x10003000);node[0x18:0x1B]=bytes([7,c['level'],c['health']]);node[0x58]=1;definition=bytearray(0x1900);definition[0x168]=7;vt=bytearray(0x40);struct.pack_into('<I',vt,0x14,0x10004000);struct.pack_into('<I',vt,0x18,0x10004020);struct.pack_into('<I',vt,0x34,0x10004040);filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x10005000)
        wanted=writer_payload(dict(x=3,y=4,health_byte=c['health'],creature_id=7,level=c['level']));body=bytes.fromhex('ff05')+pack32(0x10006000)+bytes.fromhex('56578b74240cbf')+pack32(0x10007000)+b'\xB9'+pack32(16)+bytes.fromhex('f3a45f5ec20800');capture=bytes.fromhex('817c24081000000075')+bytes([len(body)])+body+bytes.fromhex('c20800');readout=bytes.fromhex('0fb6d0')+compare_code(0x10007000,0x10008000,16)+load_abs(3,0x10006000)+END
        mem=[region(0x10000000,world),region(0x10001000,pack32(0x10002000)),region(0x10002000,node),region(0x10003000,vt),region(0x10004000,return_const(3)),region(0x10004020,return_const(4)),region(0x10004040,return_const(1)),region(0x10005000,capture),region(0x10006000,bytes(4)),region(0x10007000,bytes(16)),region(0x10008000,wanted),region(0x10009000,pack32(0x1000A000)),region(0x1000A000,filevt),region(0x1000B000,readout),region(0x1000C000,definition),region(0x1000E000,pack32(0x1000C000)),region(0x54F9D0,pack32(0x1000E000)+pack32(1)),region(0x7FFEFC00,bytes([c['seed']])*0x400),region(0x7FFF0000,pack32(0x1000B000)+pack32(0x10009000))]
        for va,cleanup in cleanups().items():
            if va not in {0x488B30,0x488D30,0x41CE20}:mem.append(region(va,return_const(0,cleanup)))
        regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0,EBX=1,EDX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=25000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase38-emulation-body-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase38-body-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
