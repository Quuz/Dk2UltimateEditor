"""Whole-file lossless Room intensity/resource edits, preserving raw36F."""
from pathlib import Path
import collections,hashlib,json
import dk2_formats as fmt
import native_room_writer as api

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def main():
    paths={r['file']:r['sha256'] for r in json.loads((ROOT/'object-loading-semantics.json').read_text(encoding='utf8'))['corpus']['files']}
    own=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Rooms.kwd';paths[str(own)]=sha(own.read_bytes())
    rows=[];views=changed=patches=0;opaque=collections.Counter();digest=hashlib.sha256()
    for file,h in paths.items():
        path=Path(file);data=path.read_bytes();assert sha(data)==h
        parts=fmt.parse(data);hits=[(i,s) for i,s in enumerate(parts) if s.signature==0x78]
        if not hits:continue
        assert b''.join(s.pack() for s in parts)==data
        for _,s in hits:
            for raw in s.records:
                assert api.patch_torch_intensity(raw,raw[0x36E])==raw
                assert api.patch_unknown_resource(raw,raw[0x1C4:0x218])==raw
                opaque[raw[0x36F]]+=1;views+=1
        si,s=hits[0];raw=s.records[0]
        for kind in ['identity','intensity','resource']:
            edited=raw if kind=='identity' else api.patch_torch_intensity(raw,raw[0x36E]^0x80) if kind=='intensity' else api.patch_unknown_resource(raw,bytes((i*47+11)&255 for i in range(84)))
            assert edited[0x36F]==raw[0x36F]
            if kind=='intensity':assert edited[:0x36E]==raw[:0x36E] and edited[0x36F:]==raw[0x36F:]
            if kind=='resource':assert edited[:0x1C4]==raw[:0x1C4] and edited[0x218:]==raw[0x218:]
            records=list(s.records);records[0]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else z.pack() for i,z in enumerate(parts))
            expected=bytearray(data);offset=s.offset+len(s.header);expected[offset:offset+1055]=edited
            assert packed==expected and fmt.parse(packed)[si].records==records
            patches+=1;changed+=packed!=data;digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=file,sha256=h))
    first=next(r for s in fmt.parse(own.read_bytes()) if s.signature==0x78 for r in s.records);refused=0
    for fn in [lambda:api.patch_torch_intensity(first,256),lambda:api.patch_torch_intensity(first,-1),
        lambda:api.patch_torch_intensity(first,True),lambda:api.patch_torch_intensity(bytearray(first),1),
        lambda:api.patch_unknown_resource(first,bytes(83)),lambda:api.patch_unknown_resource(first[:-1],bytes(84))]:
        try:fn()
        except ValueError:refused+=1
        else:raise AssertionError('Invalid input accepted')
    assert len(rows)==82 and patches==246 and refused==6
    for r in rows:assert sha(Path(r['file']).read_bytes())==r['sha256']
    out=dict(scope=__doc__,files=rows,records=views,exact_container_rebuilds=patches,
        changed=changed,identity=patches-changed,API_refusals=refused,
        opaque36F_histogram=dict(sorted(opaque.items())),ordered_digest=digest.hexdigest(),source_writes=0)
    (ROOT/'phase122-room-api-validation.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    print(len(rows),'sources;',views,'records;',patches,'rebuilds;',refused,'refusals;opaque36F',dict(opaque))

if __name__=='__main__':main()
