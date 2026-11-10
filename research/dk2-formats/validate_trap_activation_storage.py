"""Exact whole-container Unknown204 and signedManaUsage-bitpattern edits, no source writes."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'phase146-Trap-WORD-api-validation.json').read_text(encoding='utf8'))
    sources=refs.get('files',refs.get('source_files'));assert isinstance(sources,list)
    rows=[];edits=0;total=0;digest=hashlib.sha256()
    for source in sources:
        path=Path(source['file']);data=path.read_bytes();assert sha(data)==source['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if fmt.FAMILIES.get(s.signature)=='Traps');total+=len(s.records)
        ri=next(i for i,r in enumerate(s.records) if r[0x20C]==1);raw=s.records[ri]
        for field,off,value in [('Unknown204',0x204,0xBEEF),('ManaUsage',0x202,65535),('ManaUsage',0x202,32768)]:
            patched=fmt.set_scalar(raw,'Traps',field,value);assert patched==raw[:off]+value.to_bytes(2,'little')+raw[off+2:]
            assert fmt.set_scalar(patched,'Traps',field,int.from_bytes(raw[off:off+2],'little'))==raw
            records=list(s.records);records[ri]=patched;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+579*ri+off
            assert packed==data[:at]+value.to_bytes(2,'little')+data[at+2:] and clone.header==s.header
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());edits+=1
        assert sha(path.read_bytes())==source['sha256'];rows.append(dict(file=str(path),sha256=source['sha256'],records=len(s.records)))
    assert len(rows)==82 and total==1293 and edits==246
    return dict(phase=161,files=rows,records=total,exact_changed_container_rebuilds=edits,public_decode_rebuild_controls=edits,
        all_other_bytes_preserved=True,inverse_edits_restore_source=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),scope=__doc__,
        limitation='ManaUsage sourceAPI remains rawu16; FFFF/8000 are bits interpreted signed byactual consumer. NoUnknown204 signedness/meaning assigned.')

if __name__=='__main__':
    d=validate();(ROOT/'phase161-trap-activation-storage-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(len(d['files']),'files',d['records'],'Traprecords',d['exact_changed_container_rebuilds'],'exactedits')
