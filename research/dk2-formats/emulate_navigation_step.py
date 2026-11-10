"""Whole4D5A40→4D4760 with original map callback/collision predicate.

Native4D27D0 installs original4D16B0+preparedWorld,not a syntheticcallback.
Finite empty force-provider array is explicit;no force callbacks patched out.
Four cases additionallyexecuteoriginalacceptanceandactor/mapapplication.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_neighbours as nb
import native_navigation_step as api
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
f=nb.f;cf=nb.cf;ROOT=Path(__file__).resolve().parent
CALLBACKS=0x6ed408;FORCES=0x6ecae0;FLAGS=0x6ec9d0;CTX=cf.ACTOR+0x124
POSITION=0x10700000

def cases():
    base=dict(state='possessed',base=1000,percent=200,position=[0x5800,0x6800,0x2000],velocity=[4096,0],nav=[])
    changes=[('open',{}),('blocked',dict(nav=[[6,6,8]])),('normal',dict(state='normal')),('secondary',dict(state='secondary')),
      ('cap-above',dict(base=1366)),('zero-max-speed',dict(base=0)),('zero-velocity',dict(velocity=[0,0])),
      ('negative-X',dict(velocity=[-4096,0])),('negative-X-blocked',dict(velocity=[-4096,0],nav=[[4,6,8]])),
      ('vertical',dict(velocity=[0,4096])),('vertical-blocked',dict(velocity=[0,4096],nav=[[5,7,8]])),
      ('diagonal',dict(velocity=[4096,4096])),('diagonal-slide',dict(velocity=[4096,4096],nav=[[6,7,8]])),
      ('bounds-stop',dict(position=[0x1f800,0x6800,0x2000])),
      ('nav1-stop',dict(nav=[[6,6,1]])),('nav1-extra-pass',dict(nav=[[6,6,1]],extra=True)),
      ('nav3-stop',dict(nav=[[6,6,3]])),('nav3-definition-pass',dict(nav=[[6,6,3]],definition_bit5=True)),
      ('nav4-stop',dict(nav=[[6,6,4]])),('nav4-bit18-pass',dict(nav=[[6,6,4]],bit18=True)),
      ('owned14-stop',dict(nav=[[6,6,0x14]],owner_index=1)),('owned14-bit17-pass',dict(nav=[[6,6,0x14]],owner_index=1,bit17=True)),
      ('other14-pass',dict(nav=[[6,6,0x14]],owner_index=2)),('other14-bit19-stop',dict(nav=[[6,6,0x14]],owner_index=2,bit19=True)),
      ('owned10-bit19-pass',dict(nav=[[6,6,0x10]],owner_index=1,bit19=True)),
      ('other22-stop',dict(nav=[[6,6,0x22]],owner_index=1,bit17=True)),
      ('apply-open',dict(apply=True)),('apply-blocked',dict(apply=True,nav=[[6,6,8]])),
      ('apply-normal',dict(apply=True,state='normal')),('apply-zero-blocked-current',dict(apply=True,base=0,nav=[[5,6,8]])),
      ('preflight-nav1-off',dict(preflight=True,nav=[[6,6,1]])),
      ('preflight-nav1-on',dict(preflight=True,nav=[[6,6,1]],definition_bit4=True))]
    return [dict(base,**v,name=n) for n,v in changes]

def build(c):
    e=api.outcome(c);source=dict(nb.cases()[0],percent=c['percent'],source_possessed=c['state'] in ['secondary','both'],target_possessed=c['state'] in ['possessed','both'],nav=c['nav'])
    args,old=nb.build(source);allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for row in old['checks']:
        checks[int(row['address'],16)]=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if (a<0x11000000 or a==0x7ffee000) and a!=f.OBS}
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    both(cf.DEFINITION,0x5b4,c['base'])
    for o,v in zip([0x1a,0x1e,0x22],c['position']):both(cf.ACTOR,o,v)
    start_cell=(c['position'][1]>>12)*32+(c['position'][0]>>12)
    if start_cell!=6*32+5:
        both(f.MAP,16*(6*32+5),0,2);both(f.MAP,16*start_cell,100,2)
    for actor in [memory[cf.ACTOR],checks[cf.ACTOR]]:
        flags=int.from_bytes(actor[0x3e6:0x3ea],'little')
        for bit in [17,18,19]:flags=(flags&~(1<<bit))|((1<<bit) if c.get('bit'+str(bit)) else 0)
        f.put(actor,0x3e6,flags)
    both(f.PLAYER,0x10fc,c.get('owner_index',0),1)
    def_flags=int.from_bytes(memory[cf.DEFINITION][0x686:0x68a],'little');both(cf.DEFINITION,0x686,(def_flags&~48)|(32 if c.get('definition_bit5') else 0)|(16 if c.get('definition_bit4') else 0))
    f.put(checks[cf.ACTOR],0x11c,e['word11c'],2);f.put(checks[cf.ACTOR],0x206,e['controller_max_speed'])
    # Neutral explicit controller flags avoid a pendingpath/end-of-path branch.
    for o,v,n in [(0x10c,0,1),(0x10d,0,1),(0x10e,0x40,1),(0x10a,0,2),(0x106,0,4),(0xfa,0,4)]:both(cf.ACTOR,0x124+o,v,n)
    for o,values in [(0x84,e['position']),(0x8c,e['velocity']),(0x94,e['normal']),(0x9c,e['perpendicular'])]:
        for i,v in enumerate(values):f.put(checks[cf.ACTOR],0x124+o+4*i,v)
    f.put(checks[cf.ACTOR],0x124+0x10a,e['history'],2);f.put(checks[cf.ACTOR],0x124+0x106,e['progress'])
    memory[CALLBACKS]=bytearray([0xa5])*20;checks[CALLBACKS]=bytearray(memory[CALLBACKS]);f.put(checks[CALLBACKS],0,0x4d16b0);f.put(checks[CALLBACKS],16,f.WORLD)
    memory[FORCES]=bytearray(8);checks[FORCES]=bytearray(8)
    initial_flags=bytearray(24);f.put(initial_flags,20,int(c.get('extra',False)));memory[FLAGS]=initial_flags;checks[FLAGS]=bytearray(initial_flags)
    if c.get('preflight'):f.put(checks[FLAGS],20,int(c.get('definition_bit4',False)))
    if not e['skipped']:
        for i,v in enumerate([c.get('owner_index',0),int(c.get('bit17',False)),int(c.get('bit18',False)),int(c.get('definition_bit5',False)),int(c.get('bit19',False))]):f.put(checks[FLAGS],i*4,v)
    memory[f.OUT]=bytearray(32)+bytearray([0xa5])*32;checks[f.OUT]=bytearray(struct.pack('<8I',1,e['getter'],e['word11c'],e['controller_max_speed'],*e['xy'],*e['velocity_q12']))+bytearray([0xa5])*32
    if c.get('preflight'):memory[f.OUT][36:40]=bytes(4);f.put(checks[f.OUT],36,e['preflight'])
    if c.get('apply'):
        memory[f.OUT][32:36]=bytes(4);f.put(checks[f.OUT],32,e['apply_accepted'])
        memory[POSITION]=bytearray(12)+bytearray([0xa5])*64;checks[POSITION]=bytearray(struct.pack('<3I',*e['xy'],c['position'][2]))+bytearray([0xa5])*64
        if e['apply_accepted']:
            for o,v in zip([0x1a,0x1e,0x22],e['actor_position']):f.put(checks[cf.ACTOR],o,v)
            oldcell=start_cell;newcell=(e['xy'][1]>>12)*32+(e['xy'][0]>>12)
            if oldcell!=newcell:
                f.put(checks[f.MAP],16*oldcell,0,2);f.put(checks[f.MAP],16*newcell,100,2)
    driver=memory[nb.DRIVER];assert driver[-6:]==b'\x68'+pack32(f.OBS)+b'\xc3';del driver[-6:]
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(nb.DRIVER+len(driver)+5)))
    emit(b'\x50\xb9'+pack32(cf.ACTOR)+bytes.fromhex('8b11ff928c000000'))
    emit(b'\xb9'+pack32(cf.ACTOR)+bytes.fromhex('8b11ff9290000000a3')+pack32(f.OUT+8))
    emit(b'\xa1'+pack32(cf.ACTOR+0x206)+b'\xa3'+pack32(f.OUT+12))
    emit(b'\x68'+pack32(f.WORLD)+b'\x68'+pack32(0x4d16b0));call(0x4d27d0);emit(bytes.fromhex('83c408'))
    if c.get('preflight'):
        for v in [int(c.get('bit18',False)),int(c.get('bit17',False)),6,6]:emit(b'\x68'+pack32(v))
        emit(b'\xb9'+pack32(cf.ACTOR));call(0x48ebf0);emit(b'\xa3'+pack32(f.OUT+36))
    for v in [c['velocity'][1],c['velocity'][0],c['position'][1],c['position'][0]]:emit(b'\x68'+pack32(v))
    emit(b'\xb9'+pack32(CTX));call(0x4d59a0)
    emit(b'\x68'+pack32(cf.ACTOR)+b'\xb9'+pack32(CTX));call(0x4d5a40)
    for target,offset in [(0x4d5ac0,16),(0x4d5af0,24)]:
        emit(b'\x68'+pack32(f.OUT+offset+4)+b'\x68'+pack32(f.OUT+offset)+b'\xb9'+pack32(CTX));call(target)
    if c.get('apply'):
        for source_address,target_address in [(f.OUT+16,POSITION),(f.OUT+20,POSITION+4),(cf.ACTOR+0x22,POSITION+8)]:emit(b'\xa1'+pack32(source_address)+b'\xa3'+pack32(target_address))
        emit(b'\x68'+pack32(POSITION));call(0x4b35c0);emit(b'\xa3'+pack32(f.OUT+32)+bytes.fromhex('85c0740f'))
        emit(b'\x68'+pack32(POSITION)+b'\xb9'+pack32(cf.ACTOR));call(0x4b5560)
    emit(b'\x68'+pack32(f.OBS)+b'\xc3')
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+16)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=mem));e['checks']=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()];return args,e

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);key=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:10];path=ROOT/'ghidra'/f'phase98-navigation-step-{c["name"]}-{key}.json'
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['xy'][0]).items()),(c,r,{k:v for k,v in e.items() if k!='checks'})
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'XY',e['xy'],'velocity',e['velocity_q12'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase98-navigation-step-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
