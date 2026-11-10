"""Publish bounded original action80 lifetime versus animation-end semantics."""
from pathlib import Path
from copy import deepcopy
import json
import emulate_crypt_action_completion as native
import crosscheck_crypt_action_completion as cross
import prepare_crypt_completion_primary as primary

ROOT = Path(__file__).resolve().parent
KEY = 'crypt_action_continuation_semantics'
SCHEMA = 'crypt-action-continuation-semantics.json'
sha = native.sha
load = lambda name: json.loads((ROOT/name).read_text(encoding='utf-8'))
dump = lambda name, value: (ROOT/name).write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
link = lambda name: dict(file=name, sha256=sha((ROOT/name).read_bytes()))
canonical = lambda value: sha(json.dumps(value, ensure_ascii=False, sort_keys=True).encode())


def build():
    manifest = load('phase189-crypt-completion-emulations.json')
    assert len(manifest['replays']) == len(native.cases()) == 11
    traces, scenes = [], {}
    for c, row in zip(native.cases(), manifest['replays']):
        args, checks, saved, file = native.execute(c)
        post, out = native.verify(c, args, checks, saved['result'])
        assert row == dict(case=c, file=str(file.relative_to(ROOT)), sha256=sha(file.read_bytes()), outcome=out)
        traces.append(saved['result']); scenes[c['name']] = (args, checks, saved['result'], post)
    controls = load('phase189-crypt-completion-crosschecks.json')['replays']
    assert [r['case'] for r in controls] == cross.CONTROLS
    for row in controls:
        args, n, size = cross.build(row['case']); saved = load(row['file']); r = saved['result']
        assert saved['input'] == args and (n, size) == (row['checked_regions'], row['checked_bytes'])
        assert row['sha256'] == link(row['file'])['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16) == v for k,v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
    proof = primary.build(); assert proof == load('phase189-crypt-completion-primary.json')
    # Record the diagnostic exit without silently converting it to normal
    # scheduling or guessing a death path. Original raw preference is edited
    # in memory, mode1 and the clock schedule are declared fixture conditions.
    wrap = scenes['80-clock-wrap'][2]
    writes = [x['value'] for x in wrap['observed_writes'] if int(x['address'],16) == native.probe.ACTOR+0x25E]
    choices = [x['value'] for x in wrap['observed_writes'] if int(x['address'],16) == native.probe.ACTOR+0x347]
    assert writes == [1, 274, 6, 275, 274, 1, 1] and choices == [10, 0], (writes, choices)
    assert wrap['visited_instructions'].get('0x483790') == 2 and wrap['visited_instructions'].get('0x483ac0') == 2
    assert [0x4772B7,0x476D30] in wrap['observed_call_edges']
    baseline = load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:
        assert sha(Path(ref['file']).read_bytes()) == ref['sha256'], ref['file']
    old = load('phase189-prior-package-state.json')
    for ref in old['components'].values():
        assert link(ref['file']) == ref
    visits = lambda address: sum(r['visited_instructions'].get(hex(address),0) for r in traces)
    counts = dict(native_scenes=11, independent_Ghidra_controls=7, checkpoint_snapshots=66,
                  Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),
                  Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
                  original_AI_dispatches=visits(0x476FC0), original_action_dispatches=visits(0x496AA0),
                  Crypt_effect_callbacks=visits(0x499CA0), static_return_epilogues=len(proof['callback_return_epilogues']),
                  additional_primary_extents=len(proof['functions']), inherited_primary_extents=len(proof['inherited']),
                  unique_executed_original_PCs=proof['unique_executed_original_PCs'],
                  prior_components_preserved=166, source_files_preserved=420, EXE_paths_preserved=7,
                  new_raw_fields=0, source_writes=0, Crypt_passive_counter_meanings_assigned=8,
                  Crypt_active_groups_assigned=10, Crypt_counter_meanings_remaining=10)
    return dict(phase=189, status='verified_original_Crypt_action_continues_despite_animation_completion', counts=counts,
                profile=dict(file=str(native.EXE), sha256=sha(native.EXE.read_bytes())),
                physical_format_scope='Nonewrawfield,noadditionalKWDmeaningassigned. ExistingCryptJob28/animationconsumer/lifetimedistinction.',
                original_action_table=dict(initializer=0x4950C0, base=0x6E4198, stride=29,
                    action80=dict(callback=0x499CA0, callback_this_offset=0, completion_mode=0, duration_byte=0, animation_selector=5, replacement_allowed_byte=1),
                    action76=dict(callback=0, completion_mode=3, duration_byte=0, animation_selector=28),
                    observed_bytes='Actualinitializeroutputcopiedafterraw/preference/AIentry; nohosttablepatch.'),
                dispatcher=dict(address=0x496AA0,
                    action80_path='Zero duration -> mode!=3 -> nonnullcallback -> EAX1 -> returnwithoutclearingaction. Animationflags/count/modelpointerarenotcompletiongatesonthisselectedpath.',
                    return_meaning='EAX1doesnotmeansacrificeorcompletion;action80persists. All11originalcallbackRETepiloguesassignEAX1.',
                    controls='Originalaction76mode3setscompletionbitsandclearsactiononanimationend. Explicitoriginal4966A0clears80; neithercontrolkillsCreature.'),
                checked_outcomes=dict(ritual='NativeAIfromrawUnhappy28->274->6->275/80->HP900->905; forcedcompletionstatusdoesnotstop80, HPcancontinue910->915.',
                    signal_scope='ExplicitruntimeF1/3AD/3AEandoptionalDC0conditions; notactualrenderedanimationevents or GPU execution.',
                    ordinary_clock='104preserves905,105heals910;105/110and200/205retainregisteredlivingparticipant/action80withHP915.',
                    wrap_control='MAX/0/4withactiondispatchonlyretainsregistration/action80. SameclockwithAIreselects28atclock0, state274handlerrejectsoccupiedCryptwithoutDancersandAIexits1;HP915/aliveregistrypreserved/action80persists.',
                    wrap_scope='Diagnosticclock/mood/preferenceconditions;notnormalterminationduration,norproofofalivegamebug/exploit.',
                    sacrifice='Nointrinsicanimation-endconsumptioninverifiedCryptchain. ActualTemple472930HP0/historypathisaseparatepreviouslyverifiedconsumer;naturalWorldlifetime/allinterruptionsnotproved.'),
                clock_wrap_evidence=dict(state_writes=writes, Unhappy_choice_clock_writes=choices, callback_failure_CALL=[0x4772B7,0x476D30]),
                native_executions=manifest, independent_Ghidra_controls=controls, primary=proof,
                previous_Temple_counterexample=link('temple-creature-entry-semantics.json'),
                preservation=link('phase165-source-baseline.json'),
                helpers=[link(name) for name in ['probe_crypt_action_completion.py','emulate_crypt_action_completion.py',
                                                'crosscheck_crypt_action_completion.py','prepare_crypt_completion_primary.py','crosscheck_crypt_waiting.py']],
                priority=dict(current='Crypt groups', block_complete=False,
                              next=['Actualconsumersof10unknownCryptownercounters.',
                                    'OthernaturalAIinterruptions/actionreplacementandfullWorldschedule; salary/experienceunproved.']),
                limitations=['Sourcefiles,gameEXEandparalleleditorunchanged; controlledmemorycopiesonly.',
                             'Preparedmap/catalogues/NAV/nullrenderer/clock/moodmode1; thisisnotfullWorldscheduler.',
                             'Noclaimofimmortality,norabsenceofallpossibledeath/sacrificepathsorallversions.',
                             '8passivegroupsand10activeeffectsarenot18assignedpassivecounterconsumers;10remainunknown.',
                             'Broader15familygoalremainsactive.'])


