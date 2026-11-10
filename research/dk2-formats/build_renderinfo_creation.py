"""Publish complete native dynamic-primitive creation and scoped verification limits."""
from pathlib import Path
import json
import emulate_renderinfo_creation as native
import emulate_renderinfo_support as support
import crosscheck_renderinfo_support as controls
import crosscheck_renderinfo_creation as fullcontrol
import prepare_renderinfo_creation_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase155-renderinfo-primary.json')
    manifests=[]
    for module,name,n in [(native,'phase155-renderinfo-emulations.json',10),(support,'phase155-support-emulations.json',4)]:
        m=load(name);assert len(m['replays'])==n
        for c,row in zip(module.cases(),m['replays']):
            if module is native:
                args,checks,tokens,d,p=module.execute(c);_,out=module.verify(c,args,checks,tokens,d['result'])
            else:args,checks,d,p=module.execute(c);_,out=module.verify(c,args,checks,d['result'])
            assert row['case']==c and row['outcome']==out and row['sha256']==sha(p.read_bytes())
        manifests.append(m)
    cross=load('phase155-support-crosschecks.json')['replays'];assert len(cross)==4
    for c,row in zip(support.cases(),cross):
        args,n,size=controls.build(c);d=load(row['file']);r=d['result']
        assert row['case']==c['name'] and d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    limited=load('phase155-renderinfo-Ghidra-limitation.json');d=load(limited['file']);r=d['result'];args,_,_=fullcontrol.build('base')
    assert d['input']==args and limited['sha256']==sha((ROOT/limited['file']).read_bytes()) and not limited['full_chain_control_passed']
    assert not d['tool_is_error'] and not r['success'] and not r['hit_return'] and int(r['final_pc'],16)==0x41C61D and 'Unimplemented CALLOTHER pcodeop (fcos)' in r['emulation_error']
    _,checks,tokens,excluded,path=native.p.execute(dict(name='base'))
    assert not excluded['result']['success'] and excluded['result']['emulation_error']=='Invalid memory read (UC_ERR_READ_UNMAPPED)' and int(excluded['result']['registers']['EIP'],16)==0x582F56
    assert all(c['returned'] for c in excluded['result']['before_calls'])
    preservation=load('primary-pick-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    old=load('phase155-prior-components.json');assert len(old)==133
    for row in old.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    return dict(phase=155,date='2026-10-31',families=['Creatures','Objects'],profile=attested['profile'],primary=attested,
        native_chain=dict(creator=0x59CCF0,allocator=0x57CEB0,allocator_search=0x57CD30,constructor=0x580480,position_helper=0x59C2A0,
            scale_and_rotation='OriginalFCOS/FSIN andmatrixfunctions executeinUnicorn;no opcode substitutes. Twofullnativecreations peracceptedmaincase.',
            raw_definitions='RealCreature/Objectloaders executebeforeeachmaincase. PreparedRenderInfo inputs areexplicit; upstreamCreature-to-RenderInfofieldsetup isnotcomposed.'),
        native_heap=dict(seed='Two192-bytefreechunks,nativebucket30,doublelinkedfree-list;no OS allocation required byselectedpath.',
            requests='Original59CCF0requests8Dhex=141bytes. Actual57CEB0/57CD30 choose192-bytechunksincluding20-byteheader; returnschunk+20.',
            verified='Bothchunksallocatednatively; headerusedbyte18 becomes1, secondprevlinkcleared, bucketheadbecomesNULLaftertwocalls. 31unusedpayloadbytes/chunk and64outerguardA5bytespreserved.',
            independent='Separatecomplete141/153requestcasescheckedbyGhidra; allocatorreclamation/expansion/OSmalloc notexecuted.'),
        RenderInfo_runtime_fields=dict(width_Q12_u32=4,height_Q12_u32=8,resource_pointer=0x28,rotation_u16_offsets=[0x20,0x24],
            color_bytes=[0x48,0x49,0x4A],alpha_byte=0x4B,offsets_s16=[0x51,0x53,0x55],flags_u32=0x5B,forwarded_parameter_byte=0xAC,
            warning='Offsetsrelative topreparedRenderInfo atActor+44,notrawCreatures/Objectsrecord offsets. AC isforwarded butnotnewlynamedasPickUpPriority.'),
        position=dict(helper=0x59C2A0,input='CurrentandprevioussignedXYZpointersplusRenderInfo3signedWORDoffsets',
            formula='outX=(inputX+s16RI51)/4096;outY=(inputY+s16RI53)/4096;outZ=-(inputZ+s16RI55)/4096, separatelyforcurrentandprevious.',
            native_cases='Zerooffsetand[1,-2,3] verifiedwithdifferentcurrent/previousvectors; bothcompletehelpershaveindependentGhidrawholememorycontrols.',
            limits='Selectedinputs; extremeintegeroverflow notexecuted andnotnormalized.'),
        constructor_output=dict(native_vtable=0x66FD1C,bytes=141,positions_f32=[8,12,16],matrix3x3_f32=0x14,resource_handle=0x38,
            RGB_f32=[0x3C,0x40,0x44],alpha_f32=0x48,opaque_output_token=0x4C,flags_u32=0x50,max_axis_scale_f32=0x58,forwarded_byte=0x8C,
            scale='Selectedfixedrotations:RIwidth/4096 scalesmodelX/Z,RIheight/4096scalesY. Halfwidthanddoubleheighttests matchexpectedmatrixdiagonalwithin1e-5; max-axis-scale equalsmax(width,height).',
            alpha='EndpointRI4B0->float0,255->float1. RGB255->255floatinthetestscenes;no completealpha/gamma/renderedpixel rule.',
            byte_AC='RIAC0/1/255 preservedatprimitive8C; thisdoesnotidentifywhichrawfieldorupstreamsetter suppliedit.',
            token='PreparedActorPointerparam passedthroughnativecreator/constructor toprimitive4C; actualrenderregistry/picking aftercreation notexecuted.'),
        renderer_staging=dict(untouched_fields=[0x5C,0x70],observed_seed_hex='a5a5a5a5',
            new_fact='BothDWORDs remainpreviousallocationbytesinall20acceptednativecreatedprimitives. Thisisnot a serializeddefault.',
            static_consumer='Original580EC0assignsprimitive5Cfromrendererargument8 andsetsprimitive70geometry-selectionindexbeforeinsertingcandidateintoregistry.',
            excluded_direct_pick=dict(file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),final_pc='0x582f56',
                reason='Initialcreation->immediatepickingexperiment omittedrenderer-staging. A5geometry-selectionindex causedunmappedread; nativecreators hadreturned. Excludedfixture,notprooforiginalgame/editor corruption.'),
            limits='Fullrenderer/textureselection/registrypopulation remainsunexecuted. Neverforceunassignedbytes0inserializerorinfermeaningfromheapjunk.'),
        remaining_priority=dict(CreaturePriority=dict(raw=0xF55,runtime=0x6EB),CreatureJump=dict(raw=0xEE0,runtime=0x75C),ObjectPriority=dict(raw=0x35D,runtime=0x112),
            result='Nooverlappingdefinitionreadin10acceptedcreationcompositions. Priorityreversed/JumpHighBitrawdefinitionsloadcorrectly; outputprimitives unchangedinrelevantselectedfields.',
            status='gameplaypurposesremainunresolved',global_nonuse_proved=False,
            boundary='PreparedRenderInfo creation isnowexecuted,butupstreamActor/AI->RenderInfo andactualrenderer-stagingnotcomposed. ACforwardingisnotarawPrioritybinding.'),
        native_executions=manifests[0],support_native_executions=manifests[1],independent_Ghidra_support_controls=cross,full_Ghidra_limitation=limited,
        serialization=dict(new_layout_or_codec_changes=0,new_exact_container_edits=0,inherited_priority_API_validation=link('phase153-pickup-api-validation.json'),
            rule='Rawunknownfields/currenteffectiveviewsunchanged;prior328priorityserializationcontrolsretainhashesandarenotcountedasnewruns.'),
        preservation=preservation,counts=dict(native_creation_scenarios=10,complete_native_creators=20,actual_Creature_definition_loads=20,actual_Object_definition_loads=10,
            native_support_scenarios=4,independent_Ghidra_support_controls=4,independent_full_creation_controls_passed=0,terminal_Ghidra_FCOS_limitations=1,
            Ghidra_support_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_support_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_code_extents=len(attested['functions']),static_only_extents=sum(r['static_only'] for r in attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
            new_exact_container_edits=0,inherited_exact_priority_edits=328,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['probe_renderinfo_creation.py','emulate_renderinfo_creation.py','emulate_renderinfo_support.py','crosscheck_renderinfo_creation.py','crosscheck_renderinfo_support.py','prepare_renderinfo_creation_primary.py']],
        limitations=['ExactDXnativecode/fullUnicorncreatorcompositions,preparedRenderInfo/resource-map/cacheandfreeheap. NoactualKMF/GPU/nativeActor-to-RenderInfobindingorfullrenderer.',
            'FullGhidracreationcontrol didnotpass: FCOS useropunsupported at41C61D. Fourpassedcontrolscoveronlycompleteallocatorandcoordinatehelpers;donotpromotethemtofullconstructorverification.',
            'Originalmatrixcode executedunderUnicornandattestedtoPE; selectedscalar/matrixoracleschecked,tolerance1e-5forselectedfixedrotations. Otherangle/mode/animatedcreator/heapexpandbranchesnotexecuted.',
            'Surface70andrenderer5Cpreviousbytesareunassignedinselectedconstructorpaths,notproofgame/editorbug. Failedimmediate-pickingfixtureexcluded.',
            'ACparameterbyteforwardingisnotaprioritylabel. RawPriority/Jump/otherfull15-familyunknowns preservedandgoalunfinished.'])
def main():
    s=build();(ROOT/'renderinfo-creation-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase155-prior-components.json')
    assert set(p['components']) in [set(old),set(old)|{'renderinfo_creation_semantics'}]
    for key,row in old.items():assert p['components'][key]==row
    p['renderinfo_creation_semantics']=s;p['components']['renderinfo_creation_semantics']=link('renderinfo-creation-semantics.json')
    note='renderinfo_creation_semantics phase155 executes real59CCF0->57CEB0/57CD30->580480 afterrawCreature/Objectloaders.20nativecreatedprimitives/10scenes,scale/position/alpha/token/ACforwardingchecked. Surface70andrenderer5Cremainpreviousheapbytesuntilrenderer-staging; excludedimmediate-pickfailureisfixturegap,notgamebug. FullGhidracontrolstopsatunsupportedFCOS41C61D,0fullpasses;4Ghidracontrolscoverallocator/positiononly. Priority/Jumpunreadinselectedpaths,upstreamActor-to-RenderInfounexecuted. No physical/effective/codecchanges.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
