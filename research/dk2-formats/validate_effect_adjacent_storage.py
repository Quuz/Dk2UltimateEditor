"""Mode2/count/eighth-slot edits via public scalar API in whole source containers."""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'effect-mode18-semantics.json').read_text(encoding='utf8'))['preservation']['source_files']
    rows=[];sources=[];original_modes=Counter();mode2=[];edits=0;digest=hashlib.sha256();total=0
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256']
        try:parts=fmt.parse(data)
        except ValueError:continue
        item=next(((i,s) for i,s in enumerate(parts) if s.signature==270),None)
        if item is None:continue
        si,s=item;assert all(len(r)==246 for r in s.records);total+=len(s.records)
        for i,r in enumerate(s.records):
            original_modes[r[0xF2]]+=1
            if r[0xF2]==2:mode2.append(dict(file=str(p),record_index=i,id=fmt.u16(r,0xB8),name=r[:32].split(b'\0')[0].decode('cp1252',errors='backslashreplace'),count=r[0xF3],flags=fmt.u32(r,0xB4)))
        selected=sorted({0,*([next(i for i,r in enumerate(s.records) if r[0xF2]==2)] if any(r[0xF2]==2 for r in s.records) else [])})
        for ri in selected:
            raw=s.records[ri]
            for field,offset,size,value in [('GenerationType',0xF2,1,2),('ElementsPerTurn',0xF3,1,255),('GenerateIds[7].id',0xD8,2,1),('GenerateIds[7].id',0xD8,2,65535)]:
                patched=fmt.set_scalar(raw,'Effects',field,value);assert patched==raw[:offset]+value.to_bytes(size,'little')+raw[offset+size:]
                original=int.from_bytes(raw[offset:offset+size],'little');assert fmt.set_scalar(patched,'Effects',field,original)==raw
                records=list(s.records);records[ri]=patched;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
                packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+ri*246+offset
                assert packed==data[:at]+value.to_bytes(size,'little')+data[at+size:] and clone.header==s.header
                assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
                digest.update(hashlib.sha256(packed).digest());edits+=1
        assert sha(p.read_bytes())==row['sha256'];sources.append(dict(file=str(p),sha256=row['sha256']))
        rows.append(dict(file=str(p),sha256=row['sha256'],records=len(s.records),selected_indices=selected))
    assert len(rows)==82
    return dict(phase=158,files=rows,source_files=sources,records=total,original_modes={str(k):v for k,v in sorted(original_modes.items())},
        mode2_records=mode2,mode2_record_count=len(mode2),exact_container_edits=edits,public_decode_rebuild_controls=edits,
        all_other_bytes_preserved=True,inverse_scalar_edits_restore_source=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),
        scope='Onlystructuralbyte edits;65535slotboundaryisnotavalidnewcatalogueIDclaim. AllFlags/MiscEE/F0/F4/headers/links/order retained. Real mode2 catalogue rows audited; preparednativeparents are separate diagnostics.')
if __name__=='__main__':
    d=validate();(ROOT/'phase158-adjacent-storage-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print('files',len(d['files']),'records',d['records'],'mode2 records',d['mode2_record_count'],'edits',d['exact_container_edits']);print('standalone examples',[r for r in d['mode2_records'] if Path(r['file']).name=='Effects.kwd'][:8])
