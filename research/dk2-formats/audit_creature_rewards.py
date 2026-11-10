"""Offline audit: actual native results, exact byte preservation and integration."""
from pathlib import Path
import json
from build_creature_rewards import ROOT,load,digest,primary,native_results
from audit_targeted_spell import executable_ranges
from build_effect_semantics import pe_read
import emulate_creature_factory as factory
import emulate_sacrifice_creatures as temple
import fixture_read_probe as probe
def main():
    s=load('creature-reward-semantics.json');assert primary()==s['primary']
    fr,fo,fi=native_results(factory,'phase90-creature-factory-emulations.json','phase90-creature-factory')
    tr,to,ti=native_results(temple,'phase90-sacrifice-creature-emulations.json','phase90-sacrifice-creature')
    assert fr==s['native_factory_replays'] and tr==s['native_reward_replays'] and fo==s['factory_outcomes'] and to==s['reward_outcomes']
    assert ti['level0']==ti['level1'] and ti['level3']==ti['level3-high-bits']==ti['raw-level3'] and ti['level10']==ti['level11']==ti['level255']
    assert int.from_bytes(ti['level2-health150'][0x40:0x44],'little')==1500
    assert int.from_bytes(ti['room-full'][0x25e:0x262],'little')==1 and int.from_bytes(ti['room-full'][0x3b9:0x3bb],'little')==0
    exe=Path(s['profile']['executable']).read_bytes();assert digest(exe)==s['profile']['sha256']
    executable=executable_ranges(exe);table_blocks=s['primary']['blocks'];probes=0
    for module,rows in [(factory,fr),(temple,tr)]:
        by={c['name']:c for c in module.cases()}
        for row in rows:
            args=module.build(by[row['case']],row['page'])[0];regions=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
            assert all(a+len(b)<=p or a>=p+n for a,b in regions for p,n in executable)
            spans=sorted((a,len(b)) for a,b in regions);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
            for a,b in regions:
                for block in table_blocks:
                    start=max(a,block['address']);end=min(a+len(b),block['address']+block['length'])
                    if start<end:
                        assert block['label']=='AI-category-initial-data' and b[start-a:end-a]==pe_read(exe,start,end-start)
            assert any(a==probe.ADDRESS for a,b in regions)
            probes+=int(load('ghidra/'+row['file'])['result']['registers']['EDX'],16)
    assert probes==0 and len(fr)+len(tr)==66
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    recipes=load('temple-recipes-current.json')
    for row in recipes['catalogues']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    assert load('phase88-validation.json')['exact_container_patch_checks']==1188 and s['counts']['new_raw_patch_checks']==0
    package=load('editor-format-package.json');prior=load('phase90-prior-components.json');assert len(prior)==68 and len(package['components'])==69
    assert package['creature_reward_semantics']==s
    for key,row in package['components'].items():
        raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==package[key]
        if key in prior:assert row==prior[key]
    assert set(prior)==set(package['components'])-{'creature_reward_semantics'}
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert 'Актуальный этап 90:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8') and (ROOT/'CREATURE_REWARDS_RU.md').exists()
    v=dict(phase=90,date='2026-10-28',**s['counts'],accepted_memory_probes=probes,original_game_function_or_vtable_patches=0,
      source_executables_unchanged=4,original_game_data_written=0,live_game_runs=0,package_components=69,previous68_components_preserved_byte_exact=True,
      prior1188_patch_checks_reused=True,previous_goal_turn_classification='progress',goal_achieved=False,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase90-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))
if __name__=='__main__':main()
