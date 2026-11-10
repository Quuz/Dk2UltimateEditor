"""Bind selected native editor controls to raw fields using saved Ghidra evidence.

Editor labels, dialog validation ranges and game behavior are separate facts.
No inference from label proximity is made for hidden or orphan static text.
"""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'

def main():
    resources=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))
    data=EXE.read_bytes();assert hashlib.sha256(data).hexdigest()==resources['sha256']
    scale=struct.unpack('<d',pe_read(data,0x50AC48,8))[0]
    inverse=struct.unpack('<d',pe_read(data,0x50AC58,8))[0]
    assert scale==1/4096 and inverse==4096
    def evidence(file,needle):
        text=(ROOT/'ghidra'/file).read_text(encoding='utf8').replace('\r','')
        assert not text.startswith(('Automatic approval review failed:','Unknown tool:'))
        line=next(i for i,s in enumerate(text.splitlines(),1) if needle in s)
        return dict(file=file,line=line,excerpt=text.splitlines()[line-1].strip())
    def control(dialog,ident):
        d=next(d for d in resources['dialogs'] if d['resource_path'][1]==dialog)
        c=next(c for c in d['controls'] if c['id']==ident)
        return dict(dialog=dialog,control_id=ident,control_id_hex=hex(ident),
            resource_path=d['resource_path'],control=c)
    bindings=[]
    # Explicitly paired labels in the visible dialog, checked against DDX and
    # panel/model transfers. Type means editor interpretation, not file rewrite.
    terrain=[
        ('StartingHealth',0x1D7,2,0x28F,0x348,0x5E,10531,'Starting Health:',0,32767,'signed_word'),
        ('MaxHealth',0x1FF,2,0x2B7,0x34A,0x5C,10931,'Max &Health:',0,32767,'signed_word'),
        ('Damage',0x194,2,0x24C,0x368,0x68,1052,'Dama&ge:',-32767,32767,'signed_word_to_int'),
        ('GoldValue',0x19A,2,0x252,0x36A,0x64,1049,'&Gold Value:',0,32767,'unsigned_word_to_int'),
        ('ManaGain',0x19C,2,0x254,0x36C,0x60,1051,'Mana Gain:',-32767,32767,'signed_word'),
        ('MaxManaGain',0x19E,2,0x256,0x36E,0x62,10511,'&Max Mana Gain:',-32767,32767,'signed_word'),
        ('WibbleH',0x1CE,1,0x286,0x360,0x6C,1053,'Wibble H:',0,255,'unsigned_byte'),
        ('WibbleV',0x1D2,1,0x28A,0x361,0x6D,10512,'Wibble V:',0,255,'unsigned_byte'),
        ('Depth',0x188,4,0x240,0x358,0x70,1050,'&Depth:',0,16,'fixed_u32_to_double'),
        ('LightHeight',0x18C,4,0x244,0x318,0x98,1777,'Height:',0,16384,'fixed_u32_to_double')]
    for name,raw,size,mid,model,panel,ident,label,lo,hi,kind in terrain:
        c=control(143,ident)
        d=next(d for d in resources['dialogs'] if d['resource_path'][1]==143)
        idx=d['controls'].index(c['control']);assert d['controls'][idx-1]['title']==label
        assert c['control']['template_visible'] and not c['control']['template_disabled']
        panel_needle=f'+ {hex(panel)})' if panel!=0x64 else '+ 100)'
        bindings.append(dict(family='Terrain',baseline_field=name,raw_offset=raw,raw_bytes=size,
            intermediate_offset=hex(mid),editor_model_offset=hex(model),editor_dialog_offset=hex(panel),
            label=label.replace('&',''),control=c,editor_interpretation=kind,
            editor_validation_range=[lo,hi],editor_scale=scale if kind.startswith('fixed') else 1,
            evidence=[evidence('editor-phase8-terrain-panel-load.txt',panel_needle),
                      evidence('editor-phase8-terrain-ddx-actual.txt',hex(ident))]))
    traps=[
        ('Height',0x1C4,4,0x26C,0x228,0x68,1872,'Hei&ght:',0,1000,'fixed_u32_to_double'),
        ('Health',0x1F0,2,0x298,0x232,0x70,1281,'&Health:',0,32767,'unsigned_word_to_int'),
        ('HealthGain',0x241,2,0x2E9,0x234,0x74,1282,'Health Gain:',0,32767,'signed_word_to_int'),
        ('ManaUsage',0x202,2,0x2AA,0x236,0x60,1786,'Mana &Usage:',-32767,32767,'signed_word_to_int'),
        ('GoldCost',0x1F2,2,0x29A,0x230,0x7C,12983,'Mana &Cost:',0,32767,'unsigned_word_to_int'),
        ('ManaCostToFire',0x1D4,4,0x27C,0x23C,0x84,1799,'&Mana Cost To Fire:',0,16000,'raw_u32_to_int')]
    for name,raw,size,mid,model,panel,ident,label,lo,hi,kind in traps:
        c=control(326,ident)
        d=next(d for d in resources['dialogs'] if d['resource_path'][1]==326)
        idx=d['controls'].index(c['control']);assert d['controls'][idx-1]['title']==label
        bindings.append(dict(family='Traps',baseline_field=name,raw_offset=raw,raw_bytes=size,
            intermediate_offset=hex(mid),editor_model_offset=hex(model),editor_dialog_offset=hex(panel),
            label=label.replace('&',''),control=c,editor_interpretation=kind,
            editor_validation_range=[lo,hi],editor_scale=scale if kind.startswith('fixed') else 1,
            evidence=[evidence('editor-phase8-traps-panel-load.txt',f'+ {hex(panel)})'),
                      evidence('editor-phase8-traps-ddx.txt',hex(ident))],
            caveat=('Baseline name GoldCost differs from the literal native editor label Mana Cost. Currency use is not established by the label alone.' if name=='GoldCost' else None)))
    hidden=control(326,12984)
    assert not hidden['control']['template_visible'] and hidden['control']['template_disabled']
    static=next(c for c in resources['dialogs'] if c['resource_path'][1]==326)
    mana_label=next(c for c in static['controls'] if c['title']=='&Mana Gen.:')
    assert not mana_label['template_visible']
    unknown=dict(family='Traps',raw_offset='0x204',raw_bytes=2,intermediate_offset='0x2ac',
        editor_model_offset='0x238',game_runtime_offset='0x98',meaning='unresolved',
        evidence=[evidence('editor-phase8-traps-convert.txt','param_1 + 0x204'),
            evidence('editor-phase8-traps-model-load-assembly.txt','004a192d:'),
            evidence('editor-phase8-traps-writer.txt','uStack_60 = (undefined2)piVar1[0x8e]')],
        rejected_label='Mana Gen.',label_status='Hidden static text; no control-to-field binding established.',
        different_field=dict(name='ResearchTime',raw_offset='0x1e8',intermediate_offset='0x290',
            editor_model_offset='0x158',editor_dialog_offset='0x64',control=hidden,
            evidence=[evidence('editor-phase8-traps-model-load-assembly.txt','004a1ab4:'),
                evidence('editor-phase8-traps-panel-load-assembly.txt','00471ac0:'),
                evidence('editor-phase8-traps-ddx-assembly.txt','00470883:')]))
    unlabelled=[dict(family='Terrain',raw_offset='0x198',raw_bytes=2,intermediate_offset='0x250',
        editor_model_offset='0x3d0',meaning='unresolved',
        evidence=[evidence('editor-phase8-terrain-model-load.txt','*(short *)(this + 0xf4)'),
                  evidence('editor-phase8-terrain-writer-assembly.txt','004a78cb:')]),
        dict(family='Terrain',raw_offset='0x224',raw_bytes=4,intermediate_offset='0x2dc',
            editor_model_offset='0x320',editor_scale=scale,inverse_scale=inverse,
            meaning='unresolved; native editor stores an unsigned raw DWORD as scaled double',
            ui_binding='not established in the inspected Terrain properties panel',
            evidence=[evidence('editor-phase8-terrain-model-load.txt','*(double *)(this + 200)'),
                evidence('editor-phase8-terrain-writer-assembly.txt','004a7919:'),
                evidence('editor-phase8-terrain-writer-assembly.txt','004a791f:')]),unknown,
        dict(family='Objects',raw_offset='0x34c',raw_bytes=2,intermediate_offset='0x3cc',
            editor_model_offset='0x3be',meaning='unresolved',
            evidence=[evidence('editor-phase8-candidate-1d80.txt','local_1c + 0x3be')])]
    cost=dict(family='Traps',raw_offset='0x1d4',field='ManaCostToFire',
        condition='editor dialog Require Mana unchecked',flag_raw_offset='0x1ec',flag_mask='0x100',
        behavior='Panel commit writes zero to model+0x23c; otherwise writes dialog+0x84.',
        effect='A native editor properties commit can change this field even without a direct cost edit.',
        evidence=[evidence('editor-phase8-traps-panel-save.txt','param_1 + 0x120'),
                  evidence('editor-phase8-traps-panel-save.txt','+ 0x23c) = 0;'),
                  evidence('editor-phase8-traps-ddx.txt','0x706'),
                  evidence('editor-phase8-traps-flags-captured.txt','bVar2 | 0x100')])
    result=dict(program='DK2 Editor pro.exe',sha256=resources['sha256'],
        scope='Selected raw/intermediate/editor/dialog chains, static examination of the supplied Editor Pro build.',
        warning='Template visibility is not proof of live visibility. Editor labels/ranges are not proof of game behavior or universal file limits.',
        constants=dict(raw_to_editor_double=dict(address='0x50ac48',value=scale),
                       editor_double_to_raw=dict(address='0x50ac58',value=inverse)),
        bindings=bindings,unknown_field_paths=unlabelled,editor_commit_side_effects=[cost],
        orphan_labels=[dict(dialog=326,control=mana_label,conclusion='No proven binding to raw+0x204. Do not rename that field ManaGen.')])
    (ROOT/'editor-field-bindings.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Editor bindings:',len(bindings),'; unknown paths:',len(unlabelled),'; one native commit side effect')

if __name__=='__main__':main()
