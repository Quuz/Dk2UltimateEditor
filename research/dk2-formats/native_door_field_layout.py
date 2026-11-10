"""Current Door manufacture WORD split; historical layout remains auditable."""
from pathlib import Path
import copy,json

ROOT=Path(__file__).resolve().parent

def effective_door_layout():
    source=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))['Doors']
    layout=copy.deepcopy(source);fields=[]
    for field in layout['fields']:
        if field['offset']!=0x234:
            fields.append(field);continue
        assert field['name']=='ManufToBuild' and field['size']==4 and field['kind']=='u32'
        low=dict(field,size=2,kind='u16',semantics='Native converter/loader/editor writer and manufacture queue confirm separate unsignedWORD234.')
        low.update(legacy_size=4,legacy_kind='u32',legacy_semantics=field['semantics'],
            native_intermediate_offset=0x2A8,native_definition_offset=0x95,
            serialization='Patch only234..235; preserve236..237 exactly.')
        high=dict(name='Unknown236',offset=0x236,offset_hex='0x236',size=2,kind='u16',
            semantics='Unresolved separateWORD. Native loader/editor writer preserve all16bits; not upper manufacture bits.',
            native_intermediate_offset=0x2AA,native_definition_offset=0x99,
            recommended_UI_label='Unknown 0x236',serialization='Preserve unless explicitly edited; no gameplay meaning assigned.')
        fields.extend([low,high])
    layout['fields']=fields
    assert sum(f['size'] for f in fields)==616
    assert [i for f in fields for i in range(f['offset'],f['offset']+f['size'])]==list(range(616))
    return layout

def patch_manufacture(record,value):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=616:raise ValueError('616-byte Door record required')
    if type(value)is not int or not 0<=value<=65535:raise ValueError('ManufToBuild is unsigned16')
    return bytes(record[:0x234])+value.to_bytes(2,'little')+bytes(record[0x236:])
