"""Original Object callback/converter, editor loader and writer field fragment.

Only EmulatorHelper memory is modified. Full game callback uses a checksum stub
and an explicit creator fixture; original gold helper runs (no x87 path selected).
Full editor loader uses array/creation fixtures; original definition lookup,
vector constructor, setters and byte selector execute. Writer starts at494DEA
after header/envelope, executing all24-byte payload assignments and actual getters.
Editor owner/position/trigger virtuals and I/O are deterministic fixtures.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from emulate_placed_structures import load_abs
from native_placed_objects import *
ROOT=Path(__file__).resolve().parent

def cases():
    rows=[]
    def game(name,**kw):
        c=dict(name=name,kind='game',object_id=7,spell=2,money=1000,flags=8,initial_type=55,initial_value=0xA5,success=1,x=3,y=4,z=0x12345678)
        c.update(kw);rows.append(c)
    for spell in [*range(20),258,0xFFFFFFFF]:game('spell-'+str(spell),spell=spell)
    for oid in sorted(TRAP_CRATE_OBJECT_IDS|DOOR_CRATE_OBJECT_IDS|{0,4,6,28,35,45,51,58,91,93,137,139,255}):game('crate-'+str(oid),object_id=oid,flags=16)
    for flags in [0,4,0x18,0x48,0x50,0x58,0xFFFFFFFF]:game('priority-'+str(flags),object_id=36,flags=flags)
    for initial,amount in [(0,0),(55,0),(55,0xFFFFFFFF),(0,0xFFFFFFFF),(0,0x80000000),(0,0x7FFFFFFF),(55,0x7FFFFFFF),(0xFFFFFFFF,1),(0xFFFFFF00,512)]:
        game(f'gold-{initial}-{amount}',object_id=3,flags=0x40,initial_type=initial,money=amount)
    game('creator-actual-id',object_id=3,actual_id=52,flags=16)
    game('creator-failure',success=0)
    game('negative-coordinate',x=0xFFFFFFFF,y=0x80000000)
    for z in [0,0xFFFFFFFF]:game(f'placement-opaque-{z}',z=z,observe_position=True)
    for flags in [0,4,8,16,0x18,0x40,0x48,0x44,0x58,0xFFFFFFFF]:
        rows.append(dict(name=f'writer-{flags}',kind='writer',flags=flags,spell=0xA5,money=0xFFFFFFFF))
    for flags in [0,4,8,16,0x18,0x40,0x48,0x44]:
        rows.append(dict(name=f'editor-load-{flags}',kind='editor_load',flags=flags,spell=258,money=0x87654321,object_id=7))
    for spell in [0,17,18,0xFFFFFFFF]:rows.append(dict(name=f'editor-selector-{spell}',kind='editor_load',flags=8,spell=spell,money=1000,object_id=7))
    for z in [0,0xFFFFFFFF,0x12345678]:rows.append(dict(name=f'converter-{z}',kind='converter',z=z))
    return rows

def return_const(value,cleanup=0):return b'\xB8'+pack32(value)+(b'\xC2'+struct.pack('<H',cleanup) if cleanup else b'\xC3')

def build_case(c):
    regs={};program='DKII-DX.exe';mem=[]
    if c['kind']=='game':
        entry=0x520BB0;mid=bytearray(48);world=bytearray(0x100);player=bytearray(0x1200);node=bytearray(0x200);definition=bytearray(0x120)
        struct.pack_into('<5I',mid,0x18,c['x'],c['y'],c['z'],c['spell'],c['money']);struct.pack_into('<HBB',mid,0x2C,0x1234,c['object_id'],1)
        struct.pack_into('<I',world,0x84,0x10003000);struct.pack_into('<H',player,4,0x4321)
        actual=c.get('actual_id',game_created_id(c['object_id'],c['spell']));node[0x12C]=actual or 0;struct.pack_into('<I',node,0x12D,0x10006000)
        struct.pack_into('<I',node,0x14A,c['initial_type']);node[0x14E]=c['initial_value'];struct.pack_into('<I',definition,0xF4,c['flags'])
        create=bytes.fromhex('ff05')+pack32(0x10007000)+bytes.fromhex('8b44240425ff000000a3')+pack32(0x10007004)+bytes.fromhex('8b44240825ffff0000a3')+pack32(0x10007008)
        create+=bytes.fromhex('56578b742414bf')+pack32(0x10007010)+bytes.fromhex('b903000000f3a58b442418c700')+pack32(0x10005000)+bytes.fromhex('5f5e')+return_const(c['success'],16)
        readout=bytes.fromhex('89c5')+load_abs(0,0x1000514A)+bytes.fromhex('0fb61d')+pack32(0x1000514E)+load_abs(1,0x10007004)+load_abs(2,0x10007000)+load_abs(6,0x10007010)+load_abs(7,0x10007014)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,mid),region(0x10001000,world),region(0x10003000,player),region(0x10005000,node),region(0x10006000,definition),region(0x10007000,bytes(32)),region(0x10008000,readout),region(0x5108C0,bytes.fromhex('c20800')),region(0x4B7A90,create),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10000000)+pack32(0x10001000))]
        created=game_created_id(c['object_id'],c['spell']);active=created is not None
        out=game_payload(c['flags'],actual or 0,c['spell'],c['money'],c['initial_type'],c['initial_value']) if active and c['success'] else dict(type_or_amount=c['initial_type'],value=c['initial_value'])
        expected=dict(EAX=out['type_or_amount'],EBX=out['value'],ECX=created if active else 0,EDX=int(active),ESI=center_q12(c['x']) if active else 0,EDI=center_q12(c['y']) if active else 0,EBP=c['success'] if active else 1)
        if c.get('observe_position'):
            readout=bytes.fromhex('89c5')+load_abs(0,0x1000514A)+bytes.fromhex('0fb61d')+pack32(0x1000514E)+load_abs(1,0x10007008)+load_abs(2,0x10007018)+load_abs(6,0x10007010)+load_abs(7,0x10007014)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem[6]=region(0x10008000,readout);expected.update(ECX=0x4321,EDX=0)
    elif c['kind'] in ['writer','editor_load']:
        program='DK2 Editor pro.exe';model=bytearray(0x40);definition=bytearray(0x440);vtable=bytearray(0x40);owner=bytearray(32)
        oid=c.get('object_id',3);model[0x18]=oid;model[0x19]=c['spell']&255 if c['kind']=='writer' else 0
        struct.pack_into('<I',model,0x1C,c['money'] if c['kind']=='writer' else 0);struct.pack_into('<I',model,0,0x10003000);definition[0x168]=oid
        for off,mask in [(0x3D8,4),(0x3DC,8),(0x3E4,16),(0x3EC,64)]:struct.pack_into('<I',definition,off,int(bool(c['flags']&mask)))
        owner[0x1C]=1
        # Native4205D0 searches this one-entry definition catalogue.
        mem=[region(0x10001000,model),region(0x10002000,definition),region(0x10004000,owner),region(0x54FA7C,pack32(1)),region(0x54FA78,pack32(0x10005000)),region(0x10005000,pack32(0x10002000))]
        if c['kind']=='writer':
            entry=0x4947C0;regs=dict(EBP='0x10001000',EDI='0x10006000')
            for off,address in [(4,0x10009000),(0x14,0x10009010),(0x18,0x10009020),(0x28,0x10009030)]:struct.pack_into('<I',vtable,off,address)
            filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x10009100)
            capture=bytes.fromhex('56578b74240cbf')+pack32(0x10008000)+bytes.fromhex('b906000000f3a55f5ec20800')
            readout=load_abs(0,0x1000800C)+load_abs(3,0x10008010)+load_abs(1,0x10008008)+load_abs(2,0x10008014)+load_abs(6,0x10008000)+load_abs(7,0x10008004)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem += [region(0x10003000,vtable),region(0x10006000,pack32(0x10007000)),region(0x10007000,filevt),region(0x10008000,bytes([0xCC])*24),region(0x10009000,return_const(0x10004000)),region(0x10009010,return_const(3)),region(0x10009020,return_const(4)),region(0x10009030,return_const(0x1234)),region(0x10009100,capture),region(0x7FFF0000,bytes([0xA5])*0xC0),region(entry,jump(entry,0x494DEA)),region(0x494E76,readout)]
            out=editor_getters(c['flags'],c['spell'],c['money']);expected=dict(EAX=out['spell_bits'],EBX=out['money_bits'],ECX=0,EDX=0x01031234,ESI=3,EDI=4)
        else:
            entry=0x4916B0;mid=bytearray(48);struct.pack_into('<6I',mid,0,*([1]*6));struct.pack_into('<5I',mid,0x18,3,4,0x12345678,c['spell'],c['money']);mid[0x2E]=oid;mid[0x2F]=1
            struct.pack_into('<I',vtable,0x2C,0x10009000)
            readout=bytes.fromhex('0fb605')+pack32(0x10001018)+bytes.fromhex('0fb61d')+pack32(0x10001019)+load_abs(1,0x1000101C)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
            mem += [region(0x10000000,mid),region(0x10003000,vtable),region(0x10009000,bytes.fromhex('c20400')),region(0x45BA10,return_const(0x10002000,4)),region(0x4898C0,return_const(0x10001000,8)),region(0x10008000,readout),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10000000)+pack32(0x10006000))]
            out=editor_load_projection(oid,c['flags'],c['spell'],c['money']);expected=dict(EAX=out['object_id'],EBX=out['model_spell_byte'],ECX=out['model_money_bits'])
    else:
        entry=0x56F730;raw=struct.pack('<5IHBB',3,4,c['z'],0x12345678,0xFFFFFFFF,0x1234,7,1);regs=dict(ECX='0x10001000')
        readout=b''.join(load_abs(r,0x10001000+off) for r,off in [(0,0x18),(3,0x1C),(1,0x20),(2,0x24),(6,0x28),(7,0x2C)])+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,raw),region(0x10001000,bytes(48)),region(0x10002000,readout),region(0x7FFF0000,pack32(0x10002000)+pack32(0x10000000))]
        expected=dict(EAX=3,EBX=4,ECX=c['z'],EDX=0x12345678,ESI=0xFFFFFFFF,EDI=0x01071234)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])), 'Overlapping fixture regions'
    return dict(program=program,address=hex(entry),max_steps=5000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase30-emulation-object-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']));print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase30-object-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
