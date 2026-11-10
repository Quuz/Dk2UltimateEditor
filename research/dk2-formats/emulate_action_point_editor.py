"""Original AP panel fill/apply/DDX, lookup and auto-ID with explicit UI fixtures.

UpdateData/modal/string helpers are stubbed. Native field assignments, setters,
DDX argument calls and original bounded AP getter execute; no actual GUI changes.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from native_action_point_editor import *
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent

def cases():
    rows=[]
    for flags in [0,1,16,32,64,0x8E,255]:rows.append(dict(name=f'fill-{flags}',kind='fill',flags=flags))
    for pattern in range(16):rows.append(dict(name=f'apply-{pattern}',kind='apply',flags=0x8E,checks=[int(bool(pattern&(1<<i))) for i in range(4)],wait=32767))
    rows.append(dict(name='apply-nonbool',kind='apply',flags=255,checks=[2,0xFFFFFFFF,1,0],wait=0xFFFF1234))
    rows.append(dict(name='DDX',kind='ddx'))
    for ids in [[],[0,81,255],[1,2,4],[*range(1,80)],[*range(1,81)]]:rows.append(dict(name=f'auto-{len(ids)}-{sum(ids)}',kind='auto',ids=ids))
    for target in [0,3,7,255]:rows.append(dict(name=f'lookup-{target}',kind='lookup',target=target))
    rows.append(dict(name='DDX-pointers',kind='ddx_pointers'))
    for index,checks in enumerate([[0,0,0,0],[1,1,1,1],[2,0xFFFFFFFF,1,0]]):
        rows.append(dict(name=f'apply-full-model-{index}',kind='apply_full',flags=255,checks=checks,wait=0xFFFF1234,index=index))
    for ids in [[7],[7,3,255],[7,7,3],[0,7,0,255]]:
        rows.append(dict(name='combo-'+ '-'.join(map(str,ids)),kind='combo',ids=ids,current=7))
        rows.append(dict(name='combo-allocated-'+ '-'.join(map(str,ids)),kind='combo_alloc',ids=ids,current=7))
    return rows

def build_case(c):
    regs={};mem=[]
    if c['kind'] in ['fill','apply','ddx','ddx_pointers','apply_full']:
        dialog=bytearray(0x90);model=bytearray((i*37+19)&255 for i in range(60)) if c['kind']=='apply_full' else bytearray(60);struct.pack_into('<I',dialog,0x78,0x10001000);struct.pack_into('<I',dialog,0x80,0x10002000);struct.pack_into('<I',dialog,0x64,c.get('index',2));struct.pack_into('<H',model,0x36,500)
        model[0x38]=c.get('flags',0);model[0x34]=0xA5
        mem=[region(0x10000000,dialog),region(0x10001000,model),region(0x10002000,bytes([0,7,255])),region(0x7FFF0000,pack32(0xDEADBEEF))]
        if c['kind']=='fill':
            entry=0x4323A0;regs=dict(ESI='0x10000000')
            readout=load_abs(0,0x10000060)+load_abs(3,0x10000068)+load_abs(1,0x1000006C)+load_abs(2,0x10000070)+load_abs(6,0x10000074)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem += [region(entry,jump(entry,0x4324B4)),region(0x4324F5,readout)];out=panel_fields(c['flags'],500);expected=dict(EAX=out['wait'],EBX=out['reveal'],ECX=out['hero_lair'],EDX=out['tool_box'],ESI=out['ignore_solid'])
        elif c['kind'] in ['apply','apply_full']:
            entry=0x432850;struct.pack_into('<I',dialog,0x60,c['wait'])
            for i,n in enumerate(c['checks']):struct.pack_into('<I',dialog,0x68+4*i,n)
            mem[0]=region(0x10000000,dialog);regs=dict(ECX='0x10000000')
            readout=bytes.fromhex('0fb605')+pack32(0x10001038)+bytes.fromhex('0fb71d')+pack32(0x10001036)+bytes.fromhex('0fb60d')+pack32(0x10001034)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem += [region(0x4DC7F3,return_const(1,4)),region(0x4D9E2E,b'\xC3'),region(0x10003000,readout)];mem[3]=region(0x7FFF0000,pack32(0x10003000));out=apply_panel(c['flags'],c['wait'],c['checks'],[0,7,255],2);expected=dict(EAX=out['flags'],EBX=out['wait_word'],ECX=out['next_id'])
            if c['kind']=='apply_full':
                out=apply_panel(c['flags'],c['wait'],c['checks'],[0,7,255],c['index']);want=bytearray(model);want[0x34]=out['next_id'];struct.pack_into('<H',want,0x36,out['wait_word']);want[0x38]=out['flags']
                readout=compare_code(0x10001000,0x10006000,len(want))+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
                mem[-1]=region(0x10003000,readout);mem.append(region(0x10006000,want));expected=dict(EAX=1,ECX=0)
        else:
            entry=0x432530;regs=dict(ECX='0x10000000')
            # Record four DDX_Check(controlID,pointer) argument pairs.
            check=bytes.fromhex('8b15')+pack32(0x10003000)+bytes.fromhex('8d14d5')+pack32(0x10003010)+bytes.fromhex('8b44240889028b44240c894204ff05')+pack32(0x10003000)+bytes.fromhex('c20c00')
            readout=load_abs(0,0x10003010)+load_abs(3,0x10003018)+load_abs(1,0x10003020)+load_abs(2,0x10003028)+load_abs(6,0x10003000)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem[3]=region(0x7FFF0000,pack32(0x10004000)+pack32(0x10005000));mem += [region(0x10003000,bytes(0x40)),region(0x10004000,readout),region(0x10005000,bytes(16)),region(0x4E490A,check),region(0x4E4A2F,bytes.fromhex('c20c00')),region(0x4E4B5D,bytes.fromhex('c21000'))]
            # DDX_Text imports are discovered from original assembly below.
            import re
            text=(ROOT/'ghidra/phase34-AP-panel-commit-assembly.txt').read_text()
            calls=[int(x,16) for x in re.findall(r'CALL (0x[0-9a-f]+)',text)]
            others=[x for x in set(calls) if x not in [0x4E490A,0x4E4A2F,0x4E4B5D]]
            assert len(others)==1,others
            mem.append(region(others[0],bytes.fromhex('c20c00')));expected=dict(EAX=1836,EBX=1840,ECX=1841,EDX=1842,ESI=4)
            if c['kind']=='ddx_pointers':
                want=b''.join(struct.pack('<II',control,0x10000068+4*i) for i,control in enumerate(CONTROL_IDS))
                readout=compare_code(0x10003010,0x10006000,len(want))+load_abs(3,0x10003000)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
                mem=[region(0x10004000,readout) if int(r['address'],16)==0x10004000 else r for r in mem];mem.append(region(0x10006000,want));expected=dict(EAX=1,ECX=0,EBX=4)
    elif c['kind'] in ['combo','combo_alloc']:
        entry=0x4325C0;count=len(c['ids']);dialog=bytearray(0x90);model=bytearray(60);model[0x1C]=c['current'];mapmodel=bytearray(0xC00);struct.pack_into('<II',mapmodel,0xB9C,0x10002000,count)
        struct.pack_into('<I',dialog,0x78,0x10001000);struct.pack_into('<III',dialog,0x80,0x10003000,0,count)
        nodes=bytearray(96*count);pointers=b''
        for i,n in enumerate(c['ids']):pointers+=pack32(0x10010000+96*i);nodes[96*i+0x1C]=n
        want=bytes(next_choices(c['ids'],c['current']));assert len(want)<=count;want+=bytes(count-len(want))
        readout=compare_code(0x10003000,0x10006000,count)+load_abs(3,0x10000084)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';combo=bytearray(32);struct.pack_into('<I',combo,0x1C,777)
        mem=[region(0x10000000,dialog),region(0x10001000,model),region(0x10002000,pointers),region(0x10003000,bytes(count)),region(0x10004000,mapmodel),region(0x10005000,combo),region(0x10006000,want),region(0x10007000,bytes.fromhex('c21000')),region(0x10008000,readout),region(0x10010000,nodes),region(0x7FFF0000,pack32(0x10008000)+pack32(1453)),region(0x504638,pack32(0x10007000)),region(0x41C670,return_const(0x10004000)),region(0x403970,bytes.fromhex('c20800')),region(0x403CA0,bytes.fromhex('c20800')),region(0x4DD248,return_const(0x10005000,4)),region(0x4D64C4,b'\xC3'),region(0x4DDDE9,b'\xC3')];regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0,EBX=count)
        if c['kind']=='combo_alloc':
            struct.pack_into('<III',dialog,0x80,0,0,0)
            mem=[region(0x10000000,dialog) if int(r['address'],16)==0x10000000 else region(0x10003000,bytes([0xA5])*count) if int(r['address'],16)==0x10003000 else r for r in mem if int(r['address'],16)!=0x403CA0]
            mem.append(region(0x4D8A63,return_const(0x10003000))) # cdecl allocation fixture, actual native zero helper executes
    elif c['kind']=='auto':
        entry=0x41B840;model=bytearray(60);model[0x1C]=0xA5;mapmodel=bytearray(0xC00);struct.pack_into('<II',mapmodel,0xB9C,0x10002000,len(c['ids']));pointers=b'';nodes=bytearray(96*max(1,len(c['ids'])))
        for i,n in enumerate(c['ids']):pointers+=pack32(0x10010000+96*i);nodes[96*i+0x1C]=n
        readout=bytes.fromhex('0fb6c00fb61d')+pack32(0x1000001C)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,model),region(0x10001000,mapmodel),region(0x10002000,pointers or bytes(4)),region(0x10010000,nodes),region(0x41C670,return_const(0x10001000)),region(0x4D64C4,b'\xC3'),region(0x4DDF22,bytes.fromhex('c20400')),region(0x4DDDE9,b'\xC3'),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000))];regs=dict(ECX='0x10000000');out=auto_id(c['ids'],0xA5);expected=dict(EAX=out['return_byte'],EBX=out['model_id'])
    else:
        entry=0x432820;dialog=bytearray(0x88);struct.pack_into('<I',dialog,0x80,0x10001000);struct.pack_into('<I',dialog,0x84,4);mem=[region(0x10000000,dialog),region(0x10001000,bytes([0,7,3,7])),region(0x7FFF0000,pack32(0xDEADBEEF)+pack32(c['target']))];regs=dict(ECX='0x10000000');expected=dict(EAX=selection_index([0,7,3,7],c['target']))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program='DK2 Editor pro.exe',address=hex(entry),max_steps=300000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase34-emulation-ap-editor-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r)
                assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase34-ap-editor-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
