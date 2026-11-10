"""Attest waiting paths with complete old PE extents and actual callback entry."""
from pathlib import Path
from functools import cache
import json
import emulate_crypt_waiting as native
import prepare_crypt_room_assignment_primary as old
from audit_targeted_spell import executable_ranges
ROOT=Path(__file__).resolve().parent
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))

@cache
def build():
    blob=native.EXE.read_bytes();ranges=executable_ranges(blob);visited=set();traces=[]
    for row in load('phase187-crypt-waiting-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];r=load(row['file'])['result'];traces.append(r)
        assert r['success'] and r['hit_return'] and not r['emulation_error']
        visited|={int(a,16) for a in r['visited_instructions'] if any(b<=int(a,16)<b+n for b,n in ranges)}
    prior=load('phase183-crypt-room-assignment-primary.json');rows=prior['functions']+prior['inherited']
    extra_manifests=['phase94-primary-manifest.json','phase179-crypt-objects-primary.json']
    candidates=[(r,'phase183-crypt-room-assignment-primary.json') for r in rows]
    for name in extra_manifests:
        manifest=load(name)
        candidates.extend((r,name) for r in manifest['functions']+manifest.get('inherited',[]))
    used=[];seen=set();covered=set()
    for row,source in candidates:
        if row.get('program','DKII-DX.exe')!='DKII-DX.exe' or row['address'] in seen:continue
        seen.add(row['address']);f=ROOT/old.path(row['memory']);raw=bytes(load(str(f.relative_to(ROOT)))['data'])
        assert len(raw)==row['length'] and raw==native.p.pe_read(blob,row['address'],row['length'])
        pcs=old.instructions(row)
        if pcs&visited:
            used.append(dict(row,memory=str(f.relative_to(ROOT)),memory_sha256=sha(f.read_bytes()),bytes_sha256=sha(raw),source_manifest=source));covered|=pcs
    missing=visited-covered
    assert not missing,[hex(a) for a in sorted(missing)[:60]]
    # The existing276 callback is independently linked by an actual AI CALL;
    # no new Ghidra function is created at an arbitrary instruction.
    edges=[(s,t) for r in traces for s,t in r['observed_call_edges'] if s in visited and t==0x4839E0]
    assert edges
    return dict(program_sha256=sha(blob),functions=[],inherited=used,unique_executed_original_PCs=len(visited),
        all_executed_original_PCs_covered=True,actual_waiting_callback_edges=[list(e) for e in sorted(set(edges))],
        inherited_manifests=[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in ['phase183-crypt-room-assignment-primary.json']+extra_manifests],
        boundary='Existingwholefunctions; coverageofactualexecutedPCsonly,notallbranchesorstartup.')

def main():
    d=build();(ROOT/'phase187-crypt-waiting-primary.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
    print(len(d['inherited']),'inherited',d['unique_executed_original_PCs'],'originalPCs')

if __name__=='__main__':main()
