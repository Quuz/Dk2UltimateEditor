"""Execute native554B30 with finite prepared free/used lists and full checks."""
from pathlib import Path
import asyncio,json,os,struct,hashlib,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
import memory_readout
ROOT=Path(__file__).resolve().parent
POOL=0x10000000;FH=0x10001000;FT=0x10002000;UH=0x10003000;UT=0x10004000;BLOCK=0x10005000;OUT=0x10006000;OBS=0x10008000
def cases():
    return [dict(name=n,size=size,request=request,seed=seed,empty=empty) for n,size,request,seed,empty in [
      ('shot-split',512,371,165,False),('shot-exact',384,371,165,False),('shot-absorbs12',396,371,165,False),
      ('shot-minimum-split',400,371,0,False),('shot-too-small',380,371,165,False),('empty',512,371,165,True),
      ('align-zero',512,0,165,False),('align-one',512,1,165,False),('align-four',512,4,165,False)]]
def build(c):
    pool=bytearray(0x34);struct.pack_into('<III',pool,0x28,UT,FH,123)
    fh=bytearray(12);ft=bytearray(12);uh=bytearray(12);ut=bytearray(12);block=bytearray([c['seed']])*max(512,c['size'])
    struct.pack_into('<III',fh,0,0,0,FT if c['empty'] else BLOCK);struct.pack_into('<III',ft,0,0,FH if c['empty'] else BLOCK,0)
    struct.pack_into('<III',uh,0,0,0,UT);struct.pack_into('<III',ut,0,0,UH,0);struct.pack_into('<III',block,0,c['size'],FH,FT)
    wanted=[bytearray(b) for b in [pool,fh,ft,uh,ut,block]];need=((c['request']+3)&0xFFFFFFFC)+12;success=not c['empty'] and need<=c['size'];ret=0
    if success:
        remain=c['size']-need
        if remain>12:
            struct.pack_into('<I',wanted[5],0,remain);at=remain
        else:
            struct.pack_into('<I',wanted[1],8,FT);struct.pack_into('<I',wanted[2],4,FH);at=0
        struct.pack_into('<III',wanted[5],at,need if remain>12 else c['size'],UH,UT)
        struct.pack_into('<I',wanted[3],8,BLOCK+at);struct.pack_into('<I',wanted[4],4,BLOCK+at);struct.pack_into('<I',wanted[0],0x30,123+need);ret=BLOCK+at+12
    buffers=[(a,b,w) for a,b,w in zip([POOL,FH,FT,UH,UT,BLOCK],[pool,fh,ft,uh,ut,block],wanted)]+[(OUT,bytes(4),pack32(ret))]
    mem=[region(a,b) for a,b,_ in buffers]+[region(0x7FFF0000,pack32(OBS)+pack32(c['request']))]
    observer=b'\xA3'+pack32(OUT)+b'\xBD'+pack32(1);at=0x11000000
    for address,_,b in buffers:mem.append(region(at,b));observer+=compare_code(address,at,len(b))+bytes.fromhex('21c5');at+=((len(b)+15)//16)*16
    observer+=memory_readout.code(BLOCK)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3';mem.append(region(OBS,observer))
    args=dict(program='DKII-DX.exe',address='0x554b30',max_steps=10000,registers=json.dumps(dict(ECX=hex(POOL))),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,'+memory_readout.REGISTERS)
    return args,dict(native_return=ret,requested_payload=c['request'],accounted_bytes=need if success else 0,readout_hex=bytes(wanted[5][:128]).hex())
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
      async with ClientSession(rd,wr) as session:
        await session.initialize()
        for c in selected:
            args,expected=build(c);p=ROOT/'ghidra'/f'phase80-emulation-pool-{c["name"]}.json'
            if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected']==expected
            else:
                response=await session.call_tool('emulate_function',args);value='\n'.join(x.text for x in response.content if x.type=='text');r=json.loads(value)
                if response.isError or 'error' in r:raise RuntimeError(str(r))
                saved=dict(case=c,input=args,expected=expected,result=r.get('data',r));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
            r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items()),(c['name'],r)
            assert memory_readout.decode(r['registers']).hex()==expected['readout_hex']
            rows.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase80-native-pool-emulations.json').write_text(json.dumps(dict(scope=__doc__,cases=rows),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
