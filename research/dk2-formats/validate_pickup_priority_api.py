"""Exact raw Creature/Object priority-byte edits without normalizing unknowns."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_pickup_selection import patch_priority
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def validate():
    rows=[];counts={};digest=hashlib.sha256()
    for family,sig,schema,field,off in [('Creatures',170,'creature-definition-semantics.json','Unk3f2',0xF55),('Objects',240,'object-loading-semantics.json','PickUpPriority',0x35D)]:
        refs=json.loads((ROOT/schema).read_text(encoding='utf8'))['corpus']['files']
        if family=='Objects':refs=[r for r in refs if r['records'].get('Objects',0)]
        assert len(refs)==82;records=edits=public_scalar=extended=0
        for row in refs:
            p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data);si,s=next((i,s) for i,s in enumerate(parts) if s.signature==sig);records+=len(s.records);raw=s.records[0]
            for value in [0,255]:
                edited=patch_priority(raw,family,value);assert edited==raw[:off]+bytes([value])+raw[off+1:]
                assert patch_priority(edited,family,raw[off])==raw
                if len(raw)==fmt.LAYOUTS[family]['record_size']:
                    assert edited==fmt.set_scalar(raw,family,field,value);public_scalar+=1
                else:
                    assert family=='Creatures' and len(raw)==5537;extended+=1
                    try:fmt.set_scalar(raw,family,field,value)
                    except ValueError as e:assert str(e)=='Record/schema size mismatch'
                    else:raise AssertionError('Expected baseline public scalar size guard')
                rr=list(s.records);rr[0]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
                packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));expected=bytearray(data);expected[s.offset+len(s.header)+off]=value;assert packed==expected
                assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack();digest.update(hashlib.sha256(packed).digest());edits+=1
            assert sha(p.read_bytes())==row['sha256'];rows.append(dict(family=family,file=row['file'],sha256=row['sha256'],records=len(s.records)))
        counts[family]=dict(files=len(refs),records=records,edits=edits,public_scalar_controls=public_scalar,extended_record_helper_controls=extended)
    assert sum(c['edits'] for c in counts.values())==328
    return dict(phase=153,files=rows,counts=counts,exact_container_edits=328,public_decode_rebuild_controls=328,
        all_other_bytes_preserved=True,inverse_patches_restore_source=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        scalar_API_limit='Baseline fmt.set_scalar rejects5537-byteCreatures byrecord size. New narrowpatch_priority supports both knownlengths without changing baseline codec/effective schemas. All328 publicdecode/rebuild controls remainlossless.',
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))
if __name__=='__main__':
    d=validate();(ROOT/'phase153-pickup-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(d['counts'])
