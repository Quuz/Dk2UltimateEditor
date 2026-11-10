"""Exact unmodified Prince records from two real maps through native DX loader."""
from pathlib import Path
import asyncio,json,os,struct,hashlib
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import dk2_formats as fmt
import emulate_creature_definition as parent
from emulate_creature_job_overlap import capture
import memory_readout_ymm as wide
ROOT=Path(__file__).resolve().parent
RAW=0x10018000;SHADOW=0x10014000;EXPECT=0x11000000
QUERIES=[0,5,9,16,0x110,0xFA,0xEF,1]
def cases():return [dict(name=n,file=str(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/maps'/f)) for n,f in [('qz2-prince','Battle for the Sun Qz2Globals.kld'),('frontend-prince','FrontEnd3DLevelGlobals.kld')]]
def build(c):
    source=Path(c['file']).read_bytes();raw=next(r for s in fmt.parse(source) if s.signature==170 for r in s.records if r[0xF4B]==2);assert len(raw)==5537
    base=next(c for c in parent.cases() if c['name']=='empty-mid');args,_=parent.build(base,0)
    regions={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions'] if int(v['address'],16)<0x11000000 and int(v['address'],16) not in [parent.DRIVER,parent.OBS]}
    regions[RAW]=raw;regions[SHADOW]=bytes(6457)
    regions[parent.GFX_CB]=bytes.fromhex('ff05')+pack32(parent.OUT+0x104)+bytes.fromhex('8b442410c7000000000033c0c21800')
    driver=bytearray()
    def emit(v):driver.extend(v)
    def call(a):emit(parent.call(parent.DRIVER+len(driver),a))
    emit(b'\xB9'+pack32(parent.MID));call(0x568820);emit(b'\x68'+pack32(RAW)+b'\xB9'+pack32(parent.MID));call(0x568E30)
    emit(b'\xBE'+pack32(parent.MID)+b'\xBF'+pack32(SHADOW)+b'\xB9'+pack32(6457)+bytes.fromhex('f3a4'))
    emit(bytes.fromhex('31c031c9'));loop=parent.DRIVER+len(driver)
    emit(bytes.fromhex('0fb691')+pack32(SHADOW)+bytes.fromhex('01d04181f9')+pack32(6457));branch=parent.DRIVER+len(driver);emit(bytes.fromhex('0f82')+pack32(loop-(branch+6)))
    assert branch-loop==16
    emit(b'\xA3'+pack32(parent.OUT+0x110));emit(b'\x6A\x00'+b'\x68'+pack32(parent.WORLD)+b'\x68'+pack32(parent.MID)+b'\xB9'+pack32(parent.DEF));call(0x492320)
    hate=bytes([raw[0xE74],raw[0xE78],raw[0xE7C],raw[0xE80]]);expected=[int(q!=0 and q in list(hate)) for q in QUERIES]
    for i,q in enumerate(QUERIES):emit(b'\x68'+pack32(q)+b'\xB9'+pack32(parent.DEF));call(0x494480);emit(b'\xA3'+pack32(parent.OUT+4*i))
    emit(b'\x68'+pack32(parent.OBS)+b'\xC3');regions[parent.DRIVER]=bytes(driver)
    fixed=[(a,regions[a]) for a in [parent.WORLD,parent.VARS,parent.SESSION,parent.GFX,parent.GV,parent.SOUND,parent.SV,0x6E4154,0x75B468,0x6E4144,0x6E4148,0]]
    fixed +=[(RAW,raw),(parent.OUT,struct.pack('<8I',*expected)),(parent.DEF+0x66E,hate),(parent.DEF+0x78E,b'\x04'),(parent.DEF+0x770,b'\x02'),(parent.DEF+2199,bytes([165])*(2560-2199))]
    observer=b'\xBD'+pack32(1)+b'\x31\xDB';at=EXPECT
    for tag,(a,b) in enumerate(fixed,1):regions[at]=b;observer+=compare_code(a,at,len(b))+bytes.fromhex('85c07505bb')+pack32(tag)+bytes.fromhex('21c5');at+=(len(b)+15)//16*16
    for a,b,n in [(parent.MID,SHADOW,6457),(parent.DEF+0x5AF,parent.OUT+0x110,4)]:observer+=compare_code(a,b,n)+bytes.fromhex('85c07505bb')+pack32(999)+bytes.fromhex('21c5')
    addresses=[parent.DEF+0x660,parent.OUT,parent.DEF+0x780,parent.MID+0x1264,parent.MID+0x2B4,parent.DEF+0x5AF,parent.DEF+0x750,parent.OUT+0x100]
    observer+=capture(addresses)+bytes.fromhex('8b15')+pack32(parent.OUT+0x100)+bytes.fromhex('8b35')+pack32(parent.OUT+0x104)+bytes.fromhex('8b3d')+pack32(parent.OUT+0x110)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';regions[parent.OBS]=observer
    stack=bytearray([165])*16384;struct.pack_into('<I',stack,8192,parent.DRIVER);regions[0x7FFEE000]=bytes(stack)
    spans=sorted((a,len(b)) for a,b in regions.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in regions.items()]))
    return args,dict(source=c['file'],source_sha256=hashlib.sha256(source).hexdigest(),record_bytes=len(raw),raw_sha256=hashlib.sha256(raw).hexdigest(),raw_hates=list(struct.unpack_from('<2I',raw,0xE74)),raw_extra_pair=list(struct.unpack_from('<2I',raw,0x14E1)),raw_alt0=list(struct.unpack_from('<IHH',raw,0xE7C)),hate_bytes_hex=hate.hex(),queries=QUERIES,membership_results=expected,capture_addresses=addresses)
def verify(c,r,e):
    assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c,r)
    b=wide.decode(r['registers']);assert b[14:18].hex()==e['hate_bytes_hex'] and list(struct.unpack_from('<8I',b,32))==e['membership_results'] and b[64+14]==4
    assert e['raw_alt0']==[16,0,0] and e['raw_extra_pair']==[0,0] and e['membership_results'][3]==1
    return b
async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in cases():
            args,e=build(c);p=ROOT/'ghidra'/f'phase86-emulation-real-job-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
            else:
                print(c['name'],'request',flush=True);response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            b=verify(c,saved['result'],e);(ROOT/'ghidra'/f'phase86-real-job-{c["name"]}.bin').write_bytes(b)
            rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified;memberships',e['membership_results'],flush=True)
    (ROOT/'phase86-real-job-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
