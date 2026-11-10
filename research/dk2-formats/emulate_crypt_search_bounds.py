"""Original PE array-search functions: finite byte counts vs Crypt false leads.

Counts128/255 are representable adversarial fixtures, not asserted legal game
capacities. No game/virtual function, code byte, or return value is replaced.
"""
from pathlib import Path
import asyncio,json,os,struct
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import native_shot_pair_trace as native
from emulate_element_colors import region,pack32
from emulate_hero_parties import compare_code
ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
OWNER,DRIVER,SHADOW=0x10000000,0x20000000,0x21000000
STOP=0xDEADBEEF
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def cases():
    return [dict(name=f'{kind}-{n}-absent',kind=kind,count=n,match=False)
        for kind,counts in [('subarray',[0,1,128,255]),('hand',[0,1,8,255])]
        for n in counts]+[dict(name='hand-255-last',kind='hand',count=255,match=True)]

def build(c):
    owner=bytearray(8192);n=c['count'];sub=c['kind']=='subarray'
    start,count=(0xA21,0xB24) if sub else (0xB2,0x1BF)
    owner[start:start+512]=bytes.fromhex('1111')*256;owner[count]=n
    if c['match']:struct.pack_into('<H',owner,start+2*(n-1),0x2222)
    owner[0x108A:0x109C]=bytes(range(0xA0,0xB2))
    entry=0x5056E0 if sub else 0x4BCAE0;this=OWNER+(0xA21 if sub else 0)
    driver=b'\x68'+pack32(0x2222)+b'\xb9'+pack32(this)+b'\xb8'+pack32(entry)+bytes.fromhex('ffd0c3')
    stack=bytearray(0x4000);struct.pack_into('<I',stack,0x2000,STOP)
    regions=[region(OWNER,owner),region(DRIVER,driver),region(0x7FFEE000,stack)]
    args=dict(program='DKII-DX.exe',address=hex(DRIVER),registers='{}',memory=json.dumps(dict(regions=regions)),max_steps=100000,
        allowed_external_IO_code=[[DRIVER,len(driver)]],observe_reads=[[OWNER,len(owner)],[0,4096]],observe_writes=[[OWNER,len(owner)],[0,4096]])
    return args,bytes(owner),entry,start

def execute(c):
    args,owner,entry,start=build(c);tag=sha(json.dumps(args,sort_keys=True).encode())[:12]
    file=f'phase186-array-native-{c["name"]}-{tag}.json'
    if (ROOT/file).exists():saved=load(file);assert saved['input']==args
    else:
        result=native.run(EXE,args,[(OWNER,owner)],STOP,OWNER,len(owner))
        saved=dict(case=c,input=args,result=result);dump(file,saved)
    r=saved['result']
    assert r['success'] and r['hit_return'] and not r['emulation_error']
    assert not r['protected_write_or_execution_failures'] and not r['context_mismatches']
    assert r['function_or_vtable_substitutions']==0 and bytes.fromhex(r['captured_hex'])==owner
    assert not r['observed_writes']
    assert all(OWNER<=int(e['address'],16)<OWNER+len(owner) for e in r['observed_reads'])
    assert all(int(e['address'],16)+e['size']<=OWNER+0x108A for e in r['observed_reads'])
    assert r['visited_instructions'][hex(entry)]==1
    if c['kind']=='hand':assert int(r['registers']['EAX'],16)==int(c['match'])
    candidates=[e for e in r['observed_reads'] if int(e['address'],16)>=OWNER+start and e['size']==2]
    if c['count']:
        last=start+2*(c['count'] if c['kind']=='subarray' else c['count']-1)
        assert max(int(e['address'],16)-OWNER for e in candidates)==last,(c,max(int(e['address'],16)-OWNER for e in candidates),last)
    else:assert not candidates
    out=dict(case=c,file=file,sha256=sha((ROOT/file).read_bytes()),states=r['instruction_count'],
        array_reads=len(candidates),maximum_array_word_offset=None if not candidates else hex(max(int(e['address'],16)-OWNER for e in candidates)),
        counter_reads=0,owner_unchanged=True)
    return args,owner,saved,out

def cross_build(c):
    args,owner,_,_=execute(c);regions=json.loads(args['memory'])['regions']
    entry=0x5056E0 if c['kind']=='subarray' else 0x4BCAE0
    this=OWNER+(0xA21 if c['kind']=='subarray' else 0)
    stack=bytearray.fromhex(regions[2]['hex']);struct.pack_into('<II',stack,0x2000,DRIVER,0x2222)
    regions[2]=region(0x7FFEE000,stack)
    # Save original return in EDX before comparison; comparison mutates EAX/ECX.
    driver=bytes.fromhex('89c2')+compare_code(OWNER,SHADOW,len(owner))+bytes.fromhex('68efbeaddec3')
    regions[1]=region(DRIVER,driver);regions.append(region(SHADOW,owner))
    return dict(program='DKII-DX.exe',address=hex(entry),registers=json.dumps(dict(ECX=hex(this),DF='0')),
        memory=json.dumps(dict(regions=regions)),max_steps=150000,return_registers='EAX,EDX,EIP,ESP')

async def cross():
    chosen=['subarray-255-absent','hand-255-absent','hand-255-last'];rows=[]
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],
        env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for name in chosen:
                c=next(c for c in cases() if c['name']==name);args=cross_build(c);tag=sha(json.dumps(args,sort_keys=True).encode())[:12]
                file=f'ghidra/phase186-array-Ghidra-{name}-{tag}.json'
                if (ROOT/file).exists():saved=load(file);assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);dump(file,saved)
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],(name,r)
                assert int(r['registers']['EAX'],16)==1
                if c['kind']=='hand':assert int(r['registers']['EDX'],16)==int(c['match'])
                rows.append(dict(case=name,file=file,sha256=sha((ROOT/file).read_bytes()),checked_regions=1,checked_bytes=len(build(c)[1])))
                print(name,'Ghidra passed',flush=True)
    dump('phase186-array-crosschecks.json',dict(replays=rows))

def main():
    rows=[]
    for c in cases():
        _,_,_,out=execute(c);rows.append(out);print(c['name'],out['array_reads'],out['maximum_array_word_offset'],flush=True)
    dump('phase186-array-emulations.json',dict(scope=__doc__,program_sha256=sha(EXE.read_bytes()),replays=rows))
    asyncio.run(cross())

if __name__=='__main__':main()
