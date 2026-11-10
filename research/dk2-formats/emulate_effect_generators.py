"""Unchanged CB parsers/converter, callbacks, tick, bounds and Things writer.

I/O, allocation, world registration, clock/random providers and creation are
explicit fixtures. Full byte comparisons use REPE CMPSB. No EXE/database byte patches.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from party_writer_stack_audit import cleanups
from native_effect_generators import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def raw_record(seed=165,x=3,y=4,width=6,height=9,frequency=7):
    return struct.pack('<II4I6H2B',203,36,x,y,0x76543210,0x80000001,width,height,21,0,65535,30,frequency,255)+bytes([seed])*6

def cases():
    rows=[]
    for p in ['game','editor']:
        for declared,fail in [(36,False),(35,False),(37,False),(36,True)]:rows.append(dict(name=f'parser-{p}-{declared}-{fail}',kind='parser',program=p,declared=declared,fail=fail))
    for seed in [0,165,255]:rows.append(dict(name=f'converter-{seed}',kind='converter',seed=seed))
    for seed in [0,165,255]:rows.append(dict(name=f'loader-{seed}',kind='loader',seed=seed))
    rows.append(dict(name='loader-disabled',kind='loader',seed=165,disabled=True))
    for seed in [0,165]:
        for xy in [(3,4),(9,14),(10,15)]:rows.append(dict(name=f'editor-load-{seed}-{xy[0]}',kind='editor_load',seed=seed,x=xy[0],y=xy[1]))
    base=dict(active=1,deadline=100,position=[0xFFFFF800,0x4800,0],width=2,height=3,effects=[21,0,65535,30],frequency=7)
    variations=[dict(name='inactive',active=0),dict(name='early',now=99),dict(name='equal',now=100),dict(name='empty',random=[1]),dict(name='rejected',rejected=True),dict(name='normal'),dict(name='create-failed',create_result=0),dict(name='slot2',random=[2,8191,12287,2047]),dict(name='slot3',random=[3,0,0,0]),dict(name='zero-frequency',frequency=0),dict(name='frequency255',frequency=255),dict(name='overflow',now=0xFFFFFFF0,tps=0xFFFFFFFF),dict(name='zero-extents',width=0,height=0,random=[0,0,0,0]),dict(name='max-extents',width=65535,height=65535,random=[0,(65535<<12)-1,(65535<<12)-1,2047])]
    for change in variations:
        c=dict(kind='tick',now=101,tps=20,random=[0,4095,4096,123],rejected=False,create_result=1,**base);c.update(change);c['name']='tick-'+c['name'];rows.append(c)
    for axis in ['width','height']:
        for origin,dim,wanted in [(3,10,6),(3,10,7),(3,10,65535),(9,10,1),(10,10,1),(0xFFFFFFFF,10,1),(0x80000000,10,65535),(0,65536,1)]:rows.append(dict(name=f'extent-{axis}-{origin}-{dim}-{wanted}',kind='extent',axis=axis,origin=origin,dim=dim,wanted=wanted))
    for bound,seed in [(0,123),(1,0),(4,0),(8192,0xFFFFFFFF),(2048,12345)]:rows.append(dict(name=f'rng-{bound}-{seed}',kind='rng',bound=bound,seed=seed))
    for seed in [0,165,255]:
        for frequency in [0,255]:rows.append(dict(name=f'writer-{seed}-{frequency}',kind='writer',seed=seed,frequency=frequency))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-DX.exe';entry=0
    if c['kind']=='parser':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x570630 if c['program']=='game' else 0x430DB0
        raw=raw_record();dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0x9C,0x10004000)
        io=bytes.fromhex('5657be')+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10003008)+bytes.fromhex('f3a48b44240cc700')+pack32(0xFFFFFFFF if c['fail'] else 0)+bytes.fromhex('8b54241c8b0d')+pack32(0x10003008)+bytes.fromhex('890a5f5ec3')
        cb=bytes.fromhex('56578b74240cbf')+pack32(0x10001000)+b'\xB9'+pack32(76)+bytes.fromhex('f3a6b8000000000f94c0a3')+pack32(0x10003000)+bytes.fromhex('890d')+pack32(0x10003004)+bytes.fromhex('5f5eb801000000c3')
        readout=bytes.fromhex('89c2')+load_abs(0,0x10003000)+load_abs(3,0x10003008)+load_abs(1,0x10003004)+END
        mem=[region(0x10000000,raw[8:]),region(0x10001000,intermediate(raw)),region(0x10002000,dispatch),region(0x10003000,bytes(12)),region(0x10004000,cb),region(0x10005000,struct.pack('<II',203,c['declared'])),region(0x10006000,pack32(1)),region(0x10007000,readout),region(0x5B3EB0 if c['program']=='game' else 0x4B7F10,io),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10005000)+pack32(0x10006000)+pack32(0x10002000))]
        if c['program']=='editor' and c['fail']:mem.append(region(0x4E4D4A,return_const(0,12)))
        expected=dict(EAX=int(not c['fail']),EBX=36,ECX=0,EDX=int(not c['fail']))
    elif c['kind']=='converter':
        program='DK2 Editor pro.exe';entry=0x42FB50;raw=raw_record();seed=bytes([c['seed']])*76
        readout=compare_code(0x10001000,0x10002000,76)+END
        mem=[region(0x10000000,raw[8:]),region(0x10001000,seed),region(0x10002000,intermediate(raw,seed)),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');expected=dict(EAX=1,ECX=0)
    elif c['kind']=='loader':
        entry=0x521380;raw=raw_record();node=bytearray([c['seed']]*69);want=bytearray(node);v=runtime_projection(raw);world=bytearray(0x100)
        if not c.get('disabled'):
            struct.pack_into('<I',want,0,0x66D814)
            for o in range(6,18,2):struct.pack_into('<H',want,o,0)
            struct.pack_into('<3I',want,0x1A,*v['position']);struct.pack_into('<I',want,0x2E,0);struct.pack_into('<4H',want,0x32,*v['effects']);want[0x3A]=v['frequency'];want[0x3B]=0;struct.pack_into('<I2HB',want,0x3C,1,v['width'],v['height'],v['id'])
        position=bytes.fromhex('56578b74240c8d791ab903000000f3a55f5ec20400')
        checksum=bytes.fromhex('ff05')+pack32(0x10004000)+bytes.fromhex('c20800')
        readout=bytes.fromhex('89c2')+compare_code(0x10001000,0x10002000,69)+load_abs(3,0x10003042)+load_abs(5,0x10004000)+END
        mem=[region(0x10000000,intermediate(raw)),region(0x10001000,node),region(0x10002000,want),region(0x10003000,world),region(0x10004000,bytes(4)),region(0x10005000,readout),region(0x6A4818,pack32(int(not c.get('disabled')))),region(0x554B30,return_const(0x10001000,4)),region(0x507850,b'\xC3'),region(0x4B4A10,return_const(1,4)),region(0x4B5560,position),region(0x5108C0,checksum),region(0x7FFF0000,pack32(0x10005000)+pack32(0x10000000)+pack32(0x10003000))];expected=dict(EAX=1,ECX=0,EBX=int(not c.get('disabled')),EDX=int(not c.get('disabled')),EBP=1)
    elif c['kind']=='editor_load':
        program='DK2 Editor pro.exe';entry=0x491F20;raw=raw_record(x=c['x'],y=c['y'],width=65535,height=65535);v=record_view(raw);node=bytearray([c['seed']]*48);want=bytearray(node);world=bytearray(0x9A0);struct.pack_into('<2I',world,0x990,10,15)
        vt=bytearray(0x28)
        for o,va in [(0x20,0x10006000),(0x24,0x10006020)]:struct.pack_into('<I',vt,o,va)
        struct.pack_into('<I',want,0,0x50AEB8);struct.pack_into('<2I',want,4,c['x'],c['y']);want[0x13]=0;want[0x18]=v['id'];struct.pack_into('<I',want,0x1C,0);want[0x20]=v['frequency'];struct.pack_into('<6H',want,0x22,editor_extent(c['x'],10,v['width']),editor_extent(c['y'],15,v['height']),*v['effects'])
        readout=bytes.fromhex('89c2')+compare_code(0x10001000,0x10002000,48)+load_abs(3,0x10004000)+END
        mem=[region(0x10000000,intermediate(raw)),region(0x10001000,node),region(0x10002000,want),region(0x10003000,world),region(0x10004000,bytes(4)),region(0x10005000,readout),region(0x10006000,bytes.fromhex('8b442404894104c20400')),region(0x10006020,bytes.fromhex('8b442404894108c20400')),region(0x50AEB8,vt),region(0x4D8A63,return_const(0x10001000)),region(0x41C5A0,return_const(1,4)),region(0x41C670,return_const(0x10003000)),region(0x48AD20,bytes.fromhex('ff05')+pack32(0x10004000)+bytes.fromhex('c20400')),region(0x7FFF0000,pack32(0x10005000)+pack32(0x10000000)+pack32(0x10003000))];expected=dict(EAX=1,ECX=0,EBX=1,EDX=1)
    elif c['kind']=='tick':
        entry=0x4B1B60;node=bytearray([165]*69);struct.pack_into('<I',node,0,0x66D814);struct.pack_into('<3I',node,0x1A,*c['position']);struct.pack_into('<I4H',node,0x2E,c['deadline'],*c['effects']);node[0x3A]=c['frequency'];struct.pack_into('<I2H',node,0x3C,c['active'],c['width'],c['height']);want=bytearray(node)
        v=tick_projection(c,c['now'],c['tps'],c['random'],c['rejected']);struct.pack_into('<I',want,0x2E,v['deadline'])
        world=bytearray(0x1600);struct.pack_into('<I',world,0,0x10004000);vt=bytearray(0x390);struct.pack_into('<I',vt,0x20C,0x10005100);struct.pack_into('<I',vt,0x38C,0x10005200)
        # Log layout: clockCalls,rngCalls,bounds[4],positionCalls,createCalls,
        # accepted position[3],created position[3],effect/owner/angle eachDWORD.
        log=bytearray(68);expected_log=bytearray(log);struct.pack_into('<II',expected_log,0,v['clock_calls'],len(v['random_bounds']))
        for i,b in enumerate(v['random_bounds']):struct.pack_into('<I',expected_log,8+4*i,b)
        if v['position'] is not None:struct.pack_into('<I3I',expected_log,24,1,*([0,0,0]));struct.pack_into('<3I',expected_log,32,*v['position'])
        if v['effect'] is not None:struct.pack_into('<I',expected_log,28,1);struct.pack_into('<3I3I',expected_log,44,*v['position'],v['effect'],0x1234,v['angle'])
        clock=bytes.fromhex('ff05')+pack32(0x10008000)+return_const(c['now'])
        rng=bytes.fromhex('8b15')+pack32(0x10008004)+bytes.fromhex('8b442404890495')+pack32(0x10008008)+bytes.fromhex('8b0495')+pack32(0x10009000)+bytes.fromhex('ff05')+pack32(0x10008004)+b'\xC3'
        pos=bytes.fromhex('5657ff05')+pack32(0x10008018)+bytes.fromhex('8b74240cbf')+pack32(0x10008020)+b'\xB9'+pack32(3)+bytes.fromhex('f3a55f5e')+return_const(int(c['rejected']),4)
        create=bytes.fromhex('5657ff05')+pack32(0x1000801C)+bytes.fromhex('8b742414bf')+pack32(0x1000802C)+b'\xB9'+pack32(3)+bytes.fromhex('f3a50fb744240ca3')+pack32(0x10008038)+bytes.fromhex('0fb7442410a3')+pack32(0x1000803C)+bytes.fromhex('8b4424180fb700a3')+pack32(0x10008040)+bytes.fromhex('5f5e')+return_const(c['create_result'],20)
        readout=compare_code(0x10000000,0x10001000,69,0x1000A000)+compare_code(0x10008000,0x1000B000,68)+load_abs(3,0x1000A000)+load_abs(2,0x1000A004)+END
        mem=[region(0x10000000,node),region(0x10001000,want),region(0x10002000,world),region(0x10004000,vt),region(0x10005100,pos),region(0x10005200,create),region(0x10006000,readout),region(0x10008000,log),region(0x10009000,b''.join(pack32(x) for x in c['random'])),region(0x1000A000,bytes(8)),region(0x1000B000,expected_log),region(0x517490,clock),region(0x5558D0,rng),region(0x6E5050,pack32(0x10002000)),region(0x6F2548,struct.pack('<H',0x1234)),region(0x6F2590,pack32(c['tps'])),region(0x7FFF0000,pack32(0x10006000))];regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0,EBX=1,EDX=0)
    elif c['kind']=='extent':
        program='DK2 Editor pro.exe';entry=0x41F990 if c['axis']=='width' else 0x41F9E0;node=bytearray([165]*48);offset=0x22 if c['axis']=='width' else 0x24;struct.pack_into('<I',node,4 if c['axis']=='width' else 8,c['origin']);world=bytearray(0x9A0);struct.pack_into('<I',world,0x990 if c['axis']=='width' else 0x994,c['dim']);want=bytearray(node);struct.pack_into('<H',want,offset,editor_extent(c['origin'],c['dim'],c['wanted']));readout=compare_code(0x10000000,0x10001000,48)+END
        mem=[region(0x10000000,node),region(0x10001000,want),region(0x10002000,world),region(0x10003000,readout),region(0x41C670,return_const(0x10002000)),region(0x7FFF0000,pack32(0x10003000)+pack32(c['wanted']))];regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0)
    elif c['kind']=='rng':
        entry=0x5558D0;v,seed=random_step(c['bound'],c['seed']);logger=bytes.fromhex('ff05')+pack32(0x10002000)+b'\xC3';readout=load_abs(3,0x10000000)+load_abs(1,0x10002000)+END
        mem=[region(0x10000000,pack32(c['seed'])),region(0x10001000,readout),region(0x10002000,bytes(4)),region(0x75B2F4,pack32(0)),region(0x634DB0,logger),region(0x7FFF0000,pack32(0x10001000)+pack32(c['bound'])+pack32(0x10000000)+pack32(0)+pack32(0))];expected=dict(EAX=v,EBX=seed,ECX=int(c['bound']==0))
    else:
        program='DK2 Editor pro.exe';entry=0x4947C0;world=bytearray(0xC00);struct.pack_into('<2I',world,0xBB0,0x10001000,1);model=bytearray(48);struct.pack_into('<I',model,0,0x10003000);model[0x18]=255;struct.pack_into('<I',model,0x1C,0xFFFFFFFF);model[0x20]=c['frequency'];struct.pack_into('<6H',model,0x22,65535,7,21,0,65535,30);vt=bytearray(0x30);struct.pack_into('<I',vt,0x14,0x10004000);struct.pack_into('<I',vt,0x18,0x10004020);filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x10005000)
        wanted=writer_payload(dict(x=3,y=4,width=65535,height=7,effects=[21,0,65535,30],frequency=c['frequency'],id=255),bytes([c['seed']])*6)
        body=bytes.fromhex('ff05')+pack32(0x10006000)+bytes.fromhex('56578b74240cbf')+pack32(0x10007000)+b'\xB9'+pack32(36)+bytes.fromhex('f3a45f5ec20800');capture=bytes.fromhex('817c24082400000075')+bytes([len(body)])+body+bytes.fromhex('c20800')
        readout=bytes.fromhex('0fb6d0')+compare_code(0x10007000,0x10008000,36)+load_abs(3,0x10006000)+END
        mem=[region(0x10000000,world),region(0x10001000,pack32(0x10002000)),region(0x10002000,model),region(0x10003000,vt),region(0x10004000,return_const(3)),region(0x10004020,return_const(4)),region(0x10005000,capture),region(0x10006000,bytes(4)),region(0x10007000,bytes(36)),region(0x10008000,wanted),region(0x10009000,pack32(0x1000A000)),region(0x1000A000,filevt),region(0x1000B000,readout),region(0x7FFEFC00,bytes([c['seed']])*0x400),region(0x7FFF0000,pack32(0x1000B000)+pack32(0x10009000))]
        for va,cleanup in cleanups().items():
            if va not in {0x48AD00,0x41FA30,0x41FA70}:mem.append(region(va,return_const(0,cleanup)))
        # A36-byte header write precedes the final36-byte CB payload write.
        regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0,EBX=2,EDX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=20000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase37-emulation-generator-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase37-generator-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
