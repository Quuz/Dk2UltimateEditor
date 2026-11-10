"""Publish the scoped, native-verified adaptive CUBE_GEN batch count rule."""
from pathlib import Path
import json
import emulate_effect_generation_scale as native
import crosscheck_effect_generation_scale as controls
import prepare_effect_generation_scale_primary as primary
from validate_effect_generation_scale import validate

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    layout=json.loads(json.dumps(load('layouts.json')['Effects']))
    for f in layout['fields']:
        if f['offset']==0xB4:
            f['verified_mode4_scale_bit']=dict(mask=0x4000,runtime_offset=0x5F,
                meaning='Scale generation batches for EffectElements target, using runtime coefficient69752C; not a direct percent or file-stored coefficient.',
                legacy_enum_name='USE_GENERATION_SCALE',scope='Whole native CUBE_GEN element branch; Effects target bypass established by static branch and zero-count native control.')
        if f['offset']==0xF3:
            f['verified_mode4_count']=dict(runtime_offset=0x8A,raw_unsigned_bytes=1,
                formula='If flags40 is clear and flags4000 set: max(N!=0,floor(N*scale/255)); otherwise N.',
                unit='Batches per eligible handler call, before physical-slot iteration; not successful creations or per-second frequency.',
                conditions='Original randomness/lifetime gates still apply. Nonrandom selection emits each nonzero physical slot per batch; random first-slot-zero can abort.')
    return layout

