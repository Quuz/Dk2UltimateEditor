"""Publish actualCreaturecreation initialWorkModifier versus loadedrawCEE."""
from copy import deepcopy
from pathlib import Path
import json
import emulate_creature_work_initial as native
import crosscheck_creature_work_initial as controls
import prepare_creature_work_initial_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase164-prior-effective-creatures.json'))
    field=next(f for f in out['fields'] if f['offset']==0xCEE)
    field['verified_work_initial']=dict(status='selected_native_creation_uses_literal_instead_of_CEE',raw_offset=0xCEE,MID_offset=0x10DE,definition_offset=0x793,
        native_UI_label='Work Modifier Default',Actor_WorkModifier_offset=0x3AF,Actor_WorkEnhancement_offset=0x3B3,
        initializer=0x4872A0,assignment_PC=0x48746C,initial_WorkModifier_bits=0xCCC,initial_WorkEnhancement_bits=0,
        initial_Q12_factor=3276/4096,scope='Wholeconstructor/free-slotfactory/fullinitializerwithactualrawImp/Vampiredefinitions. RawCEE0/1/4096/8192/FFFFFFFFloaded; initial3AFalwaysliteral3276.',
        global_nonuse_proved=False,remaining_question='Otherpossibleconsumers/versionpathsnotresolved; preserveCEEanditsnativeeditorlabel.',schema='creature-work-initial-semantics.json')
    return out

