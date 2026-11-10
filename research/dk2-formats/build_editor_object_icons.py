"""Publish confirmed Objects+C8 editor-map icon; retain historical slot aliases."""
from pathlib import Path
import json
import emulate_editor_object_icons as native
import crosscheck_editor_object_icons as cross
import prepare_editor_icon_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=native.sha((ROOT/n).read_bytes()))

def build():
    manifest=load('phase121-editor-icon-emulations.json');assert manifest['scope']==native.__doc__
    assert len(manifest['replays'])==18
    for c,row in zip(native.cases(),manifest['replays']):
        saved=load(row['file']);assert saved['input']==c and row['sha256']==native.sha((ROOT/row['file']).read_bytes())
        r=saved['result'];assert r['success'] and r['regions_match'] and r['actual']==c['expected']
        assert r.get('expected_EAX_matches',True) and not r['protected_failures']
        assert r['substitutions']==0 and r['external_API_adapters']==0
    controls=load('phase121-editor-icon-crosschecks.json');assert len(controls['replays'])==2
    for (kind,value),row in zip(cross.CONTROLS,controls['replays']):
        args,count,size=cross.build(kind,value);saved=load(row['file'])
        assert saved['input']==args and row['checked_regions']==count and row['checked_bytes']==size
        assert row['sha256']==native.sha((ROOT/row['file']).read_bytes())
        r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    attest=primary.build();assert load('phase121-primary-manifest.json')==attest
    api=load('phase121-icon-api-validation.json');assert len(api['files'])==82 and api['exact_container_rebuilds']==328
    for row in api['files']:assert native.sha(Path(row['file']).read_bytes())==row['sha256']
    sources={r['file']:r['sha256'] for r in source_executables()+load('editor-variant-semantics.json')['profiles']}
    for p,h in sources.items():assert native.sha(Path(p).read_bytes())==h
    previous=ROOT/'phase121-prior-components.json'
    if not previous.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==99
        previous.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==99
    historical=load('object-resource-bindings.json')['slots'][2];assert historical['raw_offset']==0xC8
    effective=dict(historical,display_name='EditorMapIconResource',meaning_status='confirmed native editor-map icon index/resource',
        native_editor=dict(label='Object Icon',group_control_id=1310,preview_control_id=1308,
            spin_control_id=1309,stock_dialog_resource=327,pro_dialog_resource=256,
            native_model_getter=0x41CB30,native_model_setter=0x41C8E0,
            template_visible=True,GUI_event_execution=False),
        game_scope='No resource load in inspected game Object loader49DD60; no claim of globally unused bytes.',
        resolution_component='editor_object_icon_semantics')
    return dict(phase=121,date='2026-10-29',family='Objects editor-map icon resource',
        source_executables_unchanged=[dict(file=p,sha256=h) for p,h in sources.items()],
        physical=dict(raw=0xC8,resource_bytes=84,name_bytes=64,intermediate=0x148,presence=0x14,
            model_resource=0x6C,model_raw_resource_bytes=0x70,model_parsed_icon_u16=0xC4,
            legacy_alias='InHandIconResource',recommended_alias='EditorMapIconResource',
            native_in_hand_icon_is_other_slot=0x170),
        original_paths=dict(converter=0x42D570,model_callback=0x4A1D80,resource_copy=0x41BB90,
            suffix_parser=0x41C0B0,getter=0x41CB30,setter=0x41C8E0,
            atlas_rect=0x49E960,map_objects_draw=0x497AC0,bitmap_draw=0x497E20,
            bitmap_load=0x484720,object_dialog_load=0x45A400,spin_handler=0x45AC40,writer=0x4A9390),
        semantics=dict(suffix='Resource name contains#N;native parser finds first# and parses following decimal substring using original atoi4ACE5A. Stores low16 at resource+58/modelC4. No# stores0.',
            getter='Object vtable50AFFC slot24 returns WORDmodelC4;slot28 setter updates #suffix and cachedWORD.',
            lookup='Global54F550 +528/52C is Objects pointer array/count, agreeing54FA78/54FA7C.49E960 searches ObjectId byte168 then calls original slot24.',
            rect='x=(u16index%columns)*64,y=(u16index//columns)*64;rect=[x,y,x+64,y+64]. No row-bound clamp in this helper.',
            ABI='49E960 consumes3stack arguments:outRect,ObjectId,3 at verified callers;thirdargument unused by this Object helper. RET0C;decompiler omits unused argument.',
            render='497AC0 iterates placed Objects;49E960 result reaches497E20→StretchDIBits. Static native consumer established,not live rendering execution.',
            save='4A9390 copies21DWORD from model6C+4 into rawC8 (auStack2D0 relative raw uStack398);writes894-byte record. CachedWORDC4 is outside84serialized bytes.',
            prefix_scope='Arbitrary resource-prefix-to-bitmap substitution is not established;this selected map route consumes parsed index in per-family atlas.',
            unrelated_slots='Game GUI iconraw74 and native InHandIconraw170 are separate84-byte resources;do not synchronize or redirectC8 to either.'),
        effective_resource_slot=effective,primary=link('phase121-primary-manifest.json'),
        native_executions=manifest['replays'],independent_Ghidra_controls=controls['replays'],
        source_bitmap=attest['bitmap'],API_validation=link('phase121-icon-api-validation.json'),
        source_corpus=api,
        helpers=[link(n) for n in ['native_editor_object_icons.py','emulate_editor_object_icons.py',
            'crosscheck_editor_object_icons.py','prepare_editor_icon_primary.py','validate_editor_object_icons.py']],
        preservation='Suffix patch changes only bytes needed for new NUL-terminated name;keeps bytes after newNUL,resource attributes,other8resources,all records/headers/suffixes. Noncanonical names remain opaque in view;unterminated/overlong writes rejected.',
        limitations=['No full raw→MFC model construction chain in CPU:converter,atoi,atlas are separate bounded executions with explicit prepared model index.',
            'MFC allocation,CString helper internals,GUI spin/commit,full renderer,Windows GDI and whole original save operation not executed.',
            'Native render/save meanings rely on original code,data,vtables and resource bindings; stock14functionranges identical toPro butnot separate stockCPU executions.',
            'Bundledbitmap256x256 gives16icons;otherfiles may have different dimensions. CPUindex16/65535 projects outsidecurrentbitmap,not proof of live crash.',
            'Do not claim C8 globally unused bygame or meanings of every ArtResource union byte.',
            'Other substantial unknowns across all15 families remain;goal not achieved.'])

if __name__=='__main__':
    value=build();(ROOT/'editor-object-icon-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects+C8 editor map icon confirmed;18separateCPU cases,2Ghidra controls,328exact rebuilds.')
