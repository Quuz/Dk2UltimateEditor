"""Full Pro Creature writer: HateJobs3/4 overwritten, extra file slots untouched.

The original writer/getters/string/numeric/resource/stack functions run.
Only the existing date-helper fixture and external file provider are used.
No live files emitted. Complete output is captured but only selected spans
and source model preservation are independently compared.
"""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import emulate_creature_extension as prior
import memory_readout_ymm as wide
ROOT=Path(__file__).resolve().parent
MODEL=0x10002000;CAPTURE=0x10012000;OBS=0x10015000;EXPECTED=0x10016000
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
    return [dict(name=n,seed=seed,hates=hates,alternatives=[list(v) for v in alt]) for n,seed,hates,alt in [
      ('hates1234-altzero',0,[1,2,3,4],[(0,0,0)]*3),
      ('hates1234-alt5',0,[1,2,3,4],[(5,9,1),(7,2,3),(8,4,5)]),
      ('hates-high-altzero',165,[0x11111101,0x22222202,0x33333303,0x44444404],[(0,0,0)]*3),
      ('hates-changed-last',165,[0x11111101,0x22222202,0xDEADBEEF,0xCAFEBABE],[(0,0,0)]*3),
      ('hates-high-altdistinct',255,[0x11111101,0x22222202,0x33333303,0x44444404],[(0x12345605,0x7F09,0x8011),(0x87654321,0xABCD,0x1234),(0xFFFFFFFF,0xFFFF,0x8000)]),
      ('hates-pattern-altdistinct','pattern',[1,2,3,4],[(0x89ABCDEF,0x8009,0xFFFF),(0,0,0),(0,0,0)])]]

def build(c):
    base=dict(name='phase86-writer',kind='writer',seed=c['seed'],flags=0,count=1,label_length=5,animation_update=0)
    args,_=prior.build_case(base);mem={int(r['address'],16):bytes.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    model=bytearray(mem[MODEL]);struct.pack_into('<4I',model,0x156C,*c['hates'])
    for i,(job,mood,mana) in enumerate(c['alternatives']):struct.pack_into('<IHH',model,0x157C+8*i,job,mood,mana)
    mem[MODEL]=bytes(model)
    band=struct.pack('<2I',*c['hates'][:2])+b''.join(struct.pack('<IHH',*v) for v in c['alternatives']);assert len(band)==32
    rawbase=0x7FFF0000-0x1640+0x94;offset=rawbase+0x14E1-0x7FFED000;tail=mem[0x7FFED000][offset:offset+8];assert len(tail)==8
    want=band+tail+bytes(model);mem[EXPECTED]=want
    observer=b'\xBD'+pack32(1)+b'\x31\xDB'
    for tag,(actual,expected,n) in enumerate([(CAPTURE+0xE74,EXPECTED,32),(CAPTURE+0x14E1,EXPECTED+32,8),(MODEL,EXPECTED+40,len(model)),(0x10011000,EXPECTED+40+len(model),4)],1):
        if tag==4:mem[EXPECTED]+=pack32(1)
        observer+=compare_code(actual,expected,n)+bytes.fromhex('85c07505bb')+pack32(tag)+bytes.fromhex('21c5')
    observer+=wide.code(CAPTURE+0xE74) #256bytes includes other independently opaque fields
    # YMM1 deliberately overwritten with second independent file span.
    observer+=bytes.fromhex('c5fe6f0d')+pack32(CAPTURE+0x14E1)+END;mem[OBS]=observer
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]));args['return_registers']='EAX,ECX,EBP,EBX,EDX,ESP,EIP,'+wide.REGISTERS
    return args,dict(raw_bytes=5537,hate_model_offset=0x156C,alternative_model_offset=0x157C,raw_hate_first_two=0xE74,overwritten_raw_hate_last_two=0xE7C,raw_alternative0=0xE7C,raw_extras=0x14E1,
      expected_raw_band_hex=band.hex(),expected_raw_extras_hex=tail.hex(),full_output_captured_but_not_fully_decoded=True,source_model_bytes_compared=len(model),date_helper_adapter_address=0x4AB320)
def verify(c,r,e):
    assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items()),(c,r)
    b=wide.decode(r['registers']);assert b[:32].hex()==e['expected_raw_band_hex'] and b[32:40].hex()==e['expected_raw_extras_hex'];return b
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,e=build(c);p=ROOT/'ghidra'/f'phase86-emulation-hate-writer-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==e
            else:
                print(c['name'],'request',flush=True);response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=e,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            b=verify(c,saved['result'],e);(ROOT/'ghidra'/f'phase86-hate-writer-{c["name"]}.bin').write_bytes(b)
            rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase86-hate-writer-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
