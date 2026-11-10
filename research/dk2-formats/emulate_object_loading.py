"""Full Object init/conversion/null-resource loading; audio capture provider only."""
from pathlib import Path
import asyncio
import hashlib
import json
import os
import struct
import sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from native_object_loading import *
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code

ROOT = Path(__file__).resolve().parent
TOKEN = 0xA1B2C3D4
END = b'\x68'+pack32(0xDEADBEEF)+b'\xC3'


def cases():
    rows = []
    for sha in PROFILES:
        tag = sha[:3]
        for seed in [0,165,255]:
            rows.append(dict(name=f'init-{tag}-{seed}',kind='init',sha=sha,seed=seed))
        for seed in [0,165,255]:
            for terminated in [False,True]:
                rows.append(dict(name=f'convert-{tag}-{seed}-{int(terminated)}',kind='converter',sha=sha,seed=seed,terminated=terminated))
        for value in [0,1,32768,65535]:
            rows.append(dict(name=f'load-{tag}-unknown-{value}',kind='loader',sha=sha,unknown=value,gate=0))
        for presence in [0,2,0xFFFFFFFF]:
            rows.append(dict(name=f'load-{tag}-presence-{presence}',kind='loader',sha=sha,presence=presence,gate=1))
        rows.append(dict(name=f'load-{tag}-allmissing',kind='loader',sha=sha,allmissing=True,gate=1))
        for pad in [0,255,'pattern']:
            for gate in [0,1]:
                rows.append(dict(name=f'chain-{tag}-pad-{pad}-gate-{gate}',kind='chain',sha=sha,pad=pad,gate=gate))
        for flags in [0,0x80000000]:
            for color in ['00000000','01000000','00010000','00000100','000000ff']:
                rows.append(dict(name=f'chain-{tag}-light-{flags}-{color}',kind='chain',sha=sha,light_flags=flags,color=color,gate=0))
        rows.append(dict(name=f'chain-{tag}-light-black-flag1',kind='chain',sha=sha,light_flags=1,color='00000000',gate=1))
        for speed in [0,1,19,20,65535,1310720,1310740,0xFFFFFFFF]:
            rows.append(dict(name=f'chain-{tag}-speed-{speed}',kind='chain',sha=sha,speed=speed,tps=20,gate=0))
        rows.append(dict(name=f'load-{tag}-speed-absent-tps0',kind='loader',sha=sha,no_speed=True,tps=0,gate=1))
        for speed in [0,1]:
            rows.append(dict(name=f'fault-{tag}-tps0-speed-{speed}',kind='fault',sha=sha,speed=speed,tps=0,gate=1))
    return rows


def raw_record(c):
    raw = bytearray((i*47+13)&255 for i in range(894))
    struct.pack_into('<H',raw,0x34C,c.get('unknown',0x1234))
    struct.pack_into('<I',raw,0x344,0x11223344)
    struct.pack_into('<I',raw,0x338,c.get('speed',8192))
    struct.pack_into('<I',raw,0x324,c.get('light_flags',0x80000000))
    raw[0x328:0x32C] = bytes.fromhex(c.get('color','010203a7'))
    pad = c.get('pad',0)
    raw[0x341:0x344] = bytes([19,92,165]) if pad=='pattern' else bytes([pad])*3
    if c.get('terminated',True):
        raw[:32] = b'Object\0'+bytes([173])*25
        raw[0x35E:0x37E] = b'Sound\0'+bytes([191])*26
    else:
        raw[:32] = b'ABCDEFGHIJKLMNOPQRSTUVWXYZ123456'
        raw[0x35E:0x37E] = b'abcdefghijklmnopqrstuvwxyz123456'
    return bytes(raw)


