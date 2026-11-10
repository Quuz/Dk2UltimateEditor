"""Validate CB native evidence, all corpus instances and exact in-memory patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_effect_generators import *
from emulate_effect_generators import cases,build_case,raw_record
from build_effect_generators import primary,native_data,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('effect-generator-semantics.json');assert s['primary_program_bytes']==primary() and s['native_data']==native_data() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==62
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    boundaries=0
    for frequency in range(256):
        for tps in [0,1,20,0xFFFFFFFF]:
            raw=raw_record(frequency=frequency);v=runtime_projection(raw);v['deadline']=100;out=tick_projection(v,101,tps,[0,0,0,0]);assert out['deadline']==(101+frequency*tps)&0xFFFFFFFF and out['clock_calls']==2 and out['effect']==21;boundaries+=1
    for seed in [0,165,255]:
        raw=raw_record(seed);mid=intermediate(raw,bytes([seed])*76);assert mid[-6:]==bytes([seed])*6 and payload(raw)[30:]==bytes([seed])*6
        for off,n in FIELD_WIDTHS.items():
            for value in [0,(1<<(n*8))-1]:
                changed=patch_raw(raw,off,value);o=8+off;assert changed[:o]==raw[:o] and changed[o+n:]==raw[o+n:] and int.from_bytes(changed[o:o+n],'little')==value;boundaries+=1
    for raw in [b'',raw_record()[:-1],raw_record()+b'X',struct.pack('<II',202,36)+raw_record()[8:],struct.pack('<II',203,35)+raw_record()[8:]]:
        try:payload(raw)
        except ValueError:boundaries+=1
        else:raise AssertionError('Invalid raw record accepted')
    patches=0;roundtrips=0;records=0
    for row in s['corpus']['files']:
        p=Path(row['file']);b=p.read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,section in enumerate(parts):
            if section.signature!=190:continue
            running=0
            for ri,raw in enumerate(section.records):
                if fmt.u32(raw)==203:
                    records+=1;record_view(raw)
                    for off,n in FIELD_WIDTHS.items():
                        for value in [0,(1<<(8*n))-1]:
                            changed=patch_raw(raw,off,value);rows=list(section.records);rows[ri]=changed;replacement=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);replacement.replace_records(rows)
                            packed=b''.join(replacement.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(b);o=section.offset+len(section.header)+running+8+off;expected[o:o+n]=value.to_bytes(n,'little');assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    assert records==80
    instructions=0
    for evidence in s['evidence']:
        lines=(ROOT/'ghidra'/evidence['file']).read_text().splitlines();assert all(l in lines for l in evidence['instructions']);instructions+=len(evidence['instructions'])
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=48 and package['effect_generator_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=37,date='2026-10-26',native_emulations=62,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=boundaries,corpus_generators=records,exact_in_memory_container_patches=patches,whole_thing_files_roundtrip=roundtrips,primary_instructions=instructions,primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase37-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'},ensure_ascii=True))
if __name__=='__main__':main()
