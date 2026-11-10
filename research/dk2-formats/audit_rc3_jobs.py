"""Offline exact-byte/native-cache/profile audit for RC3 HateJobs overlap."""
from pathlib import Path
import json,struct
from build_rc3_jobs import ROOT,load,digest,link,primary,native_results,exact_getter_pairs
from audit_targeted_spell import executable_ranges
import emulate_rc3_hate_jobs as native
import native_creature_jobs as api
import emulate_creature_job_overlap as paired
import dk2_formats as fmt
def main():
    s=load('rc3-job-overlap-semantics.json');functions=primary();rows,out=native_results()
    assert functions==s['primary_program_bytes'] and rows==s['native_emulations'] and out==s['outcomes'] and exact_getter_pairs(functions)==s['exact_model_function_pairs']
    code=Path(s['profile']['research_copy']).read_bytes();assert digest(code)==digest(Path(s['profile']['original_file']).read_bytes())==api.RC3;protected=executable_ranges(code);clock=0
    for row in rows:
        saved=load('ghidra/'+row['file']);args=saved['input'];c=saved['case'];assert args['program']=='DK2-Editor-rc3.exe' and int(args['address'],16)==(0x4199E0 if c['kind']=='converter' else 0x4AD720)
        for r in json.loads(args['memory'])['regions']:
            a=int(r['address'],16);b=bytes.fromhex(r['hex'])
            if all(a+len(b)<=p or a>=p+n for p,n in protected):continue
            assert c['kind']=='writer' and a==0x4B0B30
            base=dict(name='phase87-native-writer-fixture',kind='writer',seed=c['seed'],flags=0,count=1);old,_=native.fixture.build_case(base)
            expect=next(bytes.fromhex(v['hex']) for v in json.loads(old['memory'])['regions'] if int(v['address'],16)==a);assert b==expect;clock+=1
    assert clock==6
    # Sequencing independently verified on the original RC3 instructions.
    converter=(ROOT/'ghidra/phase87-final-RC3-419ff0-assembly.txt').read_text();setter=(ROOT/'ghidra/phase87-final-RC3-41b180-assembly.txt').read_text();alt=(ROOT/'ghidra/phase87-final-RC3-41b1f0-assembly.txt').read_text();writer=(ROOT/'ghidra/phase87-final-RC3-4ad720-assembly.txt').read_text()
    assert converter.index('0041aa38: LEA EDI,[EBX + 0x14e1]')<converter.index('0041aa6d: CALL 0x0041b1f0')
    assert 'MOV dword ptr [ECX + EAX*0x4 + 0x1264],EDX' in setter and 'MOV dword ptr [ECX + EAX*0x8 + 0x126c],EDX' in alt
    assert writer.index('004ae199: CALL 0x00428bd0')<writer.index('004ae1b5: CALL 0x00428c00') and '004ae1a4: CMP ESI,0x4' in writer and '004ae1c5: CMP ESI,0x3' in writer
    prior=load('creature-job-overlap-semantics.json');assert link('creature-job-overlap-semantics.json')==s['cross_profile']['reference']
    matched=0
    for c in paired.cases():
        row=next(r for r in prior['native_emulations'] if r['case']==c['name']);p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());_,raw=paired.records(c)
        view=api.view(raw,api.DX if c['program']=='game' else api.PRO);assert view['final_intermediate_hate_dwords']==v['expected']['hate_mid_dwords']
        if c['program']=='game':assert bytes(view['runtime_hate_bytes']).hex()==v['expected']['game_hate_bytes_hex']
        matched+=1
    for name in ['qz2-prince','frontend-prince']:
        row=next(r for r in prior['native_emulations'] if r['case']==name);p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];e=json.loads(p.read_text())['expected'];b=Path(e['source']).read_bytes();assert digest(b)==e['source_sha256'];raw=next(r for sec in fmt.parse(b) if sec.signature==170 for r in sec.records if r[0xF4B]==2)
        assert bytes(api.view(raw,api.DX)['runtime_hate_bytes']).hex()==e['hate_bytes_hex'];matched+=1
    assert matched==16
    c=s['api_checks'];assert len(c['files'])==c['source_files_hashchecked']==82 and c['source_records_roundtripped']==3734 and c['new_api_exact_container_patch_checks']==1722 and c['profile_views']==246 and c['explicit_refusals']==6
    assert s['api']['sha256']==digest((ROOT/'native_creature_jobs.py').read_bytes()) and s['api']['profile_keys']==api.PROFILES
    for r in c['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['rc3_compatibility_semantics']['phase87_job_overlap']==s
    assert package['creature_subrecords']['phase86_job_overlap']==prior and package['catalogue_loading']['phase85_complete_definition']==load('creature-definition-semantics.json')
    base=dict(package['rc3_compatibility_semantics']);base.pop('phase87_job_overlap');assert digest(json.dumps(base,ensure_ascii=False,sort_keys=True).encode())==s['prior_rc3_schema_sha256_without_new_supplement']
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=87,date='2026-10-28',**s['counts'],new_api_exact_container_patch_checks=1722,source_files_hashchecked=82,source_records_roundtripped=3734,profile_views=246,api_matches_existing_native_cases=matched,explicit_refusals=6,
      complete_identical_model_function_pairs=4,emulator_clock_helper_replacements=clock,investigated_function_or_vtable_replacements=0,original_files_written=0,source_executables_unchanged=4,package_components=67,
      prior166_RC3_cases_rerun=0,prior22_phase86_cases_rerun=0,new_RC3_game_or_AI_runs=0,real_editor_GUI_runs=0,Priority_gameplay_proved=False,Jump_gameplay_proved=False,significant_unknowns_remain=True,goal_achieved=False,
      limitations=s['limitations'],codec_scope='1722newAPIexactchecks executedbybuilderonce;sourcehashes revalidatedhere;notrepeatedunchangedcodecchecks')
    (ROOT/'phase87-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k not in ['limitations','codec_scope']}))
if __name__=='__main__':main()
