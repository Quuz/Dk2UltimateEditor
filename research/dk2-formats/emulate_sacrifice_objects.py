"""Complete 4F1780 with successful native Object factory and unchanged vtables.

Factory fixture is reused as data only; native temple dispatcher executes.
Room owner8 differs from object-owner global7. External sound/GFX providers
are identical to the separately verified Object factory fixture.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_object_factory as f
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import memory_readout_ymm as readout
import fixture_read_probe as probe
from native_sacrifice import raw_recipe,runtime_recipe,history_append
ROOT=Path(__file__).resolve().parent
ROOM=0x10080000;ROOMDEF=0x10090000;ROOMOWNER=0x100a0000
def cases():
    base=dict(ident=17,value=17,gate=4094,block_size=512,empty=False,kind=2,room_x=5,room_y=6)
    return [dict(base,**kw,name=n) for n,kw in [('object',{}),('object-high-bits',dict(value=0xfedcba11)),('object-gold-bag',dict(ident=1,value=1)),
      ('object-gate-zero',dict(gate=0)),('object-pool-empty',dict(empty=True)),('object-pool-small',dict(block_size=404)),('object-id-zero',dict(ident=0,value=0)),
      ('raw-object',dict(raw_pipeline=True)),('raw-object-high-bits',dict(raw_pipeline=True,value=0xfedcba11)),
      ('gold5000',dict(kind=3,ident=1,value=5000)),('gold1000',dict(kind=3,ident=1,value=1000)),
      ('gold1',dict(kind=3,ident=1,value=1)),('gold-capacity-zero',dict(kind=3,ident=1,value=5000,gate=0)),
      ('gold-direct-y0',dict(kind=3,ident=1,value=5000,room_y=0)),('gold-zero',dict(kind=3,ident=1,value=0)),
      ('gold-negative-no-treasury',dict(kind=3,ident=1,value=0xffffffff))]]
def build(c,page):
    fc=dict(f.cases()[0],ident=c['ident'],gate=c['gate'],block_size=c['block_size'],empty=c['empty'],position=[(c['room_x']<<12)+0x800,(c['room_y']<<12)+0x800,0])
    args,e=f.build(fc,page);regions={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for check in e['checks']:
        a=int(check['address'],16);checks[a]=regions[shadow];assert len(checks[a])==check['bytes'];shadow+=((check['bytes']+255)//256)*256
    memory={a:b for a,b in regions.items() if a<0x11000000 and a not in [f.OBS,0x7ffef000]}
    creation_requested=c['kind']==2 or (c['kind']==3 and 0<c['value']<0x80000000 and c['room_x'] and c['room_y'])
    if not creation_requested:
        for a in [f.WORLD,f.PLAYER,f.MAP,f.FH,f.FT,f.UH,f.UT,0x6ee540,0x6ed540,0x6a4818,0x6f2540,f.KINDS]:checks[a]=bytearray(memory[a])
        for a in list(checks):
            if f.BLOCK<=a<f.BLOCK+768:del checks[a]
        checks[f.BLOCK]=bytearray(memory[f.BLOCK]);e.update(success=False,node=None,entity_id=None)
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    both(f.WORLD,0x15a0,1);f.put(checks[f.WORLD],0x15a0,(0x19660d+0x3c6ef35f)&0xffffffff)
    both(0x6ee540,8*4,ROOMOWNER);both(0x6ed540,8,1,1)
    memory[0x6ed484]=bytearray(pack32(f.WORLD));checks[0x6ed484]=bytearray(pack32(f.WORLD))
    if c['kind']==3:memory[0x6e5054]=bytearray(pack32(f.WORLD));checks[0x6e5054]=bytearray(pack32(f.WORLD))
    memory[0x6f2548]=bytearray(struct.pack('<H',7));checks[0x6f2548]=bytearray(struct.pack('<H',7))
    room=bytearray((i*31+7)&255 for i in range(0xc0));room[0x18]=13;f.put(room,0x19,ROOMDEF);f.put(room,0x1d,8,2)
    room[0x2d]=c['room_x'];room[0x2e]=c['room_y'];f.put(room,0xa7,1,2);room[0x5b:0x5e]=bytes([1,0,3]);wanted_room=bytearray(room);wanted_room[0x5b:0x5e]=bytes([255,0,0])
    roomowner=bytearray(0x1200);f.put(roomowner,0x19a,1)
    wanted_owner=bytearray(roomowner)
    if c['kind']==3 and 0<c['value']<0x80000000 and (c['room_x']==0 or c['room_y']==0):f.put(wanted_owner,0x7e,c['value'])
    for a,b,w in [(ROOM,room,wanted_room),(ROOMDEF,bytes(0x500),bytes(0x500)),(ROOMOWNER,roomowner,wanted_owner)]:memory[a]=bytearray(b);checks[a]=bytearray(w)
    arguments=bytearray(64);arguments[:12]=memory[f.ARGS];struct.pack_into('<III',arguments,16,c['kind'],c['value'],0);struct.pack_into('<III',arguments,32,0x9800,0xa800,0x1000)
    memory[f.ARGS]=arguments;checks[f.ARGS]=bytearray(arguments)
    raw_pipeline=c.get('raw_pipeline',False)
    if raw_pipeline:
        raw=raw_recipe([(3,9)],c['kind'],c['value']);memory[0x100b0000]=bytearray(bytes(16)+raw);checks[0x100b0000]=bytearray(memory[0x100b0000])
        memory[0x100b1000]=bytearray(12);checks[0x100b1000]=bytearray(struct.pack('<III',c['kind'],c['value'],0))
        checks[f.WORLD][0x2e70:0x2e70+34]=runtime_recipe(raw);f.put(checks[f.WORLD],0x731b,1,1)
        memory[ROOM][0x5b:0x5e]=bytes([255,0,0]);hist=bytearray(history_append(memory[ROOM],(3,9,0)));hist[0x5b:0x5e]=bytes([255,0,0]);checks[ROOM]=hist
        driver=bytearray();at=0x100b2000
        def emit(data):driver.extend(data)
        def native(target):emit(b'\xe8'+pack32(target-(at+len(driver)+5)))
        emit(b'\x68'+pack32(0)+b'\x68'+pack32(9)+b'\x68'+pack32(3)+b'\xb9'+pack32(ROOM));native(0x4f1600)
        emit(b'\x68'+pack32(0x100b1000)+b'\x68'+pack32(f.WORLD+0x2e70)+b'\xb9'+pack32(ROOM));native(0x4f1660)
        emit(b'\x68'+pack32(f.ARGS+32)+b'\x68'+pack32(0x100b1000)+b'\xb9'+pack32(ROOM));native(0x4f1780)
        emit(b'\x68'+pack32(f.OBS)+b'\xc3');memory[at]=driver
    # Observe creation from the real registration table, not the void dispatcher's EAX.
    checks[f.OUT]=bytearray(pack32(int(e['success']))+pack32(e['node'] or 0)+pack32(2 if e['success'] else 0)+pack32(int(e['success'])))
    observer=b'\xa1'+pack32(0x6ee540+100*4)+b'\xa3'+pack32(f.OUT+4)+bytes.fromhex('85c00f95c00fb6c0a3')+pack32(f.OUT)+b'\xbd'+pack32(1)
    mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for actual,data in checks.items():mem.append(region(shadow,data));observer+=compare_code(actual,shadow,len(data))+bytes.fromhex('21c5');shadow+=((len(data)+255)//256)*256
    node=(f.BLOCK+(c['block_size']-408 if c['block_size']-408>12 else 0)+12)
    observer+=readout.code(node+256*page)
    if probe.COUNTER in memory:observer+=bytes.fromhex('8b1d')+pack32(probe.COUNTER)
    observer+=b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    stack=bytearray([0xa5])*8192
    if raw_pipeline:struct.pack_into('<2I',stack,4096,0x100b2000,0x100b0000)
    else:struct.pack_into('<3I',stack,4096,f.OBS,f.ARGS+16,f.ARGS+32)
    mem.append(region(0x7ffef000,stack))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args.update(address='0x4f1780',registers=json.dumps(dict(ECX=hex(ROOM),DF='0')),memory=json.dumps(dict(regions=mem)))
    if raw_pipeline:args.update(address='0x51db30',registers=json.dumps(dict(ECX=hex(f.WORLD),DF='0')))
    expected=dict(success=e['success'],node=e['node'],entity_id=e['entity_id'],object_owner=7,room_owner=8,position=[fc['position'][0],fc['position'][1],0x1000],requested_position=fc['position'],
      argument_position=[0x9800,0xa800,0x1000],kind=c['kind'],value=c['value'],history_reset=True,checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])
    if c['kind']==3:expected.update(gold_amount=c['value'] if e['success'] else 0,direct_balance=int.from_bytes(wanted_owner[0x7e:0x82],'little'),treasury_list_empty=True)
    if raw_pipeline:expected['raw_pipeline']=True
    return args,expected,fc
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                chunks=[]
                for page in range(2):
                    args,e,fc=build(c,page);p=ROOT/'ghidra'/f'phase89-sacrifice-object-{c["name"]}-page{page}.json'
                    if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
                    else:
                        response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                        if response.isError or 'error' in r:raise RuntimeError(str(r))
                        saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                    r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],page,r,e)
                    chunks.append(readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page))
                b=b''.join(chunks)[:393];f.verify_node(fc,b,e)
                if c['kind']==3 and e['success']:
                    u=lambda o:int.from_bytes(b[o:o+4],'little');scale=0x11c4 if c['value']>=5000 else 0x800+int(c['value']*0.5)
                    assert u(0x14a)==c['value'] and u(0x48)==u(0x4c)==scale and u(0x58)==u(0x5c)==0
                (ROOT/'ghidra'/f'phase89-sacrifice-object-{c["name"]}.bin').write_bytes(b);print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase89-sacrifice-object-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
