"""Original pointer-return executions plus synthetic search regression controls.

Synthetic code checks the analyser only. Original PE scenes establish two
arithmetic return contracts and bounded writes, not unknown Crypt meanings.
"""
from pathlib import Path
from functools import lru_cache
import asyncio,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
import scan_deep_crypt_pointers as old
import scan_edx_crypt_pointers as edx
import scan_crypt_verified_returns as extended
import native_shot_pair_trace as native
from emulate_element_colors import region
ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
sha=edx.sha
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
d=lambda n:struct.pack('<I',n)
OWNER,DRIVER,STACK,STOP=0x10000000,0x20000000,0x7FFEE000,0xDEADBEEF


def synthetic():
    sys.path.insert(0,str(ROOT/'dependencies/unicorn-2.1.4'))
    from unicorn import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_MEM_READ
    from unicorn.x86_const import UC_X86_REG_ECX,UC_X86_REG_ESP
    base,owner,stack,stop=0x100000,0x200000,0x300000,0x100F00
    leaf,clobber=base+0x200,base+0x300
    rows=[]
    for kind in ['EDX','EDX-adjusted','EDX-clobbered','return-leaf','return-clobbered']:
        body=bytearray()
        def call(target):body.extend(b'\xe8'+struct.pack('<i',target-(base+len(body)+5)))
        if kind.startswith('EDX'):
            body+=bytes.fromhex('89ca31c9') if kind!='EDX-adjusted' else bytes.fromhex('8d918a10000031c9')
            if kind=='EDX-clobbered':call(clobber)
            call(leaf);body+=b'\xc3'
            leafcode=bytes.fromhex('0fb602c3') if kind=='EDX-adjusted' else bytes.fromhex('0fb6828a100000c3')
        else:
            call(leaf)
            if kind=='return-clobbered':call(clobber)
            body+=bytes.fromhex('0fb68047050000c3')
            leafcode=bytes.fromhex('8d81430b0000c3')
        image=bytearray([0xCC])*4096;image[:len(body)]=body
        image[0x200:0x200+len(leafcode)]=leafcode;image[0x300:0x305]=bytes.fromhex('31d231c0c3')
        md=old.Cs(old.CS_ARCH_X86,old.CS_MODE_32);md.detail=True
        @lru_cache(None)
        def ins(pc):
            if not base<=pc<base+len(image):return None
            return next(md.disasm(bytes(image[pc-base:pc-base+15]),pc,count=1),None)
        def fields(factory):
            walk,_=factory(ins);root=walk(base,{'ecx':0},span=(base,base+0x100),limit=1000)
            reads=list(root['reads'])
            for callrow in root['calls']:
                q=walk(callrow['target'],callrow['registers'],span=(callrow['target'],callrow['target']+256),initial_stack=callrow['arguments'],limit=1000)
                assert not q['limited'];reads.extend(q['reads'])
            assert not root['limited']
            return sorted(set(r['offset'] for r in reads))
        before=fields(old.make_walker);edxonly=fields(edx.make_walker)
        after=fields(lambda ins:extended.make_walker(ins,contracts={leaf:0xB43} if kind.startswith('return') else {}))
        cpu=Uc(UC_ARCH_X86,UC_MODE_32);cpu.mem_map(base,4096);cpu.mem_write(base,bytes(image))
        cpu.mem_map(0,8192);cpu.mem_map(owner,8192);cpu.mem_write(owner+0x108A,b'\x55')
        cpu.mem_map(stack,65536);cpu.mem_write(stack+32768,d(stop));cpu.reg_write(UC_X86_REG_ESP,stack+32768);cpu.reg_write(UC_X86_REG_ECX,owner)
        observed=[]
        def onread(cpu,access,address,size,value,user):
            if owner<=address<owner+8192:observed.append(dict(offset=address-owner,bytes=size))
        cpu.hook_add(UC_HOOK_MEM_READ,onread);cpu.emu_start(base,stop,count=200)
        positive=kind in ['EDX','EDX-adjusted','return-leaf']
        assert before==[] and after==([0x108A] if positive else []),(kind,before,after)
        assert observed==([dict(offset=0x108A,bytes=1)] if positive else []),(kind,observed)
        assert edxonly==([0x108A] if kind in ['EDX','EDX-adjusted'] else [])
        rows.append(dict(case=kind,synthetic=True,old_offsets=before,EDX_offsets=edxonly,new_offsets=after,native_reads=observed,image_sha256=sha(image)))
    return rows


def cases():
    return [dict(name='getter',entry=0x4C5F50),dict(name='setter-all',entry=0x4DC7E0,match='all'),
            dict(name='setter-alternate',entry=0x4DC7E0,match='alternate'),dict(name='setter-none',entry=0x4DC7E0,match='none')]


