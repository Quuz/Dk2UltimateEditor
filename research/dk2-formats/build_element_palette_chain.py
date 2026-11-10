"""Publish bounded unknown EffectElement bytes across full raw/tick/render chain."""
from pathlib import Path
import json
import emulate_element_palette_chain as native
import crosscheck_element_palette_chain as controls
import prepare_element_palette_primary as primary
from validate_element_palette_edits import validate as validate_edits

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    layout=json.loads(json.dumps(load('layouts.json')['EffectElements']))
    for f in layout['fields']:
        if f['offset'] not in [0xB1,0xB2]:continue
        f['native_name_confirmed']=False
        f['meaning_status']='Unresolved; historical field name comes fromOpenKeeper interpretation, not a verified nativepalette consumer.'
        f['verified_storage']=dict(raw_bytes=1,intermediate_offset=0x11D+f['offset']-0xB1,runtime_offset=0x4D+f['offset']-0xB1,
            raw_initializer=0x56B460,converter=0x56B570,loader=0x4366A0)
        f['selected_native_scope']='Phase140: complete raw loader/spawn/update/render2/3 paths; zero reads ofruntime4D/4E and wholememory pairs except raw/MID/definitionstorage. Not globalnonuse.'
        f['recommended_UI_label']=f'Unknown0x{f["offset"]:X} (legacy {f["name"]})'
        f['serialization']='Preserve complete unsignedbyte and everyunrelated byte; do not zero or clamp based on palette assumptions.'
    return layout

