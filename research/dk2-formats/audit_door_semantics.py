"""Verify door evidence, arithmetic boundaries and read-only corpus projections."""
from collections import Counter
from pathlib import Path
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_door_semantics import SCHEMA,door_scalar_projection,door_editor_flags_projection,door_health_tick_projection,door_shot_projection

ROOT=Path(__file__).resolve().parent

def main():
    cases=0;seed=bytes((i*29+7)&255 for i in range(616))
    for bit in range(32):
        r=bytearray(seed);struct.pack_into('<I',r,0x228,1<<bit);before=bytes(r)
        p=door_editor_flags_projection(r)
        assert p['native_editor_serialized']==(1<<bit if bit in (5,6,7) else 0)
        assert p['would_change']==(bit not in (5,6,7)) and bytes(r)==before;cases+=1
    for low,high in [(0,65535),(65535,0),(32768,1),(1,32768),(0x1234,0xABCD)]:
        r=bytearray(seed);struct.pack_into('<HH',r,0x234,low,high)
        p=door_scalar_projection(r);assert p['manufacture_required_u16']==low and p['unresolved_236_u16']==high;cases+=1
    r=bytearray(616);struct.pack_into('<H',r,0x22C,100)
    for gain,health,tick,deadline,dead,active,delta,next_tick in [
        (10,90,11,10,False,True,10,30),(10,99,11,10,False,True,1,30),
        (10,100,11,10,False,True,0,30),(10,90,10,10,False,False,0,10),
        (0,90,11,10,False,False,0,10),(65535,100,11,10,False,True,-1,30),
        (32768,100,11,10,False,True,-32768,30),(32767,90,11,10,False,True,10,30),
        (10,90,11,10,True,False,0,10),(10,105,11,10,False,True,-5,30),
        (10,-32768,11,10,False,True,10,30),
        (10,90,0xFFFFFFFF,0xFFFFFFF0,False,True,10,4)]:
        struct.pack_into('<H',r,0x21C,gain);before=bytes(r)
        p=door_health_tick_projection(r,health,tick,deadline,20,is_dead=dead)
        assert (p['gate_passed'],p['delta_to_apply'],p['next_health_tick'])==(active,delta,next_tick)
        assert p['health_helper_called']==(active and delta!=0) and bytes(r)==before;cases+=1
    # The next deadline advances from the previous deadline, one update per call.
    struct.pack_into('<H',r,0x21C,1)
    deadline=0
    for _ in range(3):
        deadline=door_health_tick_projection(r,90,1000,deadline,20)['next_health_tick']
    assert deadline==60;cases+=1
    door=bytearray(616);trap=bytearray(579);door[0x227]=16;trap[0x20C]=16;trap[0x20D]=7
    struct.pack_into('<I',trap,0x1C8,4096);struct.pack_into('<I',trap,0x1D4,42)
    struct.pack_into('<II',trap,0x1E0,0xDEADBEEF,0x12345678)
    for i in (0x231,0x235,0x239):struct.pack_into('<i',trap,i,8192)
    for ok in (False,True):
        d0=bytes(door);t0=bytes(trap)
        p=door_shot_projection(door,trap,20,0xFFFFFFF8,[4096,-4096,0],door_tag=0x12345,target_tag=0xABCDE,factory_succeeded=ok)
        assert p['initial_origin']==[4096,-4096,2048] and p['next_shot_tick']==12
        assert p['shot_data2']==(0x12345678 if ok else None) and p['shot_data1']==(0xDEADBEEF if ok else None)
        assert p['source_tag']==(0x2345 if ok else None) and p['target_tag']==(0xBCDE if ok else None)
        assert bytes(door)==d0 and bytes(trap)==t0;cases+=1
    p=door_shot_projection(door,trap,20,0,[0,0,0x7FFFFFFF]);assert p['initial_origin'][2]==-2147481601;cases+=1
    for action in [lambda:door_scalar_projection(seed[:615]),
        lambda:door_health_tick_projection(r,100,10,0,0),lambda:door_health_tick_projection(r,100,True,0,20),
        lambda:door_health_tick_projection(r,1<<31,10,0,20),
        lambda:door_shot_projection(door,trap,20,1,[0,0,0],factory_succeeded=1),
        lambda:door_shot_projection(bytes(616),trap,20,1,[0,0,0])]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    hashes={};excerpts=0;instructions=0
    def evidence(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                b=(ROOT/'ghidra'/v['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(i in lines for i in v['instructions']);instructions+=len(v['instructions'])
                hashes[v['file']]=hashlib.sha256(b).hexdigest()
            for x in v.values():evidence(x)
        elif isinstance(v,list):
            for x in v:evidence(x)
    evidence(SCHEMA)
    def inspect(record):
        return dict(name=fmt.string(record[:32]),scalars=door_scalar_projection(record),flags=door_editor_flags_projection(record),trap_type_id=record[0x227])
    scopes=[];patches=0
    for label,p in [('standalone',DATA/'Doors.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=p.read_bytes();digest=hashlib.sha256(b).hexdigest();sections=fmt.parse(b);values=[]
        for si,s in enumerate(sections):
            if s.signature!=0x8C:continue
            values.extend(inspect(r) for r in s.records)
            for ri,r in enumerate(s.records):
                for offset in [0x234,0x236,0x21C]:
                    changed=bytearray(r);changed[offset:offset+2]=b'\xCD\xAB'
                    records=list(s.records);records[ri]=bytes(changed)
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else sec.pack() for i,sec in enumerate(sections))
                    reread=fmt.parse(packed)[si].records[ri]
                    absolute=s.offset+len(s.header)+ri*616+offset
                    assert packed[:absolute]==b[:absolute] and packed[absolute+2:]==b[absolute+2:]
                    assert reread[:offset]==r[:offset] and reread[offset:offset+2]==b'\xCD\xAB' and reread[offset+2:]==r[offset+2:]
                    expected_records=list(s.records);expected_records[ri]=reread
                    assert fmt.parse(packed)[si].records==expected_records
                    patches+=1
        assert hashlib.sha256(p.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(p),sha256=digest,records=values))
    corpus=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'));files=[];total=0
    upper=Counter();gain=Counter();unknown=Counter();flags_changes=[];trap_links=Counter();missing=[]
    for f in corpus['files']:
        if not any(s['family']=='Doors' for s in f.get('sections',[])):continue
        p=DATA/'maps'/f['file'];b=p.read_bytes();assert hashlib.sha256(b).hexdigest()==f['sha256']
        sections=fmt.parse(b);traps={r[0x20C] for s in sections if s.signature==0x82 for r in s.records};n=0
        for s in sections:
            if s.signature!=0x8C:continue
            for r in s.records:
                n+=1;v=inspect(r);a=v['scalars'];upper[a['unresolved_236_u16']]+=1;gain[a['health_gain_s16']]+=1
                unknown[json.dumps(a['untransferred_hex'],sort_keys=True)]+=1;trap_links[v['trap_type_id']]+=1
                if v['flags']['would_change']:flags_changes.append(dict(file=f['file'],name=v['name'],flags=v['flags']))
                if v['trap_type_id'] and v['trap_type_id'] not in traps:missing.append(dict(file=f['file'],name=v['name'],trap_type_id=v['trap_type_id']))
        total+=n;files.append(dict(file=f['file'],sha256=f['sha256'],door_records=n))
    validation=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))
    for f in validation['files']:
        p=DATA/('maps/'+f['file'] if f['file']=='kop4Globals.kld' else f['file'])
        assert hashlib.sha256(p.read_bytes()).hexdigest()==f['sha256']
    for file,digest in SCHEMA['executables'].items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    result=dict(synthetic_cases_passed=cases,actual_scalar_patch_roundtrips=patches,
        evidence_excerpts_verified=excerpts,evidence_instructions_verified=instructions,source_evidence_sha256=hashes,
        main_input_files_hashes_unchanged=len(validation['files']),executable_hashes_unchanged=SCHEMA['executables'],scopes=scopes,
        corpus=dict(files=files,door_records=total,upper_236_counts=dict(upper),signed_health_gain_counts=dict(gain),
            untransferred_bytes_counts=dict(unknown),native_editor_flag_change_candidates=flags_changes,
            trap_type_id_counts=dict(trap_links),trap_ids_absent_from_same_container=missing,
            link_note='Same-container presence check only; external catalogue resolution is not simulated.'),
        limitation=SCHEMA['limitation'])
    (ROOT/'door-semantics-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Door semantics:',cases,'boundary cases;',patches,'actual scalar patch roundtrips;',excerpts,'excerpts;',instructions,'instructions')
    print('Targeted corpus:',len(files),'files;',total,'Doors; upper236:',dict(upper),'; signed HealthGain:',dict(gain))

if __name__=='__main__':main()
