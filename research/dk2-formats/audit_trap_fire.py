"""Verify native trap-origin arithmetic and conditional state projections."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_trap_fire import SCHEMA,trap_shot_origin,trap_post_creation_projection,advance_trap_threat_fields

ROOT=Path(__file__).resolve().parent

def fixture(offsets=(0,0,0)):
    r=bytearray(579);struct.pack_into('<iii',r,0x231,*offsets);r[0x20D]=1
    return r

def main():
    cases=0
    r=fixture((4096,8192,2048));position=[100,200,300]
    expected={0:[4196,8392,2348],512:[-8092,4296,2348],1024:[-3996,-7992,2348],1536:[8292,-3896,2348]}
    before=bytes(r)
    for heading,wanted in expected.items():
        p=trap_shot_origin(r,position,heading);assert p['origin_raw']==wanted and not p['product_wrap_indices'];cases+=1
    assert bytes(r)==before
    for offsets,heading,wanted,wrapped in [((4096,0,0),256,[2896,2896,0],False),
        ((-1,0,0),256,[-1,-1,0],False),((32768,0,0),0,[-32768,0,0],True),
        ((0,0,1),0,[0,0,1],False)]:
        p=trap_shot_origin(fixture(offsets),[0,0,0],heading)
        assert p['origin_raw']==wanted and bool(p['product_wrap_indices'])==wrapped;cases+=1
    p=trap_shot_origin(fixture((0,0,1)),[0,0,0x7FFFFFFF],0)
    assert p['origin_raw'][2]==-0x80000000 and p['coordinate_wrap_indices']==[2];cases+=1
    for ticks,wanted_byte in [(0,0),(1,1),(255,255),(256,0),(900,132),(1200,176)]:
        r=fixture();r[0x1D0:0x1D4]=(ticks*4096).to_bytes(4,'little');r[0x1EA:0x1EC]=(74).to_bytes(2,'little')
        p=trap_post_creation_projection(r,1,0)
        assert p['threat_duration_ticks']==ticks and p['threat_duration_byte']==wanted_byte
        assert p['threat_value']==(74 if ticks else 0)
        assert p['duration_zero_with_nonzero_threat']==(ticks==256);cases+=1
    # Counter exhaustion and Threat emission are independent in inspected tick.
    state=advance_trap_threat_fields(1,74)
    assert state==dict(duration_byte=0,threat_value=74,emission_gate=True);cases+=1
    assert advance_trap_threat_fields(0,0)==dict(duration_byte=0,threat_value=0,emission_gate=False);cases+=1
    r=fixture();r[0x1C8:0x1CC]=(4096).to_bytes(4,'little')
    p=trap_post_creation_projection(r,20,0xFFFFFFFA)
    assert p['recharge_ticks']==20 and p['recharge_deadline']==14;cases+=1
    r[0x1E0:0x1E8]=bytes.fromhex('efbeadde67452381');r[0x1EC:0x1F0]=(2).to_bytes(4,'little');r[0x20C]=16;r[0x20B]=2
    p=trap_post_creation_projection(r,20,0,target_tag=0x12340042,instance_flags=0xFF)
    assert p['copied_shot_data1']==0xDEADBEEF and p['copied_shot_data2']==0x81234567
    assert p['target_tag_low16']==0x42 and p['normal_caller_data2_override']==0x42 and p['pressure_target_marker_write']==1
    assert p['projected_instance_flags']==0xEF and p['reveal_on_fire'];cases+=1
    for trigger,target,wanted in [(0,1,1),(1,1,None),(2,1,1),(3,1,None),(2,0,None),(255,1,1)]:
        r[0x20B]=trigger;p=trap_post_creation_projection(r,20,0,target_tag=target)
        assert p['pressure_target_marker_write']==wanted;cases+=1
    for action in [lambda:trap_shot_origin(r,[0,0,0],2048),lambda:trap_shot_origin(r,[0,0,0],-1),
        lambda:trap_shot_origin(r,[0,0,1.5],0),lambda:trap_shot_origin(r[:578],[0,0,0],0),
        lambda:trap_post_creation_projection(r,0,0),lambda:advance_trap_threat_fields(256,1)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid projection input accepted')
        cases+=1
    source_hashes={};excerpts=0;instructions=0
    def walk(value):
        nonlocal excerpts,instructions
        if isinstance(value,dict):
            if 'file' in value and ('excerpt' in value or 'instructions' in value):
                b=(ROOT/'ghidra'/value['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in value:assert lines[value['line']-1].strip()==value['excerpt'];excerpts+=1
                else:assert all(i in lines for i in value['instructions']);instructions+=len(value['instructions'])
                source_hashes[value['file']]=hashlib.sha256(b).hexdigest()
            for v in value.values():walk(v)
        elif isinstance(value,list):
            for v in value:walk(v)
    walk(SCHEMA)
    tps=20;scopes=[];patches=0
    def describe(record):
        return dict(name=fmt.string(record[:32]),trap_id=record[0x20C],
            origin_samples=[trap_shot_origin(record,[0,0,0],heading) for heading in (0,512,1024,1536)],
            post_creation_scenario=trap_post_creation_projection(record,tps,1000,target_tag=1,instance_flags=0x10))
    for label,path in [('standalone Traps',DATA/'Traps.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('referenced kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();digest=hashlib.sha256(b).hexdigest();rows=[]
        for section in fmt.parse(b):
            if section.signature!=0x82:continue
            for index,record in enumerate(section.records):
                rows.append(describe(record))
                changed=fmt.set_scalar(record,'Traps','ShotData1',0xDEADBEEF)
                assert changed[:0x1E0]==record[:0x1E0] and changed[0x1E4:]==record[0x1E4:]
                assert trap_shot_origin(changed,[0,0,0],0)==trap_shot_origin(record,[0,0,0],0)
                projection=trap_post_creation_projection(changed,tps,1000)
                assert projection['copied_shot_data1']==0xDEADBEEF
                records=list(section.records);records[index]=changed
                clone=fmt.Section(section.offset,section.signature,section.header,records,section.suffix);clone.replace_records(records)
                out=fmt.parse(clone.pack())[0]
                assert out.records[index]==changed
                assert all(a==c for n,(a,c) in enumerate(zip(out.records,section.records)) if n!=index);patches+=1
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(path),sha256=digest,records=rows))
    corpus=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'));files=[];narrowed=[];zero=[];offsets={};total=0
    for file in corpus['files']:
        if not any(s['family']=='Traps' for s in file.get('sections',[])):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==file['sha256'];count=0
        for section in fmt.parse(b):
            if section.signature!=0x82:continue
            for r in section.records:
                count+=1;total+=1;p=trap_post_creation_projection(r,tps,1000)
                key=struct.unpack_from('<iii',r,0x231);offsets.setdefault(key,r)
                row=dict(file=file['file'],name=fmt.string(r[:32]),ticks=p['threat_duration_ticks'],duration_byte=p['threat_duration_byte'],threat=p['threat_value'])
                if p['threat_duration_narrowed']:narrowed.append(row)
                if p['duration_zero_with_nonzero_threat']:zero.append(row)
        files.append(dict(file=file['file'],sha256=file['sha256'],trap_records=count))
    origin_cases=0;wraps=[]
    for key,r in offsets.items():
        for angle in range(2048):
            p=trap_shot_origin(r,[0,0,0],angle);origin_cases+=1
            if p['product_wrap_indices']:wraps.append(dict(offset_raw=key,heading=angle,indices=p['product_wrap_indices']))
    validation=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))
    for f in validation['files']:
        path=DATA/('maps/'+f['file'] if f['file']=='kop4Globals.kld' else f['file'])
        assert hashlib.sha256(path.read_bytes()).hexdigest()==f['sha256']
    exes=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for file,digest in exes.items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    result=dict(synthetic_cases_passed=cases,actual_shotdata1_patch_roundtrips=patches,evidence_excerpts_verified=excerpts,evidence_instructions_verified=instructions,
        source_evidence_sha256=source_hashes,main_input_files_hashes_unchanged=len(validation['files']),executable_hashes_unchanged=exes,
        scenario=dict(ticks_per_second=tps,current_tick=1000,target_tag=1,instance_flags=0x10,
            note='Explicit audit scenario, not values inferred from game runtime or maps; all post-creation assignments are conditional.'),
        scopes=scopes,corpus=dict(files=files,trap_records=total,threat_duration_narrowing=narrowed,zero_duration_with_nonzero_threat=zero,
            distinct_offsets=len(offsets),all_heading_origin_projections=origin_cases,offset_multiplication_wraps=wraps),
        limitation='Static arithmetic and selected assignments. Threat countdown independence is a local tick observation, not a global perpetual-threat claim. No modified file executed.')
    (ROOT/'trap-fire-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Trap fire:',cases,'boundary cases;',patches,'ShotData1 roundtrips;',excerpts,'evidence excerpts;',instructions,'instructions')
    print('Corpus:',len(files),'files;',total,'Traps;',len(narrowed),'narrowed duration at explicit20TPS;',len(zero),'zero-byte nonzero-threat cases')
    print('Origins:',len(offsets),'distinct offsets;',origin_cases,'all-angle projections;',len(wraps),'multiply wraps')

if __name__=='__main__':main()
