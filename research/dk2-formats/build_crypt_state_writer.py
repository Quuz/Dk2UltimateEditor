"""Publish the original runtime-state writer consumer of Crypt counters."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_state_writer as native
import crosscheck_crypt_state_writer as cross
import prepare_crypt_state_writer_primary as primary
import compare_player_runtime_layout as comparison
ROOT=native.ROOT
KEY,SCHEMA='crypt_state_writer_semantics','crypt-state-writer-semantics.json'
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())


def excluded():
    rows=[]
    for file in sorted((ROOT/'ghidra').glob('phase190-crypt-state-writer-*.json')):
        saved=json.loads(file.read_text(encoding='utf-8'))
        if 'case' not in saved:continue
        regions=json.loads(saved['input']['memory'])['regions']
        spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in regions)
        overlaps=[]
        for i,(a,n) in enumerate(spans):
            for b,m in spans[i+1:]:
                if b>=a+n:break
                overlaps.append(dict(first=a,first_bytes=n,second=b,second_bytes=m))
        if overlaps:rows.append(dict(file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),reason='Exploratoryoverlappingoutput/state/logregions;notaccepted.',overlaps=overlaps))
    return rows


def build():
    manifest=load('phase190-crypt-state-writer-emulations.json');assert len(manifest['replays'])==len(native.cases())==6
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,s,file=native.execute(c);post,out=native.verify(c,args,ch,s['result'])
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
        assert native.probe.DATA==0x31400000
        traces.append(s['result'])
    controls=load('phase190-crypt-state-writer-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and row['sha256']==link(row['file'])['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase190-crypt-state-writer-primary.json')
    layouts=comparison.build();assert layouts==load('phase190-player-runtime-layout.json')
    rtti=load('phase190-player-rtti.json');assert rtti['rows'][0]['COLs'][0]['tables'][0]['vtable']==0x66D99C
    assert [r['target'] for r in rtti['rows'][0]['COLs'][0]['tables'][0]['entries']]==[0x4C1FB0,0x4B8D40]
    bad=excluded();assert len(bad)>=12
    accepted={row['file'] for row in manifest['replays']};assert not accepted&{r['file'] for r in bad}
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase190-prior-package-state.json')
    for ref in old['components'].values():assert link(ref['file'])==ref
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=6,independent_Ghidra_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),
                Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),original_CPlayer_constructors=visits(0x4B8450),
                original_CPlayer_writers=visits(0x4B8D40),original_MyFile_writes=visits(0x5B6EC0),original_Disc_writes=visits(0x5FFFF0),
                complete_output_scenes=4,short_or_failed_OS_scenes=2,additional_primary_extents=len(proof['functions']),
                inherited_primary_extents=len(proof['inherited']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
                excluded_exploratory_traces=len(bad),prior_components_preserved=167,source_files_preserved=420,EXE_paths_preserved=7,
                new_raw_fields=0,source_writes=0,Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,
                Crypt_counter_meanings_remaining=10)
    return dict(phase=190,status='verified_original_CPlayer_writer_copies_Crypt_runtime_state',counts=counts,
                profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
                format_scope='RuntimeCPlayerstreamfragmentonly;notanewKWDfieldorcompleteSAVformat. Existing18Cryptgroupsplus2unassignedtailbytesarepreserved.',
                owner_type=dict(RTTI_name='.?AVCPlayer@@',type_descriptor=0x69F330,COL=0x67D900,vtable=0x66D99C,
                                destructor=0x4C1FB0,writer=0x4B8D40,stride=0x1125,
                                proof='OriginalRTTI/vtableandfull4B8450constructor;primarytablehasdeletingdestructorandwriter,notadditionalCryptgetters.'),
                native_chain=dict(writer=0x4B8D40,base_tag_writer=0x5079F0,write_wrapper=0x5B3EE0,MyFile_slot_C=0x5B6EC0,
                                  Disc_slot_8=0x5FFFF0,WriteFile_IAT=0x66C1D0,external_WriteFile_target=0x28CF36,
                                  IO_scope='OriginalIATandstreamvtables/methodsretained. Onlyoutside-imageWindowsWriteFileendpointcopiesintoresearchmemory.'),
                fixed_state_block=dict(runtime_offset=0x76,bytes=0x1028,end_exclusive=0x109E,stream_fragment_offset=3,
                                       runtime_counter_span=dict(offset=0x108A,bytes=20,mapped_species_group_bytes=18,unassigned_tail_bytes=2),
                                       counter_stream_fragment_offset=0x1017,
                                       offset_scope='OffsetsrelativeonlytothisstandaloneCPlayerfragmentstartingatstream0;notabsoluteoffsetsinKWDorwholeSAV.'),
                outcomes=dict(byte_preservation='All20bytesincludingdiagnostic80/FFpreservedwithoutnormalizationin4complete-outputscenes. Suchbitsarenotclaimednaturallyreachablecountervalues.',
                              ordinary_output_bytes=4184,one_array_entry_output_bytes=4192,
                              short_17_bytes=65,OS_failure_bytes=0,
                              return_scope='CPlayerwriterreturns1inall6scenes,includingshort/failedexternalwrites. Returnaloneisnotproofacompletestreamwaswritten;outergamesaveframeworknotexecuted.'),
                source_preservation='Constructor/bareCPlayerconditionsandone9-bytearrayentryareexplicit. Observationcopyproveswritercannotalterthepreparedsourceobject;guard/IAT/vtablechecksandfullydisjointregionspass.',
                fixture=dict(bare_constructor='NoWorld/playerconfigbootstrap;tag1andoriginalembedded506120subobjectconstructedbygamecode.',
                             optional_controllers='Absent;onlyoneexplicit9-byteowned-arrayentrycontrol. Otherarrays/optionalcontrollersnotfullyexercised.',
                             corrected_map='Output31400000..31420040;IOstate/logoutsideoutput. BothnativeandGhidrabuildersassertdisjointregions.',
                             prior_failures='Old31040000outputoverlappedstate/logandfailedfullguards;allretainedandexcluded.ExtraarrayIOsourcerangewasalsomissinginanearlierexploration.'),
                native_executions=manifest,independent_Ghidra_controls=controls,primary=proof,RTTI_evidence=link('phase190-player-rtti.json'),
                version_layout_comparison=link('phase190-player-runtime-layout.json'),older_version_scope=layouts['old_version_scope'],
                excluded_exploratory=bad,preservation=link('phase165-source-baseline.json'),
                helpers=[link(n) for n in ['probe_crypt_state_writer.py','native_file_write_api_fixture.py','emulate_crypt_state_writer.py',
                                          'crosscheck_crypt_state_writer.py','prepare_crypt_state_writer_primary.py','compare_player_runtime_layout.py',
                                          'inventory_player_rtti.py','native_unicorn_mapreader.py','native_file_api_fixture.py']],
                priority=dict(current='Crypt groups',block_complete=False,next=['Actualgameplayconsumer/meaningof10unassignedCryptcounters.',
                              'Pass-pointerbulk/returncontextsnotcoveredbyearlierforwardsearch;do notinferunusedfromabsence.',
                              'OthernaturalAIinterruptionsandfullWorldlifetime;globalsremainnextuser-priorityblock.']),
                limitations=['Noconclusionthatremaining10countersgrantnewpassivebonuses;generalstate-serializationconsumeronly.',
                             'Twoextra20-byte-tailentriesarenotnewCryptspeciesgroups;18registrationgroupsremain.',
                             'Olderusercopyhasdifferentstate-blocksize/stride;noDXoffsettransferoroldCryptmappingproved.',
                             'WholeSAVenvelope/readback/optionalcontrollers/fullsaveframeworknotexecuted;notasavegameeditor.',
                             'NoKWDcodec/APIorparalleleditorchanges;broader15familygoalstillactive.'])


NOTE='crypt_state_writer_semantics phase190:6native/6Ghidra;originalCPlayer/MyFile/Discwriterpreserves20runtimecounterbytesinside76..109Dblock. 18mappedgroupsplus2unassignedtailbytes;notnewKWDfields. Shorts/failuresreturn1with65/0outputbytes,outerframeworkunproved. Fullguardchecksnowpasswithdisjoint31400000output;oldoverlaptracesexcluded. 10new+7inheritedspans/562PC;167priorcomponents/source/codec preserved;10gameplaymeaningsopen.'


def main():
    package=load('editor-format-package.json');file=ROOT/'phase190-prior-package-state.json'
    if not file.exists():
        assert len(package['components'])==167
        dump(file.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
                           preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(file.name);schema=build();dump(SCHEMA,schema)
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    for k,v in old['components'].items():assert package['components'][k]==v
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase190-search-status.json',dict(phase=190,goal_status='active',current_block='Crypt groups',block_complete=False,
         previous_goal_turn=dict(classification='progress',reason='Read-onlyauditcaughtoverlappingfixturebuffersandrejectedoldtraces;oldCPlayerRTTI/layoutanchorschangedthenextaction.'),
         current_turn=dict(classification='progress',reason='Afterquota-resetthefixturewasrepaired;6native/6Ghidra/fullguardsand562PEPCattestationpublishedruntimecounterserialization.'),
         counts=schema['counts'],remaining=schema['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))


if __name__=='__main__':main()
