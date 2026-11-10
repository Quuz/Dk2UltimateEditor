"""Raw224→actualflagproducer/speedgetter→virtualcap→nativevectorlimiter.

Nativefunctionscomposedbyexplicitdriver;fullCreaturetick/controllerstepnotrun.
Staticoriginaltick/controllercodebindsthesamecapandmax_speedfieldtotheirpaths.
Nooriginalgamecode,vtable,tableorcallbackpatches;phase90preparedactorsreused.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_neighbours as nb
import native_navigation_speed as api
import native_possession_stats as stat
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
f=nb.f;cf=nb.cf;ROOT=Path(__file__).resolve().parent
VIN=0x10600000;VOUT=0x10610000

def cases():
    base=dict(state='possessed',base=1000,percent=200,mode=0,vector=[65536,0])
    changes=[('possessed',{}),('normal',dict(state='normal')),('secondary',dict(state='secondary')),('both',dict(state='both')),
      ('pos-cap-equal',dict(base=1365)),('pos-cap-above',dict(base=1366)),
      ('secondary-cap-equal',dict(state='secondary',base=2730)),('secondary-cap-above',dict(state='secondary',base=2731)),
      ('normal-cap-above',dict(state='normal',base=4096)),('base-zero',dict(base=0)),
      ('percent0',dict(percent=0)),('secondary-percent0',dict(state='secondary',percent=0)),
      ('percent100',dict(percent=100)),('secondary-percent100',dict(state='secondary',percent=100)),
      ('percent-minus1',dict(percent=0xffffffff)),('percent-min16',dict(percent=0x8000)),('percent-max16',dict(percent=0x7fff)),
      ('percent-highbits',dict(percent=0xabcd00c8)),('base-product-wrap',dict(base=0xffffffff)),
      ('vertical-vector',dict(vector=[0,65536])),('negative-vector',dict(vector=[-65536,0])),
      ('diagonal-vector',dict(vector=[65536,65536])),('small-vector',dict(vector=[100,100])),('zero-vector',dict(vector=[0,0])),
      ('run',dict(mode=1)),('shuffle',dict(mode=2)),('current-record',dict(source_current=True))]
    return [dict(base,**v,name=n) for n,v in changes]

def expected(c):
    raw=stat.speed_modifier(c['base'],c['percent'],c['state'] in ['possessed','both'],c['state'] in ['secondary','both'])
    e=dict(getter=raw,**api.instance_cap(raw));e['limited_vector']=api.truncate_vector(c['vector'],e['controller_max_speed']);return e

def build(c):
    e=expected(c);source=dict(nb.cases()[0],percent=c['percent'],source_possessed=c['state'] in ['secondary','both'],target_possessed=c['state'] in ['possessed','both'],mode=c['mode'],source_current=c.get('source_current',False))
    args,old=nb.build(source);allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for row in old['checks']:
        checks[int(row['address'],16)]=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if (a<0x11000000 or a==0x7ffee000) and a!=f.OBS}
    off=[0x5b4,0x5b8,0x5bc][c['mode']]
    for b in [memory[cf.DEFINITION],checks[cf.DEFINITION]]:f.put(b,off,c['base'])
    f.put(checks[cf.ACTOR],0x11c,e['word11c'],2);f.put(checks[cf.ACTOR],0x206,e['controller_max_speed'])
    vin=struct.pack('<2I',*(v&0xffffffff for v in c['vector']));vout=bytes(8)
    memory[VIN]=bytearray(vin)+bytearray([0xa5])*64;checks[VIN]=bytearray(memory[VIN])
    memory[VOUT]=bytearray(vout)+bytearray([0xa5])*64;checks[VOUT]=bytearray(struct.pack('<2I',*e['limited_vector']))+bytearray([0xa5])*64
    memory[f.OUT]=bytearray(20)+bytearray([0xa5])*12;checks[f.OUT]=bytearray(struct.pack('<5I',1,e['getter'],e['word11c'],e['controller_max_speed'],VOUT))+bytearray([0xa5])*12
    driver=memory[nb.DRIVER];assert driver[-6:]==b'\x68'+pack32(f.OBS)+b'\xc3';del driver[-6:]
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(nb.DRIVER+len(driver)+5)))
    # EAXholdsreal48ABB0getterresult;invokeoriginalCreaturevslots8C/90.
    emit(b'\x50\xb9'+pack32(cf.ACTOR)+bytes.fromhex('8b11ff928c000000'))
    emit(b'\xb9'+pack32(cf.ACTOR)+bytes.fromhex('8b11ff9290000000a3')+pack32(f.OUT+8))
    emit(bytes.fromhex('a1')+pack32(cf.ACTOR+0x206)+b'\xa3'+pack32(f.OUT+12))
    emit(b'\x50\x68'+pack32(VIN)+b'\x68'+pack32(VOUT));call(0x4d4f60)
    emit(bytes.fromhex('83c40ca3')+pack32(f.OUT+16)+b'\x68'+pack32(f.OBS)+b'\xc3')
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+8)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
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
                args,e=build(c);path=ROOT/'ghidra'/f'phase97-navigation-speed-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['word11c']).items()),(c,r,e)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],'getter',e['getter'],'cap',e['word11c'],'vector',e['limited_vector'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase97-navigation-speed-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
