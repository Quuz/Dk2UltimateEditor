"""Native Room construction names, resource requests and unresolved-slot traces."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read,asm_range

ROOT=Path(__file__).resolve().parent
EDITOR=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
GAME=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    editor=EDITOR.read_bytes();game=GAME.read_bytes()
    def proof(file,needle):
        text=(ROOT/'ghidra'/file).read_text(encoding='utf8').replace('\r','')
        assert not text.startswith(('Automatic approval','The automatic permission','Error:'))
        line=next(i for i,v in enumerate(text.splitlines(),1) if needle in v)
        return dict(file=file,line=line,excerpt=text.splitlines()[line-1].strip())
    def cstring(data,address):return pe_read(data,address,64).split(b'\0',1)[0].decode('ascii')
    construction=[]
    baseline=json.loads((ROOT/'enums.json').read_text(encoding='utf8'))['Room.TileConstruction']
    for i in range(32):
        address=0x513438+i*8
        value,pointer=struct.unpack('<II',pe_read(editor,address,8))
        if value&255==255:break
        raw=pe_read(editor,pointer,128)
        length=next(k for k in range(0,len(raw)-1,2) if raw[k:k+2]==b'\0\0')
        label=raw[:length].decode('utf-16-le')
        construction.append(dict(value=value,native_editor_label=label,baseline_name=baseline.get(str(value)),
            table_address=hex(address),string_address=hex(pointer)))
    assert [r['value'] for r in construction]==list(range(1,14))
    # DWORD table values are read as low-byte IDs by the native combo initializer.
    forced=[];mapping=pe_read(game,0x4EBF0C,13)
    targets=[struct.unpack('<I',pe_read(game,0x4EBEF4+4*i,4))[0] for i in range(max(mapping)+1)]
    for i,index in enumerate(mapping):
        assert targets[index] in (0x4EADCF,0x4EADD6)
        if targets[index]==0x4EADCF:forced.append(i+1)
    assert forced==[1,5,7,9,12,13]
    # Index is the numeric suffix; entry_index is its position in the runtime array.
    indexed={1:[0,1,2,6,7,8,9],2:list(range(4)),3:list(range(9)),4:list(range(3)),
        5:list(range(10)),6:[],7:list(range(15)),8:list(range(4)),9:list(range(1,10)),
        10:[],11:list(range(4)),12:list(range(1,17)),13:list(range(7))}
    ranges={1:(0x4EAF60,0x4EB02F),2:(0x4EB03A,0x4EB0C1),3:(0x4EB210,0x4EB297),
        4:(0x4EB29E,0x4EB325),5:(0x4EB6A0,0x4EB727),6:(0x4EB72E,0x4EB730),
        7:(0x4EB0C8,0x4EB14F),8:(0x4EB32C,0x4EB423),9:(0x4EB42A,0x4EB4B1),
        10:(0x4EB62E,0x4EB696),11:(0x4EB4B8,0x4EB595),12:(0x4EB5A0,0x4EB627),13:(0x4EB72E,0x4EB80B)}
    for row in construction:
        value=row['value'];row.update(indexed_suffixes=indexed[value],
            indexed_runtime_base=0xC1,indexed_runtime_stride=27,
            indexed_runtime_uses_suffix_as_index=value not in (9,12),
            indexed_name_rule='remove the last byte of the base name, then append suffix' if value==1 else 'append suffix to the full base name',
            exact_base_resource_runtime_offset=0x1B if value==1 else (0xC1 if value==10 else None),
            forces_runtime_flag_0x8=value in forced,
            evidence=asm_range('game-phase11-room-loader-assembly.txt',*ranges[value]))
        if value==8:row['destroyed_resources']=dict(indices=list(range(6)),name_rule='Dungeon_Destroyed{index}',runtime_base=0x271,requires_nonempty_base=False)
        if value==11:row['destroyed_resources']=dict(indices=list(range(4)),name_rule='{base}Destroyed{index}',runtime_base=0x271,requires_nonempty_base=True)
        if value==13:row['destroyed_resources']=dict(indices=list(range(7)),name_rule='{base}{index}_DESTROYED',runtime_base=0x271,requires_nonempty_base=True)
        if value==6:row['limitation']='Listed by the native editor, but no floor-resource generation branch exists for value6 in this inspected loader. This does not prove absence of all rendering support.'
    resource_bindings=[]
    # These are every direct ArtResource button in the inspected Room panel.
    for raw,model,cid,entry,handler,getter,label,bfile,gfile,needle in [
        (0x20,0x10,1744,0x513540,0x468A50,0x442BE0,'GUI Icon...','editor-phase11-room-gui-button.txt','editor-phase10-object-gui-getter.txt','+ 0x10'),
        (0xC8,0x16C,1746,0x513558,0x468B10,0x45BA00,'Complete Mesh...','editor-phase11-room-complete-button.txt','editor-phase10-object-unknown-getter.txt','+ 0x16c'),
        (0x11C,0x1C8,1747,0x513570,0x468BD0,0x472D10,'Straight Mesh...','editor-phase11-room-straight-button.txt','editor-phase9-object-inhand-getter-assembly.txt','0x1c8'),
        (0x170,0x224,1748,0x513588,0x468C90,0x4484C0,'Inside Corner Mesh...','editor-phase11-room-inside-button.txt','editor-phase11-room-inside-getter.txt','+ 0x224'),
        (0x218,0x2DC,1749,0x5135A0,0x468D50,0x469410,'Outside Corner Mesh...','editor-phase11-room-outside-button.txt','editor-phase11-room-outside-getter.txt','+ 0x2dc'),
        (0x26C,0x338,1750,0x5135B8,0x468E10,0x469420,'Wall Mesh...','editor-phase9-room-wall-handler.txt','editor-phase11-room-wall-getter.txt','+ 0x338'),
        (0x2C0,0x394,1751,0x5135D0,0x468ED0,0x469430,'Cap Mesh...','editor-phase11-room-cap-button.txt','editor-phase11-room-cap-getter.txt','+ 0x394'),
        (0x3C7,0x3F0,12424,0x5135E8,0x468F90,0x469440,'Torch Mesh...','editor-phase11-room-torch-button.txt','editor-phase9-room-mesh-getter-unknown1.txt','+ 0x3f0'),
        (0x314,0x44C,1752,0x513600,0x469050,0x469450,'Ceiling Resource...','editor-phase9-room-mesh-handler-unknown1.txt','editor-phase11-room-ceiling-getter.txt','+ 0x44c')]:
        words=struct.unpack('<6I',pe_read(editor,entry,24));assert words==(0x111,0,cid,cid,12,handler)
        dialog=next(d for d in json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))['dialogs'] if d['resource_path'][1]==251)
        ctrl=next(c for c in dialog['controls'] if c['id']==cid);assert ctrl['title']==label
        resource_bindings.append(dict(raw_offset=raw,raw_bytes=84,editor_model_offset=model,control_id=cid,label=label,
            entry_address=hex(entry),handler=hex(handler),getter=hex(getter),template_visible=ctrl['template_visible'],
            evidence=[proof(bfile,f'FUN_{getter:08x}'),proof(gfile,needle)]))
    result=dict(family='Rooms',record_bytes=1055,editor_sha256=hashlib.sha256(editor).hexdigest(),game_sha256=hashlib.sha256(game).hexdigest(),
        game_intermediate_initializer='0x56de40',game_converter='0x56dfd0',game_loader='0x4eacc0',
        construction_raw_offset=0x395,construction_intermediate_offset=0x469,construction_runtime_offset=0x452,
        complete_resource_raw_offset=0xC8,complete_resource_intermediate_offset=0x19C,
        native_construction_table_address='0x513438',native_combo_initializer='0x466e90',construction=construction,
        format_strings={hex(a):cstring(game,a) for a in (0x69FF1C,0x6974FC,0x69FF0C,0x6A2EB4,0x6A2EBC,0x6A2EC8,0x6A2ED4)},
        ordinary_load_gate='world+0xA3C7 ==0 and nonnull resource service; applies before all floor branches',
        runtime_flags=dict(raw_offset=0x370,raw_bytes=4,runtime_offset=0x43D,runtime_bytes=2,
            forced_bit=8,forced_for_construction=forced,rule='low16(raw Flags) OR8 in the listed modes; no raw writeback',
            dispatch_byte_table='0x4ebf0c',dispatch_targets='0x4ebef4',
            evidence=asm_range('game-phase11-room-loader-assembly.txt',0x4EAD83,0x4EADCF)),
        water_special=dict(construction=7,room_id=13,requires_nonempty_base=True,
            requests=[dict(name='Water',resource_type=2,service_flags=0x20000,runtime_offset=0x256),dict(name='Water',resource_type=1,service_flags=0x20000,runtime_offset=0xA6)],
            evidence=asm_range('game-phase11-room-loader-assembly.txt',0x4EB151,0x4EB1CD)),
        native_resource_buttons=resource_bindings,
        unresolved_resource=dict(raw_offset=0x1C4,raw_bytes=84,intermediate_offset=0x298,editor_model_offset=0x280,
            meaning='unresolved',editor_converter='0x42df90',editor_model_callback='0x49fc30',editor_writer='0x4a7ac0',
            observation='All9 inspected direct resource buttons skip this slot. No load from intermediate0x298 in the inspected game0x4EACC0 body. Neither observation proves global non-use.',
            evidence=[proof('editor-phase9-room-convert.txt','param_1 + 0x1c4'),proof('editor-phase9-room-convert.txt','this + 0x298'),
                proof('editor-phase9-room-model-candidate.txt','this + 0xa0,piVar7 + 0xa6'),
                proof('rooms-catalogue-writer.txt','iVar2 + 0x280'),proof('rooms-catalogue-writer.txt','puVar9 = auStack_274'),
                proof('rooms-catalogue-writer.txt','&uStack_438,0x41f')]),
        evidence=[proof('editor-phase11-room-construction-options-init.txt','local_24 = &DAT_00513438'),
            proof('editor-phase11-room-construction-options-init.txt','local_24 + 2'),
            proof('game-phase11-room-intermediate-init.txt','puVar2 = param_1 + 0x35'),
            proof('game-phase11-room-intermediate-init.txt','iVar1 = 0x107'),
            proof('game-rooms-convert.txt','param_2[0x395]'),proof('game-rooms-convert.txt','param_2 + 0x370'),
            proof('game-rooms-runtime.txt','cVar4 = *(char *)(param_1 + 0x452)')],
        limitation='Static resource request plan for fully converted Room definitions. It does not prove asset availability, successful loads, final tile placement or dynamic GUI visibility. Start/End resource loads are outside the request projector.')
    (ROOT/'room-resource-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Room semantics:13 native construction labels;9 resource buttons;12 floor branches; unresolved0x1C4 preserved')

if __name__=='__main__':main()
