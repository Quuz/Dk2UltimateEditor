"""Publish the selected rawCF2 -> level-up -> nativeworkcallback -> TerrainHP chain."""
from copy import deepcopy
from pathlib import Path
import json
import emulate_creature_work_cell as native
import crosscheck_creature_work_cell as controls
import prepare_creature_work_cell_primary as primary
import validate_creature_work_actions as storage
import native_creature_work_cell as api

ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase163-prior-effective-creatures.json'))
    field=next(f for f in out['fields'] if f['offset']==0xCF2)
    field['verified_cell_work']=dict(status='confirmed_selected_work_consumer',callback=0x496E00,
        terrain_HP_callback=0x452F60,WorkModifier_offset=0x3AF,WorkEnhancement_offset=0x3B3,
        representation='Q12 sum of current ActorWorkModifier and ActorWorkEnhancement; signed64 multiply, low32 SHRD12 result, then signed SAR12.',
        source_path='ActualrawCF2load -> optionalwhole48B120level-up -> whole496E00 -> originalCMap4508D0 -> whole452F60 changesRockHP.',
        scope='Selectednonpossessed,nonlethalRockpath. Inputs3B3/initial3AFprepared; no newCryptactivation/CEEinitialconsumer, noalljobsclaim.',
        schema='creature-work-cell-semantics.json')
    return out

