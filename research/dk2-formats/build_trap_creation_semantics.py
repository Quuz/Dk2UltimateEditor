"""Publish complete raw Trap creation/activation/update with scoped unknown98."""
import hashlib
import json
from pathlib import Path
import emulate_raw_trap_factory as native
import crosscheck_trap_creation as cross
import prepare_raw_trap_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def build():
    manifest=load('phase119-raw-trap-emulations.json')
    assert manifest['scope']==native.__doc__ and len(manifest['replays'])==len(native.cases())==23
    assert len(manifest['paired_full_context_cases'])==12 and manifest['measured_baselines']==6
    post={}
    for case,row in zip(native.cases(),manifest['replays']):
        args,checks,expected=native.probe.build(**native.inputs(case));path=native.path_for(args)
        saved=json.loads(path.read_text(encoding='utf8'))
        assert saved['input']==args and saved['case']==case and saved['expected']==expected
        assert row['case']==case['name'] and row['file']==path.name and row['sha256']==sha(path.read_bytes())
        post[case['name']],outcome=native.verify(case,args,checks,expected,saved['result'])
        assert outcome==row['outcome']
    for case in native.cases():
        if not case['name'].startswith('trap') or case['unknown']==0:continue
        name=f'trap{case["ident"]}-'+('active-tick' if case['activation'] else 'inactive-tick' if case['tick'] else 'create')+'-0'
        wanted={a:bytearray(b) for a,b in post[name].items()}
        _,_,expected=native.probe.build(**native.inputs(case))
        for a,k in [(native.probe.RAW,'raw'),(native.probe.MID,'mid'),(native.probe.f.DEFINITION,'definition')]:
            blob=bytes.fromhex(expected[k]);wanted[a][:len(blob)]=blob
        assert post[case['name']]==wanted
    controls=load('phase119-trap-crosschecks.json');assert len(controls['replays'])==3
    for name,row in zip(cross.CONTROLS,controls['replays']):
        args,count,size=cross.build(name);path=ROOT/'ghidra'/row['file'];value=json.loads(path.read_text(encoding='utf8'))
        assert row['case']==name and value['input']==args and row['sha256']==sha(path.read_bytes())
        assert row['checked_regions']==count and row['checked_bytes']==size
        r=value['result'];assert not value['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    assert load('phase119-primary-manifest.json')==primary.build()
    previous=ROOT/'phase119-prior-components.json'
    if not previous.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==97
        previous.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==97
    sources=load('trap-loading-semantics.json')['corpus']['files'];assert len(sources)==82
    for row in sources:assert sha(Path(row['file']).read_bytes())==row['sha256']
    return dict(phase=119,date='2026-10-29',family='Raw Traps definition to complete instance creation,activation andtick',
        executable=str(primary.EXE),executable_sha256=sha(primary.EXE.read_bytes()),
        source=dict(file=str(native.probe.SOURCE),sha256=sha(native.probe.SOURCE.read_bytes())),
        primary=link('phase119-primary-manifest.json'),native_replays=manifest['replays'],
        independent_Ghidra_controls=controls['replays'],
        sizes=dict(raw_record=579,intermediate=747,definition=209,instance_allocation_request=291,
            instance_allocation_hex='123h',pool_bytes_with_header=304,pool_payload_alignment=4,
            warning='Older287byteCTrap symbol hint is not the actual factory allocation size in suppliedDX. This is runtime memory,not a new KWD stride.'),
        pipeline=dict(definition_constructor=0x4AA330,intermediate_initializer=0x570B60,converter=0x570D10,
            loader=0x4AA440,factory=0x4B7C10,instance_constructor=0x4B7400,instance_init=0x4A7940,
            activation=0x4A96D0,tick=0x4A84E0,owner_upkeep=0x4C1B60,
            note='Doorloader4A7100 is a separate type; it is not used in this rawTrap chain.'),
        unknown204=dict(raw=0x204,width=2,intermediate=0x2AC,presence=0x5C,definition=0x98,
            values=[0,0xFFF6,0xFFFF],status='Unresolved gameplay meaning',
            observation='Preserved16bits through actualloader; no definition98read in selected creation/activation/update chains. Twelve paired complete contexts differonly inraw/mid/definition/checksum.',
            checksum='Withgate0 its bytes participate in the unsigned intermediatebyte sum atdefinition6C. This is not a damage/mana consumer or universal file checksum.',
            scope='GasTrap1 andTriggerTrap9,preparedowner/map,oneupdate andno qualifying target. No globalunused claim.'),
        confirmed_inputs=dict(health=dict(raw=0x1F0,width=2,definition=0xA2,instance=0x40,
            result='Raw1234 producesHP1234 throughload/create/activation/tick.'),
            upkeep=dict(raw=0x202,width=2,definition=0x96,game_read='signed16',
                owner_expense=0x10D6,owner_income=0x10E2,
                result='Activation adds signedupkeep toexpense andsubtracts itincome. Default5 gives+5/-5;13 gives+13/-13;FFFF gives-1/+1.',
                boundary='Aggregatecounter updates,not actualearned/spentmana or rate measuredinliveWorld. Unknown204 is separate.')),
        activation=dict(instance_flags=0x10C,clear_mask=0x20,
            neighbour_helper_by_trigger={'3':0x4A9720,'other':0x4A9A00},
            owner_accounting='Calls original4C1B60 once. Newlycreated butunactivated instances do not add thisupkeep.'),
        cell_owner=dict(runtime_cell_owner_offset=8,owner_mask=0xFFF,
            handler=0x4A84E0,result='If low12cellowner differsfrominstanceowner,selectedtick invokes originalHP0 setter.',
            tested='Owner7 oncell7 retains3000HP;owner7 oncell0 becomesHP0. Entitytag/pointer remainregistered;Worlddeferredremoval not executed.',
            raw_map_warning='This is16byte runtimecell,+8 is not a raw AutoSaveMap.kld celloffset.'),
        constructor_state=dict(seed=0xA5,
            untouched_definition_spans=[dict(offset=0x6C,length=4,condition='Retained onlywhengate1skipschecksum.'),dict(offset=0x9E,length=2,condition='Retained byconstructor andselectedloader.')],
            policy='Runtimeunassigned bytes do not authorize normalizing unknown filebytes.'),
        fixture=dict(types=[dict(ident=1,name='Gas Trap',trigger=2),dict(ident=9,name='Trigger Trap',trigger=3)],
            owner=7,position=[0x5800,0x6800,0x2000],ticks_per_second=20,
            map='Prepared32x32 runtime map;centerowner7 exceptexplicit mismatch case.',
            resources='Nullvisualprovider duringdefinitionload;inheritedexternalnullGFX/sound providers andoriginal4430D0 effectrequest. No actual assetloaded.',
            no_qualifying_target=True,readiness_handler_calls=0,
            natural_raw_Things_placement_executed=False,World_scheduler_executed=False),
        verification=dict(native_chains=23,raw_loaders=23,creations=23,activations=11,full_ticks=17,
            paired_full_context_cases=manifest['paired_full_context_cases'],measured_opaque_baselines=6,
            full_intermediate_and_definition_compared=True,source_raw_and64byteguards_preserved=True,
            independent_Ghidra_CPU_controls=3,Ghidra_context_regions=99,Ghidra_context_bytes=232989,
            key_whole_function_ranges=13,fresh_Ghidra_function_ranges=8,data_tables=3,additional_PE_helper_bytes=31,
            scope='All23selected fullchains return;12unknownvariants comparecompletepreparedcontexts. Otherfivepositivecontrols checkedsemantically andcaptured;negativeupkeep additionallyGhidracrosschecked. Not independentdecoding ofallopaque state.'),
        helpers=[link(n) for n in ['probe_raw_trap_factory.py','emulate_raw_trap_factory.py','crosscheck_trap_creation.py','prepare_raw_trap_primary.py']],
        dependencies=[link(n) for n in ['probe_trap_factory.py','emulate_object_factory.py','native_trap_loading.py','native_unicorn_factorytrace.py']],
        source_corpus=dict(files=sources,records=1293,new_container_rebuilds=0,
            explanation='No rawcodec orpatchAPIchange;previousphase61 containeredits remain historical and are not recounted.'),
        source_executables_unchanged=source_executables(),
        limitations=['Exact suppliedDX only;nototherEXEs/Flameruntimepatches.',
            'PreparedWorld/pool/map/owner withnullresources;not naturalplacement,fullCWorldstartup orlivegame.',
            'Tick has no qualifyingtarget anddoesnot reachreadiness orfire. Hits,collision,othertrigger conditions andlaterstates remainopen.',
            'HP0doesnotexecuteWorlddeferredremoval here;no globaltraplifecycle claim.',
            'Primarymanifest covers13keywholefunctions/3tables,notnewlyeverytransitivehelper orallbranches.',
            'Newunknownfieldnames0;Traps204remainsunresolved. Significant15familyquestions persist.',
            'EarlydiscoveryNULLGFX90/mismatchedcell artifacts areexcludedfrom23acceptedcases. Correctedscene usesoriginalnullcallback andexplicitowner7;notagamebugclaim.'])


if __name__=='__main__':
    result=build();(ROOT/'trap-creation-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('23rawTrapchains/17ticks/11activations/12wholecontextpairs/3Ghidracontrols;unknown204open.')
