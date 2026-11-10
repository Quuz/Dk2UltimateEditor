"""CD parsers/converters, full preset callback and selected active controls.

Controlled I/O, camera provider, checksum, matrix rebuild and seek fixtures.
Editor registration fragment and actual zero initializer test camera callback0.
No modified game process or rendered camera view.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_cameras import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def raw_record(size=84,ident=1,opaque=165,flags=0x800000F9,zoom_min=3000,zoom_max=12000):
    values=[(0x1234567*(i+1))&0xFFFFFFFF for i in range(18)];values[13]=zoom_min;values[14]=zoom_max
    p=struct.pack('<19I3H2B',*values,flags,0x1234,0x5678,0x9ABC,ident,opaque)
    return struct.pack('<II',205,size)+p[:size]

def cases():
    rows=[]
    for program in ['game','editor']:
        for size in [0,48,72,76,82,83,84]:rows.append(dict(name=f'parser-{program}-{size}',kind='parser',program=program,size=size,fail=False))
        rows.append(dict(name=f'parser-{program}-fail',kind='parser',program=program,size=84,fail=True))
        for seed in [0,165,255]:rows.append(dict(name=f'converter-{program}-{seed}',kind='converter',program=program,seed=seed))
        for length in [0,84,255]:rows.append(dict(name=f'skip-{program}-{length}',kind='skip',program=program,length=length))
    for ident in [0,1,18,19,255]:
        for setting in [4,5]:rows.append(dict(name=f'loader-{ident}-{setting}',kind='loader',id=ident,setting=setting,opaque=165))
    for opaque in [0,255]:rows.append(dict(name=f'loader-tail-{opaque}',kind='loader',id=1,setting=5,opaque=opaque))
    for lo,hi in [(12000,3000),(0xFFFFFFFF,0),(0x80000000,0x7FFFFFFF)]:rows.append(dict(name=f'loader-bounds-{lo}-{hi}',kind='loader',id=1,setting=4,opaque=165,zoom_min=lo,zoom_max=hi))
    for field in ['zoom','view','lens']:
        mask={'zoom':8,'view':16,'lens':32}[field]
        for value in [0xFFFFFF9C,50,500]:
            for flags in [0,mask]:rows.append(dict(name=f'delta-{field}-{value}-{flags}',kind='delta',field=field,delta=value,flags=flags,busy=0))
        rows.append(dict(name=f'delta-{field}-busy',kind='delta',field=field,delta=50,flags=0,busy=1))
    for field in ['view','lens']:
        for value in [0,2000]:rows.append(dict(name=f'set-{field}-{value}',kind='scalar_set',field=field,value=value,flags=0xFFFFFFFF))
    for relative in [False,True]:
        for flags,busy in [(0,0),(64,0),(0,1)]:rows.append(dict(name=f'position-{relative}-{flags}-{busy}',kind='position',relative=relative,flags=flags,busy=busy))
    for flags,busy in [(0,0),(1,0),(2,0),(4,0),(7,0),(0xFFFFFFFF,0),(0,1)]:rows.append(dict(name=f'angles-{flags}-{busy}',kind='angles',flags=flags,busy=busy))
    for seed in [0,165,255]:rows.append(dict(name=f'editor-registration-{seed}',kind='registration',seed=seed))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-DX.exe'
    if c['kind']=='parser':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x570A20 if c['program']=='game' else 0x431220;raw=raw_record(c['size']);dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0xA4 if c['program']=='game' else 0xA8,0x10004000)
        io=bytes.fromhex('5657be')+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10003008)+bytes.fromhex('f3a48b44240cc700')+pack32(0xFFFFFFFF if c['fail'] else 0)+bytes.fromhex('8b54241c8b0d')+pack32(0x10003008)+bytes.fromhex('890a5f5ec3');cb=bytes.fromhex('56578b74240cbf')+pack32(0x10001000)+b'\xB9'+pack32(152)+bytes.fromhex('f3a6b8000000000f94c0a3')+pack32(0x10003000)+bytes.fromhex('890d')+pack32(0x10003004)+bytes.fromhex('5f5eb801000000c3');readout=bytes.fromhex('89c2')+load_abs(0,0x10003000)+load_abs(3,0x10003008)+load_abs(1,0x10003004)+END
        mem=[region(0x10000000,payload(raw)),region(0x10001000,intermediate(raw)),region(0x10002000,dispatch),region(0x10003000,bytes(12)),region(0x10004000,cb),region(0x10005000,struct.pack('<II',205,c['size'])),region(0x10006000,pack32(1)),region(0x10007000,readout),region(0x5B3EB0 if c['program']=='game' else 0x4B7F10,io),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10005000)+pack32(0x10006000)+pack32(0x10002000))]
        if c['program']=='editor' and c['fail']:mem.append(region(0x4E4D4A,return_const(0,12)))
        expected=dict(EAX=int(not c['fail']),ECX=0,EBX=c['size'],EDX=int(not c['fail']))
    elif c['kind']=='converter':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x56F900 if c['program']=='game' else 0x42FE20;raw=raw_record();seed=bytes([c['seed']])*152;readout=compare_code(0x10001000,0x10002000,152)+END
        mem=[region(0x10000000,payload(raw)),region(0x10001000,seed),region(0x10002000,intermediate(raw,seed)),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');expected=dict(EAX=1,ECX=0)
    elif c['kind']=='skip':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x570A20 if c['program']=='game' else 0x431220;seek=bytes.fromhex('8b442408a3')+pack32(0x10002000)+bytes.fromhex('83c064c3');readout=load_abs(3,0x10002000)+END
        mem=[region(0x10000000,bytes(0xB4)),region(0x10001000,struct.pack('<II',205,c['length'])),region(0x10002000,bytes(4)),region(0x10003000,pack32(1)),region(0x10004000,readout),region(0x5B3A80 if c['program']=='game' else 0x4B7E70,return_const(100)),region(0x5B3A90 if c['program']=='game' else 0x4B7E80,seek),region(0x7FFF0000,pack32(0x10004000)+pack32(0x10001000)+pack32(0x10003000)+pack32(0x10000000))];expected=dict(EAX=1,EBX=c['length'])
    elif c['kind']=='loader':
        entry=0x5215F0;raw=raw_record(ident=c['id'],opaque=c['opaque'],zoom_min=c.get('zoom_min',3000),zoom_max=c.get('zoom_max',12000));mid=intermediate(raw);state=bytes([165])*0xC00;want=bytearray(state);v=preset_projection(raw,c['setting'])
        if v['accepted']:want[v['slot_offset']:v['slot_offset']+152]=v['slot']
        world=bytearray(8);struct.pack_into('<I',world,4,0x10007000);child=bytearray(0x14);struct.pack_into('<I',child,0x10,0x10008000);vt=bytearray(0xD4);struct.pack_into('<I',vt,0xD0,0x1000A000);checksum=bytes.fromhex('ff05')+pack32(0x1000B000)+bytes.fromhex('c20800');readout=bytes.fromhex('89c2')+compare_code(0x10001000,0x10003000,0xC00)+load_abs(3,0x1000B000)+END
        mem=[region(0x10000000,mid),region(0x10001000,state),region(0x10003000,want),region(0x10005000,world),region(0x10006000,readout),region(0x10007000,child),region(0x10008000,pack32(0x10009000)),region(0x10009000,vt),region(0x1000A000,return_const(0x10001000)),region(0x1000B000,bytes(4)),region(0x5108C0,checksum),region(0x759AEC,pack32(c['setting'])),region(0x7FFF0000,pack32(0x10006000)+pack32(0x10000000)+pack32(0x10005000))];expected=dict(EAX=1,ECX=0,EDX=1,EBX=1)
    elif c['kind']=='registration':
        program='DK2 Editor pro.exe';entry=0x48B5F0;context=0x7FFF0028;adapter=bytes.fromhex('b9')+pack32(context)+b'\xE8'+struct.pack('<i',0x42C730-(0x10000000+10))+bytes.fromhex('be')+pack32(0x10001000)+jump(0x10000000+15,0x48B703)
        # Last registration phase: context starts at frameSP+28. No camera
        # callback is assigned at A8; A4 is also untouched. RoomA0 is populated.
        readout=load_abs(0,context+0xA8)+load_abs(3,context+0xA4)+load_abs(2,context+0xA0)+END
        mem=[region(entry,jump(entry,0x10000000)),region(0x10000000,adapter),region(0x10001000,bytes(0xC00)),region(0x48B84E,readout),region(0x7FFF0000,bytes([c['seed']])*0x180)];expected=dict(EAX=0,EBX=0,EDX=0x492030)
    else:return controls_case(c)
    return finalize(c,program,entry,mem,regs,expected)

def finalize(c,program,entry,mem,regs,expected):
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

def controls_case(c):
    state=bytearray([165]*0xF00);want=bytearray(state);args=[];mem=[];counter=0
    if c['kind'] in ['delta','scalar_set']:
        field=c['field'];config={'zoom':(0x449CA0,0xD80,0xD50,0xD54),'view':(0x449FC0,0xD8A,0xD60,0xD64),'lens':(0x44A070,0xD8E,0xD58,0xD5C)};entry,off,lo,hi=config[field];struct.pack_into('<I',state,off,150);struct.pack_into('<I',state,lo,100);struct.pack_into('<I',state,hi,200);struct.pack_into('<I',state,0xD92,c['flags']);struct.pack_into('<I',state,0xDD8,c.get('busy',0));want=bytearray(state)
        if c['kind']=='delta':value=active_delta(field,150,c['delta'],100,200,c['flags'],c['busy']);arg=c['delta'] if field!='lens' else 0x10003000
        else:
            entry=0x449F70 if field=='view' else 0x44A020;value=clamp_exclusive(c['value'],100,200);arg=c['value'] if field=='view' else 0x10003000
        struct.pack_into('<I',want,off,value);args=[arg]
        if field=='lens':mem.append(region(0x10003000,pack32(c.get('delta',c.get('value')))))
    elif c['kind']=='position':
        entry=0x449BD0 if c['relative'] else 0x449AC0;struct.pack_into('<3I',state,0xD38,100,200,300);struct.pack_into('<3I',state,0xD44,1000,2000,3000);struct.pack_into('<3I',state,0xD68,500,1000,1500);struct.pack_into('<I',state,0xD92,c['flags']);struct.pack_into('<I',state,0xDD8,c['busy']);wanted=[500,1000,1500];vector=[0xFFFFFF00,5000,0]
        if not c['flags']&64 and not c['busy']:
            values=[(a+b)&0xFFFFFFFF for a,b in zip(wanted,vector)] if c['relative'] else vector;wanted=[clamp_exclusive(v,lo,hi) for v,lo,hi in zip(values,[100,200,300],[1000,2000,3000])]
        want=bytearray(state);struct.pack_into('<3I',want,0xD68,*wanted);args=[0x10003000];mem.append(region(0x10003000,struct.pack('<3I',*vector)))
    else:
        entry=0x449D40;struct.pack_into('<3H',state,0xD84,2040,100,200);struct.pack_into('<I',state,0xD92,c['flags']);struct.pack_into('<I',state,0xDD8,c['busy']);struct.pack_into('<I',state,0xDA8,1);want=bytearray(state);out=relative_angles([2040,200,100],[20,30,40],c['flags'],c['busy']);struct.pack_into('<3H',want,0xD84,out[0],out[2],out[1]);counter=int(not c['busy'] and c['flags']&7!=7);args=[0x10003000,0x10003002,0x10003004];mem.append(region(0x10003000,struct.pack('<3H',20,30,40)));mem.append(region(0x44D6D0,bytes.fromhex('ff05')+pack32(0x10004000)+b'\xC3'))
    readout=compare_code(0x10000000,0x10001000,len(state))+load_abs(3,0x10004000)+END
    mem.extend([region(0x10000000,state),region(0x10001000,want),region(0x10004000,bytes(4)),region(0x10005000,readout),region(0x7FFF0000,pack32(0x10005000)+b''.join(pack32(a) for a in args))]);return finalize(c,'DKII-DX.exe',entry,mem,dict(ECX='0x10000000'),dict(EAX=1,ECX=0,EBX=counter))

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase40-emulation-camera-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase40-camera-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
