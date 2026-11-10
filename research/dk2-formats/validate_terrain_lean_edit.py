"""One combined six-layer-byte edit in each original Terrain container.

Different from phase132's8independent-byte toggles: direct editor example with
three ordered H and V levels, preserving Wibble and all other opaque bytes.
"""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
import native_terrain_deformation as api

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()


def validate():
    sources=json.loads((ROOT/'phase132-deformation-api-validation.json').read_text())['files']
    digest=hashlib.sha256();changed=0
    for source in sources:
        data=Path(source['file']).read_bytes();assert sha(data)==source['sha256']
        sections=fmt.parse(data);hits=[(si,ri,r) for si,s in enumerate(sections) if s.signature==0x6E
            for ri,r in enumerate(s.records) if len(r)==552 and r[0x1D6]==8]
        assert len(hits)==1
        si,ri,raw=hits[0];s=sections[si]
        new=api.patch(raw,lean_h=[32,128,255],lean_v=[255,128,32])
        assert new[0x1CE]==raw[0x1CE] and new[0x1D2]==raw[0x1D2]
        assert api.view(new)['lean_h']==[32,128,255] and api.view(new)['lean_v']==[255,128,32]
        records=list(s.records);records[ri]=new
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
        expected=bytearray(data);start=s.offset+len(s.header)+ri*552
        expected[start+0x1CF:start+0x1D2]=bytes([32,128,255]);expected[start+0x1D3:start+0x1D6]=bytes([255,128,32])
        assert packed==expected and fmt.parse(packed)[si].records==records
        changed+=packed!=data;digest.update(hashlib.sha256(packed).digest())
        assert sha(Path(source['file']).read_bytes())==source['sha256']
    assert len(sources)==changed==82
    return dict(phase=133,files=sources,exact_changed_container_rebuilds=changed,edited_raw_bytes_per_record=6,
        lean_h=[32,128,255],lean_v=[255,128,32],Wibble_and_unknowns_preserved=True,
        ordered_patch_digest=digest.hexdigest(),source_writes=0,scope=__doc__)


if __name__=='__main__':
    value=validate();(ROOT/'phase133-Lean-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Exact complete-container combined edits',value['exact_changed_container_rebuilds'])
