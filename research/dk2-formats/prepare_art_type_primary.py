"""Primary Pro type-edit spans, native UI tables and inherited exact MFC writer."""
from pathlib import Path
import json,re,sys,struct,hashlib
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import prepare_room_model_primary as common
import extract_editor_dialogs as dialogs

ROOT=Path(__file__).resolve().parent;EXE=common.EXE
FRESH=[0x41BBB0,0x41BC50,0x41C080,0x41C090,0x41C240,0x433C40,0x4333D0,0x433CF0,0x4349F0,0x434650,0x465160]
BLOCKS=[(0x50C0F8,80,'ArtResource type table'),(0x513198,80,'second identical type table'),
    (0x50C148,248,'ArtResource message map'),(0x532AE4,196,'type labels1..8 UTF16'),
    (0x52F8F4,14,'type0 UTF16 label'),(0x532BA8,3,'type7 Field1 ANSI label ID'),
    (0x57A920,2530,'native dialog276 language2057')]
def sha(b):return hashlib.sha256(b).hexdigest()
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))


def seed():
    data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);out=[]
    for address in FRESH:
        name=f'ghidra/phase130-editor-{address:06x}-assembly.txt'
        pcs=[int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',(ROOT/name).read_text(encoding='utf8'),re.M)]
        assert pcs and pcs[0]==address
        ins=[next(md.disasm(pe_read(data,pc,15),pc)) for pc in pcs]
        assert all(i.address+i.size<=j.address for i,j in zip(ins,ins[1:]))
        gaps=[dict(address=i.address+i.size,length=j.address-i.address-i.size) for i,j in zip(ins,ins[1:]) if i.address+i.size<j.address]
        out.append(dict(address=address,length=ins[-1].address+ins[-1].size-address,
            assembly=name,assembly_sha256=sha((ROOT/name).read_bytes()),instructions=len(pcs),unlisted_intervals=gaps))
    return out


def build():
    data=EXE.read_bytes();rows=seed()
    for row in rows:
        name=f'ghidra/phase130-primary-editor-{row["address"]:06x}.txt';value=load(name);blob=bytes(value['data'])
        assert int(value['address'],16)==row['address'] and len(blob)==row['length'] and blob==pe_read(data,row['address'],row['length'])
        row.update(memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(blob))
    blocks=[]
    for address,length,label in BLOCKS:
        name=f'ghidra/phase130-primary-block-{address:06x}.txt';value=load(name);blob=bytes(value['data'])
        assert int(value['address'],16)==address and len(blob)==length and blob==pe_read(data,address,length)
        blocks.append(dict(address=address,length=length,label=label,memory=name,memory_sha256=sha((ROOT/name).read_bytes()),bytes_sha256=sha(blob)))
    assert pe_read(data,0x50C0F8,80)==pe_read(data,0x513198,80)
    table=[]
    for index in range(9):
        ident,p=struct.unpack('<2I',pe_read(data,0x50C0F8+8*index,8));units=[]
        while True:
            two=pe_read(data,p+len(units)*2,2)
            if two==b'\0\0':break
            units.append(two);assert len(units)<100
        table.append(dict(id=ident,string_pointer=p,label=b''.join(units).decode('utf-16le')))
    assert [r['id'] for r in table]==list(range(9)) and table[7]['label']=='Procedural Mesh'
    dialog=next(d for d in dialogs.extract(data) if d['resource_path']==[5,276,2057])
    current=next(d for d in load('editor-dialogs.json')['dialogs'] if d['resource_path']==[5,276,2057]);assert dialog==current
    assert pe_read(data,0x532BA8,3)==b'ID\0'
    map_rows=[]
    for index in range(9):
        address=0x50C150+24*index;v=struct.unpack('<6I',pe_read(data,address,24))
        map_rows.append(dict(address=hex(address),message=v[0],notification=v[1],control_from=v[2],control_to=v[3],signature=v[4],handler=hex(v[5])))
    assert map_rows==load('phase130-art-message-map.json')
    return dict(phase=130,executable=str(EXE),sha256=sha(data),fresh_functions=rows,blocks=blocks,
        inherited=common.build(),type_labels=table,dialog276=dialog,message_map=map_rows,
        scope='11whole Ghidra spans and7primary blocks plus exact frozen31span MFC/Room manifest. UI bodies static; original helper/model/writer CPU paths are recorded separately. Listed instruction starts and full byte ranges attested; unlisted intervals are retained as bytes, not asserted instructions.')


if __name__=='__main__':
    if '--queries' in sys.argv:
        qs=[dict(id=f'phase130-primary-editor-{r["address"]:06x}',tool='read_memory',args=dict(program='DK2 Editor pro.exe',address=hex(r['address']),length=r['length']),print_limit=0) for r in seed()]
        for address,length,_ in BLOCKS:qs.append(dict(id=f'phase130-primary-block-{address:06x}',tool='read_memory',args=dict(program='DK2 Editor pro.exe',address=hex(address),length=length),print_limit=0))
        (ROOT/'phase130-primary-queries.json').write_text(json.dumps(qs,indent=2),encoding='utf8');print('Prepared',len(qs),'whole function/block reads')
    else:
        value=build();(ROOT/'phase130-art-type-primary.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
        print('Attested11functions/7blocks and inherited31MFC/Room spans')
