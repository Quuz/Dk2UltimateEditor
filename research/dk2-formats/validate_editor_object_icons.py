"""Exact full-container rebuilds for editor icon suffix edits; no source writes."""
from pathlib import Path
import hashlib,json
import dk2_formats as fmt
import native_editor_object_icons as api

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def main():
    sources=[r for r in json.loads((ROOT/'object-loading-semantics.json').read_text(encoding='utf8'))['corpus']['files'] if r['records'].get('Objects')]
    rows=[];views=identity_views=patches=changed=0;digest=hashlib.sha256();samples=[]
    for row in sources:
        data=Path(row['file']).read_bytes();assert sha(data)==row['sha256']
        parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        hits=[(i,s) for i,s in enumerate(parts) if s.signature==0xF0];assert hits
        for _,s in hits:
            for r in s.records:
                v=api.view(r);views+=1
                if v['canonical_icon_number'] is not None:
                    index=v['canonical_icon_number'];old=bytes.fromhex(v['name_bytes_hex'])
                    if old==bytes.fromhex(v['resource_prefix_bytes_hex'])+b'#'+str(index).encode():
                        assert api.patch_icon_index(r,index)==r;identity_views+=1
                if len(rows)==0 and len(samples)<12:
                    samples.append(dict(object_id=r[0x35A],name=fmt.string(r[:32]),resource_name_bytes=v['name_bytes_hex'],icon_index=v['canonical_icon_number']))
        si,s=hits[0];raw=s.records[0]
        for index in [0,15,65535,None]:
            edited=raw if index is None else api.patch_icon_index(raw,index)
            assert edited[:0xC8]==raw[:0xC8] and edited[0x108:]==raw[0x108:]
            end=edited[0xC8:0x108].find(b'\0')
            assert end>=0 and edited[0xC8+end+1:]==raw[0xC8+end+1:]
            records=list(s.records);records[0]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else z.pack() for i,z in enumerate(parts))
            want=bytearray(data);pos=s.offset+len(s.header);want[pos:pos+894]=edited
            assert packed==want and fmt.parse(packed)[si].records==records
            patches+=1;changed+=packed!=data;digest.update(hashlib.sha256(packed).digest())
        assert sha(Path(row['file']).read_bytes())==row['sha256'];rows.append(row)
    first=next(r for s in fmt.parse(Path(rows[0]['file']).read_bytes()) if s.signature==0xF0 for r in s.records)
    long=bytearray(first);long[0xC8:0x108]=b'A'*63+b'\0'
    bad=bytearray(first);bad[0xC8:0x108]=b'A'*64
    refusals=0
    for fn in [lambda:api.patch_icon_index(first,-1),lambda:api.patch_icon_index(first,65536),
        lambda:api.patch_icon_index(first,True),lambda:api.patch_icon_index(first,1.5),
        lambda:api.patch_icon_index(bytearray(first),2),lambda:api.view(first[:-1]),
        lambda:api.patch_icon_index(bytes(long),2),lambda:api.patch_icon_index(bytes(bad),2),
        lambda:api.icon_rect(1,0),lambda:api.icon_rect(1,True)]:
        try:fn()
        except ValueError:refusals+=1
        else:raise AssertionError('Invalid API input accepted')
    assert len(rows)==82 and patches==328 and refusals==10
    result=dict(scope=__doc__,files=rows,records_inspected=views,canonical_identity_records=identity_views,
        exact_container_rebuilds=patches,changed=changed,identity=patches-changed,
        invalid_inputs_rejected=refusals,ordered_patch_digest=digest.hexdigest(),samples=samples,source_writes=0)
    (ROOT/'phase121-icon-api-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(len(rows),'sources;',views,'records;',patches,'rebuilds;',changed,'changes;',refusals,'refusals')

if __name__=='__main__':main()
