"""Attest actual phase184 PCs using unchanged complete phase183 PE extents.

No new function is created at an arbitrary internal instruction. A bounded
CRT span remains explicitly bounded, as recorded in the inherited evidence.
"""
from pathlib import Path
from functools import cache
import json
import emulate_crypt_drop_recovery as native
import prepare_crypt_room_assignment_primary as prior
from audit_targeted_spell import executable_ranges
ROOT=Path(__file__).resolve().parent
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))

@cache
def build():
    manifest=load('phase185-crypt-drop-recovery-emulations.json');old=load('phase183-crypt-room-assignment-primary.json')
    blob=native.EXE.read_bytes();ranges=executable_ranges(blob);visited=set()
    for row in manifest['replays']:
        f=ROOT/row['file'];assert sha(f.read_bytes())==row['sha256']
        r=load(row['file'])['result'];assert r['success'] and r['hit_return'] and not r['emulation_error']
        visited|={int(pc,16) for pc in r['visited_instructions'] if any(a<=int(pc,16)<a+n for a,n in ranges)}
    used=[];covered=set();seen=set()
    for row in old['functions']+old['inherited']:
        address=row['address']
        if address in seen:continue
        seen.add(address)
        f=ROOT/prior.path(row['memory']);data=bytes(load(str(f.relative_to(ROOT)))['data'])
        assert len(data)==row['length'] and data==native.p.pe_read(blob,address,row['length'])
        assert sha(data)==row['bytes_sha256'] and sha(f.read_bytes())==row['memory_sha256']
        instructions=prior.instructions(row)
        if instructions&visited:
            used.append(dict(row,source_manifest='phase183-crypt-room-assignment-primary.json'));covered|=instructions
    # State255's callback is an actual observed original476FC0 CALL target.
    # Keep the captured288-byte extent explicitly bounded; do not infer a
    # function at a sampled interior PC or claim unexecuted branch coverage.
    memory='ghidra/phase185-primary-4810e0-memory.txt';assembly='ghidra/phase185-primary-4810e0-assembly.txt'
    raw=bytes(load(memory)['data']);assert len(raw)==288 and raw==native.p.pe_read(blob,0x4810E0,288)
    seen_call=False
    for row in manifest['replays']:
        r=load(row['file'])['result']
        if any(source in visited and target==0x4810E0 for source,target in r['observed_call_edges']):seen_call=True
    assert seen_call
    fresh=dict(address=0x4810E0,length=288,extent_kind='bounded_state255_callback_span',
        memory=memory,memory_sha256=sha((ROOT/memory).read_bytes()),bytes_sha256=sha(raw),
        assembly=assembly,assembly_sha256=sha((ROOT/assembly).read_bytes()),entry_evidence='ActualoriginalAIcallbackCALLtargetandverifiedstate255tableentry')
    pcs=prior.instructions(fresh);assert min(pcs)==0x4810E0 and max(pcs)<0x481200
    covered|=pcs
    assert visited<=covered,[hex(a) for a in sorted(visited-covered)[:40]]
    blocks=[]
    for row in old['blocks']:
        f=ROOT/prior.path(row['memory']);data=bytes(load(str(f.relative_to(ROOT)))['data'])
        assert data==native.p.pe_read(blob,row['address'],row['length']) and sha(f.read_bytes())==row['memory_sha256']
        blocks.append(dict(row,source_manifest='phase183-crypt-room-assignment-primary.json'))
    return dict(profile=dict(file=str(native.EXE),sha256=sha(blob)),functions=[fresh],inherited=used,blocks=blocks,
        unique_executed_original_PCs=len(visited),all_executed_original_PCs_covered=True,
        inherited_manifest_sha256=sha((ROOT/'phase183-crypt-room-assignment-primary.json').read_bytes()))

def main():
    d=build();(ROOT/'phase185-crypt-drop-recovery-primary.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
    print(len(d['functions']),'new',len(d['inherited']),'inherited',d['unique_executed_original_PCs'],'original PCs')

if __name__=='__main__':main()
