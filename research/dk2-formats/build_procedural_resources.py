"""Publish primary procedural resource mapping, actual consumers and lossless edits."""
from pathlib import Path
import hashlib,json
import emulate_procedural_resources as native
import crosscheck_procedural_resources as independent
import prepare_procedural_primary as primary
from emulate_resistance import post_context

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def build():
    manifest=primary.build();assert manifest==load('phase129-primary-manifest.json')
    runs=load('phase129-procedural-native-emulations.json')['replays'];assert len(runs)==len(native.cases())==20
    posts={};outcomes={}
    for case,row in zip(native.cases(),runs):
        args,checks,source=native.build(case);path=native.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert saved['case']==case and saved['input']==args and sha(path.read_bytes())==row['sha256']
        out=native.verify(case,args,checks,source,saved['result']);assert out==row['outcome']
        posts[case['name']]=post_context(checks,saved['result']);outcomes[case['name']]=out
    pairs=[]
    for name,base in [('ring-opaque-ffffffff','Effects-27-render'),('ring-opaque-highbit','Effects-27-render'),
        ('beam-opaque-ffffffff','Shots-20-render'),('beam-opaque-highbit','Shots-20-render')]:
        want={a:bytearray(b) for a,b in posts[base].items()};got=posts[name]
        want[native.RAW]=got[native.RAW]
        mid_offset=0xA8 if name.startswith('beam') else 0xD4
        want[native.MID][mid_offset:mid_offset+84]=got[native.RAW][0x20:0x74]
        if name.startswith('beam'):
            native.put(want[native.DEFINITION],0x33,sum(want[native.MID]))
        assert want==got,name
        pairs.append(dict(case=name,baseline=base,complete_prepared_outputs_equal_except=['raw union DWORDs','copied intermediate union DWORDs']+
            (['native Shot checksum'] if name.startswith('beam') else []),geometry_and_RNG_equal=True))
    controls=load('phase129-procedural-crosschecks.json')['replays'];assert len(controls)==3
    for row in controls:
        args,count,size=independent.build(row['case']);saved=load(row['file']);result=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    corpus=load('phase129-procedural-api-validation.json')
    for row in corpus['files']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    sources=load('game-executable-compatibility.json')['original_executable_paths_preserved']
    for row in sources:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase129-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==107
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==107
    return dict(phase=129,date='2026-10-29',family='ArtResource procedural variant used by Shots and Effects',
        profile=dict(executable=str(native.EXE),sha256=sha(native.EXE.read_bytes())),primary=manifest,
        ArtResource_variant=dict(resource_bytes=84,type_offset=80,type_value=7,union_offset=68,union_bytes=12,
            fields=[dict(name='procedure_id',offset=68,size=4,kind='u32',runtime_descriptor_offset=0,
                meaning='Selector for native procedural dispatcher5A01A0; full DWORD, not byte.',evidence=['504D80:case7','5A01D4']),
                dict(name='opaque_1',offset=72,size=4,kind='u32',meaning_resolved=False,copy_to_descriptor=False),
                dict(name='opaque_2',offset=76,size=4,kind='u32',meaning_resolved=False,copy_to_descriptor=False)],
            preserve_all_fields=True,unknown_procedure_IDs_allowed=True,
            flags_mapping=dict(raw_offset=64,runtime_offset=11,operation='flags | 1 for initialized type7'),
            handle=dict(runtime_offset=15,value_in_selected_native_request=0,successful=True,
                meaning='Original graphics59A6A0 case7 returns success1 with handle0 and performs no asset load. Do not reject procedural resources solely for handle0 or empty resource name.'),
            raw_opaque_scope='Two DWORDs are copied from file into intermediate but not descriptor. Shot intermediate checksum still reads them; no global nonuse or permission to clear them.'),
        paths=[dict(family='Shots',record_bytes=239,raw_resource_offset=32,raw_selector_offset=100,
            initializer=0x56E540,converter=0x56E690,intermediate_resource_offset=0xA8,intermediate_selector_offset=0xEC,
            constructor=0x4ACD60,loader=0x4ACDF0,definition_bytes=118,resource_descriptor_offset=0),
            dict(family='Effects',record_bytes=246,raw_resource_offset=32,raw_selector_offset=100,
            converter=0x56AFE0,intermediate_resource_offset=0xD4,intermediate_selector_offset=0x118,
            loader=0x4358C0,definition_bytes=148,resource_descriptor_offset=0,
            initial_state='Resource initializer504D60 and explicitly zero remaining definition; complete Effect constructor/parser initializer not executed.')],
        render_fixture=dict(dispatcher=0x5A01A0,render_info_resource_pointer_offset=0x28,source_entity_tag_offset=0x4F,
            generic_graphics_dispatcher59B880_executed=False,source_factory_or_full_tick_executed=False,
            chain='Original raw converter→full loader→original graphics slot54→504D80 descriptor→5A01A0 on explicit RenderInfo.',
            geometry_allocator=0x57CEB0,mesh_constructor=0x588190,mesh_vtable=0x66FE04,queue_append=0x571AE0,
            prepared_engine_heap_bytes=0x10000,prepared_finite_CRT_heap_bytes=native.CRT_SIZE,
            native_RNG=0x6359F0,RNG_seed=1,providers='Sound lookup0; HeapAlloc/HeapFree, critical sections, TLS/GetLastError/SetLastError IAT boundaries. Original malloc/free/RNG/procedures preserved.',
            IAT_slots=[0x66C14C,0x66C148,0x66C22C,0x66C118,0x66C0F4,0x66C0A4,0x66C0F8],
            CRT_small_block_threshold=0,prepared_critical_section='single thread, CRT lock9 initialized',
            cross_backend='Absolute callee stack frames matched, including pointers retained in freed temporary CRT nodes; every prepared byte compared.'),
        selector_review=[dict(value=1,executed='diagnostic raw selector',outcome='One mesh with5vertices/4triangles; no canonical native UI name assigned.'),
            dict(value=2,executed='Original Thunderbolt Shot',outcome='Shared lightning generator, one direct5A0030 call; prepared source tag and caster lookup used.'),
            dict(value=3,executed=False,outcome='Static branch exists; complete consumer/camera context unresolved.'),
            dict(value=4,executed='Original Frost Ball Ring Effect',outcome='Two procedural meshes, each16vertices/16triangles.'),
            dict(value=5,executed='diagnostic raw selector',outcome='No geometry in selected dispatcher.'),
            dict(value=6,executed='diagnostic raw selector',outcome='Two procedural meshes, each16vertices/16triangles; no canonical native UI name assigned.'),
            dict(value=7,executed='Original Lightning Shot',outcome='Three direct calls to shared lightning generator; not a fixed final mesh count.'),
            dict(value=8,executed='Original Lightning Bolt Effect',outcome='Shared lightning generator without source tag requirement. Recursive branches may add generator calls.'),
            dict(value=0,executed='diagnostic raw selector',outcome='No geometry in selected dispatcher.'),
            dict(value=0xFFFFFFFF,executed='diagnostic raw selector and Ghidra control',outcome='Full u32 preserved; no geometry in selected dispatcher.')],
        native_replays=runs,opaque_DWORD_pairs=pairs,independent_Ghidra_controls=controls,corpus=corpus,
        counts=dict(native_compositions=20,original_standalone_type7_records=7,original_procedures_executed=[2,4,7,8],
            diagnostic_procedures_executed=[0,1,5,6,0xFFFFFFFF],paired_whole_context_predictions=4,
            independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),whole_native_function_spans=34,primary_data_blocks=4,
            source_files=len(corpus['files']),exact_container_rebuilds=corpus['exact_container_rebuilds'],source_writes=0),
        source_executables=sources,helpers=[link(n) for n in ['emulate_procedural_resources.py','crosscheck_procedural_resources.py',
            'prepare_procedural_primary.py','native_procedural_resources.py','validate_procedural_resources.py','native_unicorn_factorytrace.py']],
        limitations=['No live Windows renderer/GPU pixels, real game scene, source factory, flight, full tick or 59B880 wrapper executed in this phase.',
            'Geometry counts outside ring4 and controls are fixture/RNG/length dependent; no fixed-mesh-count enum inferred.',
            'No full independent mathematical prediction of every geometry byte; selected headers checked, opaque outputs cross-checked in a second CPU.',
            'Three Ghidra controls cover2/7/default, not FCOS ring branches1/4/6 or large recursive8.',
            'Full Effects constructor and parser initializer not executed; explicit zero remaining definition plus native resource initializer.',
            'Two opaque union DWORDs and selector3 remain unresolved; no stale-pointer label, native UI enum or global nonuse claim.',
            'Prior ElementB1/B2 meaning remains unresolved; actual corpus contains no Element resource type7.',
            'API tests are whole-container lossless rebuilds, not original editor GUI saves; no game code or original source writes.',
            'Earlier discovery snapshots with overlapping fixtures, missing CRT providers or differing stack frames are not accepted controls.',
            'All15family objective remains active; significant unknowns remain.'])


if __name__=='__main__':
    value=build();(ROOT/'procedural-resource-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
