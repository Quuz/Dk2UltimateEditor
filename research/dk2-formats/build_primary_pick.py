"""Publish actual primary model picking, preserving unresolved raw-field meanings."""
from pathlib import Path
import json
import emulate_primary_pick as native
import prepare_primary_pick as primary
import crosscheck_primary_pick as controls
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase154-primary-pick-primary.json')
    manifest=load('phase154-primary-pick-emulations.json');rows=manifest['replays'];assert len(rows)==27
    for c,row in zip(native.cases(),rows):
        args,checks,d,path=native.execute(c);_,out=native.verify(c,args,checks,d['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
    cross=load('phase154-primary-pick-crosschecks.json')['replays'];assert len(cross)==8 and [r['case'] for r in cross]==controls.CONTROLS
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    old=load('phase154-prior-components.json');assert len(old)==132
    for row in old.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('pickup-selection-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    totals={a:sum(load(r['file'])['result']['visited_instructions'].get(hex(a),0) for r in rows) for a in [0x582D30,0x585AD0,0x57E5B0]}
    return dict(phase=154,date='2026-10-31',families=['Creatures','Objects'],profile=attested['profile'],primary=attested,
        native_chain=dict(root=0x572820,registry_iterator=0x588A60,dynamic_callback=0x582D30,animated_callback=0x585AD0,
            dynamic_vtable=0x66FD1C,animated_vtable=0x66FD64,callback_slot=0xC,raw_loading='InheritedactualCreature/Objectinit/converter/fullscalarloaderchain executesbeforeeachnewprimarypick.',
            model_cache_acquire=0x57ED30,model_cache_release=0x57ECE0,model_matrix=0x594DB0,camera_matrix=0x594E10,animated_vertex=0x57E5B0,
            native_code_policy='Original virtual tables andcode unchanged. Newbackendadditionallyprotects66FD1C..66FD83. Only inheritedexternalGFX/soundloaderIO adapters; no geometry/callback substitutions.'),
        registry=dict(address=0x769A78,row_bytes=16,row_fields=dict(centerX_f32=0,centerY_f32=4,squared_screen_radius_f32=8,primitive_pointer=12),
            count_offset=0x1000,capacity_from_static_producers=256,
            iterator='Startatsignedindex*indexPtr;return firstrow where (screenX-centerX)^2+(screenY-centerY)^2 < squaredradius,advanceindexby1. Continueuntilcount,thenreturnNULL.',
            tested='Prepared2rows; centers9withr2=100accepted,centers10tangentrejected,centers20rejected. EmptyregistryreturnsNULL. No automaticdepthsort beforecallback.',
            producer_scope='Actual580EC0/584900registryappend sites identifiedfromoriginalvtablepointers; rendererpopulation/256capacityboundary notexecuted. No shuffledsource/auto-sort rule inferred.'),
        selection=dict(passes='Firstcallseachcandidaterealvslot0C withmode1; ORallcallbackreturns. Ifanynonzero,returnoutputtokenandsetoutBYTE1. Otherwiseiterateregistryagainwithmode0andreturnoutputtoken,outBYTE0.',
            shared_depth='Bothpasses shareinitialfloatdepth1e30 andoutputtoken0. Callbacksupdateonlywhennewdepthisstrictlysmaller.',
            exact_priority='Afartherexacttrianglehit canwin overnearer modelwithnotrianglehit; coarsepassisnotexecutedwhenanyexactcallbackreturns1.',
            ties='Equaldepthretainsfirsteligible registryentry; reversingnormalunequaldepthorderstillselectsnearer model.',
            output='EAXisopaqueper-primitivetoken; preparedtokensareActorPointers9/10. OutparameterisonlyBYTE: initialupperA5A5A5preserved; thisisnot aDWORDboolean field inKWD.'),
        dynamic_geometry=dict(metadata='Preparednativecachedescriptor:meshptr8,vertexptrC,meshcount18,coarsecenter24. Meshstride5C,vertexindexrecords20bytes,triangles3BYTEindices.',
            exact='Originalmodel3x3matrix -> primitiveXYZ ->cameraoffsetandmatrix ->perspectiveprojection; originalthreeedgecrossproducttests withzero threshold; candidate depth=arithmeticmean ofthreeclamped vertexdepths.',
            coarse='Transformdescriptormodelcenter andcompareclampedcenterdepth. No trianglecontainmentinthissecondmode.',
            tested='Singletriangleperscene/identitymatrices/preparedcamera; windingreversalmissesexactthenfallsback; no-trianglecount fallsback; vertexdepths1/4/4loseagainst2/2/2,confirmingmean-depthselection inselectedscene.'),
        animated_geometry=dict(metadata='Preparednativecachedescriptor andpacked10-bitXYZvertextracks,frame0,three18-bytevertexindexrecords; actual57E5B0 executes.',
            exact='Originalpackedvertexdecode/interpolationandmatrix/perspective/edge/mean-depthconsumer execute; actual sourcevtable585AD0 used.',
            tested='Twoidenticalpreparedframes,frame0. Thisprovesactualconsumerpath,notactualKMFparsing,nontrivialanimationorGPUoutput.',
            coarse='Originaldescriptorcenter10 transformed;callbackoutputtokenatprimitive50 ratherthandynamic4C.'),
        depth_clamp=dict(original_threshold_float_address=0x66FC80,threshold_bits='3a83126f',float_value=0.0010000000474974513,
            rule='Eachtesteddepthbelowthresholdreplacedbyfloat001. Negativecenterdepthsminus1/minus2 becomeequalcoarsedepths;firstregistryentrywins.',
            limits='Nocompletenear-planeclipping/behindcamera/renderingcorrectnessclaim. NoNaN/Inf/extremeprojection inputtests.'),
        runtime_scope=dict(scene='Preparedcachelinkedlistandthreedescriptorhandles,screenregistry,primitives,one-trianglegeometry,camera,Actor/World. FullrawCreature/Objectloaders execute; fullActor-to-primitivecreation doesnot.',
            cache='Actual57ED30/57ECE0 reorderpreparedcachelistandreleaseflags; allhandlelinks/head/tail/countchecked. ThreecoherentnodesavoidNULL/dummycallbacks; dummyunuseddescriptorisnotreturnedbyregistry.',
            integrity='Allotherpreparedmemoryunchangedoutsideknownloader/cache/scratch/outputregions; scratchguard16A5bytespreserved. LowreadsareonlyactualFS:0inCreatureloaders.',
            no_source_geometry='KMF/WAD filesnotdecodedbythisharness. Preparedgeometryisdeclaredexplicitly; no claimthatstandaloneImp actuallyusesthesevertices.'),
        remaining_priority=dict(CreaturePriority=dict(raw=0xF55,runtime=0x6EB),CreatureJump=dict(raw=0xEE0,runtime=0x75C),ObjectPriority=dict(raw=0x35D,runtime=0x112),
            new_scope='Nooverlappingdefinitionreadin27newrawload+primarypicktraces. SwappingPriority0/255,255/0,1/40 andJump80000000/FFFFFFFF leavesnearer-tokenresult; objectpriority0/255same.',
            unresolved='Bothselectedprimaryandpreviousfallbackselectionconsumersareknown,butnativeActor/RenderInfo->primitiveconstruction andregistrypopulationnotcomposed. Thoseproducersmaycopyderivedvalues; do notinferglobalunusedfields.',
            gameplay_purpose='unresolved',global_nonuse_proved=False),
        native_executions=manifest,independent_Ghidra_controls=cross,preservation=preservation,
        serialization=dict(new_raw_layout_changes=0,new_exact_container_edits=0,
            inherited_priority_API_validation=link('phase153-pickup-api-validation.json'),inherited_priority_edits=328,
            scope='No newserializerorphysicalfieldchange. Prior328exactprioritypatch/decode-rebuildcontrolsretainhashes; notcountedasnewtestsorrepeatedinphase154. Allnewdiagnosticraweditsstayonlyinmemoryandoriginalsourcesarehashchecked.'),
        counts=dict(native_scenarios=27,complete_primary_pick_calls=27,actual_Creature_definition_loads=54,actual_Object_definition_loads=27,
            native_model_callbacks=sum(totals[a] for a in [0x582D30,0x585AD0]),dynamic_callbacks=totals[0x582D30],animated_callbacks=totals[0x585AD0],native_animated_vertex_calls=totals[0x57E5B0],
            independent_Ghidra_CPU_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_code_extents=len(attested['functions']),static_only_extents=sum(r['static_only'] for r in attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
            new_exact_container_edits=0,inherited_exact_priority_edits=328,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['emulate_primary_pick.py','native_unicorn_primarypicktrace.py','prepare_primary_pick.py','crosscheck_primary_pick.py']],
        limitations=['ExactDX;completeprimaryquerycallbacks executewithpreparedgeometryandcamera,notcomplete5992B0callerorclick/pickup/gameWorld/GPU.',
            'Rawdefinitionsloadnatively;Actor-to-RenderInfo/primitiveconstruction andrealregistryrenderpopulation notexecuted. No globalPriority/Jumpnonuse ornewrawpurpose claim.',
            'Dynamic/animatedsingletriangles,identitytransforms,frame0andtwoidenticalpackedframes. RealKMFassets,nontrivialanimation/cameraorientation,NaN/Inf/clippingandregistrycapacityboundary notcovered.',
            'Modelcache/sceneinputs areexplicitpreparedstate;fileson disk unchanged. Coarsecentercomparison isnotevidence thatallvisiblemodelswillbepickableeveryframe.',
            'EightGhidracompositionscomparefullpreparedmemorytoUnicorn andsame selectedtoken/flag; rawAPIcontrolsareinheritedphase153,notnew328replays.',
            'Full15familygoal incomplete; remainingphysical/semanticunknowns preserved.'])
def main():
    s=build();(ROOT/'primary-pick-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase154-prior-components.json')
    assert set(p['components']) in [set(old),set(old)|{'primary_pick_semantics'}]
    for key,row in old.items():assert p['components'][key]==row
    p['primary_pick_semantics']=s;p['components']['primary_pick_semantics']=link('primary-pick-semantics.json')
    note='primary_pick_semantics phase154 executes complete572820->588A60->originaldynamic582D30/animated585AD0 callbacks,actualpackedvertexdecoder/cache/matrixmath. Exacttrianglepass precedescoarsecenterpass; meanvertexdepth/strictties/registrycircle tested.27native/60callbacks/8Ghidra. Rawpriority/Jumpfieldsloadedbutunreadinselectedpath; Actor-to-primitiveproducer/renderregistrypopulation stillunexecuted,globalpurposesunresolved. Preparedgeometry,notrealKMF/GPU. No effectiveorphysicalviews change; phase153serializercontrols inheritedunchanged.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
