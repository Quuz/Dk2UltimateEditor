"""Verify six fresh Ghidra disassemblies against original PE instructions locally.

No new Ghidra request: independent CPU controls await approval-review recovery.
Other ranges are original PE instruction evidence with explicitly named earlier
Ghidra loader/HP data evidence; source symbol-map names are hints only.
"""
import hashlib
import json
import re
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def norm(text):
    text=text.lower().strip()
    if text.startswith('rep '):m,_,op=text[4:].partition(' ');m='rep '+m
    else:m,_,op=text.partition(' ')
    if '.rep' in m:m='rep '+m.replace('.rep','')
    aliases={'jz':'je','jnz':'jne','jc':'jb','jnc':'jae','jna':'jbe','jnbe':'ja','sal':'shl','setnz':'setne'}
    m=aliases.get(m,m)
    if m in ['rep movsd','rep stosd','stosw','stosb']:return m,'' # inherent implicit registers
    op=re.sub(r'\b(?:byte|word|dword|qword|ptr|xmmword)\b','',op)
    op=re.sub(r'0x[0-9a-f]+|\b\d+\b',lambda q:str(int(q[0],16 if q[0].startswith('0x') else 10)),op)
    return m,re.sub(r'\*1(?=[+\]\-,]|$)','',re.sub(r'\s+','',op))


def build():
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[]
    for a,n in [(0x56E540,334),(0x56E690,623),(0x4ACD60,133),(0x4ACDF0,940),(0x4AC420,373),(0x4AC5A0,300)]:
        p=ROOT/'ghidra'/f'phase114-shot-{a:x}-disassemble_function.txt'
        text=p.read_text(encoding='utf8')
        saved=[re.match(r'^([0-9a-fA-F]{8}):\s+(.*)',l).groups() for l in text.splitlines() if re.match(r'^[0-9a-fA-F]{8}:',l)]
        raw=pe_read(data,a,n);ins=list(md.disasm(raw,a))
        assert ins[-1].address+ins[-1].size==a+n
        assert [x.address for x in ins]==[int(x[0],16) for x in saved]
        assert all(norm(x.mnemonic+' '+x.op_str)==norm(s) for x,(_,s) in zip(ins,saved))
        rows.append(dict(address=a,length=n,hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest(),
                         Ghidra_assembly=p.name,Ghidra_assembly_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),
                         Ghidra_instruction_text_matches_original_PE=True,instructions=len(ins)))
    other=[]
    for a,n in [(0x4AC3C0,51),(0x4AC6D0,594),(0x4B5C30,11),(0x504D60,31),(0x50E4D0,56),(0x509090,41),(0x628BC0,6)]:
        raw=pe_read(data,a,n);ins=list(md.disasm(raw,a));assert ins[-1].address+ins[-1].size==a+n
        other.append(dict(address=a,length=n,hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest(),
                          instructions=[dict(address=x.address,bytes=x.bytes.hex(),text=x.mnemonic+' '+x.op_str) for x in ins],
                          evidence='Original PE bytes; no new Ghidra memory/CPU request for this row.'))
    blocks=[]
    for a,n,label in [(0x66D86C,0x100,'Shot vtable'),(0x670574+0x98,4,'graphics creation-effect slot98')]:
        raw=pe_read(data,a,n);blocks.append(dict(address=a,length=n,label=label,hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest()))
    inherited=[]
    m=json.loads((ROOT/'phase113-primary-manifest.json').read_text(encoding='utf8'))
    for row in m['functions']:
        if row['address'] in [0x4430D0,0x491DC0,0x568820,0x568E30,0x492320,0x569D70,0x569D90,0x569DB0,0x4B2CE0,0x494D00]:
            p=ROOT/'ghidra'/row['memory'];v=json.loads(p.read_text(encoding='utf8'));raw=bytes(v['data'])
            assert raw==pe_read(data,row['address'],row['length'])
            inherited.append(dict(row,evidence_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),program_sha256=hashlib.sha256(raw).hexdigest()))
    assert len(inherited)==10
    return dict(source_exe=str(EXE),sha256=hashlib.sha256(data).hexdigest(),fresh_Ghidra_instruction_ranges=rows,
                additional_PE_ranges=other,inherited_Ghidra_memory_ranges=inherited,PE_data_blocks=blocks,
                independent_Ghidra_CPU_controls=0,
                pending='Five prepared Ghidra CPU controls were not dispatched: automatic approval review could not complete due to usage limit. No bypass attempted.')


if __name__=='__main__':
    v=build();(ROOT/'phase114-primary-manifest.json').write_text(json.dumps(v,indent=2),encoding='utf8')
    print('6 fresh Ghidra instruction ranges,7 original PE ranges,10 inherited Ghidra byte ranges,2 data blocks verified')
