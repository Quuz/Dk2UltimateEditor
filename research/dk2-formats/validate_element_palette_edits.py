"""82 precise paired unknown-byte edits; preserve every other container byte."""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
from native_element_initialization import patch_raw

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    files=json.loads((ROOT/'element-initialization-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    rows=[];hist=Counter();records_total=0;digest=hashlib.sha256()
    for row in files:
        data=Path(row['file']).read_bytes();assert sha(data)==row['sha256'];sections=fmt.parse(data)
        hits=[]
        for si,s in enumerate(sections):
            if s.signature!=250:continue
            for ri,r in enumerate(s.records):
                assert len(r)==182;hist[tuple(r[0xB1:0xB3])]+=1;records_total+=1
                if int.from_bytes(r[0xA0:0xA2],'little')==294:hits.append((si,ri,r))
        if not hits:
            si,s=next((i,s) for i,s in enumerate(sections) if s.signature==250 and s.records);hits=[(si,0,s.records[0])]
        assert len(hits)==1;si,ri,raw=hits[0];s=sections[si]
        new=patch_raw(patch_raw(raw,0xB1,0x12),0xB2,0xE7)
        assert patch_raw(patch_raw(new,0xB1,raw[0xB1]),0xB2,raw[0xB2])==raw
        records=list(s.records);records[ri]=new;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else p.pack() for i,p in enumerate(sections))
        expected=bytearray(data);at=s.offset+len(s.header)+ri*182;expected[at+0xB1:at+0xB3]=bytes([0x12,0xE7])
        assert packed==expected and fmt.parse(packed)[si].records==records
        assert sha(Path(row['file']).read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=row['file'],sha256=row['sha256'],record_index=ri,element_id=int.from_bytes(raw[0xA0:0xA2],'little')))
    assert len(rows)==82
    return dict(phase=140,files=rows,new_exact_container_edits=82,inverse_record_edits=82,
        changed_raw_span=[0xB1,2],diagnostic_values=[0x12,0xE7],all_other_container_bytes_preserved=True,source_writes=0,
        ordered_patch_digest=digest.hexdigest(),source_records=records_total,
        source_unknown_pair_histogram=[dict(values=list(k),records=v) for k,v in sorted(hist.items())],
        scope='One combined two-byte diagnostic edit percontainer; this does not assign palette semantics or claim native support for everypossible pair.')

if __name__=='__main__':
    v=validate();(ROOT/'phase140-element-palette-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(len(v['files']),'exact paired unknown-byte edits;',v['source_records'],'records surveyed;',v['source_unknown_pair_histogram'])
