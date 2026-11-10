"""Validate CC first-party evidence, corpus associations and exact raw patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_placed_rooms import *
from emulate_placed_rooms import cases,build_case,raw_record
from build_placed_rooms import primary,native_data,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('placed-room-semantics.json');assert s['primary_program_bytes']==primary() and s['native_data']==native_data() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==136
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    boundaries=0
    for size in range(21):
        raw=raw_record(size);assert len(payload(raw))==20 and len(intermediate(raw))==44 and payload(raw)[:size]==raw[8:] and not any(payload(raw)[size:]);boundaries+=1
        for off,n in FIELD_WIDTHS.items():
            if off+n>size:
                try:patch_raw(raw,off,0)
                except ValueError:boundaries+=1
                else:raise AssertionError('Patch outside physical payload')
    for direction in range(256):
        raw=raw_record(terrain=37,direction=direction);out=game_projection(raw);assert out['orientation']==(direction//2 if direction in [0,2,4,6] else 0xA5A5A5A5) and out['refresh_calls']==1;boundaries+=1
    for percent,delta in [(0,-10000),(50,-5000),(99,-100),(100,0),(101,100),(300,20000)]:assert portal_delta(10000,percent)==delta&0xFFFFFFFF;boundaries+=1
    for health in [0,1,100,256,300,65535]:
        p=payload(raw_record(health=health));out=writer_portal(p);assert out[:16]==p[:16] and out[18:]==p[18:] and int.from_bytes(out[16:18],'little')==(100 if health==0 else health);boundaries+=1
    for raw in [b'',raw_record()[:-1],struct.pack('<II',203,20)+raw_record()[8:],struct.pack('<II',204,21)+raw_record()[8:]+b'X']:
        try:payload(raw)
        except ValueError:boundaries+=1
        else:raise AssertionError('Invalid record accepted')
    patches=0;roundtrips=0;records=0
    for row in s['corpus']['files']:
        p=Path(row['file']);b=p.read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,section in enumerate(parts):
            if section.signature!=190:continue
            running=0
            for ri,raw in enumerate(section.records):
                if fmt.u32(raw)==204:
                    records+=1;record_view(raw)
                    for off,n in FIELD_WIDTHS.items():
                        for value in [0,(1<<(8*n))-1]:
                            rows=list(section.records);rows[ri]=patch_raw(raw,off,value);replacement=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);replacement.replace_records(rows)
                            packed=b''.join(replacement.pack() if j==si else q.pack() for j,q in enumerate(parts));want=bytearray(b);o=section.offset+len(section.header)+running+8+off;want[o:o+n]=value.to_bytes(n,'little');assert packed==want and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    assert records==858
    instructions=0
    for evidence in s['evidence']:
        lines=(ROOT/'ghidra'/evidence['file']).read_text().splitlines();assert all(l in lines for l in evidence['instructions']);instructions+=len(evidence['instructions'])
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=50 and package['placed_room_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=39,date='2026-10-26',native_emulations=136,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=boundaries,corpus_room_records=records,exact_in_memory_container_patches=patches,whole_thing_files_roundtrip=roundtrips,primary_instructions=instructions,primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase39-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'},ensure_ascii=True))
if __name__=='__main__':main()
