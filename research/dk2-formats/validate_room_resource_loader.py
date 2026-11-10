"""Exact four-resource container edits and source metadata survey; no disk edits."""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
from native_room_writer import patch_unknown_resource
from native_room_resource_loader import patch_resource,SPANS
from probe_room_resource_loader import opaque_resource
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'room-writer-semantics.json').read_text())['source_corpus']['files'];assert len(refs)==82
    rows=[];records=0;digest=hashlib.sha256();populated=Counter();names=Counter()
    replacement=opaque_resource()
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==0x78);records+=len(s.records)
        for raw in s.records:
            for off in SPANS:
                if any(raw[off:off+84]):populated[str(off)]+=1
                if raw[off:off+64].split(b'\0',1)[0]:names[str(off)]+=1
        edits=[]
        for off in SPANS:
            raw=s.records[0];changed=patch_resource(raw,off,replacement)
            assert changed[:off]==raw[:off] and changed[off+84:]==raw[off+84:] and changed[off:off+84]==replacement
            assert patch_resource(changed,off,raw[off:off+84])==raw
            if off==0x1C4:assert changed==patch_unknown_resource(raw,replacement)
            rr=list(s.records);rr[0]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))
            exact=bytearray(data);at=s.offset+len(s.header)+off;exact[at:at+84]=replacement;assert packed==exact
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());edits.append(dict(raw_offset=off,changed_bytes=sum(a!=b for a,b in zip(data,packed))))
        assert sha(p.read_bytes())==row['sha256'];rows.append(dict(**row,edits=edits))
    return dict(phase=150,files=rows,source_Room_records=records,new_exact_container_edits=4*len(rows),public_decode_rebuild_controls=4*len(rows),
        resource_spans=SPANS,resource_bytes=84,nonzero_resource_records=dict(populated),nonempty_name_records=dict(names),
        inverse_patches_restore_original=True,all_other_bytes_preserved=True,ordered_patch_digest=digest.hexdigest(),source_writes=0,
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))

if __name__=='__main__':
    d=validate();(ROOT/'phase150-room-resource-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(d['new_exact_container_edits'],'edits;',d['source_Room_records'],'records;',d['nonzero_resource_records'])
