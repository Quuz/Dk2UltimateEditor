"""Offline audit of whole Creature captures, original code and publication scope."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_creature_definition import ROOT,load,digest,primary,snapshots,diagnostics
from audit_targeted_spell import executable_ranges
import memory_readout_ymm as readout

def main():
    s=load('creature-definition-semantics.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges']
    assert len(functions)==17 and len(blocks)==1 and s['native']==snapshots() and s['indexed_diagnostics']==diagnostics()
    exe=Path(s['executable']).read_bytes();protected=executable_ranges(exe)+[(0x66E3EC,0x400)]
    code=Cs(CS_ARCH_X86,CS_MODE_32)
    for row in s['native']['replays']:
        saved=load('ghidra/'+row['file']);args=saved['input'];regions=[(int(r['address'],16),bytes.fromhex(r['hex'])) for r in json.loads(args['memory'])['regions']]
        assert all(a+len(b)<=target or a>=target+n for a,b in regions for target,n in protected)
        if row['kind']=='raw':
            driver=next(b for a,b in regions if a==0x10060000);ins=list(code.disasm(driver,0x10060000));branch=next(i for i in ins if i.mnemonic=='jb')
            assert branch.address==0x1006003E and branch.op_str=='0x1006002e'
            r=saved['result']['registers'];assert int(r['EDX'],16)==2 and int(r['ESI'],16)==0
            capture=next(v for v in s['native']['captures'] if v['case']==row['case']);assert int(r['EDI'],16)==capture['checksum']
    control=load('ghidra/phase85-wide-readout-ymm.json');r=control['result'];assert r['success'] and r['hit_return']
    pattern=next(bytes.fromhex(v['hex']) for v in json.loads(control['input']['memory'])['regions'] if int(v['address'],16)==0x10010000)
    assert readout.decode(r['registers'])==pattern[:256]
    zmm=load('ghidra/phase85-wide-readout-zmm.json');assert not zmm['result']['success'] and 'vmovdqu64' in str(zmm['result']).lower()
    failed=load('phase85-raw-driver-diagnostic.json');assert digest((ROOT/failed['old_script']).read_bytes())==failed['script_sha256']
    assert any(i['address']=='0x1006003e' and i['mnemonic']=='jb' and i['operands']=='0x1006003e' for i in failed['instructions'])
    corpus=s['corpus'];assert corpus['source_files_hashchecked']==82 and corpus['records']==3734 and corpus['new_exact_container_patches']==82*24*3
    for row in corpus['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['catalogue_loading']['phase85_complete_definition']==s
    assert package['catalogue_loading']['phase73_creature_lookup']==load('creature-lookup-review.json')
    assert package['keeper_pipeline_semantics']['phase84_ui_order']==load('keeper-ui-semantics.json')
    assert package['keeper_pipeline_semantics']['phase83_runtime_array']==load('keeper-array-semantics.json')
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=85,date='2026-10-28',**s['counts'],complete_definition_images=19,definition_bytes_per_image=2199,
      constructor_seed_dependent_bytes_observed=249,new_exact_container_patches=5904,source_files_hashchecked=82,source_records_roundtripped=3734,
      source_executables_unchanged=4,package_components=67,original_code_or_vtable_replacements=0,real_game_or_editor_launches=0,
      ghidra_gui_started=True,Priority_gameplay_purpose_proved=False,Jump_gameplay_purpose_proved=False,
      artificial_mid_index7_other_array_side_effect_verified=True,significant_unknowns_remain=True,goal_achieved=False,
      limitations=s['limitations'],codec_validation='5904 exact rebuild comparisons completed bybuild_creature_definition.py;not needlessly rerun byofflineaudit',
      excluded_experiments='Old synthetic self-jump and unsupported AVX512 are diagnostics,not successful native cases')
    (ROOT/'phase85-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in v.items() if k not in ['limitations','codec_validation','excluded_experiments']}))
if __name__=='__main__':main()
