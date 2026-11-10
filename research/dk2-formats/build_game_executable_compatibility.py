"""Publish actual normal/DX static code identity and scoped C8 lead review."""
from pathlib import Path
import json
import scan_game_executable_variants as variants
import emulate_normal_executable_controls as native
import native_game_executable_profiles as api
from build_effect_semantics import pe_read
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=variants.sha((ROOT/n).read_bytes()))

def primary():
    rows=[];editor=(variants.GAME/'Data/editor/DK2 Editor pro.exe').read_bytes()
    for row in load('phase124-editor-primary-seed.json'):
        name=f'ghidra/phase124-editor-primary-{row["address"]:06x}.txt';saved=load(name);b=bytes(saved['data'])
        assert int(saved['address'],16)==row['address'] and len(b)==row['length'] and b==pe_read(editor,row['address'],row['length'])
        rows.append(dict(row,memory=link(name),assembly_sha256=variants.sha((ROOT/row['assembly']).read_bytes()),original_bytes_sha256=variants.sha(b)))
    dx=(variants.GAME/'DKII-DX.exe').read_bytes();blocks=[]
    for a,n,name in [(0x68C8D8,10,'phase124-DX-import-library-name.txt'),(0x66C020,8,'phase124-DX-ddraw-IAT.txt')]:
        file='ghidra/'+name;saved=load(file);b=bytes(saved['data'])
        assert len(b)==n and b==pe_read(dx,a,n)
        blocks.append(dict(address=a,length=n,memory=link(file)))
    return dict(editor_ranges=rows,DX_import_blocks=blocks,
        normal_import_not_in_Ghidra='NormalPEcompared directly;DXGhidra memoryprimaryisexplicit. No normalGhidra import ornewnormalGhidraCPUcontrol.')

def build():
    survey=variants.build();assert load('phase124-game-executable-variants.json')==survey
    replays=load('phase124-normal-executable-controls.json');assert replays['scope']==native.__doc__ and len(replays['replays'])==3
    for name,row in zip(native.cases(),replays['replays']):
        c=native.build(name);p=native.path_for(name,c['args']);saved=json.loads(p.read_text(encoding='utf8'))
        assert saved['case']==name and saved['input']==c['args'] and row['sha256']==variants.sha(p.read_bytes())
        assert row['outcome']==native.verify(name,c,saved['result'])
        assert row['source_executable_sha256']==variants.PROFILES['normal'][1]
    old=ROOT/'phase124-prior-components.json'
    if not old.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==102
        old.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(old.name))==102
    executables={r['file']:r['sha256'] for r in source_executables()+load('editor-variant-semantics.json')['profiles']+survey['profiles']}
    for file,h in executables.items():assert variants.sha(Path(file).read_bytes())==h
    for profile in survey['profiles']:
        resolved=api.static_definition_profile(profile['sha256'])
        assert resolved['source_sha256']==profile['sha256'] and resolved['projection_profile_sha256']==api.DX
    return dict(phase=124,date='2026-10-29',family='Exact provided normal/DX game static format-code compatibility',
        profiles=survey['profiles'],exhaustive_byte_comparison=survey,
        primary=primary(),native_normal_EXE_controls=replays['replays'],new_Ghidra_CPU_controls=0,
        static_profile_resolution=[api.static_definition_profile(r['sha256']) for r in survey['profiles']],
        original_executable_paths_preserved=[dict(file=p,sha256=h) for p,h in executables.items()],
        Keeper_C8_review=dict(raw=0xC8,intermediate=0x140,runtime=0x3A,editor_model=0x178,
            meaning='unresolved',lead_inventory=link('phase124-keeper-model178-leads.json'),literal_leads=43,
            owner_queries='All43literaloperandleads have saved get_function_by_address outputs;that doesnot establish receiveridentity.',
            selected_panel_functions=[0x4439A0,0x443CF0,0x44C610,0x44C940,0x44EB00,0x44EEA0],
            selected_panel_arrays=[0x54FAEC,0x54FB4C,0x54FB38],actual_Keeper_array_count=0x54FAB4,
            observed='Selectedreaders/commit paths operateothercatalogue selections andcannotnameKeeperC8. Twofresh callbacks4A2260/4A2A70 appendtootherarrays. Remainingliteralhits notdeclared globallyclosed.',
            boundary='Model178 iscommon numeric offsetacrossclasses;no newKeeper semanticconsumer ornewunits established.',
            inherited_storage=link('keeper-pipeline-semantics.json'),
            inherited_actual_Keeper_callback_evidence=link('ghidra/phase26-keeper-editor-model.txt')),
        helpers=[link(n) for n in ['scan_game_executable_variants.py','emulate_normal_executable_controls.py','native_game_executable_profiles.py']],
        preservation=dict(new_container_rebuilds=0,reason='No binaryformat rule changed;newexactEXEprofile and3isolatednormalPEcontrols. OldDX tests notcounted as new.',source_writes=0),
        limitations=survey['limitations']+['Three newnormalPE CPUcontrols coverselectedrawTrapcycle andtwoCLight paths,notall15families/allWindows runtime.',
            'No independentnormalGhidraCPUreplay;priorDXGhidrachecks remainhistorical.',
            'FiniteKeepereditorlead reviewdoesnotprove globalnonuse;alias/indexed accesses andotherbinaries remainpossible.',
            'Substantial unknownfields across15families stillopen;goalnotachieved.'])

if __name__=='__main__':
    value=build();(ROOT/'game-executable-compatibility.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Normal/DXsamecodeprofile verified;3newnormalCPUcontrols;KeeperC8stillunresolved.')
