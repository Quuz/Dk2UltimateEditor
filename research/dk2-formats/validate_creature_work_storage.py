"""Exact complete-container CF2/CEE edits includingextendedCreaturetails; no source writes."""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
import native_creature_work_level as api
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def validate():
    refs=json.loads((ROOT/'creature-definition-semantics.json').read_text(encoding='utf8'))['corpus']['files']
    rows=[];total=edits=extended=public=0;hist=Counter();digest=hashlib.sha256()
    for source in refs:
        p=Path(source['file']);data=p.read_bytes();assert sha(data)==source['sha256'];parts=fmt.parse(data);si,s=next((i,s) for i,s in enumerate(parts) if s.signature==170);total+=len(s.records)
        for raw in s.records:
            values=api.view(raw);hist[(values['default'],values['level_increase'])]+=1
        raw=s.records[0]
        for key,value,field in [('level_increase',1024,'Unkcf2'),('level_increase',0xFFFFFFFF,'Unkcf2'),('default',0x89ABCDEF,'Unkcee')]:
            edited=api.patch(raw,key,value);off=api.FIELDS[key];assert edited==raw[:off]+value.to_bytes(4,'little')+raw[off+4:]
            assert api.patch(edited,key,api.view(raw)[key])==raw
            if len(raw)==5449:assert edited==fmt.set_scalar(raw,'Creatures',field,value);public+=1
            else:
                assert len(raw)==5537 and edited[5449:]==raw[5449:];extended+=1
                try:fmt.set_scalar(raw,'Creatures',field,value)
                except ValueError as e:assert str(e)=='Record/schema size mismatch'
                else:raise AssertionError('Expected historicalgenericcodec sizeguard')
            records=list(s.records);records[0]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+off
            assert packed==data[:at]+value.to_bytes(4,'little')+data[at+4:] and clone.header==s.header
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack();digest.update(hashlib.sha256(packed).digest());edits+=1
        assert sha(p.read_bytes())==source['sha256'];rows.append(dict(file=str(p),sha256=source['sha256'],records=len(s.records),selected_record_bytes=len(raw)))
    assert len(rows)==82 and total==3734 and edits==246
    refusals=0
    for field,value in [('level_increase',-1),('level_increase',2**32),('default',True),('default',1.5),('unknown',0)]:
        try:api.patch(raw,field,value)
        except ValueError:refusals+=1
        else:raise AssertionError((field,value))
    return dict(phase=162,files=rows,records=total,exact_changed_container_rebuilds=edits,public_decode_rebuild_controls=edits,
        generic_scalar_controls=public,extended_record_helper_controls=extended,invalid_argument_refusals=refusals,
        source_pair_histogram=[dict(rawCEE=a,rawCF2=b,records=n) for (a,b),n in sorted(hist.items())],
        all_other_bytes_preserved=True,extended_tails_preserved=True,inverse_edits_restore_source=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),scope=__doc__,
        limitation='Narrowhelper handlesknown5537variant; genericcodecguard unchanged. CEEgameplayinitializationunresolved; nativelevel-upCF2testsseparatefromsourcecorrelation.')
if __name__=='__main__':
    d=validate();(ROOT/'phase162-creature-work-storage-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print(len(d['files']),'files',d['records'],'records',d['exact_changed_container_rebuilds'],'edits','histogram',d['source_pair_histogram'])