NOTE = 'crypt_action_continuation_semantics phase189:11native/7Ghidra/66snapshots. Real80callbackreturns1andduration0/mode0bypassesanimationcompletion;76mode3endsunderthesamesignals. ExplicitstopandclockwrapAIreselectioncontrolsdistinct;livingparticipantpreserved.3new+235inheritedspans/18686PC;166priorcomponents/source/codec preserved;10counterreadersandfullWorldlifetimeopen.'


def main():
    package = load('editor-format-package.json'); snapshot = ROOT/'phase189-prior-package-state.json'
    if not snapshot.exists():
        assert len(package['components']) == 166
        dump(snapshot.name, dict(components=deepcopy(package['components']), codec=link('dk2_formats.py'),
                                package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
                                preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old = load(snapshot.name); schema = build(); dump(SCHEMA, schema)
    assert set(package['components']) in [set(old['components']), set(old['components'])|{KEY}]
    for k,v in old['components'].items(): assert package['components'][k] == v
    for k,h in old['preserved_top_level'].items(): assert canonical(package[k]) == h
    package[KEY] = schema; package['components'][KEY] = link(SCHEMA)
    if NOTE not in package['precedence']: package['precedence'].append(NOTE)
    dump('editor-format-package.json', package)
    dump('phase189-search-status.json', dict(phase=189, goal_status='active', current_block='Crypt groups', block_complete=False,
         previous_goal_turn=dict(classification='no_progress', reason='Userquestionansweredfromexisting187/188evidence;nonewformatmeaningorpublication.Thatclarificationleftactualactioncompletionasnextsafetest.'),
         current_turn=dict(classification='progress', reason='Original80/76completioncausalityanddiagnosticAIexitverifiedin11nativeand7Ghidrascenes,publishedwithPEattestation.'),
         counts=schema['counts'], remaining=schema['priority']['next'], evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
