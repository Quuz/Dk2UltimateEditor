"""Attest Room save/calendar and byte-width load paths against both original PEs."""
from pathlib import Path
import json,re,sys
import capstone,pefile
from build_effect_semantics import pe_read
from emulate_room_writer import ROOT,EXE,sha

SPECS=[('Pro','save',a) for a in [0x4A7AC0,0x4AB320,0x4AB3B0,0x424B40,0x424780,0x424970,
    0x49FC30,0x42DF90,0x470740,0x471930,0x471E00]]+[
    ('Pro','time',0x4D6C53),('Pro','time',0x4D6C66)]+[
    ('Pro','calendar',a) for a in [0x4AEB67,0x4AED4D,0x4AEC43,0x4B08D3,0x4B3876,0x4B3E38,0x4ACDA8]]+[
    ('DX','intensity',a) for a in [0x56DFD0,0x4EACC0]]
PATHS={'Pro':EXE,'DX':EXE.parent.parent.parent/'DKII-DX.exe'}
PROGRAMS={'Pro':'DK2 Editor pro.exe','DX':'DKII-DX.exe'}

def seed():
    md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);rows=[]
    for profile,prefix,a in SPECS:
        path=ROOT/f'ghidra/phase122-{prefix}-{a:06x}-disassemble_function.txt'
        text=path.read_text(encoding='utf8');addresses=[int(m[1],16) for m in re.finditer(r'^([0-9a-fA-F]{8}):',text,re.M)]
        assert addresses[0]==a
        image=PATHS[profile].read_bytes();last=next(md.disasm(pe_read(image,addresses[-1],15),addresses[-1]))
        n=addresses[-1]+last.size-a;native=[i.address for i in md.disasm(pe_read(image,a,n),a)]
        rows.append(dict(profile=profile,program=PROGRAMS[profile],address=a,length=n,
            assembly=str(path.relative_to(ROOT)),assembly_sha256=sha(path.read_bytes()),
            linear_instruction_boundaries_match=native==addresses,reported_instructions=len(addresses)))
    return rows

def queries(profile):
    q=[dict(id=f'phase122-primary-{profile}-{r["address"]:06x}',tool='read_memory',
        args=dict(address=hex(r['address']),length=r['length'],program=r['program']),print_limit=0)
        for r in seed() if r['profile']==profile]
    if profile=='Pro':
        for a,n in [(0x50B5A8,80),(0x504220,8),(0x504364,4),(0x50426C,4),(0x5042E8,4)]:
            q.append(dict(id=f'phase122-table-{a:06x}',tool='read_memory',args=dict(address=hex(a),length=n,program=PROGRAMS[profile]),print_limit=0))
        q.append(dict(id='phase122-editor-saved',tool='save_program',args=dict(program=PROGRAMS[profile]),print_limit=0))
    (ROOT/f'phase122-primary-{profile}-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')

def build():
    rows=[]
    for r in seed():
        path=ROOT/f'ghidra/phase122-primary-{r["profile"]}-{r["address"]:06x}.txt';j=json.loads(path.read_text(encoding='utf8'));b=bytes(j['data'])
        assert int(j['address'],16)==r['address'] and len(b)==r['length'] and b==pe_read(PATHS[r['profile']].read_bytes(),r['address'],r['length'])
        rows.append(dict(r,memory=str(path.relative_to(ROOT)),memory_sha256=sha(path.read_bytes()),original_bytes_sha256=sha(b)))
    tables=[]
    for a,n in [(0x50B5A8,80),(0x504220,8),(0x504364,4),(0x50426C,4),(0x5042E8,4)]:
        path=ROOT/f'ghidra/phase122-table-{a:06x}.txt';j=json.loads(path.read_text(encoding='utf8'))
        assert bytes(j['data'])==pe_read(EXE.read_bytes(),a,n)
        tables.append(dict(address=a,length=n,memory=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
    imports={i.address:i.name.decode() for d in pefile.PE(str(EXE)).DIRECTORY_ENTRY_IMPORT for i in d.imports if i.name}
    assert {a:imports[a] for a in [0x504220,0x504224,0x504364,0x50426C,0x5042E8]}=={
        0x504220:'GetLocalTime',0x504224:'GetSystemTime',0x504364:'GetLastError',0x50426C:'TlsGetValue',0x5042E8:'SetLastError'}
    return dict(phase=122,functions=rows,tables=tables,
        linear_match_ranges=sum(r['linear_instruction_boundaries_match'] for r in rows),
        source_executables={k:dict(file=str(v),sha256=sha(v.read_bytes())) for k,v in PATHS.items()},
        scope='22wholememoryspans ofkeynativefunctions;Ghidra instruction boundaries compared to linear Capstone where applicable. Not every transitive callee orallcalendar/DST/init branches.')

if __name__=='__main__':
    if len(sys.argv)>1:queries(sys.argv[1]);print('Queries prepared',sys.argv[1])
    else:
        out=build();(ROOT/'phase122-primary-manifest.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
        print('Primary spans',len(out['functions']),'linear matches',out['linear_match_ranges'],'tables',len(out['tables']))
