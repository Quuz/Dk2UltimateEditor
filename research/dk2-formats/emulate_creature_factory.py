"""Native CCreature ctor→free-slot factory→full initializer, no game code patches.

One prepared unused slot models the original 448×1343 pool. Native constructor
registers its tag; factory removes it and initializes the real CCreature.
Runtime definition, owner table and level coefficients are explicit fixtures.
External null GFX, fixed sound and range-checked IsBadReadPtr are providers.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_object_factory as f
import fixture_read_probe as probe
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import memory_readout_ymm as readout
ROOT=Path(__file__).resolve().parent
ACTOR=0x10080000;DEFINITION=0x100c0000;STATS=0x100d0000;VARIABLES=0x100e0000;DRIVER=0x100f0000
def cases():return [dict(name='base',ident=8,pre_level=1,active_count=0,position=[0x5800,0x6800,0x2000])]
def build(c,page):
    base=f.cases()[0];args,old=f.build(base,0);allmem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']};checks={};shadow=0x11000000
    for row in old['checks']:
        checks[int(row['address'],16)]=allmem[shadow];shadow+=((row['bytes']+255)//256)*256
    memory={a:b for a,b in allmem.items() if a<0x11000000 and a not in [f.OBS,0x7ffef000]}
    # No Object allocation occurs. Reconstruct expected original input state.
    for a in [f.WORLD,f.PLAYER,f.MAP,f.FH,f.FT,f.UH,f.UT,0x6ee540,0x6ed540,0x6a4818,0x6f2540,f.KINDS]:checks[a]=bytearray(memory[a])
    for a in list(checks):
        if f.BLOCK<=a<f.BLOCK+768:del checks[a]
    checks[f.BLOCK]=bytearray(memory[f.BLOCK])
    def both(a,o,v,n=4):f.put(memory[a],o,v,n);f.put(checks[a],o,v,n)
    for o,v,n in [(0x24,100,2),(0x26,c['active_count'],4),(0x15c4,DEFINITION,4),(0x15cc,1,4)]:both(f.WORLD,o,v,n)
    both(0x6e504c,0,f.WORLD+8)
    both(f.SESSION,4,100);both(f.PLAYER,0x4e,1);both(f.PLAYER,0x52,STATS)
    for a,v in [(0x6dc424,f.WORLD),(0x6ed484,f.WORLD),(0x6e5054,f.WORLD),(0x6e4154,VARIABLES),(0x6f2590,20),(0x6f28a3,1000)]:memory[a]=bytearray(pack32(v));checks[a]=bytearray(pack32(v))
    definition=bytearray(2199);definition[0x770]=c['ident'];definition[0x5b3]=c['ident']
    for o,v,n in [(0x5b4,0x1234,4),(0x5b8,0x2345,4),(0x5bc,0x3456,4),(0x626,4096,2),(0x63a,4096,2),(0x648,4096,2),(0x690,65535,2),(0x692,1000,2)]:f.put(definition,o,v,n)
    stats=bytearray(13);stats[0]=c['ident'];wanted_stats=bytearray(stats);f.put(wanted_stats,9,1)
    variables=bytearray(struct.pack('<H',100)*2048);f.put(variables,0x40,20)
    actor=bytearray(1343)+bytearray([0xa5])*256;actor[0x403]=c['pre_level']
    for a,b,w in [(ACTOR,actor,None),(DEFINITION,definition,definition),(STATS,stats,wanted_stats),(VARIABLES,variables,variables)]:
        memory[a]=bytearray(b)
        if w is not None:checks[a]=bytearray(w)
    checks[ACTOR+1343]=bytearray([0xa5])*256
    # Native constructor507850 consumes one tag; no memory-pool block is allocated.
    f.put(checks[0x6ee540],100*4,ACTOR);checks[0x6ed540][100]=1;f.put(checks[0x6a4818],0,4093);f.put(checks[0x6a4818],4,101)
    f.put(checks[f.WORLD],0x24,0,2);f.put(checks[f.WORLD],0x26,c['active_count']+1);f.put(checks[f.WORLD],8,100,2)
    seed=int.from_bytes(memory[f.WORLD][0x15a0:0x15a4],'little')
    for _ in range(2):seed=(seed*0x19660d+0x3c6ef35f)&0xffffffff
    f.put(checks[f.WORLD],0x15a0,seed)  # native 491900/4919c0→5558d0
    f.put(checks[f.PLAYER],6,100,2);f.put(checks[f.PLAYER],0x16,1);checks[f.PLAYER][0x10f8]=1
    # 48795e→4be330: once-per-owner timer, not three unrelated bytes.
    # The fixture leaves global 6f2611 zero and uses tick100/TPS20.
    f.put(checks[f.PLAYER],0x76,0x200);f.put(checks[f.PLAYER],0xb4f,100);f.put(checks[f.PLAYER],0xb53,100)
    cell=((c['position'][1]>>12)*32+(c['position'][0]>>12))*16;f.put(checks[f.MAP],cell,100,2)
    memory[f.ARGS]=bytearray(struct.pack('<3I',*c['position']));checks[f.ARGS]=bytearray(memory[f.ARGS])
    memory[f.OUT]=bytearray(pack32(0)+pack32(0xcafebabe)+bytes(8));checks[f.OUT]=bytearray(pack32(1)+pack32(ACTOR)+pack32(0)+pack32(1))
    driver=bytearray(b'\x68'+pack32(f.OUT+4)+b'\x68'+pack32(f.ARGS)+b'\x68'+pack32(7)+b'\x68'+pack32(c['ident'])+b'\xb9'+pack32(f.WORLD+8))
    driver+=b'\xe8'+pack32(0x4b78d0-(DRIVER+len(driver)+5))+b'\xa3'+pack32(f.OUT)+b'\x68'+pack32(f.OBS)+b'\xc3';memory[DRIVER]=driver
    stack=bytearray([0xa5])*16384;f.put(stack,8192,DRIVER);memory[0x7ffee000]=stack
    inputs=[region(a,b) for a,b in memory.items()]
    readable=probe.allowed_ranges((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes(),inputs)
    memory[probe.ADDRESS]=bytearray(probe.adapter(readable));memory[probe.COUNTER]=bytearray(8);checks[probe.COUNTER+4]=bytearray(4)
    observer=bytearray(b'\xbd'+pack32(1)+b'\xbb'+pack32(0));mem=[region(a,b) for a,b in memory.items()];shadow=0x11000000
    for i,(actual,data) in enumerate(checks.items()):
        mem.append(region(shadow,data));observer+=compare_code(actual,shadow,len(data))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5');shadow+=((len(data)+255)//256)*256
    observer+=readout.code(ACTOR+256*page)+bytes.fromhex('8b15')+pack32(probe.COUNTER)+b'\x68'+pack32(0xdeadbeef)+b'\xc3';mem.append(region(f.OBS,observer))
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    args.update(address='0x4b5e10',max_steps=1000000,registers=json.dumps(dict(ECX=hex(ACTOR),EAX='0',EBX='0',EDX='0',ESI='0',EDI='0',DF='0')),memory=json.dumps(dict(regions=mem)),return_registers='EAX,EBP,EBX,ECX,EDX,EIP,ESP,'+readout.REGISTERS)
    return args,dict(actor=ACTOR,ident=c['ident'],owner=7,position=c['position'],level=1,health=1000,checks=[dict(address=hex(a),bytes=len(b)) for a,b in checks.items()])
def verify(c,b,e):
    u=lambda o,n=4:int.from_bytes(b[o:o+n],'little')
    assert len(b)==1343 and u(0)==0x66d524 and u(4,2)==100 and u(0x12)==0 and u(0x370)==DEFINITION
    assert u(0x36f,1)==c['ident'] and u(0x403,1)==1 and u(0x40)==e['health'] and u(0x28,2)==7 and u(0x2a)==f.PLAYER
    assert u(0x24a,2)==u(0x24c,2)==0 and list(struct.unpack_from('<3I',b,0x1a))==e['position']
async def main():
    selected=cases();params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                chunks=[]
                for page in range(6):
                    if len(sys.argv)>1 and page!=int(sys.argv[1]):continue
                    args,e=build(c,page);p=ROOT/'ghidra'/f'phase90-creature-factory-{c["name"]}-page{page}.json'
                    if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
                    else:
                        response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                        if response.isError or 'error' in r:raise RuntimeError(str(r))
                        saved=dict(case=c,page=page,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                    r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c['name'],page,r,e)
                    chunks.append(readout.decode(r['registers']));rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name'],page=page));print(c['name'],page,'verified',flush=True)
                if len(chunks)==6:
                    b=b''.join(chunks)[:1343];verify(c,b,e);(ROOT/'ghidra'/f'phase90-creature-factory-{c["name"]}.bin').write_bytes(b)
    if len(sys.argv)==1:(ROOT/'phase90-creature-factory-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
