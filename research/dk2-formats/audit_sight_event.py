"""Audit real Sight event chain, new Data1 meaning and raw-byte preservation."""
from pathlib import Path
import json
from build_sight_event import ROOT,load,digest,primary,corpus
from emulate_sight_event import cases,build
from native_sight_event import describe_data1

def main():
    s=load('sight-event-semantics.json');functions,blocks=primary()
    assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges']
    assert len(functions)==11 and sum(r['executed'] for r in functions)==8 and len(blocks)==7
    canonical={c['name']:c for c in cases()};observed={}
    for row in s['native_emulations']:
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());c=canonical.pop(row['case']);args,expected=build(c);r=saved['result']
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['case']==c and saved['expected']==expected
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1).items())
        memory={int(v['address'],16):bytes.fromhex(v['hex']) for v in json.loads(args['memory'])['regions']}
        for fn in functions:assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in memory.items())
        observed[row['case']]=expected
    assert not canonical and len(s['native_emulations'])==30
    c8=[v for k,v in observed.items() if k.startswith('c8-')];assert len(c8)==6 and all(v==c8[0] for v in c8)
    assert observed['data1-00000000']['region'] is None
    assert observed['data1-00010000']['region']['radius']==0 and observed['data1-00010000']['effects']==1
    assert observed['data1-00000008']['region']['radius']==observed['data1-FFFFFFFF']['region']['radius']==7
    assert observed['wrong-owner']['processed']==1 and not observed['wrong-owner']['engine_events']
    assert observed['bridge-null']['session_count']==7 and not observed['bridge-null']['engine_events']
    assert observed['sound-time20']['sound_calls']==observed['processed-highbit']['sound_calls']==1
    assert observed['sound-high40']['sound_calls']==0 and observed['first-and-repeat20']['sound_calls']==1
    assert observed['tick-overflow']['region']['end_tick']==65533
    assert observed['session-counter-wrap']['session_count']==0
    assert len(observed['old-region']['engine_events'])==3 and observed['c8-FE0C0000']==observed['data2-ignored-here']
    assert describe_data1(0x10000)==dict(initial_event_enabled=True,event_radius_word=0,receiver_radius=0)
    for value in [-1,0x100000000,True,1.5,None,'5']:
        try:describe_data1(value)
        except ValueError:pass
        else:raise AssertionError('InvalidData1accepted')
    assert corpus()==s['corpus'] and s['corpus']['file_count']==82 and s['corpus']['records']==1066 and s['corpus']['exact_container_patch_cases']==656
    for row in s['source_example']['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    shot=load('shot-semantics.json');row=next(r for r in shot['processes'] if r['process_type']==25)
    assert row['native_event_consumer']==dict(file='sight-event-semantics.json',sha256=digest((ROOT/'sight-event-semantics.json').read_bytes()))
    assert 'radius capped at7' in row['data1'] and '65536' in row['note']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['keeper_cast_semantics']['phase77_sight_event']==s
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=77,date='2026-10-28',native_Sight_event_compositions=30,complete_executed_native_functions=8,additional_static_registration_functions=3,primary_data_ranges=7,
        C8_paired_values=6,new_process25_Data1_cell_radius_consumer=True,full_DWORD_vs_low16_boundary_confirmed=True,
        exact_in_memory_container_patches=656,Keeper_source_files_hashchecked=82,Keeper_records=1066,standalone_source_examples=2,source_executables_unchanged=4,
        api_invalid_input_checks=6,package_components=67,new_C8_meaning_assignments=0,research_writes_to_game_data=0,modified_gameplay_runs=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase77-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))

if __name__=='__main__':main()
