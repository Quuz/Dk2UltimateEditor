"""Exact Keeper rule edits, unsupported-ID preservation and complete-container rebuild."""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def validate():
    refs=json.loads((ROOT/'keeper-array-semantics.json').read_text(encoding='utf8'))['corpus']['files'];assert len(refs)==82
    counts=Counter();casts=Counter();targets=Counter();rows=[];digest=hashlib.sha256()
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==150);counts['records']+=len(s.records)
        casts.update(r[0xEF] for r in s.records);targets.update(r[0xDA] for r in s.records)
        index=next(i for i,r in enumerate(s.records) if r[0xEE]==1);raw=s.records[index]
        for cast,target in [(1,6),(5,5),(255,255)]:
            edited=fmt.set_scalar(fmt.set_scalar(raw,'KeeperSpells','CastRule',cast),'KeeperSpells','TargetRule',target)
            exact=bytearray(raw);exact[0xEF]=cast;exact[0xDA]=target;assert edited==exact
            assert edited[0xC8:0xCC]==raw[0xC8:0xCC]
            inverse=fmt.set_scalar(fmt.set_scalar(edited,'KeeperSpells','CastRule',raw[0xEF]),'KeeperSpells','TargetRule',raw[0xDA]);assert inverse==raw
            records=list(s.records);records[index]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else part.pack() for i,part in enumerate(parts));at=s.offset+len(s.header)+index*s.stride
            exact=bytearray(data);exact[at:at+len(raw)]=edited;assert packed==exact
            decoded=fmt.decoded(clone);assert fmt.rebuild_decoded(dict(sections=[decoded]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());counts['edits']+=1
        assert sha(p.read_bytes())==row['sha256'];rows.append(dict(file=row['file'],sha256=row['sha256'],records=len(s.records)))
    assert counts['records']==1066 and counts['edits']==246
    return dict(phase=152,files=rows,counts=dict(counts),CastRule_counts={str(k):v for k,v in sorted(casts.items())},TargetRule_counts={str(k):v for k,v in sorted(targets.items())},
        all_other_bytes_preserved=True,C8_unchanged=True,unsupported_ID255_preserved=True,inverse_restores_source=True,
        exact_container_edits=counts['edits'],public_decode_rebuild_controls=counts['edits'],ordered_patch_digest=digest.hexdigest(),source_writes=0,
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))
if __name__=='__main__':
    d=validate();(ROOT/'phase152-keeper-validation-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(d['counts'],d['CastRule_counts'],d['TargetRule_counts'])
