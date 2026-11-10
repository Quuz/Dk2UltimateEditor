"""Check Level scratch semantics, corpus variation and exact in-memory patches."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_level_descriptor import *
from emulate_level_descriptor import cases,build_case,fixture
from build_level_descriptor import primary
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('level-descriptor-semantics.json');assert schema['primary_program_bytes']==primary();boundaries=0
    for b in range(256):
        for count in [0,1,2]:
            actual=ansi_to_wide(bytes([b]),count);value=b if b<128 else b|0xFF00
            assert actual==struct.pack('<H',value)+bytes(2*max(0,count-1));assert ansi_copy(bytes([b]),count)==bytes([b])+bytes(max(0,count-1));boundaries+=2
    model,strings,players,refs,ids,source=fixture({});raw=writer_payload(bytes(SIZE),model,strings,players)
    for b in range(256):
        for midseed in [0,165]:
            changed=bytearray(raw)
            for o,n in OPAQUE:changed[o:o+n]=bytes([b])*n
            changed[0x63CA]=b;seed=bytes([midseed])*MID_SIZE;mid=intermediate(bytes(changed),seed)
            for o,n in OPAQUE:assert mid[MID_BASE+o:MID_BASE+o+n]==bytes([midseed])*n
            assert mid[MID_BASE+0x63CA]==b;boundaries+=1
    for size in [25603,SIZE]:
        for o,n in WRITER_UNASSIGNED:
            before=bytes((i*37+19)&255 for i in range(size));after=patch_raw(before,o,bytes(n));assert after[:o]==before[:o] and after[o+n:]==before[o+n:];boundaries+=1
    evidence={};instructions=0
    for e in schema['evidence']:
        p=ROOT/'ghidra'/e['file'];lines=p.read_text().splitlines();assert all(s in lines for s in e['instructions']);instructions+=len(e['instructions']);evidence[e['file']]=digest(p.read_bytes())
    assert len(schema['native_emulations'])==len(cases())==38;generated=0
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
        if saved['case']['kind']=='writer':
            regions=json.loads(args['memory'])['regions'];file=bytes.fromhex(next(x for x in regions if int(x['address'],16)==0x10030000)['hex']);parts=fmt.parse(file)
            assert b''.join(s.pack() for s in parts)==file and len(parts)==1 and fmt.u32(parts[0].header,len(parts[0].header)-4)==SIZE
            n,k=struct.unpack_from('<HH',file,20);assert len(file)==0x6493+72*n+4*k;generated+=1
    sizes=Counter();history=Counter();unknown2=Counter();unique=Counter();counts=Counter();details=[];patches=0
    paths=[(DATA/'AutoSave.kwd',None)]
    for f in load('corpus-scan.json')['files']:
        if 'sections' in f and any(s['family']=='Level' for s in f['sections']):paths.append((DATA/'maps'/f['file'],f['sha256']))
    for path,sha in paths:
        b=path.read_bytes()
        if sha:assert digest(b)==sha
        parts=fmt.parse(b)
        for si,s in enumerate(parts):
            if s.signature!=220:continue
            df_size=fmt.u32(s.header,len(s.header)-4);raw=s.records[0][:df_size];tail=s.records[0][df_size:];h=raw[0x1184:0x138C];word=struct.unpack_from('<H',raw,0x63C1)[0];byte=raw[0x63CA];sizes[len(raw)]+=1;history[digest(h)]+=1;unknown2[word]+=1;unique[byte]+=1;counts['history_nonzero']+=any(h);counts['opaque_word_nonzero']+=word!=0;counts['unique_byte_nonzero']+=byte!=0
            details.append(dict(file=str(path.relative_to(DATA)),sha256=digest(b),payload_bytes=len(raw),opaque520_sha256=digest(h),opaque520_nonzero_bytes=sum(v!=0 for v in h),opaque_word=word,unique_byte=byte))
            for off,n in WRITER_UNASSIGNED:
                for value in [0,165,255]:
                    new=patch_raw(raw,off,bytes([value])*n);records=list(s.records);records[0]=new+tail;changed=fmt.Section(s.offset,s.signature,s.header,records,s.suffix);packed=b''.join(changed.pack() if j==si else q.pack() for j,q in enumerate(parts));want=bytearray(b);o=s.offset+len(s.header)+off;want[o:o+n]=bytes([value])*n
                    assert packed==want and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for f in load('validation.json')['files']:
        p=DATA/f['file']
        if not p.exists():p=DATA/'maps'/f['file']
        assert digest(p.read_bytes())==f['sha256'];inputs.append(f['file'])
    exes=load('executable-hash-verification.json')
    for f,sha in exes.items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=46 and package['level_descriptor_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=35,date='2026-10-26',boundary_cases=boundaries,native_emulations=len(cases()),native_case_kinds=dict(Counter(c['kind'] for c in cases())),native_generated_files_parsed_exactly=generated,exact_in_memory_container_patches=patches,
        corpus=dict(level_files=len(details),includes_standalone_autosave=True,payload_sizes=dict(sizes),counts=dict(counts),distinct_opaque520_buffers=len(history),opaque_word_values=dict(sorted(unknown2.items())),unique_byte_values=dict(sorted(unique.items())),file_details=details),
        primary_evidence=dict(instructions=instructions,sha256=evidence,live_program_ranges_matching_pe=len(schema['primary_program_bytes']),unchanged_disassemblies=len(schema['primary_program_bytes'])),unchanged_inputs=inputs,unchanged_executables=exes,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase35-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','native_case_kinds','native_generated_files_parsed_exactly','exact_in_memory_container_patches']}));print('Corpus:',len(details),'Level;',dict(sizes),dict(counts),'history buffers',len(history),'unique bytes',dict(unique));print('Evidence:',instructions,'instructions;',len(schema['primary_program_bytes']),'ranges match PE;17 inputs/2 EXEs unchanged')
if __name__=='__main__':main()
