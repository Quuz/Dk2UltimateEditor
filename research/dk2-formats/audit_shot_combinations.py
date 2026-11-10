"""Final package, evidence and preservation audit of phase168 publication."""
from pathlib import Path
from copy import deepcopy
import json
import build_shot_combinations as build
ROOT=Path(__file__).resolve().parent
load=build.load
sha=build.sha

def main():
    package=load('editor-format-package.json');s=load(build.SCHEMA);old=load('phase168-prior-package-state.json')
    assert len(package['components'])==146 and len(old['components'])==145
    assert package[build.KEY]==s and package['components'][build.KEY]==build.link(build.SCHEMA)
    for k,row in old['components'].items():
        assert package['components'][k]==row
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    for k,h in old['preserved_top_level'].items():assert build.canonical(package[k])==h,k
    effective=deepcopy(package['effective_shot_parameters']);extra=effective.pop('combination_rules')
    assert effective==old['effective_shot_parameters'] and extra['source']==build.link(build.SCHEMA) and not extra['block_complete']
    assert build.link('dk2_formats.py')==old['codec']
    for row in s['helpers']:assert build.link(row['file'])==row
    for row in s['native_executions']['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        saved=load(row['file']);assert saved['case']==row['case']
        assert saved['result']['success'] and saved['result']['hit_return'] and not saved['result']['emulation_error']
        assert not saved['result']['protected_write_or_execution_failures']
    for row in s['independent_Ghidra_controls']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        d=load(row['file']);assert not d['tool_is_error'] and d['result']['success'] and d['result']['hit_return']
    assert s['primary']==load('phase168-shot-combinations-primary.json') and s['primary']['all_executed_original_PCs_covered']
    assert s['storage_validation']==load('phase168-shot-combinations-storage.json')
    for row in s['preservation']['source_files']+s['preservation']['executable_paths']:
        assert sha(Path(row['file']).read_bytes())==row['sha256'],row['file']
    status=load('phase168-search-status.json')
    assert status['goal_status']=='active' and status['block']=='ProcessType/DamageType' and not status['block_complete']
    assert len(s['block_remaining']['observed_mixed_pairs_without_new_scenes'])==12
    for row in status['evidence']:assert build.link(row['file'])==row
    d=dict(passed=True,phase=168,package_components=146,preserved_prior_components=145,counts=s['counts'],
        block_complete=False,goal_status='active',priority='ProcessType/DamageType -> Crypt -> globals -> creature resistances/flags -> other',
        package=build.link('editor-format-package.json'),schema=build.link(build.SCHEMA),source_writes=0)
    build.dump('phase168-validation.json',d);print(json.dumps(d['counts']))

if __name__=='__main__':main()
