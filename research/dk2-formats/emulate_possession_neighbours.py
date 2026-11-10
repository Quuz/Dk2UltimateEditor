"""Raw224→real source possession setter→4BFD00/XY-ray→full target speed.

Two explicitly prepared actors and finite owner list reuse phase90 native actor
data. Original ray/tile/map code, vtables and game callbacks are unchanged.
Nav bitmap and Door definition are prepared runtime inputs, not raw loaders.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_speed as sp
import native_possession_neighbours as api
import native_door_loading as door_api
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
f=sp.f;cf=sp.cf;ROOT=Path(__file__).resolve().parent
NAV=0x10400000;BITS=0x10410000;SOURCE=0x10420000;DRIVER=0x10430000;DOOR=0x10440000;DOORDEF=0x10450000
RAWDOOR=0x10460000;MIDDOOR=0x10470000

def cases():
    base=dict(source_pos=[0x5800,0x6800,0x2000],target_pos=[0x8800,0x6800,0x2000],source_possessed=True,
      initial_flag=False,initial_source_flag=False,mode=0,percent=200,scalars=[6,6,6],nav=[],doors=[],line_clear=True,list_mode='both')
    changes=[('open',{}),('blocked',dict(nav=[[7,6,8]],initial_flag=True,line_clear=False)),
      ('same-cell-blocked',dict(target_pos=[0x5900,0x6900,0],nav=[[5,6,8]])),
      ('vertical-open',dict(target_pos=[0x5800,0xa800,0x2000])),
      ('vertical-blocked',dict(target_pos=[0x5800,0xa800,0x2000],nav=[[5,8,8]],line_clear=False)),
      ('reverse-open',dict(target_pos=[0x2800,0x6800,0x2000])),
      ('reverse-blocked',dict(target_pos=[0x2800,0x6800,0x2000],nav=[[4,6,8]],line_clear=False)),
      ('diagonal-open',dict(target_pos=[0x9800,0xa800,0x2000])),
      ('diagonal-blocked',dict(target_pos=[0x9800,0xa800,0x2000],nav=[[7,8,8]],line_clear=False)),
      ('far-open',dict(source_pos=[0x1800,0x1800,0],target_pos=[0x1e800,0x1e800,0])),
      ('different-Z',dict(target_pos=[0x8800,0x6800,0x7fffffff])),
      ('source-cell-blocked',dict(nav=[[5,6,8]])),
      ('target-cell-blocked',dict(nav=[[8,6,8]],line_clear=False)),
      ('nav-high-empty',dict(nav=[[7,6,0x10]])),
      ('door-blocked',dict(nav=[[7,6,0x10]],doors=[[7,6,0]],line_clear=False)),
      ('door-flag40',dict(nav=[[7,6,0x10]],doors=[[7,6,0x40]])),
      ('door-no-nav-high',dict(doors=[[7,6,0]])),
      ('run',dict(mode=1)),('shuffle',dict(mode=2)),
      ('percent0',dict(percent=0)),('percent100',dict(percent=100)),('percent-highbits',dict(percent=0xabcd00c8)),
      ('scalar0',dict(scalars=[0,0,0])),('scalar-high',dict(scalars=[30000,30000,30000])),
      ('clear-unpossessed',dict(source_possessed=False,initial_flag=True,initial_source_flag=True)),
      ('skip-source',dict(initial_source_flag=True)),
      ('empty-list',dict(list_mode='empty',initial_flag=True)),
      ('source-only-list',dict(list_mode='source-only',initial_flag=True)),
      ('target-also-possessed',dict(target_possessed=True)),
      ('current-record',dict(source_current=True)),
      ('raw-door-blocked',dict(nav=[[7,6,0x10]],doors=[[7,6,0]],raw_door=True,line_clear=False)),
      ('raw-door-flag40',dict(nav=[[7,6,0x10]],doors=[[7,6,0x40]],raw_door=True)),
      ('raw-current-door-blocked',dict(nav=[[7,6,0x10]],doors=[[7,6,0]],raw_door=True,current_door=True,line_clear=False)),
      ('raw-current-door-flag40',dict(nav=[[7,6,0x10]],doors=[[7,6,0x40]],raw_door=True,current_door=True))]
    return [dict(base,**v,name=n) for n,v in changes]

def expected(c):
    target=api.flag_after(8 if c['initial_flag'] else 0,source_possessed=c['source_possessed'],is_source=False,line_clear=c['line_clear'])
    source=api.flag_after(8 if c['initial_source_flag'] else 0,source_possessed=c['source_possessed'],is_source=True,line_clear=False)
    if c['list_mode']!='both':target=8 if c['initial_flag'] else 0
    if c['list_mode']=='empty':source=8 if c['initial_source_flag'] else 0
    speed_case=dict(sp.cases()[0],possessed=c.get('target_possessed',False),other_flag=bool(target&8),mode=c['mode'],percent=c['percent'])
    return dict(target_bit08=target,source_bit08=source,speed=sp.expected_speed(speed_case))

def build(c):
    e=expected(c);sc=dict(sp.cases()[0],possessed=False,other_flag=False,mode=c['mode'],percent=c['percent'],scalars=c['scalars'],source_current=c.get('source_current',False))
    args,old=sp.build(sc);allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for row in old['checks']:
        checks[int(row['address'],16)]=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if (a<0x11000000 or a==0x7ffee000) and a not in [f.OBS,sp.DRIVER]}
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    for o,v in zip([0x1a,0x1e,0x22],c['target_pos']):both(cf.ACTOR,o,v)
    # Bit08 follows the update; other actor bits must remain unchanged.
    initial=int.from_bytes(memory[cf.ACTOR][0x3ea:0x3ee],'little')
    both(cf.ACTOR,0x3ea,(initial&~8)|(8 if c['initial_flag'] else 0));f.put(checks[cf.ACTOR],0x3ea,(initial&~8)|e['target_bit08'])
    source=bytearray(memory[cf.ACTOR]);f.put(source,4,101,2);f.put(source,0x10,100 if c['list_mode']=='both' else 0,2)
    for o,v in zip([0x1a,0x1e,0x22],c['source_pos']):f.put(source,o,v)
    f.put(source,0x3ea,(initial&~8)|(8 if c['initial_source_flag'] else 0));memory[SOURCE]=source;checks[SOURCE]=bytearray(source)
    f.put(checks[SOURCE],0x3ea,(initial&~8)|e['source_bit08'])
    both(cf.ACTOR,0x10,0,2);both(f.PLAYER,6,0 if c['list_mode']=='empty' else 101,2)
    for b in [memory[0x6ee540],checks[0x6ee540]]:f.put(b,101*4,SOURCE)
    for b in [memory[0x6ed540],checks[0x6ed540]]:b[101]=1
    # Real ray ECX=World+9DA uses World, row width, tile list and NAV bitmap.
    both(f.WORLD,0x9e2,f.WORLD);both(f.WORLD,0x9ea,32);both(f.WORLD,0x9f2,NAV)
    nav=bytearray(32);f.put(nav,8,32);f.put(nav,16,BITS);bits=bytearray(1024)
    for x,y,v in c['nav']:assert 0<=x<32 and 0<=y<32;bits[32*y+x]=v
    for a,b in [(NAV,nav),(BITS,bits)]:memory[a]=b;checks[a]=bytearray(b)
    for x,y,flags in c['doors']:
        assert len(c['doors'])==1;door=bytearray(0x180);f.put(door,4,102,2);f.put(door,0x12,4);f.put(door,0xf7,DOORDEF)
        definition=bytearray(0xb5);definition[0x8b]=flags
        for a,b in [(DOOR,door),(DOORDEF,definition)]:memory[a]=b;checks[a]=bytearray(b)
        for b in [memory[0x6ee540],checks[0x6ee540]]:f.put(b,102*4,DOOR)
        for b in [memory[0x6ed540],checks[0x6ed540]]:b[102]=1
        both(f.MAP,16*(32*y+x),102,2)
    if c.get('raw_door'):
        raw=bytes(616)
        if c.get('current_door'):
            doc=json.loads((ROOT/'decoded/Doors.kwd.json').read_text(encoding='utf8'));data=Path(doc['source']).read_bytes();assert hashlib.sha256(data).hexdigest()==doc['sha256']
            row=doc['sections'][0]['records'][0];raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+len(raw)]==raw
        raw=api.patch_door_bit40(raw,bool(c['doors'][0][2]&0x40));mid=door_api.intermediate(raw,bytes(732))
        previous=bytes(181);definition=door_api.definition(mid,previous,door_api.DX,gate=1)
        assert bool(definition[0x8b]&0x40)==bool(c['doors'][0][2]&0x40)
        for a,initial,wanted in [(RAWDOOR,raw,raw),(MIDDOOR,bytes([0xa5])*732,mid),(DOORDEF,previous,definition)]:
            memory[a]=bytearray(initial)+bytearray([0xa5])*64;checks[a]=bytearray(wanted)+bytearray([0xa5])*64
        memory[0x6fcdcf]=bytearray(pack32(1));checks[0x6fcdcf]=bytearray(pack32(1))
    driver=bytearray(b'\xa3'+pack32(f.OUT))
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(DRIVER+len(driver)+5)))
    def possess(actor):
        emit(b'\x68'+pack32(1)+b'\xb9'+pack32(actor));call(0x48dea0)
        flags=int.from_bytes(checks[actor][0x3e6:0x3ea],'little');f.put(checks[actor],0x3e6,flags|0x80)
        f.put(checks[actor],0x488,0);f.put(checks[actor],0x3ee,0)
    if c.get('raw_door'):
        emit(b'\xb9'+pack32(MIDDOOR));call(0x56a920)
        emit(b'\x68'+pack32(RAWDOOR)+b'\xb9'+pack32(MIDDOOR));call(0x56aa40)
        emit(b'\x68'+pack32(0)+b'\x68'+pack32(MIDDOOR)+b'\xb9'+pack32(DOORDEF));call(0x4a7100)
    if c['source_possessed']:possess(SOURCE)
    if c.get('target_possessed'):possess(cf.ACTOR)
    emit(b'\x68'+pack32(SOURCE)+b'\xb9'+pack32(f.PLAYER));call(0x4bfd00)
    emit(b'\x68'+pack32(c['mode'])+b'\xb9'+pack32(cf.ACTOR));call(0x48abb0);emit(b'\xa3'+pack32(f.OUT+4))
    emit(b'\x68'+pack32(f.OBS)+b'\xc3');memory[DRIVER]=driver;f.put(memory[0x7ffee000],8192,DRIVER)
    f.put(checks[f.OUT],4,e['speed'])
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=mem));e['checks']=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()]
    return args,e

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);path=ROOT/'ghidra'/f'phase95-possession-neighbours-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['speed']).items()),(c,r,e)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'flag',e['target_bit08'],'speed',e['speed'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase95-possession-neighbour-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
