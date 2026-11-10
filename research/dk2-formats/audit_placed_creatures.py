"""Check C6/C7/C8 evidence, native runs, raw corpus and exact in-memory edits."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_placed_creatures import *
from emulate_placed_creatures import build_case,cases,raw_record
from build_placed_creatures import native_data
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('placed-creature-semantics.json');assert schema['native_data']==native_data();boundary=0
    for base in [0,1,1000,65535]:
        for level in [0,1,2,10,255]:
            for growth in [-32768,-101,0,100,32767]:
                p=base*growth;assert -(1<<31)<=p<1<<31
                expected=base if level==1 else (p//100 if p>=0 else -((-p)//100))&0xFFFFFFFF
                assert scaled_base_bits(base,level,growth)==expected;boundary+=1
    for tag in SIZES:
        for health in [0,99,100,255,256,300,0xFFFFFFFF]:
            for gold in [0,99,100,255,256,300,65535]:
                raw=raw_record(tag,health=health,gold=gold);v=record_view(raw);assert v['health_percent_bits']==health and v['gold_percent_bits']==gold
                out=modern_editor_projection(raw);assert out['health']==min(health%256,100) and out['gold']==min(gold%256,100);boundary+=1
        for flags in range(256):
            out=runtime_flags(tag,flags,255,0xA6,0xFFFFFFFF,0x77)
            if tag==199:assert out==dict(flags=flags,extra=0xFFFFFFFF,f0=1)
            else:assert out==dict(flags=(flags&0xF9)|6,extra=0xFFFFFFFF,f0=0x77)
            boundary+=1
    evidence={};instructions=0;unchanged=[]
    for proof in schema['evidence']:
        p=ROOT/'ghidra'/proof['file'];lines=p.read_text().splitlines();assert all(s in lines for s in proof['instructions']);instructions+=len(proof['instructions']);evidence[proof['file']]=digest(p.read_bytes())
    for file in evidence:
        before=ROOT/'ghidra'/file;after=ROOT/'ghidra'/('phase31-after-'+before.stem+'.txt');assert before.read_text().rstrip()==after.read_text().rstrip();unchanged.append(file)
    assert len(schema['native_emulations'])==len(cases())==88
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        assert all(int(saved['result']['registers'][r],16)==v for r,v in expected.items())
    stats={tag:{n:Counter() for n in ['gold_percent_bits','health_percent_bits','level','flags','z_bits','wander_radius_byte','party_id','flags2','ap_bits','objective']} for tag in SIZES};totals=Counter();files=[]
    for f in load('corpus-scan.json')['files']:
        if 'sections' not in f or not any(s['family']=='Things' for s in f['sections']):continue
        b=(DATA/'maps'/f['file']).read_bytes();assert digest(b)==f['sha256'];local=Counter()
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for raw in s.records:
                tag=fmt.u32(raw,0)
                if tag not in SIZES:continue
                v=record_view(raw);local[tag]+=1;totals[tag]+=1
                for key in stats[tag]:
                    if key in v:stats[tag][key][v[key]]+=1
        files.append(dict(file=f['file'],sha256=f['sha256'],selected_records=dict(local)))
    patches=0
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,s in enumerate(sections):
            if s.signature!=190:continue
            running=0
            for ri,raw in enumerate(s.records):
                tag=fmt.u32(raw,0)
                if tag in SIZES:
                    offsets=[(8,4),(12,2),(14,1),(15,1),(20 if tag==199 else 16,4)]
                    offsets += {198:[(23,1)],199:[(16,4),(29,1),(30,1),(31,1)],200:[(20,4)]}[tag]
                    for off,n in offsets:
                        for value in [0,(1<<(n*8))-1]:
                            records=list(s.records);records[ri]=patch_raw(raw,off,value);changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records(records)
                            packed=b''.join(changed.pack() if j==si else part.pack() for j,part in enumerate(sections));expected=bytearray(original)
                            absolute=s.offset+len(s.header)+running+8+off;expected[absolute:absolute+n]=value.to_bytes(n,'little')
                            assert packed==expected and b''.join(p.pack() for p in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    inputs=[]
    for f in load('validation.json')['files']:
        p=DATA/f['file']
        if not p.exists():p=DATA/'maps'/f['file']
        assert digest(p.read_bytes())==f['sha256'];inputs.append(f['file'])
    exes=load('executable-hash-verification.json')
    for file,sha in exes.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=42 and package['placed_creature_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=31,date='2026-10-26',boundary_cases=boundary,native_emulations=len(cases()),native_case_kinds=dict(Counter(c['kind'] for c in cases())),exact_in_memory_container_patches=patches,
        corpus=dict(thing_files=len(files),record_counts=dict(sorted(totals.items())),raw_counts={t:{n:dict(sorted(c.items())) for n,c in v.items() if c} for t,v in stats.items()},file_details=files,
            scope='Direct C6/C7/C8 only. Embedded C9 party members are not counted or normalized by this audit.'),
        primary_evidence=dict(instructions=instructions,sha256=evidence,unchanged_disassembly=unchanged),unchanged_inputs=inputs,unchanged_executables=exes,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase31-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','native_case_kinds','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'Things files;',dict(totals))
    for tag in SIZES:print(tag,{n:dict(c) for n,c in stats[tag].items() if c})
    print('Evidence:',instructions,'instructions;',len(unchanged),'unchanged disassemblies;17 inputs/2 EXEs unchanged')
if __name__=='__main__':main()
