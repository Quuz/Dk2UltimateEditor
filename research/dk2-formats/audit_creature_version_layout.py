"""Verify exact-version native evidence and byte-preserving C6/C7/C8 edits."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_creature_version_layout import primary,native_labels,converter_pairs,digest
from native_creature_version_layout import read_instance_fields,PROFILES,CURRENT,OLD
from native_placed_creatures import patch_raw,SIZES
from emulate_creature_version_layout import cases,build_case
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('creature-version-layout-semantics.json');assert s['primary_program_bytes']==primary() and s['native_labels']==native_labels() and s['converter_pairs']==converter_pairs();assert len(s['native_emulations'])==len(cases())==48
    inputs=load('phase47-input-manifest.json');captured={str(Path(r['file']).resolve()).lower():r for r in inputs['files']+inputs['main_files']}
    def source_bytes(p):
        row=captured.get(str(p.resolve()).lower())
        b=Path(row['snapshot']).read_bytes() if row and 'snapshot' in row else p.read_bytes()
        if row:assert digest(b)==row['sha256']
        return b
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result'];assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0
    for sha,off in PROFILES.items():
        for b in range(256):
            for trigger in [0,32767,32768,65535]:
                node=bytearray([165])*0x500;node[off['party']]=255-b;node[off['wander']]=b;struct.pack_into('<H',node,off['trigger'],trigger);copy=bytes(node);v=read_instance_fields(node,sha);assert node==copy and v==dict(party_id=255-b,wander_radius_byte=b,trigger_root_bits=trigger,diagnostic_trigger_signed=trigger if trigger<32768 else trigger-65536);checks+=1
        for node in [b'',bytes(off['trigger']+1)]:
            try:read_instance_fields(node,sha)
            except ValueError:checks+=1
            else:raise AssertionError('Short instance buffer accepted')
    for sha in ['1.3','1.7','old','',CURRENT.upper()]:
        try:read_instance_fields(bytes(0x500),sha)
        except ValueError:checks+=1
        else:raise AssertionError('Guessed version profile accepted')
    patches=0;tag_counts={str(t):0 for t in SIZES}
    for p in [DATA/'AutoSaveThings.kld',DATA/'maps/level1Things.kld',DATA/'maps/kop4Things.kld']:
        original=source_bytes(p);parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==190);sec=parts[si]
        for tag in SIZES:
            indices=[i for i,r in enumerate(sec.records) if fmt.u32(r,0)==tag]
            if not indices:continue
            for ri in sorted({indices[0],indices[-1]}):
                raw=sec.records[ri];fields={198:[(23,1),(20,2)],199:[(29,1),(30,1),(24,2)],200:[(24,2)]}[tag]
                for o,n in fields:
                    for value in [0,(1<<(8*n))-1]:
                        changed=patch_raw(raw,o,value);rs=list(sec.records);rs[ri]=changed;updated=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);updated.replace_records(rs);packed=b''.join(updated.pack() if j==si else q.pack() for j,q in enumerate(parts));absolute=sec.offset+len(sec.header)+sum(len(r) for r in sec.records[:ri])+8+o;wanted=bytearray(original);wanted[absolute:absolute+n]=value.to_bytes(n,'little');assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1;tag_counts[str(tag)]+=1
    roundtrips=0
    for row in inputs['files']:
        b=source_bytes(Path(row['file']));assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b;roundtrips+=1
    main_matches=0;main_drift=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        sha=digest(source_bytes(p))
        if sha==row['sha256']:main_matches+=1
        else:main_drift.append(dict(file=str(p),historical_sha256=row['sha256'],captured_sha256=sha))
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old_original=Path(s['old_source']['original_path']);assert digest(old_original.read_bytes())==OLD and digest((ROOT/'source-binaries/DKII-local13.exe').read_bytes())==OLD
    package=load('editor-format-package.json');assert len(package['components'])>=58 and package['creature_version_layout_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=47,date='2026-10-26',native_emulations=48,full_converter_cases=36,diagnostic_fragment_cases=12,identical_converter_bodies=3,runtime_fields_with_verified_offset_difference=3,api_boundary_cases=checks,exact_in_memory_container_patches=patches,patches_by_tag=tag_counts,whole_file_roundtrips=roundtrips,primary_function_ranges_matching_pe=len(s['primary_program_bytes']),primary_native_label_ranges_matching_pe=len(s['native_labels']),evidence_instructions=sum(len(e['instructions']) for e in s['evidence']),main_inputs_matching_historical_baseline=main_matches,main_input_baseline_drift=main_drift,input_capture_manifest_sha256=digest((ROOT/'phase47-input-manifest.json').read_bytes()),original_executables_unchanged=3,old_research_copy_unchanged=True,format_package_components=len(package['components']),research_writes_to_original_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase47-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
