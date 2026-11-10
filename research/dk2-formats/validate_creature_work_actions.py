"""Exact whole-container Variables8/9/10value edits in currentCreature-linked source containers."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_creature_work_cell import patch_action_value
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def validate():
    refs=json.loads((ROOT/'creature-definition-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    core=json.loads((ROOT/'creature-work-level-semantics.json').read_text(encoding='utf8'))['preservation']['source_files']
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
    own=next(row for row in core if Path(row['file'])==source);refs=refs+[own]
    rows=[];counts={8:0,9:0,10:0};digest=hashlib.sha256();edits=0
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data);items=[(i,s) for i,s in enumerate(parts) if s.signature==230]
        if not items:continue
        selected=0
        for si,s in items:
            for typ in [8,9,10]:
                candidates=[(i,r) for i,r in enumerate(s.records) if len(r)==16 and int.from_bytes(r[:4],'little')==typ]
                if not candidates:continue
                ri,raw=candidates[0];new=patch_action_value(raw,0xFFFFFF9C);assert new==raw[:4]+bytes.fromhex('9cffffff')+raw[8:]
                assert patch_action_value(new,int.from_bytes(raw[4:8],'little'))==raw
                records=list(s.records);records[ri]=new;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
                packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+16*ri+4
                assert packed==data[:at]+bytes.fromhex('9cffffff')+data[at+4:] and clone.header==s.header
                assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
                digest.update(hashlib.sha256(packed).digest());counts[typ]+=1;edits+=1;selected+=1
        assert sha(p.read_bytes())==row['sha256']
        rows.append(dict(file=str(p),sha256=row['sha256'],selected_records=selected))
    assert rows and edits==sum(counts.values()) and all(n>0 for n in counts.values())
    return dict(phase=163,files=rows,file_count=len(rows),records_by_type={str(k):v for k,v in counts.items()},exact_container_edits=edits,
        public_decode_rebuild_controls=edits,all_other_bytes_preserved=True,inverse_edits_restore_source=True,source_writes=0,
        ordered_patch_digest=digest.hexdigest(),scope=__doc__,limitations='Firstphysicalrecordofeachselectedtypepatchedexplicitly; no duplicate-resolution/order rewrite. Native effects testedseparately withactualsourcecallbackrecords.')
if __name__=='__main__':
    d=validate();(ROOT/'phase163-creature-work-action-storage.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(d['file_count'],'sources',d['records_by_type'],d['exact_container_edits'],'exactvalueedits')
