"""Full two Keeper cast routes with explicit world/validation/factory fixtures.

Execute native field selection, factory argument formation, output Shot fields,
mana accounting, timestamps and sound dispatch. No actual Shot processing/game.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
    rows=[]
    for entry in [0x4BBF90,0x4BC290]:
        for upgraded in [0,1]:
            for primary,bonus in [(20,40),(21,40),(0,40),(20,0)]:
                for c8 in [0,1,0x7FFFFFFF,0x80000000,0xFE0C0000,0xFFFFFFFF]:rows.append(dict(name=f'cast-{entry:x}-{upgraded}-{primary}-{bonus}-{c8}',entry=entry,upgraded=upgraded,primary=primary,bonus=bonus,c8=c8,rule=1,exists=1,afford=1,valid=1,create=1,flags=0))
        for rule in range(7):rows.append(dict(name=f'rule-{entry:x}-{rule}',entry=entry,upgraded=1,primary=20,bonus=40,c8=0xFE0C0000,rule=rule,exists=1,afford=1,valid=1,create=1,flags=0))
        for stop in ['exists','afford','valid','create']:
            c=dict(name=f'stop-{entry:x}-{stop}',entry=entry,upgraded=1,primary=20,bonus=40,c8=0xFFFFFFFF,rule=1,exists=1,afford=1,valid=1,create=1,flags=0);c[stop]=0;rows.append(c)
        rows.append(dict(name=f'force-mana-{entry:x}',entry=entry,upgraded=1,primary=20,bonus=40,c8=0xFFFFFFFF,rule=1,exists=1,afford=0,valid=1,create=1,flags=1))
    return rows
def build_case(c):
    player=bytearray(0x1100);struct.pack_into('<H',player,4,7);struct.pack_into('<I',player,0x6E,1);struct.pack_into('<I',player,0x72,0x10005000);struct.pack_into('<I',player,0x10F2,111)
    world=pack32(0x10002000)+bytes(16);vt=bytearray(0x1B4);struct.pack_into('<I',vt,0x10C,0x10010000);struct.pack_into('<I',vt,0x110,0x10010100);struct.pack_into('<I',vt,0x1B0,0x10010200)
    definition=bytearray(122);struct.pack_into('<I',definition,0x3A,c['c8']);definition[0x42]=c['primary'];struct.pack_into('<II',definition,0x43,0x11223344,0x55667788);definition[0x4B]=c['bonus'];struct.pack_into('<II',definition,0x4C,0x99AABBCC,0xDDEEFF00);struct.pack_into('<I',definition,0x58,1234);definition[0x75]=5;definition[0x77]=c['rule'];struct.pack_into('<I',definition,0x6D,0x11223344)
    shotdef=bytes(0x80);state=bytearray(17);state[0]=5;struct.pack_into('<I',state,13,c['upgraded']);target=bytearray(0x410);position=[0x1200,0xFFFFDE00,0x3300];struct.pack_into('<3I',target,0x1A,*position)
    shot=bytes([165])*0x173;wantedshot=bytearray(shot);wantedplayer=bytearray(player);wantedstate=bytearray(state);capture=bytearray(0x40)
    selected=c['bonus'] if c['upgraded'] else c['primary'];reached=bool(c['exists'] and (c['afford'] or c['flags']&1) and c['valid'] and selected);success=bool(reached and c['create'])
    if reached:
        struct.pack_into('<I',capture,0,1);struct.pack_into('<I',capture,4,c['primary']);pos=list(position)
        if c['entry']==0x4BBF90 and c['primary']==21:pos[2]+=0x2000
        struct.pack_into('<3I',capture,8,*pos);struct.pack_into('<II',capture,0x14,1,selected);struct.pack_into('<I',capture,0x28,7);struct.pack_into('<I',capture,0x34,1)
    if success:
        d1,d2=(0x99AABBCC,0xDDEEFF00) if c['upgraded'] else (0x11223344,0x55667788)
        struct.pack_into('<II',wantedshot,0x13D,d1,d2);struct.pack_into('<H',wantedshot,0x147,9)
        if c['rule'] in [1,2,3,6]:struct.pack_into('<I',wantedshot,0x14D,1)
        struct.pack_into('<I',wantedplayer,0x10F2,1345);struct.pack_into('<II',wantedstate,5,0x11223344,0x11223344);struct.pack_into('<II',capture,0x1C,1,(-1234)&0xFFFFFFFF);struct.pack_into('<I',capture,0x24,1);struct.pack_into('<I',capture,0x2C,1)
    # Lookup only receives selected Shot, factory only receives the primary Shot.
    lookup=bytes.fromhex('ff05')+pack32(0x10008014)+bytes.fromhex('0fb6442404a3')+pack32(0x10008018)+return_const(0x10004000,4)
    creator=bytes.fromhex('5657ff05')+pack32(0x10008000)+bytes.fromhex('0fb644240ca3')+pack32(0x10008004)+bytes.fromhex('0fb7442410a3')+pack32(0x10008028)+bytes.fromhex('8b742414bf')+pack32(0x10008008)+b'\xB9'+pack32(3)+bytes.fromhex('f3a58b4424180fb700a3')+pack32(0x10008030)+bytes.fromhex('8b44241cc700')+pack32(0x10007000)+bytes.fromhex('8b442420a3')+pack32(0x10008034)+bytes.fromhex('5f5e')+return_const(c['create'],24)
    mana=bytes.fromhex('ff05')+pack32(0x1000801C)+bytes.fromhex('8b442404a3')+pack32(0x10008020)+bytes.fromhex('c20400')
    sound=bytes.fromhex('ff05')+pack32(0x10008024)+bytes.fromhex('c21000');soundvt=bytearray(0x24);struct.pack_into('<I',soundvt,0x20,0x10010500)
    clock=bytes.fromhex('ff05')+pack32(0x1000802C)+return_const(0x11223344)
    # Preserve native return before comparison; both routes return0 even after success.
    readout=b'\xA3'+pack32(0x10008038)+b'\xBD'+pack32(1)
    for actual,expected,n in [(0x10007000,0x1000B000,len(shot)),(0x10000000,0x1000C000,len(player)),(0x10005000,0x1000E000,len(state)),(0x10003000,0x1000E100,len(definition)),(0x10008000,0x1000E200,len(capture))]:readout+=compare_code(actual,expected,n)+bytes.fromhex('21c5')
    readout+=END
    mem=[region(0x10000000,player),region(0x10012000,world),region(0x10002000,vt),region(0x10003000,definition),region(0x10004000,shotdef),region(0x10005000,state),region(0x10006000,target),region(0x10007000,shot),region(0x10008000,bytes(0x40)),region(0x10009000,pack32(0x1000A000)),region(0x1000A000,soundvt),region(0x1000B000,wantedshot),region(0x1000C000,wantedplayer),region(0x1000E000,wantedstate),region(0x1000E100,definition),region(0x1000E200,capture),region(0x1000F000,readout),region(0x10010000,return_const(c['exists'],4)),region(0x10010100,return_const(0x10003000,4)),region(0x10010200,lookup),region(0x10010500,sound),region(0x4C1740,return_const(c['afford'],4)),region(0x4BBA40 if c['entry']==0x4BBF90 else 0x4BBBC0,return_const(c['valid'],12)),region(0x4BB010,return_const(0x10005000,4)),region(0x4B7CD0,creator),region(0x4C19C0,mana),region(0x517490,clock),region(0x6E5054,pack32(0x10012000)),region(0x6EE540+9*4,pack32(0x10006000)),region(0x75B468,pack32(0x10009000)),region(0x7FFF0000,pack32(0x1000F000)+pack32(5)+pack32(9)+pack32(c['flags']))]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DKII-DX.exe',address=hex(c['entry']),max_steps=30000,registers=json.dumps(dict(ECX='0x10000000')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase53-emulation-keeper-cast-v2-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase53-keeper-cast-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
