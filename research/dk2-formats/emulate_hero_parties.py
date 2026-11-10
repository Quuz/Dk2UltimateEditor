"""Original C9 parsers, world storage, editor member fields and full Things writer.

Comparisons execute REPE CMPSB against an independently built expected byte buffer;
no checksum collision assumption. I/O, creation and virtual provider fixtures are
explicit. Program bytes are changed only inside EmulatorHelper memory.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_placed_creatures import raw_record
from native_hero_parties import *
from party_writer_stack_audit import cleanups
ROOT=Path(__file__).resolve().parent

def fixture_record(size=555,opaque=0xA5,party=3):
    p=bytearray(size)
    if size==555:
        p[:32]=b'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef';struct.pack_into('<HB',p,32,0x1234,party);p[35:43]=bytes([opaque])*8;start=43;count=16
    else:
        struct.pack_into('<HB',p,0,0x1234,party);start=8 if size==264 else 12;count=8
        if size==300:p[268:300]=b'LegacyName\0'+bytes(21)
    for i in range(count):p[start+32*i:start+32*(i+1)]=raw_record(199,creature=7 if i%3 else 0,party=0xA5,flags=i&7,level=0,gold=300,health=300)[8:]
    return struct.pack('<II',201,size)+p

def compare_code(actual,expected,n,capture=None):
    code=b'\xBE'+pack32(actual)+b'\xBF'+pack32(expected)+b'\xB9'+pack32(n)+bytes.fromhex('f3a6b8000000000f94c0')
    if capture:code+=bytes.fromhex('a3')+pack32(capture)+bytes.fromhex('890d')+pack32(capture+4)
    return code

def cases():
    rows=[]
    for program in ['game','editor']:
        for size in [264,268,300,555]:rows.append(dict(name=f'parser-{program}-{size}',kind='parser',program=program,size=size,opaque=165))
        for opaque in [0,255]:rows.append(dict(name=f'parser-{program}-opaque-{opaque}',kind='parser',program=program,size=555,opaque=opaque))
        rows.append(dict(name=f'parser-{program}-fallback-length',kind='parser',program=program,size=555,opaque=165,declared=556))
        rows.append(dict(name=f'parser-{program}-io-failure',kind='parser',program=program,size=555,opaque=165,fail=True))
    for party in [0,23,24,255]:rows.append(dict(name=f'world-{party}',kind='world',party=party))
    for party in [0,23,255]:
        for flags in [0,2,4,6]:
            for prior in [0,1]:rows.append(dict(name=f'member-{party}-{flags}-{prior}',kind='member',party=party,flags=flags,prior=prior))
    for seed in [0,165,255]:
        for occupied in [False,True]:rows.append(dict(name=f'writer-{seed}-{int(occupied)}',kind='writer',seed=seed,occupied=occupied))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-DX.exe'
    if c['kind']=='parser':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x570210 if c['program']=='game' else 0x430A70;raw=fixture_record(c['size'],c['opaque']);tail=b'TailSeed\0'+bytes(23);expected_bytes=loader_intermediate(raw,tail)
        status=0xFFFFFFFF if c.get('fail') else 0
        io=bytes.fromhex('5657be')+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10003008)+bytes.fromhex('f3a48b44240cc700')+pack32(status)+bytes.fromhex('8b54241c8b0d')+pack32(0x10003008)+bytes.fromhex('890a5f5ec3')
        callback=bytes.fromhex('56578b74240cbf')+pack32(0x10001000)+b'\xB9'+pack32(631)+bytes.fromhex('f3a6b8000000000f94c0a3')+pack32(0x10003000)+bytes.fromhex('890d')+pack32(0x10003004)+bytes.fromhex('5f5eb801000000c3')
        dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0x94,0x10004000)
        readout=bytes.fromhex('89c2')+load_abs(0,0x10003000)+load_abs(3,0x10003008)+load_abs(1,0x10003004)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,raw[8:]),region(0x10001000,expected_bytes),region(0x10002000,dispatch),region(0x10003000,bytes(12)),region(0x10004000,callback),region(0x10005000,struct.pack('<II',201,c.get('declared',c['size']))),region(0x10006000,pack32(1)),region(0x10007000,readout),region(0x5B3EB0 if c['program']=='game' else 0x4B7F10,io),region(0x7FFF0000-(0xFC if c['program']=='game' else 0x374),tail),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10005000)+pack32(0x10006000)+pack32(0x10002000))]
        if c['program']=='editor' and c.get('fail'):mem.append(region(0x4E4D4A,return_const(0,12)))
        expected=dict(EAX=int(not c.get('fail')),EBX=c['size'],ECX=0,EDX=int(not c.get('fail')))
    elif c['kind']=='world':
        entry=0x5212A0;raw=fixture_record(party=c['party']);mid=loader_intermediate(raw);seed=bytes([0xCC])*520;projection=world_store_projection(seed,raw);world=bytearray(0x4030+256*520+16);world[:]=bytes([0xCC])*len(world);target=0x10010000+projection['world_offset']
        readout=compare_code(target,0x10002000,520)+load_abs(3,0x10003000)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        checksum=bytes.fromhex('ff05')+pack32(0x10003000)+bytes.fromhex('c20800')
        mem=[region(0x10000000,mid),region(0x10002000,projection['slot']),region(0x10003000,bytes(4)),region(0x10004000,readout),region(0x10010000,world),region(0x5108C0,checksum),region(0x7FFF0000,pack32(0x10004000)+pack32(0x10000000)+pack32(0x10010000))];expected=dict(EAX=1,EBX=1,ECX=0)
    elif c['kind']=='member':
        program='DK2 Editor pro.exe';entry=0x491A40;raw=fixture_record(party=c['party']);p=bytearray(raw[8:]);p[43:75]=raw_record(199,flags=c['flags'],party=0xA5,level=0,gold=300,health=300,objective=0)[8:];raw=raw[:8]+p;mid=loader_intermediate(raw);model=bytearray(96);struct.pack_into('<I',model,0,0x10005000);model[24]=7
        party=bytearray(96);vtable=bytearray(0x30);struct.pack_into('<I',vtable,0x2C,0x10006000);mapmodel=bytearray(0x68);struct.pack_into('<I',mapmodel,0x60,3);stack=bytearray(0xA0);struct.pack_into('<I',stack,0x10,0x10002000);struct.pack_into('<I',stack,0x94,0x10003000);stack[0x5C:0x7C]=p[43:75]
        if c['prior']:struct.pack_into('<I',party,0x20,0x10007000)
        fields=editor_member_fields(raw)[0]
        if c['flags']&6==0:fields['flags']=2 if not c['prior'] else 4
        want=bytearray(model);struct.pack_into('<H',want,0x10,fields['trigger_id']);want[0x19:0x1C]=bytes([fields['level'],fields['health'],fields['gold']]);want[0x20:0x25]=bytes([fields['wander'],fields['party_id'],fields['objective'],fields['target_player'],fields['ap']])
        for i in range(8):struct.pack_into('<I',want,0x28+4*i,int(bool(fields['flags']&(1<<i))))
        for i in range(4):struct.pack_into('<I',want,0x48+4*i,int(bool(fields['flags2']&(1<<i))))
        trigger=bytes.fromhex('668b44240466894110c20400')
        readout=compare_code(0x10001000,0x10004000,96)+load_abs(3,0x10002020+4*c['prior'])+load_abs(2,0x10001010)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,mid),region(0x10001000,model),region(0x10002000,party),region(0x10003000,mapmodel),region(0x10004000,want),region(0x10005000,vtable),region(0x10006000,trigger),region(0x10007000,bytes(96)),region(0x7FFF0000,stack),region(entry,jump(entry,0x491C4A)),region(0x491D87,readout)];regs=dict(EBP='0x10000000',ESI='0x10001000',EBX='0x0');expected=dict(EAX=1,EBX=0x10001000,ECX=0,EDX=0x1234)
    else:
        program='DK2 Editor pro.exe';entry=0x4947C0;mapmodel=bytearray(0xC00);party=bytearray(96);child=bytearray(96);definition=bytearray(0x1900);filevt=bytearray(0x40);childvt=bytearray(0x30);partyvt=bytearray(0x30)
        struct.pack_into('<I',mapmodel,0xA8C+3*4,0x10002000);struct.pack_into('<I',party,0,0x10009000);party[0x1C]=3;struct.pack_into('<I',partyvt,0x28,0x10009100)
        for i in range(16):struct.pack_into('<I',party,0x20+4*i,0x10003000 if c['occupied'] else 0)
        struct.pack_into('<I',child,0,0x10008000);child[0x18:0x1C]=bytes([7,1,100,50]);child[0x21:0x25]=bytes([3,0,2,0xA5]);definition[0x168]=7
        for off,va in [(0x14,0x10009200),(0x18,0x10009210),(0x28,0x10009100)]:struct.pack_into('<I',childvt,off,va)
        struct.pack_into('<I',filevt,0x38,0x10009300)
        # Capture only the555-byte party payload; earlier header writes remain readonly.
        body=bytes.fromhex('ff05')+pack32(0x1000A000)+bytes.fromhex('56578b74240cbf')+pack32(0x1000B000)+b'\xB9'+pack32(555)+bytes.fromhex('f3a45f5ec20800')
        capture=bytes.fromhex('817c24082b02000075')+bytes([len(body)])+body+bytes.fromhex('c20800')
        member=struct.pack('<3IHBBIIH6B',3,4,0,50,1,0,0xA5,100,0x1234,2,19,7,0,3,0)
        want=writer_overlay(bytes([c['seed']])*555,3,0x1234,[member if c['occupied'] else None]*16)
        readout=bytes.fromhex('0fb6d0')+compare_code(0x1000B000,0x1000C000,555)+load_abs(3,0x1000A000)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,mapmodel),region(0x10002000,party),region(0x10003000,child),region(0x10010000,definition),region(0x10008000,childvt),region(0x10009000,partyvt),region(0x10009100,return_const(0x1234)),region(0x10009200,return_const(3)),region(0x10009210,return_const(4)),region(0x10009300,capture),region(0x1000A000,bytes(4)),region(0x1000B000,bytes(555)),region(0x1000C000,want),region(0x1000D000,pack32(0x1000E000)),region(0x1000E000,filevt),region(0x1000F000,readout),region(0x54F9D0,pack32(0x1000FF00)+pack32(1)),region(0x1000FF00,pack32(0x10010000)),region(0x7FFEFC00,bytes([c['seed']])*0x400),region(0x7FFF0000,pack32(0x1000F000)+pack32(0x1000D000))]
        keep={0x41CE20,0x41D170,0x41DE60,0x421190,0x48DDE0,0x48DE00,0x4952F0}
        for va,cleanup in cleanups().items():
            if va not in keep:mem.append(region(va,return_const(0,cleanup)))
        regs=dict(ECX='0x10000000');expected=dict(EAX=1,EBX=1,ECX=0,EDX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])), 'Overlapping fixture regions'
    return dict(program=program,address=hex(entry),max_steps=15000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,expected=build_case(c);p=ROOT/'ghidra'/('phase32-emulation-party-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=expected,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r)
                assert all(int(r['registers'][reg],16)==v for reg,v in expected.items()),(c['name'],r,expected)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase32-party-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