def build():
    attest=primary.build();assert attest==load('phase164-creature-work-initial-primary.json')
    manifest=load('phase164-creature-work-initial-emulations.json');assert len(manifest['replays'])==8
    traces=[];posts={}
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,path=native.execute(c);post,out=native.verify(c,args,ch,d['result']);posts[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes())
        assert d['input']==args and d['backend_sha256']==sha(Path(native.p.cpu.__file__).read_bytes())
        traces.append(d['result'])
    pairs=native.pairs(posts);assert pairs==manifest['paired_controls'] and len(pairs)==6
    cross=load('phase164-creature-work-initial-crosschecks.json')['replays'];assert len(cross)==4
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert (n,size)==(row['checked_regions'],row['checked_bytes']) and not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    prior=load('phase164-prior-components.json');assert len(prior)==142
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('creature-work-cell-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    assert sha((ROOT/'dk2_formats.py').read_bytes())==load('phase159-terrain-light-storage-validation.json')['codec']['sha256']
    blob=native.p.EXE.read_bytes()
    assert native.p.pe_read(blob,0x48746C,10)==bytes.fromhex('c786af030000cc0c0000')
    assert native.p.pe_read(blob,0x487476,6)==bytes.fromhex('899eb3030000')
    failed_stack='ghidra/phase164-creature-work-initial-probe-c8d2d2d86ddf.json'
    failed_Imp='ghidra/phase164-creature-work-initial-probe-de242bc461f2.json'
    assert not load(failed_stack)['result']['success'] and not load(failed_stack)['result']['visited_instructions']
    bad=load(failed_Imp)['result'];assert bad['success']
    assert any(x['pc']=='0x4c1afb' and x['address']=='0x5c' for x in bad['observed_reads'])
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenarios=8,Creature_raw_loads=visits(0x492320),Keeper4_raw_loads=visits(0x4F7CE0),whole_Creature_constructors=visits(0x4B5E10),
        whole_Creature_factories=visits(0x4B78D0),whole_Creature_initializers=visits(0x4872A0),native_WorkModifier_literal_assignments=visits(0x48746C),
        whole_memory_pairs=len(pairs),pair_compared_regions=sum(r['compared_regions'] for r in pairs),pair_compared_bytes=sum(r['compared_bytes'] for r in pairs),
        independent_full_Ghidra_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        original_code_extents=len(attest['functions']),unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        new_storage_edits=0,inherited_CF2_CEE_exact_container_edits=246,newly_named_opaque_fields=0,excluded_incomplete_fixture_files=2,
        preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=164,date='2026-11-01',families=['Creatures','KeeperSpells'],profile=attest['profile'],primary=attest,
        CEE_binding=dict(raw_offset=0xCEE,MID=0x10DE,definition=0x793,native_UI_label='Work Modifier Default',status='does_not_determine_selected_initial_assignment',
            observation='ActualrawCEEloadsandremainsindefinition; fullnativecreationassignsliteral3276toActor3AFat48746C, then0toActor3B3at487476. No CEE-to-Actorbindingclaimed.',
            global_nonuse_proved=False),
        native_initialization=dict(constructor=0x4B5E10,factory=0x4B78D0,initializer=0x4872A0,WorkModifier_writer=0x48746C,WorkModifier_bits=0xCCC,
            WorkModifier_decimal=3276,Q12_factor=3276/4096,WorkEnhancement_writer=0x487476,WorkEnhancement_bits=0,
            exact_instructions=['MOV DWORD PTR[ESI+3AF],0CCC','MOV DWORD PTR[ESI+3B3],EBX; EBX=0'],
            object='Real1343byteCreature constructed/tag100/registeredandremovedfrompreparedfreeslot; original66D524vtable andcompleteinitialization. NotpreparedActorWorkModifier.',
            source_values=[0,1,4096,8192,0xFFFFFFFF],tested_species=[1,8],CF2_control='RawCF21024loadedinonecase; nolevelup, initial3AFstill3276.'),
        source_and_fixture=dict(raw='ActualstandaloneImp1/Vampire8records5449bytes; CEE/CF2andHP1000diagnosticinmemory. SourceCEE/CF2zeroacross3734records/82containersfrompreservedphase162corpus.',
            World='PreparedWorld/owner7/map32x32/freeCreaturepoolslot andtagtable; nativeconstructorconsumestag100,factory/owner/mapregistrationandwhole4872A0execute.',
            growth='Preparedpercent100/TPS20andrawbaseHP1000, level1,position5.5/6.5/2. ActorWorkModifiernotassignedbyfixture.',
            Keeper4='ActualrawKeeperSpellID4loadedthrough56B990/56BAB0/4F7CE0into122bytedefinition; world1604/1608/160Cpointsoneentry. OriginalWorld110/50DD60 resolvesitforImp4C1AE0manaaccounting.',
            providers='Originalcode/vtablespreserved. Declaredsoundlookup0/playtoken13572468/GFXnull; explicitexternalIsBadReadPtrproviderchecksactualpreparedreadableranges; badrangecounter0.',
            scope='OnlyDXprofile, twoactualspecies,onepreparedowner/free-slot/worldstate. No ordinarylevelbootstrap/AI/scheduler/GPU; noallversions/allcreationpathsclaim.'),
        paired_controls=pairs,runtime_outcomes=[dict(case=r['case'],**r['outcome']) for r in manifest['replays']],
        implications=dict(editor='PreserveCEErawDWORDandnativeUIlabel; annotateitsselectedgameinitializationboundary. SettingCEEisnotconfirmedwaytochangeinitialWorkModifierinthisDXpath.',
            relation_to_163='Phase163explicitlypreparedinitial3AF4096foreasytoreadQ12controls; thisphasefindsactualfactoryinitial3276. Priorwork/levelupformulasanddiagnosticoutcomesremainvalid.',
            remaining='GlobalCEEconsumerstillunresolved; literalinitializationconfirmed. No conclusionthatallCEEreadsinallversionsareunused.'),
        excluded_fixtures=[dict(**link(failed_stack),reason='CPUcannotwriteunpreparedstackbeforefirstinstruction; correctedstackadded. Notgamebug.'),
            dict(**link(failed_Imp),reason='CPUreachedreturnbutstrictlowpageauditrejectsNULL+5Creadat4C1AFB: missingKeeper4catalogue. CorrectedbyactualrawKeeper4load; nevercountedassuccessfulevidence.')],
        inherited_storage_validation=dict(**link('phase162-creature-work-storage-validation.json'),new_edits_in_this_phase=0,
            scope='Prior246exactCF2/CEEwholecontaineredits82sourcesremainfrozen; sourcehashesrechecked. Notnewstoragecount.'),
        native_executions=manifest,independent_Ghidra_controls=cross,preservation=preservation,effective_creature_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['probe_creature_work_initial.py','emulate_creature_work_initial.py','crosscheck_creature_work_initial.py','prepare_creature_work_initial_primary.py']],
        limitations=['Actualconstructor/factory/initializerexecuteonpreparedWorld/map/player/pool; ordinaryWorldbootstrap/memorypoolpopulation/AIcaller/schedulernotexecuted.',
            'OnlyImp1/Vampire8,owner7,onelevel1/free-slotconfiguration. Initial3276literalconfirmedforthispath; noallversions/allspecies/allcreationpathstestingclaim.',
            'RawCEEpreservedandloaded; doesnotdetermineselectedinitialActor3AF. PossibleotherCEEconsumersremainunresolved; do notdeleteordiscardthefield.',
            'Phase163initial4096wasexplicitdiagnosticinput; thisfactoryinitial3276doesnotinvalidateitsworkarithmetic. No newwholefactory-to-levelup-to-jobcompositionexecuted.',
            'Declaredexternalresource/audio/IsBadReadPtrproviders; no actualGPU/assets/sound orWindowsgameexecution.',
            'No newphysicalstorageeditsornewopaque-fieldnames; inherited246CF2/CEEeditsnotcountednew. Prior142schemas/420sources/7EXEpreserved. Other15familyunknownsremainopen.'])

def main():
    s=build();(ROOT/'creature-work-initial-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase164-prior-components.json');key='creature_work_initial_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('creature-work-initial-semantics.json');p['effective_catalogue_layouts']['Creatures']=s['effective_creature_layout']
    note='creature_work_initial_semantics phase164 refinesCEEinitializationboundary:actualrawCreature/Keeper4load ->nativeconstructor4B5E10/free-slotfactory4B78D0/wholeinitializer4872A0 assignsliteral3276at48746CtoActor3AFand0to3B3at487476. CEE0/1/4096/8192/FFFFFFFF/CF21024controls;Imp1/Vampire8;8native/6wholememorypairs/4wholeGhidra196regions394816bytes/115extents5042PC2blocks. CEEdoesnotdetermineselectedinitialvalue,globalnonuseunproved. Phase163prepared4096diagnosticunchanged.2incompletefixturesrejected(stackandImpNULL+5C/missingKeeper4);actualKeeper4loadfixesImp.0newstorageedits,inherited246preserved;prior142schemas/420sources/7EXEpreserved;effectiveCEEaddsverified_work_initialonly.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])

if __name__=='__main__':main()
