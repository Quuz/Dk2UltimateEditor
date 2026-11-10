"""Full AP getters in DX/old plus DX PRNG/map-getter stack-spill compositions.

All native code remains intact. Synthetic composition code starts only after
the root AP getter returns. Stack slots and call arguments are explicit data.
These are not full patrol/state/world simulations or raw-file loading tests.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_ap_lookup import lookup_tag
from native_wander_state import rand
ROOT=Path(__file__).resolve().parent
PROFILES={'DX':dict(program='DKII-DX.exe',getter=0x50FAE0,table=0x6EE540),'old':dict(program='DKII-local13.exe',getter=0x504140,table=0x6D95C8)}
WORLD=0x10000000;NODES=0x10010000;OUT=0x10050000;POINT=0x7FFF0030;SPILL=0x7FFF0028;OBS=0x10080000;STACK=0x7FFF0000

def cases():
    ordinary=[(5,1,9),(9,7,3),(3,2,0)]
    variants=[
     ('empty',[],0,1,0),('first',ordinary,5,1,0),('middle',ordinary,5,7,0),('last',ordinary,5,2,0),
     ('missing',ordinary,5,3,0),('ownID0',[(5,0,0)],5,0,0),('high-key',ordinary,5,0x12340007,0),
     ('id255',[(5,255,0)],5,255,0),('unsorted',[(5,7,9),(9,1,3),(3,2,0)],5,2,0),
     ('duplicate-first',[(5,7,9),(9,7,0)],5,7,0),('flags0',ordinary,5,7,0),('flags0C',ordinary,5,7,12),
     ('wrong-kind-owner',ordinary,5,7,0),('tag0-mapped',[(0,0,0)],0,0,0),
     ('next0-mapped',[(5,7,0),(0,0,0)],5,0,0),('head65535',[(65535,7,0)],65535,7,0),
    ]
    out=[dict(profile=profile,name=name,rows=rows,head=head,key=key,flags=flags,contract=False) for profile in PROFILES for name,rows,head,key,flags in variants]
    out.extend(dict(profile='DX',name=f'spill-flags{flags:02X}',rows=ordinary,head=5,key=7,flags=flags,contract=True) for flags in [0,4,8,12])
    return out

def build(c):
    p=PROFILES[c['profile']];nodes={tag:(ident,nxt) for tag,ident,nxt in c['rows']};found=lookup_tag(nodes,c['head'],c['key'])
    pointer_by_tag={tag:NODES+64*i for i,(tag,_,_) in enumerate(c['rows'])};native=0 if found is None else pointer_by_tag[found]
    world=bytearray((i*13+7)&255 for i in range(0x1700));struct.pack_into('<H',world,0x12,c['head'])
    records=bytearray((i*37+11)&255 for i in range(max(len(c['rows']),1)*64));tags=bytearray(4*max(1,1+max([c['head'],*[t for t,_,_ in c['rows']],*[n for _,_,n in c['rows']]])))
    for i,(tag,ident,nxt) in enumerate(c['rows']):
        struct.pack_into('<I',tags,4*tag,NODES+64*i);struct.pack_into('<H',records,64*i+8,nxt);records[64*i+0x37]=ident;records[64*i+0x32]=c['flags']
        if c['name']=='wrong-kind-owner':struct.pack_into('<I',records,64*i+0x12,3);struct.pack_into('<H',records,64*i+0x28,0xFFFF)
    output=bytearray(64);want=bytearray(output);struct.pack_into('<I',want,0,native)
    mem=[region(WORLD,world),region(NODES,records),region(p['table'],tags),region(OUT,output),region(STACK,pack32(OBS)+pack32(c['key']))]
    checks=[(WORLD,world),(NODES,records),(p['table'],tags),(OUT,want)];observer=b'\xA3'+pack32(OUT)
    contract_details=None
    if c['contract']:
        # Native PRNG mutates caller argument3; unpassed pointer spill is higher.
        seed=1;struct.pack_into('<I',world,0x15A0,seed);struct.pack_into('<I',world,0x9E6,32);struct.pack_into('<I',world,0x9EE,0x20000000)
        mem[0]=region(WORLD,world);expected_world=bytearray(world);new_seed,random_value=rand(seed,9);struct.pack_into('<I',expected_world,0x15A0,new_seed);checks[0]=(WORLD,expected_world)
        point=pack32(10*4096+123)+pack32(12*4096+456)+pack32(0x89ABCDEF);mem.extend([region(POINT,point),region(SPILL,bytes(4)),region(0x75B2F4,bytes(4))]);checks.extend([(POINT,point),(SPILL,pack32(native)),(0x75B2F4,bytes(4))])
        expected_map=0x20000000+16*(12*32+10)
        for at,value in [(4,random_value),(8,expected_map),(12,native),(16,c['flags'])]:struct.pack_into('<I',want,at,value)
        # SP after AP RET4 is STACK+8;spill is [SP+20],point [SP+28].
        observer+=bytes.fromhex('89442420')
        for value in [0x10B,0x699F78,WORLD+0x15A0,9]:observer+=b'\x68'+pack32(value)
        observer+=b'\xB8'+pack32(0x5558D0)+bytes.fromhex('ffd083c410a3')+pack32(OUT+4)
        observer+=bytes.fromhex('8d44242850b9')+pack32(WORLD)+b'\xB8'+pack32(0x508FE0)+bytes.fromhex('ffd0a3')+pack32(OUT+8)
        observer+=bytes.fromhex('8b4c2420890d')+pack32(OUT+12)+bytes.fromhex('0fb64132a3')+pack32(OUT+16)
        contract_details=dict(PRNG=random_value,final_seed=new_seed,map_pointer=expected_map,unpassed_spill=SPILL,position_pointer=POINT)
    observer+=b'\xBD'+pack32(1);at=0x11000000
    for address,b in checks:
        mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=p['program'],address=hex(p['getter']),max_steps=2000000 if len(tags)>4096 else 90000,registers=json.dumps(dict(ECX=hex(WORLD))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(native_pointer=native,returned_tag=found,contract=contract_details)

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in cases():
            args,expected=build(c);path=ROOT/'ghidra'/f'phase76-emulation-AP-{c["profile"]}-{c["name"]}.json'
            if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected']==expected
            else:
                response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=expected,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['profile'],c['name'],r,expected)
            rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),profile=c['profile'],case=c['name'],contract=c['contract']));print(c['profile'],c['name'],'verified',flush=True)
    (ROOT/'phase76-ap-lookup-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
