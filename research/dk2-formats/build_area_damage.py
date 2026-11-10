"""Publish actual multi-target falloff, TIME and expanding-square damage paths."""
from pathlib import Path
import copy,hashlib,json
import emulate_area_damage as native
import crosscheck_area_damage as independent
import prepare_area_primary as primary
from build_poison import effective_shot_parameters as prior_shot_parameters

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def effective_shot_parameters():
    value=copy.deepcopy(prior_shot_parameters())
    changes={
        '1':dict(data1='Constant damage per eligible target; tested Boulder74 receives half.',
            data2='Instance integer cell extent R. Filled square [cx-R,cx+R) x [cy-R,cy+R), no radial test.'),
        '2':dict(data1='Shared working damage: each eligible in-radius target overwrites the accumulator with truncated falloff; not independent per-target Data1.',
            data2='Instance integer radius R in cells. Inclusive clipped cell scan, then strict Q12 radial distance test.',
            native_defect='4AC8BF retains EDI after falloff across targets in the same dispatch; instance Data1 stays unchanged. Boulder74 quarter candidate only gates zero; radial formula uses the unquartered accumulator.',
            traversal='Ascending y then x then linked cell-list order; accumulator restarts from instance Data1 on next call.'),
        '3':dict(data1='Truncated instance Data1 * currentShotHP / definition initialHP(u16).',
            data2='Definition signed Data2 >>12 supplies filled half-open square cell extent; instance Data2 is not its source.',
            corpus_scope='No mode3 in5551 source Shot records. Executed diagnostic mode, not a newly discovered shipped spell.'),
        '4':dict(data1='Full instance Data1 per eligible target inside a filled expanding square.',
            data2='Extent = trunc((initialHP-currentShotHP)*(signed instance Data2 >>12)/initialHP); half-open square, no annulus or radial-distance test.',
            corpus_scope='No mode4 in5551 source Shot records. Executed diagnostic mode, not a newly discovered shipped spell.')}
    for key,edit in changes.items():
        value['damage_types'][key].update(edit,phase131_evidence='area-damage-semantics.json',
            execution_scope='Direct original dispatcher invocation after native loaders/factory; complete tick/lifetime scheduler not executed.')
    return value


