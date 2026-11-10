"""Re-attest cached original code covering the accepted directional melee scenes."""
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_melee_direction as native
import probe_melee_direction_versions as versions
from build_effect_semantics import pe_read
ROOT,load,dump,sha=native.ROOT,native.load,native.dump,native.sha


def build():
    data=native.EXE.read_bytes();pcs=set();external=set()
    for row in load('phase194-direction-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        saved=load(row['file']);allowed=saved['input']['allowed_external_IO_code']
        for text in saved['result']['visited_instructions']:
            a=int(text,16)
            if 0x400000<=a<0x800000:pcs.add(a)
            else:
                assert any(p<=a<p+n for p,n in allowed),hex(a)
                external.add(a)
    prior=load('phase147-melee-animation-primary.json');functions=[];covered=set()
    md=Cs(CS_ARCH_X86,CS_MODE_32)
    for row in prior['functions']:
        if not any(row['address']<=pc<row['address']+row['length'] for pc in pcs):continue
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256']
        assert sha((ROOT/row['assembly']).read_bytes())==row['assembly_sha256']
        memory=load(row['memory']);raw=bytes(memory['data'])
        assert len(raw)==row['length'] and raw==pe_read(data,row['address'],row['length']) and sha(raw)==row['bytes_sha256']
        addresses=[int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',(ROOT/row['assembly']).read_text(encoding='utf8'),re.M)]
        covered.update(addresses)
        for pc in addresses:
            ins=next(md.disasm(pe_read(data,pc,15),pc,count=1))
            if ins.mnemonic=='wait':
                follow=next(md.disasm(pe_read(data,pc+ins.size,15),pc+ins.size,count=1))
                if follow.mnemonic.startswith('fn'):covered.add(follow.address)
        functions.append(row)
    assert pcs<=covered,[hex(x) for x in sorted(pcs-covered)[:20]]
    tables=[]
    for row in prior['blocks']:
        if 'vtable' not in row['meaning'].lower():continue
        memory=load(row['memory']);raw=bytes(memory['data'])
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256']
        assert raw==pe_read(data,row['address'],row['length']) and sha(raw)==row['bytes_sha256']
        tables.append(row)
    v=load('phase194-direction-versions.json')
    for p,row,proof in zip(versions.profiles(),v['replays'],v['profiles']):
        assert proof==versions.static(p)
        assert row['profile']==p and sha((ROOT/row['file']).read_bytes())==row['sha256']
        trace=load(row['file'])['result'];allowed=trace['allowed_external_IO_code']
        assert all(p['helper']<=int(pc,16)<p['helper']+320 or any(a<=int(pc,16)<a+n for a,n in allowed) for pc in trace['visited_instructions'])
    return dict(profile=dict(file=str(native.EXE),sha256=sha(data)),inherited_functions=functions,inherited_tables=tables,
        unique_executed_original_PCs=len(pcs),declared_external_IO_PCs=len(external),
        all_executed_original_PCs_match_primary_instruction_starts=True,new_function_extents=0,
        version_scope='DX full loader/melee/HP; all3 profiles pure angle helper plus125-byte direction block static attestation. No full1.3/standard melee scene.')


if __name__=='__main__':
    p=build();dump('phase194-direction-primary.json',p)
    print('direction primary',len(p['inherited_functions']),'functions',p['unique_executed_original_PCs'],'PCs')
