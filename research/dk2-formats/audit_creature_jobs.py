"""Audit native alias evidence, exact PE bytes and lossless package embedding."""
from pathlib import Path
import json
from build_creature_jobs import ROOT,load,digest,primary,evidence,indexed_leads
from audit_targeted_spell import executable_ranges
import emulate_creature_extension as prior
def main():
    s=load('creature-job-overlap-semantics.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges'] and len(functions)==48 and len(blocks)==1
    rows,outcomes=evidence();assert rows==s['native_emulations'] and outcomes==s['outcomes'] and indexed_leads()==s['indexed_leads']
    sources={r['program']:Path(r['exe']).read_bytes() for r in functions};clock_replacements=0
    for row in rows:
        v=load('ghidra/'+row['file']);args=v['input'];regions=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        protected=executable_ranges(sources[args['program']])+([(0x66E3EC,0x400)] if args['program']=='DKII-DX.exe' else [])
        for a,b in regions:
            if all(a+len(b)<=p or a>=p+n for p,n in protected):continue
            assert row['kind']=='writer' and a==0x4AB320
            case=v['case'];base=dict(name='phase86-writer',kind='writer',seed=case['seed'],flags=0,count=1,label_length=5,animation_update=0);old,_=prior.build_case(base)
            expected=next(bytes.fromhex(r['hex']) for r in json.loads(old['memory'])['regions'] if int(r['address'],16)==a);assert b==expected;clock_replacements+=1
    assert clock_replacements==6
    by={r['case']['name']:r['expected'] for r in outcomes}
    assert by['qz2-prince']['membership_results']==[0,0,0,1,0,0,0,0] and by['frontend-prince']['membership_results']==[0,1,0,1,0,0,0,0]
    assert by['qz2-prince']['raw_hates']==[0,0] and by['frontend-prince']['raw_hates']==[5,0]
    for n in ['qz2-prince','frontend-prince']:
        assert by[n]['raw_extra_pair']==[0,0] and by[n]['raw_alt0']==[16,0,0] and digest(Path(by[n]['source']).read_bytes())==by[n]['source_sha256']
    # Exact original instructions establish sequencing and the real rejection.
    dx=(ROOT/'ghidra/phase86-final-DX-568e30-assembly.txt').read_text();pro=(ROOT/'ghidra/phase86-final-Pro-4a7f00-assembly.txt').read_text()
    assert dx.index('00569a07: LEA EDX,[EBX + 0x14e1]')<dx.index('00569a61: MOV dword ptr [ECX + -0x4],EDI')
    assert pro.index('004a8988: CALL 0x0041e5a0')<pro.index('004a89a4: CALL 0x0041e5d0') and '004a8993: CMP ESI,0x4' in pro and '004a89b4: CMP ESI,0x3' in pro
    consumer=(ROOT/'ghidra/phase86-final-DX-4e8da0-assembly.txt').read_text();assert '004e9018: CALL 0x00494480' in consumer and '004e901f: JNZ 0x004e90f6' in consumer
    c=s['corpus'];assert c['new_exact_container_patches']==82*7*3 and c['source_files_hashchecked']==82 and c['records']==3734
    assert c['observations']==dict(nonzero_extra_pair=46,nonzero_alt0_job=19,nonzero_alt0_mood=0,nonzero_effective_hate3=19,nonzero_effective_hate4=0,effective_extra_job_not_in_first_two=19)
    for r in c['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['creature_subrecords']['phase86_job_overlap']==s
    assert package['catalogue_loading']['phase85_complete_definition']==load('creature-definition-semantics.json') and package['keeper_pipeline_semantics']['phase84_ui_order']==load('keeper-ui-semantics.json')
    for key,r in package['components'].items():
        b=(ROOT/r['file']).read_bytes();assert digest(b)==r['sha256'] and json.loads(b)==package[key]
    v=dict(phase=86,date='2026-10-28',**s['counts'],new_exact_container_patches=1722,source_files_hashchecked=82,physical_records_roundtripped=3734,source_executables_unchanged=4,package_components=67,
      indexed_expressions_classified=15,real_map_native_cases=2,synthetic_full_writers=6,emulator_clock_helper_replacements=clock_replacements,investigated_code_or_vtable_replacements=0,
      original_file_writes=0,real_game_or_editor_gui_runs=0,full_AI_assignment_runs=0,Priority_gameplay_proved=False,Jump_gameplay_proved=False,
      significant_unknowns_remain=True,goal_achieved=False,limitations=s['limitations'],codec_validation='1722exact comparisons completed once;sourcehashes revalidated byofflineaudit')
    (ROOT/'phase86-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k not in ['limitations','codec_validation']}))
if __name__=='__main__':main()
