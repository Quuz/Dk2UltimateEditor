"""Bind physical Objects resources to native MFC controls and runtime slots."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'

def main():
    data=EXE.read_bytes()
    dialogs=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))
    digest=hashlib.sha256(data).hexdigest()
    assert digest==dialogs['sha256']
    dialog=next(d for d in dialogs['dialogs'] if d['resource_path'][1]==327)
    layout=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))['Objects']
    fields={f['offset']:f for f in layout['fields'] if f['kind']=='ArtResource'}
    def proof(file,needle):
        text=(ROOT/'ghidra'/file).read_text(encoding='utf8').replace('\r','')
        assert not text.startswith(('Automatic approval','The automatic permission','Error:','Unknown tool:'))
        line=next(i for i,v in enumerate(text.splitlines(),1) if needle in v)
        return dict(file=file,line=line,excerpt=text.splitlines()[line-1].strip())
    # Physical file order, not GUI order. Runtime offsets refer to MyObjectDataObj.
    definitions=[
        (0x20,0xA0,0xC8,0x51,1590,0x511788,0x45B350,0x4484B0,'Mesh...','mesh'),
        (0x74,0xF4,0x10,0,1589,0x511770,0x45B290,0x442BE0,'GUI Icon...','gui'),
        (0xC8,0x148,0x6C,None,None,None,None,None,None,None),
        (0x11C,0x19C,0x1C8,0x36,1595,0x5117B8,0x45B4D0,0x472D10,'In Hand Mesh...','inhand'),
        (0x170,0x1F0,0x16C,0x1B,1587,0x5117A0,0x45B410,0x45BA00,'In Hand Icon...','unknown'),
    ]
    for i in range(4):
        definitions.append((0x1C4+i*84,0x244+i*84,0x224+i*92,0x6C+i*27,
            1591+i,0x5117D0+i*24,0x45B590+i*0xC0,0x420D00,f'Additional {i+1}...',f'additional{i+1}'))
    slots=[]
    for raw,mid,model,runtime,cid,entry,handler,getter,title,key in definitions:
        assert fields[raw]['size']==84
        slot=dict(raw_offset=raw,raw_offset_hex=hex(raw),raw_bytes=84,
            baseline_field=fields[raw]['name'],intermediate_offset=mid,editor_model_offset=model,
            native_editor=None,runtime_entry_offset=runtime,
            runtime_observation='No resource load from intermediate+0x148 in the inspected 0x49DD60 body.' if runtime is None else
                'Loaded conditionally via resource service vtable+0x54 and CWorldEntry helper0x504D80.',
            meaning_status='native editor label and game resource loading established' if cid else 'unresolved')
        if cid:
            ctrl=next(c for c in dialog['controls'] if c['id']==cid)
            assert ctrl['title']==title
            words=struct.unpack('<6I',pe_read(data,entry,24))
            assert words==(0x111,0,cid,cid,12,handler),words
            button_file='editor-phase8-object-inhand-handler.txt' if key=='inhand' else f'editor-phase10-object-{key}-button.txt'
            getter_file='editor-phase9-object-inhand-getter-assembly.txt' if key=='inhand' else (
                'editor-phase10-object-additional-getter.txt' if key.startswith('additional') else f'editor-phase10-object-{key}-getter.txt')
            needle=f'FUN_{getter:08x}'
            getneedle='0x1c8' if key=='inhand' else ('0x5c + 0x224' if key.startswith('additional') else
                ('+ 200' if key=='mesh' else '+ '+hex(model)))
            slot['native_editor']=dict(dialog_resource=327,control_id=cid,label=title,
                template_visible=ctrl['template_visible'],template_disabled=ctrl['template_disabled'],
                message_map_entry_address=hex(entry),message_map_words=list(words),
                handler=hex(handler),getter=hex(getter),
                additional_index=int(key[-1])-1 if key.startswith('additional') else None,
                evidence=[proof(button_file,needle),proof(getter_file,getneedle)])
        slots.append(slot)
    assert len(slots)==9 and len({s['raw_offset'] for s in slots})==9
    evidence=[proof('editor-phase10-object-convert.txt','param_1 + 0x170'),
        proof('editor-phase10-object-convert.txt','puVar5 = (undefined4 *)((int)this + 0x1f0)'),
        proof('editor-phase8-candidate-1d80.txt','local_1c + 0x5b,piVar1 + 0x7c'),
        proof('editor-phase9-object-resource-writer.txt','this_00 + 0x16c'),
        proof('editor-phase9-object-resource-writer.txt','puVar2 = auStack_228'),
        proof('editor-phase9-object-resource-writer.txt','&uStack_398,0x37e'),
        proof('game-objects-convert.txt','pcVar8 = param_2 + 0x170'),
        proof('game-objects-convert.txt','puVar5 = param_1 + 0x7c'),
        proof('game-phase10-object-runtime-assembly.txt','0049df84:'),
        proof('game-phase10-object-runtime-assembly.txt','0049df8d:'),
        proof('game-phase10-object-runtime-assembly.txt','0049df9a:'),
        proof('game-phase10-object-runtime-assembly.txt','0049df9d:'),
        proof('game-phase10-object-runtime-assembly.txt','0049deb5:'),
        proof('game-phase10-object-runtime-assembly.txt','0049e080:'),
        proof('game-phase10-object-runtime-assembly.txt','0049e28b:'),
        proof('game-phase10-object-runtime-assembly.txt','0049e325:'),
        proof('game-phase10-object-runtime-assembly.txt','0049e387:'),
        proof('game-phase10-worldentry-load.txt','+ 0xf) = param_2')]
    result=dict(family='Objects',record_bytes=894,resource_bytes=84,
        editor_program='DK2 Editor pro.exe',editor_sha256=digest,
        game_program='DKII-DX.exe',game_sha256=hashlib.sha256((ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()).hexdigest(),
        editor_converter='0x42d570',editor_model_callback='0x4a1d80',editor_writer='0x4a9390',
        game_converter='0x56d620',game_loader='0x49dd60',world_entry_helper='0x504d80',
        slots=slots,evidence=evidence,
        naming_conflict=dict(raw_offset=0x170,baseline_field='kUnknownResource',native_editor_label='In Hand Icon...',
            other_raw_offset=0xC8,other_baseline_field='InHandIconResource',
            rule='Keep both physical slots. UI alias for0x170 must not redirect accesses to baseline0xC8 or copy either resource automatically.'),
        load_conditions=['The ordinary resource-loading path is skipped when DAT_006fcdcf is nonzero.',
            'Each ordinary slot checks its intermediate presence flag, resource-service pointer and nonzero resource type.',
            'Main mesh has an additional alternate-name path for certain object IDs; this schema does not model resource-name substitution.',
            'Successful main-resource load writes a 27-byte CWorldEntry; Start/End handles are populated by separate flag-dependent loads.'],
        preservation='Each resource is84 physical bytes:64-byte name, flags DWORD,12-byte union, type byte,2 type-specific bytes, final byte. Preserve uninterpreted data.',
        limitation='Native GUI binding and definition loading are established. These8 buttons are hidden in the dialog template; dynamic visibility is not established. Full rendering/pickup behavior and the purpose of raw0xC8 remain unresolved; no modified files were run in game.')
    (ROOT/'object-resource-bindings.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects resources:9 physical slots,8 verified native buttons; raw0x170 = native In Hand Icon')

if __name__=='__main__':main()
