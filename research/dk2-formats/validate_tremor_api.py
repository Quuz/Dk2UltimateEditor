"""Exact Shotmode8/Q12/worddamage and TerrainDestroyedType container edits."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_tremor import patch_destroyed_type
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    shot_refs=json.loads((ROOT/'area-damage-semantics.json').read_text())['corpus']['files'];assert len(shot_refs)==82
    preservation=json.loads((ROOT/'room-resource-loader-semantics.json').read_text())['preservation']
    terrain_source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd'
    pinned=next(r for r in preservation['source_files'] if Path(r['file']).resolve()==terrain_source.resolve())
    terrain_refs=[r for r in shot_refs if Path(r['file']).name.lower()!='shots.kwd']+[pinned]
    assert len(terrain_refs)==82
    rows=[];counts=dict(Shot_records=0,Terrain_records=0,Shot_edits=0,Terrain_edits=0,Terrain_changed=0,Terrain_identity=0);digest=hashlib.sha256()
    for family,refs,signature in [('Shots',shot_refs,0x104),('Terrain',terrain_refs,0x6E)]:
        for row in refs:
            p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data);si,s=next((i,s) for i,s in enumerate(parts) if s.signature==signature)
            counts['Shot_records' if family=='Shots' else 'Terrain_records']+=len(s.records)
            index=0 if family=='Shots' else next(i for i,r in enumerate(s.records) if r[0x1D6]==2)
            raw=s.records[index]
            edits=[dict(Data1=65537,Data2=8192,DamageType=8,ProcessType=0),dict(Data1=0xFFFFFFB0,Data2=4096,DamageType=8,ProcessType=0)] if family=='Shots' else [None]
            for values in edits:
                edited=raw
                if family=='Shots':
                    for field,value in values.items():edited=fmt.set_scalar(edited,'Shots',field,value)
                    exact=bytearray(raw);exact[0x98:0x9C]=values['Data1'].to_bytes(4,'little');exact[0x9C:0xA0]=values['Data2'].to_bytes(4,'little');exact[0xC5]=8;exact[0xC7]=0
                    assert edited==exact
                    inverse=edited
                    for field,off,n in [('Data1',0x98,4),('Data2',0x9C,4),('DamageType',0xC5,1),('ProcessType',0xC7,1)]:inverse=fmt.set_scalar(inverse,'Shots',field,int.from_bytes(raw[off:off+n],'little'))
                    assert inverse==raw
                    counts['Shot_edits']+=1
                else:
                    edited=patch_destroyed_type(raw,raw[0x1D6]);assert edited[:0x1DA]==raw[:0x1DA] and edited[0x1DB:]==raw[0x1DB:]
                    assert patch_destroyed_type(edited,raw[0x1DA])==raw
                    if len(raw)==552:assert edited==fmt.set_scalar(raw,'Terrain','DestroyedTypeTerrainId',raw[0x1D6])
                    counts['Terrain_edits']+=1;counts['Terrain_changed' if edited!=raw else 'Terrain_identity']+=1
                rr=list(s.records);rr[index]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
                packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));at=s.offset+len(s.header)+index*s.stride
                expected=bytearray(data);expected[at:at+len(raw)]=edited;assert packed==expected
                assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
                digest.update(hashlib.sha256(packed).digest())
            assert sha(p.read_bytes())==row['sha256'];rows.append(dict(family=family,file=row['file'],sha256=row['sha256']))
    return dict(phase=151,files=rows,counts=counts,new_exact_container_edits=counts['Shot_edits']+counts['Terrain_edits'],
        public_decode_rebuild_controls=counts['Shot_edits']+counts['Terrain_edits'],all_other_bytes_preserved=True,inverse_patches_restore_original=True,
        ordered_patch_digest=digest.hexdigest(),source_writes=0,physical_Data1_Data2_storage='u32 unchanged; mode-specificinterpretation',
        current_codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))

if __name__=='__main__':
    d=validate();(ROOT/'phase151-tremor-api-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(d['new_exact_container_edits'],'edits;',d['counts'])
