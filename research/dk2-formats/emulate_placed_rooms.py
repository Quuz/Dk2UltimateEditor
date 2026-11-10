"""CC parsers, game callback, editor preallocated add/resolve and full writer.

World validity/tile/health/rebuild/checksum and readonly file I/O are explicit
fixtures. Editor player lookup and terrain lookup execute actual code.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from party_writer_stack_audit import cleanups
from native_placed_rooms import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def raw_record(size=20,**kw):
    v=dict(x=3,y=4,z=0x76543210,flags=0x8001,direction=2,opaque=0x5A,health=300,terrain=12,owner=3);v.update(kw)
    p=struct.pack('<3IHBBHBB',v['x'],v['y'],v['z'],v['flags'],v['direction'],v['opaque'],v['health'],v['terrain'],v['owner'])
    return struct.pack('<II',204,size)+p[:size]

def cases():
    rows=[]
    for p in ['game','editor']:
        for size in [0,12,14,15,16,18,19,20]:rows.append(dict(name=f'parser-{p}-{size}',kind='parser',program=p,size=size,fail=False))
        rows.append(dict(name=f'parser-{p}-fail',kind='parser',program=p,size=20,fail=True))
    for seed in [0,165,255]:rows.append(dict(name=f'converter-{seed}',kind='converter',seed=seed))
    for terrain in [12,14,33,34,37,40,255]:
        for health in [0,50,100,300,65535]:rows.append(dict(name=f'game-{terrain}-{health}',kind='game',terrain=terrain,health=health,direction=2,owner=3,valid=True,room=1,current=10000))
    for direction in [0,1,2,3,4,5,6,7,255]:rows.append(dict(name=f'direction-{direction}',kind='game',terrain=37,health=100,direction=direction,owner=3,valid=True,room=1,current=10000))
    for name,kw in [('owner0',dict(owner=0)),('invalid-position',dict(valid=False)),('no-room',dict(room=0))]:
        c=dict(name='game-'+name,kind='game',terrain=37,health=100,direction=2,owner=3,valid=True,room=1,current=10000);c.update(kw);rows.append(c)
    for current in [1,12345,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        for health in [0,99,101,65535]:rows.append(dict(name=f'health-arithmetic-{current}-{health}',kind='game',terrain=12,health=health,direction=0,owner=3,valid=True,room=1,current=current))
    for terrain in [12,14,33,34,37,40]:
        for health in [0,100,256,300,65535]:rows.append(dict(name=f'editor-{terrain}-{health}',kind='editor',terrain=terrain,health=health,major=3,direction=255,owner=3,seed=165))
    for terrain in [12,14,37,40]:rows.append(dict(name=f'editor-legacy-{terrain}',kind='editor',terrain=terrain,health=300,major=1,direction=2,owner=3,seed=0))
    rows.append(dict(name='editor-terrain-mismatch',kind='editor',terrain=14,tile=12,health=100,major=3,direction=0,owner=3,seed=165))
    for seed in [0,165,255]:rows.append(dict(name=f'heart-serializer-{seed}',kind='heart_serializer',seed=seed))
    for seed in [0,165,255]:
        for health in [0,100,65535]:rows.append(dict(name=f'writer-{seed}-{health}',kind='writer',seed=seed,health=health,owner=3))
    rows.append(dict(name='writer-player2',kind='writer',seed=165,health=0,owner=2))
    return rows

def build_case(c):
    mem=[];regs={};program='DKII-DX.exe'
    if c['kind']=='parser':
        program='DKII-DX.exe' if c['program']=='game' else 'DK2 Editor pro.exe';entry=0x5708A0 if c['program']=='game' else 0x430FA0;raw=raw_record(c['size']);dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0xA0,0x10004000)
        io=bytes.fromhex('5657be')+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10003008)+bytes.fromhex('f3a48b44240cc700')+pack32(0xFFFFFFFF if c['fail'] else 0)+bytes.fromhex('8b54241c8b0d')+pack32(0x10003008)+bytes.fromhex('890a5f5ec3')
        cb=bytes.fromhex('56578b74240cbf')+pack32(0x10001000)+b'\xB9'+pack32(44)+bytes.fromhex('f3a6b8000000000f94c0a3')+pack32(0x10003000)+bytes.fromhex('890d')+pack32(0x10003004)+bytes.fromhex('5f5eb801000000c3');readout=bytes.fromhex('89c2')+load_abs(0,0x10003000)+load_abs(3,0x10003008)+load_abs(1,0x10003004)+END
        mem=[region(0x10000000,payload(raw)),region(0x10001000,intermediate(raw)),region(0x10002000,dispatch),region(0x10003000,bytes(12)),region(0x10004000,cb),region(0x10005000,struct.pack('<II',204,c['size'])),region(0x10006000,pack32(1)),region(0x10007000,readout),region(0x5B3EB0 if c['program']=='game' else 0x4B7F10,io),region(0x7FFF0000,pack32(0x10007000)+pack32(0x10005000)+pack32(0x10006000)+pack32(0x10002000))]
        if c['program']=='editor' and c['fail']:mem.append(region(0x4E4D4A,return_const(0,12)))
        expected=dict(EAX=int(not c['fail']),ECX=0,EBX=c['size'],EDX=int(not c['fail']))
    elif c['kind']=='converter':
        program='DK2 Editor pro.exe';entry=0x42FC20;raw=raw_record();seed=bytes([c['seed']])*44;readout=compare_code(0x10001000,0x10002000,44)+END
        mem=[region(0x10000000,payload(raw)),region(0x10001000,seed),region(0x10002000,intermediate(raw,seed)),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');expected=dict(EAX=1,ECX=0)
    elif c['kind']=='game':
        entry=0x521420;raw=raw_record(terrain=c['terrain'],health=c['health'],direction=c['direction'],owner=c['owner']);v=game_projection(raw,c['current'],valid_position=c['valid'],room_id=c['room']);room=bytearray([165]*0xB0);struct.pack_into('<H',room,0x1D,0x1234);struct.pack_into('<I',room,0x35,c['current']);want=bytearray(room);struct.pack_into('<I',want,0x9B,v['orientation']);vt=bytearray(0x204);struct.pack_into('<I',vt,0x200,0x1000A000);struct.pack_into('<I',vt,0x1D4,0x1000A100);tile=bytearray(16);struct.pack_into('<H',tile,2,0xF000|c['room']);log=bytearray(48);expected_log=bytearray(log)
        struct.pack_into('<6I',expected_log,0,v['position_checks'],v['tile_lookups'],v['checksum_calls'],v['refresh_calls'],int(v['heart_health'] is not None),int(v['portal_delta'] is not None))
        if v['portal_delta'] is not None:struct.pack_into('<II',expected_log,24,v['portal_delta'],0x1234)
        if v['heart_health'] is not None:struct.pack_into('<I',expected_log,32,v['heart_health'])
        if v['position'] is not None:struct.pack_into('<3I',expected_log,36,*v['position'])
        valid=bytes.fromhex('5657ff05')+pack32(0x10005000)+bytes.fromhex('8b74240cbf')+pack32(0x10005024)+b'\xB9'+pack32(3)+bytes.fromhex('f3a55f5e')+return_const(int(c['valid']),4)
        tile_get=bytes.fromhex('ff05')+pack32(0x10005004)+return_const(0x10003000,4);checksum=bytes.fromhex('ff05')+pack32(0x10005008)+bytes.fromhex('c20800');refresh=bytes.fromhex('ff05')+pack32(0x1000500C)+bytes.fromhex('c20400');heart=bytes.fromhex('ff05')+pack32(0x10005010)+bytes.fromhex('0fb7442404a3')+pack32(0x10005020)+bytes.fromhex('c20400');health=bytes.fromhex('ff05')+pack32(0x10005014)+bytes.fromhex('8b4424048b008905')+pack32(0x10005018)+bytes.fromhex('0fb7442408a3')+pack32(0x1000501C)+bytes.fromhex('c20800')
        readout=bytes.fromhex('89c2')+compare_code(0x10004000,0x10006000,0xB0,0x10009000)+compare_code(0x10005000,0x10007000,48)+load_abs(3,0x10009000)+END
        mem=[region(0x10000000,intermediate(raw)),region(0x10001000,pack32(0x10002000)),region(0x10002000,vt),region(0x10003000,tile),region(0x10004000,room),region(0x10005000,log),region(0x10006000,want),region(0x10007000,expected_log),region(0x10008000,readout),region(0x10009000,bytes(8)),region(0x1000A000,valid),region(0x1000A100,tile_get),region(0x5108C0,checksum),region(0x4EF6C0,refresh),region(0x4DB700,heart),region(0x4E5300,health),region(0x6EE544,pack32(0x10004000)),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10000000)+pack32(0x10001000))];expected=dict(EAX=1,ECX=0,EBX=1,EDX=1)
    elif c['kind']=='editor':
        return editor_case(c)
    elif c['kind']=='heart_serializer':
        program='DK2 Editor pro.exe';entry=0x4225A0;player=bytearray(0x100);player[0x1C]=3;struct.pack_into('<IIH',player,0x2C,3,4,65535);readout=compare_code(0x10001000,0x10002000,20)+END
        mem=[region(0x10000000,player),region(0x10001000,bytes([c['seed']])*20),region(0x10002000,writer_heart(3,3,4,65535,c['seed'])),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10001000))];regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0)
    else:return writer_case(c)
    return finalize(c,program,entry,mem,regs,expected)

def finalize(c,program,entry,mem,regs,expected):
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

def editor_case(c):
    raw=raw_record(terrain=c['terrain'],health=c['health'],direction=c['direction'],owner=c['owner']);out=editor_projection(raw,c['major'],c.get('tile',c['terrain']));world=bytearray(0xC00);player=bytearray(0x100);player[0x1C]=c['owner'];struct.pack_into('<IIH',player,0x2C,10,11,0x7777)
    struct.pack_into('<III',player,0xC8,0x10002000,0,1);struct.pack_into('<III',player,0xDC,0x10003000,0,1);struct.pack_into('<I',world,0x60,c['major']);struct.pack_into('<I',world,0x950,0x10004000);struct.pack_into('<I',world,0x990,10);struct.pack_into('<II',world,0xA64,0x10005000,1);tiles=bytearray(100);tiles[43]=c.get('tile',c['terrain']);want_player=bytearray(player);portal=bytes([c['seed']])*20;gate=portal;want_portal=bytearray(portal);want_gate=bytearray(gate)
    if out:
        if out['kind']=='heart':struct.pack_into('<IIH',want_player,0x2C,3,4,out['health'])
        elif out['kind']=='portal':
            struct.pack_into('<I',want_player,0xCC,1);want_portal[:]=struct.pack('<3IHBBHBB',3,4,0,0,0,0,out['health'],c['terrain'],c['owner'])
        else:
            struct.pack_into('<I',want_player,0xE0,1);want_gate[:]=struct.pack('<3IHBBHBB',3,4,0,0,c['direction'],0,100,c['terrain'],1)
    # Exclude only the unassigned scratch BYTE0F of each freshly constructed
    # portal/gate record; compare every player byte and all other record bytes.
    checks=[(0x10001000,0x10007000,256),(0x10002000,0x10008000,15),(0x10002010,0x10008010,4),(0x10003000,0x10009000,15),(0x10003010,0x10009010,4)]
    readout=b''.join(compare_code(a,b,n,0x1000A000+8*i) for i,(a,b,n) in enumerate(checks))+b'\xA1'+pack32(0x1000A000)
    for i in range(1,5):readout+=bytes.fromhex('2305')+pack32(0x1000A000+8*i)
    readout+=END
    mem=[region(0x10000000,world),region(0x10001000,player),region(0x10002000,portal),region(0x10003000,gate),region(0x10004000,tiles),region(0x10005000,pack32(0x10001000)),region(0x10006000,intermediate(raw)),region(0x10007000,want_player),region(0x10008000,want_portal),region(0x10009000,want_gate),region(0x1000A000,bytes(40)),region(0x1000B000,readout),region(0x7FFEFC00,bytes([c['seed']])*0x400),region(0x7FFF0000,pack32(0x1000B000)+pack32(0x10006000)+pack32(0x10000000))]
    return finalize(c,'DK2 Editor pro.exe',0x492030,mem,{},dict(EAX=1,ECX=0))

def writer_case(c):
    world=bytearray(0xC00);player=bytearray(0x100);player[0x1C]=c['owner'];struct.pack_into('<IIH',player,0x2C,3,4,c['health']);struct.pack_into('<II',world,0xA64,0x10001000,1);struct.pack_into('<II',player,0xC8,0x10003000,1);struct.pack_into('<II',player,0xDC,0x10004000,1)
    portal=payload(raw_record(health=c['health'],terrain=12,owner=c['owner'],z=0x11111111,flags=0x8001,opaque=0x5A));gate=payload(raw_record(health=0,terrain=37,owner=1,direction=255,z=0x22222222,flags=0xFFFE,opaque=0xA5));wanted_portal=writer_portal(portal);parts=[]
    if c['owner']>=3:parts.append(writer_heart(c['owner'],3,4,c['health'],c['seed']))
    parts.extend([wanted_portal,gate]);wanted=b''.join(parts);filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x10005000)
    body=bytes.fromhex('5657a1')+pack32(0x10006000)+bytes.fromhex('6bf81481c7')+pack32(0x10007000)+bytes.fromhex('8b74240cb914000000f3a4ff05')+pack32(0x10006000)+bytes.fromhex('5f5ec20800');capture=bytes.fromhex('817c24081400000075')+bytes([len(body)])+body+bytes.fromhex('c20800')
    readout=bytes.fromhex('0fb6d0')+compare_code(0x10007000,0x10008000,len(wanted),0x1000F000)+compare_code(0x10003000,0x10009000,20)+bytes.fromhex('2305')+pack32(0x1000F000)+load_abs(3,0x10006000)+END
    mem=[region(0x10000000,world),region(0x10001000,pack32(0x10002000)),region(0x10002000,player),region(0x10003000,portal),region(0x10004000,gate),region(0x10005000,capture),region(0x10006000,bytes(4)),region(0x10007000,bytes(60)),region(0x10008000,wanted),region(0x10009000,wanted_portal),region(0x1000A000,pack32(0x1000B000)),region(0x1000B000,filevt),region(0x1000C000,readout),region(0x1000F000,bytes(8)),region(0x7FFEFC00,bytes([c['seed']])*0x400),region(0x7FFF0000,pack32(0x1000C000)+pack32(0x1000A000))]
    for va,cleanup in cleanups().items():mem.append(region(va,return_const(0,cleanup)))
    return finalize(c,'DK2 Editor pro.exe',0x4947C0,mem,dict(ECX='0x10000000'),dict(EAX=1,ECX=0,EBX=len(parts),EDX=1))

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase39-emulation-room-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase39-room-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
