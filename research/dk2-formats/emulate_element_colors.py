"""Emulate the original color/fade instruction fragments with explicit adapters.

Only EmulatorHelper memory is patched. No program database or EXE writes.
Entry jumps skip unrelated setup. Exit adapters read results then RET to sentinel.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from native_element_colors import initial_color,fade_after_lifetime_update,alpha_threshold

ROOT=Path(__file__).resolve().parent
def jump(source,target):return b'\xe9'+struct.pack('<i',target-source-5)
def region(address,data):return dict(address=hex(address),hex=bytes(data).hex())
def pack32(value):return struct.pack('<I',value&0xFFFFFFFF)

def build_case(case):
    node=bytearray(0x140);definition=bytearray(0x94);resource=bytearray(0x20)
    struct.pack_into('<I',node,0x103,0x10001000)
    memories=[region(0x10000000,node),region(0x10001000,definition),region(0x10002000,resource)]
    registers={'ESI':'0x10000000'}
    if case['kind']=='fade':
        values=case['values'];rgba=values['rgba'];h=values['h'];threshold=values['threshold'];flags=values['flags'];kind=values['resource_type'];render=values.get('renderer_flags',0)
        struct.pack_into('<I',node,0x6E,sum(v<<(8*i) for i,v in enumerate(rgba)))
        struct.pack_into('<I',node,0x109,h&0xFFFFFFFF);struct.pack_into('<I',node,0x10D,threshold)
        struct.pack_into('<I',node,0x81,render);struct.pack_into('<I',node,0x4E,0x10002000)
        struct.pack_into('<I',definition,0x1F,flags);resource[0xA]=kind
        entry=0x436430;start=0x4364DD;end=0x4365A2
        adapter=bytes.fromhex('8b466e8b96810000008b8e09010000c3')
        stack=bytearray(0x40);stack[:4]=pack32(0xDEADBEEF)
        memories=[region(0x10000000,node),region(0x10001000,definition),region(0x10002000,resource),region(0x7FFF0000,stack)]
        expected=fade_after_lifetime_update(rgba,h,threshold,flags,kind,render)
        expected_registers=dict(EAX=expected['packed'],EDX=expected['renderer_flags'],ECX=h&0xFFFFFFFF)
    elif case['kind']=='threshold':
        values=case['values'];h=values['initial_lifetime'];percentage=values['fade_percentage']
        struct.pack_into('<H',definition,0x23,h);definition[0x3D]=percentage
        registers.update(ECX='0x0',EBP='0x7FFF0000')
        entry=0x435F20;start=0x4361CB;end=0x436200
        adapter=bytes.fromhex('8b86090100008b960d010000c3')
        memories=[region(0x10000000,node),region(0x10001000,definition)]
        expected_registers=dict(EAX=h,EDX=alpha_threshold(h,percentage)['unsigned'])
    else:
        values=case['values'];kind=values['resource_type'];flags=values['resource_flags'];rgb=values['rgb']
        definition[0xA]=kind;struct.pack_into('<I',definition,0xB,flags);definition[0x4A:0x4D]=bytes(rgb)
        # Set unrelated index slots deliberately; they are not consumed by this fragment.
        definition[0x4D:0x4F]=bytes(values.get('indices',[0,0]))
        entry=0x435F20;start=0x43630B;end=0x4363B7;adapter=bytes.fromhex('8b466ec3')
        registers['EBP']='0x7FFF0000';stack=bytearray(0x40);stack[:4]=pack32(0xDEADBEEF)
        owner=values.get('owner',1);local=values.get('local',1);stack[0xC:0x10]=pack32(owner)
        bridge=pack32(0x10003100);table1=bytearray(0x300);table1[0xC4:0xC8]=pack32(0x10004000)
        colors=pack32(0x10003300);table2=bytearray(0x300);table2[0x298:0x29C]=pack32(0x10004020);table2[0x290:0x294]=pack32(0x10004040)
        local_rgb=values.get('local_rgb',[0,47,255]);other_rgb=values.get('other_rgb',[1,48,49])
        memories=[region(0x10000000,node),region(0x10001000,definition),region(0x7FFF0000,stack),
            region(0x6DA458,pack32(0x10003000)+pack32(0x10003200)),region(0x10003000,bridge),region(0x10003100,table1),
            region(0x10003200,colors),region(0x10003300,table2),region(0x10005000,local_rgb),region(0x10005010,other_rgb),
            region(0x10004000,b'\xb8'+pack32(local)+b'\xc3'),
            region(0x10004020,b'\xb8'+pack32(0x10005000)+bytes.fromhex('c20400')),
            region(0x10004040,b'\xb8'+pack32(0x10005010)+bytes.fromhex('c20800'))]
        expected=initial_color(kind,flags,rgb,resolved_player_rgb=local_rgb if owner==local else other_rgb)
        expected_registers=dict(EAX=expected['packed'])
    memories +=[region(entry,jump(entry,start)),region(end,adapter)]
    args=dict(program='DKII-DX.exe',address=hex(entry),max_steps=1000,
        registers=json.dumps(registers),memory=json.dumps(dict(regions=memories)),return_registers=','.join(expected_registers)+',ESP,EIP')
    return args,expected_registers,dict(original_start=hex(start),original_end_exclusive=hex(end),
        entry_adapter=jump(entry,start).hex(),exit_adapter=adapter.hex(),scope='Original branch instructions unchanged; surrounding code and color providers replaced in emulator memory only.')

def cases():
    result=[]
    for name,h,threshold,flags,kind,rgb in [
        ('rgb-threshold-equal',100,0,8,3,[100,50,10,255]),('rgb-threshold-above',101,0,8,3,[100,50,10,255]),
        ('rgb-last-nonzero',1,0,8,3,[100,50,10,255]),('zero-hp-skip',0,25,8,3,[100,50,10,255]),
        ('no-fade-flag',10,25,0,3,[100,50,10,255]),('alpha-strict-equal',25,25,8,3,[100,50,10,255]),
        ('alpha-below',24,25,8,3,[100,50,10,255]),('alpha-threshold-negative',24,0xFFFFFFFB,8,3,[100,50,10,255]),
        ('negative-lifetime',-1,25,8,3,[100,200,250,255]),('alpha-sprite-no-render-bit',10,25,8,1,[100,50,10,255]),
        ('alpha-mesh-render-bit',10,25,8,5,[100,50,10,255]),('alpha-type6-no-render-bit',10,25,8,6,[100,50,10,255])]:
        result.append(dict(name=name,kind='fade',values=dict(h=h,threshold=threshold,flags=flags,resource_type=kind,rgba=rgb,renderer_flags=0x120)))
    for name,kind,flags,owner,indices in [
        ('raw-rgb',3,0,1,[0,0]),('raw-rgb-indices255',3,0,1,[255,255]),
        ('sprite-white',1,0,1,[0,0]),('sprite-overrides-player',1,2,1,[9,17]),
        ('player-local-clamp',3,2,1,[0,0]),('player-other-clamp',3,2,2,[0,0])]:
        result.append(dict(name=name,kind='initial',values=dict(resource_type=kind,resource_flags=flags,owner=owner,local=1,rgb=[5,70,240],indices=indices)))
    for h,percentage in [(0,100),(1,70),(128,70),(600,0),(600,25),(600,100),(600,101),(65535,255)]:
        result.append(dict(name=f'threshold-{h}-{percentage}',kind='threshold',values=dict(initial_lifetime=h,fade_percentage=percentage)))
    return result

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
                path=ROOT/'ghidra'/('phase21-emulation-color-'+case['name']+'.json')
                args,expected,adapter=build_case(case)
                if path.exists():
                    saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw='\n'.join(c.text for c in response.content if c.type=='text');result=json.loads(raw)
                    if response.isError or 'error' in result:raise RuntimeError(raw)
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,adapter=adapter,expected_registers=expected,result=result)
                    path.write_text(json.dumps(saved,ensure_ascii=False,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                for register,value in expected.items():assert int(result['registers'][register],16)==value,(case['name'],register,result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']))
                print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase21-color-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
