"""Audit placed Trap/Door native rules, original runs and raw Things preservation."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_placed_structures import *
from emulate_placed_structures import build_case
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    schema=load('placed-structure-semantics.json');boundaries=0
    for value in range(256):
        t=trap_placement(value);assert t==dict(activate=value!=0,queue_manufacture=value==0,quantity_forwarded=False);boundaries+=1
        d=door_placement(value,0x77);assert d['blueprint']==(value&2)//2 and d['lock_requested']==bool(value&1)
        assert d['projected_state']==(3 if value&3==1 else 0x77) and d['editor_flags']==value&3;boundaries+=1
    for value in [0,1,3,0x7FFFF,0x80000,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        assert center_q12(value)==(value*4096+2048)%0x100000000;boundaries+=1
    for tag in [195,196]:
        raw=struct.pack('<II',tag,SIZES[tag])+bytes(SIZES[tag]);v=record_view(raw)
        assert v['position_q12']==[2048,2048,0];boundaries+=1
        for off in ([8,12,15] if tag==195 else [8,16]):
            width=4 if off==8 else 1;n=0xFFFFFFFF if width==4 else 255;new=patch_raw(raw,off,n)
            assert new[:off+8]==raw[:off+8] and new[off+8+width:]==raw[off+8+width:];boundaries+=1
    hashes={};excerpts=0;instructions=0
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                p=ROOT/'ghidra'/v['file'];lines=p.read_text().splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(s in lines for s in v['instructions']);instructions+=len(v['instructions'])
                hashes[v['file']]=digest(p.read_bytes())
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(schema)
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        for reg,value in expected.items():assert int(saved['result']['registers'][reg],16)==value
    assert len(schema['native_emulations'])==28
    unchanged=[]
    for name in ['thing-trap','thing-door','thing-trap-activate','thing-door-blueprint','thing-door-state','thing-trap-converter','thing-door-converter','door-flags-encoder','door-flags-decoder']:
        before=ROOT/'ghidra'/('phase29-'+name+'-assembly.txt');after=ROOT/'ghidra'/('phase29-'+name+'-after.txt')
        assert before.read_text().rstrip()==after.read_text().rstrip();unchanged.append(before.name)
    values={tag:Counter() for tag in SIZES};opaque={tag:Counter() for tag in SIZES};tails={tag:Counter() for tag in SIZES};totals=Counter();files=[]
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family']=='Things' for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];local=Counter()
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for raw in s.records:
                tag=fmt.u32(raw,0)
                if tag not in SIZES:continue
                v=record_view(raw);local[tag]+=1;totals[tag]+=1;values[tag][v['raw_shots'] if tag==195 else v['raw_flags']]+=1;opaque[tag][v['opaque_position_word']]+=1
                tails[tag][f'{v["opaque_flags"]:02x}' if tag==195 else v['opaque_tail']]+=1
        files.append(dict(file=file['file'],sha256=file['sha256'],selected_records=dict(sorted(local.items()))))
    patches=0
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,s in enumerate(sections):
            if s.signature!=190:continue
            running=0
            for ri,raw in enumerate(s.records):
                tag=fmt.u32(raw,0)
                if tag in SIZES:
                    for off in ([8,12,15] if tag==195 else [8,16]):
                        width=4 if off==8 else 1
                        for value in ([0,0x12345678,0xFFFFFFFF] if width==4 else [0,1,3,255]):
                            new=patch_raw(raw,off,value);records=list(s.records);records[ri]=new
                            changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records(records)
                            packed=b''.join(changed.pack() if j==si else part.pack() for j,part in enumerate(sections));expected=bytearray(original)
                            absolute=s.offset+len(s.header)+running+8+off;expected[absolute:absolute+width]=value.to_bytes(width,'little')
                            assert packed==expected and b''.join(part.pack() for part in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=40 and package['placed_structure_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=29,date='2026-10-26',boundary_cases=boundaries,native_emulations=28,exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        corpus=dict(thing_files=len(files),file_details=files,record_counts=dict(sorted(totals.items())),selector_counts={tag:dict(sorted(v.items())) for tag,v in values.items()},
            opaque_position_counts={tag:dict(sorted(v.items())) for tag,v in opaque.items()},opaque_tail_counts={tag:dict(sorted(v.items())) for tag,v in tails.items()}),
        primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=hashes,unchanged_disassembly=unchanged),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=len(package['components']),limitations=schema['limitations'])
    (ROOT/'phase29-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'Things files;',dict(totals),'; selectors',values,'opaque tails',tails)
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(unchanged),'unchanged disassemblies')
if __name__=='__main__':main()
