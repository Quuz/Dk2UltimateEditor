"""Exact public-codec whole-container patches for nativeStat19observed raw224bits."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'phase159-terrain-light-storage-validation.json').read_text(encoding='utf8'))['files']
    rows=[];edits=total=0;digest=hashlib.sha256()
    for row in refs:
        path=Path(row['file']);data=path.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==110);assert all(len(r)==552 for r in s.records);total+=len(s.records)
        ri=next(i for i,r in enumerate(s.records) if r[0x1D6]==1);raw=s.records[ri]
        for value in [0x3800,0xFFFFF000,1]:
            patched=fmt.set_scalar(raw,'Terrain','Unk224',value)
            assert patched==raw[:0x224]+struct.pack('<I',value)+raw[0x228:] and patched!=raw
            assert fmt.set_scalar(patched,'Terrain','Unk224',fmt.u32(raw,0x224))==raw
            records=list(s.records);records[ri]=patched
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else p.pack() for i,p in enumerate(parts));start=s.offset+len(s.header)+552*ri+0x224
            assert packed==data[:start]+struct.pack('<I',value)+data[start+4:] and clone.header==s.header
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());edits+=1
        assert sha(path.read_bytes())==row['sha256'];rows.append(dict(file=str(path),sha256=row['sha256'],records=len(s.records),selected_record_index=ri))
    assert len(rows)==82 and total==3306 and edits==246
    return dict(phase=165,files=rows,records=total,exact_changed_container_rebuilds=edits,public_decode_rebuild_controls=edits,
        raw224_values=[0x3800,0xFFFFF000,1],all_other_bytes_preserved=True,inverse_edits_restore_source=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),scope=__doc__,
        limitations='Physicalu32bitsonly; nopublicAPInegative-integer/scaledvaluecoercionintroduced. No newgameplaymeaning/unitsinferred.')

if __name__=='__main__':
    r=validate();(ROOT/'phase165-terrain-text-storage.json').write_text(json.dumps(r,indent=2),encoding='utf8');print(len(r['files']),r['records'],r['exact_changed_container_rebuilds'],'exactraw224edits')
