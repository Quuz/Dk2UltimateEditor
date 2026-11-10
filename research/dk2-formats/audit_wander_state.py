"""Audit scoped native wandering evidence, corpus inventory and package hashes."""
from pathlib import Path
from collections import Counter
import json
from build_wander_state import ROOT,load,digest,primary
from emulate_wander_state import cases,build
from native_wander_state import candidates,DIRS,JITTER,rand

def main():
    s=load('wander-state-semantics.json');functions,blocks=primary()
    assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges']
    assert len(functions)==9 and sum(r['executed'] for r in functions)==5 and len(blocks)==3
    assert next(r for r in functions if r['address']==0x480A00)['length']==1217
    canonical={c['name']:c for c in cases()};observed={};queries=moves=0
    for row in s['native_emulations']:
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());c=canonical.pop(row['case']);args,expected=build(c);r=saved['result']
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['case']==c and saved['expected']==expected
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
        observed[row['case']]=expected;queries+=len(expected['queries']);moves+=expected['movement_attempts']
    assert not canonical and len(s['native_emulations'])==18
    assert observed['basic']==observed['placement255']
    assert observed['radius0']['queries']==[] and len(observed['basic']['queries'])==4
    assert observed['home-north-radius0']['queries']==[[10,9]]
    assert observed['expired-equal']['seed']==observed['unsigned-expired']['seed']==1
    assert observed['query-negative']['queries']==observed['query-blocked']['queries'] and observed['query-negative']['movement_attempts']==0
    default=cases()[0];args={k:default[k] for k in ['seed','tick','deadline','position','home','radius','blocked']}
    invalid=[dict(args,radius=-1),dict(args,seed=True),dict(args,position=[0]),dict(args,home=[0,0,0]),dict(args,blocked=[0]),dict(args,blocked=[0,0,0,0x100000000])]
    for c in invalid:
        try:candidates(**c)
        except ValueError:pass
        else:raise AssertionError('Invalidinputaccepted')
    corpus=load(s['corpus']['file']);assert digest((ROOT/s['corpus']['file']).read_bytes())==s['corpus']['sha256']
    assert corpus['file_count']==len(corpus['files'])==109 and not corpus['nonzero_instances']
    assert corpus['radius_byte_counts']==dict(Neutral={'0':512},Good={'0':6067},C9_occupied={'0':5768},C9_empty={'0':6744})
    for row in corpus['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    exposure=load(s['exposure']['file']);assert digest((ROOT/s['exposure']['file']).read_bytes())==s['exposure']['sha256']
    assert [(len(p['candidates']),len(p['context_AF_writers'])) for p in exposure['profiles']]==[(24,8),(23,9)]
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['placed_creature_semantics']['phase75_wander_state']==s
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=75,date='2026-10-27',native_compositions=18,complete_executed_native_functions=5,additional_static_native_functions=4,primary_data_ranges=3,
        recorded_world208_queries=queries,failed_native_movement_attempts=moves,api_invalid_input_checks=6,
        exposure_candidates_DX=24,exposure_candidates_old=23,source_Things_files_hashchecked=109,standalone_radius_placements=6579,occupied_C9_radius_slots=5768,empty_C9_slots=6744,
        nonzero_corpus_radius_bytes=0,source_executables_unchanged=4,package_components=67,new_raw_patch_cases=0,new_placement_radius_unit_assignments=0,
        research_writes_to_game_data=0,modified_gameplay_runs=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase75-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))

if __name__=='__main__':main()
