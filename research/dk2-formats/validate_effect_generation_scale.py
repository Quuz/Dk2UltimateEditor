"""Precise count+scale-bit edits in all82 supplied Effects containers."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_effect_generation_scale import patch_generation_count

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    corpus=json.loads((ROOT/'phase27-validation.json').read_text(encoding='utf8'))['corpus']['file_details']
    files=[dict(file=str(DATA/'maps'/r['file']),sha256=r['sha256']) for r in corpus]
    prior=json.loads((ROOT/'element-palette-chain-semantics.json').read_text(encoding='utf8'))['preservation']['source_files']
    known={str(Path(r['file'])).lower():r['sha256'] for r in prior}
    for path in [DATA/'Effects.kwd',DATA/'AutoSaveGlobals.kld']:
        assert str(path).lower() in known
        files.append(dict(file=str(path),sha256=known[str(path).lower()]))
    rows=[];hist=Counter();records_total=0;digest=hashlib.sha256()
    for row in files:
        path=Path(row['file']);data=path.read_bytes();assert sha(data)==row['sha256'];ss=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(ss) if s.signature==270 and s.records)
        for raw in s.records:
            records_total+=1;hist[(raw[0xF2],bool(int.from_bytes(raw[0xB4:0xB8],'little')&0x4000))]+=1
        ri=0;raw=s.records[ri];flags=int.from_bytes(raw[0xB4:0xB8],'little');newcount=raw[0xF3]^0xA7
        edited=patch_generation_count(raw,newcount,use_scale=not bool(flags&0x4000))
        assert patch_generation_count(edited,raw[0xF3],use_scale=bool(flags&0x4000))==raw
        records=list(s.records);records[ri]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else part.pack() for i,part in enumerate(ss))
        expected=bytearray(data);at=s.offset+len(s.header)+ri*246
        expected[at+0xF3]=newcount;expected[at+0xB4:at+0xB8]=(flags^0x4000).to_bytes(4,'little')
        assert packed==expected and fmt.parse(packed)[si].records==records
        assert raw[0xF4:0xF6]==edited[0xF4:0xF6] and raw[0xEE:0xF2]==edited[0xEE:0xF2]
        assert sha(path.read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(**row,record_index=ri,effect_id=int.from_bytes(raw[0xB8:0xBA],'little')))
    assert len(rows)==82
    return dict(phase=141,files=rows,new_exact_container_edits=82,inverse_record_edits=82,
        changed_fields=['Only rawF3 andbit4000 ofrawDWORD_B4'],all_other_container_bytes_preserved=True,source_writes=0,
        ordered_patch_digest=digest.hexdigest(),source_records=records_total,
        generation_type_scale_bit_histogram=[dict(type=t,scale_bit=s,records=n) for (t,s),n in sorted(hist.items())])

if __name__=='__main__':
    v=validate();(ROOT/'phase141-effect-scale-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(v['new_exact_container_edits'],'exact container edits;',v['source_records'],'source records')
