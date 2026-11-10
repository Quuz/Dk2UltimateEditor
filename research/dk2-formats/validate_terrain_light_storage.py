"""Whole-container light/height/gold/opaque edits through existing public APIs.

No source writes; exact bytes, all other records, flags, framing and unknowns
are checked. RGB edits preserve enable and all unrelated flag bits.
"""
from pathlib import Path
from collections import Counter
from decimal import Decimal
import json,hashlib,struct
import dk2_formats as fmt
import native_terrain_lighting as light
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=json.loads((ROOT/'phase132-deformation-api-validation.json').read_text(encoding='utf8'))['files']
    rows=[];hist=Counter();edits=0;total=0;digest=hashlib.sha256()
    for source in refs:
        path=Path(source['file']);data=path.read_bytes();assert sha(data)==source['sha256']
        sections=fmt.parse(data);items=[(i,s) for i,s in enumerate(sections) if s.signature==110];assert len(items)==1
        si,s=items[0];assert all(len(r)==552 for r in s.records);total+=len(s.records)
        for raw in s.records:hist[(fmt.u16(raw,0x198),fmt.u32(raw,0x224))]+=1
        ri=next(i for i,r in enumerate(s.records) if r[0x1D6]==3);raw=s.records[ri]
        rgb=raw
        for channel,value in enumerate([511,256,257]):rgb=light.patch_channel(rgb,'terrain',channel,value)
        assert light.view(rgb)['terrain']['editor_values']==[511,256,257]
        assert light.view(rgb)['terrain']['game_loaded_words']==[510,255,256]
        expected=bytearray(raw);expected[0x1DB:0x1DE]=bytes([255,0,1]);struct.pack_into('<I',expected,0x190,fmt.u32(raw,0x190)|0x380000)
        assert rgb==expected
        variants=[('RGB511-256-257',rgb,[(0x190,4),(0x1DB,3)]),
            ('height-minus-half',fmt.set_scalar(raw,'Terrain','LightHeight',Decimal('-0.5')),[(0x18C,4)]),
            ('nonzero-GoldValue',fmt.set_scalar(raw,'Terrain','GoldValue',37),[(0x19A,2)]),
            ('opaque-preservation',fmt.set_scalar(fmt.set_scalar(raw,'Terrain','Unk198',0xBEEF),'Terrain','Unk224',0x89ABCDEF),[(0x198,2),(0x224,4)])]
        assert variants[1][1][0x18C:0x190]==struct.pack('<i',-2048)
        for label,patched,spans in variants:
            expected=bytearray(raw)
            for off,n in spans:expected[off:off+n]=patched[off:off+n]
            assert patched==expected and patched!=raw
            records=list(s.records);records[ri]=patched
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
            start=s.offset+len(s.header)+ri*552;whole=bytearray(data)
            for off,n in spans:whole[start+off:start+off+n]=patched[off:off+n]
            assert packed==whole and clone.header==s.header and fmt.parse(packed)[si].records==records
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
            digest.update(hashlib.sha256(packed).digest());edits+=1
        assert sha(path.read_bytes())==source['sha256']
        rows.append(dict(file=str(path),sha256=source['sha256'],records=len(s.records),selected_record_index=ri))
    assert len(rows)==82 and edits==328
    return dict(phase=159,files=rows,records=total,exact_changed_container_rebuilds=edits,public_decode_rebuild_controls=edits,
        all_other_bytes_preserved=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        opaque_value_pairs=[dict(WORD198=a,DWORD224=b,records=n) for (a,b),n in sorted(hist.items())],
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),
        scope=__doc__,limitations='GoldValue37 and opaque values are storage diagnostics; sourcecorrelations do not establish opaque gameplay meaning.')

if __name__=='__main__':
    d=validate();(ROOT/'phase159-terrain-light-storage-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8')
    print('Terrain',len(d['files']),'files',d['records'],'records',d['exact_changed_container_rebuilds'],'exact edits')
