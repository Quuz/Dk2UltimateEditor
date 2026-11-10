"""Whole original Room writer with native calendar/string/flags/ftol helpers.

External CFile sink and five Win32 clock/TLS/error providers are explicit.
Only IAT data pointers are rebound in the isolated CPU; original PE code is
protected, including4AB320. CRT timezone/TLS state is prepared, not bootstrapped.
MFC GUI and raw-to-MFC construction are not executed.
"""
from pathlib import Path
import datetime,hashlib,json,struct,sys
import pefile
import native_room_writer as api
from audit_targeted_spell import executable_ranges

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
CTX,ARRAY,M1,M2,FILE,FVT,OUTPUT,META,STRINGS,PTD,TM,CODE= [0x15000000+i*0x10000 for i in range(12)]
STACK,STOP=0x7FFF0000,0x16000000
STAMP=bytes.fromhex('112233445566778899aa')
SYSTEM=struct.pack('<8H',2026,10,4,29,12,34,56,0)
sys.path.insert(0,str(ROOT/'dependencies/unicorn-2.1.4'))
import unicorn
from unicorn import x86_const as xc

def sha(b):return hashlib.sha256(b).hexdigest()
def p32(v):return struct.pack('<I',v)
def region(a,b):return dict(address=hex(a),hex=bytes(b).hex())

def sink():
    b=bytearray.fromhex('56578b74240c8b4c2410')
    b+=bytes.fromhex('81f9')+p32(1055);b+=b'\x77\x00';j1=len(b)-1
    b+=bytes.fromhex('8b3d')+p32(META+4)+bytes.fromhex('8d040f3d')+p32(OUTPUT+0x4000)
    b+=b'\x77\x00';j2=len(b)-1
    b+=bytes.fromhex('a3')+p32(META+4)+bytes.fromhex('ff05')+p32(META)+bytes.fromhex('f3a45f5ec20800')
    fail=len(b);b+=bytes.fromhex('c705')+p32(META+8)+p32(1)+bytes.fromhex('5f5ec20800')
    for j in [j1,j2]:b[j]=fail-j-1
    return bytes(b)

def model(n,unknown='pattern',flags=0xFFFFFFFF,torch=0x12):
    b=bytearray([165])*0x548
    b[:4]=p32(0x50B5A8) # Actual Room model vtable; not dereferenced by this writer.
    b[8:12]=p32(STRINGS+(n-1)*0x200+8)
    b[0x154:0x158]=p32(STRINGS+(n-1)*0x200+0x108)
    for i,(_,src,count) in enumerate(api.DIRECT):
        value=(n*179+i*41)&((1<<(count*8))-1);b[src:src+count]=value.to_bytes(count,'little')
    for i,(_,src,_) in enumerate(api.SCALED):struct.pack_into('<d',b,src,n+i/4)
    for i,(_,src) in enumerate(api.RESOURCES):b[src:src+84]=bytes((j*7+i+n)&255 for j in range(84))
    resource=bytes(84) if unknown=='zero' else bytes([255])*84 if unknown=='ones' else bytes((i*37+n)&255 for i in range(84))
    b[0x284:0x2D8]=resource
    b[0x4BC:0x4C0]=p32(flags)
    for bit,off in api.FLAG_FIELDS:b[off:off+4]=p32(0x80000000 if flags&bit else 0)
    b[0x4D0]=torch;b[0x168]=n
    return bytes(b)

