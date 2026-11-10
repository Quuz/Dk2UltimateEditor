"""Publish rawTrap->actualfactory->activation and conservative catalogue-consumer search."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_trap_creation as native
import crosscheck_trap_creation as controls
import prepare_trap_creation_primary as primary
import validate_trap_activation_storage as storage
import catalogue_call_abi as abi
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase161-prior-effective-traps.json'))
    for f in out['fields']:
        if f['offset']==0x202:f['verified_native_creation_activation']=dict(MID_offset=0x2AA,definition_offset=0x96,consumer=0x4C1B60,
            numeric_view='signed16 from rawWORD, preservedbitpattern',
            factory='RawloadedGas/Sentry/Trigger actualfactory registersTrap with10Cbit20set; noManaUsageadditionyet inselectedpaths.',
            activation='Full4A96D0 clearsinstance10Cbit20; originalTriggerType3/otherhelpers execute then4C1B60adds signedusage toPlayer10D6 andsubtractsfrom10E2.',
            repeated='Directsecondactivation addsagain; no normalWorld double-call guarantee.',
            scope='PreparedWorld/nullgraphics/soundproviders; realallocator/constructor/map/owner/tagregistration. No wholeWorldscheduler/shot/deletion.')
        elif f['offset']==0x204:f['verified_native_creation_activation']=dict(status='unresolved',MID_offset=0x2AC,definition_offset=0x98,
            observation='14complete rawdefinition/factory/optionalactivation scenarios; no reads ofdefinitionWORD98. Threewholememorypairs differonlyraw/MID/definition copies anddefinitionchecksum.',
            all16bits_preserved=True,global_nonuse_proved=False)
    return out

def build():
    attest=primary.build();assert attest==load('phase161-trap-creation-primary.json')
    manifest=load('phase161-trap-creation-emulations.json');assert len(manifest['replays'])==14
    posts={};trace=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,node,d,path=native.execute(c);post,out=native.verify(c,args,ch,node,d['result']);posts[c['name']]=post;trace.append(d['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
    pairs=[native.compare_pair(posts['gas-active'],posts[n],n) for n in ['gas-unknown0','gas-unknownFFFF']]
    pairs.append(native.compare_pair(posts['trigger-active'],posts['trigger-unknown'],'trigger-unknown'))
    assert pairs==manifest['paired_controls']
    cross=load('phase161-trap-creation-crosschecks.json')['replays'];assert len(cross)==8
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert row['sha256']==sha((ROOT/row['file']).read_bytes()) and d['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase161-trap-activation-storage-validation.json')
    assert sha((ROOT/'dk2_formats.py').read_bytes())==load('phase159-terrain-light-storage-validation.json')['codec']['sha256']
    search=load('phase161-catalogue-bounded-argument-leads.json');abidata=load('phase161-catalogue-call-ABI.json');abifns,rejected=abi.evidence()
    assert abidata['functions']==list(abifns.values()) and abidata['rejected_extents']==rejected
    assert len(abifns)==548 and len(abidata['input_manifests'])==30 and not abidata['indirect_sites']
    assert search['constant_domain_sha256']==sha((ROOT/'constant_branch_domain.py').read_bytes()) and search['ABI_evidence_sha256']==sha((ROOT/'phase161-catalogue-call-ABI.json').read_bytes())
    assert abifns[0x4C5FD0]['callee_pop']==4 and abifns[0x4C5FD0]['argument_bytes']==4
    assert len(search['producer_roots'])==438 and search['root_states']==8641 and search['limited_roots']==1 and search['limited_callees']==3
    unknown=[h for h in search['candidates'] if not h['field'].startswith('control')]
    assert len(unknown)==8 and {h['field'] for h in unknown}=={'possibleDoorUnknown236','possibleTrapUnknown204'}
    assert not any(h['field'] in ['ObjectUnknown34C','ObjectPriority','TrapUnknown204','DoorUnknown236'] for h in unknown)
    height=next(h for h in unknown if h['field']=='possibleTrapUnknown204');assert height['read']['address']=='0x5955e5' and height['read']['offset']==0x97
    text=(ROOT/'ghidra/phase161-primary-5955c0-assembly.txt').read_text(encoding='utf8')
    assert '005955c9: MOV EAX,dword ptr [EDI + 0x12]' in text and '005955d4: JZ 0x0059560c' in text
    assert '005955e5: MOV AX,word ptr [ECX + 0x97]' in text and '00595614: MOV AX,word ptr [EDX + 0xa0]' in text
    prior=load('phase161-prior-components.json');assert len(prior)==139
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('native-door-reader-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in trace)
    counts=dict(native_scenarios=14,Trap_raw_loads=visits(0x4AA440),actual_Trap_factory_calls=visits(0x4B7C10),actual_Trap_constructors=visits(0x4B7400),
        registered_Traps=14,complete_activation_calls=visits(0x4A96D0),actual_Player_upkeep_add_calls=visits(0x4C1B60),
        independent_full_Ghidra_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        whole_memory_pairs=3,pair_compared_bytes=sum(r['compared_bytes'] for r in pairs),original_code_extents=len(attest['functions']),
        static_only_function_extents=2,unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        exact_changed_container_rebuilds=serial['exact_changed_container_rebuilds'],source_Trap_records=serial['records'],source_Trap_files=len(serial['files']),
        search_roots=len(search['producer_roots']),search_states=search['root_states'],search_callee_contexts=search['callee_summaries'],search_limited_roots=search['limited_roots'],search_limited_callees=search['limited_callees'],
        byte_attested_direct_ABI_records=len(abifns),newly_named_opaque_fields=0,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=161,date='2026-11-01',family='Traps',profile=attest['profile'],primary=attest,
        native_pipeline=dict(raw_size=579,MID_size=747,definition_size=209,instance_size=291,
            chain='4AA330/570B60/570D10/4AA440 actualrawdefinition ->4B7C10->554B30->4B7400/507850->4A7940->nativeWorld/getter/owner/map/tagregistration;optionalcomplete4A96D0.',
            inputs='OriginalGasID1,SentryID3,TriggerID9 records, explicitlypatchedunknown/usageinmemoryonly. Cataloguehasoneactualdefinition andoriginalWorld160getter.',
            actual_instance='OriginalTrapvtable66D754,kind3,tag100,sourceIDF6/definitionF7,owner7/PLAYER,HPfromdefinitionA2,XYZ5800/6800/2000hex; allasserted.',
            allocator='Original291byteallocation rounds292+12header=304,free208in512bytearena;fullheaders/freelist/usedlist/guardschecked.',
            registration='NativeWorld32count1,PlayerWORDCtag100/DWORD22count1,globaltagpointer/alive,kind3head,mapcell5/6head100;neighborlinks0.'),
        activation=dict(factory='Setsinstance10Cbit20; rawGas/Sentry/Triggercreation leavesPlayerupkeep100/income1000.',
            callback=0x4A96D0,enable='Clearsinstance10Cbit20, callsfull4A9F10, then TriggerType==3 ->4A9720 else4A9A00, thenPlayer4C1B60.',
            nearby='OriginalWorldwidth/height/tilegetters and453700 run onpreparedempty-neighborTrapmap; no fabricatedneighborreturncallback. No ruleforpopulatedTriggernetworkassigned.',
            mana_rule='MOVSXWORDdefinition96(raw202); addtoPlayer10D6 andsubtractfromPlayer10E2, modulo32. Raw204/definition98notread.',
            examples=[dict(raw_usage=5,Player_upkeep=105,Player_income=995),dict(raw_usage=65535,signed_usage=-1,Player_upkeep=99,Player_income=1001),
                dict(raw_usage=32768,signed_usage=-32768,Player_upkeep=-32668,Player_income=33768),dict(raw_usage=32767,Player_upkeep=32867,Player_income=-31767)],
            repeated='TwoexplicitcompleteactivationcallsontheactualsamecreatedTrap produceupkeep110/income990. Callbackhasnoprevious-bitguard; notnormalWorldschedulerproof.',
            gameplay_limits='No shot,HPdamage,ownertransfer,removal/destruction or normal scheduler executed; accountingoutcomesarenotproofnegativeupkeepisbalancedoverwholegame.'),
        opaque_review=dict(raw_WORD=0x204,MID_WORD=0x2AC,definition_WORD=0x98,status='unresolved',
            all16bits_preserved=True,selected_definition_reads=0,whole_memory_pairs=pairs,global_nonuse_proved=False,
            distinction='NegativeManaUsage202isconfirmed consumer; raw204FFF6 sourcecorrelation doesnotassignnegativeusage/ManaGenmeaning to204.'),
        pointer_search=dict(evidence=link('phase161-catalogue-bounded-argument-leads.json'),ABI_evidence=link('phase161-catalogue-call-ABI.json'),
            helpers=[link(n) for n in ['catalogue_call_abi.py','scan_catalogue_consumers_bounded.py','phase161-ABI-input-manifests.json']],
            root_classes='246Object/entity12D/vslot188/direct50DB20;148sharedTrapOrDoorF7hints;25Doorvslot138/direct50D650;19Trapvslot160/direct50D8C0. Entity/vslothintsarenotverifiedobjecttypes.',
            method='NumericCMP/branch/stacklocals andknownindexvalues; consistentRETcleanupsfrom548exactDXfunctions. Up to3directcalllevels/first8potentialstackargs; excludesargumentsbeyondknowncalleeRETpop.',
            ABI_limits='30primarymanifests;94extentoccurrencesrejectedforlistingentry/inconsistentRET/outgoingorindirectJMP. Noindirectbindings,arbitraryRAMnumbers,stackpointeealiaswrites orglobalABIproof.',
            result='128fieldoverlaphints;only8unknown-shaped:7historicalDWORD9AsharedF7readsandoneWORD97heightpath. No newconfirmedObject34C/Priority/Trap204/Door236consumer.',
            scoped_height_rejection='Original5955C0kindbranch:kind4Doorreads97/98(height),kind3TrapreadsA0/A1. 5955E5numericallyoverlaps98butisDoorHeight, notTrap204. StaticonlynewDXcontext; oldanalogueclassifiedhistorically.',
            false_argument='Previouslybounded4C5FD0contextstackslot32wasnotformalarg:exacthelperRET4 permitsonlyslot4. Contextremoved; memorylinkedlistloophasnoinventedbound.',
            remaining_limits='OneObjecthintroot4F4CFB reaches1500states,3callees4A7100/4AA440/49DD60reach1000states. Noindexed-eventsfoundinreachedpaths; thisdoesnotexhaustindexed/return/indirect/globalpaths.',
            global_nonuse_proved=False),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,effective_trap_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['probe_trap_creation.py','emulate_trap_creation.py','crosscheck_trap_creation.py','prepare_trap_creation_primary.py','validate_trap_activation_storage.py']],
        limitations=['PreparedWorld8000prefix/separateA360scalars,512bytepool,32x32map/player7; originalcreation/registration executes but noWorldbootstrap/fileparser/scheduler.',
            'ExternalnullGFX/soundlookup0/playtoken13572468; no realassets/GPU/pixels/audio. Originalcode/vtables retained.',
            'OnlyGas1/Sentry3/Trigger9 withselectedrawflags. EmptyTrigger-neighbor scene; no fullTriggernetwork orall16Traptypes proof.',
            'Repeatactivation explicitdirectcalls; no normalgame doublecount claim. NegativeManaUsage outcome limitedtoaccounting,notentirelifecycle.',
            'Unknown204remainsunresolved; Object34C/Priority andDoor236 notdecoded. Negative/boundedsearchisnotglobalnonuse.',
            'Searchone root/threecallee limits,entity/vslottypeshints,unknownreturns/indirects/stackpointeealiaswrites unresolved.',
            'No newphysicalwidth/codecchange;prior139schemas/sources preserved,onlycurrentTrapsannotationsextended. Significant15familyunknowns remain.'])

def main():
    s=build();(ROOT/'trap-creation-activation-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase161-prior-components.json');key='trap_creation_activation_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('trap-creation-activation-semantics.json');p['effective_catalogue_layouts']['Traps']=s['effective_trap_layout']
    note='trap_creation_activation_semantics phase161 executes raw579/MID747/definition209->actual291byteTrapfactory/allocator/tag/map/ownerregistration->full4A96D0.14native/14registeredTraps/12activationcalls,8wholeGhidra/328regions/631128bytes;46extents/1797PC/5blocks. SignedManaUsage202addsPlayerupkeep/subtractsincomeafteractivation;repeatdirectcalladdsagain,notnormalWorlddoublecallproof. Unknown204unreadselectedpaths;3wholememorypairsdiffonlystorage/checksum.246exactscalarcontrols82files/1293Traps. Newconservativecataloguepointersearch438roots/8641states/29contexts/548byteattestedRETrecords;1root/3calleelimits;nonewopaqueconsumer. Prior139schemas/420sources/7EXEpreserved;effectiveTrapsannotationsonly.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
