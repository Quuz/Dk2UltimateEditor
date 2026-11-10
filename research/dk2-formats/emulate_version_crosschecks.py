"""Old/current native CreatureSpell loaders, AP state serializers and Variables no-op wrappers.

Complete functions run with null spell resource provider, explicit AP base/I/O/
post-load fixtures. AP serializers here concern runtime state, NOT raw C5.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from emulate_placed_structures import load_abs
from native_version_crosschecks import *
from native_spell_alternatives import full_intermediate
from emulate_spell_alternatives import raw_record
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
    rows=[]
    for sha in PROFILES:
        for gate in [0,1]:
            for pair in [(0,0),(36,4),(255,255)]:
                for tps in [0,20,0xFFFFFFFF]:rows.append(dict(name=f'spell-{sha[:3]}-{gate}-{pair[0]}-{pair[1]}-{tps}',kind='spell',sha=sha,gate=gate,shot=pair[0],room=pair[1],tps=tps,tail=165))
        for seed in [0,165,255]:
            for tail in [0,255]:rows.append(dict(name=f'converter-{sha[:3]}-{seed}-{tail}',kind='converter',sha=sha,seed=seed,tail=tail))
            rows.append(dict(name=f'init-{sha[:3]}-{seed}',kind='init',sha=sha,seed=seed))
        for flags in [0,1,4,8,0x80,255]:rows.append(dict(name=f'ap-write-{sha[:3]}-{flags}',kind='ap_write',sha=sha,flags=flags))
        for flags in [0,8,255]:
            for baseok in [False,True]:rows.append(dict(name=f'ap-read-{sha[:3]}-{flags}-{int(baseok)}',kind='ap_read',sha=sha,flags=flags,baseok=baseok))
        for pattern in [0,0xA5,255]:rows.append(dict(name=f'noop-{sha[:3]}-{pattern}',kind='noop',sha=sha,pattern=pattern))
        rows.append(dict(name=f'variable-init-{sha[:3]}',kind='variable_init',sha=sha))
    for gate,alloc,seed in [(1,True,0),(1,True,165),(0,True,165),(1,False,165)]:rows.append(dict(name=f'factory-old-{gate}-{int(alloc)}-{seed}',kind='ap_factory',sha=OLD,gate=gate,alloc=alloc,seed=seed))
    return rows
def build_case(c):
    p=profile(c['sha']);old=c['sha']==OLD;mem=[];regs={};kind=c['kind']
    if kind=='spell':
        mid=bytearray(full_intermediate(raw_record(c['shot'],c['room']),0));mid[319:]=bytes([c['tail']])*27;node=bytes([165])*70;expected=spell_projection(mid,node,c['tps'],c['sha'],c['gate']);entry=p['spell_loader']
        mem=[region(0x10000000,mid),region(0x10001000,node),region(0x10002000,expected),region(0x10003000,compare_code(0x10001000,0x10002000,70)+END),region(p['tps_global'],pack32(c['tps'])),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000)+pack32(0))]
        # Same DX gate address is deliberately varied in OLD emulation too.
        mem.append(region(0x6FCDCF,pack32(c['gate'])));regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    elif kind=='init':
        entry=p['spell_init'];mem=[region(0x10001000,bytes([c['seed']])*346),region(0x10002000,bytes(346)),region(0x10003000,compare_code(0x10001000,0x10002000,346)+END),region(0x7FFF0000,pack32(0x10003000))];regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    elif kind=='converter':
        entry=p['spell_converter'];raw=raw_record(tail=c['tail']);expected=full_intermediate(raw,c['seed'])
        mem=[region(0x10000000,raw),region(0x10001000,bytes([c['seed']])*346),region(0x10002000,expected),region(0x10003000,compare_code(0x10001000,0x10002000,346)+END),region(0x7FFF0000,pack32(0x10003000)+pack32(0x10000000))];regs=dict(ECX='0x10001000');want=dict(EAX=1,ECX=0)
    elif kind=='ap_write':
        entry=p['ap_state_writer'];node=bytearray((i*29+7)&255 for i in range(64));node[0x32]=c['flags'];expected=b''.join(ap_state_segments(node,c['sha']))
        # cdecl I/O(outStatus,handle,buffer,length,readCount); capture all four local writes.
        io=bytes.fromhex('5356578b7424188b4c241cbf')+pack32(0x10002000)+b'\xA1'+pack32(0x10003000)+bytes.fromhex('01c7f3a48b44241c0105')+pack32(0x10003000)+bytes.fromhex('ff05')+pack32(0x10003004)+bytes.fromhex('8b442410c700000000005f5e5bc3')
        readout=compare_code(0x10002000,0x10004000,18)+bytes.fromhex('89c5')+compare_code(0x10000000,0x10005000,64)+load_abs(3,0x10003000)+load_abs(2,0x10003004)+END
        mem=[region(0x10000000,node),region(0x10001000,pack32(1)),region(0x10002000,bytes(18)),region(0x10003000,bytes(8)),region(0x10004000,expected),region(0x10005000,node),region(0x10006000,readout),region(0x5A8DF0 if old else 0x5B3EE0,io),region(0x4AB6D0 if old else 0x4B4B50,return_const(1,4)),region(0x7FFF0000,pack32(0x10006000)+pack32(0x10001000))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBP=1,EBX=18,EDX=4)
    elif kind=='ap_read':
        entry=p['ap_state_reader'];node=bytes((i*29+7)&255 for i in range(64));state=bytearray((i*47+5)&255 for i in range(9));state[4]=c['flags'];expected=ap_loaded_local_state(node,state,c['sha']) if c['baseok'] else node
        io=bytes.fromhex('5657be')+pack32(0x10001000)+bytes.fromhex('8b7c24148b4c2418f3a4ff05')+pack32(0x10003000)+bytes.fromhex('8b44240cc700000000005f5ec3')
        post=bytes.fromhex('ff05')+pack32(0x10003004)+bytes.fromhex('c20400')
        readout=compare_code(0x10000000,0x10002000,64)+load_abs(3,0x10003000)+load_abs(2,0x10003004)+END
        mem=[region(0x10000000,node),region(0x10001000,state),region(0x10002000,expected),region(0x10003000,bytes(8)),region(0x10004000,readout),region(0x10005000,pack32(1)),region(0x5A8DC0 if old else 0x5B3EB0,io),region(0x4AB730 if old else 0x4B4BB0,return_const(int(c['baseok']),4)),region(0x4AC0D0 if old else 0x4B5560,post),region(0x7FFF0000,pack32(0x10004000)+pack32(0x10005000))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBX=int(c['baseok']),EDX=int(c['baseok']))
    elif kind=='noop':
        entry=0x511940 if old else 0x51DC20;mid=bytes([c['pattern']])*32
        mem=[region(0x10000000,mid),region(0x10001000,mid),region(0x10002000,compare_code(0x10000000,0x10001000,32)+END),region(0x7FFF0000,pack32(0x10002000)+pack32(0x10000000))];want=dict(EAX=1,ECX=0)
    elif kind=='variable_init':
        entry=0x50E860 if old else 0x51AA90;table=0x697914 if old else 0x6A611C;callback=0x511940 if old else 0x51DC20;expected=b''.join(struct.pack('<5I',tag,callback,0,0,0) for tag in [61,62,63,64])
        mem=[region(table,bytes([165])*80),region(0x10000000,expected),region(0x10001000,compare_code(table,0x10000000,80)+END),region(0x7FFF0000,pack32(0x10001000))];want=dict(EAX=1,ECX=0)
    elif kind=='ap_factory':
        entry=0x4AEA80;node=bytes([c['seed']])*64;expected=bytearray(node);ok=bool(c['gate'] and c['alloc']);mapmodel=bytearray(0x100);struct.pack_into('<I',mapmodel,0x32,7)
        if ok:
            struct.pack_into('<I',expected,0,0x65E73C)
            for off in [6,8,10,12,14,16]:struct.pack_into('<H',expected,off,0)
            for off in [0x1A,0x1E,0x22]:struct.pack_into('<I',expected,off,0)
            expected[0x2E:0x37]=bytes(9);expected[0x37]=255;struct.pack_into('<4H',expected,0x38,3,4,8,9)
        alloc=bytes.fromhex('8b442404a3')+pack32(0x10004000)+return_const(0x10001000 if c['alloc'] else 0,4)
        readout=bytes.fromhex('89c5')+compare_code(0x10001000,0x10002000,64)+load_abs(3,0x10000032)+load_abs(2,0x10004000)+load_abs(6,0x10004004)+END
        mem=[region(0x10000000,mapmodel),region(0x10001000,node),region(0x10002000,expected),region(0x10003000,struct.pack('<4H',3,4,8,9)),region(0x10004000,bytes(8)),region(0x10005000,readout),region(0x696158,pack32(c['gate'])),region(0x54C6E0,alloc),region(0x4FC2C0,b'\xC3'),region(0x4AB590,bytes.fromhex('c20400')),region(0x7FFF0000,pack32(0x10005000)+pack32(255)+pack32(0x10003000)+pack32(0x10003004)+pack32(0x10004004))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBP=int(ok),EBX=7+int(ok),EDX=64 if c['gate'] else 0,ESI=0x10001000 if ok else 0)
    else:raise AssertionError(c)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=p['program'],address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase52-emulation-versions-v2-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase52-version-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
