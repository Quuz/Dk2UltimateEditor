"""Native loaders, intermediate extent, editor reference fragments and lookups.

No game process or executable changes. Resource provider is null for complete
game loader; all70 output bytes compared, including checksum over346 bytes.
Editor callbacks/panels execute explicit bounded fragments; full UI not run.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_spell_alternatives import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def raw_record(shot=36,room=4,tail=165):
    raw=bytearray((i*31+7)&255 for i in range(266));raw[:32]=b'NativeSpell\0'+bytes(20);raw[232]=shot;raw[233]=room;raw[238]=2;raw[239:]=bytes([tail])*27;return bytes(raw)

def cases():
    rows=[]
    rows.append(dict(name='ddx-bindings',kind='ddx'))
    for program in ['game','editor']:
        for seed in [0,165,255]:rows.append(dict(name=f'init-{program}-{seed}',kind='init',program=program,seed=seed))
        for init in [False,True]:
            for tail in [0,165,255]:rows.append(dict(name=f'converter-{program}-{init}-{tail}',kind='converter',program=program,initialize=init,tail=tail,seed=165))
    for gate in [0,1]:
        for shot,room in [(0,0),(36,4),(68,0),(255,255)]:rows.append(dict(name=f'loader-{gate}-{shot}-{room}',kind='loader',gate=gate,shot=shot,room=room,seed=165,tps=20,tail=0))
    for tail in [165,255]:rows.append(dict(name=f'loader-ignored-mid-tail-{tail}',kind='loader',gate=0,shot=36,room=4,seed=165,tps=20,tail=tail))
    for tps in [0,0xFFFFFFFF]:rows.append(dict(name=f'loader-tps-{tps}',kind='loader',gate=0,shot=36,room=4,seed=0,tps=tps,tail=0))
    for p28,p2c in [(0,0),(0,1),(1,0),(1,1)]:
        for shot,room in [(0,0),(36,4),(255,255)]:rows.append(dict(name=f'model-{p28}-{p2c}-{shot}-{room}',kind='model',p28=p28,p2c=p2c,shot=shot,room=room))
    for field in ['shot','room']:
        for ids,value in [([0,36,255,36],36),([0,36,255,36],255),([0,36,255,36],4),([],0),([0],0)]:rows.append(dict(name=f'lookup-{field}-{len(ids)}-{value}',kind='lookup',field=field,ids=ids,value=value))
    for shot,room in [(36,4),(0,0),(255,255),(68,12)]:rows.append(dict(name=f'fill-{shot}-{room}',kind='fill',shot=shot,room=room))
    for shot_idx,room_idx,guard in [(1,1,165),(0,0,0),(2,2,255),(-1,1,17),(1,-1,34),(-1,-1,68)]:rows.append(dict(name=f'commit-{shot_idx}-{room_idx}-{guard}',kind='commit',shot_idx=shot_idx,room_idx=room_idx,guard=guard))
    return rows

def build_case(c):
    mem=[];regs={};program='DK2 Editor pro.exe';kind=c['kind']
    if kind=='ddx':
        entry=0x443010;panel=bytes(0x110);ddx=bytes(16)
        wanted=b''.join(struct.pack('<II',control,0x10000000+offset) for control,offset in [(0x6FE,0x74),(0x704,0x7C),(0x703,0x78),(0x705,0x80),(0x95,0x8C),(0x98,0x90),(0x9A,0x94),(0x96,0x98),(0x97,0x9C),(0x99,0xA0),(0x9C,0xA4),(0x9B,0xA8)])
        capture=bytes.fromhex('8b15')+pack32(0x10001000)+bytes.fromhex('8b44240889028b44240c89420483c2088915')+pack32(0x10001000)+bytes.fromhex('ff05')+pack32(0x10001004)+bytes.fromhex('c20c00')
        readout=compare_code(0x10002000,0x10003000,len(wanted))+load_abs(3,0x10001004)+END
        mem=[region(0x10000000,panel),region(0x10001000,pack32(0x10002000)+pack32(0)),region(0x10002000,bytes(len(wanted))),region(0x10003000,wanted),region(0x10004000,readout),region(0x10005000,ddx),region(0x4E4A2F,capture),region(0x4E490A,capture),region(0x7FFF0000,pack32(0x10004000)+pack32(0x10005000))]
        for va,cleanup in [(0x4E4889,12),(0x4E4B31,16),(0x4E4F78,12),(0x4E5038,24),(0x4E485A,12)]:mem.append(region(va,return_const(1,cleanup)))
        regs=dict(ECX='0x10000000');expected=dict(EAX=1,ECX=0,EBX=12)
    elif kind in ['init','converter']:
        program='DKII-DX.exe' if c['program']=='game' else program;init=0x56A490 if c['program']=='game' else 0x42A000;conv=0x56A560 if c['program']=='game' else 0x42A0D0
        wantbuf=bytes(346) if kind=='init' else full_intermediate(raw_record(tail=c['tail']),0 if c['initialize'] else c['seed'])
        regs=dict(ECX='0x10001000');entry=init if kind=='init' or c['initialize'] else conv
        readout=compare_code(0x10001000,0x10002000,346)+END
        mem=[region(0x10001000,bytes([c['seed']])*346),region(0x10002000,wantbuf),region(0x10003000,readout)]
        if kind=='init':mem.append(region(0x7FFF0000,pack32(0x10003000)))
        else:
            mem.append(region(0x10000000,raw_record(tail=c['tail'])))
            if c['initialize']:
                adapter=b'\xB9'+pack32(0x10001000)+b'\x68'+pack32(0x10003000)+jump(0x10004000+10,conv)
                mem.extend([region(0x10004000,adapter),region(0x7FFF0000,pack32(0x10004000)+pack32(0x10000000))])
            else:mem.append(region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000)))
        expected=dict(EAX=1,ECX=0)
    elif kind=='loader':
        program='DKII-DX.exe';entry=0x4F7860;mid=bytearray(full_intermediate(raw_record(c['shot'],c['room']),0));mid[319:]=bytes([c['tail']])*27;node=bytes([c['seed']])*70;wantbuf=runtime_projection(mid,node,c['tps'],c['gate']);readout=compare_code(0x10001000,0x10002000,70)+END
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,wantbuf),region(0x10003000,readout),region(0x6FCDCF,pack32(c['gate'])),region(0x6F2590,pack32(c['tps'])),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000)+pack32(0))];regs=dict(ECX='0x10001000');expected=dict(EAX=1,ECX=0)
    elif kind=='model':
        entry=0x4A2260;mid=bytearray(346);struct.pack_into('<I',mid,0x28,c['p28']);struct.pack_into('<I',mid,0x2C,c['p2c']);mid[0x138:0x13A]=bytes([c['shot'],c['room']]);node=bytes([165])*0x200;wantbuf=bytearray(node);v=model_reference_projection(c['shot'],c['room'],c['p28'],c['p2c']);wantbuf[0x16F]=v['alternative_shot_id'];wantbuf[0x16E]=v['alternative_room_id'];readout=compare_code(0x10001000,0x10002000,len(node))+END
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,wantbuf),region(0x10003000,readout),region(entry,jump(entry,0x4A23E4)),region(0x4A2406,jump(0x4A2406,0x10003000))];regs=dict(ESI='0x10000000',EBP='0x10001000',EDI='0x0');expected=dict(EAX=1,ECX=0)
    elif kind=='lookup':
        entry=0x443C20 if c['field']=='shot' else 0x443C50;off=0xD4 if c['field']=='shot' else 0xE8;panel=bytearray(0x110);struct.pack_into('<2I',panel,off,0x10001000,len(c['ids']));mem=[region(0x10000000,panel),region(0x10001000,bytes(c['ids']) or b'\xA5'),region(0x7FFF0000,pack32(0xDEADBEEF)+pack32(c['value']))];regs=dict(ECX='0x10000000');expected=dict(EAX=lookup(bytes(c['ids']),c['value'])&0xFFFFFFFF)
    elif kind=='fill':
        entry=0x4439A0;panel=bytearray([165])*0x110;struct.pack_into('<I',panel,0xAC,0x10001000);struct.pack_into('<2I',panel,0xD4,0x10003000,3);struct.pack_into('<2I',panel,0xE8,0x10004000,3);node=bytearray(0x200);node[0x16F]=c['shot'];node[0x16E]=c['room'];wantbuf=bytearray(panel);struct.pack_into('<i',wantbuf,0x78,lookup(bytes([0,36,255]),c['shot']));struct.pack_into('<i',wantbuf,0x7C,lookup(bytes([0,4,255]),c['room']));readout=compare_code(0x10000000,0x10002000,len(panel))+END
        mem=[region(0x10000000,panel),region(0x10001000,node),region(0x10002000,wantbuf),region(0x10003000,bytes([0,36,255])),region(0x10004000,bytes([0,4,255])),region(0x10005000,readout),region(entry,jump(entry,0x443A69)),region(0x443AA7,jump(0x443AA7,0x10005000))];regs=dict(ESI='0x10000000');expected=dict(EAX=1,ECX=0)
    elif kind=='commit':
        entry=0x443CF0;panel=bytearray(0x110);struct.pack_into('<I',panel,0xAC,0x10001000);struct.pack_into('<I',panel,0xD4,0x10003001);struct.pack_into('<I',panel,0xE8,0x10004001);struct.pack_into('<i',panel,0x78,c['shot_idx']);struct.pack_into('<i',panel,0x7C,c['room_idx']);node=bytes([165])*0x200;wantbuf=bytearray(node);shotlist=bytes([c['guard'],0,36,255]);roomlist=bytes([c['guard'],0,4,255]);wantbuf[0x16F]=shotlist[c['shot_idx']+1];wantbuf[0x16E]=roomlist[c['room_idx']+1];readout=compare_code(0x10001000,0x10002000,len(node))+END
        mem=[region(0x10000000,panel),region(0x10001000,node),region(0x10002000,wantbuf),region(0x10003000,shotlist),region(0x10004000,roomlist),region(0x10005000,readout),region(entry,jump(entry,0x443D72)),region(0x443DA2,jump(0x443DA2,0x10005000))];regs=dict(ESI='0x10000000');expected=dict(EAX=1,ECX=0)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase42-emulation-spell-alt-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase42-spell-alternative-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
