"""Audit storage/selected-melee findings while retaining all-family unknown scope."""
import copy
import json
from build_resistance import ROOT,build,load,digest,link
import emulate_resistance as native


def main():
    s=load('creature-resistance-semantics.json');assert s==build()
    p,prior=load('editor-format-package.json'),load('phase113-prior-components.json')
    assert len(p['components'])==92 and len(prior)==91
    assert set(p['components'])-set(prior)=={'creature_resistance_semantics'} and p['creature_resistance_semantics']==s
    for k,row in p['components'].items():
        blob=(ROOT/row['file']).read_bytes();assert digest(blob)==row['sha256'] and json.loads(blob)==p[k]
        if k in prior:assert row==prior[k]
    for row in s['helpers']+s['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    args,e,checks=native.probe.build(100);wanted=native.expected(100)
    r=load('ghidra/'+native.path_for(args).name)['result'];wrong=copy.deepcopy(wanted)
    native.probe.f.put(wrong[native.probe.attack.TARGET],0x40,1000)
    try:native.verify(100,args,checks,wrong,r)
    except AssertionError:pass
    else:raise AssertionError('False immunity expectation accepted')
    assert s['physical']['loaded_count']==4 and s['melee']['HP_after']==[980,980,980]
    assert not s['physical']['percentage_formula_established']
    assert 'Актуальный этап 113:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    assert 'пакет содержит 92 компонента' in (ROOT/'READINESS_RU.md').read_text(encoding='utf8')
    assert (ROOT/'RESISTANCES_RU.md').exists()
    v=dict(phase=113,date='2026-10-29',**s['counts'],package_components=92,
           prior91_components_preserved_byte_exact=True,source_executables_unchanged=4,source_Creature_files_unchanged=82,
           paired_full_context_checks=3,measured_baselines=1,rejected_false_immunity_expectation=1,
           gameplay_function_replacements=0,live_game_runs=0,all_spell_resistance_paths_verified=False,
           goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase113-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    names=['creature-resistance-semantics.json','RESISTANCES_RU.md','phase113-validation.json',
           'phase113-primary-manifest.json','phase113-resistance-emulations.json','phase113-resistance-crosschecks.json',
           'phase113-resistance-consumer-review.json','phase113-resistance-api-validation.json',
           'INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']
    status=dict(phase=113,date='2026-10-29',published_phase=113,
        status='Published/audited four-slot resistance loading and matched-type melee nonapplication; no universal spell/version claim.',
        previous_goal_turn_classification='progress: phase112 published full prepared CMap Vault chain and91component package; Crypt cross-species explanation clarified.',
        current_goal_turn_classification='progress: user resistance question led to full rawtarget loading plus real matched melee,no table reads in damage,3CPU/2Ghidra/410rebuilds.',
        pending_native_jobs=[],package_components=92,evidence=[link(n) for n in names],readiness_estimate_percent=75,
        findings=['Original converter/getters/loader preserve rawE0C fourpairs into61C/count78A.',
                  'Matched type1,resvalues0/100/255 eachproduceHP1000->980; selected attack/HP path doesnotread the table.',
                  '20directcanonical leads classified asloading/lifetime/copy orotherstructures; globalunused notproved.',
                  'Firstfalse-immunity expectation rejected; allsourcebytes preserved,prior91component schemas unchanged.'],
        next_action='Do not repeat matchedmelee3cases orbasicload storage. If followingresistances, inspect a concrete actualshot damage consumer andwhether the loadedtable canreach it; keep separate1545 Lightning immunity distinct. '
                    'Otherwise continue pendingrawfield consumers andall15families. Work113queries47B190/4C3200/475A20/475D20/470110/470470 areWIP only; manufacturecontribution47B190 reads6BD andcalls4C3200, noWorkEnhancement proof there yet. '
                    'Jump/Priority searches have nofreshlead andpriornegativewidework mustnot berepeated. Fullgame/AI/renderer remainopen.',
        goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase113-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))


if __name__=='__main__':main()
