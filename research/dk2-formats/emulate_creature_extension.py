"""Full Creatures writer and paired complete converters, exact native code.

Writer uses prebuilt1890-byte models, file append capture and fixed date only.
Original getters, flags, resource copies, ftol, string copy and stack probe run.
Writer comparisons target final92 bytes; no claim all5537 fields are verified.
Converters execute twice and compare their complete native intermediate buffers.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def pattern(seed,n):return bytes((i*73+19)&255 for i in range(n)) if seed=='pattern' else bytes([seed])*n

def cases():
    rows=[]
    for seed in [0,165,255,'pattern']:
        for flags in [0,1,15]:rows.append(dict(name=f'writer-{seed}-{flags}',kind='writer',seed=seed,flags=flags,count=1,label_length=5,animation_update=0))
    rows.append(dict(name='writer-consecutive',kind='writer',seed='pattern',flags=2,count=2,label_length=32,animation_update=1))
    for context in [0x54F550,0x10234560,0x20ABCDF0]:rows.append(dict(name=f'writer-context-{context}',kind='writer',seed=165,flags=1,count=1,label_length=5,animation_update=0,context_address=context))
    for size in [5450,5530]:rows.append(dict(name=f'writer-overlong-name-{size}',kind='writer',seed=165,flags=15,count=1,label_length=size,animation_update=0))
    for program in ['game','editor']:
        for seed in [0,165]:
            for which in ['opaque80','lastwords','other_flag_slot']:
                rows.append(dict(name=f'converter-{program}-{seed}-{which}',kind='converter',program=program,seed=seed,which=which))
        for size in [5449,5537]:
            for count in [1,2]:rows.append(dict(name=f'parser-{program}-{size}-{count}',kind='parser',program=program,size=size,count=count))
    return rows

def build_case(c):
    mem=[];regs={};program='DK2 Editor pro.exe'
    if c['kind']=='writer':
        entry=0x4A7F00;frame=0x7FFF0000-0x1640;stack=pattern(c['seed'],0x3000);rawbase=frame+0x94;context_address=c.get('context_address',0x10000000)
        want=bytearray(stack[rawbase-0x7FFED000+0x1545:rawbase-0x7FFED000+0x15A1]);assert len(want)==92
        # Original _chkstk PUSH ECX and CALL return slot overlap the final4
        # payload bytes: caller ECX bytes1..3, then return-address low byte10.
        want[88:92]=pack32(context_address)[1:]+b'\x10'
        struct.pack_into('<I',want,84,c['flags']);label=b'A'*c['label_length']
        if len(label)>0x1545:want[:min(len(label)-0x1545,len(want))]=label[0x1545:0x15A1]
        context=bytearray(0x500);struct.pack_into('<2I',context,0x480,0x10001000,c['count']);model=bytearray(0x1890)
        for off in [8,0x154,0x15A8]:struct.pack_into('<I',model,off,0x1000A008 if off==8 else 0x1000C008)
        for bit,off in [(1,0x1880),(2,0x1884),(4,0x1888),(8,0x1828)]:struct.pack_into('<I',model,off,int(bool(c['flags']&bit)))
        struct.pack_into('<I',model,0x1838,c['animation_update']);model[0x168]=21
        vt=bytearray(0x40);struct.pack_into('<I',vt,0x38,0x1000D000)
        # Capture only record writes. Header is still produced by the full writer.
        body=bytes.fromhex('ff05')+pack32(0x10011000)+bytes.fromhex('56578b74240cbf')+pack32(0x10012000)+b'\xB9'+pack32(5537)+bytes.fromhex('f3a45f5eb801000000c20800')
        capture=bytes.fromhex('817c2408a115000075')+bytes([len(body)])+body+bytes.fromhex('b801000000c20800')
        date=bytes.fromhex('ea070a1a000c2238');date_stub=bytes.fromhex('8b442404c700')+date[:4]+bytes.fromhex('c74004')+date[4:]+bytes.fromhex('c20400')
        readout=bytes.fromhex('0fb6d0')+compare_code(0x10012000+0x1545,0x10014000,92)+load_abs(3,0x10011000)+load_abs(5,0x10012000+0x1599)+load_abs(6,0x10012000+0x1545)+END
        expected=dict(EAX=1,ECX=0,EDX=1,EBX=c['count'],EBP=int.from_bytes(want[84:88],'little'),ESI=int.from_bytes(want[:4],'little'))
        mem=[region(context_address,context),region(0x10001000,b''.join(pack32(0x10002000) for _ in range(c['count']))),region(0x10002000,model),region(0x10006000,pack32(0x10007000)),region(0x10007000,vt),region(0x1000A000,pack32(len(label))+pack32(0)+label+b'\0'),region(0x1000C000,bytes(9)),region(0x1000D000,capture),region(0x10011000,bytes(4)),region(0x10012000,bytes(5537)),region(0x10014000,want),region(0x10015000,readout),region(0x4AB320,date_stub),region(0x550940,bytes.fromhex('112233445566778899aa')),region(0x7FFED000,stack),region(0x7FFF0000,pack32(0x10015000)+pack32(0x10006000))];regs=dict(ECX=hex(context_address))
    elif c['kind']=='parser':
        import dk2_formats as fmt
        from inventory import DATA
        game=c['program']=='game';program='DKII-DX.exe' if game else program;entry=0x5558D0 if game else 0x4ACDA8;adapter=0x10022000;parser=0x56A250 if game else 0x429D90;init=0x568820 if game else 0x4281B0;conv=0x568E30 if game else 0x4287C0;length=6457 if game else 6561;mid=0x748409 if game else 0x540AE1
        raw=bytearray(5537);raw[0xF4B]=21;struct.pack_into('<I',raw,0x1545,1)
        if c['size']==5537:raw[0x1549:0x1599]=pattern('pattern',80);struct.pack_into('<I',raw,0x1599,2);raw[0x159D:]=bytes.fromhex('f5540010')
        header=bytearray(next(s for s in fmt.parse((DATA/'Creatures.kwd').read_bytes()) if s.signature==170).header);struct.pack_into('<I',header,20,c['count']);struct.pack_into('<I',header,52,c['count']*c['size']);struct.pack_into('<I',header,8,56+c['count']*c['size']);source=header[12:]+raw[:c['size']]*c['count'];dispatch=bytearray(0x68);struct.pack_into('<I',dispatch,0x60,0x10020000)
        callback=bytes.fromhex('5657')+compare_code(mid,0x10008000,length)+bytes.fromhex('2105')+pack32(0x10018004)+bytes.fromhex('ff05')+pack32(0x10018000)+bytes.fromhex('5f5eb801000000c3')
        io=bytes.fromhex('56578b35')+pack32(0x10018008)+b'\x81\xC6'+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418010d')+pack32(0x10018008)+bytes.fromhex('f3a4')
        # Capture the raw read's exact88-byte overwrite of adjacent game mid.
        if game and c['size']==5537:
            audit=compare_code(mid,0x10021000,88)+bytes.fromhex('2105')+pack32(0x1001800C)
            io+=bytes.fromhex('817c2418a115000075')+bytes([len(audit)])+audit
        io+=bytes.fromhex('8b44240cc700000000008b54241c8b4c2418890a5f5ec3')
        code=b''
        def call(target):
            nonlocal code
            code+=b'\xE8'+struct.pack('<i',target-(adapter+len(code)+5))
        code+=b'\xB9'+pack32(0x10008000);call(init);code+=b'\x68'+pack32(0x10004000)+b'\xB9'+pack32(0x10008000);call(conv)
        code+=b'\x68'+pack32(0x10019000)+b'\x68'+pack32(0x1001A000);call(parser);code+=bytes.fromhex('83c40889c2')+load_abs(0,0x10018004)+load_abs(3,0x10018000)+load_abs(1,0x10018008)+load_abs(5,0x1001800C)+END
        mem=[region(0x10000000,source),region(0x10004000,raw),region(0x10008000,bytes(length)),region(0x10018000,struct.pack('<4I',0,1,0,1)),region(0x10019000,dispatch),region(0x1001A000,pack32(1)),region(0x10020000,callback),region(0x10021000,raw[5449:5537]),region(adapter,code),region(0x5B3EB0 if game else 0x4B7F10,io),region(entry,jump(entry,adapter))];expected=dict(EAX=1,EBX=c['count'],ECX=len(source),EDX=1,EBP=1)
    else:
        program='DKII-DX.exe' if c['program']=='game' else program;entry=0x5558D0 if c['program']=='game' else 0x4ACDA8;adapter=0x10020000;init=0x568820 if c['program']=='game' else 0x4281B0;conv=0x568E30 if c['program']=='game' else 0x4287C0;length=6457 if c['program']=='game' else 6561
        raw=bytearray(5537);raw[0xF4B]=21;struct.pack_into('<I',raw,0x1545,1);struct.pack_into('<I',raw,0x1599,2);other=bytearray(raw)
        if c['which']=='opaque80':other[0x1549:0x1599]=pattern('pattern',80)
        elif c['which']=='lastwords':other[0x159D:0x15A1]=b'\xFF'*4
        else:struct.pack_into('<I',other,0x1599 if c['program']=='game' else 0x1545,0xFFFFFFFF)
        # Execute initializer and converter for A and B with identical seeds.
        code=b''
        def call(target):
            nonlocal code
            code+=b'\xE8'+struct.pack('<i',target-(adapter+len(code)+5))
        for source,dest in [(0x10000000,0x10004000),(0x10002000,0x10008000)]:
            code+=b'\xB9'+pack32(dest);call(init);code+=b'\x68'+pack32(source)+b'\xB9'+pack32(dest);call(conv)
        code+=compare_code(0x10004000,0x10008000,length)+load_abs(3,0x10004000+(0x1935 if c['program']=='game' else 0x1999))+END
        expected=dict(EAX=1,ECX=0,EBX=1 if c['program']=='game' else 2)
        mem=[region(0x10000000,raw),region(0x10002000,other),region(0x10004000,pattern(c['seed'],length)),region(0x10008000,pattern(c['seed'],length)),region(adapter,code),region(entry,jump(entry,adapter))]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=200000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*expected,'ESP','EIP'])),expected

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase43-emulation-extension-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase43-creature-extension-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
