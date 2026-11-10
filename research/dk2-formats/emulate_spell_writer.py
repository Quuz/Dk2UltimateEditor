"""Full Editor Pro CreatureSpells writer, controlled file/virtual/date fixtures.

All4A9730 instructions,41D6F0 flags,41CE60 resource getter,4AB3B0 string
copy and4ACDA8 ftol execute. Floating inputs multiply4096 to exact integers;
these cases do not establish fractional rounding. No game/database byte edits.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
FLAGS=[(1,0x184),(2,0x188),(4,0x18C),(8,0x190),(32,0x194),(64,0x198),(128,0x19C),(256,0x1A0)]
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    out=[]
    for seed in [0,165,255,'pattern']:
        out.append(dict(name=f'writer-{seed}',kind='writer',seed=seed,flags=0xFFFFFFFF,slots=[1,2],label=32))
    for flags in [0,*[b for b,_ in FLAGS]]:
        out.append(dict(name=f'writer-flags-{flags}',kind='writer',seed=165,flags=flags,slots=[1],label=0))
    for slots in [[],[0],[1,0,2],[0,2],[2,1]]:
        out.append(dict(name='writer-slots-'+''.join(map(str,slots)),kind='writer',seed='pattern',flags=0x105,slots=slots,label=5))
    out.append(dict(name='writer-overlong-name-240',kind='writer',seed=165,flags=0x105,slots=[1],label=240))
    for flags in [0,0xFFFFFFFF,*[b for b,_ in FLAGS]]:
        out.append(dict(name=f'flags-{flags}',kind='flags',flags=flags))
        for presence in [0,1]:out.append(dict(name=f'model-{flags}-{presence}',kind='model',flags=flags,presence=presence))
    for program in ['game','editor']:
        for seed in [0,165]:
            for tail in [0,255,'pattern']:out.append(dict(name=f'converter-{program}-{seed}-{tail}',kind='converter',program=program,seed=seed,tail=tail))
    return out

def fixture_model(number,flags,label_length):
    node=bytearray(0x200);struct.pack_into('<I',node,0,0x10008000)
    label=bytes([64+number])*label_length
    struct.pack_into('<I',node,8,0x1000B008+(number-1)*0x100)
    node[0x14:0x68]=bytes((i+number)&255 for i in range(84))
    node[0x70:0xC4]=bytes((255-i-number)&255 for i in range(84))
    for off,v in [(0x124,0x1234+number),(0x126,0x2345+number),(0x128,0x3456+number),(0x12A,0x4567+number),(0x12C,0x5678+number),(0x16A,0x6789+number),(0x16C,0x789A+number)]:struct.pack_into('<H',node,off,v)
    for off,v in [(0x168,number),(0x16E,36+number),(0x16F,68+number),(0x180,number+2)]:node[off]=v
    struct.pack_into('<d',node,0x170,number+0.25);struct.pack_into('<d',node,0x178,number+0.5)
    for bit,off in FLAGS:struct.pack_into('<I',node,off,0x80000000 if flags&bit else 0)
    return node,label

def expected_record(node,label,tail,number,flags):
    raw=bytearray(266)
    raw[0x20:0x74]=node[0x70:0xC4];raw[0x74:0xC8]=node[0x14:0x68]
    struct.pack_into('<4I',raw,0xC8,0x12345678,0x89ABCDEF,int((number+0.5)*4096),flags&0x1EF)
    for dst,src in [(0xD8,0x16A),(0xDA,0x16C),(0xDC,0x126),(0xDE,0x124),(0xE0,0x128),(0xE2,0x12A),(0xE4,0x12C)]:raw[dst:dst+2]=node[src:src+2]
    raw[0xE6:0xEA]=bytes([number,21,68+number,36+number]);struct.pack_into('<I',raw,0xEA,int((number+0.25)*4096));raw[0xEE]=number+2;raw[0:len(label)]=label;raw[0xEF+max(0,len(label)-239):]=tail[max(0,len(label)-239):]
    return bytes(raw)

def build_case(c):
    mem=[];regs={};entry=0x4A9730
    if c['kind']=='flags':
        node=bytearray(0x200)
        for bit,off in FLAGS:struct.pack_into('<I',node,off,0x80000000 if c['flags']&bit else 0)
        mem=[region(0x10000000,node)];regs=dict(ECX='0x10000000');entry=0x41D6F0;want=dict(EAX=c['flags']&0x1EF)
    elif c['kind']=='converter':
        from native_spell_writer import intermediate
        raw=bytearray((i*31+7)&255 for i in range(266));raw[:32]=b'NativeSpell\0'+bytes(20)
        raw[239:]=bytes((i*73+19)&255 for i in range(27)) if c['tail']=='pattern' else bytes([c['tail']])*27
        readout=compare_code(0x10001000,0x10002000,320)+END
        mem=[region(0x10000000,raw),region(0x10001000,bytes([c['seed']])*320),region(0x10002000,intermediate(raw,c['seed'])),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))]
        regs=dict(ECX='0x10001000');entry=0x56A560 if c['program']=='game' else 0x42A0D0;want=dict(EAX=1,ECX=0)
    elif c['kind']=='model':
        mid=bytearray(320);struct.pack_into('<I',mid,0x4C,c['presence']);struct.pack_into('<I',mid,0x124,c['flags']);node=bytearray([165])*0x200;expected=bytearray(node)
        if c['presence']:
            for bit,off in FLAGS:struct.pack_into('<I',expected,off,int(bool(c['flags']&bit)))
        readout=compare_code(0x10001000,0x10002000,len(node))+END
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,expected),region(0x10003000,readout),region(0x4A2260,jump(0x4A2260,0x4A2466)),region(0x4A2479,jump(0x4A2479,0x10003000))]
        regs=dict(ESI='0x10000000',EBP='0x10001000',EDI='0x0');entry=0x4A2260;want=dict(EAX=1,ECX=0)
    else:
        stack=bytes((i*73+19)&255 for i in range(0x400)) if c['seed']=='pattern' else bytes([c['seed']])*0x400
        tail=stack[0x3DB:0x3F6];assert len(tail)==27
        count=len(c['slots']);context=bytearray(0x600);struct.pack_into('<2I',context,0x598,0x10001000,count)
        ptrs=b''.join(pack32(0 if n==0 else 0x10002000+(n-1)*0x1000) for n in c['slots']) or bytes(4)
        vt=bytearray(0x44)
        for off,va in [(0x2C,0x10009000),(0x38,0x10009020),(0x40,0x10009040)]:struct.pack_into('<I',vt,off,va)
        filevt=bytearray(0x40);struct.pack_into('<I',filevt,0x38,0x1000A000)
        date=bytes.fromhex('ea070a1a000c2238')
        header=struct.pack('<7I',160,4,count*266+56,161,28,count,1)+bytes.fromhex('112233445566778899aa')+date+stack[0x2EA:0x2EC]+struct.pack('<2I',162,count*266)
        assert len(header)==56
        models={};names={}
        for n in [1,2]:models[n],names[n]=fixture_model(n,c['flags'],c['label'])
        expected=header+b''.join(expected_record(models[n],names[n],tail,n,c['flags']) for n in c['slots'] if n)
        # File callback appends every write to one buffer; count and pointer are observable.
        capture=bytes.fromhex('56578b74240c8b4c2410ff05')+pack32(0x10011000)+bytes.fromhex('8b3d')+pack32(0x10011004)+bytes.fromhex('01cf893d')+pack32(0x10011004)+bytes.fromhex('29cff3a45f5eb801000000c20800')
        date_stub=bytes.fromhex('8b442404c700')+date[:4]+bytes.fromhex('c74004')+date[4:]+bytes.fromhex('c20400')
        readout=bytes.fromhex('0fb6d0')+compare_code(0x10012000,0x10014000,len(expected))+load_abs(3,0x10011000)+load_abs(5,0x10011004)+END
        mem=[region(0x10000000,context),region(0x10001000,ptrs),region(0x10002000,models[1]),region(0x10003000,models[2]),region(0x10008000,vt),region(0x10009000,return_const(21)),region(0x10009020,return_const(0x12345678)),region(0x10009040,return_const(0x89ABCDEF)),region(0x1000A000,capture),region(0x1000B000,pack32(len(names[1]))+pack32(0)+names[1]+b'\0'),region(0x1000B100,pack32(len(names[2]))+pack32(0)+names[2]+b'\0'),region(0x1000C000,pack32(0x1000D000)),region(0x1000D000,filevt),region(0x10011000,pack32(0)+pack32(0x10012000)),region(0x10012000,bytes(0x1000)),region(0x10014000,expected),region(0x10016000,readout),region(0x550972,bytes.fromhex('112233445566778899aa')),region(0x4AB320,date_stub),region(0x7FFEFC00,stack),region(0x7FFF0000,pack32(0x10016000)+pack32(0x1000C000))]
        regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBX=3+sum(bool(n) for n in c['slots']),EBP=0x10012000+len(expected),EDX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    program='DKII-DX.exe' if c['kind']=='converter' and c['program']=='game' else 'DK2 Editor pro.exe'
    return dict(program=program,address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase41-emulation-spell-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase41-spell-writer-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
