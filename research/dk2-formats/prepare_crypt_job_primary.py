"""Primary bytes/actual CALL entries for phase188 job and source selectors."""
from pathlib import Path
from functools import cache
import json
import emulate_crypt_job_selection as native
import prepare_crypt_room_assignment_primary as old
from audit_targeted_spell import executable_ranges
ROOT=Path(__file__).resolve().parent
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
NEW=[0x4773D0,0x4780C0,0x4780E0,0x4782C0,0x478800,0x483AC0,0x494550,0x635F60]
NEW += [0x45A290,0x46F390,0x4778E0,0x491880,0x4944D0,0x4D93E0,0x4E0470,0x509200,0x636110,0x636170]
INHERITED=['phase183-crypt-room-assignment-primary.json','phase179-crypt-objects-primary.json',
    'phase180-crypt-action-primary.json','phase145-room-wander-primary.json']

@cache
def build():
    blob=native.EXE.read_bytes();ranges=executable_ranges(blob);visited=set();roots=set();traces=[]
    for row in load('phase188-crypt-job-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256'];r=load(row['file'])['result'];traces.append(r)
        assert r['success'] and r['hit_return'] and not r['emulation_error']
        visited|={int(a,16) for a in r['visited_instructions'] if any(b<=int(a,16)<b+n for b,n in ranges)}
        roots.update(t for s,t in r['observed_call_edges'])
    old_rows={}
    for name in INHERITED:
        m=load(name)
        for row in m['functions']+m.get('inherited',[]):
            if row.get('program','DKII-DX.exe')!='DKII-DX.exe' or row['address'] in old_rows:continue
            old_rows[row['address']]=dict(row,source_manifest=name)
    used=[];fresh=[];covered=set()
    for address in NEW:
        assert address in roots,hex(address)
        mem=f'ghidra/phase188-primary-{address:x}-memory.txt';asm=f'ghidra/phase188-primary-{address:x}-assembly.txt'
        raw=bytes(load(mem)['data']);assert raw==native.p.pe_read(blob,address,len(raw))
        row=dict(address=address,length=len(raw),memory=mem,assembly=asm,
            memory_sha256=sha((ROOT/mem).read_bytes()),assembly_sha256=sha((ROOT/asm).read_bytes()),bytes_sha256=sha(raw),
            entry_evidence='ActualcapturedCALLtarget;Ghidrafunctionbody;4773D0alsooriginalstate1initializerbinding',extent_kind='Ghidra_function_listing_span')
        pcs=old.instructions(row);assert min(pcs)==address
        if pcs&visited:fresh.append(row);covered|=pcs
    for address,row in old_rows.items():
        if address in NEW:continue
        mem=old.path(row['memory']);raw=bytes(load(mem)['data'])
        assert len(raw)==row['length'] and raw==native.p.pe_read(blob,address,len(raw))
        pcs=old.instructions(row)
        if pcs&visited:
            used.append(dict(row,memory=mem,memory_sha256=sha((ROOT/mem).read_bytes()),bytes_sha256=sha(raw)));covered|=pcs
    missing=visited-covered
    if missing:
        dump('phase188-primary-gaps.json',dict(missing=[hex(x) for x in sorted(missing)],
            actual_CALL_entries_not_covered=[hex(x) for x in sorted((roots&visited)-covered)]))
    assert not missing,[hex(x) for x in sorted(missing)[:60]]
    return dict(profile=dict(file=str(native.EXE),sha256=sha(blob)),functions=fresh,inherited=used,
        unique_executed_original_PCs=len(visited),all_executed_original_PCs_covered=True,
        entries_evidence=dict(file='phase188-function-entry-proofs.json',sha256=sha((ROOT/'phase188-function-entry-proofs.json').read_bytes())),
        additional_entries_evidence=dict(file='phase188-additional-entry-proofs.json',sha256=sha((ROOT/'phase188-additional-entry-proofs.json').read_bytes())),
        inherited_manifests=[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in INHERITED],
        scope='ExecutedPEinstructioncoverageonly;notthewholeWorldscheduler/allbranches/allversions.')

def main():
    d=build();dump('phase188-crypt-job-primary.json',d)
    print(len(d['functions']),'newspans',len(d['inherited']),'inherited',d['unique_executed_original_PCs'],'PC')

if __name__=='__main__':main()
