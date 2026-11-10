"""Audit C5 semantics, native byte comparisons and lossless container edits."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_action_points import *
from emulate_action_points import build_case,cases,raw_record
from build_action_points import primary
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    schema=load('action-point-semantics.json');assert schema['primary_program_bytes']==primary();boundaries=0
    for x,y,r in [(3,4,2),(0,0,1),(0xFFFFFFFF,0xFFFFFFFF,0x80000000)]:
        v=bounds([x,y,0,r]);assert v==[(x-r)%2**32,(y-r)%2**32,(x+r)%2**32,(y+r)%2**32];boundaries+=1
    for flags in range(65536):
        assert editor_saved_flags(flags,[0,0])==flags&255 and editor_saved_flags(flags,[0,2])==(flags&255)|4;boundaries+=1
    for wait in [0,1,500,32767,32768,65535]:
        for tps in [0,1,20,100,0xFFFFFFFF]:
            raw=raw_record(wait=wait);p=runtime_projection(raw,tps);assert p['wait_ticks']==(wait*tps)%65536 and p['flags']==0x3D;boundaries+=1
    for size in range(57):
        raw=raw_record(size=size);assert len(intermediate(raw))==92 and record_view(raw)['payload_bytes']==size;boundaries+=1
    evidence={};instructions=0
    for e in schema['evidence']:
        p=ROOT/'ghidra'/e['file'];lines=p.read_text().splitlines();assert all(s in lines for s in e['instructions']);instructions+=len(e['instructions']);evidence[e['file']]=digest(p.read_bytes())
    assert len(schema['native_emulations'])==len(cases())==70
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    sizes=Counter();flags=Counter();waits=Counter();modes=Counter();names=Counter();links=Counter();ids=Counter();files=[];unresolved=[];missing=[];total=0
    for f in load('corpus-scan.json')['files']:
        if 'sections' not in f or not any(s['family']=='Things' for s in f['sections']):continue
        b=(DATA/'maps'/f['file']).read_bytes();assert digest(b)==f['sha256'];views=[]
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for raw in s.records:
                if fmt.u32(raw,0)!=197:continue
                v=record_view(raw);views.append(v);total+=1;sizes[v['payload_bytes']]+=1;flags[v['flags']]+=1;waits[v['wait']]+=1;modes[v['coordinate_mode']]+=1;ids[v['id']]+=1;links[v['next_id']]+=1;names[v['name_raw_hex']]+=1
        local_ids={v['id'] for v in views};next_ids=[v['next_id'] for v in views]
        for v in views:
            if v['next_id'] and v['next_id'] not in local_ids:missing.append(dict(file=f['file'],id=v['id'],next=v['next_id']))
            projected=editor_saved_flags(v['flags'],next_ids)
            if projected!=v['flags']:unresolved.append(dict(file=f['file'],id=v['id'],raw_flags=v['flags'],editor_flags=projected))
        files.append(dict(file=f['file'],sha256=f['sha256'],action_points=len(views)))
    patches=0
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        b=path.read_bytes();parts=fmt.parse(b)
        for si,s in enumerate(parts):
            if s.signature!=190:continue
            running=0
            for ri,raw in enumerate(s.records):
                if fmt.u32(raw,0)==197:
                    for off,n in [(0,4),(4,4),(8,4),(12,4),(16,2),(18,2),(20,2),(22,1),(23,1)]:
                        for v in [0,(1<<(8*n))-1]:
                            new=patch_raw(raw,off,v);records=list(s.records);records[ri]=new;changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records(records)
                            packed=b''.join(changed.pack() if j==si else part.pack() for j,part in enumerate(parts));want=bytearray(b);o=s.offset+len(s.header)+running+8+off;want[o:o+n]=v.to_bytes(n,'little');assert packed==want and b''.join(x.pack() for x in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    inputs=[]
    for f in load('validation.json')['files']:
        p=DATA/f['file']
        if not p.exists():p=DATA/'maps'/f['file']
        assert digest(p.read_bytes())==f['sha256'];inputs.append(f['file'])
    exes=load('executable-hash-verification.json')
    for f,sha in exes.items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=44 and package['action_point_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=33,date='2026-10-26',boundary_cases=boundaries,native_emulations=70,native_case_kinds=dict(Counter(c['kind'] for c in cases())),exact_in_memory_container_patches=patches,
        corpus=dict(thing_files=len(files),action_points=total,payload_sizes=dict(sizes),raw_flags=dict(sorted(flags.items())),raw_waits=dict(sorted(waits.items())),coordinate_modes=dict(modes),raw_ids=dict(sorted(ids.items())),next_ids=dict(sorted(links.items())),distinct_raw_names=len(names),
            raw_mask40_records=sum(n for v,n in flags.items() if v&64),missing_nonzero_next_links=missing,editor_flag_projection_changes=unresolved,file_details=files),
        primary_evidence=dict(instructions=instructions,sha256=evidence,live_program_ranges_matching_pe=len(schema['primary_program_bytes']),unchanged_disassemblies=len(schema['primary_program_bytes'])),unchanged_inputs=inputs,unchanged_executables=exes,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase33-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','native_case_kinds','exact_in_memory_container_patches']}))
    print('Corpus:',total,'AP;',sizes,'flags',flags,'geometry',modes,'editor_changes',len(unresolved),'missing links',len(missing));print('Evidence:',instructions,'instructions;',len(schema['primary_program_bytes']),'live ranges match PE;17 inputs/2 EXEs unchanged')
if __name__=='__main__':main()
