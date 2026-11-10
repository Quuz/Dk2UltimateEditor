"""Exact in-memory edits for mode5/6 signed Data1, retaining rawu32 storage."""
from pathlib import Path
import json,hashlib,struct,collections
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'area-damage-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    assert len(refs)==82
    rows=[];records=0;counts=collections.Counter();digest=hashlib.sha256()
    fields={f['name']:f for f in fmt.catalogue_layout('Shots')['fields']}
    assert fields['Data1']['kind']=='u32' and fields['Data1']['offset']==0x98
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==0x104)
        records+=len(s.records);counts.update(rec[0xC5] for rec in s.records)
        outcomes=[]
        for mode in [5,6]:
            raw=s.records[0];edited=raw
            values=dict(Data1=0xFFFFFFB0,Data2=2,DamageType=mode,ProcessType=0)
            for name,value in values.items():edited=fmt.set_scalar(edited,'Shots',name,value)
            exact=bytearray(raw);struct.pack_into('<II',exact,0x98,0xFFFFFFB0,2);exact[0xC5]=mode;exact[0xC7]=0
            assert edited==exact and int.from_bytes(edited[0x98:0x9C],'little',signed=True)==-80
            restored=edited
            for name in values:
                f=fields[name];original=int.from_bytes(raw[f['offset']:f['offset']+f['size']],'little')
                restored=fmt.set_scalar(restored,'Shots',name,original)
            assert restored==raw
            rr=list(s.records);rr[0]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))
            expected=bytearray(data);start=s.offset+len(s.header);expected[start:start+len(raw)]=edited
            assert packed==expected
            doc=fmt.decoded(clone);decoded=doc['records'][0]['fields']
            assert decoded['Data1']==0xFFFFFFB0 and decoded['Data2']==2 and decoded['DamageType']['raw']==mode and decoded['ProcessType']['raw']==0
            assert fmt.rebuild_decoded(dict(sections=[doc]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());outcomes.append(dict(mode=mode,changed_bytes=sum(a!=b for a,b in zip(data,packed))))
        assert sha(p.read_bytes())==row['sha256'];rows.append(dict(**row,edits=outcomes))
    return dict(phase=148,files=rows,source_records=records,damage_mode_counts={str(k):v for k,v in sorted(counts.items())},
        new_exact_container_edits=2*len(rows),public_decode_rebuild_controls=2*len(rows),
        changed_raw_spans=[[0x98,8],[0xC5,1],[0xC7,1]],all_other_bytes_preserved=True,inverse_patches_restore_original=True,
        physical_Data1_storage='u32 unchanged; signed interpretation belongs to the selected damage handler',
        ordered_patch_digest=digest.hexdigest(),source_writes=0,current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))

if __name__=='__main__':
    d=validate();(ROOT/'phase148-shot-lifecycle-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(d['new_exact_container_edits'],'exact container edits;',d['source_records'],'source records; modes',d['damage_mode_counts'])
