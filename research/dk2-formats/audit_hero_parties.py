"""Verify C9 primary bytes, native comparisons, corpus and lossless edits."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_hero_parties import *
from emulate_hero_parties import build_case,cases,fixture_record
from build_hero_parties import primary
from party_writer_stack_audit import inspect
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('hero-party-semantics.json');assert schema['primary_program_bytes']==primary() and schema['native_writer']['stack_audit']==inspect();boundary=0
    for party in range(256):
        raw=fixture_record(party=party);v=record_view(raw);mid=loader_intermediate(raw);w=world_store_projection(bytes([0xCC])*520,raw)
        assert v['party_id']==party and mid[110]==party and w['world_offset']==0x4030+party*520 and w['slot'][3:8]==bytes([0xCC])*5;boundary+=1
    for n in [264,268,300,555]:
        raw=fixture_record(n);p=canonical_payload(raw,b'ExternalName\0');assert len(p)==555 and len(loader_intermediate(raw,b'ExternalName\0'))==631;boundary+=1
    try:canonical_payload(fixture_record(268))
    except ValueError:boundary+=1
    else:raise AssertionError('268 cannot yield a file-only name')
    for mask in range(256):
        raw=bytearray(fixture_record());raw[8+43+28]=7;raw[8+43+15]=mask;models=editor_member_fields(bytes(raw));f=models[0]['flags']
        assert f==(mask if mask&6 else mask|2) and models[0]['level']==0 and models[0]['party_id']==3;boundary+=1
    evidence={};instructions=0
    for e in schema['evidence']:
        p=ROOT/'ghidra'/e['file'];lines=p.read_text().splitlines();assert all(s in lines for s in e['instructions']);instructions+=len(e['instructions']);evidence[e['file']]=digest(p.read_bytes())
    assert len(schema['native_emulations'])==len(cases())==50
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    files=[];sizes=Counter();reserved=Counter();names=Counter();occupancy=Counter();ids=Counter();empty_nonzero=0;sparse=0;member_parties=Counter();total=0
    for f in load('corpus-scan.json')['files']:
        if 'sections' not in f or not any(s['family']=='Things' for s in f['sections']):continue
        b=(DATA/'maps'/f['file']).read_bytes();assert digest(b)==f['sha256'];n=0
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for raw in s.records:
                if fmt.u32(raw,0)!=201:continue
                v=record_view(raw);p=raw[8:];sizes[len(p)]+=1;n+=1;total+=1;ids[v['party_id']]+=1
                if len(p)==555:
                    reserved[p[35:43].hex()]+=1;names[p[:32].hex()]+=1;occupied=v['occupied_slots'];occupancy[len(occupied)]+=1
                    if occupied!=list(range(len(occupied))):sparse+=1
                    for i,m in enumerate(v['members']):
                        slot=p[43+32*i:75+32*i]
                        if not m['creature_id'] and any(slot):empty_nonzero+=1
                        if m['creature_id']:member_parties[(v['party_id'],m['party_id'])]+=1
        files.append(dict(file=f['file'],sha256=f['sha256'],party_records=n))
    patches=0;appends=0
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,s in enumerate(sections):
            if s.signature!=190:continue
            running=0
            for ri,raw in enumerate(s.records):
                if fmt.u32(raw,0)==201 and len(raw)==563:
                    edits=[]
                    for off,n in [(32,2),(34,1),(35,4),(39,4)]:
                        for value in [0,(1<<(n*8))-1]:edits.append((patch_raw(raw,off,value),off,n,value))
                    for slot in range(16):
                        for off,n in [(8,4),(12,2),(14,1),(15,1),(16,4),(20,4),(29,1),(30,1),(31,1)]:
                            for value in [0,(1<<(n*8))-1]:edits.append((patch_member(raw,slot,off,value),43+32*slot+off,n,value))
                    for new,off,n,value in edits:
                        records=list(s.records);records[ri]=new;changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records(records)
                        packed=b''.join(changed.pack() if j==si else p.pack() for j,p in enumerate(sections));want=bytearray(original);absolute=s.offset+len(s.header)+running+8+off;want[absolute:absolute+n]=value.to_bytes(n,'little')
                        assert packed==want and b''.join(p.pack() for p in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
            for size in [264,268,300,555]:
                extra=fixture_record(size);changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records([*s.records,extra]);packed=b''.join(changed.pack() if j==si else p.pack() for j,p in enumerate(sections));parts=fmt.parse(packed)
                assert parts[si].records[-1]==extra and parts[si].records[:-1]==s.records and len(packed)==len(original)+len(extra)
                assert fmt.u32(changed.header,20)==len(s.records)+1 and fmt.u32(changed.header,8)==fmt.u32(s.header,8)+len(extra)
                assert fmt.u32(changed.header,len(changed.header)-4)==fmt.u32(s.header,len(s.header)-4)+len(extra)
                assert all(part.pack()==sections[j].pack() for j,part in enumerate(parts) if j!=si);appends+=1
    inputs=[]
    for f in load('validation.json')['files']:
        p=DATA/f['file']
        if not p.exists():p=DATA/'maps'/f['file']
        assert digest(p.read_bytes())==f['sha256'];inputs.append(f['file'])
    exes=load('executable-hash-verification.json')
    for f,sha in exes.items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=43 and package['hero_party_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=32,date='2026-10-26',boundary_cases=boundary,native_emulations=50,native_case_kinds=dict(Counter(c['kind'] for c in cases())),exact_in_memory_container_patches=patches,in_memory_appends=appends,
        corpus=dict(thing_files=len(files),party_records=total,payload_sizes=dict(sizes),raw_party_ids=dict(sorted(ids.items())),reserved_counts=dict(sorted(reserved.items())),distinct_name_raw32=len(names),occupied_slot_counts=dict(sorted(occupancy.items())),sparse_parties=sparse,empty_slots_with_nonzero_raw=empty_nonzero,
            nonempty_member_party_pairs=[dict(header=a,member=b,count=n) for (a,b),n in sorted(member_parties.items())],file_details=files),
        primary_evidence=dict(instructions=instructions,sha256=evidence,live_program_ranges_matching_pe=len(schema['primary_program_bytes']),unchanged_disassemblies=len(schema['primary_program_bytes'])),
        unchanged_inputs=inputs,unchanged_executables=exes,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase32-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','native_case_kinds','exact_in_memory_container_patches','in_memory_appends']}))
    print('Corpus:',total,'parties;',sizes,'occupied',occupancy,'sparse',sparse,'nonzero empty',empty_nonzero,'nonzero reserved',total-reserved['0000000000000000']);print('Evidence:',instructions,'instructions;46 live ranges match PE;17 inputs/2 EXEs unchanged')
if __name__=='__main__':main()
