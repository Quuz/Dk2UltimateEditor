"""Whole-container exact preservation under four independent Shot field edits."""
import hashlib
import json
from pathlib import Path
import dk2_formats as fmt
import native_shot_damage as api

ROOT=Path(__file__).resolve().parent


def main():
    sources=json.loads((ROOT/'shot-creation-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    rows=[];views=patches=changed=0;aggregate=hashlib.sha256()
    for row in sources:
        data=Path(row['file']).read_bytes();assert hashlib.sha256(data).hexdigest()==row['sha256']
        parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        hits=[(i,s) for i,s in enumerate(parts) if s.signature==0x104];assert hits
        for _,s in hits:
            for raw in s.records:
                assert api.patch(raw,**api.view(raw))==raw;views+=1
        si,s=hits[0];raw=s.records[0]
        for field in [*api.FIELDS,None]:
            changes={} if field is None else {field:api.view(raw)[field]^0x80}
            edited=api.patch(raw,**changes);records=list(s.records);records[0]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else z.pack() for i,z in enumerate(parts))
            want=bytearray(data)
            if field is not None:want[s.offset+len(s.header)+api.FIELDS[field][0]]^=0x80
            assert packed==want and fmt.parse(packed)[si].records==records
            patches+=1;changed+=int(packed!=data);aggregate.update(hashlib.sha256(packed).digest())
        rows.append(row)
    refusals=0
    for fn in [lambda:api.patch(bytes(238)),lambda:api.patch(bytearray(239)),
               lambda:api.patch(bytes(239),unknown=1),lambda:api.patch(bytes(239),data1=-1),
               lambda:api.patch(bytes(239),data1=1<<32),lambda:api.patch(bytes(239),attack_category=256),
               lambda:api.patch(bytes(239),damage_type=True),lambda:api.patch(bytes(239),data2=1.5)]:
        try:fn()
        except ValueError:refusals+=1
        else:raise AssertionError('Expected refusal')
    assert len(rows)==82 and patches==410 and changed==328 and refusals==8
    v=dict(files=rows,identity_records=views,exact_container_rebuilds=patches,changed=changed,
           identity=patches-changed,API_refusals=refusals,ordered_patch_digest=aggregate.hexdigest(),
           scope=__doc__,source_writes=0)
    (ROOT/'phase114-shot-damage-api-validation.json').write_text(json.dumps(v,indent=2),encoding='utf8')
    print(len(rows),'files;',views,'identity records;',patches,'exact rebuilds;',refusals,'refusals')


if __name__=='__main__':main()
