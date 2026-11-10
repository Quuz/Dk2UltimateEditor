"""Raw type224 callback→native possession flag setter→complete speed getter.

Prepared world/actor state comes from verified phase90 native factory data.
No game code/vtable replacement; all checked bytes compared after execution.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_creature_factory as cf
import emulate_object_factory as f
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
VARS=0x6f2550;RAW=0x10100000;DRIVER=0x10110000
def cases():
    base=dict(percent=200,possessed=True,other_flag=False,mode=0,base_speed=4096,scalars=[6,6,6])
    return [dict(base,**v,name=n) for n,v in [('possessed200',{}),('unpossessed200',dict(possessed=False)),('possessed0',dict(percent=0)),
      ('possessed100',dict(percent=100)),('possessed-minus1',dict(percent=0xffffffff)),('possessed-min16',dict(percent=0x8000)),
      ('possessed-max16',dict(percent=0x7fff)),('percent-high-bits',dict(percent=0xabcd00c8)),('run',dict(mode=1)),('shuffle',dict(mode=2)),
      ('other-flag200',dict(possessed=False,other_flag=True)),('other-flag0',dict(possessed=False,other_flag=True,percent=0)),
      ('both-flags',dict(other_flag=True)),('scalar-high',dict(scalars=[30000,30000,30000])),('scalar-zero',dict(scalars=[0,0,0])),
      ('stat22',dict(raw_index=22)),('stat1',dict(raw_index=1,percent=100)),('speed-product-wrap',dict(base_speed=0xffffffff)),
      ('writer32-getter32',dict(raw_index=32,percent=9999,query=32)),('writer277-getter277',dict(raw_index=277,percent=200,query=277)),
      ('current-record',dict(source_current=True))]]
def expected_speed(c):
    base=[c['base_speed'],8192,2048][c['mode']]
    p=(c['percent'] if c.get('raw_index',21)==21 else 0)&65535;p=p if p<32768 else p-65536
    if c['possessed']:return ((base*(100+p))&0xffffffff)//100
    if c['other_flag']:return ((base*(100+p))&0xffffffff)//200
    return base
def build(c):
    args,e=cf.build(cf.cases()[0],0);allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};memory={a:b for a,b in allmem.items() if a<0x11000000 and a not in [cf.DRIVER,f.OBS,0x7ffee000]};shadow=0x11000000
    # Previously observed post-factory data is prepared state, not rerun here.
    for row in e['checks']:
        a=int(row['address'],16);data=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
        for at,b in memory.items():
            if at<=a and a+len(data)<=at+len(b):b[a-at:a-at+len(data)]=data;break
        else:raise AssertionError(hex(a))
    image=(ROOT/'ghidra/phase90-creature-factory-base.bin').read_bytes();assert len(image)==1343
    memory[cf.ACTOR]=bytearray(image)+bytearray([0xa5])*256
    for a in list(memory):
        if VARS<=a<VARS+4096 or a==cf.VARIABLES:del memory[a]
    variables=bytearray(4096)
    for o,value in zip([0x390,0x3a2,0x3a4],c['scalars']):f.put(variables,o,value,2)
    memory[VARS]=variables;memory[0x6e4154]=bytearray(pack32(VARS))
    for o,value in [(0x5b4,c['base_speed']),(0x5b8,8192),(0x5bc,2048)]:f.put(memory[cf.DEFINITION],o,value)
    if c['other_flag']:memory[cf.ACTOR][0x3ea]|=8
    raw=struct.pack('<4I',224,c.get('raw_index',21),c['percent']&0xffffffff,0x11223344)
    if c.get('source_current'):
        doc=json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text(encoding='utf8'));data=Path(doc['source']).read_bytes();assert hashlib.sha256(data).hexdigest()==doc['sha256']
        row=next(r for r in doc['sections'][0]['records'] if r['words_u32'][:2]==[224,21]);raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+16]==raw
        assert struct.unpack('<4I',raw)==(224,21,200,10)
    memory[RAW]=bytearray(bytes(16)+raw)
    memory[f.OUT]=bytearray(8)+bytearray([0xa5])*24
    checks={a:bytearray(b) for a,b in memory.items() if a not in [f.GFX_CB,f.SOUND_CB,0x28cfa4]}
    f.put(checks[VARS],0x313+2*c.get('raw_index',21),c['percent'],2)
    if c['possessed']:
        flags=int.from_bytes(checks[cf.ACTOR][0x3e6:0x3ea],'little');f.put(checks[cf.ACTOR],0x3e6,flags|0x80)
        f.put(checks[cf.ACTOR],0x488,0);f.put(checks[cf.ACTOR],0x3ee,0)
    struct.pack_into('<2I',checks[f.OUT],0,1,expected_speed(c))
    if 'query' in c:
        q=c['query']&255;got=100 if q>=32 else (c['percent']&65535 if q==c.get('raw_index',21) else 0);got=got if got<32768 else got-65536
        f.put(checks[f.OUT],8,got)
    driver=bytearray(b'\xa3'+pack32(f.OUT))
    def emit(b):driver.extend(b)
    def call(target):emit(b'\xe8'+pack32(target-(DRIVER+len(driver)+5)))
    if c['possessed']:emit(b'\x68'+pack32(1)+b'\xb9'+pack32(cf.ACTOR));call(0x48dea0)
    emit(b'\x68'+pack32(c['mode'])+b'\xb9'+pack32(cf.ACTOR));call(0x48abb0);emit(b'\xa3'+pack32(f.OUT+4))
    if 'query' in c:emit(b'\x68'+pack32(c['query']));call(0x495080);emit(bytes.fromhex('83c404a3')+pack32(f.OUT+8))
    emit(b'\x68'+pack32(f.OBS)+b'\xc3')
    memory[DRIVER]=driver;stack=bytearray([0xa5])*16384;struct.pack_into('<2I',stack,8192,DRIVER,RAW);memory[0x7ffee000]=stack
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,wanted) in enumerate(checks.items()):
        mem.append(region(shadow,wanted));observer+=compare_code(actual,shadow,len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(wanted)+255)//256)*256
    observer+=bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args.update(address='0x51dc70',registers=json.dumps(dict(ECX='0',EAX='0',EBX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,EBX,ECX,EDX,EIP,ESP')
    return args,dict(speed=expected_speed(c),raw=raw.hex(),actor_source_sha256=hashlib.sha256(image).hexdigest(),checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    p=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'});rows=[]
    async with stdio_client(p) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,e=build(c);path=ROOT/'ghidra'/f'phase92-possession-speed-{c["name"]}.json'
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==e
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'));assert not response.isError and 'error' not in r,r
                    saved=dict(case=c,input=args,expected=e,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['speed']).items()),(c,r,e)
                rows.append(dict(case=c['name'],file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()));print(c['name'],e['speed'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase92-possession-speed-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
