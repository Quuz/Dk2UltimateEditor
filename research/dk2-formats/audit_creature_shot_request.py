"""Audit pre-allocation request observations without claiming a spawned Shot."""
from pathlib import Path
import json
from build_creature_shot_request import ROOT,load,digest,link,primary,corpus
from emulate_creature_shot_request import cases,build
from native_creature_shot_request import waiting,rotated_origin,patch_slot
def main():
    s=load('creature-shot-request-semantics.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_vslot_data_ranges']
    assert len(functions)==10 and sum(r['executed'] for r in functions)==8 and len(blocks)==3
    canonical={c['name']:c for c in cases()};observed={}
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=canonical.pop(row['case']);args,expected=build(c);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['input']==args and saved['expected']==expected
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
        observed[row['case']]=expected
    assert not canonical and len(observed)==52
    for root in ['4982b0','4984d0']:
        one=lambda k:observed[root+'-'+k]
        assert one('base')==one('alternative-zero')==one('alternative68')==one('alternative255')
        assert one('base')['native_return']==0 and one('base')['factory_calls']==1 and one('base')['shot_id']==17
        assert one('deadline-equal')['native_return']==1 and one('deadline-equal')['factory_calls']==0
        assert one('deadline-wrap-wait')['native_return']==1 and one('deadline-wrap-ready')['factory_calls']==1
        assert one('slot256-is-not-slot0')['factory_calls']==one('spell-missing')['factory_calls']==0
        assert one('shot-zero')['factory_calls']==1 and one('shot-zero')['shot_exists']==0
        assert one('shot255')['shot_id']==255 and one('shot255')['shot_exists']==1
        assert one('slot-flag-high-only')['actor_flags3EA']&0x1000==0 and one('slot-flag-clears-existing')['actor_flags3EA']&0x1000==0
        assert not any(v['allocation_reached'] for k,v in observed.items() if k.startswith(root))
    c=corpus();assert c==s['corpus'] and c['source_file_roles']==164 and c['creature_records']==3734 and c['creature_spell_records']==3535 and c['exact_container_patch_cases']==2961
    assert s['alternatives']['prior_loader_review']==link('spell-catalogue-validation.json')
    assert digest(Path(s['source_example']['file']).read_bytes())==s['source_example']['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    assert waiting(5,0,5,0) and not waiting(6,0,5,0) and not waiting(0,0,5,0x80)
    assert rotated_origin([0,0,0],[0x80000000,0x7FFFFFFF,0xFFFFFFFF],0,65536)==[0,0xFFFFFFFF,0xFFFFFFFF]
    invalid=[lambda:waiting(-1,0,0,0),lambda:waiting(0,0,0,True),lambda:rotated_origin(None,[0,0,0],0,0),lambda:rotated_origin([0,0],[0,0,0],0,0),lambda:rotated_origin([0,0,0],[0,0,0],0x100000000,0),
      lambda:patch_slot(bytes(5449),3,'x',0),lambda:patch_slot(bytes(5449),False,'x',0),lambda:patch_slot(bytes(5449),0,[],0),lambda:patch_slot(bytes(5449),0,'play_animation',256),lambda:patch_slot(bytes(5448),0,'x',0)]
    for f in invalid:
        try:f()
        except ValueError:pass
        else:raise AssertionError('Invalid binary API input accepted')
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['creature_subrecords']['phase79_cast_request']==s
    assert pack['spell_alternative_semantics']['phase79_cast_request_reference']==link('creature-shot-request-semantics.json')
    assert pack['spell_alternative_semantics']['phase78_predicate_runtime']==load('spell-predicate-semantics.json')
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=79,date='2026-10-28',native_request_compositions=52,complete_executed_functions=8,additional_static_functions=2,primary_vslots=3,
        exact_container_patch_cases=2961,creature_slot_patches=c['creature_slot_patches'],primary_shot_patches=c['primary_shot_patches'],source_file_roles=164,unique_source_files=c['unique_source_files'],
        creature_records=3734,creature_spell_records=3535,source_executables_unchanged=4,package_components=67,API_invalid_input_checks=len(invalid),
        original_code_replacements=0,newly_successful_Shot_allocations=0,AlternativeShotRoom_gameplay_meaning_assigned=False,game_data_or_parallel_editor_writes=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase79-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
