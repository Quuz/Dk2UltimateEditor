"""82 exact in-memory Room construction edits; runtime cell byte preservation."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
import native_room_adjacency as api
import emulate_room_adjacency as native

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    files=json.loads((ROOT/'room-writer-semantics.json').read_text(encoding='utf8'))['source_corpus']['files'];rows=[];digest=hashlib.sha256()
    for row in files:
        data=Path(row['file']).read_bytes();assert sha(data)==row['sha256']
        sections=fmt.parse(data)
        hits=[(si,ri,r) for si,s in enumerate(sections) if fmt.FAMILIES.get(s.signature)=='Rooms' for ri,r in enumerate(s.records) if len(r)==1055 and r[0x392]==13]
        assert len(hits)==1;si,ri,raw=hits[0];s=sections[si];value=6 if raw[0x395]!=6 else 7
        new=api.patch_room_construction(raw,value);assert api.patch_room_construction(new,raw[0x395])==raw
        records=list(s.records);records[ri]=new
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
        expected=bytearray(data);start=s.offset+len(s.header)+ri*1055;expected[start+0x395]=value
        assert packed==expected and fmt.parse(packed)[si].records==records and packed!=data
        assert sha(Path(row['file']).read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest());rows.append(row)
    assert len(rows)==82
    runtime=[]
    for c in native.cases():
        args,checks=native.build(c);cells=checks[native.prior.MAP];x,y=c['xy'];at=16*(5*y+x)
        new=api.project_cell(cells,5,5,x,y,checks[native.ROOMRAW][0x394])
        assert new[:at+12]==cells[:at+12] and new[at+13:]==cells[at+13:]
        runtime.append(dict(case=c['name'],cellC=new[at+12],only_possible_changed_offset=at+12))
    refused=0
    for raw,value in [(bytes(1054),7),(bytes(1055),-1),(bytes(1055),256)]:
        try:api.patch_room_construction(raw,value)
        except ValueError:refused+=1
    assert refused==3
    return dict(phase=137,files=rows,new_exact_container_rebuilds=82,inverse_record_edits=82,raw_changed_offset=0x395,
        all_other_container_bytes_preserved=True,ordered_patch_digest=digest.hexdigest(),runtime_cell_projections=runtime,
        runtime_projection_is_not_raw_KLD_codec=True,invalid_patch_inputs_refused=refused,source_writes=0,
        scope='Construction6/7 edits are diagnostic memory copies, not a gameplay recommendation. 26 runtime projection-only checks accompany independent native26case oracles; do not count host projections as native executions.')

if __name__=='__main__':
    value=validate();(ROOT/'phase137-room-adjacency-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('82 exact Room construction container edits; 26 runtime-cell preservation checks')
