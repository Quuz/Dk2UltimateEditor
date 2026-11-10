"""Publish procedural cursor3 and real Terrain deformation parameter consumers."""
from pathlib import Path
import copy,hashlib,json,struct
import emulate_cursor_procedure as native
import emulate_terrain_deform_samples as samples
import crosscheck_cursor_procedure as independent
import prepare_cursor_primary as primary

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def effective_terrain_layout():
    value=copy.deepcopy(load('layouts.json')['Terrain'])
    for field in value['fields']:
        o=field['offset']
        if not 0x1CE<=o<=0x1D5:continue
        field['semantics']='Native loader and executed terrain deformation consumer; exact raw byte retained.'
        field['phase132_evidence']='procedural-cursor-semantics.json'
        field['runtime_offset']=0x1A1+(o-0x1CE)
        if o in [0x1CE,0x1D2]:
            field['native_consumer']='59E210 zero-extends BYTE, shifts left7 to integer RNG range. 59ED80 uses scale1/131072.'
            field['native_direction']='Horizontal X/Y centred random offsets' if o==0x1CE else 'Vertical Z non-centred random offset in render coordinates'
            field['native_scope']='Selected map-cell bit2000h; bit8000h suppresses Wibble values. Not a direct final vertex displacement or global all-renderer guarantee.'
        else:
            field['native_consumer']='59E210 zero-extends BYTE and saves float32(BYTE/512), preserving all8bits; one of3vertical grid layers. 59F4B0 consumes float slopes with occupancy-dependent interpolation.'
            field['native_scope']='Exact float sampling verified0/255 and real ClaimedPath values. Uniform occupied-neighbour fixture leaves final geometry unchanged for Lean-only255; no global nonuse or percent label.'
    return value


def validate_runs(module,manifest):
    rows=load(manifest)['replays'];assert len(rows)==len(module.cases())
    for case,row in zip(module.cases(),rows):
        args,checks,source=module.build(case);path=module.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['input']==args and saved['case']==case and sha(path.read_bytes())==row['sha256']
        assert module.verify(case,args,checks,source,saved['result'])==row['outcome']
    return rows


def grids_by_name():
    out={}
    for name in ['deform-Terrain-zero','deform-Lean-high','deform-Wibble-H','deform-Wibble-V','deform-Terrain-source']:
        case=next(c for c in native.cases() if c['name']==name);args,_,_=native.build(case)
        saved=load(str(native.path_for(args).relative_to(ROOT)))
        out[name]=list(struct.iter_unpack('<3f',bytes.fromhex(saved['result']['deform_captures'][0]['hex'])))
    baseline=out['deform-Terrain-zero'];result={}
    for name,points in out.items():
        result[name]=dict(changed_points_per_axis=[sum(a[j]!=b[j] for a,b in zip(points,baseline)) for j in range(3)],
            maximum_absolute_delta_per_axis=[max(abs(a[j]-b[j]) for a,b in zip(points,baseline)) for j in range(3)])
    assert result['deform-Wibble-H']['changed_points_per_axis']==[27,27,0]
    assert result['deform-Wibble-V']['changed_points_per_axis']==[0,0,27]
    assert result['deform-Lean-high']['changed_points_per_axis']==[0,0,0]
    return result


