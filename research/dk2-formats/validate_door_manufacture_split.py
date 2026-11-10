"""Current decode/edit/rebuild paths preserve the separate opaque Door WORD."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
import dk2_formats_phase141 as legacy
from native_door_field_layout import effective_door_layout,patch_manufacture

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    preserved=json.loads((ROOT/'effect-generation-scale-semantics.json').read_text(encoding='utf8'))['preservation']['source_files']
    # Reuse the fixed, source-hashed Door corpus without adding inferred files.
    source=json.loads((ROOT/'door-writer-semantics.json').read_text(encoding='utf8'))
    refs={str(Path(r['file'])).lower():r for r in source['corpus']['files']}
    assert len(refs)==82,len(refs)
    rows=[];records_count=0;digest=hashlib.sha256();source_nonzero_upper=0
    for row in refs.values():
        path=Path(row['file']);data=path.read_bytes();assert sha(data)==row['sha256'];ss=fmt.parse(data)
        si,s=next((i,s) for i,s in enumerate(ss) if s.signature==140);assert s.stride==616
        records_count+=len(s.records);source_nonzero_upper+=sum(fmt.u16(r,0x236)!=0 for r in s.records)
        ri=0;raw=s.records[ri];man=fmt.u16(raw,0x234);upper=fmt.u16(raw,0x236)
        marked=fmt.set_scalar(raw,'Doors','Unknown236',0xA55A)
        edited=fmt.set_scalar(marked,'Doors','ManufToBuild',65535)
        assert edited==patch_manufacture(marked,65535)
        assert edited[0x236:0x238]==bytes.fromhex('5aa5')
        assert legacy.set_scalar(marked,'Doors','ManufToBuild',65535)[0x236:0x238]==bytes(2)
        assert fmt.set_scalar(fmt.set_scalar(edited,'Doors','ManufToBuild',man),'Doors','Unknown236',upper)==raw
        rr=list(s.records);rr[ri]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
        packed=b''.join(clone.pack() if i==si else part.pack() for i,part in enumerate(ss))
        expected=bytearray(data);at=s.offset+len(s.header)+ri*616
        expected[at+0x234:at+0x238]=bytes.fromhex('ffff5aa5')
        assert packed==expected
        decoded=fmt.decoded(clone);fields=decoded['records'][ri]['fields']
        assert fields['ManufToBuild']==65535 and fields['Unknown236']==0xA55A
        assert fmt.rebuild_decoded(dict(sections=[decoded]))==clone.pack()
        assert sha(path.read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=row['file'],sha256=row['sha256'],record_index=ri))
    # Actual API contract: values cannot spill into the second physical WORD.
    fixture=bytes(616)
    for bad in [-1,65536,0xFFFFFFFF]:
        try:fmt.set_scalar(fixture,'Doors','ManufToBuild',bad)
        except (ValueError,OverflowError):pass
        else:raise AssertionError('Out-of-range native WORD accepted')
    assert legacy.set_scalar(fixture,'Doors','ManufToBuild',65536)[0x234:0x238]==bytes.fromhex('00000100')
    assert fmt.LAYOUTS['Doors']['fields']!=effective_door_layout()['fields']
    return dict(phase=142,files=rows,new_exact_container_edits=len(rows),native_door_records=records_count,
        source_nonzero_upper_words=source_nonzero_upper,public_decode_rebuild_controls=len(rows),legacy_clobber_reproductions=len(rows),
        raw_changes='Explicitly seed upperWORD toA55A, then edit native lowerWORD toFFFF; whole originalcontainer differs only234..237 ofone chosenrecord.',
        unknown_high_word_preserved_by_manufacture_edit=True,all_other_container_bytes_preserved=True,source_writes=0,
        ordered_patch_digest=digest.hexdigest(),historical_codec=dict(file='dk2_formats_phase141.py',sha256=sha((ROOT/'dk2_formats_phase141.py').read_bytes())),
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))

if __name__=='__main__':
    v=validate();(ROOT/'phase142-door-split-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(v['new_exact_container_edits'],'exact containers;',v['native_door_records'],'source records;',v['source_nonzero_upper_words'],'source nonzero upper words')
