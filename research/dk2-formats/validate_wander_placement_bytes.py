"""Precise raw WanderRadius byte edits, without assuming gameplay units."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_placed_creatures import patch_raw
from native_hero_parties import patch_member

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    files=json.loads((ROOT/'phase75-wander-corpus.json').read_text(encoding='utf8'))['files'];rows=[];skipped=[];digest=hashlib.sha256()
    for row in files:
        path=Path(row['file']);data=path.read_bytes();assert sha(data)==row['sha256'];ss=fmt.parse(data);hit=None
        for si,s in enumerate(ss):
            if s.signature!=190:continue
            for ri,r in enumerate(s.records):
                tag=fmt.u32(r)
                if tag in [198,199]:
                    offset=23 if tag==198 else 29
                    hit=(si,ri,r,8+offset,patch_raw(r,offset,53),tag,None);break
                if tag==201 and fmt.u32(r,4)==555:
                    slot=next((slot for slot in range(16) if r[8+43+32*slot+28]),None)
                    if slot is not None:
                        hit=(si,ri,r,8+43+32*slot+29,patch_member(r,slot,29,53),tag,slot);break
            if hit:break
        if hit is None:skipped.append(dict(file=row['file'],sha256=row['sha256'],reason='No occupied supported WanderRadius carrier'));continue
        si,ri,raw,offset,edited,tag,slot=hit;s=ss[si]
        restored=patch_raw(edited,offset-8,raw[offset]) if tag in [198,199] else patch_member(edited,slot,29,raw[offset])
        assert restored==raw
        rr=list(s.records);rr[ri]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
        packed=b''.join(clone.pack() if i==si else part.pack() for i,part in enumerate(ss))
        expected=bytearray(data);at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+offset;expected[at]=53
        assert packed==expected and fmt.parse(packed)[si].records==rr
        assert sha(path.read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=row['file'],sha256=row['sha256'],record_index=ri,record_tag=tag,party_slot=slot,full_record_byte_offset=offset))
    assert len(rows)+len(skipped)==109
    return dict(phase=144,files=rows,skipped=skipped,source_files=109,new_exact_container_edits=len(rows),inverse_record_edits=len(rows),
        changed_value=53,all_other_container_bytes_preserved=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        scope='Physical BYTE storage edits only; no units or functional mapping from thisbyte tostate87 hardcoded radius claimed.')

if __name__=='__main__':
    v=validate();(ROOT/'phase144-wander-placement-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(v['new_exact_container_edits'],'exact carrier-container edits;',len(v['skipped']),'files without supported occupied carrier')
