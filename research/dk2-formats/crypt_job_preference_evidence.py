"""Current source corpus and exact reversible preference container edits."""
from pathlib import Path
import hashlib,json,struct
import dk2_formats as fmt
import native_crypt_job_preference as api
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))

def corpus():
    refs=load('creature-lookup-review.json')['corpus']['files'];files=[];hits=[];records=0
    for ref in refs:
        file=Path(ref['file']);data=file.read_bytes();assert sha(data)==ref['sha256']
        sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
        for s in sections:
            if s.signature!=170:continue
            for ri,raw in enumerate(s.records):
                records+=1
                for kind,start,count,stride in [('happy',0xE14,3,12),('unhappy',0xE38,2,12),('angry',0xE50,3,12),('alternative',0xE7C,3,8)]:
                    for i in range(count):
                        job=int.from_bytes(raw[start+stride*i:start+stride*i+4],'little')
                        if job&255==28:hits.append(dict(file=str(file),source_sha256=sha(data),record=ri,record_sha256=sha(raw),
                            creature_id=raw[0xF4B],name=raw[:32].split(b'\0')[0].decode('ascii'),array=kind,slot=i,raw_job=job,
                            weight=raw[start+stride*i+8] if stride==12 else None))
        files.append(dict(file=str(file),sha256=sha(data)))
    assert len(files)==82 and records==3734 and len(hits)==3
    assert all(h['creature_id']==32 and h['name']=='[Unused] Firefly' and h['array']=='happy' and h['weight']==55 for h in hits)
    return dict(source_files=files,record_copies=records,Job28_occurrences=hits,
        caveat='Catalogueoccurrencesarecopiesofdefinitions,notproofthatCreature32isplaced/spawnedinallthesemaps.')

def storage():
    file=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd';data=file.read_bytes();parts=fmt.parse(data)
    si,s=next((i,s) for i,s in enumerate(parts) if s.signature==170)
    ri,raw=next((i,r) for i,r in enumerate(s.records) if r[0xF4B]==5)
    out=[];digest=hashlib.sha256()
    for array,(_,count) in api.ARRAYS.items():
        for index in range(count):
            for field,values in [('job',[0,0x11C,0xFFFFFFFF]),('weight',[0,100,255])]:
                for value in values:
                    at,width=api.offset(raw,api.DX,array,index,field);edited=api.patch(raw,api.DX,array,index,field,value)
                    assert edited[:at]==raw[:at] and edited[at+width:]==raw[at+width:] and len(edited)==len(raw)
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride)
                    records=list(s.records);records[ri]=edited;clone.replace_records(records)
                    packed=b''.join(clone.pack() if j==si else x.pack() for j,x in enumerate(parts));absolute=s.offset+len(s.header)+ri*s.stride+at
                    assert packed==data[:absolute]+value.to_bytes(width,'little')+data[absolute+width:]
                    assert clone.header==s.header and clone.suffix==s.suffix
                    assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(x) for x in fmt.parse(packed)]))==packed
                    records[ri]=api.patch(edited,api.DX,array,index,field,int.from_bytes(raw[at:at+width],'little'));clone.replace_records(records)
                    assert b''.join(clone.pack() if j==si else x.pack() for j,x in enumerate(parts))==data
                    digest.update(hashlib.sha256(packed).digest());out.append(dict(array=array,slot=index,field=field,value=value,offset=at,width=width))
    assert len(out)==30
    extended=raw+bytes((17*i+3)&255 for i in range(88));changed=api.patch(extended,api.DX,'unhappy',0,'job',28)
    assert changed[5449:]==extended[5449:] and len(changed)==5537
    return dict(file=str(file),sha256=sha(data),exact_inverse_container_edits=out,ordered_patch_digest=digest.hexdigest(),
        preserved_extra88_bytes=True,reserved3_bytes_preserved=True,source_writes=0)

def main():
    d=dict(corpus=corpus(),storage=storage())
    (ROOT/'phase188-preference-storage.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
    print('82sources/3734records/3Job28copies;30inversecontaineredits;unknownpaddingpreserved')

if __name__=='__main__':main()
