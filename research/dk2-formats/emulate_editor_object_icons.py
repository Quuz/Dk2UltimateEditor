"""Original Pro converter, atoi and atlas getter as separate bounded executions.

MFC resource construction/string allocation and full renderer/GUI are not run.
The atlas fixture's parsed index is explicit, not claimed to be a native full
raw-to-MFC chain. No original instructions, imports or vtables are replaced.
"""
from pathlib import Path
import hashlib, json, struct, sys
import pefile
import dk2_formats as fmt
import native_object_loading as loading
import native_editor_object_icons as api
from audit_targeted_spell import executable_ranges

ROOT = Path(__file__).resolve().parent
EXE = ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
SOURCE = ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Objects.kwd'
RAW,MID,MODEL,CATALOG,ARRAY,OUT = [0x12000000+i*0x10000 for i in range(6)]
STACK,STOP = 0x7FFF0000,0x13000000
sys.path.insert(0,str(ROOT/'dependencies/unicorn-2.1.4'))
import unicorn
from unicorn import Uc, UcError
from unicorn import x86_const as xc

def sha(b):return hashlib.sha256(b).hexdigest()
def pack(v):return struct.pack('<I',v)
def region(a,b):return dict(address=hex(a),hex=bytes(b).hex())

def build(kind, value=2, columns=4):
    rows=[r for s in fmt.parse(SOURCE.read_bytes()) if s.signature==0xF0 for r in s.records]
    raw=next(r for r in rows if r[0x35A]==1)
    if kind=='converter':
        raw=api.patch_icon_index(raw,value)
        memory={RAW:raw+bytes([165])*64,MID:bytes([165])*(1022+64)}
        expected=loading.intermediate(raw,bytes([165])*1022)+bytes([165])*64
        entry,ecx,stack=0x42D570,MID,[RAW]
        wanted={RAW:memory[RAW],MID:expected}
    elif kind=='atoi':
        suffix=str(value).encode('ascii')+b'\0'
        memory={RAW:suffix+bytes([165])*64}
        entry,ecx,stack=0x4ACE5A,0,[RAW]
        wanted=dict(memory)
    elif kind=='atlas':
        model=bytearray([165])*(0x438+64)
        model[:4]=pack(0x50AFFC);model[0x168]=1
        struct.pack_into('<H',model,0xC4,value)
        catalog=bytearray(0x600+64)
        catalog[0x528:0x52C]=pack(ARRAY);catalog[0x52C:0x530]=pack(1)
        catalog[0x554:0x558]=pack(columns)
        memory={MODEL:bytes(model),CATALOG:bytes(catalog),ARRAY:pack(MODEL),OUT:bytes([165])*80}
        entry,ecx,stack=0x49E960,CATALOG,[OUT,1,3]
        wanted=dict(memory);wanted[OUT]=struct.pack('<4I',*api.icon_rect(value,columns))+bytes([165])*64
    else:raise ValueError(kind)
    return dict(kind=kind,value=value,columns=columns,entry=entry,ecx=ecx,stack=stack,
        memory=[region(a,b) for a,b in memory.items()],expected=[region(a,b) for a,b in wanted.items()])

def run(case):
    data=EXE.read_bytes();pe=pefile.PE(data=data);base=pe.OPTIONAL_HEADER.ImageBase
    image=pe.get_memory_mapped_image();cpu=Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32);pages=set()
    def write(a,b):
        for page in range(a&~4095,(a+len(b)+4095)&~4095,4096):
            if page not in pages:cpu.mem_map(page,4096);pages.add(page)
        cpu.mem_write(a,bytes(b))
    write(base,image)
    for r in case['memory']:write(int(r['address'],16),bytes.fromhex(r['hex']))
    write(STACK-4096,bytes(8192));write(STACK,pack(STOP)+b''.join(pack(v) for v in case['stack']))
    protected=executable_ranges(data)+[(0x50AFFC,104),(0x50AC9C,104)]
    visits={};failures=[]
    def code(uc,a,n,_):
        visits[hex(a)]=visits.get(hex(a),0)+1
        if not any(p<=a<p+s for p,s in executable):failures.append(['outside_PE_execution',hex(a)]);uc.emu_stop()
    def written(uc,access,a,n,v,_):
        if any(a+n>p and a<p+s for p,s in protected):failures.append(['protected_write',hex(a)]);uc.emu_stop()
    executable=executable_ranges(data)
    cpu.hook_add(unicorn.UC_HOOK_CODE,code);cpu.hook_add(unicorn.UC_HOOK_MEM_WRITE,written)
    cpu.reg_write(xc.UC_X86_REG_ESP,STACK);cpu.reg_write(xc.UC_X86_REG_EBP,STACK)
    cpu.reg_write(xc.UC_X86_REG_ECX,case['ecx']);cpu.reg_write(xc.UC_X86_REG_EFLAGS,2)
    error=None
    try:cpu.emu_start(case['entry'],STOP,timeout=5_000_000,count=100000)
    except UcError as e:error=str(e)
    regs={n:hex(cpu.reg_read(getattr(xc,'UC_X86_REG_'+n))) for n in ['EAX','EIP','ESP','EBX','EBP','ECX']}
    actual=[region(int(r['address'],16),cpu.mem_read(int(r['address'],16),len(bytes.fromhex(r['hex'])))) for r in case['expected']]
    result=dict(success=error is None and not failures and int(regs['EIP'],16)==STOP,
        registers=regs,error=error,protected_failures=failures,regions_match=actual==case['expected'],
        actual=actual,visited_instructions=visits,instruction_count=sum(visits.values()),
        source_exe_sha256=sha(data),substitutions=0,external_API_adapters=0,
        unmapped_memory_policy='fatal; page0 not mapped')
    if case['kind']=='atoi':result['expected_EAX_matches']=int(regs['EAX'],16)==case['value']&0xFFFFFFFF
    return result

def cases():
    return [build('converter',v) for v in [0,2,9,15,65535]]+[build('atoi',v) for v in [0,2,9,15,65535,65536]]+[
        build('atlas',v,c) for v,c in [(0,4),(2,4),(9,4),(15,4),(16,4),(65535,4),(9,3)]]

def main():
    rows=[]
    for c in cases():
        r=run(c);name=f"{c['kind']}-{c['value']}-{c['columns']}"
        path=ROOT/'ghidra'/f'phase121-editor-icon-{name}.json'
        path.write_text(json.dumps(dict(scope=__doc__,input=c,result=r),indent=2),encoding='utf8')
        assert r['success'] and r['regions_match'] and r.get('expected_EAX_matches',True),(name,r['error'],r['registers'])
        rows.append(dict(case=name,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),instructions=r['instruction_count']))
        print(name,'passed',r['instruction_count'])
    (ROOT/'phase121-editor-icon-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        source_exe_sha256=sha(EXE.read_bytes()),source_object_sha256=sha(SOURCE.read_bytes())),indent=2),encoding='utf8')

if __name__=='__main__':main()