def build():
    attested=primary.build();assert attested==load('phase141-effect-scale-primary.json')
    manifest=load('phase141-effect-scale-emulations.json');rows=manifest['replays'];assert len(rows)==18
    posts={}
    for c,row in zip(native.cases(),rows):
        args,checks,saved,path=native.execute(c);post,out=native.verify(c,args,checks,saved['result']);posts[c['name']]=post
        assert row['case']==c and row['summary']==out and row['file']==str(path.relative_to(ROOT)) and row['sha256']==sha(path.read_bytes())
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
    assert manifest['rate_fragment_controls']==native.rate_controls()
    expected={a:bytearray(b) for a,b in posts['scaled6-127'].items()}
    expected[native.p.ERAW][0xEE:0xF2]=bytes.fromhex('3412dcfe');expected[native.p.EMID][0x1A2:0x1A6]=bytes.fromhex('3412dcfe');expected[native.p.base.PARENT_DEF][0x4B:0x4F]=bytes.fromhex('3412dcfe')
    assert {a:bytes(b) for a,b in expected.items()}==posts['misc-pair']
    cross=load('phase141-effect-scale-crosschecks.json')['replays'];assert len(cross)==5
    for row in cross:
        args,n,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert row['checked_regions']==n and row['checked_bytes']==size
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase141-effect-scale-api-validation.json')
    preservation=load('element-palette-chain-semantics.json')['preservation']
    old={r['file'].lower():r for r in preservation['source_files']}
    for r in edits['files']:old[r['file'].lower()]={k:r[k] for k in ['file','sha256']}
    preservation=dict(source_files=list(old.values()),executable_paths=preservation['executable_paths'])
    for r in preservation['source_files']+preservation['executable_paths']:assert sha(Path(r['file']).read_bytes())==r['sha256']
    prior=ROOT/'phase141-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==119
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==119
    return dict(phase=141,date='2026-10-30',families=['Effects','EffectElements'],profile=attested['profile'],primary=attested,
        field=dict(raw_flags_offset=0xB4,raw_flags_width=4,flag_mask=0x4000,raw_count_offset=0xF3,raw_count_width=1,
            intermediate_flags_offset=0x168,intermediate_count_offset=0x1A7,runtime_flags_offset=0x5F,runtime_count_offset=0x8A,
            legacy_flag_name='USE_GENERATION_SCALE',native_behavior_confirmed=True,native_label_confirmed=False),
        raw_chain=dict(raw_effect_bytes=246,intermediate_effect_bytes=426,runtime_effect_definition_bytes=148,
            definition_constructor=0x4357E0,initializer=0x56AEC0,converter=0x56AFE0,loader=0x4358C0,
            original_element_loader_chain=[0x436630,0x56B460,0x56B570,0x4366A0],generator=0x4386F0,child_creator=0x4352E0,child_initializer=0x435F20,
            parent='Prepared effect instance, not native parent factory. Empty initial child list; native children created/linked/registered by actual code.',
            effect_graphics_provider='null; whole loader executed, actual Effects asset resolution not covered',
            intermediate_extent='Original56AEC0 writes through1A9 inclusive; whole426-byte buffer plus64-byte guard observed. Historical448-byte converter fixture allocation was not a native size claim.'),
        count_rule=dict(generation_type=4,coefficient_address=0x69752C,coefficient_bytes=1,
            predicate='After native randomness gate succeeds and lifetime>0; target selected from instance definition flags.',
            element_branch='If flags4000: max(N!=0,floor(N*scale/255)); else N.',
            effect_branch='Flags40 target branch bypasses scale; full nonzero Effects creation not executed here.',
            nonrandom='Each batch visits eight physical slots and skips zero; duplicate IDs are independent creation attempts.',
            random='One physical-slot selection per batch. First physical slot0 can abort before the loop; minimum-one batch does not guarantee any live child.',
            unit='Batches per eligible generator callback, not per-second frequency, successful allocation count, or lifetime.',
            examples=[dict(N=6,scale=k,batches=v) for k,v in [(0,1),(50,1),(127,2),(128,3),(254,5),(255,6)]],
            zero_count_remains_zero=True,scale_zero_is_diagnostic='Runtime scale setter clamps ordinary values to50..255; direct0 input tests minimum-one arithmetic.'),
        coefficient_source=dict(input_rate_dword=0x75CA70,frame_timer_function=0x575700,frame_timer_caller=0x59D760,
            timer_import='timeGetTime',timer_IAT=0x66C360,
            rate_formula='s32(1000 / s32(now_ms-previous_ms)); zero elapsed replaced by1; native signed integer division.',
            setter_function=0x43B220,executed_fragment=[0x43B28B,0x43B2C9],
            scale_formula='clamp(trunc(s32(205*s32(rate))/14)+50,50,255)',
            ordinary_nonoverflow_examples=[dict(rate=k,scale=v) for k,v in [(0,50),(1,64),(7,152),(13,240),(14,255),(30,255)]],
            interpretation='Adaptive generation density from a timer-derived frame-rate estimate. Exact timing origin is successive575700 calls; no manual UI quality setting inferred.',
            scope='Nine original arithmetic-fragment controls. Whole43B220 World tick and575700/59D760 frame functions are static lineage only, not executed end to end.'),
        remaining_unknowns=dict(Misc2_Misc3='rawEE/F0 copied into definition4B/4D. One whole-memory pair differs only in raw/MID/definition copies; purpose remainsunknown, no globalunused claim.',
            opaque_F4='Preserved; earlier native writer/converter evidence remains unchanged.'),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,effective_effect_layout=effective_layout(),
        counts=dict(native_raw_chains=18,native_rate_fragments=9,successful_children=sum(r['summary']['successful_child_creations'] for r in rows),
            paired_misc_controls=1,paired_misc_compared_bytes=manifest['misc_pair_compared_bytes'],independent_Ghidra_CPU_controls=5,
            Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],
            original_vtable_blocks=3,new_exact_container_edits=82,preserved_source_files=len(old),preserved_executable_paths=len(preservation['executable_paths']),
            newly_verified_mode_specific_flag_rules=1,newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['native_effect_generation_scale.py','probe_effect_generation_scale.py','emulate_effect_generation_scale.py','crosscheck_effect_generation_scale.py','prepare_effect_generation_scale_primary.py','validate_effect_generation_scale.py']],
        limitations=['Exact supplied DX only; no new oldEXE/RC3/Flame/liveWindows comparison.',
            'Prepared parent/world connection/creation-only arena and uniform Q16 coefficients. Child lookup has explicit external resource handle and connected-engine providers; no GPU/audio or actual asset decode.',
            'Full raw loaders and CUBE_GEN executed; parent factory, enclosing tick/scheduler, later child physics/deletion and nonzero Effects-target creation not executed.',
            'Coefficient arithmetic fragment executed separately; original timer source and entire World setter have static evidence only. No runtime FPS measurement or manual settings inference.',
            'Zero coefficient and source record edits are diagnostic memory inputs, not a claim all variants occur naturally.',
            'Misc2/Misc3/F4 and other unknowns remain unresolved; selected no-read/no-effect observations do not establish global nonuse.',
            'One confirmed mode-specific flag behavior does not complete the15-family goal; readiness estimate remains about75%.'])

if __name__=='__main__':
    v=build();(ROOT/'effect-generation-scale-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(v['counts']))
