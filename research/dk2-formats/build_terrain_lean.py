"""Publish real mixed-occupancy conditions and complete selected Lean geometry."""
from pathlib import Path
import copy,json,hashlib
import emulate_terrain_lean as native
import emulate_lean_controls as controls
import crosscheck_terrain_lean as independent
import prepare_terrain_lean_primary as primary
from validate_terrain_lean_output import validate as validate_output

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def effective_terrain_layout():
    value=copy.deepcopy(load('procedural-cursor-semantics.json')['effective_terrain_layout'])
    for field in value['fields']:
        o=field['offset']
        if o not in [0x1CF,0x1D0,0x1D1,0x1D3,0x1D4,0x1D5]:continue
        layer=o-(0x1CF if o<0x1D2 else 0x1D3)
        field['phase133_evidence']='terrain-lean-semantics.json'
        field['Lean_grid_layer']=layer
        field['native_consumer']='Raw BYTE→float32(u8/512), then actual59F4B0/59F2F0/59E700/59EC90 applies boundary-dependent Lean. Corner factor float32(1.4), midpoint corner average and centre average verified.'
        field['native_direction']='Horizontal X/Y displacement toward occupied side of selected boundary' if o<0x1D2 else 'Negative render-Z displacement at selected boundary; not a percentage'
        field['native_scope']='Uniform Terrain definition with mixed real World/CMap occupancy.22full raw→wrapper paths verify all27final vertices; identical free/occupied neighbour results can suppress Lean. Raw128 gives boundary displacement0.25 in simple tested plane.'
    return value


