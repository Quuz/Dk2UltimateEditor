"""Append and reorder complete Door catalogues in memory, preserving opaque bytes and other sections."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'phase142-door-split-api-validation.json').read_text(encoding='utf8'))
    sources=refs.get('source_files',refs.get('files'))
    assert isinstance(sources,list)
    rows=[];edits=0;total=0;digest=hashlib.sha256()
    for source in sources:
        p=Path(source['file']);data=p.read_bytes();assert sha(data)==source['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==140);assert all(len(r)==616 for r in s.records);total+=len(s.records)
        ident=max(r[0x244] for r in s.records)+1;assert 1<=ident<=255
        appended=fmt.set_scalar(fmt.set_scalar(s.records[0],'Doors','DoorId',ident),'Doors','Unknown236',0xBEEF)
        wanted=bytearray(s.records[0]);wanted[0x244]=ident;wanted[0x236:0x238]=bytes.fromhex('efbe');assert appended==wanted
        for mode,records in [('append',list(s.records)+[appended]),('reverse',list(reversed(s.records)))]:
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))
            head=bytearray(s.header)
            if mode=='append':
                for off,inc in [(8,616),(20,1),(52,616)]:head[off:off+4]=(int.from_bytes(head[off:off+4],'little')+inc).to_bytes(4,'little')
            assert clone.header==head
            expected=data[:s.offset]+bytes(head)+b''.join(records)+s.suffix+data[s.offset+len(s.pack()):]
            assert packed==expected and fmt.parse(packed)[si].records==records
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());edits+=1
        assert sha(p.read_bytes())==source['sha256'];rows.append(dict(file=str(p),sha256=source['sha256'],records=len(s.records),appended_ID=ident))
    assert len(rows)==82 and total==656 and edits==164
    return dict(phase=160,files=rows,records=total,exact_changed_container_rebuilds=edits,append_controls=82,reorder_controls=82,
        public_decode_rebuild_controls=edits,only_append_updates_S_N_D=True,all_other_records_headers_suffixes_and_unknowns_preserved=True,
        physical_record_order_retained=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        scope=__doc__,limitation='Storage diagnostics, no game-file edits. ID9 etc are new diagnosticrows; runtime IDsort does not authorize normalizing physical order on save.')

if __name__=='__main__':
    d=validate();(ROOT/'phase160-door-catalogue-storage-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(len(d['files']),'Door sources',d['records'],'records',d['exact_changed_container_rebuilds'],'exact append/reorder controls')
