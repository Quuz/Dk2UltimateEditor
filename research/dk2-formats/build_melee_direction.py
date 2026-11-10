"""Integrate the completed direction/backstab investigation without changing raw formats."""
from copy import deepcopy
from pathlib import Path
import json
import emulate_melee_direction as native
import crosscheck_melee_direction as cross
import probe_melee_direction_versions as versions
import prepare_melee_direction_primary as primary
ROOT,sha,load,dump=native.ROOT,native.sha,native.load,native.dump
KEY,SCHEMA='melee_direction_semantics','melee-direction-semantics.json'
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda v:sha(json.dumps(v,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['native_melee_direction.py','emulate_melee_direction.py','probe_melee_direction_versions.py',
         'crosscheck_melee_direction.py','prepare_melee_direction_primary.py','build_melee_direction.py',
         'probe_melee_animation_flag.py','probe_creature_resistance.py','crosscheck_conversion_score.py','native_unicorn_mapreader.py']


def build():
    old=load('phase194-prior-package-state.json');assert len(old['components'])==171
    for ref in old['components'].values():assert link(ref['file'])==ref
    baseline=load('phase194-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    manifest=load('phase194-direction-emulations.json');assert len(manifest['replays'])==len(native.cases())==54
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,saved,file=native.execute(c);_,out=native.verify(c,args,ch,saved['result'])
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
    controls=load('phase194-direction-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and link(row['file'])['sha256']==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    profiles=load('phase194-direction-versions.json');assert len(profiles['replays'])==3
    for p,row,static in zip(versions.profiles(),profiles['replays'],profiles['profiles']):
        args,ch,saved,file=versions.execute(p)
        assert row['profile']==p and row['file']==str(file.relative_to(ROOT)) and row['sha256']==sha(file.read_bytes())
        assert static==versions.static(p) and row['angle_calls']==2057 and row['unique_angles']==1864
    proof=primary.build();assert proof==load('phase194-direction-primary.json')
    counts=dict(DX_whole_melee_scenes=54,DX_whole_raw_Creature_loads=54,independent_Ghidra_controls=6,
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        angle_helper_profiles=3,angle_calls_per_profile=2057,angle_calls_total=6171,unique_angles_per_profile=1864,
        inherited_function_extents=len(proof['inherited_functions']),new_function_extents=0,
        unique_executed_original_DX_PCs=proof['unique_executed_original_PCs'],
        new_raw_fields=0,new_container_roundtrip_runs=0,source_writes=0,prior_components_preserved=171,
        source_files_preserved=420,EXE_paths_preserved=7,historical_source_hash_differences=len(baseline['historical_drift']))
    assert counts['Ghidra_compared_regions']==288 and counts['Ghidra_compared_bytes']==683076
    return dict(phase=194,status='verified_absolute_world_direction_melee_modifier_not_victim_relative_backstab',counts=counts,
        profile=proof['profile'],field_scope='No new serialized KWD field. This is original EXE arithmetic applied after the base melee getter, before owner modifiers and target HP scaling.',
        contract=dict(melee_entry=0x498E30,angle_call=0x4990D6,angle_helper=0x65FB54,angle_table=0x65FC94,
            coordinate_input='World-coordinate difference attacker minus target, DWORD XY atActor1A/1E; target kindCreature only in this selected path.',
            folding='a=angle&2047; folded=(2046-a) if a>1023 else a; p=(folded&1023)//20',
            damage='For the checked unsigned16 base damage, D=base+floor(base*p/100) if p>15 else base.',
            exact_full_arithmetic='Game works on negative HPdelta: delta+=trunc0(s32(low32(p*delta))/100); only whenp>15. Downstream target scaling follows separately.',
            maximum_percent=51,activation_threshold='Candidate percentages0..15 are ignored;16 is the first applied increment.',
            target_facing_read=False,attacker_facing_read=False,heading_offset=0xF4,
            interpretation='Confirmed current selected code behavior. Lack of target-relative angle and the2047 seam are defects relative to a smooth facing-relative design; intended replacement formula is not established.'),
        examples=[dict(dx=0,dy=-4096,base=100,damage=100),dict(dx=4096,dy=0,base=100,damage=125),
                  dict(dx=0,dy=4096,base=100,damage=151),dict(dx=-16,dy=-4096,angle=2047,base=100,damage=151),
                  dict(dx=-32,dy=-4096,angle=2046,base=100,damage=100)],
        heading_controls='Eight relative directions times4 victim headings,6 additional attacker-heading controls,10 threshold/seam controls,6 translation/scaling controls. Headings and XY remain unchanged; selected full scenes read neither actorWORD_F4.',
        versions=profiles,primary=proof,native_executions=manifest,independent_Ghidra_controls=controls,
        API=dict(file=link('native_melee_direction.py'),functions=['table','angle','modifier','damage'],
                 scope='angle API limited to nonoverflowing abs(dx/dy)<=7FFFFF; damage API limited to unsigned16 base and isolated directional increment.'),
        fixture='Original rawCreature loader consumes a deliberately edited copy with melee damage100. Prepared post-factory Actor/target/World and declared resource/audio providers; game code/vtables remain original. No game source writes.',
        source_baseline=link('phase194-source-baseline.json'),historical_baseline=link('phase165-source-baseline.json'),
        source_drift=baseline['historical_drift'],helpers=[link(n) for n in HELPERS],
        inherited_storage_evidence=link('phase193-gold-storage.json'),
        priority=dict(current='Global parameters',block_complete=False,Crypt='Deferred; ten meanings remain unknown.',
            next=['Variables19/runtime6F2590 and Level override51E708','Unknown global meanings and full reader/writer/load order','Creature resistances and flags']),
        limitations=['Complete loader/melee/HP scenes and6 independent Ghidra controls belong toDX only.',
            'Other two EXEs have statically equivalent arithmetic cores and executed pure angle helpers, not whole melee/AI/session execution.',
            '2057 inputs cover all257 lookup ratios in8 octants plus zero, not all32-bit coordinate values/overflow cases.',
            'No rendered combat, natural AI/animation/World scheduler or runtimeDLL patches are certified.',
            'No working victim-relative backstab formula, new editable percent, wholeCreature flags/resistances closure or global readiness increase is claimed.'])


NOTE='melee_direction_semantics phase194:54fullDXraw/melee/HPscenes+6Ghidra/288regions/683076B confirmworldXYdirection,notvictimheading. Candidatep=floor((fold(angle)&1023)/20),onlyp>15;max51. Angle2047folds-1->1023 andjumps100->151near2046.3profiles execute2057pureangleinputs each;1.3/standard fullmelee notcertified.171priorcomponents/codec/420sources/7EXEs preserved;0newKWDfields. ReturntoGlobals;Cryptdeferred.'


def main():
    package=load('editor-format-package.json');p=ROOT/'phase194-prior-package-state.json'
    if not p.exists():
        assert len(package['components'])==171
        dump(p.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),
            package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    previous=load(p.name)
    for k,h in previous['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(previous['components']),set(previous['components'])|{KEY}]
    schema=build();dump(SCHEMA,schema);package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase194-search-status.json',dict(phase=194,goal_status='active',current_block='Global parameters',block_complete=False,
        current_turn=dict(classification='progress',reason='Previously executed backstab investigation integrated; ready to continue Variables19/Level override.'),counts=schema['counts'],evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))


if __name__=='__main__':main()
