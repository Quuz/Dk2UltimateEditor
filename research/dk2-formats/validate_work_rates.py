"""Whole Creature containers retain all other bytes under work-rate patches."""
import hashlib
import json
from pathlib import Path
import dk2_formats as fmt
import native_work_rates as api

ROOT=Path(__file__).resolve().parent


def main():
    source=json.loads((ROOT/'creature-lookup-review.json').read_text(encoding='utf8'))['corpus']['files']
    files=[];views=patches=changed=0;aggregate=hashlib.sha256()
    for row in source:
        data=Path(row['file']).read_bytes();assert hashlib.sha256(data).hexdigest()==row['sha256']
        parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        hits=[(i,s) for i,s in enumerate(parts) if s.signature==0xAA]
        for _,s in hits:
            for raw in s.records:assert api.patch(raw,**api.view(raw))==raw;views+=1
        si,s=hits[0];raw=s.records[0]
        for name,mask in [('research',0x80),('research',0x8000),('manufacture',0x80),('manufacture',0x8000),('research',0)]:
            edited=api.patch(raw,**{name:api.view(raw)[name]^mask});records=list(s.records);records[0]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else z.pack() for i,z in enumerate(parts))
            wanted=bytearray(data);o=s.offset+len(s.header)+api.FIELDS[name]
            wanted[o:o+2]=edited[api.FIELDS[name]:api.FIELDS[name]+2]
            assert packed==wanted and fmt.parse(packed)[si].records==records
            patches+=1;changed+=int(packed!=data);aggregate.update(hashlib.sha256(packed).digest())
        files.append(row)
    bad=[lambda:api.patch(bytes(5448)),lambda:api.patch(bytearray(5449)),lambda:api.patch(bytes(5449),other=1),
         lambda:api.patch(bytes(5449),research=-1),lambda:api.patch(bytes(5449),manufacture=65536),
         lambda:api.patch(bytes(5449),research=True),lambda:api.loaded_rate(1,0),lambda:api.contribution(1,20,0)]
    refused=0
    for fn in bad:
        try:fn()
        except ValueError:refused+=1
        else:raise AssertionError('Expected API refusal')
    assert len(files)==82 and views==3734 and patches==410 and changed==328 and refused==8
    out=dict(files=files,identity_records=views,exact_rebuilds=patches,changed=changed,identity=patches-changed,
             API_refusals=refused,ordered_patch_digest=aggregate.hexdigest(),source_writes=0)
    (ROOT/'phase116-work-rate-api-validation.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    print(len(files),'sources;',views,'records;',patches,'rebuilds;',refused,'refusals')


if __name__=='__main__':main()
