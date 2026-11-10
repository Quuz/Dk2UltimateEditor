"""Audit native AP predicates, schema correction and exact trigger parameter patches."""
from pathlib import Path
from collections import defaultdict
import json,struct
import dk2_formats as fmt
from build_ap_conditions import ROOT,load,digest,primary,corpus,KINDS
from emulate_ap_conditions import cases,build_case,expected
from native_ap_conditions import compare,evaluate
def main():
    s=load('action-point-condition-semantics.json');p=primary();assert s['primary_program_bytes']==p['functions'] and s['primary_data_ranges']==p['blocks'] and s['corpus']==corpus() and len(cases())==len(s['native_emulations'])==61
    groups=defaultdict(list);special={}
    for row,c in zip(s['native_emulations'],cases()):
        path=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert digest(path.read_bytes())==row['sha256'] and c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
        m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};res=struct.unpack('<I',m[0x10061000])[0];assert res==expected(c)[0]
        assert all(a not in m for a in [0x4B0D70,0x4B0620,0x4B06D0,0x4B07E0,0x4B0960,0x4B0A00,0x4B0A80,0x508290,0x519980])
        if c['name'].startswith('core-'):groups[c['kind']].append((c['flags'],res))
        special[c['name']]=res
    assert len(groups)==7 and all(len(g)==4 and len({n for _,n in g})==1 for g in groups.values())
    assert special['claim-all-65536']==0 and special['claim-all-ignore-threshold']==1 and special['tag-all-empty-marks-0']==0 and special['tag-all-empty-marks-1']==1
    assert special['compare-1-4294967295']==0 and special['compare-4-4294967295']==1
    layouts=load('trigger-layouts.json')['0xD5']
    for kind in KINDS:assert layouts[str(kind)]['native_evidence']=='action-point-condition-semantics.json;DX4B0D70'
    for kind in [40,41]:
        row=layouts[str(kind)];assert any(f['offset']==2 and f['name']=='reserved_2' and f['size']==2 for f in row['baseline_fields']);assert any(f['offset']==2 and f['name']=='terrainId' and f['size']==1 for f in row['fields']) and any(f['offset']==3 and f['size']==1 for f in row['fields'])
    patches=roundtrips=records=apnodes=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,sec in enumerate(parts):
            if sec.signature!=210:continue
            rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                records+=1
                if fmt.u32(raw)==213 and raw[22] in KINDS:
                    apnodes+=1
                    for off,n in [(8,1),(10,1),(12,4)]:
                        changed=bytearray(raw);changed[off:off+n]=bytes(v^255 for v in raw[off:off+n]);rs=list(sec.records);rs[ri]=bytes(changed);new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(b);absolute=sec.offset+rel+off;manual[absolute:absolute+n]=changed[off:off+n]
                        assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;assert changed[11]==raw[11];patches+=1
                rel+=len(raw)
    checks=0
    for left,op,right,want in [(3,1,0xFFFFFFFF,False),(3,4,0xFFFFFFFF,True),(0x80000000,1,0,True),(0x7FFFFFFF,4,0x80000000,True),(0,0,0,False),(0,255,0,False)]:assert compare(left,op,right)==want;checks+=1
    for values in [(-1,3,0),(1,1<<32,0),(True,3,1),(1,3,'1')]:
        try:compare(*values)
        except ValueError:checks+=1
        else:raise AssertionError(values)
    for kind,params in [(-1,bytes(8)),(256,bytes(8)),(True,bytes(8)),(39,bytes(7)),(39,'12345678'),(0,bytes(8))]:
        try:evaluate(kind,params)
        except ValueError:checks+=1
        else:raise AssertionError(kind)
    for kwargs in [dict(entity_count=-1),dict(owned_count=1<<32),dict(marked_count=True),dict(possessed=1)]:
        try:evaluate(39,bytes(8),**kwargs)
        except ValueError:checks+=1
        else:raise AssertionError(kwargs)
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256'];rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==66 and pack['action_point_condition_semantics']==s and pack['trigger_parameter_layouts']==load('trigger-layouts.json')
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=56,date='2026-10-27',native_condition_dispatches=61,native_condition_types=7,complete_primary_pe_functions=12,primary_data_ranges=4,ap04_08_bounded_equal_result_groups=len(groups),api_boundary_checks=checks,whole_file_roundtrips=roundtrips,total_trigger_nodes=records,ap_condition_nodes=apnodes,exact_in_memory_parameter_patches=patches,corrected_baseline_condition_layouts=2,original_executables_unchanged=4,format_package_components=66,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    assert apnodes==1412 and patches==4236 and roundtrips==110
    (ROOT/'phase56-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
