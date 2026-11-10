"""Real raw→World→UI selector/tab/click→Session queue,with original code/vtables.

The client UI is prepared at cursor stateC and initial hand mode0;no summoned
object buttons,World7291=FF. Native local queue executes;no network/game GUI.
Only external sound is a fixed provider;GFX provider is null during loading.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_keeper_pipeline import intermediate
from native_keeper_array import apply,default_definition,put
from native_keeper_ui import sequence,select
import dk2_formats as fmt
import memory_readout
ROOT=Path(__file__).resolve().parent
DATA=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor'
WORLD=0x10000000;ARRAY=0x10010000;MID=0x10020000;RAW=0x10024000;SESSION=0x10028000;UI=0x10030000;BUTTON=0x10038000;PLAYER=0x10039000;STATES=0x1003A000
SOUND=0x1003B000;SV=0x1003C000;LOAD_CB=0x1003D000;UI_CB=0x1003D100;SCRATCH=0x1003E000;OUT=0x1003F000;DRIVER=0x10040000;OBS=0x10048000
SOURCES={'standalone':DATA/'KeeperSpells.kwd','qz2':DATA/'maps/Battle for the Sun Qz2Globals.kld','qz':DATA/'maps/Battle for the Sun QzGlobals.kld'}

def cases():
    base=dict(source='standalone',default_state=0,states={},omit_state=[],db={},reverse=False,subset=None,c8=None,click=9,queue_count=0,queue_write=0)
    variants=[('standalone',{}),('qz2',dict(source='qz2')),('qz',dict(source='qz')),('qz2-reverse',dict(source='qz2',reverse=True)),
      ('qz2-gold-order10',dict(source='qz2',db={'13':10},click=10)),('qz2-raise-dead-order10',dict(source='qz2',db={'9':10})),
      ('gold-order0',dict(db={'13':0})),('gold-order255',dict(db={'13':255})),
      ('only-gold',dict(default_state=1,states={'13':0},click=1)),('qz2-only-gold',dict(source='qz2',default_state=1,states={'13':0},click=1)),
      ('only-reserved',dict(default_state=1,states={'5':0,'14':0},click=1)),('qz2-only-id5',dict(source='qz2',default_state=1,states={'5':0},click=1)),
      ('all-hidden',dict(default_state=1,click=1)),('gold-state2',dict(default_state=1,states={'13':2},click=1)),
      ('gold-state100',dict(default_state=1,states={'13':256},click=1)),('gold-state-max',dict(default_state=1,states={'13':0xFFFFFFFF},click=1)),
      ('gold-state-missing',dict(default_state=1,states={'13':0},omit_state=[13],click=1)),
      ('queue-full',dict(queue_count=32)),('queue-wrap',dict(queue_count=31,queue_write=31)),
      ('empty',dict(subset=[],click=1)),('sparse-order-outside-count',dict(subset=[1,3],default_state=1,states={'3':0},click=1)),
      ('sparse-order-fixed',dict(subset=[1,3],default_state=1,states={'3':0},db={'3':2},click=1))]
    rows=[dict(base,**kw,name=name) for name,kw in variants]
    for value in [0,1,0x7FFFFFFF,0x80000000,0xFE0C0000,0xFFFFFFFF]:rows.append(dict(base,c8=value,name=f'c8-{value:x}'))
    return rows

def call(at,target):return b'\xE8'+pack32(target-(at+5))
def source_records(c):
    data=SOURCES[c['source']].read_bytes();records=next(s.records for s in fmt.parse(data) if s.signature==150);out=[]
    for raw in records:
        if c['subset'] is not None and raw[0xEE] not in c['subset']:continue
        r=bytearray(raw);key=str(r[0xEE])
        if key in c['db']:r[0xDB]=c['db'][key]
        if c['c8'] is not None and r[0xEE]==13:put(r,0xC8,c['c8'])
        out.append(bytes(r))
    if c['reverse']:out.reverse()
    return out,dict(file=str(SOURCES[c['source']]),sha256=hashlib.sha256(data).hexdigest())

def build(c):
    records,source=source_records(c);capacity=len(records);world=bytearray(0xA3D0);session=bytearray([0xA5])*0x300;ui=bytearray(0x2800);button=bytearray([0xA5])*0x80;player=bytearray(0x100)
    for o,v in [(0,0x66E3EC),(4,SESSION),(0x1604,ARRAY),(0x1608,capacity)]:put(world,o,v)
    world[0x7291]=255;put(session,0x10,0);put(session,0x14,WORLD);put(session,0x25C,c['queue_count']);put(session,0x260,0);put(session,0x264,c['queue_write'])
    for o,v,n in [(0,0x66C4A4,4),(4,SESSION,4),(8,7,2),(0x53,WORLD,4),(0xB5,4,4),(0x12F6,12,4),(0x13E8,204,1)]:put(ui,o,v,n)
    put(button,0x6B,2);state_ids=sorted(r[0xEE] for r in records if r[0xEE] not in c['omit_state']);values=[c['states'].get(str(i),c['default_state']) for i in state_ids]
    states=bytearray([0xA5])*max(17*len(state_ids),17)
    for j,(ident,value) in enumerate(zip(state_ids,values)):states[17*j]=ident;put(states,17*j+1,value)
    put(player,0x6E,len(state_ids));put(player,0x72,STATES)
    tags=bytearray(4096*4);put(tags,7*4,PLAYER)
    array=bytes([0xA5])*(122*capacity);wanted_array=default_definition()*capacity;count=aggregate=sound_loads=0;mid_last=bytes([0xCC])*526
    driver=bytearray()
    def emit(b):driver.extend(b)
    def native_call(a):emit(call(DRIVER+len(driver),a))
    for i in range(capacity):emit(b'\xB9'+pack32(ARRAY+122*i));native_call(0x4F7C70)
    for i,raw in enumerate(records):
        emit(b'\xB9'+pack32(MID));native_call(0x56B990);emit(b'\x68'+pack32(RAW+406*i)+b'\xB9'+pack32(MID));native_call(0x56BAB0)
        emit(b'\x68'+pack32(WORLD)+b'\x68'+pack32(MID));native_call(0x51FCD0);emit(bytes.fromhex('83c408'))
        mid_last=intermediate(raw);e=apply(wanted_array,count,capacity,aggregate,mid_last);wanted_array=e['array'];count=e['count'];aggregate=e['aggregate'];sound_loads+=e['sound_calls']
    definitions=[wanted_array[i:i+122] for i in range(0,len(wanted_array),122)];scan=sequence(definitions,state_ids,values);visible=[r['id'] for r in scan if r['eligible']]
    ordinals=[0,*range(1,14),0xFFFFFFFF];output=bytearray(0x200);wanted_out=bytearray(output);results=[]
    for j,ordinal in enumerate(ordinals):
        at=0x80+j;output[at]=204;wanted_out[at]=204;selected=select(scan,ordinal)
        put(wanted_out,4*j,int(selected is not None))
        if selected is not None:wanted_out[at]=selected
        results.append(dict(ordinal=ordinal,success=selected is not None,id=selected))
        emit(b'\x68'+pack32(UI)+b'\x68'+pack32(OUT+at)+b'\x68'+pack32(ordinal));native_call(0x4115E0);emit(bytes.fromhex('83c40ca3')+pack32(OUT+4*j))
    emit(b'\x68'+pack32(UI)+b'\x68'+pack32(BUTTON));native_call(0x416E20);emit(bytes.fromhex('83c408'))
    emit(b'\x68'+pack32(UI)+b'\x6A\x00'+b'\x68'+pack32(c['click']));native_call(0x411140);emit(bytes.fromhex('83c40c'))
    emit(b'\x68'+pack32(OBS)+b'\xC3')
    selected_click=select(scan,c['click']);wanted_ui=bytearray(ui);wanted_session=bytearray(session);wanted_button=bytearray(button);put(wanted_button,0x61,int(bool(visible)))
    queued=False
    if selected_click is not None:
        put(wanted_ui,0x13DB,8);put(wanted_ui,0x13E8,selected_click,1);put(wanted_ui,0x2E,0x55667788);put(wanted_ui,0xA,1)
        put(wanted_out,0xD4,1);put(wanted_out,0xE0,0);put(wanted_out,0xE4,0x11223344);put(wanted_out,0xE8,0x104)
        if c['queue_count']!=32:
            at=0x1C+18*c['queue_write'];wanted_session[at:at+18]=struct.pack('<4IH',5,selected_click,0,0x72,7)
            put(wanted_session,0x25C,c['queue_count']+1);put(wanted_session,0x264,(c['queue_write']+1)%32);queued=True
    put(wanted_out,0xD0,sound_loads);wanted_world=bytearray(world);put(wanted_world,0x160C,count);put(wanted_world,0x1610,aggregate)
    sv=bytearray(0x28);put(sv,0x1C,LOAD_CB);put(sv,0x24,UI_CB)
    load_cb=bytes.fromhex('ff05')+pack32(OUT+0xD0)+b'\xB8'+pack32(0x11223344)+bytes.fromhex('c20800')
    ui_cb=bytes.fromhex('ff05')+pack32(OUT+0xD4)
    for off,dst in [(4,0xE0),(8,0xE4),(12,0xE8)]:ui_cb+=bytes.fromhex('8b4424')+bytes([off])+b'\xA3'+pack32(OUT+dst)
    ui_cb+=b'\xB8'+pack32(0x55667788)+bytes.fromhex('c20c00')
    rawbuf=b''.join(records)+bytes([0x5A])*64;arrbuf=array+bytes([0x5A])*256;wantarr=wanted_array+bytes([0x5A])*256
    fixed=[(WORLD,world,wanted_world),(ARRAY,arrbuf,wantarr),(MID,bytes([0xCC])*526,mid_last),(RAW,rawbuf,rawbuf),(SESSION,session,wanted_session),
      (UI,ui,wanted_ui),(BUTTON,button,wanted_button),(PLAYER,player,player),(STATES,states,states),(0x6EE540,tags,tags),(OUT,output,wanted_out),
      (SOUND,pack32(SV),pack32(SV)),(SV,sv,sv),(0x75B468,pack32(SOUND),pack32(SOUND)),(0x6F2590,pack32(20),pack32(20)),(0x6FCDCF,bytes(4),bytes(4)),
      (SCRATCH,bytes([0xA5])*122,default_definition()),(0,bytes(4),bytes(4))]
    mem=[region(a,b) for a,b,_ in fixed]+[region(DRIVER,driver),region(LOAD_CB,load_cb),region(UI_CB,ui_cb)]
    observer=b'\xBD'+pack32(1)+b'\x31\xDB';at=0x11000000
    for i,(a,_,b) in enumerate(fixed):mem.append(region(at,b));observer+=compare_code(a,at,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(SESSION+0x1C)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer));stack=bytearray([0xA5])*16384;put(stack,8192,DRIVER);mem.append(region(0x7FFEE000,stack))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),spans
    args=dict(program='DKII-DX.exe',address='0x4f7c70',max_steps=1000000,registers=json.dumps(dict(ECX=hex(SCRATCH),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,'+memory_readout.REGISTERS)
    return args,dict(source=source,raw_records=len(records),runtime_ids=[r[0x75] for r in definitions],order_scan=scan,visible_ids=visible,selection_results=results,
      clicked_id=selected_click,tab_enabled=bool(visible),action_enqueued=queued,action_record_hex=struct.pack('<4IH',5,selected_click,0,0x72,7).hex() if queued else None,
      queue128_expected_hex=bytes(wanted_session[0x1C:0x1C+128]).hex(),output_expected_hex=wanted_out.hex(),checks=[dict(address=hex(a),bytes=len(b)) for a,_,b in fixed])

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,e=build(c);p=ROOT/'ghidra'/f'phase84-emulation-keeper-ui-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
            else:
                response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c['name'],r,e)
            assert memory_readout.decode(r['registers']).hex()==e['queue128_expected_hex'];rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']))
            print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase84-keeper-ui-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
