"""Exact one-bit edits and current enum decoding for both native Creature variants."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
import dk2_formats_phase146 as legacy
import native_melee_animation_flag as api
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def validate():
    refs=json.loads((ROOT/'creature-lookup-review.json').read_text(encoding='utf8'))['corpus']['files'];assert len(refs)==82
    rows=[];records=0;digest=hashlib.sha256()
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==0xAA);records+=len(s.records)
        raw=s.records[0];flags=fmt.u32(raw,0xEFC);enabled=bool(flags&api.MASK);edited=api.patch(raw,not enabled)
        layout='Creatures_5537' if len(raw)==5537 else 'Creatures'
        assert edited==fmt.set_scalar(raw,layout,'Flags',flags^api.MASK)
        assert [i for i,(a,b) in enumerate(zip(raw,edited)) if a!=b]==[0xEFF]
        assert raw[0xEFF]^edited[0xEFF]==0x20 and api.patch(edited,enabled)==raw
        rr=list(s.records);rr[0]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+0xEFF
        expected=bytearray(data);expected[at]^=0x20;assert packed==expected
        doc=fmt.decoded(clone);fields=doc['records'][0]['fields']['Flags'];assert fields['raw']==flags^api.MASK
        assert (api.NAME in fields['names'])==(not enabled) and 'UNKNOWN_5' not in fields['names']
        old=legacy.decoded(clone)['records'][0]['fields']['Flags'];assert ('UNKNOWN_5' in old['names'])==(not enabled)
        assert fmt.rebuild_decoded(dict(sections=[doc]))==clone.pack()
        assert sha(p.read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest());rows.append(row)
    before=fmt.ENUMS;after=api.effective_enums();diff=[(group,key) for group,values in before.items() for key,value in values.items() if after[group][key]!=value]
    assert diff==[('Creature.CreatureFlag',str(api.MASK))]
    return dict(phase=147,files=rows,source_records=records,new_exact_container_edits=len(rows),public_decode_rebuild_controls=len(rows),
        changed_byte=0xEFF,xor_mask=0x20,all_other_bits_and_bytes_preserved=True,source_writes=0,ordered_patch_digest=digest.hexdigest(),
        enum_changes=[list(x) for x in diff],historical_codec=dict(file='dk2_formats_phase146.py',sha256=sha((ROOT/'dk2_formats_phase146.py').read_bytes())),
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))
if __name__=='__main__':
    d=validate();(ROOT/'phase147-animation-flag-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(d['new_exact_container_edits'],'exact one-bit container edits',d['source_records'],'source records')
