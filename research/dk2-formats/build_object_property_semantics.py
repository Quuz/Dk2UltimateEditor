"""Native scalar GUI bindings, fixed-point views and object flag commits."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read,asm_range

ROOT=Path(__file__).resolve().parent

def main():
    def proof(file,needle):
        lines=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines();i=next(i for i,line in enumerate(lines) if needle in line)
        return dict(file=file,line=i+1,excerpt=lines[i].strip())
    exes=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for f,digest in exes.items():assert hashlib.sha256((ROOT.parent.parent/f).read_bytes()).hexdigest()==digest
    editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    constants={hex(a):struct.unpack('<d',pe_read(editor,a,8))[0] for a in [0x50AC48,0x50AC58,0x5163B0,0x5163C8]}
    assert list(constants.values())==[1/4096,4096,1/65536,65536]
    ds=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))['dialogs']
    dialogs={i:next(d for d in ds if d['resource_path'][1]==i) for i in [256,327]}
    baseline={f['offset']:f for f in json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))['Objects']['fields']}
    ddx=(ROOT/'ghidra/phase16-object-ddx-assembly.txt').read_text().splitlines()
    definitions=[
        ('Width',0x32C,4,0x3AC,0x3A8,0x60,1299,'&Width:','scaled_u32',4096,8,0xF8),
        ('Height',0x330,4,0x3B0,0x3B0,0x68,1298,'&Height:','scaled_u32',4096,16,0xFA),
        ('Mass',0x334,4,0x3B4,0x130,0x70,1305,'&Mass:','scaled_u32',4096,16000,0xFC),
        ('Speed',0x338,4,0x3B8,0x398,0x78,1312,'Speed:','scaled_u32',4096,16000,0x100),
        ('AirFriction',0x33C,4,0x3BC,0x3A0,0x80,1314,'Air Friction:','scaled_u32',65536,16000,0xFE),
        ('Health',0x348,2,0x3C8,0x3B8,0x88,1304,'H&ealth:','u16',1,32000,0x104),
        ('MaxAngle',0x34A,2,0x3CA,0x3BA,0x98,1316,'Max Angle:','u16',1,16000,0x102),
        ('ManaGen',0x34C,2,0x3CC,0x3BE,0x8C,1318,'Mana Gen:','s16',1,32000,0x106),
        ('ManaValue',0x34E,2,0x3CE,0x3C0,0x90,1320,'Mana Value:','s16',1,32000,0x108),
        ('RoomCapacity',0x35C,1,0x3DC,0x3BC,0x94,1302,'Room &Capacity:','u8',1,255,0x114),
        ('PickUpPriority',0x35D,1,0x3DD,0x3CE,0xA0,1324,'Pick-Up Priority:','u8',1,255,0x112)]
    fields=[]
    for name,raw,width,mid,model,panel,cid,label,kind,divisor,maximum,runtime in definitions:
        assert baseline[raw]['size']==width
        native=[]
        for did,d in dialogs.items():
            control=next(c for c in d['controls'] if c['id']==cid);caption=next(c for c in d['controls'] if c['title']==label)
            native.append(dict(dialog_resource=did,control=control,caption=caption))
        start=next(i for i,line in enumerate(ddx) if f'LEA EBX,[EDI + {hex(panel)}]' in line)
        stop=next(i for i in range(start,start+9) if 'CALL 0x004e' in ddx[i])
        assert any(f'PUSH {hex(cid)} ' in line+' ' for line in ddx[start:stop])
        ddx_evidence=asm_range('phase16-object-ddx-assembly.txt',int(ddx[start][:8],16),int(ddx[stop][:8],16))
        fields.append(dict(name=name,baseline_name=baseline[raw]['name'],baseline_kind=baseline[raw]['kind'],raw_offset=raw,raw_bytes=width,
            intermediate_offset=mid,editor_model_offset=model,panel_offset=panel,control_id=cid,label=label,
            native_editor_kind=kind,divisor=divisor,gui_range=[0,maximum],native_editor=native,ddx_evidence=ddx_evidence,
            runtime_offset=runtime,runtime_bytes=1 if width==1 else 2,
            runtime_load_rule='unsigned DWORD/TPS, low16, then replace zero with1 if original raw!=0' if name=='Speed' else 'copy low16' if width==4 else 'copy',
            meaning_status='Native editor label established; gameplay generation effect remains unresolved' if name=='ManaGen' else 'Native GUI label/view and runtime storage; not proof of every gameplay unit'))
    models=[0x3D0,0x3D4,0x3D8,0x3DC,0x3E4,0x3E8,0x3EC,0x3F0,0x3FC,0x400,0x404,0x3E0,0x408,0x3F4,0x3F8,0x40C,0x410,0x414,0x418,0x41C,0x420,0x424,0x428,0x42C,0x430]
    bits=[0,1,2,3,4,5,6,7,8,9,10,12,13,14,15,16,17,18,20,21,22,23,24,25,26]
    enums=json.loads((ROOT/'enums.json').read_text(encoding='utf8'))['GameObject.ObjectFlag']
    flags=[dict(mask=1<<bit,editor_model_offset=model,baseline_name=enums[str(1<<bit)]) for bit,model in zip(bits,models)]
    supported=sum(f['mask'] for f in flags);assert supported==0x07F7F7FF
    choices=[dict(index=0,mask=0,label='&Normal',control_id=1398)]
    for index,(mask,cid,label) in enumerate([(4,1391,'&Special'),(8,13921,'Spell &Book'),(0x1000,13922,'Level Gem'),(0x10,1394,'Crate'),(0x20,1396,'&Lair'),(0x40,1401,'&Gold'),(0x80,1402,'&Food')],1):
        for d in dialogs.values():assert next(c for c in d['controls'] if c['id']==cid)['title']==label
        choices.append(dict(index=index,mask=mask,label=label,control_id=cid))
    emulations=[]
    for p in sorted((ROOT/'ghidra').glob('phase16-emulation-object-flags-*.json')):
        v=json.loads(p.read_text());r=v['result'];assert r['success'] and r['hit_return'] and int(r['registers']['EAX'],16)==v['raw_flags']&supported
        emulations.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    assert len(emulations)==4
    result=dict(family='Objects',record_bytes=894,executables=exes,fields=fields,editor_constants=constants,
        functions=dict(constructor='0x459270',fill='0x45a400',ddx='0x4597b0',commit='0x45a870',model_load='0x4a1d80',writer='0x4a9390'),
        dialog_note='Constructor requests resource256. Alternate resource327 has matching IDs/captions but different visibility/disabled bits. Template flags are not a claim about the active UI.',
        mana_gen=dict(raw_offset=0x34C,raw_bytes=2,intermediate_offset=0x3CC,editor_model_offset=0x3BE,panel_offset=0x8C,game_runtime_offset=0x106,
            label='Mana Gen:',control_id=1318,editor_read='signed16',gui_range=[0,32000],gameplay_effect='unresolved',
            evidence=[proof('editor-phase10-object-convert.txt','param_1 + 0x34c'),
                proof('editor-phase8-candidate-1d80.txt','local_1c + 0x3be'),
                asm_range('phase16-object-panel-fill-assembly.txt',0x45A531,0x45A53E),
                asm_range('phase16-object-ddx-assembly.txt',0x4598FA,0x459917),
                proof('phase16-object-panel-commit.txt','param_1 + 0x8c'),
                asm_range('phase16-object-writer-assembly.txt',0x4A955A,0x4A9561),
                proof('game-objects-runtime.txt','local_10c + 0x106')]),
        unsigned_scaled_views=dict(rule='The editor zero-extends each raw DWORD into a64-bit integer before FILD, then multiplies by an exact reciprocal power of two.',
            baseline_conflict='These native editor views are unsigned; baseline i32/4096 and i32/65536 names are preserved only as source history.',
            evidence=[asm_range('phase16-object-model-load-assembly.txt',0x4A1EF1,0x4A1FA4),
                asm_range('phase16-object-writer-assembly.txt',0x4A94C4,0x4A9535)]),
        speed_load=dict(raw_offset=0x338,intermediate_offset=0x3B8,runtime_offset=0x100,runtime_bytes=2,
            rule='quotient=u32(raw)/positive_TPS; bits=quotient&65535; if bits==0 and raw!=0:bits=1',
            example=dict(raw=1310720,tps=20,quotient=65536,runtime_bits=1),
            evidence=[asm_range('game-phase10-object-runtime-assembly.txt',0x49DE10,0x49DE37)]),
        flags=dict(raw_offset=0x344,intermediate_offset=0x3C4,runtime_offset=0xF4,supported_mask=supported,dropped_mask=0xFFFFFFFF^supported,
            decode='0x420e80',encode='0x420d20',flags=flags,
            object_type_radio=dict(panel_offset=0x5C,type_mask=0x10FC,choices=choices,
                fill_rule='Select the first set bit in choice order1..7; if none, select Normal0.',
                commit_rule='Clear all7 model type booleans and set only the selected index1..7. Other checkboxes are assigned independently.',
                evidence=[proof('phase16-object-panel-fill.txt','piVar1[0xf6] == 0'),proof('phase16-object-panel-commit.txt','switch(*(undefined4 *)(param_1 + 0x5c))')]),
            evidence=[proof('phase16-object-flags-decode.txt','param_1 >> 0x1a & 1'),
                proof('phase16-object-flags-encode-verified.txt','uVar1 | 0x4000000'),proof('phase16-object-raw-writer.txt','uStack_54 = FUN_00420d20')],
            rule='Decode/encode loses unsupported bits. Accepting the properties panel also reduces the7 object-type bits to a single choice. Do not apply either projection implicitly in a lossless serializer.'),
        native_flag_emulations=emulations,
        full_text_searches=[dict(file=f,sha256=hashlib.sha256((ROOT/f).read_bytes()).hexdigest()) for f in ['phase16-full-game-unknown-candidates.json','phase16-full-editor-unknown-candidates.json']],
        search_limitation='Full .text linear and displacement-anchored searches include false instruction starts and unrelated structures. They do not rule out computed pointers, other binaries or indirect consumption.',
        limitation='Native GUI bindings/quantization, selected runtime loads and isolated flag helpers. No modified file executed; Mana Gen gameplay effect and Terrain unknown meanings remain unresolved.')
    if (ROOT/'entity-definition-semantics.json').exists():
        from build_entity_definition_semantics import link
        result['phase62_old_entity_definitions']=link('Objects')
    if (ROOT/'object-indexed-consumer-review.json').exists():result['phase70_indexed_review']=json.loads((ROOT/'object-indexed-consumer-review.json').read_text(encoding='utf8'))
    if (ROOT/'object-loading-semantics.json').exists():result['phase71_loading']=json.loads((ROOT/'object-loading-semantics.json').read_text(encoding='utf8'))
    (ROOT/'object-property-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects:11 scalar bindings, Mana Gen UI label,25 flag bits,7 type radio choices')

if __name__=='__main__':main()
