"""Full91-byte kind/owner filters; source list/table bytes remain unchanged."""
from pathlib import Path
import asyncio
import hashlib
import json
import os
import struct
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from native_entity_kind_filter import *
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code

ROOT = Path(__file__).resolve().parent
PRIOR = 0xCAFEBABE


def cases():
    # Each tuple is tag,kind,owner,next. Expected native output is independently
    # designated by tag, and is not derived from the projection under test.
    variants = [
        ('empty',0,[],3,3,None),
        ('first',1,[(1,3,3,2),(2,3,3,0)],3,3,1),
        ('later',1,[(1,4,3,2),(2,3,2,3),(3,3,3,0)],3,3,3),
        ('no-kind',1,[(1,4,3,2),(2,2,3,0)],3,3,None),
        ('no-owner',1,[(1,3,2,2),(2,3,4,0)],3,3,None),
        ('owner-high',1,[(1,3,3,0)],3,0x12340003,1),
        ('owner-ffff',1,[(1,3,65535,0)],3,0xFFFFFFFF,1),
        ('kind-high',1,[(1,3,3,0)],0x10003,3,None),
        ('kind-zero',1,[(1,0,3,0)],0,3,1),
        ('door-kind',1,[(1,4,3,0)],4,3,1),
        ('chain-order',100,[(100,3,3,1),(1,3,3,50),(50,3,3,0)],3,3,100),
        ('null-end',1,[(1,4,3,0),(2,3,3,0)],3,3,None),
        ('head-zero-valid',0,[(0,3,3,0)],3,3,0),
        ('tag-ffff',65535,[(65535,3,3,0)],3,3,65535),
        ('first-head-only',1,[(1,4,3,0),(2,3,3,0)],3,3,None),
        ('no-owner-wildcard',1,[(1,3,3,0)],3,0xFFFFFFFF,None),
    ]
    out = []
    for sha in PROFILES:
        for name,head,rows,kind,owner,wanted in variants:
            out.append(dict(name=f'{sha[:3]}-{name}',sha=sha,head=head,rows=rows,kind=kind,owner=owner,wanted_tag=wanted))
    return out


def build_case(c):
    p = PROFILES[c['sha']]
    entries = {}
    originals = {0x10000000:struct.pack('<HH',c['head'],2),p['table']:pack32(0)}
    for i,(tag,kind,owner,nxt) in enumerate(c['rows']):
        pointer = 0x10003000+i*0x100
        raw = bytearray((j*37+11)&255 for j in range(64))
        struct.pack_into('<H',raw,0xC,nxt)
        struct.pack_into('<I',raw,0x12,kind)
        struct.pack_into('<H',raw,0x28,owner)
        entries[tag] = dict(pointer=pointer,kind=kind,owner=owner,next=nxt)
        originals[p['table']+4*tag] = pack32(pointer)
        originals[pointer] = bytes(raw)
    wanted = (0,PRIOR) if c['wanted_tag'] is None else (1,entries[c['wanted_tag']]['pointer'])
    assert first_match(c['head'],entries,c['kind'],c['owner'],PRIOR) == wanted
    originals[0x10001000] = pack32(wanted[1])
    originals[0x10002000] = pack32(wanted[0])
    observer = b'\xA3'+pack32(0x10002000)+b'\xBD'+pack32(1)
    mem = []
    for i,(address,expected) in enumerate(originals.items()):
        target = 0x10100000+i*0x100
        actual = pack32(PRIOR) if address == 0x10001000 else bytes(4) if address == 0x10002000 else expected
        mem += [region(address,actual),region(target,expected)]
        observer += compare_code(address,target,len(expected))+bytes.fromhex('21c5')
    observer += b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    mem += [region(0x100F0000,observer),region(0x7FFF0000,pack32(0x100F0000)+pack32(c['kind'])+pack32(c['owner'])+pack32(0x10001000))]
    spans = sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem)
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(program=p['program'],address=hex(p['entry']),max_steps=8000,
                registers=json.dumps(dict(ECX='0x10000000')),memory=json.dumps(dict(regions=mem)),
                return_registers='EAX,ECX,EBP,ESP,EIP'),dict(EAX=1,ECX=0,EBP=1),wanted


async def main():
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in cases():
                args,want,native = build_case(c)
                path = ROOT/'ghidra'/f'phase69-emulation-entity-kind-filter-{c["name"]}.json'
                if path.exists():
                    saved = json.loads(path.read_text())
                    assert saved['input'] == args and saved['expected_native'] == list(native)
                else:
                    response = await session.call_tool('emulate_function',args)
                    r = json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    if response.isError or 'error' in r:
                        raise RuntimeError(str(r))
                    saved = dict(case=c,input=args,expected_registers=want,expected_native=native,result=r.get('data',r))
                    path.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r = saved['result']
                assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items()),(c['name'],r)
                rows.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),case=c['name']))
                print(c['name'],'verified',flush=True)
    (ROOT/'phase69-entity-kind-filter-emulations.json').write_text(json.dumps(dict(cases=rows,scope=__doc__),indent=2),encoding='utf8')


if __name__=='__main__':
    asyncio.run(main())
