"""Audit phase112 without inflating isolated CMap execution into a full World event."""
import copy
import json
from build_vault_map import ROOT,build,load,digest,link
import emulate_vault_map as native


def main():
    s=load('vault-map-semantics.json');assert s==build()
    package,prior=load('editor-format-package.json'),load('phase112-prior-components.json')
    assert len(package['components'])==91 and len(prior)==90
    assert set(package['components'])-set(prior)=={'vault_map_semantics'} and package['vault_map_semantics']==s
    for k,row in package['components'].items():
        blob=(ROOT/row['file']).read_bytes();assert digest(blob)==row['sha256'] and json.loads(blob)==package[k]
        if k in prior:assert row==prior[k]
    for row in s['helpers']+s['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert not s['unresolved']['ObjectManaGen']['resolved']
    expected={r['case']:r['expected'] for r in s['native_replays']}
    assert expected['claim-existing']['mana']==expected['claim-object-manaffff']['mana']==1220
    assert expected['claim-existing']['other_player_ceiling']==400
    assert expected['same-owner-cap80']['player_ceiling']==80 and expected['same-owner-cap80']['mana']==1080
    assert expected['claim-edited']['mana']==1245 and expected['claim-signed']['mana']==1119
    assert expected['revert-to-unclaimed']['actual_owner']==8
    # A plausible but false additive ceiling expectation must fail whole-context verification.
    c=next(c for c in native.cases() if c['name']=='same-owner-cap80')
    args,e,checks,wanted=native.build(c);row=next(r for r in s['native_replays'] if r['case']==c['name'])
    r=load('ghidra/'+row['file'])['result'];wrong=copy.deepcopy(wanted)
    native.f.put(wrong[native.f.PLAYER],0x10DE,180)
    try:native.verify(c,args,e,checks,wrong,r)
    except AssertionError:pass
    else:raise AssertionError('Incorrect same-owner ceiling accepted')
    # External import code is an explicit boundary, not permission for arbitrary stubs.
    bad=copy.deepcopy(args);mem=json.loads(bad['memory'])
    import_row=next(x for x in mem['regions'] if int(x['address'],16)==native.probe.imported.ADDRESS)
    import_row['hex']='90'+import_row['hex'][2:];bad['memory']=json.dumps(mem)
    try:native.cpu.run(native.EXE,bad,[],native.f.OBS,e['new_node'],393)
    except AssertionError:pass
    else:raise AssertionError('Modified external adapter accepted')
    assert 'Актуальный этап 112:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 91 компонент' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'VAULT_MAP_RU.md').exists()
    v=dict(phase=112,date='2026-10-29',**s['counts'],package_components=91,
           prior90_components_preserved_byte_exact=True,unchanged_source_executables=4,
           unchanged_source_Object_files=82,unchanged_source_Terrain_files=1,
           original_source_writes=0,gameplay_function_replacements=0,IAT_patches=0,
           rejected_additive_ceiling_expectation=1,rejected_modified_external_adapter=1,
           live_game_runs=0,full_World_runs=0,goal_achieved=False,significant_unknowns_remain=True,
           limitations=s['limitations'])
    (ROOT/'phase112-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    names=['vault-map-semantics.json','VAULT_MAP_RU.md','phase112-validation.json','phase112-primary-manifest.json',
           'phase112-vault-map-emulations.json','phase112-vault-map-crosschecks.json',
           'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']
    status=dict(phase=112,date='2026-10-29',published_phase=112,
        status='Published/audited complete prepared CMap Vault replacement, HP0 lifetime stage and sameCPU mana; all-family semantics remain incomplete.',
        previous_goal_turn_classification='progress: Crypt gameplay explanation, cross-species Vampire recipient evidence and primary caller bindings were saved; main goal remained active.',
        current_goal_turn_classification='progress: 7 whole CMap/mana chains, 13 factories, 2 independent Ghidra whole-context controls and91-component evidence package.',
        package_components=91,pending_native_jobs=[],evidence=[link(n) for n in names],readiness_estimate_percent=75,
        findings=['Whole44FC40 selected ordinary-mode ground route executes; originalZ0 floor/RTTI path givesZ1000h.',
                  'Old Vault vslot20 is4B2AD0: only HP0; separate new allocation preserves old registration until later scheduling.',
                  '455B90 changes oldowner income only; same-owner4559D0 doesnot add a new ceiling. No global gameplay-defect claim.',
                  'FFFF ObjectManaGen not read on selected path; its purpose and Terrain183/1B5 remain unresolved.'],
        next_action='Do not repeat7map chains or pursue fullWorld scheduling just to inflate coverage. Pivot to concrete remaining raw-field consumers using prior bounded instruction review: '
                    'Creature JumpEE0->75C/PriorityF55->6EB/scalars113114, Objects34C, Terrain198224,Trap204 orDoor236. '
                    'Use exact instruction operands/pointer provenance ratherthan broadtext hits. Prioritize new field meaning over more closed-route arithmetic. '
                    'Crypt18groups/8assigned clarification is saved inCRYPT_EXPLAINED_RU.md; allAIentry/liveeffects stillunverified. '
                    'All15families and source-preserving/editor-independent goal remain active.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase112-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))


if __name__=='__main__':main()
