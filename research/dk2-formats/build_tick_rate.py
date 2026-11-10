"""Integrate the proven Variables19 duration multiplier and bounded Level lead."""
from copy import deepcopy
from pathlib import Path
import json
import emulate_tick_rate as native
import crosscheck_tick_rate as cross
import native_tick_rate as api
import prepare_tick_rate_primary as primary
import validate_tick_rate_storage as storage
ROOT,sha,load,dump=native.ROOT,native.sha,native.load,native.dump
KEY,SCHEMA='simulation_tick_rate_semantics','simulation-tick-rate-semantics.json'
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda v:sha(json.dumps(v,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['native_tick_rate.py','emulate_tick_rate.py','crosscheck_tick_rate.py','prepare_tick_rate_primary.py',
    'validate_tick_rate_storage.py','build_tick_rate.py','probe_shot_target_combinations.py',
    'probe_melee_animation_flag.py','crosscheck_conversion_score.py','native_unicorn_mapreader.py']


def build():
    old=load('phase195-prior-package-state.json');assert len(old['components'])==172
    for ref in old['components'].values():assert link(ref['file'])==ref
    baseline=load('phase194-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    manifest=load('phase195-tps-emulations.json');assert len(manifest['replays'])==len(native.cases())==12
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,saved,file=native.execute(c);_,out=native.verify(c,args,ch,saved['result'])
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
        assert out['freeze_timer']==api.duration_ticks(c['duration'],c['tps'])
    controls=load('phase195-tps-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and link(row['file'])['sha256']==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof==load('phase195-tps-primary.json')
    physical=storage.build();assert physical==load('phase195-tps-storage.json')
    runtime=load('variable-runtime-map.json')['rows']['19'];assert runtime['baseline_name']=='UNKNOWN' and runtime['runtime_address']=='0x6f2590' and runtime['runtime_bytes']==4
    counts=dict(native_scenes=12,registered_Variables19_callbacks=12,original_collisions=12,complete_Freeze_callbacks=12,
        independent_Ghidra_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),
        Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),inherited_function_extents=len(proof['inherited_functions']),
        unique_executed_original_PCs=proof['unique_executed_original_PCs'],new_executed_function_extents=0,
        static_Level_function_ranges=1,synthetic_Variables_append_remove_controls=len(physical['edits']),
        exact_Level_field_edits=len(physical['Level']['edits']),new_raw_fields=0,newly_assigned_existing_variable_types=1,
        prior_components_preserved=172,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0)
    assert counts['Ghidra_compared_regions']==276 and counts['Ghidra_compared_bytes']==580936
    return dict(phase=195,status='verified_existing_Variables19_simulation_tick_multiplier_and_static_Level_alias',counts=counts,
        profile=proof['profile'],field=dict(family='Variables',type=19,payload_bytes=16,type_record_offset=0,Value_record_offset=4,Value_bytes=4,
            confirmed_alias='SIMULATION_TICKS_PER_SECOND',label_ru='Частота логики: множитель длительностей в тиках',historical_alias='UNKNOWN',
            source_record_present=False,source_default_value=None,physical='Keep full32 Value bits and opaque Extra1/Extra2.',
            runtime=dict(registration_initializer=0x51AA90,registration_row=0x6A5E38,callback=0x51CD90,store_instruction=0x51CDCA,address=0x6F2590,width=4)),
        consumer=dict(process_type=11,entry=0x4AE1D0,multiply_instruction=0x4AE238,timer_setter=0x48CB60,timer_store=0x48CB83,
            target_timer_offset=0x447,target_timer_bytes=2,formula='timer=low16(low32(instanceShotData1 * Variables19_bits))',
            examples=[dict(tps=4,Data1=5,timer=20),dict(tps=20,Data1=5,timer=100),dict(tps=65536,Data1=5,timer=0),dict(tps=0xFFFFFFFF,Data1=5,timer=65531)],
            scope='Exactly this original duration consumer; no rendered FPS, natural scheduler rate or wall-clock timing measurement.'),
        Level=dict(raw_properties_offset=0x1182,raw_bytes=2,converter=0x56C100,converter_store_instruction=0x56C1D5,MID_offset=0x12B6,
            callback=0x51E6D0,runtime_store_instruction=0x51E708,runtime_address=0x6F2590,zero_extends_to_bits=32,
            current_source=physical['Level'],status='static_runtime_writer_plus_current_physical_field_edits',
            scope='Same runtime address and u16-to-u32 write confirmed statically. FullLevel callback and automatic load order are not executed here.',
            converter_evidence=link('level-descriptor-semantics.json')),
        order=dict(scene='Original Type19 callback is deliberately placed after sourceCreature/Shot/CreatureSpell setup and before original collision.',
            natural_common_then_level_or_reverse='Unresolved; caller ordering must be proved before assigning default/override priority.',
            absent_record='Current commonGlobalVariables contains no19. Synthetic16-byte records test code support, not an actual shipped default.'),
        fixture=dict(code='Original code/vtables preserved; actual registration pointer used. Prepared resource/audio providers remain explicit.',
            independent_input='Overlapping native World views are coalesced into an identical ordered last-write-wins input region forGhidra; early overlap assertion is a rejected fixture diagnostic.',
            measured_opaque_context='IndependentGhidra checks compare native full-context oracles; opaque bytes receive no new semantic interpretation.',
            frame_scope='No post-FreezeCreature tick or naturalWorld scheduler. Timer0 and extremebits are diagnostics, not recommended gameplay settings.'),
        primary=proof,native_executions=manifest,independent_Ghidra_controls=controls,storage=physical,
        API=dict(file=link('native_tick_rate.py'),functions=['view','patch_bits','duration_ticks']),helpers=[link(n) for n in HELPERS],
        source_baseline=link('phase194-source-baseline.json'),
        priority=dict(current='Global parameters',block_complete=False,Crypt='Deferred; ten counter meanings remain unknown.',
            next=['Natural common/level loading order and the complete Level callback51E6D0.','Other unknown globals29/186 and full Variables reader/writer.','Creature resistances and flags after globals.']),
        limitations=['One existing variable meaning confirmed; no completion of global parameter block or15-family objective.',
            'StaticLevel alias is not proof which input wins in a natural game session.',
            'No default20 inferred: sourceAutoSave field is4; actual live rate not measured.',
            'No new raw fields, new resistance percent result, otherEXE certification or readiness percentage increase.'])


NOTE='simulation_tick_rate_semantics phase195: existingVariables19 UNKNOWN->SIMULATION_TICKS_PER_SECOND; actualrow19/51AA90->51CD90store51CDCA->6F2590->wholecollision/Freeze4AE1D0. Data1*TPS low16timer447;12native/4Ghidra/276regions/580936B/11930PC. Levelraw1182u16->MID12B6->sameglobal51E708 isstaticonly; naturalorder unresolved.8syntheticVariablesappends+5exactLeveledits.172priorcomponents/codec/420currentbaseline sources/7EXEs preserved. Globalsactive,Cryptdeferred.'


def main():
    package=load('editor-format-package.json');p=ROOT/'phase195-prior-package-state.json'
    if not p.exists():
        assert len(package['components'])==172
        dump(p.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),
            package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(p.name)
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    s=build();dump(SCHEMA,s);package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase195-search-status.json',dict(phase=195,goal_status='active',current_block='Global parameters',block_complete=False,
        current_turn=dict(classification='progress',reason='Variables19 duration multiplier executed; staticLevel alias bounded; natural order remains next.'),counts=s['counts'],evidence=[link(SCHEMA)]))
    print(json.dumps(s['counts']))


if __name__=='__main__':main()
