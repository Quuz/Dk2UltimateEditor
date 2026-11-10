"""Current native Trap WORD204 view; historical byte layout remains auditable."""
from pathlib import Path
import copy,json
ROOT=Path(__file__).resolve().parent
def effective_trap_layout():
    layout=copy.deepcopy(json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))['Traps'])
    old=[f for f in layout['fields'] if f['offset'] in [0x204,0x205]]
    assert [(f['offset'],f['size'],f['kind']) for f in old]==[(0x204,1,'u8'),(0x205,1,'u8')]
    fields=[]
    for f in layout['fields']:
        if f['offset']==0x205:continue
        if f['offset']!=0x204:fields.append(f);continue
        fields.append(dict(name='Unknown204',offset=0x204,offset_hex='0x204',size=2,kind='u16',
            semantics='Unresolved single nativeWORD. Unsigned display stores its16bits; native arithmetic signedness/gameplay meaning not established.',
            legacy_byte_fields=old,native_intermediate_offset=0x2AC,native_editor_model_offset=0x238,native_definition_offset=0x98,
            recommended_UI_label='Unknown 0x204 (16-bit)',serialization='Edit both bytes as one rawWORD; preserve all other record bytes. Do not interpret as two unrelated native properties.'))
    layout['fields']=fields
    assert [i for f in fields for i in range(f['offset'],f['offset']+f['size'])]==list(range(579))
    return layout
