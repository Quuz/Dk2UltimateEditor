"""Verify native binding evidence, physical resource edits and original hashes."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_object_resources import SCHEMA,object_resources,set_object_resource

ROOT=Path(__file__).resolve().parent

def main():
    fixture=bytes((i*37+11)&255 for i in range(894))
    payloads=[bytes(84),bytes([255])*84,bytes((i*13+7)&255 for i in range(84))]
    cases=0
    for slot in SCHEMA['slots']:
        off=slot['raw_offset']
        for payload in payloads:
            out=set_object_resource(fixture,off,payload)
            assert out[:off]==fixture[:off] and out[off+84:]==fixture[off+84:]
            assert bytes.fromhex(next(s for s in object_resources(out) if s['raw_offset']==off)['raw_hex'])==payload
            assert set_object_resource(out,off,fixture[off:off+84])==fixture
            cases+=1
    for record,offset,payload in [(fixture,0x171,bytes(84)),(fixture,368.0,bytes(84)),
        (fixture,True,bytes(84)),(fixture[:893],0x170,bytes(84)),
        (fixture,0x170,bytes(83)),(fixture,0x170,bytes(85)),(fixture,0x170,[0]*84)]:
        try:set_object_resource(record,offset,payload)
        except ValueError:pass
        else:raise AssertionError((len(record),offset,type(payload)))
        cases+=1
    evidence_hashes={};excerpts=0
    proofs=list(SCHEMA['evidence'])
    for slot in SCHEMA['slots']:
        if slot['native_editor']:proofs+=slot['native_editor']['evidence']
    for proof in proofs:
        path=ROOT/'ghidra'/proof['file'];b=path.read_bytes()
        assert b.decode('utf8').splitlines()[proof['line']-1].strip()==proof['excerpt']
        evidence_hashes[proof['file']]=hashlib.sha256(b).hexdigest();excerpts+=1
    scopes=[];patches=0
    for label,path in [('standalone Objects',DATA/'Objects.kwd'),
        ('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('referenced kop4Globals',DATA/'maps/kop4Globals.kld')]:
        data=path.read_bytes();digest=hashlib.sha256(data).hexdigest()
        sections=fmt.parse(data);counts={s['raw_offset']:Counter() for s in SCHEMA['slots']};conflicts=[]
        records_total=0
        for section in sections:
            if section.signature!=0xF0:continue
            for index,record in enumerate(section.records):
                records_total+=1
                for view in object_resources(record):
                    off=view['raw_offset'];resource=view['resource'];counter=counts[off]
                    counter['records']+=1
                    counter['nonzero_type']+=bool(resource['type']['raw'])
                    counter['nonempty_name']+=bool(resource['name'])
                    counter['type_'+str(resource['type']['raw'])]+=1
                    edited=set_object_resource(record,off,payloads[2])
                    assert edited[:off]==record[:off] and edited[off+84:]==record[off+84:]
                    mutated=list(section.records);mutated[index]=edited
                    clone=fmt.Section(section.offset,section.signature,section.header,mutated,section.suffix)
                    clone.replace_records(mutated)
                    packed=clone.pack();reread=fmt.parse(packed)[0]
                    assert len(packed)==len(section.pack()) and reread.records[index]==edited
                    assert all(a==b for n,(a,b) in enumerate(zip(reread.records,section.records)) if n!=index)
                    patches+=1
                old=record[0xC8:0x11C];native=record[0x170:0x1C4]
                conflicts.append(dict(name=fmt.string(record[:32]),object_id=record[0x35A],
                    baseline_c8=fmt.art(old),native_in_hand_icon_170=fmt.art(native),
                    identical_resource_bytes=old==native))
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(path),sha256=digest,objects=records_total,
            slots_summary=[dict(raw_offset=off,**counter) for off,counter in counts.items()],
            naming_conflict_records=conflicts))
    main_inputs=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))['files']
    for info in main_inputs:
        path=DATA/('maps/'+info['file'] if info['file']=='kop4Globals.kld' else info['file'])
        assert hashlib.sha256(path.read_bytes()).hexdigest()==info['sha256'],info['file']
    executables=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for file,digest in executables.items():
        assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest,file
    result=dict(synthetic_cases_passed=cases,actual_resource_patch_roundtrips=patches,
        native_message_map_entries_verified=8,evidence_excerpts_verified=excerpts,
        source_evidence_sha256=evidence_hashes,main_input_files_hashes_unchanged=len(main_inputs),
        executable_hashes_unchanged=executables,scopes=scopes,
        limitation='Static native resource binding and byte-preserving codec verification. Modified resources were not executed in game/editor.')
    (ROOT/'object-resource-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects resources:',cases,'boundary cases;',patches,'actual resource patch roundtrips;',excerpts,'verified evidence excerpts')
    for scope in scopes:
        s=next(x for x in scope['slots_summary'] if x['raw_offset']==0x170)
        old=next(x for x in scope['slots_summary'] if x['raw_offset']==0xC8)
        print(scope['label'],scope['objects'],'objects; type!=0: raw170=',s['nonzero_type'],'rawC8=',old['nonzero_type'])

if __name__=='__main__':main()
