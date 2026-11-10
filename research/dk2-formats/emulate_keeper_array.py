"""Compose real raw→mid→World insertion and real native array accessors.

Original World vtable,getter,scalar loader,converter,initializers execute.
Preallocated arrays/world are explicit state;GFX null,sound fixed external.
Null-overlay cases use a controlled low-address guard,not a real game crash.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_keeper_pipeline import intermediate
from native_keeper_lookup import lookup_index
from native_keeper_array import apply,default_definition,ids_result,put
import dk2_formats as fmt
import memory_readout
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/KeeperSpells.kwd'
TEMPLATE=next(r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==150 for r in s.records)
WORLD=0x10000000;ARRAY=0x10010000;MID=0x10012000;RAW=0x10014000;SESSION=0x10018000;OUT=0x10019000;SOUND=0x1001A000;SV=0x1001B000;SCB=0x1001C000;SCRATCH=0x1001D000;OBS=0x10020000;DRIVER=0x10024000

def cases():
    base=dict(kind='load',initial=[1,7],capacity=5,ids=[3],overlay=False,gate=0,tps=20,c8=0xFE0C0000,constructor=False,partial=False,presence=True,seed=165)
    variants=[('insert-middle',{}),('insert-first',dict(ids=[0])),('insert-last',dict(ids=[255])),('physical-unsorted',dict(initial=[],ids=[7,1,3])),
      ('duplicate3',dict(ids=[3,3])),('duplicate1',dict(ids=[1,1])),('capacity-full',dict(capacity=2)),('capacity-zero',dict(initial=[],capacity=0)),
      ('capacity-one-overflow',dict(initial=[],capacity=1,ids=[7,1,3])),('checksum-gate',dict(gate=1)),
      ('constructor-raw-chain',dict(initial=[],ids=[7,1,3],constructor=True)),('constructor-checksum-gate',dict(initial=[],ids=[3],constructor=True,gate=1)),
      ('overlay-existing',dict(ids=[7],overlay=True)),('overlay-duplicate3',dict(initial=[1,3,3,7],ids=[3],overlay=True)),
      ('overlay-missing',dict(ids=[3],overlay=True)),('overlay-zero',dict(ids=[0],overlay=True)),
      ('partial-insert',dict(partial=True)),('partial-c8-absent',dict(partial=True,presence=False)),
      ('partial-overlay',dict(partial=True,ids=[7],overlay=True)),('partial-overlay-absent',dict(partial=True,presence=False,ids=[7],overlay=True)),
      ('partial-overlay-missing',dict(partial=True,ids=[3],overlay=True)),('tps-zero',dict(tps=0)),('tps-max',dict(tps=0xFFFFFFFF))]
    rows=[dict(base,**kw,name=name) for name,kw in variants]
    for v in [0,1,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:rows.append(dict(base,c8=v,name=f'c8-{v:x}'))
    source=json.loads((ROOT/'phase83-order-duplicate-source.json').read_text())
    rows.append(dict(base,initial=[],ids=[r['id'] for r in source['entries']],actual_source_file=source['file'],actual_source_sha256=source['sha256'],
      actual_source_indices=[r['index'] for r in source['entries']],order_probes=[source['order'],10],name='source-order-duplicate'))
    for name,ids,arg,index,order in [('empty',[],1,0,42),('ordinary',[1,3,7],3,1,42),('wrap',[1,3,7],7,2,42),
      ('previous-wrap',[1,3,7],1,0,42),('missing',[1,3,7],2,3,42),('high-id-bits',[1,3,7],0xABCD0003,1,42),
      ('index-max',[1,3,7],3,0xFFFFFFFF,42),('order-high-bits',[1,3,7],3,1,0x100002A),
      ('zero-id',[0,1,3],0,0,42),('duplicates',[1,3,3,7],3,2,42),('unsorted',[7,1,3],1,1,42),('single255',[255],255,0,42)]:
        rows.append(dict(base,kind='access',initial=ids,ids=[],argument=arg,index=index,order=order,name='access-'+name))
    return rows

def call(at,target):return b'\xE8'+pack32(target-(at+5))

def build(c):
    assert c['capacity']>=len(c['initial'])
    world=bytearray(0xA3D0);put(world,0,0x66E3EC);put(world,4,SESSION);put(world,0x1604,ARRAY);put(world,0x1608,c['capacity']);put(world,0x160C,len(c['initial']));put(world,0xA3C7,int(c['overlay']))
    array=bytearray([c['seed']])*(122*c['capacity']);count=len(c['initial']);aggregate=0
    orders=[42 if i!=1 else 17 for i in range(len(c['initial']))]
    for i,ident in enumerate(c['initial']):
        r=bytearray(((j*17+i*31+9)&255) for j in range(122));r[0x75]=ident;r[0x6C]=orders[i];put(r,0x3A,0x12340000+i);put(r,0x36,0x100+i)
        array[i*122:(i+1)*122]=r;aggregate=(aggregate+0x100+i)&0xFFFFFFFF
    put(world,0x1610,aggregate);wanted_world=bytearray(world);wanted_array=bytes(array)
    guard=bytes([204])*256;wanted_guard=bytearray(guard);sound=bytes(32);sv=bytearray(32);put(sv,0x1C,SCB)
    output=bytearray(128);wanted_out=bytearray(output);regions=[region(WORLD,world),region(ARRAY,bytes(array)+bytes([0x5A])*256),region(SESSION,bytes(20)),region(0,guard),
      region(SOUND,pack32(SV)),region(SV,sv),region(OUT,output),region(0x75B468,pack32(SOUND)),region(0x6F2590,pack32(c['tps'])),region(0x6FCDCF,pack32(c['gate']))]
    callback=bytes.fromhex('ff05')+pack32(OUT+0x40)+b'\xB8'+pack32(0x11223344)+bytes.fromhex('c20800');regions.append(region(SCB,callback));readonly=[(SESSION,bytes(20)),(SOUND,pack32(SV)),(SV,bytes(sv)),(0x75B468,pack32(SOUND)),(0x6F2590,pack32(c['tps'])),(0x6FCDCF,pack32(c['gate']))]
    driver=bytearray();steps=[];probes=[]
    def emit(b):driver.extend(b)
    def native_call(a):emit(call(DRIVER+len(driver),a))
    if c['constructor']:
        assert not c['initial']
        for i in range(c['capacity']):emit(b'\xB9'+pack32(ARRAY+122*i));native_call(0x4F7C70)
        wanted_array=default_definition(c['seed'])*c['capacity']
    if c['kind']=='load':
        for i,ident in enumerate(c['ids']):
            if c.get('actual_source_file'):
                source=Path(c['actual_source_file']).read_bytes();assert hashlib.sha256(source).hexdigest()==c['actual_source_sha256']
                records=next(s.records for s in fmt.parse(source) if s.signature==150);raw=records[c['actual_source_indices'][i]];assert raw[0xEE]==ident
            else:
                raw=bytearray(TEMPLATE);raw[:32]=(f'ArrayCase{i}'.encode()+b'\0').ljust(32,b'\0');raw[0xEE]=ident;put(raw,0xC8,(c['c8']+i)&0xFFFFFFFF)
                raw[0xDB]=42+i;raw=bytes(raw)
            ma=MID+i*0x400;ra=RAW+i*0x200
            if c['partial']:
                mid=bytearray(526);put(mid,4,1);put(mid,0x18,int(c['presence']));mid[0x166]=ident;put(mid,0x140,(c['c8']+i)&0xFFFFFFFF);mid=bytes(mid);initial_mid=mid
            else:
                mid=intermediate(raw);initial_mid=bytes([0xCC])*526;emit(b'\xB9'+pack32(ma));native_call(0x56B990)
                emit(b'\x68'+pack32(ra)+b'\xB9'+pack32(ma));native_call(0x56BAB0)
            regions.extend([region(ma,initial_mid),region(ra,raw)]);readonly.extend([(ma,mid),(ra,raw)])
            emit(b'\x68'+pack32(WORLD)+b'\x68'+pack32(ma));native_call(0x51FCD0);emit(bytes.fromhex('83c408a3')+pack32(OUT+4*i));put(wanted_out,4*i,1)
            e=apply(wanted_array,count,c['capacity'],aggregate,mid,c['overlay'],c['gate'],c['tps'],bytes(wanted_guard[:122]))
            wanted_array=e['array'];count=e['count'];aggregate=e['aggregate']
            if e['null_result'] is not None:wanted_guard[:122]=e['null_result']
            put(wanted_out,0x40,int.from_bytes(wanted_out[0x40:0x44],'little')+e['sound_calls'])
            steps.append(dict(id=ident,selected=e['selected'],loader_called=e['loader_called'],null_write=e['null_result'] is not None,count=count,aggregate=aggregate))
        for i,key in enumerate(c['ids'][:4]):
            slot=lookup_index([wanted_array[j*122+0x75] for j in range(count)],key);ptr=0 if slot is None else ARRAY+122*slot;at=0x10+8*i
            put(wanted_out,at,ptr);put(wanted_out,at+4,0xAABBCCDD if slot is None else int.from_bytes(wanted_array[slot*122+0x3A:slot*122+0x3E],'little'))
            emit(b'\x68'+pack32(key)+b'\xB9'+pack32(WORLD));native_call(0x50DD60);emit(b'\xA3'+pack32(OUT+at)+bytes.fromhex('85c07408')+bytes.fromhex('8b403aa3')+pack32(OUT+at+4))
            # A null getter skips the C8 read;the prefilled sentinel is explicit.
            put(output,at+4,0xAABBCCDD);probes.append(dict(id=key,index=slot))
        for i,order in enumerate(c.get('order_probes',[])):
            selected=next((j for j in range(count) if wanted_array[j*122+0x6C]==order),None)
            value=0 if selected is None else wanted_array[selected*122+0x75];put(wanted_out,0x50+4*i,value)
            emit(b'\x68'+pack32(order)+b'\xB9'+pack32(WORLD));native_call(0x50DE00);emit(bytes.fromhex('25ff000000a3')+pack32(OUT+0x50+4*i))
    else:
        for i,(method,fn,arg) in enumerate([('first',0x50DC80,None),('next',0x50DCA0,c['argument']),('previous',0x50DD00,c['argument']),('index',0x50DDD0,c['index']),('order',0x50DE00,c['order'])]):
            if arg is not None:emit(b'\x68'+pack32(arg))
            emit(b'\xB9'+pack32(WORLD));native_call(fn);emit(bytes.fromhex('25ff000000a3')+pack32(OUT+4*i))
            value=ids_result(c['initial'],method,0 if arg is None else arg,orders);put(wanted_out,4*i,value)
    # Reflect getter sentinels in initial output after native call sequence is built.
    regions=[region(OUT,output) if int(r['address'],16)==OUT else r for r in regions]
    put(wanted_world,0x160C,count);put(wanted_world,0x1610,aggregate)
    # Enter through a genuine native constructor,then return to the synthetic
    # call-sequence driver. No function/program mapping or original code patch.
    regions.append(region(SCRATCH,bytes([c['seed']])*122))
    checks=[(WORLD,wanted_world),(ARRAY,wanted_array+bytes([0x5A])*256),(0,wanted_guard),(OUT,wanted_out),(SCRATCH,default_definition(c['seed']))]+readonly
    emit(b'\x68'+pack32(OBS)+b'\xC3');regions.append(region(DRIVER,driver))
    observer=b'\xBD'+pack32(1)+b'\x31\xDB';at=0x11000000
    for i,(a,b) in enumerate(checks):
        regions.append(region(at,b));observer+=compare_code(a,at,len(b))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(ARRAY)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';regions.append(region(OBS,observer));stack=bytearray([0xA5])*8192;put(stack,4096,DRIVER);regions.append(region(0x7FFEF000,stack))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in regions);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),spans
    args=dict(program='DKII-DX.exe',address='0x4f7c70',max_steps=100000,registers=json.dumps(dict(ECX=hex(SCRATCH),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=regions)),return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,'+memory_readout.REGISTERS)
    return args,dict(runtime_ids=[wanted_array[i*122+0x75] for i in range(count)],count=count,aggregate=aggregate,steps=steps,probes=probes,
      array_expected_hex=wanted_array.hex(),first128_expected_hex=(wanted_array+bytes([0x5A])*256)[:128].hex(),output_expected_hex=wanted_out.hex(),checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks])

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,e=build(c);p=ROOT/'ghidra'/f'phase83-emulation-keeper-array-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
            else:
                response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c['name'],r,e)
            assert memory_readout.decode(r['registers']).hex()==e['first128_expected_hex']
            rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase83-keeper-array-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
