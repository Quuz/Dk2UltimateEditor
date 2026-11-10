"""Exact scalar-value edits of the real mixed GlobalVariables container, in memory."""
from pathlib import Path
import json,struct,hashlib
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
def sha(b):return hashlib.sha256(b).hexdigest()

def patch_value(raw,value):
    assert len(raw)==16 and 0<=value<=0xffffffff
    return raw[:4]+struct.pack('<I',value)+raw[8:]

def validate():
    data=SOURCE.read_bytes();sections=fmt.parse(data);targets=[(100,42),(100,(-80)&0xffffffff),(100,0x7fffffff),(100,0x80000000),
        (67,0x3000),(67,0xffffffff),(233,99),(113,0)]
    assert len(sections)==2 and sections[0].signature==0xE6 and sections[1].signature==0xBE
    s=sections[0];rows=[]
    for typ,value in targets:
        hits=[(i,r) for i,r in enumerate(s.records) if len(r)==16 and int.from_bytes(r[:4],'little')==typ]
        assert len(hits)==1,(typ,len(hits));index,raw=hits[0];changed=patch_value(raw,value)
        assert changed!=raw and patch_value(changed,int.from_bytes(raw[4:8],'little'))==raw
        records=list(s.records);records[index]=changed
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=clone.pack()+sections[1].pack();oracle=bytearray(data);offset=s.offset+len(s.header)+index*16+4
        struct.pack_into('<I',oracle,offset,value)
        assert packed==oracle and len(packed)==len(data)
        parsed=fmt.parse(packed);assert parsed[0].records==records and parsed[1].pack()==sections[1].pack()
        assert parsed[0].header==s.header and parsed[0].suffix==s.suffix
        rows.append(dict(type=typ,value_bits=value,record_index=index,file_value_offset=offset,rebuilt_sha256=sha(packed)))
    assert sha(SOURCE.read_bytes())==sha(data)
    return dict(source=dict(file=str(SOURCE),sha256=sha(data)),source_bytes=len(data),source_variable_records=len(s.records),
        exact_container_edits=len(rows),rows=rows,headers_other_records_Extra1_Extra2_and_other_section_preserved=True,
        source_writes=0,codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),
        full_native_file_parser_executed=False)

if __name__=='__main__':
    d=validate();(ROOT/'phase166-editor-variable-storage.json').write_text(json.dumps(d,indent=2),encoding='utf-8');print('Verified',d['exact_container_edits'],'exact full-container edits, no source writes.')
