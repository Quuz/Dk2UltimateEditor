"""Eight independent physical Terrain-byte edits per whole source container."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
import native_terrain_deformation as api

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()


def validate():
    files=json.loads((ROOT/'terrain-model-chain-semantics.json').read_text())['source_preservation']['Terrain_sources']
    counts=Counter();digest=hashlib.sha256();records_seen=0
    for source in files:
        data=Path(source['file']).read_bytes();assert sha(data)==source['sha256']
        sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
        hits=[]
        for si,s in enumerate(sections):
            if s.signature!=0x6E:continue
            records_seen+=len(s.records)
            hits += [(si,ri,r) for ri,r in enumerate(s.records) if len(r)==552 and r[0x1D6]==8]
        assert len(hits)==1,source['file']
        si,ri,raw=hits[0];s=sections[si];view=api.view(raw)
        assert api.patch(raw,**view)==raw
        for offset in range(0x1CE,0x1D6):
            changes={}
            if offset in [0x1CE,0x1D2]:changes['wibble_h' if offset==0x1CE else 'wibble_v']=raw[offset]^128
            else:
                name='lean_h' if offset<0x1D2 else 'lean_v';values=view[name].copy();values[offset-api.OFFSETS[name]]^=128;changes[name]=values
            patched=api.patch(raw,**changes);expected_record=bytearray(raw);expected_record[offset]^=128;assert patched==expected_record
            rr=list(s.records);rr[ri]=patched
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
            expected=bytearray(data);start=s.offset+len(s.header)+ri*552;expected[start+offset]^=128
            assert packed==expected and fmt.parse(packed)[si].records==rr
            counts['exact_changed_container_rebuilds']+=1;digest.update(hashlib.sha256(packed).digest())
        assert sha(Path(source['file']).read_bytes())==source['sha256']
    good=bytes(552)
    refusals=[lambda:api.view(bytes(551)),lambda:api.patch(good,unknown=0),lambda:api.patch(good,wibble_h=-1),
        lambda:api.patch(good,wibble_v=256),lambda:api.patch(good,wibble_v=True),lambda:api.patch(good,lean_h=[1,2]),
        lambda:api.patch(good,lean_v=[0,1,256]),lambda:api.patch(good,lean_v=[0,True,1])]
    for call in refusals:
        try:call()
        except ValueError:pass
        else:raise AssertionError('Bad input accepted')
    assert len(files)==82 and counts['exact_changed_container_rebuilds']==656
    return dict(phase=132,files=files,records_seen=records_seen,**counts,identity_views=82,API_refusals=len(refusals),
        ordered_patch_digest=digest.hexdigest(),source_writes=0,
        scope='Claimed Path8 per source; each of8deformation bytes changed independently. Full containers exact, including unknown198/1AE/224, all other records, headers and tails. Not original editor saves.')


if __name__=='__main__':
    value=validate();(ROOT/'phase132-deformation-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Source files',len(value['files']),'records',value['records_seen'],'rebuilds',value['exact_changed_container_rebuilds'])