def build(slots=(1,),seed=165,unknown='pattern',flags=0xFFFFFFFF,torch=0x12):
    ctx=bytearray(0x600);ctx[0x448:0x450]=struct.pack('<2I',ARRAY,len(slots))
    ptrs=b''.join(p32({0:0,1:M1,2:M2}[n]) for n in slots) or bytes(4)
    models={n:model(n,unknown,flags,torch) for n in [1,2]}
    labels={n:bytes([64+n])*5 for n in [1,2]};sounds={n:bytes([96+n])*7 for n in [1,2]}
    stack=bytearray((i*73+19)&255 for i in range(0x5000)) if seed=='pattern' else bytearray([seed])*0x5000
    # Header date starts at entryESP-42A. Gaps+4,+5,+9 are never written.
    def original_byte(address):return stack[address-(STACK-0x4000)]
    date=bytearray(original_byte(STACK-0x42A+i) for i in range(10))
    struct.pack_into('<H',date,0,2026);date[2]=29;date[3]=10;date[6]=12;date[7]=34;date[8]=56
    raw36f=original_byte(STACK-0x420+0x36F)
    records=b''.join(api.record(models[n],labels[n],sounds[n],unassigned_byte=raw36f) for n in slots if n)
    header=struct.pack('<7I',0x78,4,56+1055*len(slots),0x79,28,len(slots),1)+STAMP+date+struct.pack('<2I',0x7A,1055*len(slots))
    assert len(header)==56
    fvt=bytearray(0x40);fvt[0x38:0x3C]=p32(CODE)
    thread=bytearray(0x100);thread[0x40:0x44]=p32(TM)
    clock=bytearray.fromhex('8b442404')
    for i in range(4):clock+=(bytes.fromhex('c700') if i==0 else bytes.fromhex('c740')+bytes([i*4]))+SYSTEM[i*4:i*4+4]
    clock+=bytes.fromhex('c20400')
    providers={CODE:sink(),CODE+0x100:clock,CODE+0x200:bytes.fromhex('33c0c3'),
        CODE+0x300:b'\xB8'+p32(PTD)+bytes.fromhex('c20400'),CODE+0x400:bytes.fromhex('c20400')}
    mem={CTX:ctx,ARRAY:ptrs,M1:models[1],M2:models[2],FILE:p32(FVT),FVT:fvt,
        OUTPUT:bytes([165])*0x4000,META:struct.pack('<3I',0,OUTPUT,0),PTD:thread,TM:bytes(36),
        0x550936:STAMP,0x552A50:p32(0),0x552A58:SYSTEM,0x552CC8:p32(1),
        0x53B928:p32(0)*3,0x53B580:p32(3),
        0x504220:p32(CODE+0x100),0x504224:p32(CODE+0x100),
        0x504364:p32(CODE+0x200),0x50426C:p32(CODE+0x300),0x5042E8:p32(CODE+0x400),
        STACK-0x4000:stack,**providers}
    for n in [1,2]:
        for delta,value in [(0,labels[n]),(0x100,sounds[n])]:mem[STRINGS+(n-1)*0x200+delta]=p32(len(value))+p32(32)+value+b'\0'
    struct.pack_into('<2I',stack,0x4000,STOP,FILE)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),spans
    expected={CTX:bytes(ctx),ARRAY:ptrs,M1:models[1],M2:models[2],FILE:p32(FVT),FVT:bytes(fvt),
        OUTPUT:header+records+bytes([165])*(0x4000-56-len(records)),
        META:struct.pack('<3I',3+sum(bool(n) for n in slots),OUTPUT+56+len(records),0),
        PTD:bytes(thread),TM:struct.pack('<9I',56,34,12,29,9,126,4,301,0)}
    return dict(case=dict(slots=list(slots),seed=seed,unknown=unknown,flags=flags,torch=torch),
        memory=[region(a,b) for a,b in mem.items()],expected=[region(a,b) for a,b in expected.items()],
        allowed_external_IO_code=[[a,len(b)] for a,b in providers.items()],
        header_hex=header.hex(),raw36f=raw36f,records_hex=records.hex())

