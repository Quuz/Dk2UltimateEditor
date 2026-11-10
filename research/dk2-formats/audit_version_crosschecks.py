"""Audit version-specific evidence, storage boundaries and original bytes."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_version_crosschecks import load,primary,labels,rtti,equivalence,state_labels,digest,ROOT
from native_version_crosschecks import *
from native_spell_alternatives import full_intermediate,runtime_projection
from native_spell_writer import patch_raw
from emulate_version_crosschecks import cases,build_case,raw_record
def main():
    s=load('version-crosscheck-semantics.json');assert s['primary_program_bytes']==primary() and s['native_rtti_ranges']==labels() and s['action_point_rtti']==rtti() and s['whole_function_equivalence']==equivalence() and s['state_native_labels']==state_labels()
    assert len(s['native_emulations'])==len(cases())==90
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert 'versions-v2-' in row['file'] and digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(c);r=v['result'];assert c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0
    for sha in PROFILES:
        for flags in range(256):
            node=bytearray((i*29+7)&255 for i in range(64));node[0x32]=flags;before=bytes(node);segments=ap_state_segments(node,sha);assert list(map(len,segments))==[1,4,4,9] and b''.join(segments)==node[55:64]+node[46:55] and bytes(node)==before;checks+=1
            local=bytearray(range(9));local[4]=flags;changed=ap_loaded_local_state(node,local,sha);assert len(changed)==64 and changed[:46]==node[:46] and changed[55:]==node[55:] and changed[46:55]==local;checks+=1
        for pair in [(0,0),(36,4),(255,255)]:
            mid=full_intermediate(raw_record(*pair),0);node=bytes([165])*70
            for gate in [0,1,0xFFFFFFFF]:
                for tps in [0,20,0xFFFFFFFF]:
                    out=spell_projection(mid,node,tps,sha,gate);assert out==runtime_projection(mid,node,tps,0 if sha==OLD else gate) and len(out)==70;checks+=1
                    checksum=struct.unpack_from('<I',out,27)[0];assert checksum==(0xA5A5A5A5 if sha==DX and gate else sum(mid));checks+=1
            base=spell_projection(full_intermediate(raw_record(0,0),0),node,20,sha)
            out=spell_projection(mid,node,20,sha);assert out[:27]+out[31:]==base[:27]+base[31:];checks+=1
    for sha in ['1.3','1.7','old','',OLD.upper(),None,[]]:
        try:profile(sha)
        except ValueError:checks+=1
        else:raise AssertionError('Guessed executable profile accepted')
    for n in [0,9,56,63,65]:
        try:ap_state_segments(bytes(n),OLD)
        except ValueError:checks+=1
        else:raise AssertionError('Non64 runtime buffer accepted')
    for n in [0,8,10,56]:
        try:ap_loaded_local_state(bytes(64),bytes(n),DX)
        except ValueError:checks+=1
        else:raise AssertionError('Non9 state accepted')
    for gate in [-1,1<<32,True,'0']:
        try:spell_projection(bytes(346),bytes(70),20,DX,gate)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid gate accepted')
    # Physical raw alternative references remain separate from runtime checksum.
    corpus=load('spell-alternative-semantics.json')['corpus']['files'];roundtrips=0;records=0;patches=0
    for row in corpus:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,sec in enumerate(parts):
            if sec.signature!=160:continue
            records+=len(sec.records);rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                for off in [232,233]:
                    changed=patch_raw(raw,off,raw[off]^255);rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(b);expected[sec.offset+rel+off]=changed[off];assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                rel+=len(raw)
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest((ROOT/'source-binaries/DKII-local13.exe').read_bytes())==OLD
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==63 and package['version_crosscheck_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=52,date='2026-10-27',native_emulations=len(cases()),native_case_kinds=dict(Counter(c['kind'] for c in cases())),primary_function_ranges_matching_pe=len(s['primary_program_bytes']),native_rtti_ranges_matching_pe=len(s['native_rtti_ranges']),native_label_strings=len(s['state_native_labels'])*9,byte_identical_initializer_pairs=1,converter_pairs_equivalent_except_one_call=1,byte_identical_strncpy_pairs=1,api_boundary_cases=checks,whole_creature_spell_catalogue_roundtrips=roundtrips,creature_spell_records=records,exact_in_memory_container_patches=patches,original_executables_unchanged=4,format_package_components=63,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase52-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
