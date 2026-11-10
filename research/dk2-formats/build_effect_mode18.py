"""Publish real mode18 Effect creation and scoped Misc2/Misc3 lead rejection."""
from pathlib import Path
import json
import emulate_effect_mode18 as native
import crosscheck_effect_mode18 as controls
import prepare_effect_mode18_primary as primary
import review_effect_misc_arguments as review
from validate_effect_mode18 import validate

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    layout=json.loads(json.dumps(load('effect-generation-scale-semantics.json')['effective_effect_layout']))
    for f in layout['fields']:
        if f['offset']==0xF2:f['verified_mode18']=dict(callback=0x439100,legacy_name='TEST_3',native_name_confirmed=False,
            meaning='Four physical-slot Effect batches; original CBridge wrappers/factory/NextEffectId chains executed. Othermodes retain prior rules.')
        if f['offset']==0xF3:f['verified_mode18_count']=dict(formula='floor(unsignedBYTE/4)',unit='Batches per live handler call; each visits firstfour slots inreverse. Not totalchild count or per-second rate.')
        if 0xCA<=f['offset']<0xDA:
            index=(f['offset']-0xCA)//2
            f['verified_mode18_slot']=dict(index=index,used=index<4,target='Effects' if index<4 else None,
                condition='Positive lifetime andflags40 required for creation; zero slots skipped. Duplicates preserved.',
                order='3,2,1,0 perbatch' if index<4 else 'Not read bymode18; preserve storage forothermodes.')
    return layout

def build():
    attested=primary.build();assert attested==load('phase143-effect-mode18-primary.json')
    manifest=load('phase143-effect-mode18-emulations.json');rows=manifest['replays'];assert len(rows)==12
    reference=None;pairs=[]
    for c,row in zip(native.cases(),rows):
        args,checks,saved,path=native.execute(c);post,out=native.verify(c,args,checks,saved['result'])
        assert row['case']==json.loads(json.dumps(c)) and row['outcome']==out and row['file']==str(path.relative_to(ROOT)) and row['sha256']==sha(path.read_bytes())
        if c['name']=='source-chains':reference=post
        if c['name'] in ['parent-misc','child-misc']:pairs.append(native.pair(reference,post,c))
    assert pairs==manifest['paired_controls'] and len(pairs)==2
    cross=load('phase143-effect-mode18-crosschecks.json')['replays'];assert len(cross)==5
    for row in cross:
        args,count,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256'] and row['checked_regions']==count and row['checked_bytes']==size
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase143-effect-mode18-api-validation.json')
    classified=review.build();assert classified==load('phase143-effect-misc-argument-review.json')
    preservation=load('door-manufacture-split-semantics.json')['preservation']
    for r in preservation['source_files']+preservation['executable_paths']:assert sha(Path(r['file']).read_bytes())==r['sha256']
    prior=ROOT/'phase143-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==121
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==121
    return dict(phase=143,date='2026-10-31',families=['Effects'],profile=attested['profile'],primary=attested,
        handler=dict(generation_type=18,legacy_name='TEST_3',callback=0x439100,
            raw_fields=dict(flags=0xB4,generate_ids=[0xCA+2*i for i in range(8)],elements_per_turn=0xF3,generation_type=0xF2),
            rule='Positive parentlifetime; batches=floor(unsignedElementsPerTurn/4). Eachbatch reads slots3,2,1,0; zero skipped; creation requiresflags40. Slots4..7 not used.',
            target='Effects via originalCBridge94→43A8E0→Bridge90/440420→Bridge98/440350, not EffectElements.',
            return_scope='Nativebody returns1 independentlyof actualcreation; final harnessEAX0 belongs517490 and isnot thehandlerreturn.'),
        native_factory=dict(constructor=0x434700,initializer=0x4347E0,lookup=0x440BC0,instance_bytes=345,
            definition_bytes=148,allocator=0x554B30,allocation_request=345,charged_allocation_bytes=360,
            complete_original_definition_closure=[1,2,3,4,143,221],source_next_edges={'1':143,'2':221},
            source_creation_order=[4,3,2,221,1,143],source_global_list_order=[143,1,221,2,3,4],
            chained_arguments='NextEffectId is processed immediately within440350; arguments retained, sameposition/owner, firstsuccess output pointer only.',
            partial_allocation='Declared1080byte poolcreates4,3,2. Next221 allocationfails; request2 stillreturns1 andpoints toEffect2. Request1 laterfails; priorinstances stayregistered.',
            registry='Actualclient tag array6D6458, globalEffect listBridge336 via instance123/127, free/usedarena partition andallocationcounter validated.',
            no_null_definition_reads='Acceptedverifier requires all definitions nonnull and everylowpageaccess tobe originalFS:[0]; first incompleteclosure probe excluded.'),
        fixture=dict(parent='Prepared native-Effect-vtable instance withdefinitionpointer andpositive life; parentfactory notexecuted.',
            raw_loaders='Sevenfull Effects constructor/initializer/converter/loader chains percase: onecontrolledparent plus sixoriginalchilddefinitions; childNext onlyaltered inexplicitnext-cleared control.',
            connected_engine='Preparedvisibility1, obstruction0, tilepointerwithflags20, floor0, and5argumentdeclined-light callbackreturn0. No originalnativevtable orcodepatched.',
            graphics='Childdefinition loaders use nullvisualprovider; inherited uniform Q16 coefficients. No assets/GPU/realWorldscheduler.',
            allocator='Prepared creation-only fittingfree/used sentinels; original554B30 runs. No destructor/free or laterchildtick.'),
        unknown_result=dict(fields=['Misc2','Misc3'],raw_offsets=[0xEE,0xF0],runtime_definition_offsets=[0x4B,0x4D],meaning='Unresolved',
            six_static_leads_rejected=classified,parent_and_child_storage_pairs=pairs,
            no_global_unused_claim=True),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,effective_effect_layout=effective_layout(),
        counts=dict(native_raw_chains=12,raw_effect_definition_loads=84,successful_child_Effects=sum(r['outcome']['registered_children'] for r in rows),
            factory_requests=sum(len(r['outcome']['requests']) for r in rows),paired_complete_memory_controls=2,paired_compared_bytes=sum(p['whole_compared_bytes'] for p in pairs),
            independent_Ghidra_CPU_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],original_vtable_blocks=3,
            rejected_static_Misc_leads=6,new_exact_container_edits=82,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['native_effect_mode18.py','probe_effect_mode18.py','emulate_effect_mode18.py','native_unicorn_effecttrace.py','crosscheck_effect_mode18.py','prepare_effect_mode18_primary.py','scan_effect_misc_argument_consumers.py','review_effect_misc_arguments.py','validate_effect_mode18.py']],
        limitations=['Newexecutions exactDX only; no newold/RC3/Flame/liveWindows coverage.',
            'Prepared parent/externalengine providers/nullgraphics/uniformcoefficients. Actualchildfactories andrawloaders run, notwholeWorld/gameplay/framebuffer ornormalactivation scheduler.',
            'Sixvalidchilddefinitions andselectedslot/count cases. Missing-definition initialprobe excluded; no broad malformed-input safety claim.',
            'Partialallocation survivalis proven atthisboundary, notlaterlifecycle robustness orfinalcleanup.',
            'Mode18 behavior isnot ageneralrule forall19 generationselectors. Literal/alias scans have documentedlimits; sixrejections do notprove globalnonuse ofMisc2/Misc3.',
            'No newlynamedopaque fields; full15-family specification remains incomplete, readiness about75%.'])

if __name__=='__main__':
    v=build();(ROOT/'effect-mode18-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(v['counts']))