def run(c):
    data=EXE.read_bytes();pe=pefile.PE(data=data);cpu=unicorn.Uc(unicorn.UC_ARCH_X86,unicorn.UC_MODE_32);pages=set()
    def write(a,b):
        for page in range(a&~4095,(a+len(b)+4095)&~4095,4096):
            if page not in pages:cpu.mem_map(page,4096);pages.add(page)
        cpu.mem_write(a,bytes(b))
    write(pe.OPTIONAL_HEADER.ImageBase,pe.get_memory_mapped_image())
    for r in c['memory']:write(int(r['address'],16),bytes.fromhex(r['hex']))
    ranges=executable_ranges(data)
    protected=ranges+[(pe.OPTIONAL_HEADER.ImageBase+s.VirtualAddress,s.Misc_VirtualSize)
        for s in pe.sections if s.Characteristics&0x40000000 and not s.Characteristics&0x80000000]
    visits={};fail=[];writes=[]
    def code(uc,a,n,_):
        visits[hex(a)]=visits.get(hex(a),0)+1
        if not any(p<=a<p+s for p,s in ranges+c['allowed_external_IO_code']):fail.append(['outside_code',hex(a)]);uc.emu_stop()
    def written(uc,access,a,n,value,_):
        if any(a+n>p and a<p+s for p,s in protected):fail.append(['original_code_or_readonly_data_write',hex(a)]);uc.emu_stop()
        if OUTPUT<=a<OUTPUT+0x4000:writes.append(dict(pc=hex(uc.reg_read(xc.UC_X86_REG_EIP)),address=hex(a),size=n,value=value))
    cpu.hook_add(unicorn.UC_HOOK_CODE,code);cpu.hook_add(unicorn.UC_HOOK_MEM_WRITE,written)
    cpu.reg_write(xc.UC_X86_REG_ESP,STACK);cpu.reg_write(xc.UC_X86_REG_EBP,STACK)
    cpu.reg_write(xc.UC_X86_REG_ECX,CTX);cpu.reg_write(xc.UC_X86_REG_EFLAGS,2)
    error=None
    try:cpu.emu_start(0x4A7AC0,STOP,timeout=5_000_000,count=200000)
    except unicorn.UcError as e:error=str(e)
    actual=[region(int(r['address'],16),cpu.mem_read(int(r['address'],16),len(bytes.fromhex(r['hex'])))) for r in c['expected']]
    registers={n:hex(cpu.reg_read(getattr(xc,'UC_X86_REG_'+n))) for n in ['EAX','EIP','ESP','EBX','EBP','ECX']}
    return dict(success=not error and not fail and int(registers['EIP'],16)==STOP,
        regions_match=actual==c['expected'],actual=actual,error=error,registers=registers,
        visited_instructions=visits,instruction_count=sum(visits.values()),protected_failures=fail,
        source_exe_sha256=sha(data),original_code_substitutions=0,Win32_IAT_data_bindings=5,
        unmapped_memory_policy='fatal;page0 notmapped',captured_output_writes=writes,
        modified_region_summaries=[dict(address=a['address'],different_offsets=[i for i,(x,y) in enumerate(zip(bytes.fromhex(a['hex']),bytes.fromhex(b['hex']))) if x!=y][:30]) for a,b in zip(actual,c['expected']) if a!=b])

def cases():
    return [build(seed=s,unknown=u) for s in [0,165,'pattern'] for u in ['zero','pattern','ones']]+[
        build(slots=s,seed='pattern',flags=f) for s,f in [([],0),([0],0),([1,0,2],0xFFFFFFFF),([2,1],0),([1,2],0x80000E00)]]+[
        build(flags=f,seed=0) for f in [0x800,0x1000,0x2000]]

def main():
    selected=cases() if '--all' in sys.argv else [build()]
    rows=[]
    for i,c in enumerate(selected):
        tag=sha(json.dumps(c,sort_keys=True).encode())[:12];path=ROOT/'ghidra'/f'phase122-Room-writer-{tag}.json'
        r=run(c);path.write_text(json.dumps(dict(scope=__doc__,input=c,result=r),indent=2),encoding='utf8')
        assert r['success'] and r['regions_match'],(c['case'],r['error'],r['registers'],r['modified_region_summaries'])
        assert (int(r['registers']['EAX'],16)&255)==1
        assert all(hex(a) in r['visited_instructions'] for a in [0x4A7AC0,0x4AB320,0x4D6C53,0x4D6C66,0x4AEB67,0x4AED4D])
        rows.append(dict(case=c['case'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),instructions=r['instruction_count']))
        print(i,c['case'],'passed',r['instruction_count'])
    if '--all' in sys.argv:(ROOT/'phase122-room-writer-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,source_exe_sha256=sha(EXE.read_bytes())),indent=2),encoding='utf8')

if __name__=='__main__':main()
