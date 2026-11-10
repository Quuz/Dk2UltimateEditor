"""Native anger helpers and actor wrapper, plus bounded real field-routing fragments.

Only actor notification4C4600 and state-request49AA30 are captured fixtures.
Threshold path skips most Creature loader/init, gift path skips most49EF90.
All in-memory input/output buffers compared; no original game or data writes.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from native_creature_anger import project,threshold,mean,max_index,u32,s32
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
ENTRIES=dict(init=0x486E00,reset_all=0x486E20,add_65534_all=0x486ED0,reset_one=0x486F80,add=0x487030,percent_add=0x4870E0)
def cases():
    out=[];patterns=[[0,1,100,32767,32768,65533],[0]*6,[0,0,0,0,0,0x7FFFFFFF],[0xFFFFFFFF,0x80000000,0,0,0,0xFFFFFFFF]]
    for mode in ENTRIES:
        for gate in [0,1]:
            for i,values in enumerate(patterns):out.append(dict(name=f'core-{mode}-{gate}-{i}',kind='core',mode=mode,gate=gate,values=values,thr=20000,value=200 if mode=='percent_add' else 1))
    for p in [0,1,50,99,100,101,200,201,255]:
        for current in [0,32767,32768]:out.append(dict(name=f'threshold-{p}-{current}',kind='threshold',p=p,current=current,presence=1))
    for p in [0,100,255]:out.append(dict(name=f'threshold-absent-{p}',kind='threshold',p=p,current=32768,presence=0))
    events=[(0,1000,0),(0,32768,0),(1000,0xFFFFFC18,1),(32768,0xFFFFFFFF,2),(1000,0,0),(1000,1,0)]
    for i,(current,delta,oldstate) in enumerate(events):
        for gate in [0,1]:out.append(dict(name=f'actor-{i}-{gate}',kind='actor',current=current,delta=delta,oldstate=oldstate,gate=gate,emotionless=0))
    out.append(dict(name='actor-emotionless',kind='actor',current=0,delta=32768,oldstate=0,gate=1,emotionless=4))
    for route in ['chicken','gold']:
        for treat in [0,1,1000,32768,65535]:
            for emotionless in [0,4]:out.append(dict(name=f'gift-{route}-{treat}-{emotionless}',kind='gift',route=route,treat=treat,emotionless=emotionless))
    for i,values in enumerate(patterns+[[5]*6,[0,65533,65533,1,0,0]]):out.append(dict(name=f'readers-{i}',kind='readers',values=values))
    for initial in [0,165,255]:
        for available in [0,1,2,255]:out.append(dict(name=f'action-{initial}-{available}',kind='action',gate=initial,available=available))
    return out
def build_case(c):
    creature=bytearray([165])*0x600;definition=bytearray([165])*0x897;world=bytes(0x3000);gate=bytearray(0x72A2);mid=bytearray([165])*0x1500;capture=bytearray(64);args=[];extra=[];regs={};entry=0
    struct.pack_into('<H',creature,4,0x1234);struct.pack_into('<H',creature,0x28,7);struct.pack_into('<II',creature,0x24E+8,0x10000000,0x10004000);struct.pack_into('<I',creature,0x370,0x10002000);definition[0x68A]=c.get('emotionless',0);gate[0x72A1]=c.get('gate',1)
    values=c.get('values',[0]*6);thr=c.get('thr',1000);oldstate=c.get('oldstate',0);block=struct.pack('<8I',*values,thr,oldstate);wanted=block
    if c['kind']=='core':
        entry=ENTRIES[c['mode']];args=[] if c['mode'] in ['init','reset_all','add_65534_all'] else [5] if c['mode']=='reset_one' else [5,c['value']];wanted=project(block,c['mode'],index=5,value=c['value'],gate=c['gate']);regs=dict(ECX='0x1000026e')
    elif c['kind']=='threshold':
        block=struct.pack('<8I',c['current'],0,0,0,0,0,0xBEEF,165);struct.pack_into('<I',mid,0x198,c['presence']);mid[0x133F]=c['p'];struct.pack_into('<H',definition,0x76A,0xBEEF);loaded=threshold(c['p']) if c['presence'] else 0xBEEF;wanted=project(block[:24]+struct.pack('<II',loaded,165),'add',value=0)
        entry=0x492320;regs=dict(EBP='0x10010000',EBX='0x10002000');extra=[region(entry,jump(entry,0x49421F)),region(0x494250,jump(0x494250,0x10050000)),region(0x10050000,b'\xBE'+pack32(0x10000000)+jump(0x10050005,0x4873CA)),region(0x4873DF,jump(0x4873DF,0x10050100)),region(0x10050100,b'\xB9'+pack32(0x1000026E)+bytes.fromhex('6a006a05b8')+pack32(0x487030)+bytes.fromhex('ffd0')+jump(0x10050110,0x10020000))]
    elif c['kind'] in ['actor','gift']:
        values=[0,0,0,0,0,c.get('current',40000)]
        if c['kind']=='gift':values[1]=3000;values[4]=10000;oldstate=2;thr=20000
        block=struct.pack('<8I',*values,thr,oldstate);entry=0x478B80;regs=dict(ECX='0x1000024e');args=[5,c.get('delta',0),2];wanted=block
        if c['kind']=='gift':
            struct.pack_into('<H',definition,0x6E1,c['treat']);index=1 if c['route']=='chicken' else 4;wanted=project(block,'reset_one',index=index);entry=0x49EF90;start,stop=(0x49F0F2,0x49F11C) if index==1 else (0x49F18C,0x49F1BA);extra=[region(entry,jump(entry,start)),region(stop,jump(stop,0x10020000))];regs=dict(EBX='0x10000000',EDI='0x1000024e');args=[];delta=u32(-c['treat']);before=struct.unpack_from('<I',wanted,28)[0]
        else:delta=c['delta'];before=oldstate
        if not c.get('emotionless'):
            wanted=project(wanted,'add',index=5,value=delta,gate=c.get('gate',1));after=struct.unpack_from('<I',wanted,28)[0]
            if after!=before and s32(delta)>0:
                if after in [1,2]:struct.pack_into('<6I',capture,0,1,0x10+after,7,2,0x1234,1)
                if after==2:struct.pack_into('<4I',capture,24,1,11,0,0)
    elif c['kind']=='readers':
        entry=0x4871E0;regs=dict(ECX='0x1000026e');wanted=block
    elif c['kind']=='action':
        block=struct.pack('<8I',0,0,0,0,0,1000,20000,0);action=bytearray([165])*20;action[0]=c['available'];action[14]=44;entry=0x518AD0;regs=dict(ECX='0x10008000');args=[0x10050000];extra=[region(0x10050000,action)]
        extra.append(region(0x10050200,action));enabled=int(c['available']==1);wanted=project(block,'add',index=5,value=1000,gate=enabled);struct.pack_into('<I',capture,40,1)
    else:raise AssertionError(c)
    creature[0x26E:0x28E]=block;wc=bytearray(creature);wc[0x26E:0x28E]=wanted;wd=bytearray(definition);wg=bytearray(gate)
    if c['kind']=='action':wg[0x72A1]=(gate[0x72A1]&254)|int(c['available']==1)
    if c['kind']=='threshold':struct.pack_into('<H',wd,0x76A,threshold(c['p']) if c['presence'] else 0xBEEF)
    # Capture native EAX only where it has a specified result,then compare full state.
    readout=b'\xBD'+pack32(1)
    if c['kind']=='readers':
        readout=b'\xA3'+pack32(0x10021030)+b'\xB9'+pack32(0x1000026E)+b'\xB8'+pack32(0x4871B0)+bytes.fromhex('ffd0a3')+pack32(0x10021034)+b'\xBD'+pack32(1);struct.pack_into('<II',capture,48,u32(mean(block)),max_index(block))
    if c['kind']=='action':readout=b'\xA3'+pack32(0x10021028)+b'\xB9'+pack32(0x1000026E)+b'\x68'+pack32(1000)+bytes.fromhex('6a05b8')+pack32(0x487030)+bytes.fromhex('ffd0bd')+pack32(1)
    for a,b,n in [(0x10000000,0x10030000,len(creature)),(0x10002000,0x10032000,len(definition)),(0x10004000,0x10034000,len(world)),(0x10008000,0x10038000,len(gate)),(0x10010000,0x10040000,len(mid)),(0x10021000,0x10042000,64)]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
    if c['kind']=='action':readout+=compare_code(0x10050000,0x10050200,20)+bytes.fromhex('21c5')
    readout+=END
    notify=bytes.fromhex('ff05')+pack32(0x10021000)+bytes.fromhex('8b442404a3')+pack32(0x10021004)+bytes.fromhex('0fb7442408a3')+pack32(0x10021008)+bytes.fromhex('8b44240ca3')+pack32(0x1002100C)+bytes.fromhex('8b442410a3')+pack32(0x10021010)+bytes.fromhex('8b442414a3')+pack32(0x10021014)+bytes.fromhex('c21400')
    request=bytes.fromhex('ff05')+pack32(0x10021018)+bytes.fromhex('8b442404a3')+pack32(0x1002101C)+bytes.fromhex('8b442408a3')+pack32(0x10021020)+bytes.fromhex('8b44240ca3')+pack32(0x10021024)+bytes.fromhex('c20c00')
    mem=[region(0x10000000,creature),region(0x10002000,definition),region(0x10004000,world),region(0x10008000,gate),region(0x10010000,mid),region(0x10020000,readout),region(0x10021000,bytes(64)),region(0x10030000,wc),region(0x10032000,wd),region(0x10034000,world),region(0x10038000,wg),region(0x10040000,mid),region(0x10042000,capture),region(0x4C4600,notify),region(0x49AA30,request),region(0x6DC424,pack32(0x10008000)),region(0x7FFF0000,pack32(0x10020000)+b''.join(pack32(v) for v in args)),*extra]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address=hex(entry),max_steps=50000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase57-emulation-anger-'+('v2-' if c['kind']=='action' else '')+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase57-anger-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
