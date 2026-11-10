"""Whole-container opaque-array edits preserve every byte outside32 selected bytes."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
import native_terrain_opaque_table as api
from emulate_terrain_opaque_table import VALUES

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    sources=json.loads((ROOT/'phase132-deformation-api-validation.json').read_text())['files']
    digest=hashlib.sha256();changed=0;identities=0
    for source in sources:
        data=Path(source['file']).read_bytes();assert sha(data)==source['sha256']
        sections=fmt.parse(data);hits=[(si,ri,r) for si,s in enumerate(sections) if s.signature==0x6E
            for ri,r in enumerate(s.records) if len(r)==552 and r[0x1D6]==8]
        assert len(hits)==1
        si,ri,raw=hits[0];s=sections[si]
        assert api.patch(raw,api.view(raw))==raw;identities+=1
        new=api.patch(raw,VALUES['highbit']);assert api.view(new)==VALUES['highbit']
        records=list(s.records);records[ri]=new
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
        expected=bytearray(data);start=s.offset+len(s.header)+ri*552
        expected[start+0x1AE:start+0x1CE]=new[0x1AE:0x1CE]
        assert packed==expected and fmt.parse(packed)[si].records==records
        changed+=packed!=data;digest.update(hashlib.sha256(packed).digest())
        assert sha(Path(source['file']).read_bytes())==source['sha256']
    assert len(sources)==changed==identities==82
    refusals=0
    for values in [[],[0]*15,[0]*17,[0]*15+[-1],[0]*15+[65536],[0]*15+[1.0],[0]*15+[True]]:
        try:api.patch(raw,values)
        except ValueError:refusals+=1
        else:raise AssertionError(values)
    return dict(phase=134,files=sources,exact_changed_container_rebuilds=changed,identity_record_roundtrips=identities,
        invalid_value_refusals=refusals,edited_raw_span=[0x1AE,32],opaque_values=VALUES['highbit'],
        original_headers_order_other_records_suffix_and_unknowns_preserved=True,
        ordered_patch_digest=digest.hexdigest(),source_writes=0,scope=__doc__)

if __name__=='__main__':
    value=validate();(ROOT/'phase134-Terrain-table-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Exact complete-container opaque-table edits',value['exact_changed_container_rebuilds'])
