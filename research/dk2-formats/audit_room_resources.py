"""Native room-resource evidence and independent request-boundary checks."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_room_resources import SCHEMA,room_floor_resource_plan

ROOT=Path(__file__).resolve().parent

def fixture(mode,name=b'Room',kind=5):
    r=bytearray(1055);r[0x395]=mode;r[0xC8:0xC8+len(name)]=name;r[0x118]=kind
    return r

def main():
    cases=0
    # Independently enumerated expected call counts for a populated base name.
    expected={1:8,2:4,3:9,4:3,5:10,6:0,7:15,8:10,9:9,10:1,11:8,12:16,13:14}
    for mode in range(256):
        r=fixture(mode);before=bytes(r);p=room_floor_resource_plan(r)
        assert len(p['requests'])==expected.get(mode,0),(mode,p)
        assert bytes(r)==before
        assert p['runtime_flags_projection']==(8 if mode in (1,5,7,9,12,13) else 0)
        cases+=1
    for mode,wanted in [(3,['Room'+str(i) for i in range(9)]),
        (4,['Room0','Room1','Room2']),(9,['Room'+str(i) for i in range(1,10)]),
        (12,['Room'+str(i) for i in range(1,17)]),
        (11,[name for i in range(4) for name in ('Room'+str(i),'RoomDestroyed'+str(i))]),
        (13,[name for i in range(7) for name in ('Room'+str(i),'Room'+str(i)+'_DESTROYED')])]:
        assert [x['name'] for x in room_floor_resource_plan(fixture(mode))['requests']]==wanted
        cases+=1
    p=room_floor_resource_plan(fixture(1,b'AbcX'))
    assert [r['name'] for r in p['requests']]==['AbcX','Abc0','Abc1','Abc2','Abc6','Abc7','Abc8','Abc9']
    assert [r['runtime_entry_offset'] for r in p['requests']]==[0x1B]+[0xC1+i*27 for i in (0,1,2,6,7,8,9)];cases+=1
    for mode in range(1,14):
        p=room_floor_resource_plan(fixture(mode,b''))
        assert len(p['requests'])==(6 if mode==8 else 1 if mode==1 else 0)
        cases+=1
    for mode in (1,5,8,10):
        p=room_floor_resource_plan(fixture(mode,kind=0))
        assert len(p['requests'])==expected[mode]-(1 if mode==1 else 0)
        assert any(x['code']=='requests_with_zero_resource_type' for x in p['findings']);cases+=1
    for mode in (1,8,11,13):
        for opts in [dict(update_mode=True),dict(resource_service_present=False)]:
            assert not room_floor_resource_plan(fixture(mode),**opts)['requests'];cases+=1
    r=fixture(7);r[0x392]=13;p=room_floor_resource_plan(r)
    assert [(x['name'],x['resource_type'],x['runtime_entry_offset'],x['service_flags']) for x in p['requests'] if x['role']=='special_water']==[('Water',2,0x256,0x20000),('Water',1,0xA6,0x20000)];cases+=1
    r=fixture(5,b'A'*63);p=room_floor_resource_plan(r)
    assert any(x['code']=='generated_name_exceeds_local_buffer' for x in p['findings']);cases+=1
    r=fixture(5,b'A'*64);p=room_floor_resource_plan(r)
    assert p['findings'][0]['code']=='unterminated_resource_name' and not p['requests'];cases+=1
    for value in (0,0xFFFFFFFF,0x80000020,0x12340004):
        r=fixture(5);r[0x370:0x374]=value.to_bytes(4,'little')
        assert room_floor_resource_plan(r)['runtime_flags_projection']==(value&65535)|8;cases+=1
    for record,opts in [(bytes(1054),{}),(fixture(5),dict(update_mode=1))]:
        try:room_floor_resource_plan(record,**opts)
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    proofs=list(SCHEMA['evidence'])+SCHEMA['unresolved_resource']['evidence']
    for slot in SCHEMA['native_resource_buttons']:proofs+=slot['evidence']
    evidence_hashes={};excerpts=0;instructions=0
    for p in proofs:
        b=(ROOT/'ghidra'/p['file']).read_bytes()
        assert b.decode('utf8').splitlines()[p['line']-1].strip()==p['excerpt']
        evidence_hashes[p['file']]=hashlib.sha256(b).hexdigest();excerpts+=1
    ranges=[m['evidence'] for m in SCHEMA['construction']]+[SCHEMA['runtime_flags']['evidence'],SCHEMA['water_special']['evidence']]
    for p in ranges:
        b=(ROOT/'ghidra'/p['file']).read_bytes();lines=b.decode('utf8').splitlines()
        assert all(line in lines for line in p['instructions']);instructions+=len(p['instructions'])
        evidence_hashes[p['file']]=hashlib.sha256(b).hexdigest()
    scopes=[];roundtrips=0
    for label,path in [('standalone Rooms',DATA/'Rooms.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('referenced kop4Globals',DATA/'maps/kop4Globals.kld')]:
        data=path.read_bytes();digest=hashlib.sha256(data).hexdigest();rooms=[];unknown_nonzero=[]
        for section in fmt.parse(data):
            if section.signature!=0x78:continue
            for index,record in enumerate(section.records):
                p=room_floor_resource_plan(record)
                rooms.append(dict(name=fmt.string(record[:32]),room_id=record[0x392],plan=p))
                unknown=record[0x1C4:0x218]
                if any(unknown):unknown_nonzero.append(dict(name=fmt.string(record[:32]),raw_hex=unknown.hex()))
                # Arbitrary resource bytes must survive every construction mode.
                for replacement in (bytes(84),bytes((i*13+7)&255 for i in range(84))):
                    changed=record[:0x1C4]+replacement+record[0x218:]
                    records=list(section.records);records[index]=changed
                    clone=fmt.Section(section.offset,section.signature,section.header,records,section.suffix)
                    clone.replace_records(records);out=fmt.parse(clone.pack())[0]
                    assert out.records[index]==changed and out.records[index][0x1C4:0x218]==replacement
                    assert all(a==b for n,(a,b) in enumerate(zip(out.records,section.records)) if n!=index)
                    assert room_floor_resource_plan(changed)==p
                    roundtrips+=1
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(path),sha256=digest,room_count=len(rooms),rooms=rooms,
            construction_counts=dict(sorted(Counter(r['plan']['construction'] for r in rooms).items())),unknown_resource_nonzero_records=unknown_nonzero,
            requested_resources=sum(len(r['plan']['requests']) for r in rooms)))
    validation=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))
    for info in validation['files']:
        path=DATA/('maps/'+info['file'] if info['file']=='kop4Globals.kld' else info['file'])
        assert hashlib.sha256(path.read_bytes()).hexdigest()==info['sha256']
    exe=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for path,digest in exe.items():assert hashlib.sha256((ROOT.parent.parent/path).read_bytes()).hexdigest()==digest
    # Targeted new semantic survey of only previously indexed Room catalogues.
    # Other corpus files and the four known malformed Variables are not parsed.
    corpus=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'))
    corpus_files=[];corpus_modes=Counter();corpus_nonzero=[];corpus_findings=[];corpus_records=0
    for row in corpus['files']:
        if not any(s['family']=='Rooms' for s in row.get('sections',[])):continue
        path=DATA/'maps'/row['file'];data=path.read_bytes()
        assert hashlib.sha256(data).hexdigest()==row['sha256'],path
        count=0
        for section in fmt.parse(data):
            if section.signature!=0x78:continue
            for r in section.records:
                count+=1;corpus_records+=1;corpus_modes[r[0x395]]+=1
                if any(r[0x1C4:0x218]):corpus_nonzero.append(dict(file=path.name,name=fmt.string(r[:32]),raw_hex=r[0x1C4:0x218].hex()))
                p=room_floor_resource_plan(r)
                if p['findings']:corpus_findings.append(dict(file=path.name,name=fmt.string(r[:32]),findings=p['findings']))
        corpus_files.append(dict(file=path.name,sha256=row['sha256'],room_records=count))
    result=dict(synthetic_cases_passed=cases,unknown_resource_patch_roundtrips=roundtrips,source_evidence_sha256=evidence_hashes,
        evidence_excerpts_verified=excerpts,evidence_instructions_verified=instructions,main_input_files_hashes_unchanged=len(validation['files']),
        executable_hashes_unchanged=exe,scopes=scopes,
        corpus_room_survey=dict(files=corpus_files,room_records=corpus_records,construction_counts=dict(sorted(corpus_modes.items())),
            nonzero_unknown_resources=corpus_nonzero,resource_plan_findings=corpus_findings,
            scope='Only Room-bearing files selected from the pinned corpus inventory. Non-use is not inferred from zero-valued samples.'),
        limitation='Read-only static base/floor/destroyed/Water request projection, not resource-manager success or geometric tile placement. Start/End and common resource requests are excluded. Modified records were not executed.')
    (ROOT/'room-resource-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Room resources:',cases,'boundary cases;',roundtrips,'unknown-resource patch roundtrips;',excerpts,'evidence excerpts;',instructions,'instructions')
    for s in scopes:print(s['label'],s['room_count'],'Rooms;',s['requested_resources'],'planned base/floor/destroyed/Water requests;',len(s['unknown_resource_nonzero_records']),'nonzero unknown resources')
    print('Targeted map corpus:',len(corpus_files),'Room-bearing files;',corpus_records,'records;',len(corpus_nonzero),'nonzero unknown resources;',len(corpus_findings),'request-plan findings')

if __name__=='__main__':main()
