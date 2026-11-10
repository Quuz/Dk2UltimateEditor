"""Native trap projections: boundaries, primary evidence and targeted corpus."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_trap_semantics import SCHEMA,trap_editor_flags_projection,trap_charge_projection,advance_trap_charge_counter,trap_los_range_projection

ROOT=Path(__file__).resolve().parent

def main():
    cases=0
    original=bytes((i*37+11)&255 for i in range(579))
    supported_bits={0,1,2,3,4,5,7,8,9,10,11,12,13,14,15,16,17}
    for bit in range(32):
        r=bytearray(original);r[0x1EC:0x1F0]=(1<<bit).to_bytes(4,'little');before=bytes(r)
        p=trap_editor_flags_projection(r)
        assert p['native_editor_serialized']==((1<<bit) if bit in supported_bits else 0)
        assert p['would_change']==(bit not in supported_bits) and bytes(r)==before;cases+=1
    for raw,tps,expected in [(0,20,0),(4096,20,20),(2048,20,10),(255*4096,1,255),
        (256*4096,1,256),(0xFFFFFFFF,1,0xFFFFF),(0x80000000,2,0)]:
        r=bytearray(original);r[0x1CC:0x1D0]=raw.to_bytes(4,'little')
        p=trap_charge_projection(r,tps);assert p['threshold']==expected
        assert p['reachable_by_byte_counter']==(expected<=255);cases+=1
    # Start from zero, then sustain256 positive callbacks through one wrap.
    # Expected success positions are independent explicit interval sets.
    for threshold in (0,1,2,254,255,256,0xFFFFFFFF):
        counter=0;successful=[]
        for poll in range(1,257):
            step=advance_trap_charge_counter(counter,True,threshold);counter=step['counter']
            if step['threshold_reached']:successful.append(poll)
        wanted=list(range(max(1,threshold),256)) if 1<=threshold<=255 else list(range(1,257)) if threshold==0 else []
        assert successful==wanted and counter==0,(threshold,successful)
        reset=advance_trap_charge_counter(127,False,threshold)
        assert reset==dict(counter=0,threshold_reached=False);cases+=1
    for raw,tracking,radius,threshold in [(0,False,0,0),(1,False,1,4096),(5,False,5,102400),
        (5,True,7,200704),(1024,False,1024,0),(0xFFFFFFFF,True,1,4096)]:
        r=bytearray(original);r[0x1DC:0x1E0]=raw.to_bytes(4,'little');before=bytes(r)
        p=trap_los_range_projection(r,tracking=tracking)
        assert p['radius_operand']==radius and p['squared_threshold_fixed12']==threshold and bytes(r)==before;cases+=1
    for action in [lambda:trap_charge_projection(original,0),lambda:trap_charge_projection(original,True),
        lambda:trap_los_range_projection(original,tracking=1),lambda:trap_editor_flags_projection(original[:578]),
        lambda:advance_trap_charge_counter(256,True,1),lambda:advance_trap_charge_counter(1,1,1)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid argument accepted')
        cases+=1
    evidence_hashes={};lines_verified=0;instructions_verified=0
    def walk(value):
        nonlocal lines_verified,instructions_verified
        if isinstance(value,dict):
            if 'file' in value and ('excerpt' in value or 'instructions' in value):
                p=ROOT/'ghidra'/value['file'];b=p.read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in value:
                    assert lines[value['line']-1].strip()==value['excerpt'];lines_verified+=1
                else:
                    assert all(line in lines for line in value['instructions']);instructions_verified+=len(value['instructions'])
                evidence_hashes[value['file']]=hashlib.sha256(b).hexdigest()
            for v in value.values():walk(v)
        elif isinstance(value,list):
            for v in value:walk(v)
    walk(SCHEMA)
    scenario_tps=20
    def inspect(record):
        flags=trap_editor_flags_projection(record);charge=trap_charge_projection(record,scenario_tps)
        return dict(name=fmt.string(record[:32]),trap_id=record[0x20C],trigger_type=record[0x20B],
            flags=flags,charge=charge,los_radius=trap_los_range_projection(record),
            unknown_204_hex=record[0x204:0x206].hex())
    scopes=[]
    for label,path in [('standalone Traps',DATA/'Traps.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('referenced kop4Globals',DATA/'maps/kop4Globals.kld')]:
        data=path.read_bytes();digest=hashlib.sha256(data).hexdigest()
        records=[inspect(r) for s in fmt.parse(data) if s.signature==0x82 for r in s.records]
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(path),sha256=digest,records=records))
    corpus=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'));files=[];flags_changes=[];charge_issues=[];ranges=[];trigger_counts=Counter();unknown=Counter();records_total=0
    for file in corpus['files']:
        if not any(s['family']=='Traps' for s in file.get('sections',[])):continue
        path=DATA/'maps'/file['file'];b=path.read_bytes();assert hashlib.sha256(b).hexdigest()==file['sha256']
        count=0
        for s in fmt.parse(b):
            if s.signature!=0x82:continue
            for r in s.records:
                count+=1;records_total+=1;v=inspect(r);trigger_counts[v['trigger_type']]+=1;unknown[v['unknown_204_hex']]+=1
                if v['flags']['would_change']:flags_changes.append(dict(file=file['file'],name=v['name'],dropped_bits=v['flags']['dropped_bits']))
                if not v['charge']['reachable_by_byte_counter']:charge_issues.append(dict(file=file['file'],name=v['name'],projection=v['charge']))
                if v['los_radius']['multiplication_wrapped']:ranges.append(dict(file=file['file'],name=v['name'],projection=v['los_radius']))
        files.append(dict(file=file['file'],sha256=file['sha256'],trap_records=count))
    validation=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))
    for file in validation['files']:
        path=DATA/('maps/'+file['file'] if file['file']=='kop4Globals.kld' else file['file'])
        assert hashlib.sha256(path.read_bytes()).hexdigest()==file['sha256']
    exes=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for file,digest in exes.items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    result=dict(synthetic_cases_passed=cases,evidence_excerpts_verified=lines_verified,evidence_instructions_verified=instructions_verified,
        source_evidence_sha256=evidence_hashes,scenario_ticks_per_second=scenario_tps,
        scenario_note='20TPS is an explicit audit scenario, not a value inferred from map files or a running game. Pass actual TPS to the API.',
        main_input_files_hashes_unchanged=len(validation['files']),executable_hashes_unchanged=exes,scopes=scopes,
        corpus=dict(files=files,trap_records=records_total,trigger_type_counts=dict(trigger_counts),unknown_204_hex_counts=dict(unknown),
            native_editor_flag_change_candidates=flags_changes,unreachable_charge_scenario=charge_issues,los_range_wrap_candidates=ranges),
        limitation='Static read-only projections and boundary checks; no modified file or trap executed in game/editor. Unknown meanings remain unresolved.')
    (ROOT/'trap-semantics-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Trap semantics:',cases,'boundary scenarios;',lines_verified,'evidence excerpts;',instructions_verified,'instructions')
    print('Targeted corpus:',len(files),'files;',records_total,'Traps;',len(flags_changes),'flag changes;',len(charge_issues),'unreachable charge at explicit20TPS;',len(ranges),'range wraps')

if __name__=='__main__':main()
