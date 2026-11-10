"""Publication audit of bounded original area process/damage combinations."""
from copy import deepcopy
from pathlib import Path
import json
import build_shot_area_combinations as build
ROOT=Path(__file__).resolve().parent
load=build.load
sha=build.sha

def main():
    package=load('editor-format-package.json');s=load(build.SCHEMA);old=load('phase169-prior-package-state.json')
    assert len(package['components'])==147 and len(old['components'])==146
    assert package[build.KEY]==s and package['components'][build.KEY]==build.link(build.SCHEMA)
    for k,row in old['components'].items():
        assert package['components'][k]==row and sha((ROOT/row['file']).read_bytes())==row['sha256']
    for k,h in old['preserved_top_level'].items():assert build.canonical(package[k])==h,k
    effective=deepcopy(package['effective_shot_parameters'])
    extension=effective['combination_rules'].pop('area_extension')
    assert effective==old['effective_shot_parameters'] and extension['source']==build.link(build.SCHEMA)
    assert extension['hostile_Jack_pending'] and not extension['block_complete']
    assert build.link('dk2_formats.py')==old['codec']
    for row in s['helpers']:assert build.link(row['file'])==row
    for row in s['native_executions']['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        d=load(row['file']);r=d['result']
        assert d['case']==row['case'] and r['success'] and r['hit_return'] and not r['emulation_error']
        assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    for row in s['independent_Ghidra_controls']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        d=load(row['file']);assert not d['tool_is_error'] and d['result']['success'] and d['result']['hit_return']
        assert all(int(d['result']['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    assert s['primary']==load('phase169-area-combinations-primary.json') and s['primary']['all_executed_original_PCs_covered']
    assert s['storage_validation']==load('phase169-area-combinations-storage.json')
    for row in s['preservation']['source_files']+s['preservation']['executable_paths']:
        assert sha(Path(row['file']).read_bytes())==row['sha256'],row['file']
    for row in s['observer'].values():
        if isinstance(row,dict):assert build.link(row['file'])==row
    status=load('phase169-search-status.json')
    assert status['goal_status']=='active' and status['block']=='ProcessType/DamageType' and not status['block_complete']
    for row in status['evidence']:assert build.link(row['file'])==row
    assert len(s['source_records'])==7 and len(s['block_remaining']['mixed_pairs_without_selected_scenes'])==7
    result=dict(passed=True,phase=169,package_components=147,preserved_prior_components=146,counts=s['counts'],
        block_complete=False,goal_status='active',next='HostileJackInTheBoxdamage/createCreature46pathwithinpriorityblock1',
        package=build.link('editor-format-package.json'),schema=build.link(build.SCHEMA),source_writes=0)
    build.dump('phase169-validation.json',result);print(json.dumps(result['counts']))

if __name__=='__main__':main()
