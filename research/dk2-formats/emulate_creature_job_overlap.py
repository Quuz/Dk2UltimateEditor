"""Native paired game/editor converters and actual game hated-job membership.

No original code or native vtable replacements. Full6457/6561 intermediate
compared with a separately converted baseline plus eight explicit raw alt0
bytes. Game also runs the original full definition loader and membership.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import emulate_raw_creature_definition as old
import memory_readout_ymm as wide
ROOT=Path(__file__).resolve().parent
MID=old.parent.MID;RAW=old.RAW;DEF=old.parent.DEF;DRIVER=old.parent.DRIVER;OBS=old.parent.OBS;OUT=old.parent.OUT
BASEMID=0x10080000;BASERAW=0x10084000;EXPECT=0x12000000
QUERIES=[0,5,9,0xFA,0x105,0xFFFFFFFF,0xEF,1]

def cases():
    variants=[('extras-zero',0,0,0,0,0),('extras-distinct',0x11223344,0x55667788,0,0,0),
      ('extras-highbits',0xFFFFFFFF,0x80000000,0,0,0),('alt-job5',0x11223344,0x55667788,0x12345605,0,0),
      ('alt-mood9',0x11223344,0x55667788,0,0x7F09,0x8011),('alt-job105',0x11223344,0x55667788,0x12340105,0,0),
      ('combined',0xA1B2C3D4,0xE5F60718,0x89ABCDEF,0x8009,0xFFFF)]
    return [dict(name=p+'-'+n,program=p,extra=[a,b],alt_job=j,mood=m,mana=k) for p in ['game','editor'] for n,a,b,j,m,k in variants]

def records(c):
    a=old.raw_record(old.cases()[0]);b=bytearray(a)
    for o,v in zip([0x14E1,0x14E5,0xE7C],c['extra']+[c['alt_job']]):struct.pack_into('<I',b,o,v)
    struct.pack_into('<HH',b,0xE80,c['mood'],c['mana'])
    if c['program']=='editor':a+=bytes(88);b+=bytes(88)
    assert len(a)==len(b) and len(a) in [5449,5537]
    return a,bytes(b)

def capture(addresses):
    assert len(addresses)==8
    return b''.join(bytes.fromhex('c5fe6f')+bytes([5+8*i])+pack32(a) for i,a in enumerate(addresses))

def build(c):
    a,b=records(c);game=c['program']=='game';length=6457 if game else 6561;init=0x568820 if game else 0x4281B0;conv=0x568E30 if game else 0x4287C0
    hate=[struct.unpack_from('<I',b,o)[0] for o in [0xE74,0xE78,0xE7C,0xE80]];off=0x1264 if game else 0x1274
    membership=[int(q!=0 and q in [v&255 for v in hate]) for q in QUERIES]
    if game:
        args,e=old.build(old.cases()[0],0);mem=json.loads(args['memory'])['regions'];regions={int(v['address'],16):bytes.fromhex(v['hex']) for v in mem}
        assert regions[RAW]==a;regions[RAW]=b
        raw_expected=[p for p,v in regions.items() if 0x11000000<=p<EXPECT and v==a];assert len(raw_expected)==1;regions[raw_expected[0]]=b
        driver=bytearray(regions[DRIVER]);assert driver[-6:]==b'\x68'+pack32(OBS)+b'\xC3';del driver[-6:]
        prior=regions[OBS];marker=wide.code(DEF);assert prior.count(marker)==1;observer=prior[:prior.index(marker)]
        root=0x491DC0
    else:
        regions={MID:bytes([165])*8192,RAW:b};driver=bytearray();observer=b'\xBD'+pack32(1)+b'\x31\xDB';root=init
    def emit(v):driver.extend(v)
    def call(target):emit(old.parent.call(DRIVER+len(driver),target))
    if not game:emit(b'\x68'+pack32(RAW)+b'\xB9'+pack32(MID));call(conv)
    # Native baseline conversion, then independent expected alt0 patch.
    regions[BASEMID]=bytes([165])*8192;regions[BASERAW]=a
    emit(b'\xB9'+pack32(BASEMID));call(init);emit(b'\x68'+pack32(BASERAW)+b'\xB9'+pack32(BASEMID));call(conv)
    for o,v in [(off+8,c['alt_job']),(off+12,c['mood']|(c['mana']<<16))]:emit(b'\xC7\x05'+pack32(BASEMID+o)+pack32(v))
    if game:
        for i,q in enumerate(QUERIES):
            emit(b'\x68'+pack32(q)+b'\xB9'+pack32(DEF));call(0x494480);emit(b'\xA3'+pack32(OUT+0x120+4*i))
    emit(b'\x68'+pack32(OBS)+b'\xC3');regions[DRIVER]=bytes(driver)
    def check(actual,expected,n,tag):
        nonlocal observer
        observer+=compare_code(actual,expected,n)+bytes.fromhex('85c07505bb')+pack32(tag)+bytes.fromhex('21c5')
    check(MID,BASEMID,length,1001)
    regions[EXPECT]=a;check(BASERAW,EXPECT,len(a),1002)
    regions[EXPECT+0x2000]=b;check(RAW,EXPECT+0x2000,len(b),1003)
    regions[EXPECT+0x4000]=struct.pack('<4I',*hate);check(MID+off,EXPECT+0x4000,16,1004)
    if game:
        regions[EXPECT+0x4100]=bytes(v&255 for v in hate);check(DEF+0x66E,EXPECT+0x4100,4,1005)
        regions[EXPECT+0x4200]=struct.pack('<8I',*membership);check(OUT+0x120,EXPECT+0x4200,32,1006)
        regions[EXPECT+0x4300]=b'\x04';check(DEF+0x78E,EXPECT+0x4300,1,1007)
        regions[EXPECT+0x4310]=struct.pack('<2I',2,0);check(OUT+0x100,EXPECT+0x4310,8,1010)
        if c['name'].startswith('game-extras-'):
            regions[EXPECT+0x5000]=(ROOT/'ghidra/phase85-raw-creature-definition-raw-imp.bin').read_bytes();check(DEF,EXPECT+0x5000,2199,1008)
        addresses=[MID+0x1200+32*i for i in range(6)]+[DEF+0x660,OUT+0x120]
    else:
        regions[EXPECT+0x4400]=bytes([165])*(8192-length);check(MID+length,EXPECT+0x4400,8192-length,1009)
        addresses=[MID+0x1200+32*i for i in range(6)]+[MID+0x2C4,MID+0x1274]
    observer+=capture(addresses)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';regions[OBS]=observer
    stack=bytearray([165])*16384;struct.pack_into('<I',stack,8192,DRIVER);regions[0x7FFEE000]=bytes(stack)
    spans=sorted((p,len(v)) for p,v in regions.items());assert all(p+n<=q for (p,n),(q,_) in zip(spans,spans[1:])),c['name']
    args=dict(program='DKII-DX.exe' if game else 'DK2 Editor pro.exe',address=hex(root),max_steps=1000000,registers=json.dumps(dict(ECX=hex(DEF if game else MID),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),
      memory=json.dumps(dict(regions=[region(p,v) for p,v in regions.items()])),return_registers='EAX,EBX,ECX,EDX,ESI,EDI,ESP,EBP,EIP,'+wide.REGISTERS)
    return args,dict(mid_bytes=length,hate_mid_offset=off,hate_mid_dwords=hate,game_hate_bytes_hex=bytes(v&255 for v in hate).hex() if game else None,membership_queries=QUERIES if game else [],membership_results=membership if game else [],
      full_mid_expected='Native baseline conversion with only8bytes patched from new rawE7C..E83',full_definition_compared=game and c['name'].startswith('game-extras-'),source=str(old.SOURCE),source_sha256=hashlib.sha256(old.SOURCE_BYTES).hexdigest(),baseline_raw_sha256=hashlib.sha256(a).hexdigest(),modified_raw_sha256=hashlib.sha256(b).hexdigest(),capture_addresses=addresses)

def verify(c,r,e):
    assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c,r)
    b=wide.decode(r['registers']);at=e['hate_mid_offset']-0x1200;assert list(struct.unpack_from('<4I',b,at))==e['hate_mid_dwords']
    if c['program']=='game':assert b[192+14:192+18].hex()==e['game_hate_bytes_hex'] and list(struct.unpack_from('<8I',b,224))==e['membership_results']
    else:assert b[192:208]==struct.pack('<4I',1,1,1,1) and list(struct.unpack_from('<4I',b,224))==e['hate_mid_dwords']
    return b

async def main():
    chosen=cases()
    if len(sys.argv)>1:chosen=[c for c in chosen if c['name']==sys.argv[1] or c['program']==sys.argv[1]];assert chosen
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in chosen:
            args,e=build(c);p=ROOT/'ghidra'/f'phase86-emulation-job-overlap-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
            else:
                print(c['name'],'request',flush=True);response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            b=verify(c,saved['result'],e);(ROOT/'ghidra'/f'phase86-job-overlap-{c["name"]}.bin').write_bytes(b)
            rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(chosen)==len(cases()):(ROOT/'phase86-job-overlap-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