def build():
    attested=primary.build();assert attested==load('phase133-Lean-primary.json')
    runs=load('phase133-Lean-emulations.json')['replays'];assert len(runs)==len(native.cases())==22
    for case,row in zip(native.cases(),runs):
        args,checks,source,parent=native.build(case);path=native.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==args and saved['case']==case and sha(path.read_bytes())==row['sha256']
        assert native.verify(case,args,checks,source,parent,saved['result'])==row['outcome']
    helpers=load('phase133-Lean-control-emulations.json')['replays'];assert len(helpers)==6
    for case,row in zip(controls.cases(),helpers):
        args,checks,source=controls.build(case);path=controls.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==args and sha(path.read_bytes())==row['sha256']
        assert controls.verify(case,args,checks,source,saved['result'])==row['outcome']
    cross=load('phase133-Lean-crosschecks.json')['replays'];assert len(cross)==6
    for row in cross:
        args,count,size=independent.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    projection=load('phase133-Lean-output-validation.json');assert projection==validate_output()
    corpus=load('phase133-Lean-api-validation.json');assert corpus['exact_changed_container_rebuilds']==82
    preservation=load('procedural-cursor-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    previous=ROOT/'phase133-prior-components.json'
    if not previous.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==111
        previous.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==111
    return dict(phase=133,date='2026-10-30',families=['Terrain','procedural ArtResource7/selector3'],profile=attested['profile'],primary=attested,
        fields=dict(raw_LeanH=[0x1CF,0x1D0,0x1D1],raw_LeanV=[0x1D3,0x1D4,0x1D5],physical_width='Each u8, full0..255 preserved',
            runtime_LeanH=[0x1A2,0x1A3,0x1A4],runtime_LeanV=[0x1A6,0x1A7,0x1A8],scale='float32(raw/512)',
            grid_layer_mapping='Index0/1/2→first/middle/last9point layer of27point render grid. Default pre-deformation renderZ about-1.55/-1.30/-1.05; avoid unqualified top/bottom labels.',
            layer_test=dict(raw_values=[32,128,255],simple_edge_displacements=[0.0625,0.25,0.498046875])),
        actual_World_occupancy=dict(producer=0x5A1070,per_point_helper=0x59ECF0,output_DWORDs=16,
            coordinates='Prepared integer centre(2,3); row-major4x4 covers x1..4/y2..5.',
            original_methods=dict(bounds=0x509090,cell=0x508FC0,nav_bit8=0x5090C0,World220=0x5092D0,
                CMap_vector_wrapper=0x4528C0,CMap_cell_test=0x4528E0,nav_high_nibble=0x4D1A20),
            rule='At valid cell with runtimeWORD8 bit2000h: occupancy=bool(navByte&08h) OR bool(navByte&F0h), equivalently bool(navByte&F8h). Else/bounds-failure59ECF0 returns1. All external-object output arguments passed0.',
            reason='World20C testsnav08h; on0 World220→CMap queries highnibble. This caller returns Boolean without enumerating real objects.',
            tested_nav_values=[0,1,8,16],tested_scope='Nav00/01 produce0;08/10 produce1 for supplied flagged cells. These are runtime navigation bytes, not rawTerrain flags or rawMap byte2.',
            source='Explicit mixed32x32map/navigation fixture; no rawKLD→World generation newly executed.'),
        geometry=dict(consumer=0x59F4B0,corner=0x59F2F0,Y_edge=0x59E700,X_edge=0x59EC90,
            primitives='For each of3layers:4corners,4edge midpoints,onecentre. Slopes depend on local Boolean occupancy OR combinations, not just value nonzero.',
            uniform='All-free context yields identical geometry for Lean0/H128/V128. Historical all-occupied fixture also matched; neither is global Lean nonuse.',
            straight_example='East half occupied, H128: X offsets0/0.125/0.25 across each row; V128: negative renderZ0.25 onlyat the occupied-side edge. North half gives correspondingY response.',
            corners='When both axis conditions differ,59F2F0 first multiplies H by float32(1.4)=1.399999976158142. Isolated-cellH128 gives float32±0.35 components before final vertex addition; not a sqrt2 normalization.',
            interpolation='Native constant670704 is-0.5: FSUBR adds corner averages to perpendicular midpoint components. Centre XY is float32 quarter-sum of four edge vectors. V is added once per affected corner/edge.',
            reference=dict(file='native_terrain_lean.py',operation='project',input_bytes=[324,64,540],output_bytes=324,
                scope='Boolean occupancy and selected finite float32 values. Derived540byte samples, not directraw-record decoder or entirerenderer. No unknown-field normalization.'),
            full_output='All324bytes of27final vertices predicted in22original fullwrapper paths from rawLean bytes, actual occupancy and explicit pre-frame buffers. Wibble forced0 to isolate Lean.'),
        derived_samples=dict(producer=0x59E480,range_min4=0x59E3A0,Lean_min4=0x59E400,
            static_aggregation='59E3A0 takes signedDWORD minimum of4source Wibble ranges;59E400 takes finitefloat minimum of4Lean slope samples. Current native cases have uniform Terrain8: heterogeneous definitions not newly executed.',
            base_wave='Onlyfirst9samples receive float32(-0.15-0.125*sin((10+x+y)*0.5)) at centre(2,3). Python sin matches native FSIN for selected5angles5..7; no all-argument x87 equivalence.',
            preservation='LastDWORD in18remaining derived samples retains72bytes of actual pre-call stack buffer; observed before/after59E480. Not zeros/defaults or decoded file data. Independent model carries them unchanged.',
            known_bytes_per_case=468,preserved_prestate_bytes_per_case=72,
            unknown_scope='No new raw Terrain field meaning assigned to stack remnants; no serialization or crash defect inferred.'),
        native_executions=runs,native_control_compositions=helpers,independent_Ghidra_controls=cross,
        complete_output_projection=projection,corpus=corpus,effective_terrain_layout=effective_terrain_layout(),
        counts=dict(full_raw_to_cursor_compositions=22,isolated_Lean_native_controls=4,raw_to_World_occupancy_native_controls=2,
            full_path_World220_calls=sum(r['outcome']['original_World220_calls'] for r in runs),
            independently_predicted_complete_output_bytes=33264,derived_input_bytes_preserved_from_measured_prestate=1584,
            independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),new_function_spans=attested['new_function_spans'],
            inherited_function_spans=attested['inherited_function_spans'],inherited_data_blocks=5,
            unique_executed_original_PCs=attested['unique_executed_original_PCs'],new_exact_container_rebuilds=82,
            original_Terrain_files=82,preserved_source_files=85,original_executable_paths=7,source_writes=0),
        preservation=preservation,
        helpers=[link(n) for n in ['native_terrain_lean.py','emulate_terrain_lean.py','native_terrain_shape_trace.py',
            'emulate_lean_controls.py','crosscheck_terrain_lean.py','prepare_terrain_lean_primary.py',
            'validate_terrain_lean_output.py','validate_terrain_lean_edit.py','native_terrain_deformation.py']],
        limitations=['All fullpaths use one original ClaimedPath8 as the raw base;8deformation bytes explicitly diagnostic, Wibble0 and integer cursor position(2,3). No original files rewritten.',
            'Prepared map/nav topology and Bridge/Session/World; original virtual methods execute. Raw KLD world loading, actualplaced objects, arbitraryworldedges and whole engine bootstrap not covered.',
            'Full FSIN renderer still unsupported byGhidra as documented132. Sixpassing controls cover4actualLeanhelper inputs from full traces and2complete rawload→occupancy paths, not complete deformed frames inGhidra.',
            'Uniform Terrain definition, defaultheightoption; heterogeneous Terrain slope minimum and other callers are static-only here. Not a universal preview model for allTerrain geometry.',
            '72derivedsample prestate bytes perfullcase are preserved input, not mathematically decoded stack data or rawformat fields. No stackclear policy or gamebug inferred.',
            'No GPU, Windows/patchDLL run, original/parallel editor change, resistance repair or spellslot expansion.',
            'Terrain198/224/16WORD1AE, Traps204, Doors236, CreatureJump/Priority, KeeperC8, spell/Element tails and major15family questions remain unresolved.'])


if __name__=='__main__':
    value=build();(ROOT/'terrain-lean-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
