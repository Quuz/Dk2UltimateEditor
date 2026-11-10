"""Isolated original checksum/guard branches; adapters live in emulator memory only.

For branches beginning at JNZ an entry adapter executes TEST EAX,EAX first.
The full original signed-world loop is run with a readout replacing its RET.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from native_catalogue_checksums import definition_checksum,signed_world_accumulate
from emulate_element_colors import region,jump,pack32

ROOT=Path(__file__).resolve().parent

def cases():
    schema=json.loads((ROOT/'catalogue-checksums.json').read_text(encoding='utf8'))
    result=[]
    for row in schema['families']:
        for pattern,gate in [('zero',0),('ff',0),('ramp',0),('ramp',1),('ramp',0xFFFFFFFF)]:
            result.append(dict(name=row['family']+'-'+pattern+'-gate'+str(gate),kind='definition',family=row['family'],pattern=pattern,gate=gate,previous=0xD1234567))
    for i,(payload,previous) in enumerate([(b'',0x12345678),(b'\xff',0),(b'\x80',0),(b'\x7f',0),(bytes(range(256)),0),
            (bytes(range(256)),0xFFFFFFFF),(b'\x7f'*3,0xFFFFFFF0),(b'\x80\xff\x00\x01',0x10)]):
        result.append(dict(name=f'world-{i}',kind='world',payload=payload.hex(),previous=previous))
    return result

def build_case(case):
    if case['kind']=='world':
        payload=bytes.fromhex(case['payload']);world=bytearray(0xA400);struct.pack_into('<I',world,0x72E7,case['previous'])
        stack=pack32(0xDEADBEEF)+pack32(0x10010000)+pack32(len(payload))
        memory=[region(0x10000000,world),region(0x10010000,payload or b'\0'),region(0x7FFF0000,stack),
            region(0x5108EB,bytes.fromhex('8b81e7720000c20800'))]
        registers=dict(ECX='0x10000000');entry=0x5108C0
        expected=signed_world_accumulate(payload,case['previous']);scope='Full original accumulator body; RET replaced by readout+RET8.'
    else:
        schema=json.loads((ROOT/'catalogue-checksums.json').read_text(encoding='utf8'))
        row=next(r for r in schema['families'] if r['family']==case['family']);e=row['emulator'];length=row['intermediate_bytes']
        payload=bytes(length) if case['pattern']=='zero' else bytes([255])*length if case['pattern']=='ff' else bytes((i*37+128)&255 for i in range(length))
        definition=bytearray(0x1000);struct.pack_into('<I',definition,row['runtime_checksum_offset'],case['previous'])
        registers={e['destination_register']:'0x10000000',e['source_register']:'0x10010000',
            'EAX':hex(case['gate']),'EFLAGS':'0x40' if case['gate']==0 else '0x0'}
        if row['family']=='Terrain':registers.update(EDI='0x0',ECX='0x10000000')
        if row['family']=='Creatures':registers['EDI']='0x0'
        modrm={'EBX':0x83,'EBP':0x85,'EDI':0x87}[e['destination_register']]
        readout=bytes([0x8B,modrm])+pack32(row['runtime_checksum_offset'])+b'\xC3'
        entry=e['entry'];entry_adapter=jump(entry,e['start'])
        if row['family'] not in ['Terrain','Rooms','Creatures']:
            # Setting EFLAGS alone did not set emulator ZF; execute TEST explicitly.
            entry_adapter=b'\xB8'+pack32(case['gate'])+b'\x85\xC0'+jump(entry+7,e['start'])
            registers.pop('EFLAGS')
        memory=[region(0x10000000,definition),region(0x10010000,payload),
            region(entry,entry_adapter),region(e['end_exclusive'],readout)]
        if row['family']=='Rooms':memory.append(region(0x4EAD44,readout))
        expected=definition_checksum(row['family'],payload,gate_value=case['gate'],previous=case['previous'])
        scope='Original reset and byte-sum loop plus guard branch. Surrounding prolog skipped. JNZ-only entries get ZF from an explicit entry TEST adapter; pointer registers supplied. Rooms skip target has a second readout adapter.'
    return dict(program='DKII-DX.exe',address=hex(entry),max_steps=100000,registers=json.dumps(registers),
        memory=json.dumps(dict(regions=memory)),return_registers='EAX,ESP,EIP'),dict(EAX=expected),scope

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                path=ROOT/'ghidra'/('phase22-emulation-checksum-'+case['name']+'.json')
                args,expected,scope=build_case(case)
                if path.exists():
                    saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args)
                    raw='\n'.join(c.text for c in response.content if c.type=='text');result=json.loads(raw)
                    if response.isError or 'error' in result:raise RuntimeError(raw)
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,scope=scope,result=result)
                    path.write_text(json.dumps(saved,ensure_ascii=False,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert int(result['registers']['EAX'],16)==expected['EAX'],(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']))
                print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase22-checksum-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')

if __name__=='__main__':asyncio.run(main())