def build():
    attest=primary.build();assert attest==load('phase163-creature-work-cell-primary.json')
    manifest=load('phase163-creature-work-cell-emulations.json');assert len(manifest['replays'])==13
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,path=native.execute(c);_,out=native.verify(c,args,ch,d['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
        assert d['input']==args and d['backend_sha256']==sha(Path(native.p.cpu.__file__).read_bytes())
        typ=out['action_type'];index=[8,9,10].index(typ);raw=ch[native.p.VRAW+4096*index][16:32]
        projection=api.delta(native.u(raw,4),20,out['modifier'],out['enhancement'],non_imp=c.get('species',1)!=1)
        assert projection['signed_HP_delta']==out['delta'] and projection['action_per_tick']==out['action_per_tick']
        assert projection['combined_factor_bits']==out['factor']&0xFFFFFFFF
        traces.append(d['result'])
    cross=load('phase163-creature-work-cell-crosschecks.json')['replays'];assert len(cross)==8
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert (n,size)==(row['checked_regions'],row['checked_bytes']) and not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase163-creature-work-action-storage.json')
    prior=load('phase163-prior-components.json');assert len(prior)==141
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('creature-work-level-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    assert sha((ROOT/'dk2_formats.py').read_bytes())==load('phase159-terrain-light-storage-validation.json')['codec']['sha256']
    variables=[dict(type=t,baseline_name=load('variable-runtime-map.json')['rows'][str(t)]['baseline_name'],raw_value_offset=4,
        handler=0x51CB80,runtime_address=a,raw_value_signed=v,TPS=20,action_per_tick=native.tz(v,20))
        for t,a,v in [(8,0x6F2564,-5000),(9,0x6F2584,-130),(10,0x6F2588,-8)]]
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenarios=13,Creature_raw_loads=visits(0x492320),Terrain_raw_conversions=visits(0x56ED20),Terrain_definition_loads=visits(0x4F66E0),
        raw_variable_callbacks=visits(0x51CB80),complete_levelup_calls=visits(0x48B120),eligible_levelups=visits(0x48B330),
        whole_work_callbacks=visits(0x496E00),CMap_work_calls=visits(0x4508D0),whole_Terrain_HP_calls=visits(0x452F60),
        Terrain_change_calls=visits(0x44FC40),independent_full_Ghidra_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),
        Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),original_code_extents=len(attest['functions']),static_only_function_extents=0,
        unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        exact_changed_container_rebuilds=serial['exact_container_edits'],action_storage_source_files=serial['file_count'],
        source_action_records=sum(serial['records_by_type'].values()),numeric_projection_controls=13,newly_named_opaque_fields=0,
        preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=163,date='2026-11-01',families=['Creatures','Terrain','GlobalVariables'],profile=attest['profile'],primary=attest,
        field_binding=dict(raw_CF2=0xCF2,MID=0x10E2,definition=0x797,levelup_callback=0x48B120,Actor_WorkModifier=0x3AF,
            Actor_WorkEnhancement=0x3B3,work_callback=0x496E00,CMap_callback=0x4508D0,Terrain_HP_callback=0x452F60,
            semantics='RawCF2 is added to ActorWorkModifier on eligiblelevelup (phase162). Thisphase executes the selected workconsumer and actualnonlethalRockHPchange.'),
        variables=variables,
        owner_selection=dict(species1='Targetlow12owner==Actorowner7 ->type9; targetowner==neutral8 ->type8; otherwise ->type10.',
            non_Imp='Selectedspecies8/global75AE93=0 uses6*type9amount; owner7tested. PreparedAIcallbackdoesnotestablishVampirejobeligibility.',
            passed_owner='Originalcallback passes Actorownerlow16=7. Rawu32argumenthigh16cancontainstale registerbits; onlylow16hasownertagmeaninginthiscontract.'),
        arithmetic=dict(action='trunc_toward_zero(signed32(rawVariablesValue)/TPS); TPS20prepared, original51CB80executes.',
            factor='signed32(low32(Actor3AF+Actor3B3))',base='signed32(low32(action_per_tick*(6ifnonImpelse1)<<12))',
            multiplication='q=signed32(low32((signed64(base)*signed64(factor))>>12)); HPdelta=q>>12(arithmetic).',
            rounding='LastSAR12floorsnegativevalues. Example-6*1.25 gives-8, not-7. SignedTPSdivision-130/20truncatesto-6first.',
            HP_application='Actual452F60 executes; negativeHPdelta uses low16magnitude forsubtraction, positive useslow16additioncapped32000. Selectedresultsstrictlybetween0andRockmax660; no destruction/typechange.'),
        source_and_fixture=dict(Creature='ActualstandaloneImpID1orVampireID8raw5449; diagnosticCF2/CEE4096/HP1000 editsinmemory. No sourcewrites; all3734corpusCEE/CF2pairszero.',
            Terrain='ActualrawRockID2loadedthrough56EB60/56ED20/4F66E0. Prepared256*460table/cache; unselecteddefinitionszeroexceptIDbyte. No newTerrainconstructor4F6570/fullWorldbootstrap.',
            map='Prepared32*32*16bytes; Rocktarget(6,6),HP600,owner7/8/9, belowloadedmax660. Actor(5.5,6.5),target(6.5,6.5). Whole16384bytemapafterequalsinputexceptexpectedHPWORD+6.',
            actors='PreparedActorfrompriornativefactorysnapshot,original66D524vtable;World66E3EC/CMap66D3E4. InitialWorkModifier4096orselected0/-4096; WorkEnhancement0or205preparedseparately.',
            enhancement='205isdiagnosticpreparedActor3B3, matchingpriorWizard/Cryptfinding. No newCryptsetter/tick orwholeCrypt-to-workcompositionexecuted.',
            variables='Threeoriginalraw16byteVariables8/9/10recordsinside32bytehandlercontexts, oneoriginalcallbackeach. Diagnostic-minus100casepatchesall3values. Postruntimeglobalscheckedagainstindependentsigneddivision.',
            providers='DeclaredresourceproviderdeclinesandzerosoutWORD; soundlookup0/playtoken13572468; originalWorldlevelupvisualforwarderexecutesbutexternalengine90declines. NoGPU/assets/audio.'),
        runtime_outcomes=[dict(case=r['case'],**r['outcome']) for r in manifest['replays']],
        verification=dict(map_oracle='Wholemap comparison; onlyexpectedHPWORDchanges. RawCreature/Terrain/Variables andselectedguards unchanged. LowpageaccessonlyoriginalSEHFS:[0].',
            Ghidra='8wholeindependentemulationscompareall63selectedmemoryregionsincludingoriginalvtablesandcapturedrootreturn; nohelper-onlysubstitution.',
            callbacks='All13workcallsreachwhole452F60once. AL0whileselectedRockremainsdiggable; no44FC40terrainchange.',
            levelup='12complete48B120calls,11eligibleandonelevel10gate;1caseomitslevelup. EligiblecaseCreatureHPrequest+500separatefromtargetTerrainHP.',
            attestation='87wholefunctionextents/4650executedPCs/3originalvtableblocks. Entrytargetsfromactualcalls/vtables orpriorprovenconstructorcallbackPUSHarguments, nointeriorguesses.'),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,
        effective_creature_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['probe_creature_work_cell.py','emulate_creature_work_cell.py','crosscheck_creature_work_cell.py','prepare_creature_work_cell_primary.py','native_creature_work_cell.py','validate_creature_work_actions.py']],
        limitations=['SelectednonpossessedcallbackonpreparedActor/World/map, no ordinaryAI/animationcaller/factory/bootstrapnewlyexecuted. NonImpdiagnosticnotproofnormalAIworkpermission.',
            'OnlynonlethalRockHPchanges; actualtiledeletion/typechange/NAV/destructionpathsnotexecutedhere. Exactpositive/negativeextremevaluecontractnotvalidatedbythese13cases.',
            'InitialActor3AFand3B3prepared. CEEinitialgameplayconsumerstillunknown; no newCryptactivation. AllsourceCF2zero; growthcasesdiagnostic.',
            'Thisworkconsumerdoesnotestablishuniversalresearch/manufacturing/training/jobbonuses. Possessedworkbranchnotexecuted; nofullpossessionmultiplierclaim.',
            'Numericprojectionvalidatedagainst13selectednativecases; storagevalidationonly3exactvalueeditsin1actualsourcewiththeseVariabletypes. No new246CF2editscounted.',
            'Prior141schemas/physicalcodec/420sources/7EXEpreserved. No newopaque-fieldnames; CF2consumerstrengthened. EF0/Jump/Priority/otherCryptgroupsand15familyunknownsremainopen.'])

def main():
    s=build();(ROOT/'creature-work-cell-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase163-prior-components.json');key='creature_work_cell_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('creature-work-cell-semantics.json');p['effective_catalogue_layouts']['Creatures']=s['effective_creature_layout']
    note='creature_work_cell_semantics phase163 supersedes phase162job_consumer_boundary onlyforselected496E00nonpossessedworkpath:actualrawCF2load/levelup ->Actor3AF+3B3Q12 ->originalCMap4508D0 ->whole452F60RockHP.13native/13Creature/13Terrain/39rawVariablecallbacks/12levelupcalls11eligible;8wholeGhidra/504regions/1750840bytes;87extents/4650PC/3blocks. Variables8/9/10selection/negativeTPSdivision/SARroundingandseparateWorkEnhancement205confirmed; initial3AF/3B3prepared,CEEinitialconsumer/Cryptactivation/alljobsnotproved.3exactVariablesvalueedits1source,prior141schemas/420sources/7EXEpreserved;effectiveCreaturesaddsverified_cell_workonly.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])

if __name__=='__main__':main()
