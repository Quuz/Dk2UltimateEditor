"""Four native callback normalizations and three first-person dialog bindings."""
from pathlib import Path
import hashlib,json,re,struct
from build_storage_paths import converter
from build_effect_semantics import asm_range,pe_read
from build_phase17_semantics import proof
from extract_editor_strings import resource_leaves

ROOT=Path(__file__).resolve().parent

def excerpt(file,line):
    lines=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
    return dict(file=file,line=line,excerpt=lines[line-1].strip())

def block(file,needle,extra):
    lines=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines();hits=[i for i,line in enumerate(lines) if needle in line]
    assert len(hits)==1,(file,needle)
    return [excerpt(file,i+1) for i in range(hits[0],hits[0]+extra)]

def main():
    exe=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    source=(ROOT/'ghidra/creature-record-decoder.txt').read_text(encoding='utf8');lines=source.splitlines()
    aliases=[i for i,line in enumerate(lines) if re.search(r'pcVar9 =',line)]
    assert lines[aliases[0]].strip()=='pcVar9 = param_1;'
    prefix='\n'.join(lines[:aliases[1]]).replace('param_1','param_2').replace('this','param_1').replace('pcVar9','param_2')
    paths=converter(prefix);layouts=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))
    runtime=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']['Creatures']['joined_paths']
    dialogs=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'));dialog=next(d for d in dialogs['dialogs'] if d['resource_path']==[5,310,2057])
    leaf=next(x for x in resource_leaves(exe) if x[0]==dialog['resource_path'])
    constants=[]
    for address,expected in [(0x50AC48,1/4096),(0x50AC58,4096.0),(0x515820,1.0),(0x5163A0,10000.0)]:
        raw=pe_read(exe,address,8);value=struct.unpack('<d',raw)[0];assert value==expected
        constants.append(dict(address=address,value=value,bytes_hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest()))
    model_file='phase17-creature-editor-model.txt'
    rows=[
        dict(offset=0xDBC,width=4,mid=0x11BC,model=0x1788,unit=4096,maximum=4096,replacement=0,
            model_index=0x5E2,model_source='local_14 = (int *)piVar1[0x46f];',guard='_DAT_00515820 < *(double *)(this + 0x5e2)',
            panel=0x88,cid=0x821,label='Waddle Scale:',gui_range=[0,1],
            writer=(0x4A8543,0x4A8554),init=(0x4396D4,0x4396DA),commit=(0x43A15B,0x43A167),ddx=(0x439832,0x439851),game=(0x493C51,0x493C61)),
        dict(offset=0xDC0,width=4,mid=0x11C0,model=0x1790,unit=4096,maximum=4096,replacement=0,
            model_index=0x5E4,model_source='local_14 = (int *)piVar1[0x470];',guard='_DAT_00515820 < *(double *)(this + 0x5e4)',
            panel=0x90,cid=0x741,label='Oscillate Scale:',gui_range=[0,1],
            writer=(0x4A855B,0x4A8570),init=(0x4396E0,0x4396E6),commit=(0x43A16D,0x43A179),ddx=(0x439856,0x439875),game=(0x493C67,0x493C77)),
        dict(offset=0xD3D,width=1,mid=0x113D,model=0x1798,unit=1,maximum=12,replacement=4,
            model_index=0x5E6,model_source='*(undefined1 *)(this + 0x5e6) = *(undefined1 *)((int)piVar1 + 0x113d);',
            guard='0xc < *(byte *)(this + 0x5e6)',panel=0x98,cid=0x743,label='Walk Cycle Scale:',gui_range=[1,12],
            writer=(0x4A8577,0x4A8582),init=(0x4396EC,0x439704),commit=(0x43A17F,0x43A18B),ddx=(0x43987A,0x439894),game=(0x493C7D,0x493C8F)),
        dict(offset=0xCF9,width=4,mid=0x10F9,model=0x1610,unit=4096,maximum=40960000,replacement=0,
            model_index=0x584,model_source='local_14 = *(int **)((int)piVar1 + 0x10f9);',
            guard='_DAT_005163a0 < *(double *)(this + 0x584)',panel=None,cid=None,label=None,gui_range=None,
            writer=(0x4A8406,0x4A8417),init=None,commit=None,ddx=None,game=None)]
    fields=[]
    for r in rows:
        path=next(p for p in paths if p['intermediate_offset']==r['mid'] and p['raw_offset']==r['offset']);assert path['raw_bytes']==r['width']
        baseline=next(f for f in layouts['Creatures']['fields'] if f['offset']==r['offset']);assert baseline['size']==r['width']
        assert next(f for f in layouts['Creatures_5537']['fields'] if f['offset']==r['offset'])['size']==r['width']
        primary=[excerpt('creature-record-decoder.txt',p['line']) for p in path['evidence']]
        if any('pcVar9' in p['excerpt'] for p in primary):primary.insert(0,excerpt('creature-record-decoder.txt',aliases[0]+1))
        primary+=block(model_file,r['model_source'],1 if r['width']==1 else 3)
        primary+=block(model_file,r['guard'],3 if r['width']==1 else 4)
        assert any(f'= {r["replacement"]};' in p['excerpt'] for p in primary)
        primary.append(asm_range('phase17-creature-writer-assembly.txt',*r['writer']))
        binding=None
        if r['panel'] is not None:
            control=next(c for c in dialog['controls'] if c['id']==r['cid']);caption=next(c for c in dialog['controls'] if c['title']==r['label'])
            binding=dict(dialog_resource=310,dialog_panel_offset=r['panel'],model_pointer_panel_offset=0xA0,
                control_id=r['cid'],caption=r['label'],control=control,caption_control=caption,
                validation_range=r['gui_range'],validation_is_integer=r['width']==1)
            primary +=[asm_range('phase17-creature-first-person-init-assembly.txt',*r['init']),
                asm_range('phase17-creature-first-person-commit-assembly.txt',*r['commit']),
                asm_range('phase17-creature-first-person-ddx-assembly.txt',*r['ddx']),
                asm_range('game-creature-type-assembly.txt',*r['game'])]
        fields.append(dict(raw_offset=r['offset'],raw_bytes=r['width'],baseline_name=baseline['name'],
            raw_type='u8' if r['width']==1 else 'u32',editor_intermediate_offset=r['mid'],editor_model_offset=r['model'],
            raw_divisor=r['unit'],normalization=dict(condition='unsigned raw > maximum_preserved_raw',
                maximum_preserved_raw=r['maximum'],replacement_raw=r['replacement'],
                scope='Native editor model callback0x4A01B0 after the presence-controlled assignment; not a game data constraint.'),
            native_dialog=binding,game_storage_paths=[p for p in runtime if int(p['raw_offset'],16)==r['offset']],
            writer_stack=dict(buffer_offset_before_pushes=0x94,extra_pushed_bytes=8 if r['width']==1 else 0,
                destination_offset=0xDD9 if r['width']==1 else r['offset']+0x94),evidence=primary))
    result=dict(family='Creatures',record_variants=[5449,5537],executable_sha256=json.loads((ROOT/'executable-hash-verification.json').read_text()),
        scope=__doc__,fields=fields,editor_constants=constants,
        dialog=dict(resource=dialog['resource_path'],title=dialog['title'],resource_sha256=hashlib.sha256(leaf[3]).hexdigest(),
            constructor=0x4392A0,init=0x439470,ddx=0x439770,commit=0x43A060,
            constructor_evidence=proof('phase17-creature-f55-ui-constructor.txt','FUN_004d9a8b(this,0x136,param_2)'),
            commit_update_call=asm_range('phase17-creature-first-person-commit-assembly.txt',0x43A060,0x43A06A)),
        writer_buffer_evidence=asm_range('phase17-creature-writer-assembly.txt',0x4A8B7D,0x4A8B84),
        limitations=['Static native model/DDX/writer evidence; no modified-data gameplay or native emulation.',
            'The byte value0 is preserved by the callback but falls outside the declared first-person DDX range1..12.',
            'The commit caller does not test the UpdateData return before subsequent writes; full dialog failure behavior is not established.',
            'A larger-than-maximum value is reset, not saturated to the upper bound.',
            'Native editor callback normalization must not be applied implicitly by the independent lossless codec.',
            'Shot Delay GUI control/range and gameplay consequences are not established in this pass.',
            'File-model offsets, first-person dialog offsets and main creature panel offsets identify distinct structures.'])
    (ROOT/'creature-editor-normalization.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Native creature normalizations:',len(fields),'; first-person DDX bindings:3')

if __name__=='__main__':main()
