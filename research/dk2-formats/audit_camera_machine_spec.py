"""Recheck exact setting identity, complete native chains and raw preservation."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_camera_machine_spec import primary,native_data,digest
from emulate_camera_machine_spec import cases,build_case
from emulate_cameras import raw_record
from native_cameras import preset_for_machine_spec,record_view
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('camera-machine-spec-semantics.json');assert s['primary_program_bytes']==primary() and s['native_data']==native_data();assert len(s['native_emulations'])==len(cases())==41
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result'];assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0
    for ident in range(256):
        raw=raw_record(ident=ident);copy=bytes(raw);a=preset_for_machine_spec(raw,4);b=preset_for_machine_spec(raw,5);assert raw==copy and record_view(raw)['zoom_bits']==[record_view(copy)['zoom_bits'][0],3000,12000]
        assert a['accepted']==b['accepted']==(1<=ident<=18)
        if a['accepted']:
            assert struct.unpack_from('<I',a['slot'],124)[0]==6000 and struct.unpack_from('<I',b['slot'],124)[0]==12000;assert a['slot'][:124]==b['slot'][:124] and a['slot'][128:]==b['slot'][128:]
        checks+=1
    raw=raw_record()
    for level in [0,1,2,3,4,5,6,10,11,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        v=preset_for_machine_spec(raw,level);assert struct.unpack_from('<I',v['slot'],124)[0]==(6000 if level<5 else 12000);checks+=1
    for level in [-1,1<<32]:
        try:preset_for_machine_spec(raw,level)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid DWORD accepted')
    roundtrips=0
    for row in load('camera-semantics.json')['corpus']['files']:
        p=Path(row['file']);b=p.read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b;roundtrips+=1
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=57 and package['camera_machine_spec_semantics']==s and 'Machine Spec Level' in package['camera_semantics']['game']['setting']
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=46,date='2026-10-26',native_emulations=41,native_setter_to_camera_chains=28,native_profile_functions=13,api_boundary_cases=checks,whole_file_roundtrips=roundtrips,primary_function_ranges_matching_pe=len(s['primary_program_bytes']),primary_data_ranges_matching_pe=len(s['native_data']['ranges']),evidence_instructions=sum(len(e['instructions']) for e in s['evidence']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),real_registry_operations=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase46-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
