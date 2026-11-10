"""Public decode/edit and exact container rebuild for one native Trap WORD204."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
import dk2_formats_phase145 as legacy
from native_trap_field_layout import effective_trap_layout
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def validate():
    corpus=json.loads((ROOT/'trap-writer-semantics.json').read_text(encoding='utf8'))['corpus']
    refs=corpus['files'];assert len(refs)==82
    rows=[];digest=hashlib.sha256();records=0
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(parts) if s.signature==0x82);assert s.stride==579
        records+=len(s.records);raw=s.records[0];old=fmt.u16(raw,0x204)
        edited=fmt.set_scalar(raw,'Traps','Unknown204',0xA55A)
        assert edited==raw[:0x204]+bytes.fromhex('5aa5')+raw[0x206:]
        assert fmt.set_scalar(edited,'Traps','Unknown204',old)==raw
        rr=list(s.records);rr[0]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
        packed=b''.join(clone.pack() if i==si else sec.pack() for i,sec in enumerate(parts))
        expected=bytearray(data);at=s.offset+len(s.header);expected[at+0x204:at+0x206]=bytes.fromhex('5aa5')
        assert packed==expected
        dec=fmt.decoded(clone);fields=dec['records'][0]['fields'];assert fields['Unknown204']==0xA55A
        assert 'unknown4[0].unknown4' not in fields and 'unknown4[1].unknown4' not in fields
        assert fmt.rebuild_decoded(dict(sections=[dec]))==clone.pack()
        oldfields=legacy.decoded(clone)['records'][0]['fields']
        assert oldfields['unknown4[0].unknown4']==0x5A and oldfields['unknown4[1].unknown4']==0xA5
        assert sha(p.read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=row['file'],sha256=row['sha256'],record_index=0))
    for bad in [-1,65536,1.5]:
        try:fmt.set_scalar(bytes(579),'Traps','Unknown204',bad)
        except (ValueError,OverflowError):pass
        else:raise AssertionError('Non-WORD value accepted')
    assert fmt.LAYOUTS['Traps']['fields']!=effective_trap_layout()['fields']
    # This same codec change routes Door through a shared selector. Protect
    # its previously corrected independent upperWORD contract explicitly.
    door=fmt.set_scalar(bytes(616),'Doors','Unknown236',0xA55A)
    door=fmt.set_scalar(door,'Doors','ManufToBuild',65535)
    assert door[0x234:0x238]==bytes.fromhex('ffff5aa5')
    return dict(phase=146,files=rows,new_exact_container_edits=len(rows),source_records=records,public_decode_rebuild_controls=len(rows),
        all_other_container_bytes_preserved=True,native_WORD_view=True,no_legacy_data_corruption_claim=True,Door_WORD_split_regression_passed=True,
        source_writes=0,ordered_patch_digest=digest.hexdigest(),historical_codec=dict(file='dk2_formats_phase145.py',sha256=sha((ROOT/'dk2_formats_phase145.py').read_bytes())),
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))
if __name__=='__main__':
    d=validate();(ROOT/'phase146-Trap-WORD-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(d['new_exact_container_edits'],'exact containers',d['source_records'],'records')
