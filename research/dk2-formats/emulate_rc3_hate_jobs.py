"""Independent native RC3 job-overlap checks;no relocation of Pro functions.

Paired full6457-byte init/converter checks and full5449-byte writer calls.
Writer clock and file provider are explicit external fixtures. Actual RC3
converter/writer/getters remain original. No files are emitted to disk.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import emulate_creature_job_overlap as older
import emulate_creature_hate_writer as pro
import emulate_rc3_creatures as fixture
import memory_readout_ymm as wide
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'

def cases():
    result=[]
    for c in older.cases():
        if c['program']=='game':result.append(dict(c,name='converter-'+c['name'][5:],kind='converter',program='rc3'))
    for c in pro.cases():result.append(dict(c,name='writer-'+c['name'],kind='writer',program='rc3'))
    return result

def build(c):
    if c['kind']=='writer':return build_writer(c)
    RAW=0x10000000;BASERAW=0x10002000;MID=0x10004000;BASE=0x10008000;DRIVER=0x10020000;OBS=0x10024000;EXPECT=0x10028000
    a,b=older.records(dict(c,program='game'));assert len(a)==len(b)==5449
    regions={RAW:b,BASERAW:a,MID:bytes([165])*8192,BASE:bytes([165])*8192};driver=bytearray()
    def emit(v):driver.extend(v)
    def call(target):emit(b'\xE8'+pack32(target-(DRIVER+len(driver)+5)))
    emit(b'\x68'+pack32(RAW)+b'\xB9'+pack32(MID));call(0x419FF0)
    emit(b'\xB9'+pack32(BASE));call(0x4199E0);emit(b'\x68'+pack32(BASERAW)+b'\xB9'+pack32(BASE));call(0x419FF0)
    for o,v in [(0x126C,c['alt_job']),(0x1270,c['mood']|(c['mana']<<16))]:emit(b'\xC7\x05'+pack32(BASE+o)+pack32(v))
    emit(b'\x68'+pack32(OBS)+b'\xC3');regions[DRIVER]=bytes(driver)
    hate=[struct.unpack_from('<I',b,o)[0] for o in [0xE74,0xE78,0xE7C,0xE80]]
    expected=a+b+struct.pack('<4I',*hate)+struct.pack('<4I',1,1,1,1)+bytes([165])*(8192-6457);regions[EXPECT]=expected
    checks=[(MID,BASE,6457),(BASERAW,EXPECT,len(a)),(RAW,EXPECT+len(a),len(b)),(MID+0x1264,EXPECT+len(a)+len(b),16),(MID+0x2B4,EXPECT+len(a)+len(b)+16,16),(MID+6457,EXPECT+len(a)+len(b)+32,8192-6457)]
    observer=b'\xBD'+pack32(1)+b'\x31\xDB'
    for i,(p,q,n) in enumerate(checks,1):observer+=compare_code(p,q,n)+bytes.fromhex('85c07505bb')+pack32(i)+bytes.fromhex('21c5')
    addresses=[MID+0x1200+32*i for i in range(6)]+[MID+0x2B4,MID+0x1264];observer+=older.capture(addresses)+END;regions[OBS]=observer
    stack=bytearray([165])*16384;struct.pack_into('<I',stack,8192,DRIVER);regions[0x7FFEE000]=bytes(stack)
    spans=sorted((p,len(v)) for p,v in regions.items());assert all(p+n<=q for (p,n),(q,_) in zip(spans,spans[1:]))
    args=dict(program='DK2-Editor-rc3.exe',address='0x4199e0',max_steps=1000000,registers=json.dumps(dict(ECX=hex(MID),DF='0')),memory=json.dumps(dict(regions=[region(p,v) for p,v in regions.items()])),return_registers='EAX,ECX,EBX,EBP,ESP,EIP,'+wide.REGISTERS)
    return args,dict(mid_bytes=6457,raw_bytes=5449,hate_mid_dwords=hate,raw_a_sha256=hashlib.sha256(a).hexdigest(),raw_b_sha256=hashlib.sha256(b).hexdigest(),full_mid_expected='Second native RC3 baseline init/conversion with only8bytes patched to rawalternative0;all6457bytes compared',capture_addresses=addresses)

def build_writer(c):
    base=dict(name='phase87-native-writer-fixture',kind='writer',seed=c['seed'],flags=0,count=1);args,_=fixture.build_case(base)
    mem={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']};MODEL=0x10002000;CAP=0x10012000;OBS=0x10015000;EXPECT=0x10016000
    model=bytearray(mem[MODEL]);struct.pack_into('<4I',model,0x156C,*c['hates'])
    for i,v in enumerate(c['alternatives']):struct.pack_into('<IHH',model,0x157C+8*i,*v)
    mem[MODEL]=bytes(model);band=struct.pack('<2I',*c['hates'][:2])+b''.join(struct.pack('<IHH',*v) for v in c['alternatives']);assert len(band)==32
    rawbase=0x7FFF0000-0x15E8+0x94;at=rawbase+0x14E1-0x7FFED000;tail=mem[0x7FFED000][at:at+8];assert len(tail)==8
    mem[EXPECT]=band+tail+bytes(model)+pack32(1)+pack32(0)
    checks=[(CAP+0xE74,EXPECT,32),(CAP+0x14E1,EXPECT+32,8),(MODEL,EXPECT+40,len(model)),(0x10011000,EXPECT+40+len(model),4),(CAP+0x1545,EXPECT+44+len(model),4),(0x10011010,0x10014000,12)]
    observer=b'\xBD'+pack32(1)+b'\x31\xDB'
    for i,(p,q,n) in enumerate(checks,1):observer+=compare_code(p,q,n)+bytes.fromhex('85c07505bb')+pack32(i)+bytes.fromhex('21c5')
    observer+=wide.code(CAP+0xE74)+bytes.fromhex('c5fe6f0d')+pack32(CAP+0x14E1)+END;mem[OBS]=observer
    spans=sorted((p,len(v)) for p,v in mem.items());assert all(p+n<=q for (p,n),(q,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=[region(p,v) for p,v in mem.items()]));args['return_registers']='EAX,ECX,EBX,EBP,ESP,EIP,'+wide.REGISTERS
    return args,dict(raw_bytes=5449,source_model_bytes=6288,expected_band_hex=band.hex(),expected_extra_hex=tail.hex(),header12_expected_hex=struct.pack('<3I',170,4,56+5449).hex(),date_adapter=0x4B0B30,
      scope='Full original RC3writer/getters;rawE74..E93/raw14E1..E8/header12/lastflags1545/wholemodel/countchecked;otherrawfieldsopaque')

def verify(c,r,e):
    assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c,r)
    b=wide.decode(r['registers'])
    if c['kind']=='writer':assert b[:32].hex()==e['expected_band_hex'] and b[32:40].hex()==e['expected_extra_hex']
    else:assert list(struct.unpack_from('<4I',b,0x64))==e['hate_mid_dwords'] and list(struct.unpack_from('<4I',b,224))==e['hate_mid_dwords'] and b[192:208]==struct.pack('<4I',1,1,1,1)
    return b
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,e=build(c);p=ROOT/'ghidra'/f'phase87-emulation-rc3-job-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
            else:
                print(c['name'],'request',flush=True);response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            b=verify(c,saved['result'],e);(ROOT/'ghidra'/f'phase87-rc3-job-{c["name"]}.bin').write_bytes(b)
            rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase87-rc3-job-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
