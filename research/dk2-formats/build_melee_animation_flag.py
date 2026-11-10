"""Publish a formerly unknown raw Creature bit with bounded native animation semantics."""
from pathlib import Path
import json
import emulate_melee_animation_flag as native
import crosscheck_melee_animation_flag as controls
import prepare_melee_animation_primary as primary
import native_melee_animation_flag as api
from validate_melee_animation_flag import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase147-melee-animation-primary.json')
    manifest=load('phase147-melee-animation-emulations.json');rows=manifest['replays'];assert len(rows)==14 and len({r['file'] for r in rows})==14
    states={}
    for c,row in zip(native.cases(),rows):
        args,checks,saved,p=native.execute(c);post,out=native.verify(c,args,checks,saved['result']);states[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['file']==str(p.relative_to(ROOT)) and row['sha256']==sha(p.read_bytes())
    off=states['melee-0-initial0'];on=states['melee-1-initial0'];diff=[]
    for a,b in off.items():
        changes=[i for i,(x,y) in enumerate(zip(b,on[a])) if x!=y]
        if changes:diff.append(dict(region=hex(a),offsets=changes))
    assert diff==manifest['whole_raw_melee_pair_differences']
    assert {int(r['region'],16):r['offsets'] for r in diff}=={
        native.melee.cf.ACTOR:[0x3EB],native.melee.cf.DEFINITION:[0x689],native.melee.prior.RAW:[0xEFF],native.melee.prior.MID:[0x12EF]}
    cross=load('phase147-melee-animation-crosschecks.json')['replays'];assert len(cross)==4
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert args==d['input'] and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase147-animation-flag-api-validation.json')
    assert edits['historical_codec']['sha256']=='d25d4f294cabac99a5fe26f45b8414a4d9ccf1c7905e666ee40b9b4ba8b1c392'
    preservation=load('trap-model-word-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    before=ROOT/'phase147-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==125
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==125
    return dict(phase=147,date='2026-10-31',family='Creatures',record_variants=[5449,5537],profile=attested['profile'],primary=attested,
      flag=dict(raw_offset=0xEFC,raw_mask=api.MASK,raw_bit=29,legacy_name='UNKNOWN_5',current_name=api.NAME,
        recommended_UI_label='Melee instance animation when standing',native_runtime_name='PlayInstanceAnimation',
        name_scope='Native runtimebit12 has this diagnostic name; rawflag name is inferred from proven rawloader/melee/animation dataflow, not from a direct rawfield UI label.'),
      lineage=dict(raw_DWORD=0xEFC,intermediate_DWORD=0x12EC,definition_DWORD=0x686,runtime_DWORD=0x3EA,runtime_mask=0x1000,
        melee=0x498E30,formula='Actor3EA = (Actor3EA & ~1000h) | (((definition686 >>29)&1)<<12).',
        timing='At wholemelee entry, before recharge refusal and target lookup. Clear rawbit clears an alreadyset runtimebit; set rawbit sets a previouslyclear runtimebit.'),
      native_action=dict(initializer=0x4950C0,table=0x6E4198,rows=81,stride=29,melee_instance=17,registered_callback=0x498E30,animation_selector=5,
        activation='Current action17 is prepared explicitly. Original action table initializer executes; normal496700 action activation/AI scheduler not composed.'),
      animation=dict(function=0x48BE50,instance_selector_getter=0x496DE0,gate_instruction=0x48C392,
        branch='Zero movement metric and mode3BB!=2: a nonzero active instance selector replaces standing resource only when runtime3EA1000 is set.',
        selected_native_scenes=[dict(enabled=False,moving=False,action=17,slot=39,name='STAND_STILL'),dict(enabled=True,moving=False,action=17,slot=5,name='MELEE_ATTACK'),
          dict(enabled=False,moving=True,action=17,slot=5,name='MELEE_ATTACK'),dict(enabled=True,moving=True,action=17,slot=5,name='MELEE_ATTACK')],
        no_action='Action0 keeps standing resource39 in both bit cases.',
        implication='The rawbit controls melee instance animation in this standing branch; it is not a universal all-animation disable flag.',
        observation='Actual descriptorDWORD_F reads at48C5A8 identify the selected resource, not a guessed return register.'),
      damage=dict(source='Current source Vampire ID8 raw record, prepared nonlethal targetHP1000.',HP_after_with_or_without_bit=334,
        interpretation='Same666HP request/application in selected successful attacks. Animation flag affects runtime selection while this damage result stays equal; not all damage/combat paths.'),
      compositions=dict(raw_melee='Original definition ctor491DC0 -> MIDinitializer568820 -> rawconverter568E30 -> full loader492320 -> Variables callbacks ->81row native actiontable -> whole498E30.',
        renderer='Separate complete48BE50 call from saved post-melee memory. Host explicitly prepares resource-presence bits/cache, movement, active action and ordinary state; no uninterrupted raw-to-render pipeline claim.',
        render_fixture='All48descriptor presence bits set; shared modelID0, current cached descriptor/selector and framecount100 explicit. No GPU assets/pixels or asset-swap correctness tested.',
        alias_normalization='Inherited extendedWorld/pool input aliases coalesced in original ordered last-write-wins order to a single consistent physical region; no original code changes.',
        external_IO='Prepared resource failure/sound/session adapters only; all gameplay/control functions original.'),
      effective_creature_flag_enum=api.effective_enums()['Creature.CreatureFlag'],
      native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
      counts=dict(native_scenarios=14,complete_raw_to_melee_cases=8,complete_separate_animation_cases=6,
        independent_Ghidra_CPU_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        original_function_extents=len(attested['functions']),executed_function_extents=sum(not r['static_only'] for r in attested['functions']),static_formatter_extents=sum(r['static_only'] for r in attested['functions']),
        unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
        whole_raw_melee_pair_regions=len(off),whole_raw_melee_pair_bytes=sum(map(len,off.values())),pair_changed_bytes=sum(len(r['offsets']) for r in diff),
        new_exact_container_edits=edits['new_exact_container_edits'],public_decode_rebuild_controls=edits['public_decode_rebuild_controls'],source_Creature_records=edits['source_records'],
        preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),newly_named_unknown_gameplay_bits=1,source_writes=0),
      helpers=[link(n) for n in ['probe_melee_animation_flag.py','probe_instance_animation.py','emulate_melee_animation_flag.py','crosscheck_melee_animation_flag.py',
        'prepare_melee_animation_primary.py','native_melee_animation_flag.py','validate_melee_animation_flag.py','dk2_formats_phase146.py']],
      limitations=['Exact suppliedDX. No certified retail1.3/RC3game or patchDLL behavior transfer.',
        'Prepared postfactory Actor/target/world and resource IO; one raw attacker species/current file, ordinary nonlethal targets, selected actions/velocities.',
        'Separate host-prepared animation context starts after raw/melee snapshot. Presence/cache/model IDs prepared; no normal AI/action activation, real asset swap or GPU/pixel proof.',
        'Native formatter string/function is static primary evidence, not executed diagnostic logging. Only bit29 enum name changes; otherflag names remain prior labels.',
        'Moving mode0 bypasses the standing flag check in selected scenes. No universal disable-allanimations or universal damage-independence claim.',
        'Original data files/EXE/parallel editor untouched. Otherunknown bits800/1000,Jump/Priority,Traps204/Doors236/Terrain198224/AP0408/KeeperC8 and significant15family questions remain; readiness about75%.'])
if __name__=='__main__':
    d=build();(ROOT/'melee-animation-flag-semantics.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(d['counts']))
