"""In-memory exact Variables102 edit and whole-source roundtrip checks."""
import json
import probe_crypt_active_action as p
ROOT=p.ROOT

def validate():
    rows=[]
    for name in ['Creatures.kwd','Objects.kwd','KeeperSpells.kwd','GlobalVariables.kwd']:
        file=p.SOURCE.parent/name;data=file.read_bytes();parts=p.prior.fmt.parse(data)
        assert b''.join(s.pack() for s in parts)==data
        assert p.prior.fmt.rebuild_decoded(dict(sections=[p.prior.fmt.decoded(s) for s in parts]))==data
        rows.append(dict(file=str(file),sha256=p.sha(data),bytes=len(data),exact_public_decode_rebuild=True))
    data=(p.SOURCE.parent/'GlobalVariables.kwd').read_bytes();parts=p.prior.fmt.parse(data)
    si,s=next((i,s) for i,s in enumerate(parts) if s.signature==230)
    ri,raw=next((i,r) for i,r in enumerate(s.records) if p.u(r,0)==102)
    assert len(raw)==16 and int.from_bytes(raw[4:8],'little',signed=True)==-275
    edited=raw[:4]+((-369)&0xFFFFFFFF).to_bytes(4,'little')+raw[8:]
    records=list(s.records);records[ri]=edited
    clone=p.prior.fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
    packed=b''.join(clone.pack() if i==si else section.pack() for i,section in enumerate(parts))
    at=s.offset+len(s.header)+16*ri+4
    assert packed==data[:at]+edited[4:8]+data[at+4:]
    assert clone.header==s.header and clone.suffix==s.suffix
    assert p.prior.fmt.rebuild_decoded(dict(sections=[p.prior.fmt.decoded(clone)]))==clone.pack()
    clone.replace_records(s.records);assert b''.join(clone.pack() if i==si else section.pack() for i,section in enumerate(parts))==data
    for row in rows:assert p.sha(p.Path(row['file']).read_bytes())==row['sha256']
    return dict(status='passed',source_files=rows,selected_variable=102,raw_value_offset=4,value_bytes=4,
        source_signed32=-275,edited_signed32=-369,exact_container_edits=1,unchanged_headers_suffix_other_records_and_reserved_bytes=True,
        inverse_edit_restores_source=True,source_writes=0,new_codec_field_names=0)

if __name__=='__main__':
    d=validate();(ROOT/'phase180-crypt-action-storage.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
    print(d['status'],len(d['source_files']),'source roundtrips',d['exact_container_edits'],'exact value edit')
