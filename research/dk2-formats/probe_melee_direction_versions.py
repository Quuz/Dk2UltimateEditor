"""Attest the melee direction block and exhaust its angle-table inputs in3 EXEs."""
from pathlib import Path
import json,struct
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM
import native_unicorn_mapreader as cpu
import native_melee_direction as api
from emulate_element_colors import region
ROOT=Path(__file__).resolve().parent
sha=cpu.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
INPUT,OUTPUT,DRIVER,STOP=0x1D000000,0x1D010000,0x1D020000,0x1D030000
HELPER_PATTERN=bytes.fromhex('83ec0453528b4424100bc0')
FOLD_PATTERN=bytes.fromhex('25ff070000663dff03')


def vectors():
    out=[]
    for i in range(257):
        n=i*256;b=65536
        out += [(n,-b),(b,-n),(b,n),(n,b),(-n,b),(-b,n),(-b,-n),(-n,-b)]
    return out+[(0,0)]


def profiles():
    rows=[r for r in load('phase165-source-baseline.json')['executable_paths'] if Path(r['file']).name.lower() in ['dkii.exe','dkii-dx.exe']]
    out=[]
    for i,r in enumerate(rows):
        p=Path(r['file']);data=p.read_bytes();assert sha(data)==r['sha256'];pe=pefile.PE(data=data,fast_load=True)
        hits=[]
        for pattern in [HELPER_PATTERN,FOLD_PATTERN]:
            at=data.find(pattern);assert at>=0 and data.find(pattern,at+1)<0
            hits.append(pe.OPTIONAL_HEADER.ImageBase+pe.get_rva_from_offset(at))
        out.append(dict(id=['DX','provided13','standard'][i],**r,helper=hits[0],table=hits[0]+320,direction_block=hits[1]-31,block_bytes=125))
    return out


def build(p):
    points=vectors();data=Path(p['file']).read_bytes();lookup=api.table(data,p['table'])
    expected=b''.join(struct.pack('<H',api.angle(x,y,lookup)) for x,y in points)
    code=bytearray(b'\xbe'+struct.pack('<I',INPUT)+b'\xbf'+struct.pack('<I',OUTPUT)+b'\xb9'+struct.pack('<I',len(points)))
    loop=len(code)
    code+=bytes.fromhex('ff7604ff36b8')+struct.pack('<I',p['helper'])+bytes.fromhex('ffd083c40866890783c60883c70249')
    code+=b'\x75'+bytes([(loop-(len(code)+2))&255])+b'\xc3'
    mem={INPUT:b''.join(struct.pack('<ii',x,y) for x,y in points),OUTPUT:bytes([165])*len(expected),DRIVER:code,0x7FFEE000:bytes([165])*16384}
    args=dict(program='DKII-DX.exe',address=hex(DRIVER),registers='{}',memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),
        max_steps=2000000,allowed_external_IO_code=[[DRIVER,len(code)]],observe_reads=[],observe_writes=[])
    return args,mem,expected


def execute(p):
    args,ch,expected=build(p);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+p['sha256']).encode())[:12]
    file=ROOT/'ghidra'/f'phase194-angle-native-{p["id"]}-{tag}.json'
    if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args and saved['profile']==p
    else:
        r=cpu.run(Path(p['file']),args,[(a,b) for a,b in ch.items() if a!=0x7FFEE000],STOP,OUTPUT,len(expected))
        saved=dict(profile=p,input=args,result=r);dump(str(file.relative_to(ROOT)),saved)
    r=saved['result'];assert r['success'] and r['hit_return'] and not r['emulation_error'],r['emulation_error']
    assert bytes.fromhex(r['captured_hex'])==expected
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert r['visited_instructions'].get(hex(p['helper']))==len(vectors())
    return args,ch,saved,file


def static(p):
    data=Path(p['file']).read_bytes();pe=pefile.PE(data=data,fast_load=True);md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    def read(a,n):return pe.get_data(a-pe.OPTIONAL_HEADER.ImageBase,n)
    raw=read(p['direction_block'],p['block_bytes']);ins=list(md.disasm(raw,p['direction_block']))
    damage_reg='ebp' if p['id']=='provided13' else 'ebx'
    assert ins[-1].mnemonic=='mov' and ins[-1].op_str==damage_reg+', edx'
    lines=[dict(address=hex(x.address),hex=x.bytes.hex(),instruction=x.mnemonic+' '+x.op_str) for x in ins]
    normalized=[]
    for x in ins:
        if x.mnemonic=='call':assert x.operands[0].type==X86_OP_IMM and x.operands[0].imm==p['helper'];op='angle_helper'
        elif x.group(1):op='block+'+hex(x.operands[0].imm-p['direction_block'])
        else:op=x.op_str.replace(damage_reg,'damage')
        # Register allocation and coordinate load order differ in1.3.
        # Compare only the actual arithmetic core, after the angle call.
        if x.address>=p['direction_block']+31:normalized.append(x.mnemonic+' '+op)
    helper=read(p['helper'],320);lookup=read(p['table'],514)
    return dict(profile=p,block_hex=raw.hex(),block_sha256=sha(raw),instructions=lines,normalized_arithmetic_core=normalized,
                helper_hex=helper.hex(),helper_sha256=sha(helper),table_hex=lookup.hex(),table_sha256=sha(lookup),
                scope='Whole125-byte arithmetic block and whole320-byte pure angle helper, not whole non-DX melee function execution.')


def main():
    rows=[];static_rows=[]
    for p in profiles():
        args,ch,saved,file=execute(p);proof=static(p);static_rows.append(proof)
        rows.append(dict(profile=p,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),angle_calls=len(vectors()),
                         unique_angles=len(set(struct.unpack('<'+'H'*len(vectors()),bytes.fromhex(saved['result']['captured_hex']))))))
        print(p['id'],'angle calls',len(vectors()),'block',hex(p['direction_block']),flush=True)
    assert all(x['normalized_arithmetic_core']==static_rows[0]['normalized_arithmetic_core'] and x['table_hex']==static_rows[0]['table_hex'] for x in static_rows)
    dump('phase194-direction-versions.json',dict(profiles=static_rows,replays=rows,normalized_arithmetic_cores_identical=True,angle_tables_identical=True,
         scope='All3 pure angle helpers executed; full loader/melee/HP scenes belong to DX only. All257 table ratios in8 octants plus zero, not all32-bit coordinate/overflow inputs.'))


if __name__=='__main__':main()