def build():
    attested=primary.build();assert attested==load('phase131-area-primary.json')
    runs=load('phase131-area-native-emulations.json')['replays'];assert len(runs)==len(native.cases())==27
    for case,row in zip(native.cases(),runs):
        args,checks=native.build(case);path=native.path_for(args);saved=load(str(path.relative_to(ROOT)))
        assert row['case']==case['name'] and saved['case']==case and saved['input']==args and sha(path.read_bytes())==row['sha256']
        assert native.verify(case,args,checks,saved['result'])==row['outcome']
    controls=load('phase131-area-crosschecks.json')['replays'];assert len(controls)==4
    for row in controls:
        args,count,size=independent.build(row['case']);saved=load(row['file']);result=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['checked_regions']==count and row['checked_bytes']==size
    corpus=load('phase131-area-api-validation.json');assert len(corpus['files'])==82 and corpus['records_seen']==5551
    assert corpus['exact_container_rebuilds']==corpus['changed_containers']==246
    executable_paths=load('art-resource-editor-semantics.json')['preservation']['original_executable_paths']
    for row in corpus['files']+executable_paths:assert sha(Path(row['file']).read_bytes())==row['sha256']
    creature=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd'
    known=next(r for r in load('phase125-category-api-validation.json')['files'] if Path(r['file'])==creature)
    assert sha(creature.read_bytes())==known['sha256']
    previous=ROOT/'phase131-prior-components.json'
    if not previous.exists():
        prior=load('editor-format-package.json')['components'];assert len(prior)==109
        previous.write_text(json.dumps(prior,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==109
    return dict(phase=131,date='2026-10-29',families=['Shots','Creatures'],profile=attested['profile'],primary=attested,
        physical_fields=dict(raw_record_bytes=239,raw_Data1=0x98,raw_Data2=0x9C,raw_DamageType=0xC5,
            raw_AttackCategory=0xC8,raw_initialHP=0xBC,definition_Data1=0x37,definition_Data2=0x3B,
            definition_initialHP_u16=0x47,instance_Data1=0x13D,instance_Data2=0x141,instance_currentHP=0x40),
        effective_shot_parameters=effective_shot_parameters(),
        shared_falloff=dict(handler=0x4AC6D0,read_initial_once='EDI = instance Data1',multiply_PC=0x4AC8B3,
            signed_divide_PC=0x4AC8B7,retain_accumulator_PC=0x4AC8BF,HP_call_PC=0x4AC8CD,
            formula='For selected bounded positive inputs: dist2Q12=(dx*dx>>12)+(dy*dy>>12); require dist2Q12 < (R*4096)^2>>12; d=isqrt(dist2Q12*4096); work=trunc0((R*4096-d)*work/(R*4096)); requestHP(-work,owner16,0).',
            traversal='Ascending y then x; within cell follow WORD+0C next tag through registry6EE540. Cell scan inclusive and clipped to map before strict radial test.',
            reset='Shared across all processed cells/targets during one dispatch; next call rereads original instance Data1.',
            centre_then_quarter_tile=dict(damage=80,radius_cells=2,positions_Q12=[[0,0],[1024,0]],HP_deltas=[-80,-70]),
            quarter_tile_then_centre=dict(HP_deltas=[-70,-70]),
            filtered_targets='Friendly/invulnerable Creature fixtures do not attenuate the accumulator; strict radius boundary and outside circle are skipped.',
            Boulder74='Quarter-damage candidate is tested for zero, then discarded by distance calculation. Damage80 yields80/70; Damage3 skips both prepared Boulders. Not a category-resistance formula.',
            defect='Real order-dependent damage in original machine code; no claim about intended design or every patch.dll hook.'),
        time_mode=dict(handler=0x4AC940,mode=3,damage='trunc0(instanceData1*currentHP/initialHP_u16)',
            extent='definitionData2 SAR12',shape='Filled half-open square; ascending x then y.',
            tested_HP=[32,16,8],tested_damage_for_Data1_80=[80,40,20],subcell_Data2_4095_extent=0),
        ring_mode=dict(handler=0x4ACAF0,mode=4,damage='Full instance Data1',
            extent='trunc0((initialHP_u16-currentHP)*(instanceData2 SAR12)/initialHP_u16)',
            shape='Filled half-open expanding square; ascending x then y. No inner hole or radial-distance condition.',
            verified='Initial HP32 produces extent0. HP16/Data2=16384 produces extent2 and hits centre plus corner outside equal-radius circle. Plain Data2=2 shifts to0.'),
        diagnostics=dict(entry=0x554560,actual_CRT_formatting=True,buffer_lines=64,line_bytes=128,
            prepared_log_object=0x7428A0,scope='Original in-memory debug log executed; complete buffers included in TIME/RING crosschecks. No disk/OS logger output.'),
        fixture_scope=dict(raw_sources=['Creature Vampire8','Shot Mistress Hail Storm17'],
            chain='Original constructors, converters, loaders, Shot factory4B7CD0/4AAC90, optional original HP setter on Shot, damage dispatcher4AC3C0 and final native HP getter.',
            extra_targets='Prepared copies of native common/Creature snapshots with unique tags and valid doubly linked per-cell lists; share actual loaded Creature definition. Not additional Creature factories.',
            health_preparation='Original4B2CE0 applies -16/-24 to Shot before dispatch; no lifetime scheduler emulation.',
            repair='Legacy frozen fixture overlapped Session/GFX with enlarged World. New fixture relocates only those two active objects to1B020000/1B030000 and fixes pointers/loader argument; asserts all input regions disjoint.',
            category=6,four_loaded_resistance_pairs='Type6, all values100; one ordered-range control value0. No resistance-pair reads in selected post-load paths.',
            external_environment='Prepared resource/sound IO responses and native snapshots; original method/vtable bytes protected. Whole Windows game not run.'),
        native_executions=runs,independent_Ghidra_controls=controls,corpus=corpus,
        counts=dict(native_compositions=27,original_HP_requests=sum(len(r['outcome']['requests']) for r in runs),
            independent_Ghidra_CPU_controls=4,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),
            Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),fresh_function_spans=len(attested['functions']),
            fresh_data_blocks=len(attested['blocks']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],
            original_Shot_sources_preserved=82,source_Shot_records=5551,new_exact_container_rebuilds=246,
            original_executable_paths_preserved=len(executable_paths),source_writes=0),
        preservation=dict(original_executable_paths=executable_paths,original_Shot_sources=corpus['files'],original_Creature_source=known),
        helpers=[link(n) for n in ['emulate_area_damage.py','native_aoe_trace.py','crosscheck_area_damage.py','prepare_area_primary.py',
            'validate_area_damage.py','native_shot_damage.py','probe_shot_hit.py','probe_creature_resistance.py']],
        limitations=['Bounded positive Data1/radii and prepared integer coordinates; no general proof for overflow, negative inputs, initialHP0 or all world-edge conditions.',
            'Mode3/4 have no records in the5551-record source corpus; diagnostic native support doesnot prove use by shipped gameplay.',
            'TIME uses definitionData2 statically; no diagnostic divergence between instanceData2 and definitionData2 was injected.',
            'No complete tick/flight/collision/lifetime scheduler, live Windows renderer, real resource IO or patch.dll detour execution.',
            'Four Ghidra controls compare every prepared non-stack output region, not entire process memory. HP/request model independent; opaque buffers use measured native outputs.',
            'Boulder diagnostics only cover selected category6/owner8 modes1/2. Gas exclusions and other target kinds are not newly executed.',
            'Mode5/6 trigger timing, terrain mode8, actual resistance repair, unit spell-slot expansion and major remaining15family unknowns remain open. No game patch or parallel editor changes.'])


if __name__=='__main__':
    value=build();(ROOT/'area-damage-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))