def build():
    attested=primary.build();assert attested==load('phase132-cursor-primary.json')
    runs=validate_runs(native,'phase132-cursor-emulations.json');assert len(runs)==28
    sample_runs=validate_runs(samples,'phase132-Terrain-samples-emulations.json');assert len(sample_runs)==2
    cross=load('phase132-cursor-crosschecks.json');assert len(cross['replays'])==4 and len(cross['diagnostics'])==2
    for row in cross['replays']+cross['diagnostics']:
        args,count,size=independent.build(row['case']);saved=load(row['file']);result=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        if row['case'] in independent.CONTROLS:
            assert not saved['tool_is_error'] and result['success'] and result['hit_return']
            assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
            assert row['checked_regions']==count and row['checked_bytes']==size
        elif row['case']=='full-frame':
            assert result['success'] and result['hit_return'] and result['registers']['EBP']=='0x0' and result['registers']['EBX']=='0xa'
        else:assert not result['success'] and 'Unimplemented CALLOTHER pcodeop (fsin)' in result['emulation_error']
    corpus=load('phase132-deformation-api-validation.json');assert corpus['exact_changed_container_rebuilds']==656
    executable_paths=load('terrain-model-chain-semantics.json')['source_preservation']['executable_paths']
    files={r['file']:r['sha256'] for r in load('procedural-resource-semantics.json')['corpus']['files']+corpus['files']}
    for path,digest in files.items():assert sha(Path(path).read_bytes())==digest
    for r in executable_paths:assert sha(Path(r['file']).read_bytes())==r['sha256']
    previous=ROOT/'phase132-prior-components.json'
    if not previous.exists():
        prior=load('editor-format-package.json')['components'];assert len(prior)==110
        previous.write_text(json.dumps(prior,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==110
    sleigh=Path('D:/Rivers Utils/ghidra_12.0.4_PUBLIC/Ghidra/Processors/x86/data/languages/ia.sinc')
    st=sleigh.read_text();frag=st[st.index(':FRNDINT'):st.index(':FRNDINT')+220]
    assert 'round(ST0)' in frag and 'FPUControlWord' not in frag.split('}')[0]
    return dict(phase=132,date='2026-10-30',families=['Shared ArtResource','Terrain','Shots','Effects'],profile=attested['profile'],primary=attested,
        ArtResource=dict(type=7,procedure_id_u32=3,resource_offset=68,opaque_offsets=[72,76],
            behavior_label='Cursor-textured wireframe with terrain deformation and shared pulse',
            label_scope='Behavioral description from original Cursor asset string and executed code, not a canonical UI enum name or proven normal placement caller.',
            historical_status_superseded='procedural_resource_semantics.selector_review value3 was static-only; actual generic wrapper and native whole branch now execute.',
            source_corpus='Diagnostic selector3. Earlier84174record survey contained only selectors2/7 for Shots and4/8 forEffects; no new shipped selector3 record claimed.'),
        generic_path=dict(wrapper=0x59B880,coordinate_conversion=0x59C2A0,procedure_dispatch=0x5A01A0,
            raw_to_descriptor='Original converters/loaders and graphics slot54 remain actual. Effect resource initializer plus zeroed remaining Effect definition, not full Effect constructor/parser.',
            position='Signed Q12 coordinates plus RenderInfo signed offsets51/53/55, then X/Y divide4096 and Z divide-4096. Selector3 floors X/Y through637600; its own lattice receives literal Z=-2.',
            original_enable_bits='Resource DWORD+B bit1 and RenderInfo BYTE+AF bit1; native wrapper executed with no source entity argument.',
            pointer_provenance='GFX+8→Bridge, Bridge+4→Session, Session+14→World. Not a camera. World original vtable,32x32cells and navigation bitmap prepared explicitly.'),
        wireframe=dict(table_initializer=0x59F760,table_address=0x795720,table_records=24,table_stride=12,
            edge_table=runs[0]['outcome']['edge_table'],lattice_points=27,lattice_dimensions=[3,3,3],
            lattice_function=0x59F3C0,lattice_XY_step=0.5,
            lattice_height='Default tested global-option0:0.25step from-1.55 to-1.05. Option759AE8=1:0.5step from-2.05 to-1.05; other globals759AD0/75AEAF participate but their game labels remain unassigned.',
            edge_filter='(requiredMask & (RenderInfo DWORD5B &255)) == requiredMask; traverse table in reverse.',
            mesh_generator=0x59DD30,meshes_per_selected_segment=2,vertices_per_mesh=8,triangles_per_mesh=4,
            thickness_float32=0.029999999329447746,engine_allocator=0x57CEB0,mesh_constructor=0x588190,queue_append=0x571AE0,
            texture='GFX DWORD8E→global795840; low16 used as texture ID. Native engine initialization5983C0 loads asset Cursor into8E (static caller evidence, not fullengine-init execution).',
            zero_mask='No line geometry. Mask16 alone also selects none; it is required together with side bits for upper/vertical segments.'),
        pulse=dict(shared_phase_float=0x795848,shared_last_ms=0x795844,shared_direction=0x6BE8EC,
            clock='Actual5B5E90→5B5E00 with QueryPerformanceCounter mode disabled; timeGetTime IAT boundary returns explicit test milliseconds.',
            update='After geometry: unsigned elapsed=now-last; direction0 subtracts elapsed*float32(0.004), clamps below0 to0/flips1. Direction1 adds same amount, clamps above1 to1/flips0. Overshoot discarded; not per-resource state.',
            color='Uses phase BEFORE update. RenderInfo5B bit20h clear:(phase,0,0); set:(0,0,phase*0.61328125). Not a raw resource color or editor-defined timer.',
            tested_phase_values=[0,0.01,0.5,0.99,1],fixture_elapsed_ms=[0,100,9000]),
        Terrain_deformation=dict(raw_record_bytes=552,raw_offsets=dict(WibbleH=0x1CE,LeanH=[0x1CF,0x1D0,0x1D1],WibbleV=0x1D2,LeanV=[0x1D3,0x1D4,0x1D5]),
            definition_offsets=dict(WibbleH=0x1A1,LeanH=[0x1A2,0x1A3,0x1A4],WibbleV=0x1A5,LeanV=[0x1A6,0x1A7,0x1A8]),
            raw_chain=[0x4F6570,0x56EB60,0x56ED20,0x4F66E0],source='Claimed Path Terrain8 from original Terrain.kwd, with explicit diagnostic byte variants.',
            enable='GFX+C5 enables deformation (native engine init assigns1). Cell WORD8 bit2000h selects Terrain bytes; bit8000h clears Wibble ranges. Lean float sampling tests2000h without8000h gate.',
            samples=dict(function=0x59E210,count=27,stride=20,H_range_DWORD_offset=0,V_range_DWORD_offset=4,
                LeanH_float_offset=8,LeanV_float_offset=12,opaque_untouched_DWORD_offset=16,
                H_range='u8(WibbleH)<<7',V_range='u8(WibbleV)<<7',Lean_scale='float32(u8/512)',
                global_fallbacks=[0x6F25F9,0x6F25FD],global_fallback_scope='Prepared zero in current fixtures, not newly derived GlobalVariables type IDs.'),
            random_consumer=dict(function=0x59ED80,PRNG=0x5558D0,scale=1/131072,
                H='Each X/Y delta uses (rand(H_range)-trunc(H_range/2))/131072.',
                V='Z delta uses rand(V_range)/131072 without centring. Zero range skips RNG for that channel.',
                scope='Internal displacements before occupancy/interpolation/base-wave composition. Not a universal final mesh displacement formula.'),
            Lean_consumer='59F4B0 consumes real float slope fields using occupancy-dependent interpolation. Decompiler printed integer casts in59E210, but59E324/59E338 are FSTP float; no integer truncation defect.',
            actual_geometry_controls=grids_by_name(),
            limitations='Uniform cell2000h/navigation08 fixture gives occupied neighbours; Lean-only255 samples change but final geometry equals zero-Lean baseline. No global Lean nonuse or all-boundary proof.'),
        effective_terrain_layout=effective_terrain_layout(),native_executions=runs,Terrain_sample_executions=sample_runs,
        independent_Ghidra_controls=cross['replays'],retained_Ghidra_diagnostics=cross['diagnostics'],
        emulator_limits=dict(FPU='Explicit FPUControlWord37F and Status0 match Unicorn input. Initial zero-control diagnostic entered native floating exception path; excluded.',
            fractional_coordinates='Ghidra whole heap differs on(2.25,3.75): original floor uses x87 downward rounding, Ghidra FRNDINT implements unconditional round(ST0). Native lattice/line arguments independently checked; fractional full-frame not a passing second-CPU control.',
            Sleigh_source=dict(file=str(sleigh),sha256=sha(sleigh.read_bytes()),FRNDINT_excerpt=frag.split('}')[0]+'}'),
            full_deformation='Ghidra stops at59E5E3 unsupportedfsin. Full enabled frame executes in Unicorn; separate entire rawTerrain→59E210 compositions pass Ghidra without replacing any instruction.',
            independent_output_scope='Two complete integer-coordinate nondeformed frames and two complete Terrain sampler compositions. All prepared non-stack bytes compared. Full deformed geometry is not cross-backend certified.'),
        corpus=corpus,
        counts=dict(native_cursor_compositions=28,native_Terrain_sampler_compositions=2,selected_line_segments=sum(r['outcome']['selected_edges'] for r in runs),
            native_meshes=sum(r['outcome']['line_meshes'] for r in runs),independent_Ghidra_CPU_controls=4,
            Ghidra_compared_regions=sum(r['checked_regions'] for r in cross['replays']),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross['replays']),
            retained_second_CPU_limit_cases=2,full_function_spans=len(attested['functions']),data_blocks=len(attested['blocks']),
            unique_executed_original_PCs=attested['unique_executed_original_PCs'],new_exact_container_rebuilds=656,Terrain_source_files=82,
            source_Terrain_records=3306,source_files_preserved=len(files),executable_paths_preserved=len(executable_paths),source_writes=0),
        preservation=dict(source_files=[dict(file=p,sha256=d) for p,d in sorted(files.items())],executable_paths=executable_paths),
        helpers=[link(n) for n in ['emulate_cursor_procedure.py','native_cursor_trace.py','emulate_terrain_deform_samples.py',
            'crosscheck_cursor_procedure.py','prepare_cursor_primary.py','native_terrain_deformation.py','validate_cursor_deformation.py']],
        limitations=['Selector3 behavior established through explicit diagnostic resource, not a discovered shipped KWD use or a complete normal placement caller.',
            'Prepared Bridge/Session/World,32x32uniform map/navigation and finite engine heap. No full engine bootstrap, raw KLD world parser, actual GPU or live Windows execution.',
            'Terrain loader receives null graphics provider; catalogue resource allocation/render flags branches are not this proof. Actual procedural graphics slot54 retained for Shot/Effect resource.',
            'Native positions/lattice/lineargs and540-byte sample records have independent projections; opaque deformed geometry has actual native traces but no full mathematical reimplementation.',
            'Only global-option759AE8 variation tested; other two global mode meanings remain unassigned. World edges, mixed occupancy, arbitrary maps/time overflow, allocator failures and parallel rendering not covered.',
            'Two explicit Ghidra limitations retained and not counted as passed; no internal code/vtable replacement or exclusions within compared output regions.',
            'Terrain198/224/16WORD1AE and major other15family unknowns remain unresolved. No new resistance fix, spell-slot expansion, original/parallel editor edit or game patch.'])


if __name__=='__main__':
    value=build();(ROOT/'procedural-cursor-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
