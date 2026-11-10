"""Audit primary bytes, explicit source/CFG scope and preservation invariants."""
from pathlib import Path
import json
from build_object_indexed_review import ROOT,load,digest,primary


def main():
    s = load('object-indexed-consumer-review.json')
    functions,blocks = primary()
    assert s['primary_code_extents']==functions and s['primary_code_and_data_ranges']==blocks
    review = load('phase70-indexed-origin-review.json')
    assert s['indexed_origin_review']==review
    assert len(review['reads'])==17 and all(r['reached_alias_states']==[True] for r in review['reads'])
    assert sum(r['states'] for r in review['functions'])==4883
    assert sum(r['recovered_instruction_count'] for r in review['functions'])==130
    assert sum(len(r['indirect_branch_stops']) for r in review['functions'])==0
    scan = load('phase70-old-object-field-candidates-v2.json')
    assert s['search']['sha256']==digest((ROOT/s['search']['file']).read_bytes())
    assert len(scan['candidates'])==146 and len(scan['novel_address_candidates'])==24
    assert len([r for r in scan['novel_address_candidates'] if r['bytes']==2 and r['index']])==17
    assert len(s['remaining_anchored_candidates'])==6
    for r in review['reads']:
        fn = next(f for f in functions if f['address']==r['function'])
        assert fn['address']<=r['read']<fn['address']+fn['length']
    files = 0
    for row in s['corpus']['files']:
        assert digest(Path(row['file']).read_bytes())==row['sha256']
        files += 1
    assert files==84
    for file,sha in load('executable-hash-verification.json').items():
        assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old = load('creature-version-layout-semantics.json')['old_source']
    assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3 = load('camera-catalogue-semantics.json')['rc3']
    assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack = load('editor-format-package.json')
    assert len(pack['components'])==67 and pack['object_property_semantics']['phase70_indexed_review']==s
    for key,row in pack['components'].items():
        b = (ROOT/row['file']).read_bytes()
        assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v = dict(phase=70,date='2026-10-27',old_text_decoded_instructions=780881,candidate_reads=146,new_address_candidates=24,
             indexed_WORD_origins_verified=17,CFG_states=4883,recovered_switch_instructions=130,resolved_computed_jumps=2,
             unresolved_indirect_jumps_in_reviewed_contexts=0,primary_code_extents=11,primary_code_and_data_ranges=3,
             canonical_prefix_false_leads=1,other_new_anchored_candidates_open=6,source_files_hashchecked=84,
             source_executables_unchanged=4,package_components=67,new_native_execution_cases=0,new_raw_patch_cases=0,
             research_writes_to_game_data=0,modified_gameplay_runs=0,Objects34C_gameplay_still_open=True,limitations=s['limitations'])
    (ROOT/'phase70-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))


if __name__=='__main__':
    main()
