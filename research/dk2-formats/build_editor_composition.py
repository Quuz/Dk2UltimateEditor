"""Native ownership of composite editor panels and exact Terrain Lean slots."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parent

def main():
    resources=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))
    def proof(file,needle):
        text=(ROOT/'ghidra'/file).read_text(encoding='utf8').replace('\r','')
        assert not text.startswith(('Automatic approval','The automatic permission','Error:','Unknown tool:'))
        line=next(i for i,v in enumerate(text.splitlines(),1) if needle in v)
        return dict(file=file,line=line,excerpt=text.splitlines()[line-1].strip())
    dialog=next(d for d in resources['dialogs'] if d['resource_path'][1]==230)
    bindings=[]
    for axis,raw,mid,model,panel,first,getter,setter in [
        ('horizontal',0x1CF,0x287,0x362,0x5C,1762,'0x425480','0x4254a0'),
        ('vertical',0x1D3,0x28B,0x365,0x68,1765,'0x425440','0x425460')]:
        for index,label in enumerate(['Top','Middle','Bottom']):
            ctrl=next(c for c in dialog['controls'] if c['id']==first+index)
            assert ctrl['template_visible'] and ctrl['window_class']=={'ordinal':129}
            # Control columns and rows are explicit in the native resource.
            assert ctrl['x']==(46 if axis=='horizontal' else 142)
            assert ctrl['y']==22+index*20
            bindings.append(dict(family='Terrain',axis=axis,position=label.lower(),index=index,
                raw_offset=raw+index,raw_bytes=1,intermediate_offset=hex(mid+index),
                editor_model_offset=hex(model+index),dialog_member_offset=hex(panel+index*4),
                control_id=first+index,editor_range=[0,255],editor_storage='u8, no fixed-point scaling',
                getter=getter,setter=setter))
    lean=dict(dialog_resource=230,dialog_title=dialog['title'],bindings=bindings,
        constructor='0x450060',init='0x4500a0',ddx='0x450130',commit='0x450200',
        getter_bounds='index >= 3 returns zero',setter_bounds='index >= 3 performs no write',
        evidence=[proof('editor-phase9-lean-dialog-ctor.txt','0xe6'),
            proof('editor-phase9-lean-ddx.txt','FUN_00425480'),
            proof('editor-phase9-lean-ddx-actual.txt','0x6e2'),
            proof('editor-phase9-lean-ddx-actual.txt','0x6e7'),
            proof('editor-phase9-lean-get-h.txt','0x362'),
            proof('editor-phase9-lean-get-v.txt','0x365'),
            proof('editor-phase9-lean-set-h.txt','param_1 < 3'),
            proof('editor-phase9-lean-set-v.txt','param_1 < 3'),
            proof('editor-phase9-lean-commit-assembly.txt','00450213:'),
            proof('editor-phase9-lean-commit-assembly.txt','00450259:'),
            proof('editor-phase8-terrain-convert.txt','uVar5 + 0x1cf'),
            proof('editor-phase8-terrain-convert.txt','uVar5 + 0x1d3')])
    room=dict(room_id_raw_offset=0x392,terrain_id_raw_offset=0x394,
        foreign_key='Rooms.TerrainId -> Terrain.TerrainId (raw+0x1D6)',
        room_converter='0x42df90',room_callback='0x49fc30',room_terrain_id_model_offset='0x4e0',
        panel_load='0x4675f0',panel_commit='0x467bb0',
        terrain_pointer_member='0x13c',room_pointer_member='0x144',
        selection='Panel selects Terrain, then scans Rooms for matching TerrainId. Last matching Room wins in that scan.',
        room_lean_button=dict(control_id=1875,handler='0x468900',argument='panel+0x13c (Terrain pointer)',
            target_file='Terrain catalogue in the same actual definition scope',
            warning='No independent Rooms Lean storage is established; do not copy six bytes into the Rooms record.'),
        shared_properties=[dict(name=name,owner_family='Terrain',raw_offset=raw,editor_model_offset=hex(model),
                                panel_member_offset=hex(panel)) for name,raw,model,panel in [
            ('MaxHealth',0x1FF,0x34A,0x7C),('StartingHealth',0x1D7,0x348,0x80),
            ('Depth',0x188,0x358,0x60),('Damage',0x194,0x368,0x68),
            ('WibbleH',0x1CE,0x360,0x90),('WibbleV',0x1D2,0x361,0x94),
            ('LightHeight',0x18C,0x318,0xE8)]],
        evidence=[proof('editor-phase9-room-convert.txt','param_1[0x394]'),
            proof('editor-phase9-room-model-candidate.txt','*(char *)(this + 0x138)'),
            proof('editor-phase9-room-panel-load.txt','this + 0x13c) = iVar3'),
            proof('editor-phase9-room-panel-load.txt','iVar3 + 0x4e0'),
            proof('editor-phase8-room-lean-handler.txt','param_1 + 0x13c'),
            proof('editor-phase9-room-panel-commit.txt','+ 0x13c) + 0x348)')])
    resets=[dict(field=name,owner_family='Terrain',raw_offset=raw,raw_bytes=2,
                 editor_model_offset=hex(model),assigned_value=0)
            for name,raw,model in [('GoldValue',0x19A,0x36A),('ManaGain',0x19C,0x36C),('MaxManaGain',0x19E,0x36E)]]
    # Translate only the unconditional boolean assignments actually present in
    # the inspected Room commit, using the exact native flag getter.
    low_flags=[(0x374,1,0),(0x378,2,0),(0x37C,4,1),(0x380,8,0),(0x384,16,1),
        (0x38C,64,0),(0x390,128,0),(0x394,256,0),(0x398,512,0),(0x39C,1024,0),
        (0x3A0,2048,0),(0x3A4,4096,0),(0x3A8,8192,0),(0x3AC,16384,0)]
    effects=dict(scalar_resets=resets,
        forced_terrain_flags=dict(raw_offset=0x190,raw_bytes=4,
            affected_mask=sum(bit for _,bit,_ in low_flags),forced_value=sum(bit for _,bit,on in low_flags if on),
            assignments=[dict(model_offset=hex(model),mask=hex(bit),value=on) for model,bit,on in low_flags],
            preserved_from_panel='Attackable bit0x20 is assigned separately from dialog+0x128.',
            scope='Known unconditional assignments only; not a full reconstruction of the editor flag serializer.'),
        evidence=[proof('editor-phase9-room-panel-commit-assembly.txt','00467bfe:'),
            proof('editor-phase9-room-panel-commit-assembly.txt','00467c0b:'),
            proof('editor-phase9-room-panel-commit-assembly.txt','00467c18:'),
            proof('editor-phase9-room-panel-commit.txt','+ 0x37c) = 1;'),
            proof('editor-phase9-room-panel-commit.txt','+ 900) = 1;'),
            proof('editor-phase9-terrain-flags-writer.txt','bVar4 | 0x10')],
        warning='These are native GUI side effects on linked Terrain. A lossless editor must not silently apply them on ordinary serialization.')
    assert effects['forced_terrain_flags']['affected_mask']==0x7FDF
    assert effects['forced_terrain_flags']['forced_value']==0x14
    commit=(ROOT/'ghidra/editor-phase9-room-panel-commit.txt').read_text()
    getter=(ROOT/'ghidra/editor-phase9-terrain-flags-writer.txt').read_text()
    for model,bit,on in low_flags:
        spelling='900' if model==0x384 else hex(model)
        assert f'+ {spelling}) = {on};' in commit,(hex(model),on)
        assert f'+ {spelling})' in getter,hex(model)
    result=dict(program='DK2 Editor pro.exe',sha256=resources['sha256'],
        terrain_lean=lean,room_terrain_panel=room,room_commit_side_effects=effects,
        resource_constraints=[dict(family='Objects',raw_offset=0x170,raw_bytes=84,
            intermediate_offset='0x1f0',editor_model_offset='0x16c',meaning='Native Editor In Hand Icon UI alias; see object-resource-bindings.json for the complete proof and baseline naming conflict.',
            known_different_field=dict(name='InHandMeshResource',raw_offset=0x11C,
                intermediate_offset='0x19c',editor_model_offset='0x1c8',
                button_id=1595,handler='0x45b4d0',getter='0x472d10'),
            conclusion='In Hand Mesh button targets a different resource. Unknown slot must retain its own full84 bytes.',
            evidence=[proof('editor-phase8-candidate-1d80.txt','local_1c + 0x5b,piVar1 + 0x7c'),
                proof('editor-phase9-object-inhand-getter-assembly.txt','0x1c8'),
                proof('editor-phase9-object-resource-writer.txt','this_00 + 0x16c'),
                proof('editor-phase9-object-resource-writer.txt','this_00 + 0x1c8')])],
        limitation='Static editor ownership/serialization evidence. Byte meanings are not a complete simulation of geometry or gameplay.')
    (ROOT/'editor-composition.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Editor composition: 6 Lean slots; linked Room/Terrain ownership; 3 scalar resets; exact forced flag mask; resource distinction')

if __name__=='__main__':main()
