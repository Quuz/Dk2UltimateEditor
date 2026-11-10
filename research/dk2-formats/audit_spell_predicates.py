"""Audit native predicates, initializer bytes, corpus patches and package links."""
from pathlib import Path
import json
from build_spell_predicates import ROOT,load,digest,primary,corpus
from emulate_spell_predicates import cases,build,CATEGORIES
from spell_state_tables import initialized_states
from native_spell_predicates import eligible,effective_category,patch_predicate

def main():
    s=load('spell-predicate-semantics.json');functions,blocks=primary()
    assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges']
    assert len(functions)==16 and sum(r['executed'] for r in functions)==11 and len(blocks)==3
    table,states=initialized_states();assert len(table)==7025 and len(states)==281
    assert digest((ROOT/'phase78-initial-ai-state-table.bin').read_bytes())==digest(table)==s['ai_state_table']['initialized_binary']['sha256']
    assert s['ai_state_table']['category22_states']==[199,200,201,202,203,251,252,259,260]
    assert s['ai_state_table']['category2_states']==[r['state'] for r in states if r['category']==2]
    canonical={c['name']:c for c in cases()};observed={}
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=canonical.pop(row['case']);args,expected=build(c);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['input']==args and saved['expected']==expected
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
        observed[row['case']]=expected
    assert not canonical and len(observed)==56
    assert observed['heal-below']['native_predicate_return']==1 and observed['heal-equal']['native_predicate_return']==0
    assert observed['heal-scaled-below']['native_predicate_return']==1 and observed['heal-scaled-equal']['native_predicate_return']==0
    assert observed['heal-negative-coefficient']['native_predicate_return']==1 and observed['heal-negative-hp']['native_predicate_return']==1
    assert observed['rule3-backup-combat']==observed['rule3-ready']==observed['rule3-state-259']==observed['backup-not-read-for-direct-category']
    assert observed['rule3-backup-transit-no-recursion']['diagnostic_category']==2 and observed['rule3-backup-transit-no-recursion']['native_predicate_return']==0
    assert observed['haste-helper-other-state']['native_predicate_return']==observed['heal-helper-other-state']['native_predicate_return']==1
    assert s['corpus']==corpus() and s['corpus']['file_count']==82 and s['corpus']['records']==3535 and s['corpus']['exact_container_patch_cases']==2248
    for row in s['source_example']['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    for rule in range(256):assert eligible(rule)==int(rule in [1,2,3,4,5])
    assert effective_category(CATEGORIES,199,0xFFFFFFFF)==22
    assert effective_category(bytes([2,2]),0,1)==2
    invalid=[lambda:eligible(-1),lambda:eligible(256),lambda:eligible(True),lambda:eligible(1,timer=65536),lambda:eligible(2,hp=-1),lambda:eligible(2,coefficient=32768),
      lambda:effective_category(CATEGORIES,281,0),lambda:effective_category(CATEGORIES,6,0xFFFFFFFF),lambda:effective_category(CATEGORIES,True,0),
      lambda:patch_predicate(bytes(266),256),lambda:patch_predicate(bytes(265),1)]
    for f in invalid:
        try:f()
        except ValueError:pass
        else:raise AssertionError('Invalid diagnostic API input accepted')
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['spell_alternative_semantics']['phase78_predicate_runtime']==s
    legacy=load('spell-semantics.json')['dispatch_tables']['creature_alternative']
    assert legacy['native_runtime_review']==dict(file='spell-predicate-semantics.json',sha256=digest((ROOT/'spell-predicate-semantics.json').read_bytes()))
    assert 'Missing definition' in legacy['out_of_range_behavior'] and 's16Coefficient' in legacy['entries'][2]['predicate']
    assert all('AI-state category' in legacy['entries'][i]['predicate'] for i in [3,4,5])
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=78,date='2026-10-28',native_compositions=56,complete_executed_functions=11,additional_static_functions=5,primary_data_ranges=3,
        actual_initialized_AI_rows=281,AI_table_bytes_compared_per_case=7025,predicate_table_bytes_compared_per_case=102,category22_states_tested=9,
        corpus_files_hashchecked=82,creature_spell_records=3535,exact_container_patch_cases=2248,API_rule_values=256,API_invalid_input_checks=len(invalid),
        source_executables_unchanged=4,package_components=67,AlternativeShotRoom_gameplay_meaning_assigned=False,whole_AI_or_gameplay_run=False,
        source_binary_or_game_data_writes=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase78-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
