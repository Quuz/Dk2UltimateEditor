"""Original Terrain cache→wholeCMap placement→preflight→wholemovement step.

Loading-mode World7239/40 selects original4D1970,without graph regeneration.
All original callbacks/vtables retained. Explicit null resources in raw cases.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_navigation_step as step
import native_navigation_map as api
import native_terrain_catalogue as terrain
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent;f=step.f;cf=step.cf
TABLE=0x10800000;RAW=0x10810000;MID=0x10820000;DRIVER=0x10830000

def cases():
    base=dict(flags=0,water=False,lava=False,raw=False,initial_nav=0)
    changes=[('solid',dict(flags=1)),('ground',{}),('water-blocked',dict(flags=0x80)),('water-allowed',dict(flags=0x80,water=True)),
      ('lava-blocked',dict(flags=0x100)),('lava-allowed',dict(flags=0x100,lava=True)),
      ('both-liquid-water-first',dict(flags=0x180,water=True)),('both-liquid-lava-only-blocked',dict(flags=0x180,lava=True)),
      ('solid-water-first',dict(flags=0x81,water=True)),('solid-both',dict(flags=0x181,water=True,lava=True)),
      ('overwrite-solid-ground',dict(initial_nav=8)),('unchanged-ground',dict(initial_nav=0)),
      ('raw-ground',dict(raw=True)),('raw-solid',dict(raw=True,flags=1)),
      ('raw-water-blocked',dict(raw=True,flags=0x80)),('raw-water-allowed',dict(raw=True,flags=0x80,water=True)),
      ('raw-lava-blocked',dict(raw=True,flags=0x100)),('raw-lava-allowed',dict(raw=True,flags=0x100,lava=True)),
      ('raw-both-water-first',dict(raw=True,flags=0x180,water=True)),('raw-solid-both',dict(raw=True,flags=0x181,water=True,lava=True))]
    return [dict(base,**v,name=n) for n,v in changes]

def build(c):
    category=api.terrain_navigation_byte(c['flags'])
    sc=dict(step.cases()[0],nav=[[6,6,category]],preflight=category==1,definition_bit4=c['water'],definition_bit5=c['lava'])
    args,e=step.build(sc);e['terrain_navigation_byte']=category;e['raw_terrain']=c['raw']
    allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for r in e['checks']:checks[int(r['address'],16)]=allmem[shadow];shadow+=((r['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if (a<0x11000000 or a==0x7ffee000) and a!=f.OBS}
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    # Real embedded CMap and original World virtual methods, not proxy vtables.
    both(f.WORLD,0x7239,0x40,1);both(f.WORLD,0x7243,0xffff,2)
    for o,v in [(0x15a4,TABLE),(0x15a8,8),(0x15ac,2)]:both(f.WORLD,o,v)
    both(f.WORLD,0x9f6,0)
    f.put(checks[f.WORLD],0x9f6,int.from_bytes(memory[f.SESSION][4:8],'little'))
    for y in range(32):
        for x in range(32):both(f.MAP,16*(y*32+x)+10,1,1)
    target=16*(6*32+6);f.put(checks[f.MAP],target+10,7,1);f.put(checks[f.MAP],target+6,0x3456,2)
    memory[step.nb.BITS][6*32+6]=c['initial_nav']
    table=bytearray(460*8);table[460+0x1c4]=1;table[7*460+0x1c4]=7
    struct.pack_into('<I',table,7*460+0x17b,c['flags']);struct.pack_into('<H',table,7*460+0x1ab,0x3456)
    memory[TABLE]=table+bytearray([0xa5])*64;checks[TABLE]=bytearray(memory[TABLE])
    cache=bytearray(1024);struct.pack_into('<I',cache,4,TABLE+460);struct.pack_into('<I',cache,28,TABLE+7*460)
    memory[0x6da4a8]=bytearray([0xa5])*1024;checks[0x6da4a8]=cache
    for a,b in [(0x6da4a4,pack32(f.WORLD)),(0x6f2548,bytes(2))]:memory[a]=bytearray(b);checks[a]=bytearray(b)
    if c['raw']:
        raw=bytearray(552);raw[:8]=b'navmap\0\0';raw[0x1d6]=7;struct.pack_into('<I',raw,0x190,c['flags']);struct.pack_into('<H',raw,0x1d7,0x3456)
        mid=terrain.intermediate(raw);definition,_=terrain.definition(mid,bytes(460),1)
        memory[TABLE][7*460:8*460]=bytes(460);checks[TABLE][7*460:8*460]=definition
        for a,initial,wanted in [(RAW,raw,raw),(MID,bytes([0xa5])*736,mid)]:memory[a]=bytearray(initial)+bytearray([0xa5])*64;checks[a]=bytearray(wanted)+bytearray([0xa5])*64
        memory[0x6fcdcf]=bytearray(pack32(1));checks[0x6fcdcf]=bytearray(pack32(1))
    driver=bytearray(b'\x50') # preserve raw224 callback result for inherited driver
    def emit(b):driver.extend(b)
    def call(a):emit(b'\xe8'+pack32(a-(DRIVER+len(driver)+5)))
    if c['raw']:
        emit(b'\xb9'+pack32(MID));call(0x56eb60)
        emit(b'\x68'+pack32(RAW)+b'\xb9'+pack32(MID));call(0x56ed20)
        emit(b'\x68'+pack32(0)+b'\x68'+pack32(MID)+b'\xb9'+pack32(TABLE+7*460));call(0x4f66e0)
    emit(b'\x68'+pack32(f.WORLD));call(0x455d40);emit(bytes.fromhex('83c404'))
    for v in [0,0,0,7,6,6]:emit(b'\x68'+pack32(v))
    emit(b'\xb9'+pack32(f.WORLD+0x9da));call(0x44fc40)
    emit(b'\x58\x68'+pack32(step.nb.DRIVER)+b'\xc3');memory[DRIVER]=driver;f.put(memory[0x7ffee000],8192,DRIVER)
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
                args,e=build(c);key=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:10];path=ROOT/'ghidra'/f'phase99-navigation-map-{c["name"]}-{key}.json'
                if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['xy'][0]).items()),(c,r,{k:v for k,v in e.items() if k!='checks'})
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'nav',e['terrain_navigation_byte'],'XY',e['xy'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase99-navigation-map-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
