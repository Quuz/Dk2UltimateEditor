"""Publish a verified meaning for existing Variables233, preserving prior work."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_conversion_score as native
import crosscheck_conversion_score as cross
import prepare_conversion_score_primary as primary
import validate_conversion_score_storage as storage
ROOT=Path(__file__).resolve().parent
KEY,SCHEMA='conversion_score_semantics','conversion-score-semantics.json'
sha,load,dump=native.sha,native.load,native.dump
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda v:sha(json.dumps(v,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['probe_conversion_score.py','native_conversion_score.py','emulate_conversion_score.py',
         'crosscheck_conversion_score.py','prepare_conversion_score_primary.py','validate_conversion_score_storage.py',
         'build_conversion_score.py','native_unicorn_mapreader.py','emulate_temple_creature_entry.py']


def build():
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase192-prior-package-state.json');assert len(old['components'])==169
    for ref in old['components'].values():assert link(ref['file'])==ref
    manifest=load('phase192-conversion-emulations.json');assert len(manifest['replays'])==len(native.cases())==16
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,saved,file=native.execute(c);post,out=native.verify(c,args,ch,saved['result'])
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
        traces.append(saved['result'])
    controls=load('phase192-conversion-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and link(row['file'])['sha256']==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof is not None and proof==load('phase192-conversion-primary.json')
    physical=storage.build();assert physical==load('phase192-conversion-storage.json')
    runtime=load('variable-runtime-map.json')['rows']['233']
    assert runtime['runtime_address']=='0x6f29e9' and runtime['runtime_bytes']==4 and runtime['transform']=='copy_low_bits'
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=16,independent_Ghidra_controls=6,
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        source_raw_Creature_loads=visits(0x492320),raw_233_callbacks=visits(0x51CD90),
        complete_conversion_calls=visits(0x4F2800),original_owner_setters=visits(0x48D050),
        original_score_updates=visits(0x511650),score_gate_rejections=16-visits(0x511650),
        new_complete_function_extents=len(proof['functions']),inherited_extents=len(proof['inherited']),
        additional_executed_byte_blocks=len(proof['executed_blocks']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        exact_in_memory_container_edits=len(physical['edits']),requested_family_roundtrips=len(physical['source_roundtrips']),
        newly_assigned_existing_variable_types=1,new_raw_fields=0,source_writes=0,
        prior_components_preserved=169,source_files_preserved=420,EXE_paths_preserved=7)
    assert counts['complete_conversion_calls']==counts['raw_233_callbacks']==counts['original_owner_setters']==16
    assert counts['original_score_updates']==11 and counts['score_gate_rejections']==5
    return dict(phase=192,status='verified_existing_Variables233_conditional_creature_conversion_score_delta',
        counts=counts,profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
        field=dict(family='Variables',type=233,payload_bytes=16,type_record_offset=0,value_record_offset=4,value_bytes=4,
                   confirmed_alias='CREATURE_CONVERSION_SCORE_DELTA_MODE4',label_ru='Очки за конверсию существа в режиме4',
                   source_value_i32=40,source_record=physical['raw_record_hex'],
                   source_file_offset=physical['record_file_offset'],
                   storage='Preserve all32 physical Value bits and opaque Extra1/Extra2. Old UNKNOWN_233 baseline name remains historical.',
                   runtime=dict(loader=0x51CD90,store_instruction=0x51D6AD,address=0x6F29E9,width=4,transform='exact32-bit copy')),
        consumer=dict(entry=0x4F2800,owner_setter=0x48D050,score_read=0x4F28B9,
                      conditions=['DWORD75AD4B equals4','Receiving Room owner lookup4E52F0 returns CPlayer whose unsignedBYTE10FC is>=3'],
                      owner_type_scope='CPlayer internaltype, not the player tag/number.',
                      mode_scope='Literal mode4 confirmed; its user-facing mode name is not established by this stage.',
                      actual_effect='Complete conversion changes Creature owner tag2->3 and pointer, preserves HP1000, then optionally updates World score.'),
        score=dict(original_World_vtable=0x66E3EC,slot=0x344,target=0x511650,field_offset=0x72A2,width=4,
                   arithmetic='result=wrap_u32(previous_bits+Value_bits); if signed32(result)<0 then result=0',
                   lower_clamp=True,upper_saturation=False,
                   examples=[dict(before=10,delta=40,after=50),dict(before=10,delta=-5,after=5),dict(before=10,delta=-40,after=0),
                             dict(before=2147483647,delta=1,after=0)],
                   diagnostic_scope='Negative initial score and type255/overflow fixtures test machine representation; not asserted legal normal game settings.'),
        caller=dict(function=0x47B590,conversion_call=0x47B71F,
                    existing_evidence=[link('phase167-caller-queries.json'),link('ghidra/phase167-DX-47b590-assembly.txt'),link('ghidra/phase167-DX-47b590-decompile.txt')],
                    scope='Static threshold/chance path ending in4F2800. Full naturally timed torture/job loop, RNG outcome and visuals are not executed here.'),
        fixture=dict(initial='Prepared native post-factory Warlock plus full original raw Creature loader; explicit World,Room,CPlayer registry/count rows and session.',
                     no_substitution='Original game code, original Creature/World vtables, owner setter and score method are unchanged.',
                     external='Declared external sound lookup/play providers from inherited fixture; only3 external audio PCs executed. No rendered audio/graphics.',
                     low_memory='Only original FS:[0] exception-chain accesses are accepted; early bad CPlayer count/World probes rejected.'),
        primary=proof,native_executions=manifest,independent_Ghidra_controls=controls,storage=physical,
        API=dict(file=link('native_conversion_score.py'),reader='view',physical_patch='patch_bits',observed_arithmetic='project',
                 scope='Inspected DX semantics; physical record patching preserves all unknown payload bytes.'),
        source_baseline=link('phase165-source-baseline.json'),helpers=[link(n) for n in HELPERS],
        priority=dict(current='Global parameters',block_complete=False,order=['ProcessType/DamageType','Global parameters','Creature resistances and flags','Other unknowns'],
                      Process_Damage_status='Core consolidated phase176; explicitly retained variants/version/scheduler gaps remain in the broad objective.',
                      Crypt='Deferred by explicit user instruction9November; phase191 unchanged,10 meanings still unassigned.',
                      next=['Other unknown global scalar meanings and formulas.', 'Global/level override order and original full Variables reader/writer.', 'Creature resistances/flags after the globals block.']),
        limitations=['One existing variable meaning confirmed; not completion of GlobalVariables or all15 families.',
                     'Natural upstream torture threshold/RNG/job loop and score UI consumption remain outside executed scenes.',
                     'Mode4 user-facing identity and transfer to other EXEs/RC3 are not certified.',
                     'Raw MID prefix explicitly prepared; full KWD file I/O and editor saving not executed by these scenes.',
                     'Broad goal remains active; no readiness percentage increase inferred from number of tests.'])


NOTE='conversion_score_semantics phase192: existingVariable233 rawDWORD+4->51CD90->6F29E9->whole4F2800/48D050->originalWorldvslot344/511650. Mode4,unsignedreceivingPlayerType>=3;wrap32addthennegativeclamp0.16native/6Ghidra,258regions/639528B;4newfunctions/8byteblocks/72inherited/7382PC.8exactValueedits/15roundtrips;169priorcomponents/source/codec preserved. User9NovdefersCrypt;currentGlobals.'


def main():
    package=load('editor-format-package.json');p=ROOT/'phase192-prior-package-state.json'
    if not p.exists():
        assert len(package['components'])==169
        dump(p.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
                        preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    previous=load(p.name)
    for k,h in previous['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(previous['components']),set(previous['components'])|{KEY}]
    schema=build();dump(SCHEMA,schema);package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase192-search-status.json',dict(phase=192,goal_status='active',current_block='Global parameters',block_complete=False,
         previous_goal_turn=dict(classification='progress',reason='Phase191 extended and attested Crypt pointer search; ten meanings explicitly remain unresolved.'),
         current_turn=dict(classification='progress',reason='User deferred Crypt; full raw233/conversion/score chain confirmed with independent CPU checks and exact physical edits.'),
         counts=schema['counts'],remaining=schema['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))


if __name__=='__main__':main()
