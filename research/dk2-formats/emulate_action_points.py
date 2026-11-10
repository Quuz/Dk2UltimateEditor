"""Original AP parsers/create/init/setters, writer, next state and patrol consumer.

Allocation, I/O, terrain/nav and clock are fixtures in EmulatorHelper only.
Entire buffers compared using CMPSB. No original EXE or process modifications.
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
from native_action_points import *
ROOT=Path(__file__).resolve().parent

def raw_record(xy=(3,4,8,9),wait=10,flags=65535,trigger=0x1234,next_id=2,ident=1,size=56):
    p=struct.pack('<4IHHHBB',*xy,wait,flags,trigger,ident,next_id)+b'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef'
    return struct.pack('<II',197,size)+p[:size]

def cases():
    rows=[]
    for pr in ['game','editor']:
        for xy in [(3,4,8,9),(3,4,0,2),(0,0,0,1),(0xFFFFFFFF,0x10000,0x10001,0xFFFFFFFF)]:rows.append(dict(name=f'parser-{pr}-{len(rows)}',kind='parser',program=pr,xy=xy,size=56))
        for size in [0,16,24]:rows.append(dict(name=f'parser-{pr}-size{size}',kind='parser',program=pr,xy=(3,4,0,2),size=size))
    for flags in [0,1,2,4,8,16,32,64,128,256,0xFFFF]:
        rows.append(dict(name=f'loader-flags-{flags}',kind='loader',flags=flags,wait=32767,tps=20,xy=(0x12340003,0xFFFF0004,0x80000008,9)))
    for wait,tps in [(0,20),(1,20),(65535,20),(500,100),(32768,2),(65535,0xFFFFFFFF)]:rows.append(dict(name=f'loader-wait-{wait}-{tps}',kind='loader',flags=0x40,wait=wait,tps=tps,xy=(3,4,0,2)))
    for nxts in [[0,0],[2,0],[0,2],[2,1]]:
        for seed in [0,165]:rows.append(dict(name=f'writer-{nxts[0]}-{nxts[1]}-{seed}',kind='writer',nexts=nxts,seed=seed))
    for current,nxt,now,wait in [(0,3,1000,20),(256,3,1000,20),(1,0,1000,0),(1,2,1000,20),(1,255,0xFFFFFFF0,65535)]:rows.append(dict(name=f'next-{current}-{nxt}-{now}-{wait}',kind='next',current=current,next_id=nxt,now=now,wait=wait))
    for xy in [(3,4,3,4),(3,4,8,9),(0,0,65535,65535),(5,6,3,4)]:rows.append(dict(name=f'area-{len(rows)}',kind='area',xy=xy))
    for xy in [(3,4,3,4),(3,4,3,9),(3,4,8,4),(3,4,8,9)]:
        for last in [False,True]:rows.append(dict(name=f'patrol-{len(rows)}',kind='patrol',xy=xy,last=last,flags=0,solid=0,success=1))
    for flags in [0,64]:rows.append(dict(name=f'patrol-solid-{flags}',kind='patrol',xy=(3,4,8,9),last=True,flags=flags,solid=1,success=1))
    rows.append(dict(name='patrol-nav-failure',kind='patrol',xy=(3,4,8,9),last=False,flags=0,solid=0,success=0))
    for deadline,now in [(1000,999),(1000,1000),(1000,1001),(0xFFFFFFFF,0),(0,0xFFFFFFFF)]:rows.append(dict(name=f'wait-{deadline}-{now}',kind='wait',deadline=deadline,now=now))
    for flags in [0,2,64,128,256,65535]:rows.append(dict(name=f'editor-codec-{flags}',kind='codec',flags=flags))
    return rows

def build_case(c):
    mem=[];regs={};pr='DKII-DX.exe'
    if c['kind']=='parser':
        pr='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x5700A0 if c['program']=='game' else 0x430910;raw=raw_record(c['xy'],size=c['size']);want=intermediate(raw)
        io=bytes.fromhex('5657be')+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10003004)+bytes.fromhex('f3a48b44240cc700000000005f5ec3')
        cb=bytes.fromhex('56578b74240cbf')+pack32(0x10001000)+b'\xB9'+pack32(92)+bytes.fromhex('f3a6b8000000000f94c0a3')+pack32(0x10003000)+bytes.fromhex('5f5eb801000000c3')
        dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0x90,0x10004000)
        readout=bytes.fromhex('89c2')+load_abs(0,0x10003000)+load_abs(3,0x10003004)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,raw[8:] or b'\0'),region(0x10001000,want),region(0x10002000,dispatch),region(0x10003000,bytes(8)),region(0x10004000,cb),region(0x10005000,raw[:8]),region(0x10006000,pack32(1)),region(0x10007000,readout),region(0x5B3EB0 if c['program']=='game' else 0x4B7F10,io),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10005000)+pack32(0x10006000)+pack32(0x10002000))];expected=dict(EAX=1,EBX=c['size'],EDX=1)
    elif c['kind']=='codec':
        pr='DK2 Editor pro.exe';entry=0x41B9D0
        adapter=b'\xB9'+pack32(0x10000000)+b'\xE8'+struct.pack('<i',0x41B9B0-(0x10001005+5))+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,bytes(60)),region(0x10001000,adapter),region(0x7FFF0000,pack32(0x10001000)+pack32(500|(c['flags']<<16)))];regs=dict(ECX='0x10000000');expected=dict(EAX=500|((c['flags']&255)<<16))
    elif c['kind']=='loader':
        entry=0x5211A0;raw=raw_record(c['xy'],wait=c['wait'],flags=c['flags']);mid=intermediate(raw);node=bytes(64);want=bytearray(64);out=runtime_projection(raw,c['tps']);struct.pack_into('<H',want,0x30,out['wait_ticks']);want[0x32:0x38]=bytes([out['flags'],2,0x34,0x12,0x77,1]);struct.pack_into('<4H',want,0x38,*out['bounds_words']);trigger=bytearray(16);trigger[15]=0x77
        readout=compare_code(0x10001000,0x10002000,64)+load_abs(3,0x10003000)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,want),region(0x10003000,bytes(4)),region(0x10004000,trigger),region(0x10005000,bytes(0x60)),region(0x10006000,readout),region(0x6A4818,pack32(1)),region(0x6F2590,pack32(c['tps'])),region(0x5108C0,bytes.fromhex('ff05')+pack32(0x10003000)+bytes.fromhex('c20800')),region(0x554B30,return_const(0x10001000,4)),region(0x4B74F0,return_const(0x10001000)),region(0x4B4A10,bytes.fromhex('c20400')),region(0x518090,return_const(0x10004000,4)),region(0x7FFF0000,pack32(0x10006000)+pack32(0x10000000)+pack32(0x10005000))];expected=dict(EAX=1,EBX=1,ECX=0)
    elif c['kind']=='writer':
        pr='DK2 Editor pro.exe';entry=0x4947C0;mapmodel=bytearray(0xC00);struct.pack_into('<II',mapmodel,0xB9C,0x10001000,2);models=[];wants=[]
        for i,nxt in enumerate(c['nexts']):
            model=bytearray(60);struct.pack_into('<I',model,0,0x10004000);model[0x1C]=i+1;struct.pack_into('<4I',model,0x24,3+i,4,8+i,9);model[0x34]=nxt;struct.pack_into('<H',model,0x36,500);model[0x38]=0x40
            models.append(model);wants.append(writer_payload(dict(bounds=[3+i,4,8+i,9],wait=500,flags=0x40,trigger=0x1234,id=i+1,next_id=nxt),bytes([c['seed']])*32,c['nexts']))
        vt=bytearray(0x30);struct.pack_into('<I',vt,0x28,0x10005000);filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x10006000)
        # Dest=base+56*count, source stackarg1. PreserveEBX across file callback.
        body=bytes.fromhex('5356578b1d')+pack32(0x10007000)+bytes.fromhex('6bdb3881c3')+pack32(0x10008000)+bytes.fromhex('89df8b742410b938000000f3a4ff05')+pack32(0x10007000)+bytes.fromhex('5f5e5bc20800')
        capture=bytes.fromhex('817c24083800000075')+bytes([len(body)])+body+bytes.fromhex('c20800')
        readout=compare_code(0x10008000,0x10009000,112)+load_abs(3,0x10007000)+bytes.fromhex('0fb615')+pack32(0x10002038)+bytes.fromhex('0fb635')+pack32(0x10003038)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,mapmodel),region(0x10001000,pack32(0x10002000)+pack32(0x10003000)),region(0x10002000,models[0]),region(0x10003000,models[1]),region(0x10004000,vt),region(0x10005000,return_const(0x1234)),region(0x10006000,capture),region(0x10007000,bytes(4)),region(0x10008000,bytes(112)),region(0x10009000,b''.join(wants)),region(0x1000A000,pack32(0x1000B000)),region(0x1000B000,filevt),region(0x1000C000,readout),region(0x7FFEFC00,bytes([c['seed']])*0x400),region(0x7FFF0000,pack32(0x1000C000)+pack32(0x1000A000))]
        for va,cleanup in cleanups().items():
            if va not in {0x48A910,0x41B9B0}:mem.append(region(va,return_const(0,cleanup)))
        regs=dict(ECX='0x10000000');flags=editor_saved_flags(0x40,c['nexts']);expected=dict(EAX=1,EBX=2,ECX=0,EDX=flags,ESI=flags)
    elif c['kind']=='next':
        entry=0x472150;state=bytearray(16);creature=bytearray(0x420);world=bytearray(4);vt=bytearray(0x2CC);ap=bytearray(64)
        struct.pack_into('<II',state,8,0x10001000,0x10002000);struct.pack_into('<II',creature,0x40F,c['current'],0xA5A5A5A5);struct.pack_into('<I',world,0,0x10003000);struct.pack_into('<I',vt,0x2C8,0x10005000);ap[0x33]=c['next_id'];struct.pack_into('<H',ap,0x30,c['wait'])
        readout=bytes.fromhex('89c2')+load_abs(0,0x1000140F)+load_abs(3,0x10001413)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,state),region(0x10001000,creature),region(0x10002000,world),region(0x10003000,vt),region(0x10004000,ap),region(0x10005000,return_const(0x10004000,4)),region(0x517490,return_const(c['now'])),region(0x476D30,return_const(1,8)),region(0x10006000,readout),region(0x7FFF0000,pack32(0x10006000))];regs=dict(ECX='0x10000000');out=advance(c['current'],c['next_id'],c['now'],c['wait'],0xA5A5A5A5);expected=dict(EAX=out['objective_bits'],EBX=out['deadline'],EDX=int(out['state_requested']))
    elif c['kind']=='wait':
        entry=0x4721C0;state=bytearray(16);creature=bytearray(0x420);world=bytearray(0x1600);vt=bytearray(0x20C)
        struct.pack_into('<II',state,8,0x10001000,0x10002000);struct.pack_into('<I',creature,0x413,c['deadline']);struct.pack_into('<I',world,0,0x10004000);struct.pack_into('<I',vt,0x208,0x10005000)
        tile=bytes.fromhex('ff05')+pack32(0x10006000)+return_const(1,8)
        change=bytes.fromhex('8b442404a3')+pack32(0x10006004)+return_const(1,8)
        readout=load_abs(0,0x10006000)+load_abs(3,0x10006004)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,state),region(0x10001000,creature),region(0x10002000,world),region(0x10004000,vt),region(0x10005000,tile),region(0x10006000,bytes(8)),region(0x10007000,readout),region(0x517490,return_const(c['now'])),region(0x5558D0,return_const(0)),region(0x476D30,change),region(0x7FFF0000,pack32(0x10007000))];regs=dict(ECX='0x10000000');expired=c['now']>c['deadline'];expected=dict(EAX=0 if expired else 4,EBX=0xAD if expired else 0)
    elif c['kind']=='area':
        entry=0x4B07E0;ap=bytearray(64);struct.pack_into('<4H',ap,0x38,*c['xy']);readout=bytes.fromhex('0fb7c0')+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem=[region(0x10000000,ap),region(0x10001000,readout),region(0x7FFF0000,pack32(0x10001000)+bytes(8))];regs=dict(ECX='0x10000000');expected=dict(EAX=inclusive_area(c['xy']))
    else:
        entry=0x471EB0;state=bytearray(16);creature=bytearray(0x420);world=bytearray(0x1600);vt=bytearray(0x2CC);ap=bytearray(64);tile=bytearray(16);terrain=bytearray(0x180)
        struct.pack_into('<II',state,8,0x10001000,0x10002000);struct.pack_into('<I',creature,0x40F,1);struct.pack_into('<H',creature,0x28,1);struct.pack_into('<I',world,0,0x10004000);struct.pack_into('<I',vt,0x2C8,0x10007000);struct.pack_into('<I',vt,0x1D4,0x10007010);struct.pack_into('<4H',ap,0x38,*c['xy']);ap[0x32]=c['flags'];terrain[0x17B]=c['solid']
        goto=bytes.fromhex('ff05')+pack32(0x10008000)+bytes.fromhex('56578b74240cbf')+pack32(0x10008004)+bytes.fromhex('b903000000f3a55f5e')+return_const(c['success'],12)
        random=(bytes.fromhex('8b44240448c3') if c['last'] else bytes.fromhex('31c0c3'))
        readout=bytes.fromhex('89c5')+load_abs(0,0x10008000)+load_abs(3,0x10008004)+load_abs(1,0x10008008)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,state),region(0x10001000,creature),region(0x10002000,world),region(0x10004000,vt),region(0x10005000,ap),region(0x10006000,tile),region(0x10007000,return_const(0x10005000,4)),region(0x10007010,return_const(0x10006000,4)),region(0x10008000,bytes(16)),region(0x10009000,terrain),region(0x6DA4A8,pack32(0x10009000)),region(0x5558D0,random),region(0x453CC0,return_const(0)),region(0x471890,goto),region(0x1000A000,readout),region(0x7FFF0000,pack32(0x1000A000))];regs=dict(ECX='0x10000000')
        reject=bool(c['flags']&64 and c['solid']&1);x,y,ex,ey=c['xy'];xy=patrol_candidate(c['xy'],max(ex-x-1,0) if c['last'] else 0,max(ey-y-1,0) if c['last'] else 0);expected=dict(EAX=0 if reject else (1 if c['success'] else 4),EBX=0 if reject else xy[0],ECX=0 if reject else xy[1],EBP=int(not reject and c['success']))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=pr,address=hex(entry),max_steps=12000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase33-emulation-ap-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r)
                assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase33-ap-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
