"""Raw224/Health and65/HealthLevel→real setter→complete native4B2CE0.

Original World→Session event send executes; external bridge vslot28 captures
the16-byte session command. Actor/world are prepared from verified native
phase90 data via phase92 fixture. No original game function/vtable patches.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_speed as sp
import emulate_creature_factory as cf
import emulate_object_factory as f
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
CAP=0x10200000;EVENT=0x10210000;RAW65=0x10220000;RAW239=0x10230000
def cases():
    base=dict(possessed=True,hp=1000,delta=-100,health_percent=100,level=1,coefficient=100,scalars=[6,6,6],period=0,source_arg=7)
    return [dict(base,**v,name=n) for n,v in [('possessed-damage',{}),('normal-damage',dict(possessed=False)),('possessed-heal-cap',dict(hp=950,delta=200)),
      ('possessed-odd',dict(delta=-101)),('normal-odd',dict(possessed=False,delta=-101)),('possessed-one',dict(delta=-1)),('normal-one',dict(possessed=False,delta=-1)),
      ('possessed-hp1',dict(delta=-1999)),('heal-below-cap',dict(hp=900,delta=50)),('heal-at-cap',dict(delta=200)),('zero-delta',dict(delta=0)),
      ('level2-cap150',dict(hp=1400,delta=200,level=2,coefficient=150)),('level2-cap200',dict(hp=1900,delta=200,level=2,coefficient=200)),
      ('type224-health0',dict(health_percent=0)),('type224-health-ffff',dict(health_percent=0xffff)),
      ('scalar-zero',dict(scalars=[0,0,0])),('scalar-high',dict(scalars=[30000,30000,30000])),('period20',dict(period=20)),
      ('word447-possessed',dict(word447=1)),('word447-normal',dict(possessed=False,word447=1)),
      ('word443-normal',dict(possessed=False,word443=1)),('word443-possessed',dict(word443=1)),
      ('owner-reduction2',dict(owner1095=2)),('owner-reduction127',dict(owner1095=127)),
      ('over-cap-heal-possessed',dict(hp=1500,delta=200)),('over-cap-heal-normal',dict(possessed=False,hp=1500,delta=200)),
      ('pit50-normal',dict(possessed=False,pit_percent=50)),('pit50-possessed',dict(pit_percent=50)),
      ('heal-cap-type224-zero',dict(hp=950,delta=200,health_percent=0)),('heal-cap-type224-high',dict(hp=950,delta=200,health_percent=30000)),
      ('heal-cap-scalars-high',dict(hp=950,delta=200,scalars=[30000,30000,30000]))]]
def outcome(c):
    cap=1000 if c['level']==1 else 1000*c['coefficient']//100;delta=c['delta'];early=delta==0
    if delta>0 and c['hp']+delta>cap:delta=cap-c['hp'];early=delta==0
    scale=128
    if delta<0:
        if c.get('word447'):scale=160
        if c.get('word443') or c['possessed']:scale-=64
        if c.get('owner1095',0)>0:scale-=8*c['owner1095']
        if 'pit_percent' in c:scale+=((c['pit_percent']*128//100)&65535)-128
        scale=max(0,scale)
    product=(scale*delta)&0xffffffff;product=product if product<0x80000000 else product-0x100000000
    scaled=abs(product)//128*(-1 if product<0 else 1)
    result=c['hp']+scaled;assert result>0
    return dict(hp=result,early=early,cap=cap,event=int(c['possessed'] and delta<0),timestamp=100+(c['period']*3>>2))
def build(c):
    source=dict(sp.cases()[0],possessed=c['possessed'],raw_index=1,percent=c['health_percent'],scalars=c['scalars']);args,old=sp.build(source)
    allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for r in old['checks']:checks[int(r['address'],16)]=allmem[shadow];shadow+=((r['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if a<0x11000000 and a not in [f.OBS,sp.DRIVER,0x7ffee000]};e=outcome(c)
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    both(cf.ACTOR,0x40,c['hp']);both(cf.ACTOR,0x403,c['level'],1);both(cf.DEFINITION,0x86f,123,2)
    for key,off in [('word443',0x443),('word447',0x447)]:
        if key in c:both(cf.ACTOR,off,c[key],2)
    if 'owner1095' in c:both(f.PLAYER,0x1095,c['owner1095'],1)
    if 'pit_percent' in c:
        for b in [memory[cf.ACTOR],checks[cf.ACTOR]]:
            flags=int.from_bytes(b[0x3e6:0x3ea],'little');f.put(b,0x3e6,flags|0x8000)
        memory[RAW239]=bytearray(bytes(16)+struct.pack('<4I',239,c['pit_percent'],0x11223344,0x55667788));checks[RAW239]=bytearray(memory[RAW239])
        f.put(checks[sp.VARS],0x6f29f6-sp.VARS,c['pit_percent']*128//100,2)
    f.put(checks[cf.ACTOR],0x40,e['hp']);f.put(checks[cf.ACTOR],0x36,c['source_arg'],2)
    if not e['early']:
        for o in [0x38,0x3c]:f.put(checks[cf.ACTOR],o,e['timestamp'])
    # Native type65 callback sets the same packed odd WORD read by health cap.
    raw=struct.pack('<4I',65,1,c['coefficient'],c['level']);memory[RAW65]=bytearray(bytes(16)+raw);checks[RAW65]=bytearray(memory[RAW65])
    off=(0x6f261d+(9+c['level'])*2+2)-sp.VARS;f.put(checks[sp.VARS],off,c['coefficient'],2)
    session=bytearray(memory[f.SESSION]);session.extend(bytes(0x280-len(session)));f.put(session,8,c['period']);memory[f.SESSION]=session;checks[f.SESSION]=bytearray(session);f.put(checks[f.SESSION],0x26c,e['event'])
    both(f.GV,0x28,EVENT)
    memory[CAP]=bytearray(24);checks[CAP]=bytearray(24)
    if e['event']:struct.pack_into('<5I',checks[CAP],0,1,7,8,123,0x12)
    # External session bridge method: preserve callee-saved ESI/EDI, copy payload.
    provider=bytes.fromhex('56578b74240c')+b'\xbf'+pack32(CAP+4)+b'\xb9'+pack32(4)+bytes.fromhex('f3a5ff05')+pack32(CAP)+bytes.fromhex('5f5eb801000000c20400');memory[EVENT]=bytearray(provider)
    memory[f.OUT]=bytearray(12)+bytearray([0xa5])*20;checks[f.OUT]=bytearray(struct.pack('<3I',1,e['hp'],1))+bytearray([0xa5])*20
    driver=bytearray(b'\xa3'+pack32(f.OUT))
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(sp.DRIVER+len(driver)+5)))
    emit(b'\x68'+pack32(RAW65));call(0x51dc40);emit(b'\xa3'+pack32(f.OUT+8))
    if 'pit_percent' in c:emit(b'\x68'+pack32(RAW239)+b'\xb9'+pack32(f.WORLD));call(0x51cd90)
    if c['possessed']:emit(b'\x68'+pack32(1)+b'\xb9'+pack32(cf.ACTOR));call(0x48dea0)
    emit(b'\x68'+pack32(0)+b'\x68'+pack32(c['source_arg'])+b'\x68'+pack32(c['delta'])+b'\xb9'+pack32(cf.ACTOR));call(0x4b2ce0)
    emit(b'\xa3'+pack32(f.OUT+4)+b'\x68'+pack32(f.OBS)+b'\xc3');memory[sp.DRIVER]=driver
    stack=bytearray([0xa5])*16384;struct.pack_into('<2I',stack,8192,sp.DRIVER,sp.RAW);memory[0x7ffee000]=stack
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    args.update(memory=json.dumps(dict(regions=mem)));e['checks']=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()]
    return args,e
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);path=ROOT/'ghidra'/f'phase93-possession-health-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['hp']).items()),(c,r,e)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],e['hp'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase93-possession-health-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