def build():
    attested=primary.build();assert attested==load('phase140-element-palette-chain-primary.json')
    manifest=load('phase140-element-palette-chain-emulations.json');runs=manifest['replays'];assert len(runs)==23
    refs={};pairs=[]
    for c,row in zip(native.cases(),runs):
        args,checks,saved,path=native.execute(c);post,out=native.verify(c,args,checks,saved['result'])
        assert out==row['outcome'] and str(path.relative_to(ROOT))==row['file'] and sha(path.read_bytes())==row['sha256']
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
        if c['spawn'] and c['rgb'] is None:
            key=(c['ident'],c['ticks'])
            if c['palette']==[0,0]:refs[key]=post
            else:pairs.append(native.pair(refs[key],post,c))
        assert not out['unknown_definition_reads']
    assert len(pairs)==15 and pairs==manifest['paired_controls']
    cross=load('phase140-element-palette-chain-crosschecks.json')['replays'];assert len(cross)==len(controls.CONTROLS)==6
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256'] and row['checked_regions']==count and row['checked_bytes']==size
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate_edits();assert edits==load('phase140-element-palette-api-validation.json')
    previous=load('native-map-reader-semantics.json')['preservation'];sources={r['file'].lower():r for r in previous['source_files']}
    for row in edits['files']:sources[row['file'].lower()]={k:row[k] for k in ['file','sha256']}
    source=native.probe.ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/EffectElements.kwd'
    sources[str(source).lower()]=dict(file=str(source),sha256=sha(source.read_bytes()))
    preservation=dict(source_files=list(sources.values()),executable_paths=previous['executable_paths'])
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase140-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==118
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==118
    ex={r['case']:r['outcome'] for r in runs}
    return dict(phase=140,date='2026-10-30',families=['EffectElements'],profile=attested['profile'],primary=attested,
        coverage_extension=dict(new_scope='Complete raw182byte source record -> native initializer/converter/79definitionloader -> actualspawn -> complete436430update/4476E0physics -> source599C60sprite/quadrenderqueue.',
            supersedes='Historical phase81 used prepareddefinition; phase102/103 used preparedpostspawninstance. Newselectedchain composes realrawload andlife/renderpaths inoneCPU.',
            remaining='Definition79opaque padding, rawB1/B2meaning, actualresource decoding/GPU and Worldscheduler/destruction remainopen.'),
        raw_chain=dict(raw_bytes=182,intermediate_bytes=290,definition_bytes=79,instance_bytes=287,
            initializer=0x56B460,converter=0x56B570,definition_constructor=0x436630,loader=0x4366A0,
            raw_unknown_offsets=[0xB1,0xB2],intermediate_offsets=[0x11D,0x11E],runtime_definition_offsets=[0x4D,0x4E],
            selected_source_elements=[dict(id=294,name='Blood Pool Element',resource_type=2,resource_flags=0x40000|0x101),dict(id=1,name='Cloud',resource_type=3,resource_flags=5)]),
        unknown_result=dict(status='Unresolved',historical_names=['RandomColorIndex','TableColorIndex'],native_labels_confirmed=False,
            exact_test_pairs=[[0,0],[255,0],[0,255],[255,255]],selected_paths_with_no_runtime_reads=23,
            wholememory_pairs=15,differences='Only the two copied bytes inraw/MID/definition differ; completeinstance/RNG/physics/renderheap/queue andallotherpreparedbuffers match.',
            not_a_global_unused_claim=True,no_new_palette_index_semantics=True),
        positive_controls=dict(sourceRGB='Native loader copies rawAE..B0 todefinition4A..4C; originalinitializer/renderframeuses them.',
            native_update=0x436430,physics=0x4476E0,
            late_original=ex['late-0-0'],late_RGB_edit=ex['RGB-control'],cloud_late=ex['cloud-late-0-0'],zero_life=ex['zero-0-0'],
            zero_life_scope='Update withelapsed128 produceslifetime0 andskipsfadebranch; manualrendercall stillmade onheldinstance. Doesnotprove normalscheduler rendersdeadelements orfinaldeletion.'),
        rendering=dict(source_wrapper=0x599C60,dispatcher=0x59B880,quad_vtable=0x66FCF4,sprite_vtable=0x66FCCC,
            actual_raw_descriptor_conversion=0x504D80,declared_external_handle=0x1234,
            provider='External6argument graphicslookup accepts request andwritesdeclaredhandle; native504D80 derives descriptor fromrawArtResourceunion. No assetloading or GPU.',
            coefficient_scope='Inherited explicit uniform Q16 coefficient table at6C1488isolates initialization arithmetic; not claimnative realangle-table values.',
            connected_engine_scope='Inherited declared visibility1,obstruction0,tile-touch0,floor0queries. CBridge/Element/physics bodies andvtablebytes original.',
            no_final_World_scheduler=True),
        earlier_wander_review=dict(scope='Re-read phase75before selectingfield. Actual47BBE0table references480750and4808E0; no newevidence connects placedBYTE418tohelperDWORD_AF.',
            evidence=[link('ghidra/phase140-wander-helper-xrefs.txt'),link('ghidra/phase140-wander-alt-xrefs.txt')],new_units_assigned=False),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,effective_element_layout=effective_layout(),
        counts=dict(native_chains=23,raw_definition_loads=23,loader_only_controls=2,full_spawn_paths=21,full_update_paths=17,full_render_paths=21,
            individual_native_physics_calls=sum(r['outcome']['full_physics_calls'] for r in runs),paired_complete_memory_controls=15,
            paired_compared_bytes=sum(r['whole_bytes'] for r in pairs),independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],original_vtable_blocks=len(attested['blocks']),
            new_exact_container_edits=82,preserved_source_files=len(sources),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['probe_raw_element_palette.py','emulate_element_palette_chain.py','crosscheck_element_palette_chain.py','prepare_element_palette_primary.py','validate_element_palette_edits.py']],
        limitations=['Exact supplied DX profile only, no new oldEXE/RC3/Flame/liveWindows coverage.',
            'Two originalsourceelements plus explicitRGB/palette diagnostic memoryedits. UnknownB1/B2 remainunknown, no globalnonuse inference.',
            'External resource/connected-engine providers anduniform coefficient table prepared. Original nativecode/vtables retained; no actualassetresolution/GPU/audio/framebuffer.',
            'Object/queue allocation use fitting creation-only free-list/heap fixtures. No finalWorldscheduler/destructor/free or actualparentEffect creation.',
            'Elapsed tick126/128 controls are explicit directupdatecalls; zero-lifemanualrender is diagnostic, not normal schedulerproof.',
            'WholeCPU/equality coverage doesnotdecodeallopaqueMID/definition/instance/renderbytes. Ghidrawholefunctionranges donotproveallbranches.',
            'Physical layouts and historical names preserved; effectiveview annotates unverifiedpalette names andkeepsfullrawbytevalues.',
            'Substantial unresolvedquestions remaininall15requestedfamilies; overallgoalnotcomplete.'])

if __name__=='__main__':
    v=build();(ROOT/'element-palette-chain-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(v['counts']))