def original_build(c):
    owner=bytearray([0xA5])*8192;owner[0x108A:0x109C]=bytes(range(18));expected=bytearray(owner)
    if c['entry']==0x4DC7E0:
        for i in range(46):
            owner[0xB57+18*i]=0x37 if c['match']=='all' or (c['match']=='alternate' and i%2==0) else 0x38
        expected=bytearray(owner);expected[0xB47]=0x37
        for i in range(46):
            if owner[0xB57+18*i]==0x37:expected[0xB65+18*i:0xB69+18*i]=d(0xA000)
    driver=bytearray(b'\xb9'+d(OWNER))
    if c['entry']==0x4DC7E0:driver+=b'\x68'+d(0x37)
    driver+=b'\xb8'+d(c['entry'])+bytes.fromhex('ffd0c3')
    stack=bytearray(0x4000);stack[0x2000:0x2004]=d(STOP)
    args=dict(program='DKII-DX.exe',address=hex(DRIVER),registers='{}',
              memory=json.dumps(dict(regions=[region(OWNER,owner),region(DRIVER,driver),region(STACK,stack)])),max_steps=10000,
              allowed_external_IO_code=[[DRIVER,len(driver)]],observe_reads=[[OWNER,len(owner)]],observe_writes=[[OWNER,len(owner)]])
    return args,bytes(owner),bytes(expected)


def execute(c):
    args,owner,expected=original_build(c);tag=sha(json.dumps(args,sort_keys=True).encode()+expected)[:12]
    file=f'ghidra/phase191-pointer-native-{c["name"]}-{tag}.json'
    if (ROOT/file).exists():saved=load(file);assert saved['input']==args
    else:
        result=native.run(EXE,args,[(OWNER,expected)],STOP,OWNER,len(owner));saved=dict(case=c,input=args,result=result);dump(file,saved)
    r=saved['result'];assert r['success'] and r['hit_return'] and not r['emulation_error']
    assert not r['protected_write_or_execution_failures'] and not r['context_mismatches']
    assert r['function_or_vtable_substitutions']==0 and bytes.fromhex(r['captured_hex'])==expected
    assert int(r['registers']['EAX'],16)==OWNER+extended.CONTRACTS[c['entry']]
    assert not any(OWNER+0x108A<=int(e['address'],16)<OWNER+0x109C for e in r['observed_reads']+r['observed_writes'])
    return args,saved,dict(case=c,file=file,sha256=sha((ROOT/file).read_bytes()),return_offset=extended.CONTRACTS[c['entry']],
                            expected_owner_sha256=sha(expected),exact_expected_owner=True,counter_accesses=0,original_instructions=r['instruction_count'])


async def cross():
    rows=[]
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in cases():
                args,native_saved,out=execute(c);_,_,expected=original_build(c)
                memory=json.loads(args['memory']);stack=bytearray.fromhex(memory['regions'][2]['hex']);stack[0x2000:0x2008]=d(STOP)+d(0x37)
                memory['regions'][2]=region(STACK,stack)
                args=dict(program='DKII-DX.exe',address=hex(c['entry']),registers=json.dumps(dict(ECX=hex(OWNER))),memory=json.dumps(memory),max_steps=10000,return_registers='EAX,ECX,ESP')
                tag=sha(json.dumps(args,sort_keys=True).encode())[:12];file=f'ghidra/phase191-pointer-Ghidra-{c["name"]}-{tag}.json'
                if (ROOT/file).exists():saved=load(file);assert saved['input']==args
                else:
                    response=await session.call_tool('emulate_function',args)
                    value=json.loads('\n'.join(x.text for x in response.content if x.type=='text'))
                    saved=dict(input=args,result=value.get('data',value),tool_is_error=response.isError);dump(file,saved)
                r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'],r
                assert int(r['registers']['EAX'],16)==OWNER+extended.CONTRACTS[c['entry']]
                # This independent control checks the return, not the whole owner.
                assert int(r['registers']['ECX'],16)==(OWNER if c['entry']==0x4C5F50 else 0)
                rows.append(dict(case=c,file=file,sha256=sha((ROOT/file).read_bytes()),checked_registers=['EAX','ECX']))
                print('Ghidra pointer return',c['name'],'passed',flush=True)
    return rows


def main():
    assert sha(EXE.read_bytes())==edx.EXPECTED
    controls=synthetic();original=[]
    for c in cases():
        _,_,out=execute(c);original.append(out);print('original',c['name'],'passed',flush=True)
    ghidra=asyncio.run(cross())
    dump('phase191-pointer-controls.json',dict(phase=191,synthetic_controls=controls,original_PE_scenes=original,independent_Ghidra_register_controls=ghidra,
        scope='Five constructed controls establish analyser behaviour. Four original PE scenes check two pointer-return contracts and exact8192-byte outputs; Ghidra independently checks EAX/ECX only. No new Crypt semantics.'))


if __name__=='__main__':main()
