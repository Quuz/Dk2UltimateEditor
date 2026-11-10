"""RC3 fullCreature writer,parser/converter andextraFlags model codec.

Writer asserts finalrawDWORD,countandheader sizes,notall5449 fieldvalues.
Parserchecksfull6457 midagainstisolatedrealconverter. NativeI/O/datefixtures;
noGUI,gameprocess ororiginalfilechanges.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_hero_parties import compare_code
from emulate_creature_extension import pattern
from native_keeper_pipeline import intermediate as keeper_mid
from native_spell_alternatives import full_intermediate as spell_mid
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    rows=[]
    for seed in [0,165,255,'pattern']:
        for flags in [0,1,2,4,8,15]:rows.append(dict(name=f'writer-{seed}-{flags}',kind='writer',seed=seed,flags=flags,count=1))
    rows.append(dict(name='writer-consecutive',kind='writer',seed='pattern',flags=1,count=2))
    for seed in [0,165]:
        for flags in [0,1,2,4,8,15,0xFFFFFFFF]:rows.append(dict(name=f'codec-{seed}-{flags}',kind='codec',seed=seed,flags=flags))
    for size in [5449,5537]:
        for count in [1,2]:rows.append(dict(name=f'parser-{size}-{count}',kind='parser',size=size,count=count))
    for seed in [0,165]:
        for which in ['opaque80','lastwords','pro_flags']:rows.append(dict(name=f'converter-{seed}-{which}',kind='converter',seed=seed,which=which))
    for family in ['KeeperSpells','CreatureSpells']:
        for seed in [0,165,255]:
            for fill in [0,255,'pattern']:rows.append(dict(name=f'catalogue-{family}-{seed}-{fill}',kind='catalogue',family=family,seed=seed,fill=fill))
    for flags,proflags in [(0,1),(1,0),(2,1),(4,1),(8,1),(15,0),(0xFFFFFFFF,0)]:rows.append(dict(name=f'chain-{flags}-{proflags}',kind='chain',flags=flags,pro_flags=proflags))
    for source in ['standalone','globals']:rows.append(dict(name=f'chain-king-{source}',kind='chain',source=source))
    rows+=comparison_cases()
    return rows

def comparison_cases():
    rows=[]
    for r in json.loads((ROOT/'phase50-converter-match-candidates.json').read_text()):
        if len(r['rc3_candidates'])!=1:continue
        for seed in [0,165,255]:
            for fill in [0,255,'pattern']:rows.append(dict(name=f'compare-{r["family"]}-{seed}-{fill}',kind='comparison',family=r['family'],seed=seed,fill=fill))
    return rows

def comparison_case(c):
    from compare_rc3_converters import PRO,RC3
    from build_effect_semantics import pe_read
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    from capstone.x86 import X86_OP_IMM
    r=next(r for r in json.loads((ROOT/'phase50-converter-match-candidates.json').read_text()) if r['family']==c['family']);pro=r['pro_address'];rc=r['rc3_candidates'][0];n=r['bytes'];body=bytearray(pe_read(PRO.read_bytes(),pro,n));ref=0x10030000
    # Relocateonlytheoriginal Pro CALL operands totheverifiedsameRC3helper.
    # Internalbranches retainrelative encodings;noabsolute/tableoperands exist.
    for m in r['masked_relocations']:
        assert m['kind']=='branch' and m['bytes']==4;off=m['offset'];ins=int(m['instruction'],16);target=rc+(ins-pro)+5+struct.unpack('<i',pe_read(RC3.read_bytes(),rc+off,4))[0];struct.pack_into('<i',body,off,target-(ref+(ins-pro)+5))
    raw=pattern(c['fill'],5600);size=9000;buf=bytes([c['seed']])*size;adapter=0x10034000;code=b''
    for dest,target in [(0x10004000,rc),(0x10008000,ref)]:
        code+=b'\x68'+pack32(0x10000000)+b'\xB9'+pack32(dest);at=adapter+len(code);code+=b'\xE8'+struct.pack('<i',target-(at+5))
    code+=compare_code(0x10004000,0x10008000,size)+END
    mem=[region(0x10000000,raw),region(0x10004000,buf),region(0x10008000,buf),region(ref,body),region(adapter,code),region(0x4B9850,jump(0x4B9850,adapter))]
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program='DK2-Editor-rc3.exe',address='0x4b9850',max_steps=200000,registers='{}',memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,ESP,EIP'),dict(EAX=1,ECX=0)

def build_case(c):
    if c['kind']=='comparison':return comparison_case(c)
    mem=[];regs={};program='DK2-Editor-rc3.exe';entry=0x4B9850
    if c['kind']=='chain':
        base=dict(name='chain-writer-fixture',kind='writer',seed=165,flags=0,count=1);args,want=build_case(base);mem=json.loads(args['memory'])['regions'];entry=0x488050
        if 'source' in c:
            import dk2_formats as fmt
            from inventory import DATA
            filename='Creatures.kwd' if c['source']=='standalone' else 'AutoSaveGlobals.kld';raw=bytearray(next(r for sec in fmt.parse((DATA/filename).read_bytes()) if sec.signature==170 for r in sec.records if r[0xF4B]==21));flags=struct.unpack_from('<I',raw,0x1545)[0]
        else:
            raw=bytearray(5537);raw[0xF4B]=21;struct.pack_into('<I',raw,0x1545,c['flags']);struct.pack_into('<I',raw,0x1599,c['pro_flags']);flags=c['flags']
        adapter=0x10030000;code=b''
        def call(target):
            nonlocal code
            code+=b'\xE8'+struct.pack('<i',target-(adapter+len(code)+5))
        code+=b'\xB9'+pack32(0x10022000);call(0x4199E0);code+=b'\x68'+pack32(0x10020000)+b'\xB9'+pack32(0x10022000);call(0x419FF0)
        code+=b'\xBB'+pack32(0x10022000)+b'\xBD'+pack32(0x10002000)+bytes.fromhex('31ff');code+=jump(adapter+len(code),0x4A68DF)
        after=b'\xB9'+pack32(0x10000000)+b'\x68'+pack32(0x10006000)+b'\x68'+pack32(0x10015000);after+=jump(0x10031000+len(after),0x4AD720)
        mem.extend([region(entry,jump(entry,adapter)),region(adapter,code),region(0x10031000,after),region(0x10020000,raw),region(0x10022000,bytes(6457)),region(0x4A68F5,jump(0x4A68F5,0x10031000))]);want['EBP']=flags&15
    elif c['kind']=='writer':
        entry=0x4AD720;frame=0x7FFF0000-0x15E8;stack=pattern(c['seed'],0x3000);context=bytearray(0x500);struct.pack_into('<2I',context,0x47C,0x10001000,c['count']);model=bytearray(0x1890)
        for off in [8,0x154,0x15A8]:struct.pack_into('<I',model,off,0x1000A008 if off==8 else 0x1000C008)
        for bit,off in [(1,0x1880),(2,0x1884),(4,0x1888),(8,0x1828)]:struct.pack_into('<I',model,off,int(bool(c['flags']&bit)))
        model[0x168]=21;vt=bytearray(0x40);struct.pack_into('<I',vt,0x38,0x1000D000)
        # Captureheader12/count/version/size/stride andwholelastemitted record.
        body=bytes.fromhex('ff05')+pack32(0x10011000)+bytes.fromhex('56578b74240cbf')+pack32(0x10012000)+b'\xB9'+pack32(5449)+bytes.fromhex('f3a45f5eb801000000c20800')
        capture=bytes.fromhex('817c24084915000075')+bytes([len(body)])+body
        # Observefirst12-byteheader and8-byte dataheader sizes,thenreturn.
        # Conditional skip uses actual handler length.
        header_body=bytes.fromhex('56578b74240cbf')+pack32(0x10011010)+b'\xB9'+pack32(3)+bytes.fromhex('f3a55f5e')
        capture=bytes.fromhex('817c24084915000075')+bytes([len(body)])+body+bytes.fromhex('837c24080c75')+bytes([len(header_body)])+header_body+bytes.fromhex('b801000000c20800')
        date=bytes.fromhex('ea070a1a00000c2238');date_stub=bytes.fromhex('8b442404c700')+date[:4]+bytes.fromhex('c74004')+date[4:8]+bytes.fromhex('c64008')+date[8:]+bytes.fromhex('c20400')
        expected_header=struct.pack('<3I',170,4,56+5449*c['count']);readout=bytes.fromhex('0fb6d0')+compare_code(0x10011010,0x10014000,12)+load_abs(3,0x10011000)+load_abs(5,0x10012000+0x1545)+END
        mem=[region(0x10000000,context),region(0x10001000,b''.join(pack32(0x10002000) for _ in range(c['count']))),region(0x10002000,model),region(0x10006000,pack32(0x10007000)),region(0x10007000,vt),region(0x1000A000,pack32(5)+pack32(0)+b'King\0\0'),region(0x1000C000,bytes(9)),region(0x1000D000,capture),region(0x10011000,bytes(0x30)),region(0x10012000,bytes(5449)),region(0x10014000,expected_header),region(0x10015000,readout),region(0x4B0B30,date_stub),region(0x5680A8,bytes.fromhex('112233445566778899aa')),region(0x7FFED000,stack),region(0x7FFF0000,pack32(0x10015000)+pack32(0x10006000))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EDX=1,EBX=c['count'],EBP=c['flags'])
    elif c['kind']=='codec':
        entry=0x4292B0;model=bytearray([c['seed']])*0x1890;expected=bytearray(model)
        for bit,off in [(1,0x1880),(2,0x1884),(4,0x1888),(8,0x1828)]:struct.pack_into('<I',expected,off,int(bool(c['flags']&bit)))
        adapter=b'\xB9'+pack32(0x10000000)+b'\x68'+pack32(0x10003000)+jump(0x10004000+10,0x429270);readout=bytes.fromhex('89c2')+compare_code(0x10000000,0x10010000,len(model))+END
        mem=[region(0x10000000,model),region(0x10010000,expected),region(0x10003000,readout),region(0x10004000,adapter),region(0x7FFF0000,pack32(0x10004000)+pack32(c['flags']))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EDX=c['flags']&15)
    elif c['kind']=='catalogue':
        n=406 if c['family']=='KeeperSpells' else 266;midlen=526 if n==406 else 346;raw=pattern(c['fill'],n);expected=keeper_mid(raw,c['seed']) if n==406 else spell_mid(raw,c['seed']);assert len(expected)==midlen
        entry=0x41CF20 if n==406 else 0x41B920;readout=compare_code(0x10001000,0x10002000,midlen)+END
        mem=[region(0x10000000,raw),region(0x10001000,bytes([c['seed']])*midlen),region(0x10002000,expected),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    elif c['kind']=='parser':
        import dk2_formats as fmt
        from inventory import DATA
        raw=bytearray(5537);raw[0xF4B]=21;struct.pack_into('<I',raw,0x1545,1);raw[0x1549:]=pattern('pattern',88)
        header=bytearray(next(s for s in fmt.parse((DATA/'Creatures.kwd').read_bytes()) if s.signature==170).header);struct.pack_into('<I',header,20,c['count']);struct.pack_into('<I',header,52,c['count']*c['size']);struct.pack_into('<I',header,8,56+c['count']*c['size']);source=header[12:]+raw[:c['size']]*c['count'];dispatch=bytearray(0x68);struct.pack_into('<I',dispatch,0x60,0x10020000);mid=0x5581ED;length=6457;adapter=0x10022000
        callback=bytes.fromhex('5657')+compare_code(mid,0x10008000,length)+bytes.fromhex('2105')+pack32(0x10018004)+bytes.fromhex('ff05')+pack32(0x10018000)+bytes.fromhex('5f5eb801000000c3')
        io=bytes.fromhex('56578b35')+pack32(0x10018008)+b'\x81\xC6'+pack32(0x10000000)+bytes.fromhex('8b7c24148b4c2418010d')+pack32(0x10018008)+bytes.fromhex('f3a4')
        if c['size']==5537:
            audit=compare_code(mid,0x10021000,88)+bytes.fromhex('2105')+pack32(0x1001800C);io+=bytes.fromhex('817c2418a115000075')+bytes([len(audit)])+audit
        io+=bytes.fromhex('8b44240cc700000000008b54241c8b4c2418890a5f5ec3')
        code=b''
        def call(target):
            nonlocal code
            code+=b'\xE8'+struct.pack('<i',target-(adapter+len(code)+5))
        code+=b'\xB9'+pack32(0x10008000);call(0x4199E0);code+=b'\x68'+pack32(0x10004000)+b'\xB9'+pack32(0x10008000);call(0x419FF0)
        code+=b'\x68'+pack32(0x10019000)+b'\x68'+pack32(0x1001A000);call(0x41B5E0);code+=bytes.fromhex('83c40889c2')+load_abs(0,0x10018004)+load_abs(3,0x10018000)+load_abs(1,0x10018008)+load_abs(5,0x1001800C)+END
        mem=[region(0x10000000,source),region(0x10004000,raw),region(0x10008000,bytes(length)),region(0x10018000,struct.pack('<4I',0,1,0,1)),region(0x10019000,dispatch),region(0x1001A000,pack32(1)),region(0x10020000,callback),region(0x10021000,raw[5449:5537]),region(adapter,code),region(0x4C53A0,io),region(entry,jump(entry,adapter))];want=dict(EAX=1,EBX=c['count'],ECX=len(source),EDX=1,EBP=1)
    else:
        raw=bytearray(5537);raw[0xF4B]=21;struct.pack_into('<I',raw,0x1545,1);struct.pack_into('<I',raw,0x1599,2);other=bytearray(raw)
        if c['which']=='opaque80':other[5449:5529]=pattern('pattern',80)
        elif c['which']=='lastwords':other[5533:]=b'\xFF'*4
        else:struct.pack_into('<I',other,5529,0xFFFFFFFF)
        adapter=0x10020000;code=b''
        def call(target):
            nonlocal code
            code+=b'\xE8'+struct.pack('<i',target-(adapter+len(code)+5))
        for src,dest in [(0x10000000,0x10004000),(0x10002000,0x10008000)]:
            code+=b'\xB9'+pack32(dest);call(0x4199E0);code+=b'\x68'+pack32(src)+b'\xB9'+pack32(dest);call(0x419FF0)
        code+=compare_code(0x10004000,0x10008000,6457)+load_abs(3,0x10004000+0x1935)+END
        mem=[region(0x10000000,raw),region(0x10002000,other),region(0x10004000,pattern(c['seed'],6457)),region(0x10008000,pattern(c['seed'],6457)),region(adapter,code),region(entry,jump(entry,adapter))];want=dict(EAX=1,ECX=0,EBX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=200000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase50-emulation-rc3-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase50-rc3-creature-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
