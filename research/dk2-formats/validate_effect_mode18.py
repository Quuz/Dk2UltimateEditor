"""Mode18 count/first-four edits preserve every other source-container byte."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_effect_mode18 import patch,requested_ids

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    files=json.loads((ROOT/'phase141-effect-scale-api-validation.json').read_text(encoding='utf8'))['files']
    rows=[];records=0;digest=hashlib.sha256()
    for row in files:
        path=Path(row['file']);data=path.read_bytes();assert sha(data)==row['sha256'];ss=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(ss) if s.signature==270)
        mode18=[(i,r) for i,r in enumerate(s.records) if r[0xF2]==18];records+=len(mode18);ri,raw=mode18[0]
        values=[0x1201,0,0xE7A9,0x00FF];edited=patch(raw,elements_per_turn=8,first_four=values)
        oldslots=[fmt.u16(raw,0xCA+2*i) for i in range(4)]
        assert patch(edited,elements_per_turn=raw[0xF3],first_four=oldslots)==raw
        assert requested_ids(edited,lifetime=1)==list(reversed([v for v in values if v]))*2
        assert requested_ids(edited,lifetime=0)==[]
        rr=list(s.records);rr[ri]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
        packed=b''.join(clone.pack() if i==si else part.pack() for i,part in enumerate(ss))
        expected=bytearray(data);at=s.offset+len(s.header)+ri*246
        expected[at+0xCA:at+0xD2]=b''.join(v.to_bytes(2,'little') for v in values);expected[at+0xF3]=8
        assert packed==expected and fmt.parse(packed)[si].records==rr
        assert edited[0xD2:0xF3]==raw[0xD2:0xF3] and edited[0xF4:]==raw[0xF4:]
        assert sha(path.read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=row['file'],sha256=row['sha256'],record_index=ri,effect_id=fmt.u16(raw,0xB8)))
    assert len(rows)==82 and records==492
    return dict(phase=143,files=rows,new_exact_container_edits=82,inverse_record_edits=82,source_mode18_records=records,
        changed_spans=[[0xCA,8],[0xF3,1]],all_other_container_bytes_preserved=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        scope='Explicit diagnostic slotIDs preserveu16 storage; no claim absent diagnosticIDs are valid gameplay references.')

if __name__=='__main__':
    v=validate();(ROOT/'phase143-effect-mode18-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(v['new_exact_container_edits'],'exact containers;',v['source_mode18_records'],'source mode18 records')
