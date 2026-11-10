"""Original493200 writer and56C100 converter with full byte comparisons.

Real stack probe, string widen/pad helpers, flags and array getters execute.
CString ownership/name/message lookup, date and file I/O are explicit fixtures.
All program substitutions exist only in EmulatorHelper memory.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_level_descriptor import *
ROOT=Path(__file__).resolve().parent

def cases():
    rows=[]
    for seed in [0,165,255]:
        for populated in [False,True]:
            for high in [False,True]:rows.append(dict(name=f'writer-{seed}-{int(populated)}-{int(high)}',kind='writer',seed=seed,populated=populated,high=high))
    rows.append(dict(name='writer-patterned-stack',kind='writer',seed=19,populated=True,high=True,pattern=True))
    rows.append(dict(name='writer-long-information',kind='writer',seed=165,populated=False,high=False,long_information=True))
    for opaque in [0,165,255]:
        for mid_seed in [0,165,255]:rows.append(dict(name=f'converter-{opaque}-{mid_seed}',kind='converter',opaque=opaque,mid_seed=mid_seed))
    for opaque in [0,165,255]:
        for mid_seed in [0,165,255]:rows.append(dict(name=f'editor-converter-{opaque}-{mid_seed}',kind='editor_converter',opaque=opaque,mid_seed=mid_seed))
    for wide in [False,True]:
        for source,count in [(b'A\x80B',2),(b'A\xFF',3),(b'Z',0)]:rows.append(dict(name=f'string-{int(wide)}-{count}',kind='string',wide=wide,source_hex=source.hex(),count=count))
    return rows

def cstring(source):return struct.pack('<iii',1,len(source),len(source))+source+b'\0'

def fixture(c):
    model=bytearray(0xC00)
    for i in range(0x8C,0xDD):model[i]=(i*17+23)&255
    struct.pack_into('<HH',model,0x944,0x1234,20)
    source=b'Level\x80\xFF' if c.get('high') else b'Level'
    strings={k:source for k in ['name','description','author','email','information','terrain_path','sound_category','hero_name']};strings['messages']=[source]*20
    if c.get('long_information'):strings['information']=b'I'*1026+b'\x80'
    players=[]
    for i in range(4 if c.get('populated') else 2):
        p=bytearray(96);p[0x1C]=i;p[0x40]=0;players.append(p)
    for off in [0x70,0x74,0x78,0x7C,0x80,0x84,0x88]:struct.pack_into('<I',model,off,0x1000200C)
    if c.get('long_information'):struct.pack_into('<I',model,0x7C,0x1000300C)
    refs=bytes((i*13+5)&255 for i in range(144)) if c.get('populated') else b'';ids=bytes([7,81,255]) if c.get('populated') else b''
    struct.pack_into('<II',model,0xE4,0x10004000,len(refs)//72);struct.pack_into('<II',model,0xF8,0x10004100,len(ids));struct.pack_into('<II',model,0xA64,0x10004400,len(players))
    return model,strings,players,refs,ids,source

def build_case(c):
    regs={};mem=[];program='DK2 Editor pro.exe'
    if c['kind']=='writer':
        entry=0x493200;model,strings,players,refs,ids,source=fixture(c)
        stack_seed=bytes(((i*17+c['seed'])&255 if c.get('pattern') else c['seed']) for i in range(0x6600));payload_seed=stack_seed[0x1A0:0x1A0+SIZE]
        payload=writer_payload(payload_seed,model,strings,players);n=len(refs)//72;k=len(ids);whole=0x6493+len(refs)+4*k
        header=struct.pack('<5I2HI',220,4,whole,221,28,n,k,0x3000)+bytes(20)
        creature_seed=stack_seed[0x110:0x114];creature_data=b''.join(bytes([ident])+creature_seed[1:] for ident in ids)
        want=header+struct.pack('<II',223,SIZE)+payload+struct.pack('<II',222,len(refs)+4*k)+refs+creature_data;assert len(want)==whole
        vt=bytearray(0x40);struct.pack_into('<I',vt,0x38,0x10008000)
        capture=bytes.fromhex('5657538b7424108b4c24148b1d')+pack32(0x10006000)+bytes.fromhex('8dbb')+pack32(0x10020000)+bytes.fromhex('01cb891d')+pack32(0x10006000)+bytes.fromhex('ff05')+pack32(0x10006004)+bytes.fromhex('f3a45b5f5ec20800')
        date=bytes.fromhex('8b442404c70000000000c740040000000066c740080000c20400')
        readout=compare_code(0x10020000,0x10030000,len(want))+load_abs(3,0x10006004)+load_abs(6,0x10006000)+bytes.fromhex('0fb715')+pack32(0x100000DC)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,model),region(0x10001000,pack32(0x10001100)),region(0x10001100,vt),region(0x10001200,pack32(0x1000200C)),region(0x10002000,cstring(source)),region(0x10003000,cstring(strings['information'])),region(0x10004000,refs or bytes(4)),region(0x10004100,ids or bytes(4)),region(0x10004200,b''.join(players)),region(0x10004400,b''.join(pack32(0x10004200+96*i) for i in range(len(players)))),region(0x10006000,bytes(8)),region(0x10008000,capture),region(0x10009000,readout),region(0x10020000,bytes(whole)),region(0x10030000,want),region(0x48F560,return_const(0x10001200)),region(0x48E930,return_const(0x10001200,4)),region(0x4DDE57,bytes.fromhex('8b4424048901c20400')),region(0x4DDDE9,b'\xC3'),region(0x4AB320,date),region(0x7FFE9A00,stack_seed),region(0x7FFF0000,pack32(0x10009000)+pack32(0x10001000))]
        regs=dict(ECX='0x10000000');flags=struct.unpack_from('<H',payload,0x638C)[0];expected=dict(EAX=1,EBX=4+n+k,ECX=0,EDX=flags,ESI=whole)
    elif c['kind'] in ['converter','editor_converter']:
        program='DKII-DX.exe' if c['kind']=='converter' else 'DK2 Editor pro.exe';entry=0x56C100 if c['kind']=='converter' else 0x42BDE0;model,strings,players,refs,ids,source=fixture({});raw=bytearray(writer_payload(bytes(SIZE),model,strings,players))
        for off,n in OPAQUE:raw[off:off+n]=bytes([c['opaque']])*n
        raw[0x63CA]=c['opaque'];seed=bytes([c['mid_seed']])*MID_SIZE;want=intermediate(bytes(raw),seed)
        readout=compare_code(0x10010000,0x10020000,MID_SIZE)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        mem=[region(0x10000000,raw),region(0x10010000,seed),region(0x10020000,want),region(0x10008000,readout),region(0x7FFF0000,pack32(0x10008000)+pack32(0x10000000))];regs=dict(ECX='0x10010000');expected=dict(EAX=1,ECX=0)
    else:
        entry=0x48CF90 if c['wide'] else 0x4AB3B0;source=bytes.fromhex(c['source_hex']);count=c['count'];encoded=ansi_to_wide(source,count) if c['wide'] else ansi_copy(source,count);want=encoded+bytes([165])*(16-len(encoded))
        readout=compare_code(0x10001000,0x10002000,16)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        first,second=(0x10003000,0x10001000) if c['wide'] else (0x10001000,0x10003000)
        mem=[region(0x10000000,cstring(source)),region(0x10001000,bytes([165])*16),region(0x10002000,want),region(0x10003000,pack32(0x1000000C)),region(0x10004000,readout),region(0x7FFF0000,pack32(0x10004000)+pack32(first)+pack32(second)+pack32(count))];expected=dict(EAX=1,ECX=0)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])), 'Overlapping fixture regions'
    return dict(program=program,address=hex(entry),max_steps=250000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase35-emulation-level-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r)
                assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase35-level-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
