"""Verify full native retreat evidence, AP patches and published components."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from build_ap_retreat_semantics import ROOT,load,digest,primary,link
from emulate_ap_retreat import cases,build_case,B,CAP
from native_action_points import patch_raw,record_view
from build_effect_semantics import pe_read

def main():
    s=load('ap-retreat-semantics.json');functions,blocks=primary()
    assert s['primary_program_bytes']==functions and s['primary_data_and_fragments']==blocks and len(functions)==2 and len(blocks)==8
    assert [(r['address'],r['length']) for r in functions]==[(0x4611D0,1654),(0x517490,7)]
    data=Path(functions[0]['exe']).read_bytes();assert digest(data)==s['executable_sha256']
    assert pe_read(data,0x46122C,4)==bytes.fromhex('f6473201')
    assert pe_read(data,0x461236,8)==bytes.fromhex('668b473a668b4f38')
    # Both tracking-source instructions read displacement1A,not1E.
    assert pe_read(data,0x4617FB,3)==bytes.fromhex('8b571a')
    assert pe_read(data,0x46180B,3)==bytes.fromhex('8b481a')
    memories={}
    for c,row in zip(cases(),s['native_emulations']):
        p=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result']
        assert digest(p.read_bytes())==row['sha256'] and v['case']==json.loads(json.dumps(c)) and v['input']==args and v['expected_registers']==want and r['success'] and r['hit_return'],c['name']
        assert all(int(r['registers'][k],16)==n for k,n in want.items())
        mem={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};memories[c['name']]=mem
        for f in functions:assert all(a+len(b)<=f['address'] or a>=f['address']+f['length'] for a,b in mem.items())
    assert len(cases())==len(s['native_emulations'])==23
    def cap(name):return memories[name][B+0x11000]
    def n(b,o):return struct.unpack_from('<I',b,o)[0]
    baseline=cap('flags-1')
    for name in ['flags-5','flags-9','flags-13','flags-61']:
        assert cap(name)==baseline and memories[name][B+0x2000]==memories['flags-1'][B+0x2000]
    for name in ['flags-0','flags-4','flags-8','flags-12','empty-list','creature-gate-off']:
        assert n(cap(name),0)==n(cap(name),12)==n(cap(name),24)==0
    assert n(cap('first-reachable'),0)==2 and n(cap('first-reachable-order'),0)==1
    assert cap('end-bounds-ignored')==baseline
    assert n(cap('second-move-success'),12)==2 and n(cap('all-moves-fail'),24)==0
    assert n(cap('state-change-rejected'),8)==1 and n(cap('state-change-rejected'),12)==0
    for name in ['flags-1','negative-entity-position']:
        b=memories[name][B+0x2000];assert b[0x52A:0x52C]==b[0x52C:0x52E] and n(b,0x52E)==123456
    checks=19
    files=records=patches=0
    for row in s['corpus']['files']:
        source=Path(row['input_path']).read_bytes();assert digest(source)==row['sha256'];parts=fmt.parse(source);assert b''.join(q.pack() for q in parts)==source;files+=1
        seen=0
        for si,sec in enumerate(parts):
            if sec.signature!=190:continue
            relative=len(sec.header)
            for ri,raw in enumerate(sec.records):
                if fmt.u32(raw)==197:
                    view=record_view(raw);seen+=1;records+=1
                    for mask in [1,4,8]:
                        value=view['flags']^mask;changed=patch_raw(raw,18,value);rr=list(sec.records);rr[ri]=changed
                        new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr)
                        packed=b''.join(new.pack() if i==si else q.pack() for i,q in enumerate(parts));manual=bytearray(source);at=sec.offset+relative+8+18;manual[at:at+2]=value.to_bytes(2,'little')
                        assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                relative+=len(raw)
        assert seen==row['records']
    assert files==106 and records==3501 and patches==10503
    # All original EXEs unchanged;sourcehashes independently stored beforephase64.
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];rc=load('camera-catalogue-semantics.json')['rc3']
    assert digest(Path(old['original_path']).read_bytes())==old['sha256'] and digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    assert s['inherited_gui_evidence']==link('action-point-editor-semantics.json') and s['read_search_review']==link('memory-read-policy.json')
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['action_point_semantics']['phase64_retreat']==s
    for file in ['unresolved-field-traces.json','creature-version-layout-semantics.json']:assert load(file)['phase64_search_review']==link('memory-read-policy.json')
    for key,row in pack['components'].items():
        raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==pack[key]
    v=dict(phase=64,date='2026-10-27',native_scenarios=23,complete_native_retreat_calls=23,complete_executed_native_functions=2,full_primary_pe_functions=2,primary_instruction_fragments=7,primary_native_source_labels=1,read_policy_native_calibrations=7,old_creature_test_checks_restored=21,ap_control_test_reads_restored=2,descriptor_candidates=396,semantic_comparison_groups=checks,source_files_roundtripped=files,action_points=records,exact_in_memory_flag_patches=patches,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase64-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))
if __name__=='__main__':main()
