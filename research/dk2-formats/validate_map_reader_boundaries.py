"""110 precise opaque-byte edits and strict Map framing versus native reader."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_map_cells import patch_cell
import emulate_native_map_reader as reader

ROOT=Path(__file__).resolve().parent
sha=reader.sha

def validate():
    prior=json.loads((ROOT/'phase138-map-room-api-validation.json').read_text(encoding='utf8'));rows=[];digest=hashlib.sha256()
    for row in prior['files']:
        path=Path(row['file']);data=path.read_bytes();assert sha(data)==row['sha256']
        sections=fmt.parse(data);si,s=next((i,s) for i,s in enumerate(sections) if s.signature==100)
        index=row['patched_cell'];cell=s.data[4*index:4*index+4]
        new=patch_cell(cell,3,cell[3]^0x80);assert new[:3]==cell[:3] and patch_cell(new,3,cell[3])==cell
        payload=s.data[:4*index]+new+s.data[4*index+4:]
        clone=fmt.Section(s.offset,s.signature,s.header,[payload],s.suffix)
        packed=b''.join(clone.pack() if i==si else p.pack() for i,p in enumerate(sections))
        expected=bytearray(data);expected[s.offset+len(s.header)+4*index+3]^=0x80
        assert packed==expected and b''.join(x.pack() for x in fmt.parse(packed))==packed
        digest.update(hashlib.sha256(packed).digest());assert sha(path.read_bytes())==row['sha256']
        rows.append(dict(file=str(path),sha256=sha(data),cell=index,changed_raw_byte_offset=3))
    assert len(rows)==110
    strict=[]
    for c in reader.cases():
        data=reader.raw_file(c)
        try:fmt.parse(data);ok=True
        except ValueError:ok=False
        strict.append(dict(case=c['name'],existing_lossless_codec_accepts=ok,bytes=len(data)))
    by={r['case']:r for r in strict}
    for n in ['world-square','source-AutoSaveMap','source-AutoSaveMap-opaque-edit','chunk-crossing']:assert by[n]['existing_lossless_codec_accepts']
    for n in ['truncated-last-fourth','truncated-last-flags','empty-payload','world-declared0','unchecked-header-size']:assert not by[n]['existing_lossless_codec_accepts']
    return dict(phase=139,source_files=rows,new_exact_opaque_Map_edits=110,record_inverse_checks=110,
        only_changed_mask=0x80,only_changed_cell_byte=3,all_other_bytes_and_headers_preserved=True,source_writes=0,
        ordered_patch_digest=digest.hexdigest(),strict_codec_controls=strict,
        scope='New memory-only opaque-byte corpus sweep; strict existing codec is not relaxed to reproduce native parser acceptance defects. Native selected source-copy edit has its own full-reader pair.')

if __name__=='__main__':
    value=validate();(ROOT/'phase139-map-reader-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('110 exact opaque-byte edits; strict framing controls passed')
