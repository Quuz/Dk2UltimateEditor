"""Append and remove a synthetic Variables19 record with exact physical preservation."""
import struct
import dk2_formats as fmt
import native_tick_rate as api
from emulate_tick_rate import ROOT,sha,dump
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'


def build():
    data=SOURCE.read_bytes();parts=fmt.parse(data);si,section=next((i,s) for i,s in enumerate(parts) if s.signature==230)
    assert not any(int.from_bytes(r[:4],'little')==19 for r in section.records)
    result=[]
    for value in [0,1,4,20,60,65536,0x80000000,0xFFFFFFFF]:
        raw=struct.pack('<4I',19,20,0x11223344,0x55667788);new=api.patch_bits(raw,value)
        clone=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride)
        clone.replace_records(list(section.records)+[new]);packed=b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))
        assert clone.records[:-1]==section.records and clone.suffix==section.suffix
        assert len(packed)==len(data)+16 and fmt.u32(clone.header,8)==fmt.u32(section.header,8)+16
        assert fmt.u32(clone.header,20)==fmt.u32(section.header,20)+1
        assert fmt.u32(clone.header,len(clone.header)-4)==fmt.u32(section.header,len(section.header)-4)+16
        changed_header={i for i,(a,b) in enumerate(zip(clone.header,section.header)) if a!=b}
        assert changed_header <= set(range(8,12))|set(range(20,24))|set(range(len(section.header)-4,len(section.header)))
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in fmt.parse(packed)]))==packed
        clone.replace_records(clone.records[:-1]);assert b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))==data
        assert new[:4]==raw[:4] and new[8:]==raw[8:]
        result.append(dict(value_bits=value,synthetic_record=True,old_records_and_unknowns_preserved=True,inverse_restores_source=True))
    assert SOURCE.read_bytes()==data
    level_path=SOURCE.parent/'AutoSave.kwd';level_data=level_path.read_bytes();level=fmt.parse(level_data)[0]
    payload_size=fmt.u32(level.header,len(level.header)-4);payload=level.data[:payload_size]
    assert payload_size in [25603,25683]
    level_edits=[];original_ticks=fmt.u16(payload,0x1182)
    for value in [0,1,4,20,65535]:
        edited=fmt.set_scalar(payload,'Level_'+str(payload_size),'TicksPerSecond',value)
        assert edited==payload[:0x1182]+struct.pack('<H',value)+payload[0x1184:]
        at=level.offset+len(level.header)+0x1182
        packed=level_data[:at]+struct.pack('<H',value)+level_data[at+2:]
        assert b''.join(s.pack() for s in fmt.parse(packed))==packed
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in fmt.parse(packed)]))==packed
        assert packed[:at]+struct.pack('<H',original_ticks)+packed[at+2:]==level_data
        level_edits.append(dict(ticks=value,only_two_field_bytes_may_change=True,inverse_restores_source=True))
    assert level_path.read_bytes()==level_data
    return dict(source=dict(file=str(SOURCE),sha256=sha(data)),source_contains_Type19=False,source_record_count=len(section.records),
        raw_record_bytes=16,Value_record_offset=4,Value_bytes=4,edits=result,new_record_count_delta=1,new_section_bytes_delta=16,
        Level=dict(file=str(level_path),sha256=sha(level_data),payload_bytes=payload_size,raw_TPS_offset=0x1182,field_bytes=2,
            source_TicksPerSecond=original_ticks,field_file_offset=at,edits=level_edits),
        scope='In-memory canonical append/remove and Level scalar edits. Does not execute original file reader/editor writer or prove natural common/level loading order.',source_writes=0)


if __name__=='__main__':
    s=build();dump('phase195-tps-storage.json',s);print('TPS storage',len(s['edits']),'append/remove controls; source count',s['source_record_count'])
