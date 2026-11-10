"""Native mana handlers with explicit external-call stubs and result trampolines.

Ownership and terrain-change bodies are unmodified. Ownership's external count
callback is a RET4 stub and game mode4 side effects are disabled. Income fragment
uses two supplied helper results; costs/world/time/UI are outside its scope.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_mana_semantics import ownership_deltas,terrain_change_delta,gross_income,tick_arithmetic,trap_upkeep,income_due,u32,s32

ROOT=Path(__file__).resolve().parent
NODE=0x10000000;WORLD=0x10001000;TERRAIN=0x10010000;PLAYERS=0x10020000;TRAMP=0x10030000
PBASE=0x10040000

def readout(pairs,word_pair=None):
    codes={'EAX':0,'ECX':1,'EDX':2,'EBX':3,'EBP':5,'ESI':6,'EDI':7};code=b''
    for register,address in pairs:code+=bytes([0x8B,0x05+(codes[register]<<3)])+pack32(address)
    if word_pair:
        register,address=word_pair;code+=bytes([0x0F,0xB7,0x05+(codes[register]<<3)])+pack32(address)
    return code+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    rows=[]
    for i,(word,request,flags,gain,cap) in enumerate([
        (1,2,0,1,100),(0,1,0,1,100),(1,0,0,1,100),(0,0,0,1,100),
        (1,1,0,1,100),(1,1,128,1,100),(0xA001,2,0,0xFFFF,0x8000),
        (0xF002,1,0,0x8000,0x7FFF),(1,0x1001,0,7,11),(1,0x10001,0,7,11),
        (1,2,0,0x7FFF,0xFFFF),(0xF001,1,128,0xFFFF,0xFFFF)]):
        rows.append(dict(name=f'ownership-{i}',kind='ownership',values=dict(owner_word=word,requested_owner=request,world_flags=flags,gain_word=gain,ceiling_word=cap)))
    for i,(old,new,of,nf,owner,wf,og,ng) in enumerate([
        (8,9,0,0,1,0,1,2),(8,9,0,0,1,64,1,2),(8,9,0,0,0,0,1,2),
        (0,9,0,0,1,0,0xFFFF,5),(8,9,0,0,0xA001,0,0x8000,0x7FFF),
        (8,8,0,0,1,0,1,2),(8,17,0,16,1,0,100,0),(17,8,16,0,1,0,0,100),
        (17,26,16,16,1,0,100,200),(8,26,0,16,0,0,0,0),
        (8,10,0,16,1,0,100,5),(26,8,16,0,1,0,0,0)]):
        rows.append(dict(name=f'terrain-change-{i}',kind='terrain',values=dict(old_id=old,new_id=new,old_flags=of,new_flags=nf,owner_word=owner,world_flags=wf,old_gain_word=og,new_gain_word=ng)))
    for i,values in enumerate([(20,100,30,0,200),(20,100,30,0,80),(-10,100,30,0,80),
        (0,0,0,0,0),(0,0,0,0,-1),(-100,0,0,0,100),
        (0x7FFFFFFF,1,0,0,100),(0,0,1,0x7FFFFFFF,100),(20,100,30,5,80)]):
        rows.append(dict(name=f'gross-{i}',kind='gross',values=dict(zip(['extra','basic','helper_cost','one_shot_gain','ceiling'],values))))
    for i,values in enumerate([
        (20,100,30,30,200,1,100,1000,0,1),(-10,100,30,30,80,1,100,1000,0,1),
        (-10,100,30,30,80,2,100,1000,0,1),(20,100,30,30,200,3,950,1000,0,1),
        (-200,0,30,30,100,3,10,1000,0,1),(20,100,30,31,200,1,100,1000,0,1),
        (20,100,30,30,200,1,100,1000,3,0),(20,100,30,30,200,1,100,1000,3,2),
        (-7,0,0,0,100,5,100,1000,3,0),(0x40000000,0,0,0,0x7FFFFFFF,1,0,0x7FFFFFFF,3,0),
        (100,0,0,0,100,0,999,1000,0,1),(100,0,0,0,100,-1,999,1000,0,1),
        (0x7FFFFFFF,0,0,0,0x7FFFFFFF,1,1,0x7FFFFFFF,0,1)]):
        rows.append(dict(name=f'tick-{i}',kind='tick',values=dict(zip(['extra','basic','helper_before','helper_after','ceiling','multiplier','current','max_mana','mode','setting'],values))))
    for adding in [True,False]:
        for usage in [0,1,0x7FFF,0x8000,0xFFFF]:
            rows.append(dict(name=f'upkeep-{int(adding)}-{usage}',kind='upkeep',values=dict(usage_word=usage,maintain=0x7FFFFFFE,extra=-10,adding=adding)))
    for i,(last,now,tps) in enumerate([(100,119,20),(100,120,20),(100,200,20),(0xFFFFFFF0,2,20),(0xFFFFFFF0,4,20),(100,100,0)]):
        rows.append(dict(name=f'due-{i}',kind='due',values=dict(last_tick=last,current_tick=now,ticks_per_second=tps)))
    return rows

def build_case(case):
    kind=case['kind'];v=case['values'];mem=[];registers={};expected={}
    if kind=='due':
        entry=0x4C10E0;player=bytearray(0x1400);struct.pack_into('<I',player,0x9A,v['last_tick'])
        registers=dict(ESI=hex(NODE),EAX=hex(v['current_tick']))
        mem=[region(NODE,player),region(entry,jump(entry,0x4C1109)),region(0x4C1127,b'\xB8'+pack32(1)+b'\xC3'),
            region(0x4C16B7,b'\xB8'+pack32(0)+b'\xC3'),region(0x6F2590,pack32(v['ticks_per_second'])),
            region(0x7FFF0000,pack32(0xDEADBEEF)+bytes(0x80))]
        expected=dict(EAX=int(income_due(**v)))
    elif kind in ['ownership','terrain']:
        node=bytearray(16);world=bytearray(0xA400);world[0x7239]=v['world_flags']
        struct.pack_into('<H',node,8,v['owner_word']);table=bytearray(1024)
        for index in range(256):struct.pack_into('<I',table,4*index,TERRAIN+index*0x200)
        definitions=bytearray(256*0x200);players=bytearray(3*0x1400);pointers=bytearray(4096*4)
        initial={}
        for owner in range(3):
            struct.pack_into('<I',pointers,owner*4,PBASE+owner*0x1400)
            gain=[0x7FFFFFFE,101,202][owner];ceiling=[0x80000002,1001,2002][owner]
            struct.pack_into('<II',players,owner*0x1400+0x10DE,ceiling,gain);initial[owner]=dict(gain=gain,ceiling=ceiling)
        if kind=='ownership':
            node[10]=8;struct.pack_into('<HH',definitions,8*0x200+0x187,v['gain_word'],v['ceiling_word'])
            result=ownership_deltas(**v);entry=0x4559D0;argument=v['requested_owner']
            for update in result['updates']:
                for field in ['gain','ceiling']:initial[update['owner']][field]=u32(initial[update['owner']][field]+update[field])
            owner_word=result['owner_word'];mem +=[region(0x4BE070,bytes.fromhex('c20400')),region(0x75AD4B,pack32(0))]
        else:
            node[10]=v['old_id']
            for index,flags,gain in [(v['old_id'],v['old_flags'],v['old_gain_word']),(v['new_id'],v['new_flags'],v['new_gain_word'])]:
                definitions[index*0x200+0x1C4]=index;struct.pack_into('<I',definitions,index*0x200+0x17B,flags)
                struct.pack_into('<H',definitions,index*0x200+0x187,gain)
            result=terrain_change_delta(**v);entry=0x455B90;argument=TERRAIN+v['new_id']*0x200
            initial[result['owner']]['gain']=u32(initial[result['owner']]['gain']+result['gain_delta']);owner_word=v['owner_word']
        pairs=[('EAX',PBASE+0x10E2),('EDX',PBASE+0x1400+0x10E2),('ECX',PBASE+0x2800+0x10E2),
            ('EBX',PBASE+0x10DE),('EDI',PBASE+0x1400+0x10DE),('EBP',PBASE+0x2800+0x10DE)]
        adapter=readout(pairs,('ESI',NODE+8))
        expected=dict(EAX=initial[0]['gain'],EDX=initial[1]['gain'],ECX=initial[2]['gain'],
            EBX=initial[0]['ceiling'],EDI=initial[1]['ceiling'],EBP=initial[2]['ceiling'])
        if kind=='ownership':expected['ESI']=owner_word
        else:
            # A second trampoline entry reads only the low byte into ESI.
            adapter=readout(pairs)[:-6]+bytes.fromhex('0fb635')+pack32(NODE+10)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            expected['ESI']=v['new_id']
        mem +=[region(NODE,node),region(WORLD,world),region(0x6DA4A4,pack32(WORLD)+table),region(TERRAIN,definitions),
            region(PBASE,players),region(0x6EE540,pointers),region(0x7FFF0000,pack32(TRAMP)+pack32(argument)),region(TRAMP,adapter)]
        registers=dict(ECX=hex(NODE))
    elif kind in ['gross','tick']:
        player=bytearray(0x1400)
        for key,offset in [('extra',0x10E2),('basic',0x10E6),('ceiling',0x10DE)]:struct.pack_into('<I',player,offset,u32(v[key]))
        if kind=='gross':
            struct.pack_into('<I',player,0x10EE,u32(v['one_shot_gain']));entry=0x4C1770;registers=dict(ECX=hex(NODE))
            mem +=[region(0x4C17A0,b'\xB8'+pack32(v['helper_cost'])+b'\xC3')]
            expected=dict(EAX=u32(gross_income(**v)))
        else:
            for key,offset in [('multiplier',0x86),('current',0x82),('max_mana',0x10EA)]:struct.pack_into('<I',player,offset,u32(v[key]))
            struct.pack_into('<II',player,0x10EE,12345,67890)
            entry=0x4C10E0;registers=dict(ESI=hex(NODE));counter=0x10035000
            stub=b'\xA1'+pack32(counter)+bytes.fromhex('85c07510')+b'\xB8'+pack32(v['helper_before'])+bytes.fromhex('c705')+pack32(counter)+pack32(1)+b'\xC3'+b'\xB8'+pack32(v['helper_after'])+b'\xC3'
            adapter=readout([('EAX',NODE+0x82),('EDX',NODE+0x10EE),('ECX',NODE+0x10F2)])
            # Preserve EBX, which holds the native final delta.
            mem +=[region(entry,jump(entry,0x4C1127)),region(0x4C11ED,jump(0x4C11ED,TRAMP)),region(TRAMP,adapter),
                region(0x4C17A0,stub),region(counter,pack32(0)),region(0x75AD4B,pack32(v['mode'])),region(0x75AEA7,struct.pack('<H',v['setting'])),
                region(0x7FFF0000,pack32(0xDEADBEEF)+bytes(0x80))]
            result=tick_arithmetic(**v);expected=dict(EAX=u32(result['mana']),EBX=u32(result['delta']),EDX=0,ECX=0)
        mem.append(region(NODE,player))
    else:
        player=bytearray(0x1400);trap=bytearray(0x120);definition=bytearray(0xD1);trap_address=0x10008000
        struct.pack_into('<II',player,0x10D6,u32(v['maintain']),0) # +10DA is unrelated padding for this isolated case
        struct.pack_into('<I',player,0x10E2,u32(v['extra']));struct.pack_into('<I',trap,0xF7,TERRAIN);struct.pack_into('<H',definition,0x96,v['usage_word'])
        entry=0x4C1B60 if v['adding'] else 0x4C1B90;registers=dict(ECX=hex(NODE))
        mem=[region(NODE,player),region(trap_address,trap),region(TERRAIN,definition),region(0x7FFF0000,pack32(TRAMP)+pack32(trap_address)),
            region(TRAMP,readout([('EAX',NODE+0x10D6),('EDX',NODE+0x10E2)]))]
        result=trap_upkeep(**v);expected=dict(EAX=u32(result['maintain']),EDX=u32(result['extra']))
    return dict(program='DKII-DX.exe',address=hex(entry),max_steps=10000,registers=json.dumps(registers),
        memory=json.dumps(dict(regions=mem)),return_registers=','.join(expected)+',ESP,EIP'),expected

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
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase23-emulation-mana-'+case['name']+'.json')
                if path.exists():
                    saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);raw='\n'.join(c.text for c in response.content if c.type=='text');result=json.loads(raw)
                    if response.isError or 'error' in result:raise RuntimeError(raw)
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result)
                    path.write_text(json.dumps(saved,ensure_ascii=False,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                for register,value in expected.items():assert int(result['registers'][register],16)==value,(case['name'],register,result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']))
                print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase23-mana-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
