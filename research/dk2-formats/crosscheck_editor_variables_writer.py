"""Unmodified Ghidra comparison, with explicit FISTP semantic differences.

Ghidra12 x86 FISTP unconditionally uses round(ST0), ignoring FLDCW rounding
mode; native __ftol truncates. Compare all other bytes identically and require
the exact independently predicted rounded DWORDs, reporting differences.
"""
import asyncio,json,os,struct,hashlib
from pathlib import Path
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_hero_parties import compare_code
import emulate_editor_variables_writer as native
engine=native.defaults.engine
ROOT=Path(__file__).resolve().parent
CONTROLS=['tail-A','tail-B','fractional-negative','negative-type7','type74-product-wrap','manual-ID-extension','type66-list']

def build(name):
    c=next(c for c in native.cases() if c['name']==name);f,d,_=native.execute(c);native.verify(c,f,d['result'])
    regions=json.loads(json.dumps(f['memory']));first=f['calls'][0];driver=bytearray()
    for call in f['calls'][1:]:
        driver+=b'\xbc'+struct.pack('<I',engine.STACK+4+4*len(call['stack']))+bytes.fromhex('33c033db33d233f633ffbd')+struct.pack('<I',engine.STACK)
        for value in reversed(call['stack']):driver+=b'\x68'+struct.pack('<I',value)
        driver+=b'\xb9'+struct.pack('<I',call['ecx'])+b'\xb8'+struct.pack('<I',call['address'])+bytes.fromhex('ffd0')
        if call['address']==0x49F5E0:driver+=bytes.fromhex('83c404')
    driver+=bytes.fromhex('bd01000000bb00000000')
    for row in regions:
        if int(row['address'],16)==engine.STACK-0x4000:
            buf=bytearray.fromhex(row['hex']);initial=struct.pack('<'+'I'*(1+len(first['stack'])),engine.STOP,*first['stack']);buf[0x4000:0x4000+len(initial)]=initial;row['hex']=buf.hex()
    shadow=0x18000000;count=size=0
    for row in d['result']['output']:
        address=int(row['address'],16);data=bytearray.fromhex(row['hex'])
        if address==engine.writer.OUTPUT:
            for diff in rounding_differences(name):struct.pack_into('<I',data,diff['output_offset'],diff['Ghidra_FISTP_bits'])
        data=bytes(data)
        regions.append(engine.region(shadow,data))
        driver+=compare_code(address,shadow,len(data))+bytes.fromhex('85c07505bb')+struct.pack('<I',count+1)+bytes.fromhex('21c5')
        count+=1;size+=len(data);shadow+=(len(data)+255)//256*256
    driver+=bytes.fromhex('68efbeaddec3');regions.append(engine.region(engine.STOP,driver))
    return dict(program='DK2 Editor pro.exe',address=hex(first['address']),
        registers=json.dumps(dict(ECX=hex(first['ecx']),ESP=hex(engine.STACK),DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=5000000,return_registers='EAX,EBP,EBX,ECX,EIP,ESP'),count,size


def rounding_differences(name):
    c=next(x for x in native.cases() if x['name']==name);f,_,_=native.execute(c)
    mem={int(x['address'],16):bytes.fromhex(x['hex']) for x in f['memory']}
    ids,_,_=native.defaults.static_defaults();ids+=([29] if c['extra'] else [])
    rows=[]
    for i,typ in enumerate(ids):
        x=struct.unpack_from('<d',mem[native.VALUES],typ*8)[0]*(4096 if typ in {67,155,158,185,220,221} else 1)
        a,b=int(x)&0xffffffff,round(x)&0xffffffff
        if a!=b:rows.append(dict(type=typ,output_offset=56+16*i+4,bytes=4,scaled_binary64=x,native_trunc_bits=a,Ghidra_FISTP_bits=b))
    return rows

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
                path=ROOT/'ghidra'/f'phase199-editor-Variables-writer-Ghidra-{name}-{tag}.json'
                if path.exists():
                    saved=json.loads(path.read_text(encoding='utf-8'));assert saved['input']==args
                else:
                    result=await session.call_tool('emulate_function',args)
                    raw=json.loads('\n'.join(c.text for c in result.content if c.type=='text'))
                    saved=dict(input=args,result=raw.get('data',raw),tool_is_error=result.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf-8')
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],r
                assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items()),r
                rows.append(dict(case=name,file=str(path.relative_to(ROOT)),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),checked_regions=count,checked_bytes=size,known_FISTP_differences=rounding_differences(name),equal_native_bytes=size-4*len(rounding_differences(name)),whole_native_output_equal=not rounding_differences(name)))
                print(name,count,size,'passed',flush=True)
    (ROOT/'phase199-editor-Variables-writer-crosschecks.json').write_text(json.dumps(dict(replays=rows,scope=__doc__,original_instructions_modified=0,known_Ghidra_FISTP_limitation=True),indent=2),encoding='utf-8')

if __name__=='__main__':asyncio.run(main())
