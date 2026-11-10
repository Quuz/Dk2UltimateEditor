"""CD synthetic native fixtures and opaque-preserving container integration."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_cameras import *
from emulate_cameras import cases,build_case,raw_record
from build_cameras import primary,native_data,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('camera-semantics.json');assert s['primary_program_bytes']==primary() and s['native_data']==native_data() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==84
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    boundaries=0
    for size in range(85):
        raw=raw_record(size);assert len(payload(raw))==84 and len(intermediate(raw))==152 and payload(raw)[:size]==raw[8:] and not any(payload(raw)[size:]);boundaries+=1
        for off,n in FIELD_WIDTHS.items():
            if off+n>size:
                try:patch_raw(raw,off,0)
                except ValueError:boundaries+=1
                else:raise AssertionError('Patch outside physical payload')
    for ident in range(256):
        raw=raw_record(ident=ident);v=preset_projection(raw);assert v['accepted']==(1<=ident<=18);boundaries+=1
    for opaque in range(256):
        raw=raw_record(opaque=opaque);assert record_view(raw)['id']==1 and record_view(raw)['opaque_tail']==opaque and intermediate(raw)==intermediate(raw_record(opaque=0));boundaries+=1
    for field in ['zoom','view','lens']:
        for value,wanted in [(0,100),(100,100),(150,150),(199,199),(200,199),(0xFFFFFFFF,100)]:assert active_delta(field,150,(value-150)&0xFFFFFFFF,100,200)==wanted;boundaries+=1
    for raw in [b'',raw_record()[:-1],struct.pack('<II',204,84)+raw_record()[8:],struct.pack('<II',205,85)+raw_record()[8:]+b'X']:
        try:payload(raw)
        except ValueError:boundaries+=1
        else:raise AssertionError('Invalid record accepted')
    originals=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b;originals+=1
    appends=0;patches=0
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        original=path.read_bytes();parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==190);section=parts[si];end_records=section.offset+len(section.header)+sum(map(len,section.records))
        for size in [0,48,83,84]:
            for opaque in [0,255]:
                raw=raw_record(size,opaque=opaque);replacement=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);replacement.replace_records([*section.records,raw]);packed=b''.join(replacement.pack() if j==si else q.pack() for j,q in enumerate(parts))
                want=bytearray(original);want[end_records:end_records]=raw
                for off,value in [(8,fmt.u32(section.header,8)+len(raw)),(20,len(section.records)+1),(len(section.header)-4,fmt.u32(section.header,len(section.header)-4)+len(raw))]:struct.pack_into('<I',want,section.offset+off,value)
                assert packed==want and b''.join(q.pack() for q in fmt.parse(packed))==packed;appends+=1
                added=fmt.parse(packed);sec=added[si]
                for off,n in FIELD_WIDTHS.items():
                    if off+n>size:continue
                    for value in [0,(1<<(8*n))-1]:
                        rows=list(sec.records);rows[-1]=patch_raw(raw,off,value);changed=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);changed.replace_records(rows);out=b''.join(changed.pack() if j==si else q.pack() for j,q in enumerate(added));expected=bytearray(packed);o=end_records+8+off;expected[o:o+n]=value.to_bytes(n,'little');assert out==expected and b''.join(q.pack() for q in fmt.parse(out))==out;patches+=1
    instructions=0
    for e in s['evidence']:
        lines=(ROOT/'ghidra'/e['file']).read_text().splitlines();assert all(l in lines for l in e['instructions']);instructions+=len(e['instructions'])
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=51 and package['camera_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=40,date='2026-10-26',native_emulations=84,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=boundaries,corpus_camera_records=0,whole_thing_files_roundtrip=originals,synthetic_in_memory_appends=appends,synthetic_in_memory_exact_patches=patches,primary_instructions=instructions,primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase40-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'},ensure_ascii=True))
if __name__=='__main__':main()
