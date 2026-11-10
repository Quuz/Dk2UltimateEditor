"""Publish native Trap unregister/accounting/free with corrected real pool init."""
from pathlib import Path
import json
import hashlib
import emulate_trap_removal as native
import crosscheck_trap_removal as cross
import prepare_trap_removal_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def build():
    manifest=load('phase120-trap-removal-emulations.json')
    assert manifest['scope']==native.__doc__ and len(manifest['replays'])==len(native.cases())==17
    assert len(manifest['paired_full_context_cases'])==8 and manifest['measured_opaque_baselines']==4
    post={}
    for case,row in zip(native.cases(),manifest['replays']):
        args,checks,expected=native.probe.build(**native.inputs(case));path=native.path_for(args)
        value=json.loads(path.read_text(encoding='utf8'))
        assert value['input']==args and value['case']==case and value['expected']==expected
        assert row['case']==case['name'] and row['file']==path.name and row['sha256']==sha(path.read_bytes())
        post[case['name']],outcome=native.verify(case,args,checks,expected,value['result'])
        assert outcome==row['outcome']
    for case in native.cases():
        if not case['name'].startswith('trap') or case['unknown']==0:continue
        base=f'trap{case["ident"]}-'+('active' if case['active'] else 'inactive')+'-0'
        wanted={a:bytearray(b) for a,b in post[base].items()}
        _,_,expected=native.probe.build(**native.inputs(case))
        for a,k in [(native.probe.creation.RAW,'raw'),(native.probe.creation.MID,'mid'),(native.probe.f.DEFINITION,'definition')]:
            blob=bytes.fromhex(expected[k]);wanted[a][:len(blob)]=blob
        assert post[case['name']]==wanted
    controls=load('phase120-trap-removal-crosschecks.json');assert len(controls['replays'])==3
    for name,row in zip(cross.CONTROLS,controls['replays']):
        args,count,size=cross.build(name);path=ROOT/'ghidra'/row['file'];value=json.loads(path.read_text(encoding='utf8'))
        assert row['case']==name and value['input']==args and row['sha256']==sha(path.read_bytes())
        assert row['checked_regions']==count and row['checked_bytes']==size
        r=value['result'];assert not value['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    assert load('phase120-primary-manifest.json')==primary.build()
    previous=ROOT/'phase120-prior-components.json'
    if not previous.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==98
        previous.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==98
    rejected='ghidra/phase120-trap-removal-discovery-8dda700cb60d.json'
    old=load(rejected)['result'];assert any(int(r['address'],16)==4 and int(r['pc'],16)==0x554BF7 for r in old['observed_reads'])
    try:native.verify_low_page_reads(old)
    except AssertionError:pass
    else:raise AssertionError('UnacceptedNULL+4fixture wasaccepted')
    sources=load('trap-loading-semantics.json')['corpus']['files'];assert len(sources)==82
    for row in sources:assert sha(Path(row['file']).read_bytes())==row['sha256']
    return dict(phase=120,date='2026-10-29',family='Trap cleanup,owner accounting,unregister andnative pool release',
        executable=str(primary.EXE),executable_sha256=sha(primary.EXE.read_bytes()),
        primary=link('phase120-primary-manifest.json'),native_replays=manifest['replays'],
        independent_Ghidra_controls=controls['replays'],
        sequence=dict(raw_loader=0x4AA440,pool_initialize=0x554AD0,create=0x4B7C10,
            optional_activation=0x4A96D0,manager_remove=0x4B7C90,cleanup=0x4A7B20,
            deleting_destructor=0x4B7470,destructor=0x4B7490,owner_detach=0x4B4D40,
            cell_detach=0x4B4F60,tag_thunk=0x4B2910,tag_target=0x4B4AB0,pool_release=0x554BD0,
            boundary='Manager removal explicitly invoked after creation/activation. FullWorld deferred-deletion scheduler isnot executed.'),
        corrected_fixture=dict(arena_bytes=536,usable_initial_free_block=512,sentinel_bytes=24,guard_bytes=64,
            native_initialize='554AD0 executes on preparedWorld allocator withrealcontiguousarena andtwooriginalsentinels.',
            result='After291-byteinstance allocation anddestruction,304allocatedpoolbytes coalescewith208remainingbytes into512.',
            rejected_discovery=link(rejected),rejection='Earliercreation-onlypoolfixture lackedallocator+24 lowsentinel;free pathreadNULL+4 at554BF7. Excludedfromall17acceptedcases.',
            prior_scope='Phase119creation-onlyresults remain scoped to their testedallocations/ticks. Do notuse thatfixture asproof of free().'),
        unregistration=dict(entity_tag=100,kind=3,owner=7,
            observed='Globaltagpointer andvalidity clear;owner/kind/celllists clear;WorldTrapcount/ownerTrapcount decrement;usedbytes0.',
            allocation_gate='4094->4093 duringcreate->4094afterremove.',next_tag='100->101;deletion doesnotrewindnexttag.',
            memory='Releasedpayload isnot erased. Capturedfreedbytes arehost-sideauditonly;no game method iscalledonthereleasedinstance.'),
        upkeep_refund=dict(raw=0x202,definition=0x96,read='signed16',handler=0x4C1B90,
            expense_counter=0x10D6,income_counter=0x10E2,
            arithmetic='ForactiveTrap andzeroWorldgate:expense-=upkeep;income+=upkeep. Combinedactivate/remove restoresbothinitial0.',
            tested=[5,13,-1],inactive='Inactiveinstance didnotaddexpense;cleanupdoesnotcallrefund.',
            boundary='Counteraccountingonly;no real-time mana payout orcompleteowner rebuild executed.'),
        World_gate=dict(offset=0x723B,width=4,getter=0x509850,virtual_slot=0x4C,
            condition='4A7B20 skipsupkeeprefund andselectedtrigger-neighbourcleanup whengetterreturnsnonzero.',
            tested='ArtificialWorldgate1 withactiveTrap:tag/poolreleasecomplete,butexpense5/income-5remain;refundcall0.',
            separate_from_definition_gate='Global6FCDCF gatesdefinitionchecksum/soundloading. Its1testwithWorldgate0stillrefundsordinaryupkeep.',
            meaning='Numericcleanupgate established;global purpose/legitimate setting conditions remainunresolved. No gamebug inferred.'),
        unknown204=dict(raw=0x204,definition=0x98,width=2,values=[0,0xFFF6,0xFFFF],
            observation='No definition98reads inselectedwholeload/create/optionalactivation/remove/destruction/free chains. Eightpairedcompletecontexts differonlyraw/mid/definition/checksum.',
            status='Gameplaymeaningstillunresolved. These cases donotproveglobalunused orjustifyManaGen label.'),
        death_case='Cellowner0->originalTrap tick setsHP0;subsequentexplicitmanagerremove actually unregisters/frees andrefunds. Scheduler selection stillnotexecuted.',
        audio='Externalnullsoundprovider slot38 receivesoriginaltwo-argumentrelease call andreturns0/RET8;onecountedreleasepercase. No realaudiohandle ororiginalgameplayfunction replaced.',
        verification=dict(native_full_cycles=17,pool_initializers=17,creations=17,removals=17,
            activations=11,full_ticks=1,paired_full_context_cases=manifest['paired_full_context_cases'],measured_opaque_baselines=4,
            independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls['replays']),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls['replays']),
            key_function_ranges=27,newly_queried_function_ranges=14,data_tables=4,
            low_page='Allacceptedreads onlyFS:[0]4bytes;oldNULL+4artifact explicitlyrejected.',
            scope='12coreunknowncases have4measuredbases/8fullpairedvariants. Fiveadditionalcontrols checkedsemantically/captured;Worldgate additionallyGhidrachecked. Not everyopaque byte independentlydecoded.'),
        helpers=[link(n) for n in ['probe_trap_removal.py','emulate_trap_removal.py','crosscheck_trap_removal.py','prepare_trap_removal_primary.py']],
        dependencies=[link(n) for n in ['probe_raw_trap_factory.py','probe_trap_factory.py','native_unicorn_factorytrace.py','trap-creation-semantics.json']],
        source_corpus=dict(files=sources,records=1293,new_container_rebuilds=0,
            reason='No codec/publicrawpatchAPIchange. Previouscontainerpatchchecksremainhistorical,notrecounted.'),
        source_executables_unchanged=source_executables(),
        limitations=['ExactsuppliedDX,preparedWorld/owner/maps/nullresources;nootherEXEs/Flameruntimepatches newlytested.',
            'Explicitmanagerremove,notfullWorldscheduler orautomaticdeletionselection.',
            'OnlyGasTrap1/TriggerTrap9 withnoothertraps/qualifyingtargets;completegeometry/combat/sound andliveGUI notexecuted.',
            'World723B numericgateproofdoesnotidentifyitsglobalpurpose orproveanaccountingleak inordinaryplay.',
            'Primarymanifest27keyranges/4tables,notalltransitivehelpers/allbranches newlyattested.',
            'No newmeaningforunknown204;othermajor15familyunknowns remainopen.'])


if __name__=='__main__':
    value=build();(ROOT/'trap-removal-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('17nativefullcycles/8wholepairs/3Ghidrachecks;realpoolrestored,unknown204stillopen.')
