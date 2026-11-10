"""Attest both executed lookup bodies and old static loader/slot evidence."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read

ROOT=Path(__file__).resolve().parent


def main():
    dx=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';old=ROOT/'source-binaries/DKII-local13.exe'
    md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];queries=[]
    for program,exe,a,before in [('DKII-DX.exe',dx,0x50DD60,'phase72-DX-50dd60-assembly.txt'),('DKII-local13.exe',old,0x5027A0,'phase72-old-5027a0-assembly.txt')]:
        aa=[int(line[:8],16) for line in (ROOT/'ghidra'/before).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        assert aa[0]==a;last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));assert last.mnemonic=='ret'
        n=last.address+last.size-a;key=f'phase72-final-{program}-{a:x}'
        rows.append(dict(program=program,exe=str(exe),address=a,length=n,before=before,after=key+'-assembly.txt',memory=key+'-memory.txt',executed=True))
        queries.extend([dict(id=key+'-assembly',tool='disassemble_function',args=dict(program=program,address=hex(a)),print_limit=5),dict(id=key+'-memory',tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=5)])
    # A bounded scalar-loader fragment supplies actual intermediate140→def3A
    # evidence;it is not counted as a complete or newly executed function.
    assert pe_read(old.read_bytes(),0x4EC919,16)==bytes.fromhex('8b461885c074098b964001000089553a')
    blocks=[dict(program='DKII-local13.exe',exe=str(old),address=0x65F42C,length=8,memory='phase72-final-old-world-slot-memory.txt',scope='Native world vslot110 and114;table type was attested inphase62.'),
        dict(program='DKII-local13.exe',exe=str(old),address=0x4EC919,length=16,memory='phase72-final-old-loader-C8-fragment-memory.txt',scope='Scalar-loader fragment only:presence18/mid140→definition3A. Not a full loader execution or new raw→mid proof.')]
    for r in blocks:
        key=Path(r['memory']).stem
        queries.append(dict(id=key,tool='read_memory',args=dict(program=r['program'],address=hex(r['address']),length=r['length']),print_limit=5))
    (ROOT/'phase72-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase72-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print('Two complete executed lookup bodies;2explicitdata/codefragmentranges')


if __name__=='__main__':main()