def build_case(c):
    p = profile(c['sha'])
    kind = c['kind']
    raw = raw_record(c)
    prior = bytes([c.get('seed',165)])*1022
    mid = bytearray(intermediate(raw,bytes(1022) if kind=='chain' else prior))
    if kind=='init':
        mid = bytearray(1022)
    if c.get('allmissing'):
        mid[:128] = bytes(128)
    if 'presence' in c:
        struct.pack_into('<I',mid,0x50,c['presence'])
    if c.get('no_speed'):
        struct.pack_into('<I',mid,0x44,0)
    previous = bytes((i*29+7)&255 for i in range(285))
    gate,tps = c.get('gate',0),c.get('tps',20)
    expected = definition(mid,previous,c['sha'],tps,gate,TOKEN) if kind in ['loader','chain'] else previous
    called = kind in ['loader','chain'] and sound_requested(mid,c['sha'],gate)
    capture_expected = pack32(1)+pack32(p['sound_prefix'])+string32(bytes(mid[0x3DE:0x3FE])) if called else bytes(40)
    buffer_prior = bytes([165])*32
    buffer_expected = string32(bytes(mid[0x3DE:0x3FE])) if called else buffer_prior
    audio = bytes.fromhex('56578b44240ca3')+pack32(0x10006304)+bytes.fromhex('8b742410bf')+pack32(0x10006308)+b'\xB9'+pack32(32)+bytes.fromhex('f3a4ff05')+pack32(0x10006300)+b'\xB8'+pack32(TOKEN)+bytes.fromhex('5f5ec20800')
    vt = bytearray(32)
    struct.pack_into('<I',vt,0x1C,0x10006200)
    observer = b'\xBD'+pack32(1)
    for a,b,n in [(0x10000000,0x10001000,894),(0x10002000,0x10003000,1022),(0x10004000,0x10005000,285),(0x10006300,0x10006400,40),(p['sound_buffer'],0x10006500,32)]:
        observer += compare_code(a,b,n)+bytes.fromhex('21c5')
    observer += END
    mem = [region(0x10000000,raw),region(0x10001000,raw),region(0x10002000,prior if kind in ['init','converter','chain'] else mid),
           region(0x10003000,mid),region(0x10004000,previous),region(0x10005000,expected),
           region(0x10006000,pack32(0x10006100)),region(0x10006100,vt),region(0x10006200,audio),
           region(0x10006300,bytes(40)),region(0x10006400,capture_expected),region(0x10006500,buffer_expected),
           region(0x10008000,observer),region(p['sound_global'],pack32(0x10006000)),region(p['sound_buffer'],buffer_prior),
           region(p['tps'],pack32(tps)),region(0x6FCDCF,pack32(gate))]
    if kind in ['loader','fault']:
        entry = p['loader'];regs = dict(ECX='0x10004000')
        stack = pack32(0x10008000)+pack32(0x10002000)+pack32(0)
    elif kind=='converter':
        entry = p['converter'];regs = dict(ECX='0x10002000')
        stack = pack32(0x10008000)+pack32(0x10000000)
    elif kind=='init':
        entry = p['init'];regs = dict(ECX='0x10002000');stack = pack32(0x10008000)
    else:
        entry = p['init'];regs = dict(ECX='0x10002000');stack = pack32(0x10007000)
        driver = b'\xB9'+pack32(0x10002000)+b'\x68'+pack32(0x10000000)+b'\xB8'+pack32(p['converter'])+bytes.fromhex('ffd0b9')+pack32(0x10004000)+bytes.fromhex('6a0068')+pack32(0x10002000)+b'\xB8'+pack32(p['loader'])+bytes.fromhex('ffd068')+pack32(0x10008000)+b'\xC3'
        mem.append(region(0x10007000,driver))
    mem.append(region(0x7FFF0000,stack))
    spans = sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=p['program'],address=hex(entry),max_steps=30000,registers=json.dumps(regs),
                memory=json.dumps(dict(regions=mem)),return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1)


async def main():
    selected = cases()
    if len(sys.argv)>1:
        selected = [c for c in selected if c['name']==sys.argv[1]]
        assert selected
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want = build_case(c)
                path = ROOT/'ghidra'/f'phase71-emulation-object-loading-{c["name"]}.json'
                if path.exists():
                    saved = json.loads(path.read_text())
                    assert saved['input']==args and saved['expected_registers']==want
                else:
                    response = await session.call_tool('emulate_function',args)
                    text = '\n'.join(x.text for x in response.content if x.type=='text')
                    try:
                        r = json.loads(text)
                    except ValueError:
                        r = dict(error=text)
                    saved = dict(case=c,input=args,expected_registers=want,result=r.get('data',r),tool_is_error=response.isError)
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r = saved['result']
                if c['kind']=='fault':
                    expected_pc = profile(c['sha'])['divide_address']
                    assert r.get('success') is False and not r.get('hit_return') and int(r['final_pc'],16)==expected_pc and r.get('emulation_error') in ['Remainder by 0','Divide by 0','Division by zero'],(c['name'],r)
                else:
                    assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r,want)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name'],expected_fault=c['kind']=='fault'))
                print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase71-object-loading-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')


if __name__=='__main__':
    asyncio.run(main())
