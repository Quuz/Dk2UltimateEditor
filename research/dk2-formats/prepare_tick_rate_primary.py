"""Original byte coverage for the accepted19->Freeze chain and static Level lead."""
from pathlib import Path
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_tick_rate as native
from build_effect_semantics import pe_read
ROOT,load,dump,sha=native.ROOT,native.load,native.dump,native.sha


def build():
    data=native.EXE.read_bytes();pcs=set();external=set()
    for row in load('phase195-tps-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        saved=load(row['file']);allowed=saved['input']['allowed_external_IO_code']
        for text in saved['result']['visited_instructions']:
            a=int(text,16)
            if 0x400000<=a<0x800000:pcs.add(a)
            else:assert any(p<=a<p+n for p,n in allowed);external.add(a)
    old={}
    for name in ['phase168-shot-combinations-primary.json','phase171-target-primary.json','phase193-gold-primary.json']:
        p=load(name)
        for row in p.get('functions',[])+p.get('inherited',[])+p.get('executed_blocks',[]):old[(row['address'],row['length'])]=row
    rows=[];covered=set();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for row in old.values():
        if not any(row['address']<=a<row['address']+row['length'] for a in pcs):continue
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256']
        obj=load(row['memory']);raw=bytes(obj['data']) if 'data' in obj else bytes.fromhex(obj['hex'])
        assert raw==pe_read(data,row['address'],row['length']) and sha(raw)==row['bytes_sha256']
        if row.get('assembly'):
            text=(ROOT/row['assembly']).read_text(encoding='utf8')
            if row.get('assembly_sha256'):assert sha((ROOT/row['assembly']).read_bytes())==row['assembly_sha256']
            for token in re.findall(r'^([0-9a-fA-F]{8}):',text,re.M):
                a=int(token,16);covered.add(a);ins=next(md.disasm(pe_read(data,a,15),a,count=1))
                if ins.mnemonic=='wait':covered.add(a+ins.size)
        else:covered.update(a for a in pcs if row['address']<=a<row['address']+row['length'])
        rows.append(row)
    missing=pcs-covered
    if missing:
        print('Uncovered',len(missing),[hex(x) for x in sorted(missing)[:24]],flush=True);return None
    name='ghidra/phase195-Level-store-memory.txt'
    if not (ROOT/name).exists():
        dump('phase195-primary-queries.json',[
            dict(id='phase195-Level-store-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address='0x51e6d0'),print_limit=0),
            dict(id='phase195-Level-store-memory',tool='read_memory',args=dict(program='DKII-DX.exe',address='0x51e6d0',length=0x364),print_limit=0)])
        print('Level static primary queries ready',flush=True);return None
    obj=load(name);raw=bytes(obj['data']);assert raw==pe_read(data,0x51E6D0,0x364)
    return dict(profile=dict(file=str(native.EXE),sha256=sha(data)),inherited_functions=rows,
        unique_executed_original_PCs=len(pcs),declared_external_IO_PCs=len(external),all_executed_original_PCs_covered=True,
        Level_static_only=dict(address=0x51E6D0,length=0x364,memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(raw),
            assembly='ghidra/phase195-Level-store-assembly.txt',assembly_sha256=sha((ROOT/'ghidra/phase195-Level-store-assembly.txt').read_bytes()),
            source_MID_offset=0x12B6,store_instruction=0x51E708,runtime_address=0x6F2590,scope='Static whole function bytes; this Level callback is not executed by the phase195 scenes.'))


if __name__=='__main__':
    p=build()
    if p is not None:dump('phase195-tps-primary.json',p);print('TPS primary',len(p['inherited_functions']),p['unique_executed_original_PCs'])
