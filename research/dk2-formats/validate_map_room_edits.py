"""Exact Map flag-bit edits across the current source corpus; no file writes."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_map_cells import patch_cell,intermediate_cell
from inventory import DATA

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    sources=[dict(file=str(DATA/'maps'/row['file']),sha256=row['sha256']) for row in json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'))['files']
        if any(s['family']=='Map' for s in row.get('sections',[]))]
    auto=DATA/'AutoSaveMap.kld';sources.append(dict(file=str(auto),sha256=sha(auto.read_bytes())))
    rows=[];digest=hashlib.sha256()
    for old in sources:
        path=Path(old['file']);data=path.read_bytes();assert sha(data)==old['sha256']
        sections=fmt.parse(data);hits=[(i,s) for i,s in enumerate(sections) if s.signature==100]
        if not hits:continue
        assert len(hits)==1;si,s=hits[0];width,height=fmt.u32(s.header,20),fmt.u32(s.header,24)
        assert len(s.data)==4*width*height
        # Prefer a real Temple cell; other maps use their first physical cell.
        index=next((i for i in range(width*height) if s.data[4*i]==22),0)
        cell=s.data[4*index:4*index+4];new=patch_cell(cell,2,cell[2]^2)
        assert new[3]==cell[3] and patch_cell(new,2,cell[2])==cell
        payload=s.data[:4*index]+new+s.data[4*index+4:]
        changed=fmt.Section(s.offset,s.signature,s.header,[payload],s.suffix)
        packed=b''.join(changed.pack() if i==si else x.pack() for i,x in enumerate(sections))
        expected=bytearray(data);expected[s.offset+len(s.header)+4*index+2]^=2
        assert packed==expected and b''.join(x.pack() for x in fmt.parse(packed))==packed
        assert sha(path.read_bytes())==sha(data);digest.update(hashlib.sha256(packed).digest())
        rows.append(dict(file=str(path),sha256=sha(data),width=width,height=height,patched_cell=index,TerrainId=cell[0],opaque_fourth=cell[3]))
    assert len(rows)==110
    # Fourth-byte variants produce identical parser-projected callback inputs.
    variants=[intermediate_cell(bytes([22,2,0,x])) for x in range(256)]
    assert len(set(variants))==1
    return dict(phase=138,files=rows,new_exact_Map_container_patches=len(rows),new_inverse_cell_edits=len(rows),only_changed_raw_offset_within_cell=2,
        changed_mask=2,all_other_bytes_including_opaque_fourth_preserved=True,ordered_patch_digest=digest.hexdigest(),
        fourth_byte_projection_variants=256,fourth_byte_semantics_remain_unknown=True,
        scope='Host codec/whole-file oracle only. Native Map parser was proved in earlier phase28; these256projections are not256new native parser runs.',source_writes=0)

if __name__=='__main__':
    value=validate();(ROOT/'phase138-map-room-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(len(value['files']),'exact Map flag edits, opaque byte3 preserved')
