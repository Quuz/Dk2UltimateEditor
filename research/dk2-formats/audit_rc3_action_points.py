"""Verify phase51 primary bytes, native cases and lossless HeroGate08 patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_rc3_action_points import load,primary,dialogs,digest,ROOT
from native_rc3_action_points import MASKS,CONTROL_IDS,panel_fields,apply_panel,patch_hero_gate
from native_action_points import record_view
from emulate_rc3_action_points import cases,build_case
from emulate_action_points import raw_record
def main():
    s=load('rc3-action-point-semantics.json');assert s['primary_program_bytes']==primary() and s['profiles']==dialogs()
    assert len(s['native_emulations'])==len(cases())==156
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(c);r=v['result']
        assert c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0
    for f in range(256):
        v=panel_fields(f,65535);assert v['wait']==65535 and all(x==int(bool(f&m)) for x,m in zip(v['checkboxes'],MASKS));checks+=1
        for pat in range(32):
            cs=[int(bool(pat&(1<<i))) for i in range(5)];o=apply_panel(f,0x1234FFFF,cs,[0,7,255],2);expected=(f&0x86)|sum(m for m,x in zip(MASKS,cs) if x==1)
            assert o==dict(flags=expected,wait_word=65535,next_id=255);checks+=1
        for hi in [0,0x100,0x8000,0xFF00]:
            raw=raw_record(flags=hi|f)
            for enabled in [False,True]:
                b=patch_hero_gate(raw,enabled);wanted=bytearray(raw);struct.pack_into('<H',wanted,26,(hi|f)|8 if enabled else (hi|f)&~8)
                assert b==wanted and len(b)==len(raw) and record_view(b)['flags']==struct.unpack_from('<H',wanted,26)[0];checks+=1
    for checks5 in [[2,0xFFFFFFFF,1,0,1],[1,0,2,1,0xFFFFFFFF]]:
        o=apply_panel(255,65536,checks5,[0],0);assert o['flags']==0x86|sum(m for m,x in zip(MASKS,checks5) if x==1) and o['wait_word']==0;checks+=1
    for size in range(20):
        try:patch_hero_gate(raw_record(size=size),True)
        except ValueError:checks+=1
        else:raise AssertionError('Absent raw flags accepted')
    for raw in [b'',struct.pack('<II',198,56)+bytes(56),struct.pack('<II',197,57)+bytes(57)]:
        try:patch_hero_gate(raw,True)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid raw accepted')
    for enabled in [0,1,None,'true']:
        try:patch_hero_gate(raw_record(),enabled)
        except ValueError:checks+=1
        else:raise AssertionError('Nonbool accepted')
    for index in [-1,1,255]:
        try:apply_panel(0,0,[0]*5,[0],index)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid selection accepted')
    corpus=[];patches=0;records=0;flag_counts=Counter();with08=0
    for row in load('phase47-input-manifest.json')['files']:
        path=Path(row.get('snapshot',row['file']));b=path.read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b
        count=0
        for si,sec in enumerate(parts):
            if sec.signature!=190:continue
            rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                if fmt.u32(raw,0)==197:
                    count+=1;records+=1;v=record_view(raw);flag_counts[f"0x{v['flags']:04X}"]+=1;with08+=bool(v['flags']&8)
                    if len(raw)>=28:
                        for enabled in [False,True]:
                            changed=patch_hero_gate(raw,enabled);rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs)
                            packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(b);absolute=sec.offset+rel+26;expected[absolute:absolute+2]=changed[26:28]
                            assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                rel+=len(raw)
        corpus.append(dict(file=row['file'],read_from=str(path),sha256=digest(b),action_points=count))
    assert len(corpus)==110
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==s['profiles']['rc3']['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==62 and package['rc3_action_point_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=51,date='2026-10-26',native_emulations=len(cases()),native_case_kinds=dict(Counter(c['kind'] for c in cases())),primary_function_ranges_matching_pe=len(s['primary_program_bytes']),api_boundary_cases=checks,exact_in_memory_container_patches=patches,whole_file_roundtrips=len(corpus),action_points=records,action_points_with_bit08=with08,raw_flag_counts=dict(flag_counts),corpus=corpus,original_executables_unchanged=4,format_package_components=62,research_writes_to_game_data=0,modified_gameplay_runs=0,input_scope='110 phase47 files, using independent snapshots where present. No claim all live AutoSaves match historical hashes.',limitations=s['limitations'])
    (ROOT/'phase51-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k not in ['corpus','limitations','raw_flag_counts']},ensure_ascii=False))
if __name__=='__main__':main()
