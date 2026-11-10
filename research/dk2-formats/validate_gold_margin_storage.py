"""Exact Variables74 Value edits; preserve source/reference distinctions."""
import struct
from pathlib import Path
import dk2_formats as fmt
import native_gold_margin as api
from emulate_gold_margin import ROOT,SOURCE74,sha,load,dump


def build():
    data=SOURCE74.read_bytes();parts=fmt.parse(data)
    si,section=next((i,s) for i,s in enumerate(parts) if s.signature==230)
    ri,raw=next((i,r) for i,r in enumerate(section.records) if int.from_bytes(r[:4],'little')==74)
    offset=section.offset+len(section.header)+ri*section.stride
    assert data[offset:offset+16]==raw and api.view(raw)['value_bits']==340
    edits=[]
    for bits in [0,1,340,6000,6001,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        edited=api.patch_bits(raw,bits);assert edited[:4]==raw[:4] and edited[8:]==raw[8:]
        clone=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride)
        records=list(section.records);records[ri]=edited;clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))
        assert packed==data[:offset+4]+struct.pack('<I',bits)+data[offset+8:]
        assert clone.header==section.header and clone.suffix==section.suffix
        assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in fmt.parse(packed)]))==packed
        records[ri]=api.patch_bits(edited,340);clone.replace_records(records)
        assert b''.join(clone.pack() if i==si else s.pack() for i,s in enumerate(parts))==data
        edits.append(dict(value_bits=bits,only_four_value_bytes_may_change=True,
                          unknown_payload_and_other_sections_preserved=True,inverse_restores_source=True))
    adversarial=struct.pack('<4I',74,340,0x11223344,0x55667788)
    assert api.patch_bits(adversarial,0xFFFFFFFF)[8:]==adversarial[8:]
    assert b''.join(s.pack() for s in parts)==data
    assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in parts]))==data
    common=SOURCE74.parent/'GlobalVariables.kwd';common_data=common.read_bytes()
    assert not any(int.from_bytes(r[:4],'little')==74 for s in fmt.parse(common_data) if s.signature==230 for r in s.records)
    descriptor=SOURCE74.parent/'AutoSave.kwd'
    decoded=[fmt.decoded(s) for s in fmt.parse(descriptor.read_bytes())]
    refs=decoded[0]['references']
    assert any(r['type']=='0xE6' and r['path'].lower()=='data\\editor\\maps\\autosavevariables.kld' for r in refs)
    assert any(r['type']=='0xE6' and r['path'].lower()=='data\\editor\\globalvariables.kwd' for r in refs)
    # Persist the exact descriptor entries, rather than infer that a sibling
    # file is loaded when the descriptor names a missing Maps file.
    referenced=SOURCE74.parent/'Maps/AutoSaveVariables.kld'
    assert not referenced.exists()
    prior=load('phase192-conversion-storage.json')
    for ref in prior['source_roundtrips']:assert sha(Path(ref['file']).read_bytes())==ref['sha256']
    assert SOURCE74.read_bytes()==data
    return dict(source=dict(file=str(SOURCE74),sha256=sha(data),bytes=len(data)),record_index=ri,
                record_file_offset=offset,value_file_offset=offset+4,field_record_offset=4,field_bytes=4,
                raw_record_hex=raw.hex(),edits=edits,source_exact_physical_and_decoded_roundtrip=True,
                common_source=dict(file=str(common),sha256=sha(common_data),contains_Type74=False),
                descriptor=dict(file=str(descriptor),sha256=sha(descriptor.read_bytes()),references=refs,
                    referenced_Variables_file=str(referenced),referenced_file_exists=False,
                    scope='Sibling source record executed directly; actual AutoSave load resolution is not claimed.'),
                inherited_15_family_roundtrip_evidence=dict(file='phase192-conversion-storage.json',
                    sha256=sha((ROOT/'phase192-conversion-storage.json').read_bytes()),
                    prior_roundtrips=len(prior['source_roundtrips']),source_hashes_rechecked=True,new_roundtrip_runs=0),
                unknown_Extra1_Extra2_preserved=True,source_writes=0)


if __name__=='__main__':
    r=build();dump('phase193-gold-storage.json',r)
    print('gold storage',len(r['edits']),'edits',r['record_file_offset'],'reference gap retained')
