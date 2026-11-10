"""Full old/DX Door initializer/converter/loader withnullvisualprovider.

Nativeboundedstrncpy executes;audio lookup is an explicitcapture/tokenprovider.
All616raw/732intermediate/181definitionbytescompared. No modifiedgameprocess.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
from native_door_loading import *
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3';TOKEN=0xA1B2C3D4

def cases():
    rows=[]
    for sha in PROFILES:
        for seed in [0,165,255]:rows.append(dict(name=f'init-{sha[:3]}-{seed}',kind='init',sha=sha,seed=seed))
        for seed in [0,165,255]:
            for terminated in [False,True]:rows.append(dict(name=f'convert-{sha[:3]}-{seed}-{int(terminated)}',kind='converter',sha=sha,seed=seed,terminated=terminated))
        for value in [0,1,32768,65535]:rows.append(dict(name=f'load-{sha[:3]}-word-{value}',kind='loader',sha=sha,value=value,gate=0))
        for pres in [0,2,0xFFFFFFFF]:rows.append(dict(name=f'load-{sha[:3]}-presence-{pres}',kind='loader',sha=sha,value=65535,gate=1,presence=pres))
        rows.append(dict(name=f'load-{sha[:3]}-allmissing',kind='loader',sha=sha,gate=1,allmissing=True))
        for gap in [0,255,'pattern']:
            for gate in [0,1]:rows.append(dict(name=f'chain-{sha[:3]}-gap-{gap}-gate-{gate}',kind='chain',sha=sha,seed=165,gap=gap,gate=gate))
        for value in [255,65280]:rows.append(dict(name=f'chain-{sha[:3]}-sum-collision-{value}',kind='chain',sha=sha,value=value,gate=0))
        for research in [0,65535]:
            for gate in [0,1]:rows.append(dict(name=f'chain-{sha[:3]}-research-{research}-gate-{gate}',kind='chain',sha=sha,research=research,gate=gate))
    return rows

def raw_record(c):
    raw=bytearray((i*47+13)&255 for i in range(616));struct.pack_into('<H',raw,0x236,c.get('value',0x1234));struct.pack_into('<I',raw,0x228,0x11223344)
    for off,n in [(0x21E,2),(0x220,4),(0x230,2)]:raw[off:off+n]=bytes((j*73+19)&255 for j in range(n)) if c.get('gap',0)=='pattern' else bytes([c.get('gap',0)])*n
    struct.pack_into('<H',raw,0x224,c.get('research',1234))
    if c.get('terminated',True):raw[:32]=b'Door\0'+bytes([173])*27;raw[0x248:0x268]=b'Sound\0'+bytes([191])*26
    else:raw[:32]=b'ABCDEFGHIJKLMNOPQRSTUVWXYZ123456';raw[0x248:0x268]=b'abcdefghijklmnopqrstuvwxyz123456'
    return bytes(raw)

def build_case(c):
    p=profile(c['sha']);kind=c['kind'];raw=raw_record(c);prior=bytes([c.get('seed',165)])*732;mid=bytearray(intermediate(raw,bytes(732) if kind=='chain' else prior));gate=c.get('gate',0)
    if kind=='init':mid=bytearray(732)
    if kind=='loader':
        if c.get('allmissing'):mid[:0x74]=bytes(0x74)
        if 'presence' in c:struct.pack_into('<I',mid,0x38,c['presence'])
    previous=bytes((i*29+7)&255 for i in range(181));expected=definition(mid,previous,c['sha'],gate,TOKEN) if kind in ['loader','chain'] else previous
    called=kind in ['loader','chain'] and sound_requested(mid,c['sha'],gate);capture_expected=pack32(1)+pack32(p['sound_prefix'])+string32(bytes(mid[0x2BC:0x2DC])) if called else bytes(40)
    bufprior=bytes([165])*32;bufexpected=string32(bytes(mid[0x2BC:0x2DC])) if called else bufprior
    sound=bytes.fromhex('56578b44240ca3')+pack32(0x10006304)+bytes.fromhex('8b742410bf')+pack32(0x10006308)+b'\xB9'+pack32(32)+bytes.fromhex('f3a4ff05')+pack32(0x10006300)+b'\xB8'+pack32(TOKEN)+bytes.fromhex('5f5ec20800')
    vt=bytearray(32);struct.pack_into('<I',vt,0x1C,0x10006200)
    readout=b'\xBD'+pack32(1)
    for a,b,n in [(0x10000000,0x10001000,616),(0x10002000,0x10003000,732),(0x10004000,0x10005000,181),(0x10006300,0x10006400,40),(p['sound_buffer'],0x10006500,32)]:readout+=compare_code(a,b,n)+bytes.fromhex('21c5')
    readout+=END
    mem=[region(0x10000000,raw),region(0x10001000,raw),region(0x10002000,prior if kind in ['converter','init','chain'] else mid),region(0x10003000,mid),region(0x10004000,previous),region(0x10005000,expected),region(0x10006000,pack32(0x10006100)),region(0x10006100,vt),region(0x10006200,sound),region(0x10006300,bytes(40)),region(0x10006400,capture_expected),region(0x10006500,bufexpected),region(0x10008000,readout),region(p['sound_global'],pack32(0x10006000)),region(p['sound_buffer'],bufprior),region(0x6FCDCF,pack32(gate))]
    if kind=='loader':entry=p['loader'];regs=dict(ECX='0x10004000');stack=pack32(0x10008000)+pack32(0x10002000)+pack32(0)
    elif kind=='converter':entry=p['converter'];regs=dict(ECX='0x10002000');stack=pack32(0x10008000)+pack32(0x10000000)
    elif kind=='init':entry=p['init'];regs=dict(ECX='0x10002000');stack=pack32(0x10008000)
    else:
        entry=p['init'];regs=dict(ECX='0x10002000');stack=pack32(0x10007000)
        driver=b'\xB9'+pack32(0x10002000)+b'\x68'+pack32(0x10000000)+b'\xB8'+pack32(p['converter'])+bytes.fromhex('ffd0b9')+pack32(0x10004000)+bytes.fromhex('6a0068')+pack32(0x10002000)+b'\xB8'+pack32(p['loader'])+bytes.fromhex('ffd068')+pack32(0x10008000)+b'\xC3'
        mem.append(region(0x10007000,driver))
    mem.append(region(0x7FFF0000,stack));spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=p['program'],address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as s:
            await s.initialize()
            for c in selected:
                args,want=build_case(c);path=ROOT/'ghidra'/('phase67-emulation-door-loading-'+c['name']+'.json')
                if path.exists():saved=json.loads(path.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await s.call_tool('emulate_function',args);r=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    saved=dict(case=c,input=args,expected_registers=want,result=r.get('data',r));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase67-door-loading-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
