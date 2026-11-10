"""Original Effects flags, load tail, converter and captured native-writer suffix.

All entry/exit adapters and the file capture callback are emulator memory only.
Writer enters after scalar/x87 conversion, runs original copies, flags encoder,
resource getter, CString helper and file vcall. Names are valid length0..32.
Converter enters its full body with a no-op strncpy stub: raw name and zeroed
destination are both32 zero bytes, so this skipped copy has the same byte result.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,jump,pack32
from native_effect_editor import *
ROOT=Path(__file__).resolve().parent
def cases():
    rows=[]
    for raw in [0,1,8,0x40,0x80,0xC0,0xFFFF,0xFFFFFFFF,0x10000,0x4000]:
        for kind in ['flags','model']:rows.append(dict(name=f'{kind}-{raw}',kind=kind,flags=raw))
    for i,incoming in enumerate([[0]*12,list(range(1,13)),[0,1,0,0x8000,0,0xFFFF,0,7,0,9,0,11]]):
        rows.append(dict(name=f'tail-{i}',kind='tail',incoming=incoming,previous=list(range(100,112))))
    for f4 in [0,3880,0xA55A,0xFFFF]:rows.append(dict(name=f'converter-{f4}',kind='converter',f4=f4))
    for i,(poison,flags,name) in enumerate([(0,0xFFFFFFFF,''),(3880,0,'E'),(0xA55A,0xC0,'Effect'),(0xFFFF,8,'N'*32)]):
        rows.append(dict(name=f'writer-{i}',kind='writer',poison=poison,flags=flags,label=name))
    return rows
def load_abs(reg,address):return bytes([0x8B,0x05+(reg<<3)])+pack32(address)
def build_case(case):
    kind=case['kind'];program='DK2 Editor pro.exe';registers={};memory=[]
    if kind=='flags':
        entry=0x41FF90;registers=dict(ECX='0x10000000')
        adapter=b'\xB9'+pack32(0x10000000)+jump(0x10002005,0x41FEC0)
        memory=[region(0x10000000,bytes(0x300)),region(0x10002000,adapter),region(0x7FFF0000,pack32(0x10002000)+pack32(case['flags'])+pack32(0xDEADBEEF))]
        expected=dict(EAX=flag_encode_decode(case['flags']))
    elif kind=='model':
        entry=0x4A2F50;mid=bytearray(0x200);struct.pack_into('<I',mid,0x4C,1);struct.pack_into('<I',mid,0x168,case['flags'])
        adapter=bytes.fromhex('8bcd')+jump(0x4A3293+2,0x41FEC0)
        memory=[region(0x10000000,mid),region(0x10001000,bytes(0x300)),region(entry,jump(entry,0x4A3267)),region(0x4A3293,adapter)]
        registers=dict(EBX='0x10000000',EBP='0x10001000',EDI='0x0');expected=dict(EAX=native_loaded_flags(case['flags']))
    elif kind=='tail':
        program='DKII-DX.exe';entry=0x4358C0;mid=bytearray(0x200);definition=bytearray(0x100)
        struct.pack_into('<12H',mid,0x18E,*case['incoming']);struct.pack_into('<12H',definition,0x37,*case['previous'])
        readout=b''
        for reg,i in [(0,0),(3,1),(1,2),(2,3),(6,4),(7,5)]:readout+=load_abs(reg,0x10001037+4*i)
        readout+=b'\xC3'
        memory=[region(0x10000000,mid),region(0x10001000,definition),region(entry,jump(entry,0x435BBA)),region(0x435BDC,readout)]
        registers=dict(EBX='0x10000000',EBP='0x10001000');out=tail_update(case['previous'],case['incoming'])
        expected={reg:out[i*2]|(out[i*2+1]<<16) for reg,i in [( 'EAX',0),('EBX',1),('ECX',2),('EDX',3),('ESI',4),('EDI',5)]}
    elif kind=='converter':
        program='DKII-DX.exe';entry=0x56AFE0;raw=bytearray(246);mid=bytearray(0x1C0)
        struct.pack_into('<H',raw,0xF4,case['f4']);struct.pack_into('<I',raw,0xB4,0x80000008)
        struct.pack_into('<12H',raw,0xDA,*range(1,13));raw[0xF2:0xF4]=bytes([4,7])
        struct.pack_into('<I',mid,0x168,0x40000000);mid[0x1A8:]=b'\xA5'*(len(mid)-0x1A8)
        # FNV1a over the complete intermediate, not just a returned field.
        readout=b'\xB8'+pack32(0x811C9DC5)+b'\xBE'+pack32(0x10001000)+b'\xB9'+pack32(len(mid))
        readout+=bytes.fromhex('0fb61633c269c093010001464975f1')
        readout+=load_abs(3,0x10001168)+load_abs(1,0x100011A4)+load_abs(2,0x100011A8)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
        memory=[region(0x10000000,raw),region(0x10001000,mid),region(0x6361A0,b'\xC3'),region(0x10003000,readout),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))]
        registers=dict(ECX='0x10001000')
        # Independent expected complete converter output for this controlled raw.
        out=bytearray(mid)
        for off in range(0,0xB4,4):struct.pack_into('<I',out,off,1)
        out[0xB4:0x168]=raw[:0xB4];out[0xB4+31]=0
        # Scalar layout differs from file after copied name/resource/light block.
        for dest,source,size in [(0x140,0x8C,4),(0x144,0x90,4),(0x148,0x94,4),(0x14C,0x98,4),
            (0x150,0x9C,4),(0x154,0xA0,4),(0x158,0xA4,4),(0x15C,0xA8,4),(0x160,0xAC,4),(0x164,0xB0,4),
            (0x16C,0xB8,2),(0x16E,0xBA,2),(0x170,0xBC,2),(0x172,0xBE,2),(0x174,0xC0,2),(0x176,0xC2,2),
            (0x178,0xC4,2),(0x17A,0xC6,2),(0x17C,0xC8,2),(0x17E,0xCA,16),(0x18E,0xDA,24),(0x1A6,0xF2,1),(0x1A7,0xF3,1)]:out[dest:dest+size]=raw[source:source+size]
        struct.pack_into('<I',out,0x168,0xC0000008)
        value=0x811C9DC5
        for b in out:value=((value^b)*0x1000193)&0xFFFFFFFF
        expected=dict(EAX=value,EBX=0xC0000008,ECX=0x0704000C,EDX=0xA5A5A5A5)
    else:
        entry=0x4AA040;model=bytearray(0x300);stack=bytearray(b'\xCC'*0x200);label=case['label'].encode('ascii')
        struct.pack_into('<I',model,8,0x10004008);struct.pack_into('<8H',model,0x1C4,*range(100,108));struct.pack_into('<12H',model,0x1D4,*range(200,212));model[0x1EE]=4
        model[0xCC:0x120]=bytes(range(84));model[0x13C:0x154]=bytes(range(24))
        for i,bit in enumerate(FLAG_MODEL_BITS):struct.pack_into('<I',model,0x1F0+4*i,(case['flags']>>bit)&1)
        struct.pack_into('<I',stack,0,0xDEADBEEF);struct.pack_into('<H',stack,0x140,case['poison']);stack[0x13F]=7;struct.pack_into('<I',stack,0x148,0x10002000)
        name=pack32(len(label))+pack32(0)+label+b'\x00';file=pack32(0x10003000);vtable=bytearray(0x40);struct.pack_into('<I',vtable,0x38,0x10005000)
        # Save the exact length, destination buffer and all246 bytes emitted by file vcall.
        capture=bytes.fromhex('56578b74240c8b4c2410')+b'\x89\x0D'+pack32(0x10007000)+b'\xBF'+pack32(0x10006000)+bytes.fromhex('f3a45f5ec20800')
        readout=bytes.fromhex('0fb705')+pack32(0x100060F4)+load_abs(3,0x100060B4)+load_abs(1,0x10007000)+load_abs(2,0x100060F0)
        # FNV1a over the complete captured record.
        readout+=b'\xBE'+pack32(0x811C9DC5)+b'\xBF'+pack32(0x10006000)+b'\xB9'+pack32(246)+bytes.fromhex('0fb62f33f569f693010001474975f1')
        readout+=load_abs(1,0x10007000)+b'\xC3'
        memory=[region(0x10000000,model),region(0x10002000,file),region(0x10003000,vtable),region(0x10004000,name),region(0x10005000,capture),
            region(0x10006000,bytes(0x100)),region(0x10007000,bytes(4)),region(0x7FFF0000,stack),region(entry,jump(entry,0x4AA2BF)),region(0x4AA370,readout)]
        registers=dict(EBX='0x10000000',EBP='0x10008000')
        out=bytearray(stack[0x4C:0x4C+246]);out[:32]=label+bytes(32-len(label));out[0x20:0x74]=model[0xCC:0x120];out[0x74:0x8C]=model[0x13C:0x154]
        struct.pack_into('<I',out,0xB4,flag_encode_decode(case['flags']));struct.pack_into('<8H',out,0xCA,*range(100,108));struct.pack_into('<12H',out,0xDA,*range(200,212));out[0xF2]=4
        value=0x811C9DC5
        for b in out:value=((value^b)*0x1000193)&0xFFFFFFFF
        expected=dict(EAX=case['poison'],EBX=flag_encode_decode(case['flags']),ECX=246,EDX=0x070400D3,ESI=value)
    return dict(program=program,address=hex(entry),max_steps=6000,registers=json.dumps(registers),memory=json.dumps(dict(regions=memory)),return_registers=','.join([*expected,'ESP','EIP'])),expected
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (read,write):
        async with ClientSession(read,write) as session:
            await session.initialize()
            for case in selected:
                args,expected=build_case(case);path=ROOT/'ghidra'/('phase27-emulation-effect-editor-'+case['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==expected
                else:
                    response=await session.call_tool('emulate_function',args);result=json.loads('\n'.join(c.text for c in response.content if c.type=='text'))
                    if response.isError or 'error' in result:raise RuntimeError(str(result))
                    if 'data' in result:result=result['data']
                    saved=dict(case=case,input=args,expected_registers=expected,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                result=saved['result'];assert result['success'] and result['hit_return'],(case['name'],result)
                assert all(int(result['registers'][r],16)==n for r,n in expected.items()),(case['name'],result,expected)
                manifest.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=case['name']));print(case['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase27-effect-editor-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
