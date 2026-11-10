"""Final evidence, preservation and package audit of stage170."""
from pathlib import Path
from copy import deepcopy
import json
import build_jack_in_box as build
ROOT=Path(__file__).resolve().parent
load=build.load
sha=build.sha

def main():
    p=load('editor-format-package.json');s=load(build.SCHEMA);old=load('phase170-prior-package-state.json')
    assert len(p['components'])==148 and len(old['components'])==147
    assert p[build.KEY]==s and p['components'][build.KEY]==build.link(build.SCHEMA)
    for key,row in old['components'].items():
        assert p['components'][key]==row and sha((ROOT/row['file']).read_bytes())==row['sha256']
    for key,digest in old['preserved_top_level'].items():assert build.canonical(p[key])==digest,key
    effective=deepcopy(p['effective_shot_parameters']);new=effective['combination_rules'].pop('jack_extension')
    assert effective==old['effective_shot_parameters'] and new['source']==build.link(build.SCHEMA) and not new['block_complete']
    assert build.link('dk2_formats.py')==old['codec']
    for row in s['helpers']:assert build.link(row['file'])==row
    for row in s['native_executions']['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        d=load(row['file']);r=d['result'];assert d['case']==row['case']
        assert r['success'] and r['hit_return'] and not r['emulation_error'] and not r['protected_write_or_execution_failures']
        assert r['function_or_vtable_substitutions']==0
    for row in s['independent_Ghidra_controls']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        d=load(row['file']);r=d['result'];assert not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][key],16)==v for key,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    assert s['primary']==load('phase170-jack-primary.json') and s['primary']['all_executed_original_PCs_covered']
    assert s['storage_validation']==load('phase170-jack-storage.json')
    for row in s['preservation']['source_files']+s['preservation']['executable_paths']:
        assert sha(Path(row['file']).read_bytes())==row['sha256'],row['file']
    for row in s['physical_sources']:assert sha(Path(row['file']).read_bytes())==row['file_sha256']
    status=load('phase170-search-status.json')
    assert status['goal_status']=='active' and not status['block_complete'] and status['block']=='ProcessType/DamageType'
    for row in status['evidence']:assert build.link(row['file'])==row
    assert len(s['block_remaining']['mixed_pairs_without_selected_scenes'])==7
    d=dict(passed=True,phase=170,package_components=148,preserved_prior_components=147,counts=s['counts'],
        goal_status='active',block='ProcessType/DamageType',block_complete=False,next_mixed_pairs=s['block_remaining']['mixed_pairs_without_selected_scenes'],
        package=build.link('editor-format-package.json'),schema=build.link(build.SCHEMA),source_writes=0)
    build.dump('phase170-validation.json',d);print(json.dumps(d['counts']))

if __name__=='__main__':main()
