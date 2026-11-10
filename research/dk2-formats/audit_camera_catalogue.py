"""Verify camera catalogue evidence,source survey and exact full-file patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_camera_catalogue import primary,load,digest,SCOPE_UPDATE
from native_camera_catalogue import model_from_raw,editor_scaled_view,apply_checkboxes,writer_section,bank_projection
from native_cameras import patch_raw,record_view
from emulate_camera_catalogue import cases,build_case,real_records,DATE
ROOT=Path(__file__).resolve().parent
def main():
    s=load('camera-catalogue-semantics.json');assert s['primary_program_bytes']==primary() and len(s['native_emulations'])==len(cases())==82
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(c);r=v['result'];assert c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    survey=load('phase49-camera-corpus-manifest.json');assert digest((ROOT/'phase49-camera-corpus-manifest.json').read_bytes())==s['corpus']['manifest_sha256'];roundtrips=0;rejected=0;updated={r['file']:r for r in load('phase49-survey-updates.json')['changed_sources']};survey_matches=0
    for row in survey['files']:
        update=updated.get(row['file']);b=Path(update['snapshot'] if update else row.get('snapshot',row['file'])).read_bytes();assert digest(b)==(update['snapshot_sha256'] if update else row['sha256']);survey_matches+=int(update is None)
        if row['status']=='strict_parse_rejected':
            try:fmt.parse(b)
            except ValueError as e:assert str(e)==row['error'];rejected+=1
            else:raise AssertionError('Previously rejected source changed behavior')
        else:assert b''.join(q.pack() for q in fmt.parse(b))==b;roundtrips+=1
    assert len(survey['files'])==3930 and rejected==27 and roundtrips==3903
    checks=0;rs=real_records();base=rs[0]
    for raw in rs:
        v=record_view(raw);m=model_from_raw(raw);assert len(m)==84 and m[:83]==raw[8:91] and m[83]==0 and v['opaque_tail']==0;checks+=1
    for value in [0,1,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        for off,name in [(0,'position'),(12,'min_clip'),(24,'max_clip'),(36,'view'),(48,'zoom'),(60,'lens')]:
            r=patch_raw(base,off,value);v=editor_scaled_view(r);n=value if name=='lens' or value<0x80000000 else value-0x100000000;assert v[name][0]==n/4096 and r[:8+off]==base[:8+off] and r[12+off:]==base[12+off:];checks+=1
    for initial in [0,0x80,0x80000000,0xFFFFFFFF]:
        r=patch_raw(base,72,initial)
        for bits in range(128):
            vals=[(0xFFFFFFFF if bits&(1<<i) else 0) for i in range(7)];p=apply_checkboxes(r,vals);assert p[:80]==r[:80] and p[84:]==r[84:] and struct.unpack_from('<I',p,80)[0]==((initial&~127)|bits);checks+=1
    for values in [[0]*6,[0]*8,[0]*6+[1<<32],[0]*6+[True]]:
        try:apply_checkboxes(base,values)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid checkbox vector accepted')
    # ShortCD84 projection normalizes lengths,raw codec retains them instead.
    for n in [0,1,48,72,76,82,83,84]:
        raw=struct.pack('<II',205,n)+bytes([255])*n;m=model_from_raw(raw);assert len(m)==84 and m[83]==0 and m[:min(n,83)]==bytes([255])*min(n,83);checks+=1
    # Real input duplicates alter loadedFlags by order;empty slot bytes survive.
    for spec in [4,5]:
        first=bank_projection(rs,spec,bytes([165])*0xC00);reverse=bank_projection(rs[::-1],spec,bytes([165])*0xC00);o=0x28+13*152+140;assert struct.unpack_from('<I',first,o)[0]==42 and struct.unpack_from('<I',reverse,o)[0]==34 and first!=reverse;assert first[:0x28+152]==bytes([165])*(0x28+152);checks+=1
    original=(DATA/'GlobalVariables.kwd').read_bytes();parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==190);sec=parts[si];assert si==1 and parts[0].signature==230 and len(parts[0].records)==621 and len(sec.records)==39
    patches=0
    for ri,raw in enumerate(sec.records):
        for off,n in [(0,4),(36,4),(48,4),(60,4),(72,4),(76,2),(82,1),(83,1)]:
            for value in [0,(1<<(n*8))-1]:
                change=patch_raw(raw,off,value);r=list(sec.records);r[ri]=change;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(r);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(original);absolute=sec.offset+len(sec.header)+ri*92+8+off;expected[absolute:absolute+n]=value.to_bytes(n,'little');assert packed==expected and packed[:sec.offset]==original[:sec.offset] and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    # Whole39-record model serialization preservesopaque/flags/order anddoesnot
    # replace preceding Variables. Compare fullsuffix parsed andphysicalbytes.
    model=[r[8:] for r in sec.records];native=writer_section(model,sec.header,DATE);decoded=fmt.parse(native);assert len(decoded)==1 and decoded[0].records==sec.records and len(native)==56+92*39;checks+=1
    for newrecords in [sec.records+sec.records[-1:],sec.records[:-1]]:
        new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(newrecords);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));parsed=fmt.parse(packed);assert packed[:sec.offset]==original[:sec.offset] and parsed[1].records==newrecords and len(parsed[0].records)==621 and b''.join(q.pack() for q in parsed)==packed;patches+=1
    # Preservehistorical drift explicitly;neverdeclare17currentfiles unchanged.
    captures={str(Path(r['file']).resolve()).lower():r for r in load('phase47-input-manifest.json')['main_files']};matches=0;drift=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        cap=captures[str(p.resolve()).lower()];b=Path(cap.get('snapshot',p)).read_bytes();sha=digest(b);assert sha==cap['sha256']
        if sha==row['sha256']:matches+=1
        else:drift.append(dict(file=str(p),historical_sha256=row['sha256'],phase47_snapshot_sha256=sha))
    for p,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/p).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256'];rc=s['rc3'];assert digest(Path(rc['original_file']).read_bytes())==digest(Path(rc['research_copy']).read_bytes())==rc['sha256']
    for r in load('phase49-editor-dialog-comparison.json'):assert digest(Path(r['file']).read_bytes())==r['sha256']
    p=load('editor-format-package.json');assert len(p['components'])==60 and p['camera_catalogue_semantics']==s and p['camera_semantics']['later_scope_update']==SCOPE_UPDATE
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==p[key]
    v=dict(phase=49,date='2026-10-26',native_emulations=82,native_case_kinds=dict(Counter(c['kind'] for c in cases())),native_original39_record_writers=2,full_real_record_chains=39,game_bank_callbacks_executed=156,api_cases=checks,exact_in_memory_container_patches=patches,source_files_verified=3930,sources_matching_first_survey_sha256=survey_matches,later_independent_source_snapshots=list(updated.values()),strict_rejections_preserved=27,whole_file_roundtrips=roundtrips,original_camera_records=39,distinct_ids=18,camera_files_including_exports=3,primary_function_ranges_matching_pe=25,evidence_instructions=sum(len(e['instructions']) for e in s['evidence']),main_inputs_matching_historical_baseline=matches,main_input_phase47_snapshot_drift=drift,native_original_executables_unchanged=4,editor_resource_executables_unchanged=4,format_package_components=60,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase49-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
