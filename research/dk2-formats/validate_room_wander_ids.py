"""Exact in-memory RoomId byte edits; no game files written or IDs renumbered."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Rooms.kwd'
def sha(b):return hashlib.sha256(b).hexdigest()
def validate():
    data=SOURCE.read_bytes();parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
    rows=[];catalogue=[];digest=hashlib.sha256()
    for si,s in enumerate(parts):
        if fmt.FAMILIES.get(s.signature)!='Rooms':continue
        for ri,raw in enumerate(s.records):
            assert len(raw)==1055;rid=raw[0x392]
            catalogue.append(dict(id=rid,name=raw[:32].split(b'\0')[0].decode('cp1252'),terrain_id=raw[0x394]))
            # A high-bit diagnostic marker verifies physical storage only.
            # It is not a supported game ID or a valid catalogue renumbering.
            marker=rid^0x80;edited=fmt.set_scalar(raw,'Rooms','RoomId',marker)
            assert [i for i,(a,b) in enumerate(zip(raw,edited)) if a!=b]==[0x392]
            assert fmt.set_scalar(edited,'Rooms','RoomId',rid)==raw
            records=list(s.records);records[ri]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else p.pack() for i,p in enumerate(parts))
            at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+0x392
            expected=bytearray(data);expected[at]=marker
            assert packed==expected and fmt.parse(packed)[si].records==records
            digest.update(hashlib.sha256(packed).digest())
            rows.append(dict(section_index=si,record_index=ri,original_id=rid,diagnostic_value=marker,container_offset=at))
    assert sorted(x['id'] for x in catalogue)==list(range(1,27))
    assert SOURCE.read_bytes()==data
    return dict(phase=145,source=dict(file=str(SOURCE),sha256=sha(data)),catalogue=catalogue,edits=rows,new_exact_container_edits=len(rows),inverse_record_edits=len(rows),
        all_other_bytes_preserved=True,ordered_patch_digest=digest.hexdigest(),source_writes=0,limitation='Diagnostic copy-only ID markers; no claim a changed ID preserves cross-file references or is accepted by game/editor.')
if __name__=='__main__':
    v=validate();(ROOT/'phase145-room-ID-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(v['new_exact_container_edits'],'exact RoomId byte edits')
