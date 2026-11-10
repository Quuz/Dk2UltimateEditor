"""Preserve all unrelated container/record bytes while editing each resistance slot."""
from pathlib import Path
import hashlib
import json
import dk2_formats as fmt
import native_creature_resistance as api

ROOT=Path(__file__).resolve().parent


def main():
    sources=json.loads((ROOT/'creature-lookup-review.json').read_text(encoding='utf8'))['corpus']['files']
    rows=[];views=patches=changes=0;aggregate=hashlib.sha256()
    for row in sources:
        data=Path(row['file']).read_bytes();assert hashlib.sha256(data).hexdigest()==row['sha256']
        parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        hits=[(i,s) for i,s in enumerate(parts) if s.signature==0xAA]
        for _,section in hits:
            for raw in section.records:
                for item in api.view(raw):assert api.patch(raw,item['slot'],attack_type=item['attack_type'],value=item['value_bits'])==raw
                views+=1
        si,section=hits[0];raw=section.records[0]
        for slot in range(5):
            edited=api.patch(raw,slot,value=raw[0xE0D+2*slot]^0x80) if slot<4 else api.patch(raw,0)
            records=list(section.records);records[0]=edited
            clone=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))
            want=bytearray(data)
            if slot<4:want[section.offset+len(section.header)+0xE0D+2*slot]^=0x80
            assert packed==want and fmt.parse(packed)[si].records==records
            patches+=1;changes+=int(packed!=data);aggregate.update(hashlib.sha256(packed).digest())
        rows.append(row)
    refusals=0
    for fn in [lambda:api.patch(bytes(5448),0,value=1),lambda:api.patch(bytes(5449),4,value=1),
               lambda:api.patch(bytes(5449),True,value=1),lambda:api.patch(bytes(5449),0,value=-1),
               lambda:api.patch(bytes(5449),0,value=256),lambda:api.patch(bytes(5449),0,value=True),
               lambda:api.patch(bytearray(5449),0,value=1)]:
        try:fn()
        except ValueError:refusals+=1
        else:raise AssertionError('Expected API refusal')
    assert len(rows)==82 and views==3734 and patches==410 and changes==328 and refusals==7
    out=dict(files=rows,identity_record_views=views,exact_container_rebuilds=patches,changed_containers=changes,
             identity_containers=patches-changes,API_refusals=refusals,ordered_patch_digest=aggregate.hexdigest(),
             scope='Four independent slot-value edits and one identity per source; no source writes; headers/order/opaque bytes preserved.')
    (ROOT/'phase113-resistance-api-validation.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    print('passed:',len(rows),'sources;',views,'identity views;',patches,'rebuilds;',refusals,'refusals')


if __name__=='__main__':main()
