"""Verify CA native evidence and lossless replacement across supplied files."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_dead_bodies import *
from emulate_dead_bodies import cases,build_case,raw_record
from build_dead_bodies import primary,native_data,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('dead-body-semantics.json');assert s['primary_program_bytes']==primary() and s['native_data']==native_data() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==74
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    boundaries=0
    for value in [0,1,99,100,255,256,300,65535]:
        for level in [0,1,10,255]:
            raw=raw_record(value=value,level=level);v=record_view(raw);assert v['editor_health_word']==value and v['level']==level and intermediate(raw)[16:]==payload(raw)
            projection=runtime_projection(raw,20,60);assert projection==dict(position=[14336,18432,0],creature_id=7,level=level,angle=0,decay_counter=1200);boundaries+=1
            for off,n in FIELD_WIDTHS.items():
                new=patch_raw(raw,off,(1<<(n*8))-1);o=8+off;assert new[:o]==raw[:o] and new[o+n:]==raw[o+n:] and int.from_bytes(new[o:o+n],'little')==(1<<(n*8))-1;boundaries+=1
    for value,want in [(0,0),(99,99),(100,100),(255,100),(256,0),(300,44),(65535,100)]:assert editor_health(value)==want;boundaries+=1
    for integer,want in [(0,0),(99,99),(100,100),(255,100),(256,0),(300,44),(6553,100)]:assert editor_health(99,1,integer)==want;boundaries+=1
    try:editor_health(99,1)
    except ValueError:boundaries+=1
    else:raise AssertionError('Legacy conversion must be explicit')
    for raw in [b'',raw_record()[:-1],raw_record()+b'X',struct.pack('<II',201,16)+raw_record()[8:],struct.pack('<II',202,15)+raw_record()[8:]]:
        try:payload(raw)
        except ValueError:boundaries+=1
        else:raise AssertionError('Invalid envelope accepted')
    patches=0;files=0;records=0
    for row in s['corpus']['files']:
        p=Path(row['file']);b=p.read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;files+=1
        for si,section in enumerate(parts):
            if section.signature!=190:continue
            running=0
            for ri,raw in enumerate(section.records):
                if fmt.u32(raw)==202:
                    records+=1;record_view(raw)
                    for off,n in FIELD_WIDTHS.items():
                        for value in [0,(1<<(8*n))-1]:
                            rows=list(section.records);rows[ri]=patch_raw(raw,off,value);replacement=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);replacement.replace_records(rows)
                            packed=b''.join(replacement.pack() if j==si else q.pack() for j,q in enumerate(parts));want=bytearray(b);o=section.offset+len(section.header)+running+8+off;want[o:o+n]=value.to_bytes(n,'little');assert packed==want and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    assert records==12
    instructions=0
    for evidence in s['evidence']:
        lines=(ROOT/'ghidra'/evidence['file']).read_text().splitlines();assert all(l in lines for l in evidence['instructions']);instructions+=len(evidence['instructions'])
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=49 and package['dead_body_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=38,date='2026-10-26',native_emulations=74,native_case_kinds=dict(Counter(c['kind'] for c in cases())),legacy_callback_cases_with_explicit_integer_fixture=9,boundary_cases=boundaries,corpus_dead_bodies=records,exact_in_memory_container_patches=patches,whole_thing_files_roundtrip=files,primary_instructions=instructions,primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase38-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'},ensure_ascii=True))
if __name__=='__main__':main()
