"""Full DX44ACE0 and44BD70 with explicit memory-only providers.

Real native initializer/vector/comparator and optional raw-converter/store run.
Clock, global-mode hook, timer reset, matrix rebuild, dimensions, ground height,
orientation runtime table and event scheduling are deterministic fixtures.
No game, GUI, rendered camera or mode11 kind6 random placement is executed.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_hero_parties import compare_code
from native_cameras import intermediate,preset_projection
from native_camera_activation import activate,get,put,setw
ROOT=Path(__file__).resolve().parent
BASE=0x10000000;CAP=0x10010000;NOW=123456
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def raw(ident):
    vals=[101,202,303,11,22,33,1111,2222,3333,4000,1000,9000,6000,3000,12000,4096,1024,8192]
    # Each bank entry distinct; invalid aliases cannot accidentally compare equal.
    vals[2]+=ident*17;vals[9]+=ident*19;vals[12]+=ident*23;vals[15]+=ident*29
    return struct.pack('<II19I3H2B',205,84,*vals,ident<<16,0x1234,0x3456,0x5678,ident,165)

def real_records():
    from emulate_camera_catalogue import real_records as records
    return records()

def cases():
    rows=[dict(name=f'mode-{m}',mode=m) for m in range(20)]
    for old,mode,pending in [(1,3,1),(3,1,3),(7,1,7),(7,7,7),(18,13,18),(3,14,3),(3,9,3),(2,1,1),(1,2,2),(2,3,2)]:
        rows.append(dict(name=f'old-{old}-mode-{mode}-pending-{pending}',old=old,mode=mode,pending=pending))
    for mode in [1,3,13,11,7]:rows.append(dict(name=f'force-instant-{mode}',mode=mode,force=True))
    for old,pending in [(3,1),(1,2),(2,1)]:rows.append(dict(name=f'blocked-{old}-{pending}',mode=13,old=old,pending=pending,blocked=True,node=0x10001))
    rows.extend([dict(name='unchanged-color',mode=3,equal_color=True),dict(name='clock-wrap',mode=1,now=0xFFFFFF00),dict(name='new-preset-lock',mode=1,preset_flags=128),dict(name='reversed-bounds',mode=13,reversed_bounds=True)])
    for kind in [0,2,4]:
        rows.append(dict(name=f'node-kind-{kind}',mode=2,node=1,entity_kind=kind,force=True,valid=True))
    rows.extend([dict(name='node-creature-2d',mode=10,node=1,entity_kind=0,creature_id=45,force=True,valid=True),dict(name='node-high-word-truncation',mode=1,node=0x10001,entity_kind=2,force=True,valid=True),dict(name='node-11-fallback-valid-kind4',mode=11,node=1,entity_kind=4,valid=True)])
    # Last occurrence of each ID is a real source record; original order is not
    # changed in any file. These are18 separate raw->bank->activation chains.
    byid={r[90]:i for i,r in enumerate(real_records())}
    for ident,idx in sorted(byid.items()):rows.append(dict(name=f'real-chain-id-{ident}-record-{idx}',mode=ident,chain_index=idx))
    rows.append(dict(name='real-chain-preset9-activate13',mode=13,chain_index=byid[9]))
    rows.append(dict(name='new-lock-blocks-next-call',mode=1,preset_flags=128,next_mode=13))
    return rows

def initial(c):
    state=bytearray([165])*0xF00;put(state,0,BASE+0x6000)
    for ident in range(1,19):state[40+152*ident:40+152*(ident+1)]=intermediate(raw(ident))
    for off,value in [(0xD68,5000),(0xD6C,7000),(0xD70,9000),(0xD80,6000),(0xD8A,4500),(0xD8E,4096),
                      (0xD92,128 if c.get('blocked') else 0),(0xDA8,c.get('old',1)),(0xDD4,c.get('pending',c.get('old',1))),
                      (0xDD8,0),(0xDE0,6800),(0xE3C,64 if c.get('force') else 0)]:put(state,off,value)
    struct.pack_into('<3H',state,0xD84,0x789,0x456,0x123)
    for o,n in [(0xE52,16),(0xE62,32),(0xE72,48),(0xE82,64),(0xE92,80)]:state[o:o+16]=bytes(range(n,n+16))
    if c.get('equal_color'):state[0xE72:0xE82]=state[0xE92:0xEA2]
    slot=9 if c['mode'] in {13,14,15} else c['mode'];off=40+slot*152
    if c.get('preset_flags') is not None:put(state,off+140,c['preset_flags'])
    if c.get('reversed_bounds'):
        for lo,hi in [(108,112),(120,124),(132,136)]:put(state,off+lo,9000);put(state,off+hi,1000)
    if 'chain_index' in c:
        r=real_records()[c['chain_index']];v=preset_projection(r,5);state[v['slot_offset']:v['slot_offset']+152]=v['slot']
    return state

def build_case(c):
    state=initial(c);native_initial=bytearray(state);ent=None;definition=None;node=c.get('node',0)&65535
    if 'entity_kind' in c:
        ent=bytearray([165])*0x400;put(ent,0x12,c['entity_kind']);struct.pack_into('<3I',ent,0x1A,11111,22222,33333);setw(ent,0xF4,0xBCA);ent[0x36F]=c.get('creature_id',12)
        definition=bytearray([165])*0x900;setw(definition,0xFA,1234);setw(definition,0x65C,4321)
        put(ent,0x12D,BASE+0x18000);put(ent,0x370,BASE+0x18000)
    orientations=bytes((i*13+7)&255 for i in range(190));now=c.get('now',NOW)
    want=activate(state,c['mode'],c.get('node',0),now=now,orientations=orientations,entity=ent,definition=definition,node_valid=c.get('valid',False))
    first_result=want['result']
    if 'next_mode' in c:
        second=activate(want['manager'],c['next_mode'],now=now,orientations=orientations)
        for k in ['time','reset','matrix','dimensions','ground']:second['calls'][k]+=want['calls'][k]
        for k in ['hook','events']:second['calls'][k]=want['calls'][k]+second['calls'][k]
        want=second
    capture=bytearray(128);calls=want['calls']
    for o,v in [(0,calls['time']),(4,len(calls['hook'])),(16,calls['reset']),(20,calls['matrix']),(24,calls['dimensions']),(28,calls['ground']),(32,len(calls['events'])//2),(36,len(calls['events'])//2),(60,want['result'])]:put(capture,o,v)
    for i,v in enumerate(calls['hook']):put(capture,80+4*i,v)
    if 'next_mode' in c:put(capture,44,first_result)
    if calls['events']:
        for o,v in zip([96,100,104,108,112],calls['events'][0]+calls['events'][1]):put(capture,o,v)
    def inc(o):return bytes.fromhex('ff05')+pack32(CAP+o)
    time=inc(0)+b'\xB8'+pack32(now)+b'\xC3'
    hook=bytes.fromhex('a1')+pack32(CAP+4)+bytes.fromhex('8b15')+pack32(0x759AE8)+bytes.fromhex('891485')+pack32(CAP+80)+inc(4)+b'\xC3'
    world=bytearray(8);put(world,0,BASE+0x7000);put(world,4,BASE+0x8000)
    vt=bytearray(0x13C);put(vt,0x138,BASE+0xC000)
    child=bytearray(0x18);put(child,0x14,BASE+0x9000)
    terrainvt=bytearray(0x208);put(terrainvt,0x204,BASE+0xD000)
    dimensions=inc(24)+b'\xB8'+pack32(BASE+0xB000)+b'\xC3'
    ground=inc(28)+b'\xB8'+pack32(768)+bytes.fromhex('c20400')
    def event(counter,offset,n):
        code=inc(counter)
        for j in range(n):code+=bytes.fromhex('8b4424')+bytes([4+4*j])+b'\xA3'+pack32(CAP+offset+4*j)
        return code+bytes.fromhex('c2')+struct.pack('<H',n*4)
    # Native return captured independently before observer comparisons.
    readout=b'\xA3'+pack32(CAP+60)+compare_code(BASE,BASE+0x2000,0xF00)+bytes.fromhex('89ca89c5')
    readout+=compare_code(CAP,BASE+0x11000,128)+bytes.fromhex('21c5')
    readout+=compare_code(0x759AE8,BASE+0x12000,4)+bytes.fromhex('21c5')
    if ent is not None:
        readout+=compare_code(BASE+0x16000,BASE+0x17000,len(ent))+bytes.fromhex('21c5')
        readout+=compare_code(BASE+0x18000,BASE+0x19000,len(definition))+bytes.fromhex('21c5')
    readout+=bytes.fromhex('89e8')+END
    mem=[region(BASE,native_initial),region(BASE+0x2000,want['manager']),region(BASE+0x4000,readout),region(BASE+0x6000,world),region(BASE+0x7000,vt),region(BASE+0x8000,child),region(BASE+0x9000,pack32(BASE+0xA000)),region(BASE+0xA000,terrainvt),region(BASE+0xB000,struct.pack('<II',32,48)),region(BASE+0xC000,dimensions),region(BASE+0xD000,ground),region(CAP,bytes(128)),region(BASE+0x11000,capture),region(BASE+0x12000,pack32(want['global_mode'])),region(0x759AE8,pack32(0xA5A5A5A5)),region(0x756EEC,pack32(4096)),region(0x75A311,orientations),region(0x5B5E90,time),region(0x571DA0,hook),region(0x5589F0,inc(16)+b'\xC3'),region(0x44D6D0,inc(20)+b'\xC3'),region(0x5976D0,event(32,96,3)),region(0x597690,event(36,108,2))]
    validity=bytearray(16);table=bytearray(64)
    if ent is not None:
        validity[node]=int(c.get('valid',False));put(table,4*node,BASE+0x16000)
        mem.extend([region(BASE+0x16000,ent),region(BASE+0x17000,want['entity']),region(BASE+0x18000,definition),region(BASE+0x19000,definition)])
    mem.extend([region(0x6ED540,validity),region(0x6EE540,table)])
    entry=0x44ACE0;regs=dict(ECX=hex(BASE));stack=pack32(BASE+0x4000)+pack32(c['mode'])+pack32(c.get('node',0))
    if 'next_mode' in c:
        a=BASE+0x14000
        adapter=b'\xA3'+pack32(CAP+44)+b'\xB9'+pack32(BASE)+b'\x68'+pack32(0)+b'\x68'+pack32(c['next_mode'])
        adapter+=b'\xE8'+struct.pack('<i',0x44ACE0-(a+len(adapter)+5))
        adapter+=jump(a+len(adapter),BASE+0x4000)
        mem.append(region(a,adapter));stack=pack32(a)+pack32(c['mode'])+pack32(c.get('node',0))
    if 'chain_index' in c:
        rawdata=real_records()[c['chain_index']];ident=rawdata[90];o=40+152*ident
        # The18 chains start with a different bank record and dirty mid state.
        native_initial[o:o+152]=intermediate(raw(ident));mem[0]=region(BASE,native_initial)
        adapter=b'';a=BASE+0x14000
        def call(target):
            nonlocal adapter
            adapter+=b'\xE8'+struct.pack('<i',target-(a+len(adapter)+5))
        adapter+=b'\xB9'+pack32(BASE+0x15000)+b'\x68'+pack32(BASE+0x1A000);call(0x56F900)
        adapter+=b'\xB9'+pack32(BASE)+b'\x68'+pack32(BASE+0x15000);call(0x449A30)
        adapter+=b'\xB9'+pack32(BASE)+b'\x68'+pack32(0)+b'\x68'+pack32(c['mode']);call(0x44ACE0)
        adapter+=jump(a+len(adapter),BASE+0x4000)
        mem.extend([region(a,adapter),region(BASE+0x15000,bytes([165])*152),region(BASE+0x1A000,rawdata[8:]),region(0x759AEC,pack32(5))])
        entry=0x56F850;regs=dict(ECX=hex(BASE+0x15000));stack=pack32(a)
    mem.append(region(0x7FFF0000,stack))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,EDX,ECX,ESP,EIP'),dict(EAX=1,EBP=1,EDX=0)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as s:
            await s.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase63-emulation-activation-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await s.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want)
                rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase63-camera-activation-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
