"""Re-attest typed Room/CLight/render instructions and native queue tables."""
from pathlib import Path
import hashlib,json,re,sys,struct
import capstone
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
FRESH=[0x439B90,0x4397E0,0x43A0D0,0x43A1E0,0x4559B0,
    0x599960,0x599C60,0x599BD0,0x571A90,0x571AB0,0x43A740]
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def seed():
    md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);image=EXE.read_bytes();rows=[]
    for a in FRESH:
        name=f'ghidra/phase123-torch-{a:06x}-disassemble_function.txt'
        addresses=[int(m[1],16) for m in re.finditer(r'^([0-9a-fA-F]{8}):',(ROOT/name).read_text(encoding='utf8'),re.M)]
        assert addresses and addresses[0]==a
        last=next(md.disasm(pe_read(image,addresses[-1],15),addresses[-1]));n=addresses[-1]+last.size-a
        assert [i.address for i in md.disasm(pe_read(image,a,n),a)]==addresses
        rows.append(dict(address=a,length=n,assembly=link(name),instruction_count=len(addresses)))
    return rows

def queries():
    q=[dict(id=f'phase123-torch-{r["address"]:06x}-read_memory',tool='read_memory',
        args=dict(address=hex(r['address']),length=r['length'],program='DKII-DX.exe'),print_limit=0)
        for r in seed() if r['address'] in [0x571AB0,0x43A740]]
    q.append(dict(id='phase123-light-all-constants',tool='read_memory',args=dict(address='0x6704b8',length=116,program='DKII-DX.exe'),print_limit=0))
    (ROOT/'phase123-final-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')

def build():
    image=EXE.read_bytes();rows=[]
    for r in seed():
        name=f'ghidra/phase123-torch-{r["address"]:06x}-read_memory.txt'
        j=json.loads((ROOT/name).read_text(encoding='utf8'));b=bytes(j['data'])
        assert int(j['address'],16)==r['address'] and len(b)==r['length'] and b==pe_read(image,r['address'],r['length'])
        rows.append(dict(r,memory=link(name),original_bytes_sha256=sha(b)))
    inherited=[]
    previous=json.loads((ROOT/'phase102-primary-manifest.json').read_text(encoding='utf8'))
    for r in previous['functions']:
        if r['address'] not in [0x57CEB0,0x57CD30]:continue
        name='ghidra/'+r['memory'];j=json.loads((ROOT/name).read_text(encoding='utf8'));b=bytes(j['data'])
        assert len(b)==r['length'] and b==pe_read(image,r['address'],r['length'])
        inherited.append(dict(address=r['address'],length=r['length'],memory=link(name),original_bytes_sha256=sha(b),inherited_from=link('phase102-primary-manifest.json')))
    assert len(inherited)==2
    blocks=[]
    for a,n,name in [(0x66CC94,192,'phase123-torch-real-render-vtable.txt'),
        (0x66CC14,20,'phase123-light-vtable.txt'),(0x6704B8,116,'phase123-light-all-constants.txt')]:
        file='ghidra/'+name;j=json.loads((ROOT/file).read_text(encoding='utf8'));b=bytes(j['data'])
        assert len(b)==n and b==pe_read(image,a,n)
        blocks.append(dict(address=a,length=n,memory=link(file),original_bytes_sha256=sha(b)))
    assert int.from_bytes(pe_read(image,0x66CC94+0xA4,4),'little')==0x4397E0
    assert pe_read(image,0x43A85D,6)==bytes.fromhex('c70694cc6600')
    assert struct.unpack('<f',pe_read(image,0x670524,4))[0]==1/128
    return dict(phase=123,fresh_function_ranges=rows,inherited_allocator_ranges=inherited,blocks=blocks,
        program='DKII-DX.exe',executable=str(EXE),executable_sha256=sha(image),
        scope='13keyfullfunctionspans plus3nativeblocks. WholeCLight-to-render CPU path runs selected allocator/queuebranches;Roomwallplacement/Lightfactory/constructors remain static in thisphase.')

if __name__=='__main__':
    if '--queries' in sys.argv:queries();print('Prepared2function+1constantsqueries.')
    else:
        v=build();(ROOT/'phase123-primary-manifest.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
        print('13keyfunctionspans/3blocksattested.')
