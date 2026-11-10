"""Whole original495080: low-byte stat guard,512 bounded native inputs."""
from pathlib import Path
import json,struct,sys
import pefile
from build_effect_semantics import EXE,pe_read
from audit_targeted_spell import executable_ranges
from emulate_editor_variables_writer import sha
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'dependencies/unicorn-2.1.4'))
import unicorn
from unicorn import x86_const as xc
BASE,STACK,STOP=0x6F2550,0x7FFF0000,0xDEADBEEF
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,x:(ROOT/n).write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf8')


def table():
    raw=bytearray([165])*4096
    for i in range(32):struct.pack_into('<H',raw,0x313+2*i,(100+i if i!=31 else -7)&65535)
    return bytes(raw)


def main():
    exe=EXE.read_bytes();pe=pefile.PE(data=exe);image=pe.get_memory_mapped_image();uc=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32)
    uc.mem_map(0x400000,(len(image)+4095)&~4095);uc.mem_write(0x400000,image)
    uc.mem_map(STACK-8192,16384);uc.mem_write(BASE,table());uc.mem_write(0x6E4154,struct.pack('<I',BASE))
    ranges=executable_ranges(exe);reads=[];writes=[];visits={}
    def code(cpu,a,size,_):
        assert any(p<=a<p+n for p,n in ranges)
        visits[a]=visits.get(a,0)+1
    def read(cpu,access,a,size,value,_):
        if BASE<=a<BASE+4096:reads.append(dict(pc=cpu.reg_read(xc.UC_X86_REG_EIP),address=a,bytes=size))
    def write(cpu,access,a,size,value,_):
        if any(a+n>p and a<p+n2 for p,n2 in ranges for n in [size]):raise AssertionError('originalcodewrite')
        if BASE<=a<BASE+4096:writes.append((a,size,value))
    uc.hook_add(unicorn.UC_HOOK_CODE,code);uc.hook_add(unicorn.UC_HOOK_MEM_READ,read);uc.hook_add(unicorn.UC_HOOK_MEM_WRITE,write)
    cases=[]
    for high in [0,0xA5A50000]:
        for index in range(256):
            arg=high|index;before=len(reads)
            for name in ['EAX','EBX','ECX','EDX','ESI','EDI']:uc.reg_write(getattr(xc,'UC_X86_REG_'+name),0)
            uc.reg_write(xc.UC_X86_REG_ESP,STACK);uc.reg_write(xc.UC_X86_REG_EFLAGS,2);uc.mem_write(STACK,struct.pack('<2I',STOP,arg))
            uc.emu_start(0x495080,STOP,count=1000);assert uc.reg_read(xc.UC_X86_REG_EIP)==STOP
            expected=(100+index if index<31 else -7) if index<32 else 100
            result=uc.reg_read(xc.UC_X86_REG_EAX);assert result==expected&0xffffffff
            new=reads[before:];assert len(new)==int(index<32)
            if new:assert new==[dict(pc=0x495093,address=BASE+0x313+2*index,bytes=2)]
            cases.append(dict(argument=arg,low_byte=index,result_bits=result,coefficient_reads=new))
    assert not writes and bytes(uc.mem_read(BASE,4096))==table()
    result=dict(profile=dict(file=str(EXE),sha256=sha(exe)),entry=0x495080,native_calls=len(cases),native_instructions=sum(visits.values()),visited_original_PCs=sorted(visits),
        cases=cases,whole_prepared_table_preserved=True,unknown29_186_reads=0,code_substitutions=0,scope=__doc__)
    dump('phase200-stat-getter-native.json',result)
    q=[]
    for arg in [0,31,32,131,0xA5A5001F]:
        regions=[dict(address=hex(BASE),hex=table().hex()),dict(address='0x6e4154',hex=struct.pack('<I',BASE).hex()),dict(address=hex(STACK-8192),hex=(bytes(8192)+struct.pack('<2I',STOP,arg)+bytes(8184)).hex())]
        q.append(dict(id=f'phase200-stat-getter-Ghidra-{arg:x}',tool='emulate_function',args=dict(program='DKII-DX.exe',address='0x495080',registers=json.dumps(dict(ESP=hex(STACK),DF='0')),memory=json.dumps(dict(regions=regions)),max_steps=1000,return_registers='EAX,EIP,ESP'),print_limit=0))
    dump('phase200-stat-getter-queries.json',q);print('PASS native getter',len(cases),'calls;',sum(visits.values()),'instructions;',len(reads),'coefficient reads')


if __name__=='__main__':main()
