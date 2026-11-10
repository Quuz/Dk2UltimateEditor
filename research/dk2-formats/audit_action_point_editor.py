"""Verify editor AP bindings, command limits, corpus and exact raw patches."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_action_point_editor import *
from native_action_points import record_view,patch_raw
from emulate_action_point_editor import cases,build_case
from build_action_point_editor import primary
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('action-point-editor-semantics.json');assert schema['primary_program_bytes']==primary();boundaries=0
    for flags in range(256):
        fields=panel_fields(flags,65535);checks=[fields[k] for k in ['reveal','hero_lair','tool_box','ignore_solid']]
        assert apply_panel(flags,65535,checks,[0],0)==dict(flags=flags,wait_word=65535,next_id=0);boundaries+=1
        for pattern in range(16):
            checks=[int(bool(pattern&(1<<i))) for i in range(4)];p=apply_panel(flags,0xFFFF1234,checks,[0,81,255],1)
            want=(flags&0x8E)|sum(mask for i,mask in enumerate(MASKS) if pattern&(1<<i))
            assert p==dict(flags=want,wait_word=0x1234,next_id=81) and p['flags']&0x0C==flags&0x0C;boundaries+=1
    for previous in [0,165,255]:
        for free in range(1,81):
            used=[0,81,255]+[n for n in range(1,81) if n!=free];r=auto_id(used+used,previous);assert r==dict(return_byte=free,model_id=free,success=True);boundaries+=1
        assert auto_id(list(range(1,81)),previous)==dict(return_byte=0,model_id=previous,success=False);boundaries+=1
    assert apply_panel(255,0,[2,0xFFFFFFFF,1,0],[0],0)['flags']==0xAE;boundaries+=1
    for ids,current,target,want in [([7,7,3],7,7,0),([7,3,255],7,255,2),([0,7,0,255],7,0,0),([7,3,3],7,3,1)]:
        choices=next_choices(ids,current);assert selection_index(choices,target)==want;boundaries+=1
    for index in [-1,1,0xFFFFFFFF]:
        try:apply_panel(0,0,[0]*4,[0],index)
        except ValueError:pass
        else:raise AssertionError('Invalid index accepted by bounded diagnostic API')
        boundaries+=1
    evidence={};instructions=0
    for e in schema['evidence']:
        p=ROOT/'ghidra'/e['file'];lines=p.read_text().splitlines();assert all(s in lines for s in e['instructions']);instructions+=len(e['instructions']);evidence[e['file']]=digest(p.read_bytes())
    assert len(schema['native_emulations'])==len(cases())==46
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,want=build_case(saved['case']);r=saved['result']
        assert args==saved['input'] and want==saved['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==v for k,v in want.items())
    counts=Counter();flags=Counter();files=[];unusual=[];link_projection=[];total=0
    for f in load('corpus-scan.json')['files']:
        if 'sections' not in f or not any(s['family']=='Things' for s in f['sections']):continue
        b=(DATA/'maps'/f['file']).read_bytes();assert digest(b)==f['sha256'];views=[record_view(raw) for s in fmt.parse(b) if s.signature==190 for raw in s.records if fmt.u32(raw,0)==197];ids=[v['id'] for v in views]
        for v in views:
            total+=1;counts['id_zero']+=v['id']==0;counts['id_above_auto80']+=v['id']>80;counts['wait_above_ui32767']+=v['wait']>32767;counts['unknown04_set']+=bool(v['flags']&4);counts['unknown08_set']+=bool(v['flags']&8);flags[v['flags']&12]+=1
            fields=panel_fields(v['flags']&255,v['wait']);choices=next_choices(ids,v['id']);out=apply_panel(v['flags']&255,v['wait'],[fields[k] for k in ['reveal','hero_lair','tool_box','ignore_solid']],choices,selection_index(choices,v['next_id']))
            assert out['flags']==v['flags']&255 and out['wait_word']==v['wait']
            if out['next_id']!=v['next_id']:link_projection.append(dict(file=f['file'],id=v['id'],next=v['next_id'],projected=out['next_id']))
            if v['id']>80 or not v['id'] or v['wait']>32767:unusual.append(dict(file=f['file'],id=v['id'],wait=v['wait']))
        counts['files_with_duplicate_ids']+=len(ids)!=len(set(ids));files.append(dict(file=f['file'],sha256=f['sha256'],action_points=len(views)))
    counts['panel_next_projection_changes']=len(link_projection)
    counts['self_links_projected_to_none']=sum(v['id']==v['next'] and v['projected']==0 for v in link_projection)
    counts['files_with_next_projection_changes']=len({v['file'] for v in link_projection})
    patches=0
    fields={16:(2,[0,32767,32768,65535]),18:(2,[0,4,8,12,113,65535]),22:(1,[0,80,81,255]),23:(1,[0,80,81,255])}
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        b=path.read_bytes();parts=fmt.parse(b)
        for si,s in enumerate(parts):
            if s.signature!=190:continue
            running=0
            for ri,raw in enumerate(s.records):
                if fmt.u32(raw,0)==197:
                    for off,(n,values) in fields.items():
                        for value in values:
                            records=list(s.records);records[ri]=patch_raw(raw,off,value);changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records(records)
                            packed=b''.join(changed.pack() if j==si else part.pack() for j,part in enumerate(parts));want=bytearray(b);o=s.offset+len(s.header)+running+8+off;want[o:o+n]=value.to_bytes(n,'little')
                            assert packed==want and b''.join(x.pack() for x in fmt.parse(packed))==packed;patches+=1
                running+=len(raw)
    inputs=[]
    for f in load('validation.json')['files']:
        p=DATA/f['file']
        if not p.exists():p=DATA/'maps'/f['file']
        assert digest(p.read_bytes())==f['sha256'];inputs.append(f['file'])
    exes=load('executable-hash-verification.json')
    for f,sha in exes.items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=45 and package['action_point_editor_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=34,date='2026-10-26',boundary_cases=boundaries,native_emulations=len(cases()),native_case_kinds=dict(Counter(c['kind'] for c in cases())),exact_in_memory_container_patches=patches,
        corpus=dict(thing_files=len(files),action_points=total,counts=dict(counts),unknown_flags04_08=dict(sorted(flags.items())),outside_native_ui_ranges=unusual,panel_next_projection_changes=link_projection,file_details=files),
        primary_evidence=dict(instructions=instructions,sha256=evidence,live_program_ranges_matching_pe=len(schema['primary_program_bytes']),unchanged_disassemblies=len(schema['primary_program_bytes'])),unchanged_inputs=inputs,unchanged_executables=exes,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase34-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','native_case_kinds','exact_in_memory_container_patches']}));print('Corpus:',total,'AP;',dict(counts),'link projection changes',len(link_projection));print('Evidence:',instructions,'instructions;',len(schema['primary_program_bytes']),'ranges match PE;17 inputs/2 EXEs unchanged')

if __name__=='__main__':main()
