"""Publish Variables74 gold capacity margin, correcting the historical label."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_gold_margin as native
import crosscheck_gold_margin as cross
import prepare_gold_margin_primary as primary
import validate_gold_margin_storage as storage
ROOT=Path(__file__).resolve().parent
KEY,SCHEMA='gold_capacity_margin_semantics','gold-capacity-margin-semantics.json'
sha,load,dump=native.sha,native.load,native.dump
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda v:sha(json.dumps(v,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['native_gold_margin.py','emulate_gold_margin.py','crosscheck_gold_margin.py',
         'prepare_gold_margin_primary.py','validate_gold_margin_storage.py','build_gold_margin.py',
         'crosscheck_conversion_score.py','native_unicorn_mapreader.py','emulate_temple_creature_entry.py']


def build():
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    old=load('phase193-prior-package-state.json');assert len(old['components'])==170
    for ref in old['components'].values():assert link(ref['file'])==ref
    manifest=load('phase193-gold-emulations.json');assert len(manifest['replays'])==len(native.cases())==16
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,saved,file=native.execute(c);post,out=native.verify(c,args,ch,saved['result'])
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
        traces.append(saved['result'])
    controls=load('phase193-gold-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and link(row['file'])['sha256']==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    proof=primary.build();assert proof is not None and proof==load('phase193-gold-primary.json')
    physical=storage.build();assert physical==load('phase193-gold-storage.json')
    runtime=load('variable-runtime-map.json')['rows']['74']
    assert runtime['baseline_name']=='GAME_TICKS' and runtime['runtime_address']=='0x6f28b6'
    assert runtime['runtime_bytes']==4 and runtime['transform']=='copy_low_bits'
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenes=16,independent_Ghidra_controls=6,
        Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        original_registration_initializers=visits(0x51AA90),raw_74_callbacks=visits(0x51CD90),
        raw_level_growth_callbacks=visits(0x51DC40),source_raw_Creature_loads=visits(0x492320),
        complete_gold_predicates=visits(0x464360),margin_comparisons=visits(0x4643C7),
        zero_argument_bypasses=16-visits(0x4643C7),
        new_complete_function_extents=len(proof['functions']),inherited_extents=len(proof['inherited']),
        additional_executed_byte_blocks=len(proof['executed_blocks']),unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        declared_caller_and_external_audio_PCs=proof['declared_caller_and_external_audio_PCs'],
        exact_in_memory_container_edits=len(physical['edits']),new_source_file_roundtrips=1,
        inherited_family_roundtrips=15,new_runs_of_inherited_family_roundtrips=0,
        corrected_existing_variable_labels=1,new_raw_fields=0,source_writes=0,
        prior_components_preserved=170,source_files_preserved=420,EXE_paths_preserved=7)
    assert counts['original_registration_initializers']==counts['raw_74_callbacks']==counts['raw_level_growth_callbacks']==counts['complete_gold_predicates']==16
    assert counts['margin_comparisons']==14 and counts['zero_argument_bypasses']==2
    assert counts['Ghidra_compared_regions']==240 and counts['Ghidra_compared_bytes']==671172
    return dict(phase=193,status='verified_existing_Variables74_gold_capacity_margin_corrects_GAME_TICKS',
        counts=counts,profile=dict(file=str(native.EXE),sha256=sha(native.EXE.read_bytes())),
        field=dict(family='Variables',type=74,payload_bytes=16,type_record_offset=0,value_record_offset=4,value_bytes=4,
            confirmed_alias='GOLD_CAPACITY_MARGIN',label_ru='Запас до предела переносимого золота',historical_alias='GAME_TICKS',
            source_value_bits=340,source_record=physical['raw_record_hex'],source_file_offset=physical['record_file_offset'],
            source_file=physical['source'],
            storage='Preserve full32 physical Value bits and opaque Extra1/Extra2. Historical baseline enum is frozen; use this component for the effective meaning.',
            runtime=dict(loader=0x51CD90,store_instruction=0x51CFE0,address=0x6F28B6,width=4,transform='exact32-bit copy')),
        registration=dict(initializer=0x51AA90,table=0x6A5CD0,row=0x6A61E4,row_bytes=20,callback_offset=4,
            callback=0x51CD90,scope='Original initializer executes; prepared caller dispatches through the actual registered table pointer. No table/vtable/game-function substitution.'),
        consumer=dict(entry=0x464360,complete_function_bytes=147,context_creature_pointer_offset=8,
            carried_gold=dict(actor_offset=0x3FB,width=4,unsigned_compare=True),
            maximum_gold=dict(raw_Creature_offset=0xF1E,runtime_definition_offset=0x69C,width=2,stat=6,source_Warlock_base=6000),
            level=dict(actor_offset=0x403,width=1),
            growth=dict(raw_Variables_type=65,stat=6,callback=0x51DC40,coefficient_bits=16,coefficient_signed=True,
                        coefficient_read='WORD[VariablesBase+13B+2*level]',VariablesBase=0x6F2550,
                        level2_prepared_coefficient=150,
                        coefficient_scope='150 is an explicitly prepared arithmetic control, not the current common source value.'),
            formula=['If argument==0: return carried_gold!=0; margin/capacity are not read.',
                     'If level==1: capacity=base_max_gold; otherwise capacity=trunc0(base_max_gold*signed16(level_coefficient)/100).',
                     'threshold=wrap_u32(capacity-Value74); return unsigned32(carried_gold)>=threshold.'],
            capacity_or_threshold_clamp=False,
            examples=[dict(base=6000,level=1,margin=340,threshold=5660,below_result=False,boundary_result=True),
                      dict(base=6000,level=2,coefficient=150,margin=340,threshold=8660),
                      dict(base=6000,level=1,margin=6001,threshold=0xFFFFFFFF,carried=6000,result=False),
                      dict(base=100,level=1,margin=340,threshold=0xFFFFFF10,carried=100,result=False)],
            scope='A complete predicate, not proof of all Treasury/deposit actions, natural job selection or every indirect consumer.'),
        second_consumer=dict(function=0x488930,read_instruction=0x4890D7,status='static_only_this_stage',
            scope='Original Creature tick reads74 near the level-adjusted gold capacity comparison. Full Creature tick is not newly executed here.',
            evidence=[link('ghidra/phase193-type74-runtime-xrefs.txt')]),
        source_resolution=physical['descriptor'],
        fixture=dict(initial='Prepared post-factory Actor/AI/World, actual raw Creature loader and native AI/NAV initialization.',
            runtime_alias='Fixed growth callback globals and VariablesBase pointer alias the same original runtime6F2550 block.',
            calls='Original registration initializer, registered raw74 callback, raw65 growth callback, full464360 predicate.',
            code='Original game code, Creature/World vtables and native registration entries remain unchanged; prepared dispatch/caller and external sound provider are declared.',
            clocks='Runtime tick rate20 is a prepared fixture value, not a measured actual map/default rate and not the meaning of74.',
            low_memory='Only original FS:[0] accesses accepted. Early disconnected growth-buffer/stack-argument diagnostic probes are excluded from accepted manifests.'),
        primary=proof,native_executions=manifest,independent_Ghidra_controls=controls,storage=physical,
        API=dict(file=link('native_gold_margin.py'),reader='view',physical_patch='patch_bits',observed_arithmetic='project',
                 scope='Selected DX predicate semantics; exact physical edits preserve unknown bytes.'),
        source_baseline=link('phase165-source-baseline.json'),helpers=[link(n) for n in HELPERS],
        priority=dict(current='Global parameters',block_complete=False,
            order=['ProcessType/DamageType','Global parameters','Creature resistances and flags','Other unknowns'],
            Process_Damage_status='Core consolidated phase176 with explicit version/variant/scheduler limits.',
            Crypt='Deferred by explicit user instruction9November; ten meanings remain unassigned.',
            next=['Type19/runtime6F2590 and the separate Level loader override51E708.',
                  'Other unknown globals, common/level overrides and full original Variables reader/writer.',
                  'Creature resistances and flags after the globals block.']),
        limitations=['Corrects one existing scalar meaning; global parameter block and broad15-family objective remain incomplete.',
            'Source74 is sibling editor/AutoSaveVariables.kld. Current GlobalVariables lacks74; descriptor names a missing Maps/AutoSaveVariables.kld, so successful actual AutoSave resolution is not claimed.',
            'No full native KWD file parser, editor writer, natural deposit/job loop or visual game session is newly executed.',
            'Static Type19 Level writer is a next lead; its complete semantics/override order are not certified here.',
            'No consumer of29/186 established; narrow direct-xref absence does not establish nonuse.',
            'Other EXEs/RC3, all levels/coefficients and indirect callers are not certified by these16 scenes.',
            'No readiness percentage increase inferred from test or component count.'])


NOTE='gold_capacity_margin_semantics phase193 corrects historicalVariables74 GAME_TICKS to GOLD_CAPACITY_MARGIN: rawDWORD+4->actualregistration51AA90/51CD90->6F28B6->whole464360. Unsignedcarry>=wrap32(leveladjustedMaxGold-Value74), no clamp; argument0 testsnonzerocarryonly.16native/6Ghidra,240regions/671172B;3newfunctions/64inherited/8076PC.8exactedits/1newsource roundtrip;15priorroundtrips inherited,notrerun.170priorcomponents/codec/420sources/7EXEs preserved. SiblingAutoSaveVariables isnot thedescriptor-namedmissingMapsfile. Globalsactive/Cryptdeferred.'


def main():
    package=load('editor-format-package.json');p=ROOT/'phase193-prior-package-state.json'
    if not p.exists():
        assert len(package['components'])==170
        dump(p.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),
            package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    previous=load(p.name)
    for k,h in previous['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(previous['components']),set(previous['components'])|{KEY}]
    schema=build();dump(SCHEMA,schema);package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase193-search-status.json',dict(phase=193,goal_status='active',current_block='Global parameters',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Variables233 conversion score confirmed; globals remain open.'),
        current_turn=dict(classification='progress',reason='Variables74 historical GAME_TICKS corrected through full gold predicate/native/Ghidra and storage controls.'),
        counts=schema['counts'],remaining=schema['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))


if __name__=='__main__':main()
