"""Publish bounded native AP-tree semantics without changing historical schemas."""
from pathlib import Path
import json
import emulate_ap_trigger_tree as native
import crosscheck_ap_trigger_tree as control
import prepare_ap_trigger_tree_primary as primary
import validate_ap_trigger_tree_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attest=primary.build();assert attest==load('phase156-AP-tree-primary.json')
    manifest=load('phase156-AP-tree-emulations.json');assert len(manifest['replays'])==33
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,p=native.execute(c);_,out=native.verify(c,args,ch,d['result']);assert row['case']==c and row['outcome']==out and row['sha256']==sha(p.read_bytes())
    cross=load('phase156-AP-tree-crosschecks.json')['replays'];assert len(cross)==12
    for row in cross:
        c=next(c for c in native.cases() if c['name']==row['case']);args,n,size=control.build(c);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase156-AP-tree-storage-validation.json')
    old=load('phase156-prior-components.json');assert len(old)==134
    for row in old.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    previous=load('renderinfo-creation-semantics.json')['preservation'];sources={str(Path(r['file'])):r['sha256'] for r in previous['source_files']}
    for row in serial['source_files']:
        p=str(Path(row['file']));assert p not in sources or sources[p]==row['sha256'];sources[p]=row['sha256']
    preservation=dict(source_files=[dict(file=p,sha256=h) for p,h in sources.items()],executable_paths=previous['executable_paths'])
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    return dict(phase=156,date='2026-10-31',families=['Things.ActionPoint','Triggers'],profile=attest['profile'],primary=attest,
        physical_layout=dict(trigger_payload_bytes=16,parameters=[0,8],Id_u16=8,NextId_u16=10,ChildId_u16=12,Type_u8=14,RepeatTimes_u8=15,
            runtime_node_bytes=20,runtime_active_DWORD=16,runtime_active_bit=1,
            boundary='The final active DWORD is runtime-only, NOT four additional on-disk bytes. Prepared nodes retain exact16-byte source payloads in three 3DLevel cases; no native raw parser/loader in this phase.'),
        AP_execution=dict(bind=0x4B0C90,tick=0x4B05E0,recursive_tree=0x4B0CC0,condition_dispatch=0x4B0D70,action_dispatch=0x518AD0,
            condition_lookup=0x518090,action_lookup=0x5180F0,
            runtime_AP_fields=dict(flags_BYTE=0x32,trigger_WORD=0x34,cached_repeat_BYTE=0x36,bounds_WORDs=[0x38,0x3A,0x3C,0x3E]),
            runtime_World_fields=dict(condition_array=0x7308,action_array=0x730C,condition_count_WORD=0x7314,action_count_WORD=0x7318),
            signed_link_dispatch='WORD interpreted as signed16:0 terminates;positive uses condition array;negative uses action array. Physicalcodec keepsu16 bitpattern.',
            bind_nonzero='Stores low16 trigger ID and caches RepeatTimesBYTE from condition getter. Missing key caches first condition; negative AP root also goes through condition getter when binding.',
            bind_zero='Does nothing, preserving existing trigger/cache. Zero initial trigger produces no tree traversal; nonzero old trigger survives bind0.',
            getters='Linear first-ID match in their own20-byte array; on miss return array base, notNULL. Count0 also returnsbase statically; zero-count execution not tested.',
            traversal='Skip inactive nodebody then follow NextId. True positive condition descends toChildId, passing that condition RepeatTimes. Successful action clears its own active bit onlyif inheritedBYTE==1, then descends toChildId withsameBYTE before following NextId.',
            repeat='In AP traversal, parent RepeatTimes1 gives once-only successful actions. Tested0/2/255 retain active actions throughthree ticks. Action own RepeatTimes is not used as this gate; nested condition supplies its own value.',
            decrement_helper=dict(address=0x508640,meaning='Decrement own RepeatTimesBYTE; clear own active bit when result0.',
                AP_callsite='4B0D17 only if recursive child returns nonzero; all ordinary4B0CC0returns explicitlyXOREAX,EAX at4B0D69. Thus helper isnotreached on this ordinary AP recursion;0calls in99ticks.',
                boundary='Does not describe other trigger walkers, abnormal flow or builds. No whole-game repeat counter defect claimed.'),
            outer_tick_return=1,ordinary_tree_return=0),
        AP_flags_review=dict(tested_runtime_masks=[0,4,8,12],combinations=16,ticks_per_scenario=3,overlapping_flag_reads=0,
            result='No AP+32 read or effect inselected completebind/tick/condition/action paths; FlagsBYTE unchanged.',
            raw_mapping_inherited='action-point-semantics.json already attests rawFlagsWORD12 -> runtimeBYTE32,mask003D; this phase prepares runtime records and doesnotrepeatloaderproof.',
            status='0x04 and0x08 gameplaypurposes remainunresolved',global_nonuse_proved=False),
        selected_actions=dict(type44=dict(label='SET_CREATURE_MOODS',World72A1_mask=1),type45=dict(label='SET_SYSTEM_MESSAGES',World72A1_mask=2),type51=dict(label='SET_TIMER_SPEECH',World72A1_mask=4),
            native_result='Native brancheswrite respective gate using availableBYTE0==1,preserving otherbits. Selected nativecases use44available1,45available1and0,51available1.',
            scope='Names retained as editor/source labels; downstreamsystemmessage/speech consumers nottracedhere. Thisphase doesnotclaim all255bytevalues or wholeUIeffects.'),
        real_source_nodes=dict(file=str(native.SOURCE),sha256=sha(native.SOURCE.read_bytes()),condition_payload_hex='040000000000000001000000ffff2201',action_payload_hex='0000000000000000ffff000000002d00',
            condition='Id1,LEVEL_TIME34,comparison4,thresholdDWORD0,ChildFFFF,Repeat1',action='IdFFFF,type45,available0,Repeat0',
            executions='Exact16-byte payloads copiedto prepared20-byte runtimenodes; activeDWORD A5 appended separately. ThreeAPticksatclock0,1,80000000; onlyclock1 clears World72A1mask0x02 once.',
            time_rule='518270case34 comparesSessionDWORD+4 returnedby517490 against parameterDWORD4 as signed32; noTPSconversionintheselectedcondition.',
            boundary='PreparedAPinvokesrealnodes; notproof3DLevelnaturally attaches thisgraph toanAPor its normalWorldcaller.'),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,
        counts=dict(native_scenarios=33,complete_native_AP_ticks=99,complete_native_bind_calls=33,
            native_condition_dispatch_calls=sum(r['outcome']['condition_calls'] for r in manifest['replays']),
            native_action_dispatch_calls=sum(len(r['outcome']['action_events']) for r in manifest['replays']),
            recursive_tree_calls=sum(r['outcome']['tree_calls'] for r in manifest['replays']),native_repeat_decrement_calls=0,
            exact_original_source_node_scenarios=3,independent_full_Ghidra_controls=12,
            Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_code_extents=len(attest['functions']),static_only_extents=sum(r['static_only'] for r in attest['functions']),unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
            exact_container_edits=serial['exact_container_edits'],preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['emulate_ap_trigger_tree.py','crosscheck_ap_trigger_tree.py','prepare_ap_trigger_tree_primary.py','validate_ap_trigger_tree_storage.py']],
        integration_rules=['KeeprawRepeatTimesBYTE unchanged; AP execution rules do not justify renaming globalRepeatTimes toonce-flag or normalizing2/255.',
            'PreserveallAPflagbits. 04/08areunresolved; onlyselectedruntimepathnonuseconfirmed.',
            'Expose danglinglinks/duplicateIDs/cycles as diagnostics; neverrepair by rewriting firstarrayentry. MissingIDcanexecute firstexistingnode.',
            'RuntimeactiveDWORD andWorld/APoffsets are noton-disk serializedfields; keep16-byte triggerpayload and original8-byte envelope.',
            'Keep previous physical/effective schemas unchanged. Apply scopedAP rules throughthiscomponent only.'],
        limitations=['PreparedruntimeWorld/AP/arrays;no originalKLDparser/triggerloader/APallocator/fullWorldscheduler. Nativegetters/bind/tick/recursivecondition/actioncode fullyexecutes withoutsubstitution.',
            'Conditions41/40use fastarea withplayer0/terrain0, no actualmap. RealLEVEL_TIME34 usespreparedSessionclock. Othercondition/action branches notcovered.',
            'Three sequentialticks ratherthan completegameplay. No cycles,invalidpointer,NULLarrays,zero-count execution or failingactionbranches tested; no automaticrepair.',
            'OrdinaryAPreturn0 makes508640call unreachable locally; doesnot prove alltriggercountermechanics or versions broken.',
            'APflags04/08,10Cryptgroups,CreatureJump/Priority andother15-familyunknownsremainopen. Goalunfinished.'])

def main():
    s=build();(ROOT/'ap-trigger-tree-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase156-prior-components.json');assert set(p['components']) in [set(old),set(old)|{'ap_trigger_tree_semantics'}]
    for k,row in old.items():assert p['components'][k]==row
    p['ap_trigger_tree_semantics']=s;p['components']['ap_trigger_tree_semantics']=link('ap-trigger-tree-semantics.json')
    note='ap_trigger_tree_semantics phase156 executes originalAPbind and99completeAPticks in33preparedruntime scenes,12independentfullGhidracontrols. Signedlinkdispatch,first-elementlookupfallback,once-onlyinheritedRepeat1 andunreachedAPrepeatdecrementconfirmed; actionownRepeatnotgate. Threeexact3DLevelrawpayloadpairs executeLEVEL_TIME signed32 comparison. AP04/08remainunresolved; noflagreadsselectedpaths.852exactstorageedits109Triggers/106APsources preserveheaders/links/unknowns;2emptyTriggersidentityonly. Rawparser/loader/Worldschedulernotexecuted; nohistoricalschema/effective/APIchanges.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
