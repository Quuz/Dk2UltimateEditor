"""Audit file semantics, isolated native results and manufacture arithmetic."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_manufacture_semantics import SCHEMA,manufacture_record_projection,manufacture_contribution,creature_manufacture_projection,manufacture_queue_projection,constant_contribution_reachability

ROOT=Path(__file__).resolve().parent

def main():
    cases=0
    for family,n,offset in [('Doors',616,0x234),('Traps',579,0x1FA)]:
        for required in [0,1,32767,32768,65535]:
            r=bytearray(n);struct.pack_into('<H',r,offset,required)
            if family=='Doors':struct.pack_into('<H',r,0x236,0xABCD)
            before=bytes(r);p=manufacture_record_projection(family,r)
            assert p['required_points_u16']==required and bytes(r)==before;cases+=1
    for n in (5449,5537):
        for raw,tps,wanted in [(0,20,0),(1,20,1),(19,20,1),(20,20,1),(39,20,1),(40,20,2),(65535,1,65535),(65535,20,3276)]:
            r=bytearray(n);struct.pack_into('<H',r,0xF0A,raw);before=bytes(r)
            p=creature_manufacture_projection(r,tps)
            assert p['runtime_points_per_contribution']==wanted and p['contribution']==wanted and bytes(r)==before;cases+=1
    for rate,level,mult,wanted in [(65535,1,0,65535),(100,2,150,150),(1,2,99,0),(100,2,-100,0),
        (65535,2,100,65534),(65535,10,32767,65534),(1,2,-99,0),(10,2,15,1),(1000,2,0,0)]:
        assert manufacture_contribution(rate,level,mult)==wanted;cases+=1
    q=bytearray(2056);struct.pack_into('<H',q,0,3000);struct.pack_into('<H',q,16,12000)
    q[0x801]=1;q[0x802]=2;q[0x803]=1
    p=manufacture_queue_projection(q,4000);assert p['completed'] and p['percent']==133 and p['current_head_required_u16']==12000 and p['threshold_differs_from_head'];cases+=1
    # At the physical threshold65535, repeated increments2 only visit even values.
    for increment in [0,1,2,16,100,65534,65535]:
        values=set();value=0
        while value not in values:
            values.add(value);value=(value+increment)&65535
        for required in [0,1,32767,65534,65535]:
            p=constant_contribution_reachability(required,increment)
            assert p['reachable']==any(v>=required for v in values)
            assert p['maximum_progress']==max(values) and p['period']==len(values);cases+=1
    for required,inc,start in [(65535,2,1),(65535,16,15),(65535,16,1),(65000,100,50)]:
        values=set();v=start
        while v not in values:values.add(v);v=(v+inc)&65535
        p=constant_contribution_reachability(required,inc,start=start)
        assert p['reachable']==(max(values)>=required) and p['period']==len(values);cases+=1
    struct.pack_into('<H',q,0,65535)
    p=manufacture_queue_projection(q,65530,20)
    assert p['addition_wrapped'] and p['progress_u16']==14 and not p['completed'];cases+=1
    for action in [lambda:manufacture_record_projection('Objects',bytes(894)),lambda:manufacture_record_projection('Doors',bytes(615)),
        lambda:creature_manufacture_projection(bytes(5449),0),lambda:manufacture_contribution(65536,1,100),
        lambda:manufacture_contribution(1,0,100),lambda:manufacture_queue_projection(q,True),
        lambda:manufacture_queue_projection(bytes(2055),1),lambda:constant_contribution_reachability(1,-1)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid argument accepted')
        cases+=1
    emulations=0;native_faults=0;emulation_hashes={}
    for row in SCHEMA['native_emulations']:
        path=ROOT/'ghidra'/row['file'];b=path.read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256']
        record=json.loads(b);regions=json.loads(record['input']['memory'])['regions'];queue=bytearray(2056);progress=None
        for region in regions:
            address=int(region['address'],16);data=bytes.fromhex(region['hex'])
            if 0x10000000<=address<0x10000808:
                offset=address-0x10000000;queue[offset:offset+len(data)]=data
            elif address==0x7FFF0004:progress=int.from_bytes(data,'little')&65535
        assert progress is not None
        projection=manufacture_queue_projection(queue,progress);r=record['result']
        if row['expected_fault']:
            assert projection['percent_divide_by_zero'] and r['success'] is False and r['final_pc']=='00506b93' and r['emulation_error']=='Divide by 0'
            native_faults+=1
        else:
            assert r['success'] and r['hit_return']
            wanted=int(projection['completed']) if record['input']['address'].lower()=='0x506b50' else projection['percent']
            assert wanted==int(r['registers']['EAX'],16)==row['expected_eax']
        emulations+=1;emulation_hashes[row['file']]=row['sha256']
    excerpts=0;instructions=0;evidence_hashes={}
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                b=(ROOT/'ghidra'/v['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(i in lines for i in v['instructions']);instructions+=len(v['instructions'])
                evidence_hashes[v['file']]=hashlib.sha256(b).hexdigest()
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(SCHEMA)
    def inspect(family,r):return dict(name=fmt.string(r[:32]),**manufacture_record_projection(family,r))
    scopes=[];patches=0
    for label,path in [('Doors',DATA/'Doors.kwd'),('Traps',DATA/'Traps.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();digest=hashlib.sha256(b).hexdigest();sections=fmt.parse(b);rows=[]
        for si,s in enumerate(sections):
            family={0x8C:'Doors',0x82:'Traps'}.get(s.signature)
            if family is None:continue
            offset=0x234 if family=='Doors' else 0x1FA
            for ri,r in enumerate(s.records):
                rows.append(inspect(family,r));changed=bytearray(r);changed[offset:offset+2]=b'\x34\x12'
                records=list(s.records);records[ri]=bytes(changed)
                clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                packed=b''.join(clone.pack() if i==si else sec.pack() for i,sec in enumerate(sections))
                absolute=s.offset+len(s.header)+ri*len(r)+offset
                assert packed[:absolute]==b[:absolute] and packed[absolute+2:]==b[absolute+2:]
                assert manufacture_record_projection(family,fmt.parse(packed)[si].records[ri])['required_points_u16']==0x1234
                patches+=1
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(path),sha256=digest,records=rows))
    corpus=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'));files=[];counts=Counter();zero=[];large=[];crate_zero=[];rates=Counter()
    for f in corpus['files']:
        if not any(s['family'] in ['Doors','Traps','Creatures'] for s in f.get('sections',[])):continue
        path=DATA/'maps'/f['file'];b=path.read_bytes();assert hashlib.sha256(b).hexdigest()==f['sha256'];local=Counter()
        for s in fmt.parse(b):
            family={0x8C:'Doors',0x82:'Traps',0xAA:'Creatures'}.get(s.signature)
            if family is None:continue
            for r in s.records:
                local[family]+=1;counts[family]+=1
                if family=='Creatures':rates[fmt.u16(r,0xF0A)]+=1;continue
                p=inspect(family,r);entry=dict(file=f['file'],family=family,name=p['name'],catalogue_id=p['catalogue_id'],required=p['required_points_u16'],crate_object_id=p['crate_object_id'])
                if p['required_points_u16']==0:zero.append(entry)
                if p['required_points_u16']>32767:large.append(entry)
                if p['crate_object_id']==0:crate_zero.append(entry)
        files.append(dict(file=f['file'],sha256=f['sha256'],record_counts=dict(local)))
    validation=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))
    for f in validation['files']:
        path=DATA/('maps/'+f['file'] if f['file']=='kop4Globals.kld' else f['file'])
        assert hashlib.sha256(path.read_bytes()).hexdigest()==f['sha256']
    for file,digest in SCHEMA['executables'].items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    result=dict(synthetic_cases_passed=cases,actual_requirement_patch_roundtrips=patches,native_emulations_verified=emulations,
        native_emulation_expected_divide_by_zero=native_faults,native_emulation_sha256=emulation_hashes,
        evidence_excerpts_verified=excerpts,evidence_instructions_verified=instructions,source_evidence_sha256=evidence_hashes,
        main_input_files_hashes_unchanged=len(validation['files']),executable_hashes_unchanged=SCHEMA['executables'],scopes=scopes,
        corpus=dict(files=files,record_counts=dict(counts),zero_requirements=zero,requirements_above_native_gui_limit=large,
            crate_object_id_zero=crate_zero,creature_raw_rates=dict(rates),
            caveat='Zero requirements may belong to types never queued. No runtime reachability or whole-game crash is inferred from file presence alone.'),limitation=SCHEMA['scope'])
    (ROOT/'manufacture-semantics-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Manufacture:',cases,'boundary cases;',patches,'whole-container patch roundtrips;',emulations,'native emulations (',native_faults,'expected IDIV0 fault)')
    print('Targeted corpus:',len(files),'files;',dict(counts),'; zero requirements:',len(zero),'; >32767:',len(large))

if __name__=='__main__':main()
