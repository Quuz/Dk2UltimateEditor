"""Exact four-byte edits of Variables233; all15 requested source roundtrips."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import native_conversion_score as api
from emulate_conversion_score import sha,ROOT
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
FILES=['Creatures.kwd','Objects.kwd','EffectElements.kwd','Effects.kwd','Rooms.kwd','AutoSave.kwd','Terrain.kwd',
       'AutoSaveMap.kld','Shots.kwd','GlobalVariables.kwd','CreatureSpells.kwd','Traps.kwd','KeeperSpells.kwd','AutoSaveThings.kld','Doors.kwd']


def build():
    data=SOURCE.read_bytes();parts=fmt.parse(data)
    si,section=next((i,s) for i,s in enumerate(parts) if s.signature==230)
    ri,raw=next((i,r) for i,r in enumerate(section.records) if int.from_bytes(r[:4],'little')==233)
    offset=section.offset+len(section.header)+ri*section.stride
    assert data[offset:offset+16]==raw and api.view(raw)['value_bits']==40
    edits=[]
    for bits in [0,1,40,123,0x7FFFFFFF,0x80000000,0xFFFFFFFF,0x12345678]:
        edited=api.patch_bits(raw,bits);assert edited[:4]==raw[:4] and edited[8:]==raw[8:]
        clone=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride)
        records=list(section.records);records[ri]=edited;clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))
        assert packed==data[:offset+4]+struct.pack('<I',bits)+data[offset+8:]
        assert clone.header==section.header and clone.suffix==section.suffix
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in fmt.parse(packed)]))==packed
        records[ri]=api.patch_bits(edited,40);clone.replace_records(records)
        assert b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))==data
        edits.append(dict(value_bits=bits,only_four_value_bytes_may_change=True,unknown_payload_and_other_sections_preserved=True,inverse_restores_source=True))
    adversarial=struct.pack('<4I',233,40,0x11223344,0x55667788)
    assert api.patch_bits(adversarial,0xFFFFFFFF)[8:]==adversarial[8:]
    roundtrips=[]
    for name in FILES:
        p=SOURCE.parent/name;b=p.read_bytes();parsed=fmt.parse(b)
        assert b''.join(s.pack() for s in parsed)==b
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in parsed]))==b
        roundtrips.append(dict(file=str(p),sha256=sha(b),bytes=len(b),exact_physical_and_decoded_roundtrip=True))
    assert SOURCE.read_bytes()==data
    return dict(source=dict(file=str(SOURCE),sha256=sha(data)),record_index=ri,record_file_offset=offset,
                field_record_offset=4,field_bytes=4,raw_record_hex=raw.hex(),edits=edits,
                source_roundtrips=roundtrips,unknown_Extra1_Extra2_preserved=True,source_writes=0)


if __name__=='__main__':
    r=build();(ROOT/'phase192-conversion-storage.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print('conversion storage',len(r['edits']),'edits',len(r['source_roundtrips']),'roundtrips',r['record_file_offset'])
