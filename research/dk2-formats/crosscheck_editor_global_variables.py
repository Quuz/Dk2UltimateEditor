"""Independent Ghidra CPU comparison of complete selected Variables scene memory."""
import asyncio,json,os,struct,hashlib
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_hero_parties import compare_code
import emulate_editor_global_variables as native
ROOT=Path(__file__).resolve().parent
CONTROLS=['scaled-all','unknown233','reinitialize-retains','temple43']

def build(name):
    c=next(c for c in native.cases() if c['name']==name);f,d,_=native.execute(c);native.verify(c,f,d['result'])
    regions=json.loads(json.dumps(f['memory']));first=f['calls'][0];driver=bytearray()
    for call in f['calls'][1:]:
        for value in reversed(call['stack']):driver+=b'\x68'+struct.pack('<I',value)
        driver+=b'\xb9'+struct.pack('<I',call['ecx'])+b'\xb8'+struct.pack('<I',call['address'])+bytes.fromhex('ffd0')
        if call['address']==0x49EC90:driver+=bytes.fromhex('83c404')
    driver+=bytes.fromhex('bd01000000bb00000000')
    for row in regions:
        if int(row['address'],16)==native.engine.STACK-0x4000:
            buf=bytearray.fromhex(row['hex']);struct.pack_into('<I',buf,0x4000,native.engine.STOP);row['hex']=buf.hex()
    shadow=0x18000000;count=size=0
    for row in d['result']['output']:
        address=int(row['address'],16);data=bytes.fromhex(row['hex'])
        regions.append(native.engine.region(shadow,data))
        driver+=compare_code(address,shadow,len(data))+bytes.fromhex('85c07505bb')+struct.pack('<I',count+1)+bytes.fromhex('21c5')
        count+=1;size+=len(data);shadow+=(len(data)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3');regions.append(native.engine.region(native.engine.STOP,driver))
    return dict(program='DK2 Editor pro.exe',address=hex(first['address']),
        registers=json.dumps(dict(ECX=hex(first['ecx']),ESP=hex(native.engine.STACK),DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=5000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP'),count,size

async def main():
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in CONTROLS:
                args,count,size=build(name);tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:12]
                path=ROOT/'ghidra'/f'phase166-editor-variables-Ghidra-{name}-{tag}.json'
                if path.exists():
                    saved=json.loads(path.read_text(encoding='utf-8'));assert saved['input']==args
                else:
                    result=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(c.text for c in result.content if c.type=='text'))
                    saved=dict(input=args,result=raw.get('data',raw),tool_is_error=result.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf-8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],r
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),r
                rows.append(dict(case=name,file=str(path.relative_to(ROOT)),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),checked_regions=count,checked_bytes=size))
                print(name,count,size,'passed',flush=True)
    (ROOT/'phase166-editor-variables-crosschecks.json').write_text(json.dumps(dict(replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':asyncio.run(main())
