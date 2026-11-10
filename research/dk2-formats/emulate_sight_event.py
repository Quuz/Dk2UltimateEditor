"""Full native Sight process→World→Session→Bridge0D→region composition.

Native508C60,4AF0E0,517480,527150,43B1E0,448580,441510 and440F50execute.
Redraw handler6,engine event/effect parameter providers,effect allocation and
sound are explicit vtable/data adapters. No original code bytes are replaced.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_sight_event import describe_data1,packed_rectangle,region_state,s32
ROOT=Path(__file__).resolve().parent
SHOT=0x10000000;WORLD=0x10010000;SESSION=0x10020000;BRIDGE=0x10030000;ENGINE=0x10040000;SOUND=0x10041000;SHOTDEF=0x10042000;KEEPER=0x10043000;PARAMS=0x10044000
WV=0x10050000;BV=0x10051000;EV=0x10052000;SV=0x10053000;OUT=0x10060000;OBS=0x10080000
REDRAW=0x10090000;ENGINE_CMD=0x10091000;PARAM_CB=0x10092000;EFFECT_CB=0x10093000;SOUND_CB=0x10094000;STACK=0x7FFF0000

def cases():
    base=dict(c8=0xFE0C0000,data1=5,data2=0,processed=0,owner=3,local=3,time40=100,tick=123,width=32,height=24,position=[10*4096+2048,12*4096+682,0xCAFEBABE],old_active=0,old_outside=[3,4,8,9],bridge_present=True,repeat=False,session_count=7)
    out=[]
    for value in [0,1,0x7FFFFFFF,0x80000000,0xFE0C0000,0xFFFFFFFF]:out.append(dict(base,name=f'c8-{value:08X}',c8=value))
    for value in [0,1,6,7,8,0xFFFF,0x10000,0x80000000,0xFFFFFFFF]:out.append(dict(base,name=f'data1-{value:08X}',data1=value))
    variations=[('wrong-owner',dict(owner=4)),('bridge-null',dict(bridge_present=False)),('already-processed',dict(processed=1)),
      ('sound-time20',dict(processed=1,time40=20)),('sound-high40',dict(processed=1,time40=0x10014)),
      ('processed-highbit',dict(processed=0x1000000,time40=20)),('first-and-repeat20',dict(time40=20,repeat=True)),
      ('old-region',dict(old_active=99)),('tick-overflow',dict(tick=0xFFFFFFFE,time40=0xFFFF)),('tick-zero',dict(tick=0)),
      ('position-negative',dict(position=[0xFFFFFFFF,0xFFFFF123,0])),('edge',dict(position=[31*4096+4095,23*4096+4095,0],data1=7)),
      ('single-cell-map',dict(width=1,height=1)),('session-counter-wrap',dict(session_count=0xFFFFFFFF)),('data2-ignored-here',dict(data2=0xFFFFFFFF))]
    out.extend(dict(base,**kw,name=name) for name,kw in variations)
    return out

def inc(address):return bytes.fromhex('ff05')+pack32(address)
def store_eax(address):return b'\xA3'+pack32(address)
def store_ecx(address):return bytes.fromhex('890d')+pack32(address)
def event_adapter(count,events,context):
    code=store_ecx(context)+b'\xA1'+pack32(count)+bytes.fromhex('c1e0048bd08b4c2404')
    for i in range(4):code+=(bytes.fromhex('8b01') if i==0 else bytes.fromhex('8b41')+bytes([4*i]))+bytes.fromhex('8982')+pack32(events+4*i)
    return code+inc(count)+bytes.fromhex('33c0c20400')

def build(c):
    shot=bytearray((i*17+19)&255 for i in range(371));world=bytearray((i*23+11)&255 for i in range(0x7300));session=bytearray((i*13+7)&255 for i in range(0x2A0));bridge=bytearray((i*31+3)&255 for i in range(0x1400))
    for a,v in [(0x40,c['time40']),(0x125,SHOTDEF),(0x13D,c['data1']),(0x141,c['data2']),(0x169,c['processed']),(0x2E,0xAA223344)]:struct.pack_into('<I',shot,a,v)
    for i,v in enumerate(c['position']):struct.pack_into('<I',shot,0x1A+4*i,v)
    struct.pack_into('<H',shot,0x28,c['owner'])
    for a,v in [(0,WV),(4,SESSION),(0x1604,KEEPER),(0x160C,1)]:struct.pack_into('<I',world,a,v)
    struct.pack_into('<H',world,0x7243,c['local'])
    for a,v in [(4,c['tick']),(0x10,BRIDGE if c['bridge_present'] else 0),(0x26C,c['session_count'])]:struct.pack_into('<I',session,a,v)
    for a,v in [(0,BV),(4,SESSION),(0x9E,ENGINE),(0x32E,c['width']),(0x332,c['height']),(0x139F,c['old_active'])]:struct.pack_into('<I',bridge,a,v)
    bridge[0x2CA:0x326]=bytes(0x5C);struct.pack_into('<I',bridge,0x2CA+6*4,REDRAW);struct.pack_into('<I',bridge,0x2CA+13*4,0x448580)
    for i,v in enumerate(c['old_outside']):struct.pack_into('<I',bridge,0x13B7+4*i,v)
    shotdef=bytearray((i*7+5)&255 for i in range(0x90));struct.pack_into('<I',shotdef,0x72,0x11223344)
    keeper=bytearray((i*37+11)&255 for i in range(122));keeper[0x75]=1;struct.pack_into('<I',keeper,0x3A,c['c8'])
    wv=bytearray(0x268);struct.pack_into('<I',wv,0x264,0x508C60);struct.pack_into('<I',wv,0x110,0x50DD60)
    bv=bytearray(0xC0)
    for a,v in [(0x28,0x43B1E0),(0x90,EFFECT_CB),(0xBC,0x440F50)]:struct.pack_into('<I',bv,a,v)
    ev=bytearray(0xC0);struct.pack_into('<I',ev,0x28,ENGINE_CMD);struct.pack_into('<I',ev,0xBC,PARAM_CB)
    sv=bytearray(0x24);struct.pack_into('<I',sv,0x20,SOUND_CB)
    engine=pack32(EV)+bytes(12);sound=pack32(SV)+bytes(12);params=bytes((i*9+1)&255 for i in range(32));out=bytearray(0x500)
    want_shot=bytearray(shot);want_session=bytearray(session);want_bridge=bytearray(bridge);want=bytearray(out);redraws=[];events=[];current_processed=c['processed'];current_handle=0xAA223344;effects=parameters=sounds=0;state=None
    for invocation in range(2 if c['repeat'] else 1):
        if current_processed==0:
            current_processed=1
            if c['owner']==c['local'] and c['data1'] and c['bridge_present']:
                struct.pack_into('<I',want_session,0x26C,(struct.unpack_from('<I',want_session,0x26C)[0]+1)&0xFFFFFFFF)
                radius=describe_data1(c['data1'])['receiver_radius'];x,y=[s32(v)>>12 for v in c['position'][:2]]
                if c['old_active']:redraws.append(packed_rectangle(c['old_outside']));events.append(redraws[-1])
                state=region_state(x,y,radius,c['width'],c['height'],c['tick'],c['time40']&65535)
                for a,v in [(0x139F,state['start_tick']),(0x13A3,state['end_tick']),(0x13C7,state['radius'])]:struct.pack_into('<I',want_bridge,a,v)
                for i,v in enumerate(state['inside']):struct.pack_into('<I',want_bridge,0x13A7+4*i,v)
                for i,v in enumerate(state['outside']):struct.pack_into('<I',want_bridge,0x13B7+4*i,v)
                redraws.append(packed_rectangle(state['outside']));events.append(redraws[-1]);events.append([c['position'][0],c['position'][1],(c['time40']&65535)|((c['data1']&65535)<<16),13])
                effects+=1;parameters+=1
                effect_position=[(x<<12)&0xFFFFFFFF,(y<<12)&0xFFFFFFFF,0x2000]
                for i,v in enumerate([BRIDGE,45,PARAMS]):struct.pack_into('<I',want,0x300+4*i,v)
                for i,v in enumerate([ENGINE,*effect_position,*effect_position,0]):struct.pack_into('<I',want,0x340+4*i,v)
        elif c['owner']==c['local'] and c['time40']==20:
            sounds+=1
            for i,v in enumerate([SOUND,current_handle,0x11223344,0xE8,SHOT+0x1A,*c['position']]):struct.pack_into('<I',want,0x400+4*i,v)
            current_handle=0x55667788
    struct.pack_into('<I',want_shot,0x169,current_processed);struct.pack_into('<I',want_shot,0x2E,current_handle)
    for a,v in [(0,1),(4,1 if c['repeat'] else 0),(8,len(redraws)),(12,len(events)),(16,effects),(20,parameters),(24,sounds),(28,ENGINE if events else 0),(32,BRIDGE if redraws else 0)]:struct.pack_into('<I',want,a,v)
    for i,record in enumerate(redraws):struct.pack_into('<4I',want,0x100+16*i,*record)
    for i,record in enumerate(events):struct.pack_into('<4I',want,0x200+16*i,*record)
    redraw=event_adapter(OUT+8,OUT+0x100,OUT+32);forward=event_adapter(OUT+12,OUT+0x200,OUT+28)
    effect=store_ecx(OUT+0x300)
    for i in range(2):effect+=bytes.fromhex('8b4424')+bytes([4+4*i])+store_eax(OUT+0x304+4*i)
    effect+=inc(OUT+16)+bytes.fromhex('b801000000c20800')
    param=store_ecx(OUT+0x340)
    for arg in range(2):
        param+=bytes.fromhex('8b4c24')+bytes([4+4*arg])
        for i in range(3):param+=(bytes.fromhex('8b01') if i==0 else bytes.fromhex('8b41')+bytes([4*i]))+store_eax(OUT+0x344+12*arg+4*i)
    param+=bytes.fromhex('8b4c240c0fb701')+store_eax(OUT+0x35C)+bytes.fromhex('8b4c2410c70178563412')+inc(OUT+20)+b'\xB8'+pack32(PARAMS)+bytes.fromhex('c21000')
    sound_code=store_ecx(OUT+0x400)
    for i in range(4):sound_code+=bytes.fromhex('8b4424')+bytes([4+4*i])+store_eax(OUT+0x404+4*i)
    sound_code+=bytes.fromhex('8b4c2410')
    for i in range(3):sound_code+=(bytes.fromhex('8b01') if i==0 else bytes.fromhex('8b41')+bytes([4*i]))+store_eax(OUT+0x414+4*i)
    sound_code+=inc(OUT+24)+b'\xB8'+pack32(0x55667788)+bytes.fromhex('c21000')
    buffers=[(SHOT,shot,want_shot),(WORLD,world,world),(SESSION,session,want_session),(BRIDGE,bridge,want_bridge),(ENGINE,engine,engine),(SOUND,sound,sound),(SHOTDEF,shotdef,shotdef),(KEEPER,keeper,keeper),(PARAMS,params,params),(WV,wv,wv),(BV,bv,bv),(EV,ev,ev),(SV,sv,sv),(OUT,out,want),(0,bytes(4),bytes(4)),(0x6E5050,pack32(WORLD),pack32(WORLD)),(0x75B468,pack32(SOUND),pack32(SOUND))]
    mem=[region(a,b) for a,b,_ in buffers]+[region(REDRAW,redraw),region(ENGINE_CMD,forward),region(PARAM_CB,param),region(EFFECT_CB,effect),region(SOUND_CB,sound_code),region(STACK,pack32(OBS))]
    observer=store_eax(OUT)
    if c['repeat']:observer+=b'\xB9'+pack32(SHOT)+b'\xB8'+pack32(0x4AF0E0)+bytes.fromhex('ffd0')+store_eax(OUT+4)
    observer+=b'\xBD'+pack32(1);at=0x11000000
    for address,_,b in buffers:mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    expected=dict(native_return=1,region=state,redraw_events=redraws,engine_events=events,effects=effects,sound_calls=sounds,processed=current_processed,session_count=struct.unpack_from('<I',want_session,0x26C)[0])
    return dict(program='DKII-DX.exe',address='0x4af0e0',max_steps=350000,registers=json.dumps(dict(ECX=hex(SHOT))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,expected=build(c);path=ROOT/'ghidra'/f'phase77-emulation-sight-{c["name"]}.json'
            if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==expected
            else:
                response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=expected,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],r,expected)
            rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase77-sight-event-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
