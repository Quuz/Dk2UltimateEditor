"""Independent x86 executions for search-stack controls; not DK2 gameplay.

Small constructed callers exercise a real saved owner pointer across stdcall,
cdecl and unknown cleanup. Unicorn records actual memory reads; the static
walker must retain the same pointer field. Synthetic tests do not name a
game field or prove any original callee's complete ABI.
"""
from pathlib import Path
from functools import lru_cache
import json,struct,sys,ast,hashlib
sys.path.insert(0,str(Path(__file__).resolve().parent/'dependencies/unicorn-2.1.4'))
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_MEM_READ,UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_EDX
from scan_stack_crypt_pointers import make_cleanup_reader,make_walker
import scan_bounded_crypt_pointers as legacy
ROOT=Path(__file__).resolve().parent
BASE,OWNER,STACK=0x100000,0x200000,0x300000
STOP=BASE+0xF00

def program(kind):
    callee=BASE+0x200
    if kind=='esp':body=bytearray.fromhex('83ec10894c240c')
    else:body=bytearray.fromhex('5589e583ec10894dfc')
    body+=bytes.fromhex('6a116a22')
    if kind=='indirect':body+=b'\xba'+struct.pack('<I',callee)+bytes.fromhex('ffd2')
    else:body+=b'\xe8'+struct.pack('<i',callee-(BASE+len(body)+5))
    if kind=='cdecl':body+=bytes.fromhex('83c408')
    body+=bytes.fromhex('8b44240c' if kind=='esp' else '8b45fc')
    body+=bytes.fromhex('0fb6808a100000')
    body+=bytes.fromhex('83c410c3' if kind=='esp' else 'c9c3')
    image=bytearray([0xCC])*4096;image[:len(body)]=body
    leaf=bytes.fromhex('31c931c0c3' if kind=='cdecl' else '31c931c0c20800')
    if kind=='mixed':leaf=bytes.fromhex('85d27403c20400c20800')
    image[0x200:0x200+len(leaf)]=leaf
    return bytes(image),callee

def main():
    rows=[]
    tree=ast.parse((ROOT/'scan_bounded_crypt_pointers.py').read_text(encoding='utf-8'))
    parent=next(x for x in tree.body if isinstance(x,ast.FunctionDef) and x.name=='main')
    original_walk=next(x for x in parent.body if isinstance(x,ast.FunctionDef) and x.name=='walk')
    for kind in ['frame','esp','cdecl','indirect','mixed']:
        data,callee=program(kind);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
        @lru_cache(None)
        def ins(pc):
            if not BASE<=pc<BASE+len(data):return None
            return next(md.disasm(data[pc-BASE:pc-BASE+15],pc,count=1),None)
        cleanup=make_cleanup_reader(ins);result=cleanup(callee)
        assert result==(0 if kind=='cdecl' else None if kind=='mixed' else 8),(kind,result)
        walk,stats=make_walker(ins);scan=walk(BASE,{'ecx':0},limit=1000,span=(BASE,BASE+0x100))
        reads=[r['offset'] for r in scan['reads']]
        assert reads==[0x108A] and not scan['limited'],(kind,scan)
        scope=dict(vars(legacy),ins=ins)
        exec(compile(ast.Module(body=[original_walk],type_ignores=[]),'<frozen-phase157-walk>','exec'),scope)
        old=scope['walk'](BASE,{'ecx':0},limit=1000,span=(BASE,BASE+0x100))
        old_reads=[r['offset'] for r in old['reads']];assert old_reads==[],(kind,old)
        cpu=Uc(UC_ARCH_X86,UC_MODE_32);cpu.mem_map(BASE,4096);cpu.mem_write(BASE,data)
        cpu.mem_map(OWNER,8192);cpu.mem_write(OWNER+0x108A,b'\x37');cpu.mem_map(STACK,65536)
        cpu.reg_write(UC_X86_REG_ESP,STACK+32768);cpu.mem_write(STACK+32768,struct.pack('<I',STOP))
        cpu.reg_write(UC_X86_REG_ECX,OWNER);cpu.reg_write(UC_X86_REG_EDX,0);observed=[]
        def read(cpu,access,address,size,value,user):
            if OWNER<=address<OWNER+8192:observed.append(dict(offset=address-OWNER,size=size))
        def code(cpu,address,size,user):
            if address==STOP:cpu.emu_stop()
        cpu.hook_add(UC_HOOK_MEM_READ,read);cpu.hook_add(UC_HOOK_CODE,code)
        cpu.emu_start(BASE,STOP,count=200)
        assert observed==[dict(offset=0x108A,size=1)],(kind,observed)
        rows.append(dict(case=kind,synthetic=True,callee_cleanup=result,static_offsets=reads,legacy_static_offsets=old_reads,native_reads=observed,call_statistics=dict(stats),states=scan['states']))
    result=dict(phase=177,passed=True,scope=__doc__,controls=rows,no_gameplay_claim=True,
        legacy_scanner_sha256=hashlib.sha256((ROOT/'scan_bounded_crypt_pointers.py').read_bytes()).hexdigest(),
        stack_scanner_sha256=hashlib.sha256((ROOT/'scan_stack_crypt_pointers.py').read_bytes()).hexdigest())
    (ROOT/'phase177-stack-controls.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result))

if __name__=='__main__':main()
