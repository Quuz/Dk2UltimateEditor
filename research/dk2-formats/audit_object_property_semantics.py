"""Audit Objects native views, flags, signed UI field and exact byte patches."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_object_properties import SCHEMA,native_object_property_views,native_object_speed_projection,native_object_flags_projection

ROOT=Path(__file__).resolve().parent

def main():
    cases=0;seed=bytes((i*17+5)&255 for i in range(894))
    for bit in range(32):
        r=bytearray(seed);struct.pack_into('<I',r,0x344,1<<bit);before=bytes(r);p=native_object_flags_projection(r)
        supported=bit not in [11,19,27,28,29,30,31]
        assert p['native_decode_encode']==(1<<bit if supported else 0) and bytes(r)==before;cases+=1
    priority=[4,8,0x1000,0x10,0x20,0x40,0x80]
    for combination in range(128):
        flags=0x210101|sum(mask for bit,mask in enumerate(priority) if combination&(1<<bit))
        r=bytearray(seed);struct.pack_into('<I',r,0x344,flags);p=native_object_flags_projection(r)
        index=next((i+1 for i in range(7) if combination&(1<<i)),0)
        expected=0 if index==0 else priority[index-1]
        assert p['selected_type_index']==index and p['native_panel_accept']==0x210101|expected
        assert p['native_decode_encode']==flags;cases+=1
    for field in [f for f in SCHEMA['fields'] if f['native_editor_kind']=='scaled_u32']:
        for raw in [0,1,4096,65535,65536,0xFFFFFFFF]:
            r=bytearray(seed);struct.pack_into('<I',r,field['raw_offset'],raw);before=bytes(r)
            view=next(v for v in native_object_property_views(r) if v['name']==field['name'])
            assert view['native_editor_value']>=0 and int(view['native_editor_value']*field['divisor'])==raw and bytes(r)==before;cases+=1
    for offset,name in [(0x34C,'ManaGen'),(0x34E,'ManaValue')]:
        for raw,value,valid in [(0,0,True),(32000,32000,True),(32767,32767,False),(32768,-32768,False),(65535,-1,False)]:
            r=bytearray(seed);struct.pack_into('<H',r,offset,raw)
            view=next(v for v in native_object_property_views(r) if v['name']==name)
            assert view['native_editor_value']==value and view['gui_value_in_range']==valid;cases+=1
    for raw,tps,quotient,bits,clamped in [(0,20,0,0,False),(1,20,0,1,True),(20,20,1,1,False),
        (1310700,20,65535,65535,False),(1310720,20,65536,1,True),(1310740,20,65537,1,False),(0xFFFFFFFF,1,0xFFFFFFFF,65535,False)]:
        r=bytearray(seed);struct.pack_into('<I',r,0x338,raw);before=bytes(r);p=native_object_speed_projection(r,tps)
        assert (p['quotient'],p['runtime_bits'],p['zero_replaced_with_one'])==(quotient,bits,clamped) and bytes(r)==before;cases+=1
    for action in [lambda:native_object_property_views(seed[:893]),lambda:native_object_speed_projection(seed,0),lambda:native_object_speed_projection(seed,True),lambda:native_object_speed_projection(seed,1<<32)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    emulations=0;emu_hashes={}
    for row in SCHEMA['native_flag_emulations']:
        b=(ROOT/'ghidra'/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'];v=json.loads(b)
        r=bytearray(seed);struct.pack_into('<I',r,0x344,v['raw_flags']);p=native_object_flags_projection(r)
        assert v['result']['success'] and v['result']['hit_return']
        assert int(v['result']['registers']['EAX'],16)==p['native_decode_encode']==v['expected_eax'];emulations+=1;emu_hashes[row['file']]=row['sha256']
    lines_verified=0;instructions=0;evidence_hashes={}
    def walk(v):
        nonlocal lines_verified,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                b=(ROOT/'ghidra'/v['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];lines_verified+=1
                else:assert all(i in lines for i in v['instructions']);instructions+=len(v['instructions'])
                evidence_hashes[v['file']]=hashlib.sha256(b).hexdigest()
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(SCHEMA)
    patches=0;scopes=[]
    def inspect(r):return dict(name=fmt.string(r[:32]),object_id=r[0x35A],fields=native_object_property_views(r),flags=native_object_flags_projection(r),speed=native_object_speed_projection(r,20))
    for label,path in [('Objects',DATA/'Objects.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();sections=fmt.parse(b);digest=hashlib.sha256(b).hexdigest();rows=[]
        for si,s in enumerate(sections):
            if s.signature!=0xF0:continue
            for ri,r in enumerate(s.records):
                rows.append(inspect(r))
                for offset,width in [(0x34C,2),(0x32C,4),(0x344,4)]:
                    changed=bytearray(r);new=bytes.fromhex('78563412')[:width];changed[offset:offset+width]=new
                    records=list(s.records);records[ri]=bytes(changed)
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else sec.pack() for i,sec in enumerate(sections));absolute=s.offset+len(s.header)+ri*len(r)+offset
                    assert packed[:absolute]==b[:absolute] and packed[absolute+width:]==b[absolute+width:]
                    assert fmt.parse(packed)[si].records[ri][offset:offset+width]==new;patches+=1
        assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,path=str(path),sha256=digest,records=rows))
    corpus=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'));files=[];objects=0;terrain=0;mana_gen=Counter();native_flag_loss=[];type_loss=[];gui_limits=Counter();gui_candidates=[];narrowing=Counter();speed_issues=[];terrain_unknown=Counter()
    for file in corpus['files']:
        if not any(s['family'] in ['Objects','Terrain'] for s in file.get('sections',[])):continue
        path=DATA/'maps'/file['file'];b=path.read_bytes();assert hashlib.sha256(b).hexdigest()==file['sha256'];local_objects=0;local_terrain=0
        for s in fmt.parse(b):
            if s.signature==0x6E:
                for r in s.records:terrain+=1;local_terrain+=1;terrain_unknown[(fmt.u16(r,0x198),fmt.u32(r,0x224))]+=1
            if s.signature!=0xF0:continue
            for r in s.records:
                objects+=1;local_objects+=1;v=inspect(r)
                for f in v['fields']:
                    if f['name']=='ManaGen':mana_gen[f['native_editor_value']]+=1
                    if not f['gui_value_in_range']:
                        gui_limits[f['name']]+=1
                        gui_candidates.append(dict(file=file['file'],name=v['name'],object_id=v['object_id'],field=f['name'],native_editor_value=f['native_editor_value']))
                    if f['runtime_copy_bits'] is not None and f['raw']>65535:narrowing[f['name']]+=1
                entry=dict(file=file['file'],name=v['name'],object_id=v['object_id'])
                if v['flags']['decode_encode_would_change']:native_flag_loss.append(dict(**entry,projection=v['flags']))
                if v['flags']['type_bits_cleared']:type_loss.append(dict(**entry,projection=v['flags']))
                if v['speed']['quotient_narrowed']:speed_issues.append(dict(**entry,projection=v['speed']))
        files.append(dict(file=file['file'],sha256=file['sha256'],objects=local_objects,terrain=local_terrain))
    validation=json.loads((ROOT/'validation.json').read_text(encoding='utf8'))
    for file in validation['files']:
        path=DATA/('maps/'+file['file'] if file['file']=='kop4Globals.kld' else file['file']);assert hashlib.sha256(path.read_bytes()).hexdigest()==file['sha256']
    for file,digest in SCHEMA['executables'].items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    result=dict(synthetic_cases_passed=cases,actual_scalar_patch_roundtrips=patches,native_flag_emulations_verified=emulations,native_emulation_sha256=emu_hashes,
        evidence_excerpts_verified=lines_verified,evidence_instructions_verified=instructions,source_evidence_sha256=evidence_hashes,
        main_input_files_hashes_unchanged=len(validation['files']),executable_hashes_unchanged=SCHEMA['executables'],scopes=scopes,
        scenario_ticks_per_second=20,scenario_note='Explicit audit scenario, not an inferred running-game TPS.',
        corpus=dict(files=files,object_records=objects,terrain_records=terrain,mana_gen_signed_gui_counts=dict(mana_gen),native_editor_unsupported_flag_candidates=native_flag_loss,
            native_panel_type_reduction_candidates=type_loss,gui_range_exceedance_counts=dict(gui_limits),gui_range_exceedance_candidates=gui_candidates,low16_narrowing_counts=dict(narrowing),speed_quotient_narrowing_candidates=speed_issues,
            terrain_unknown_pair_counts=[dict(raw198=a,raw224=b,count=n) for (a,b),n in sorted(terrain_unknown.items())]),
        limitation=SCHEMA['limitation'])
    (ROOT/'object-property-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects properties:',cases,'boundary cases;',patches,'whole-container patches;',emulations,'native flag emulations;',lines_verified,'excerpts;',instructions,'instructions')
    print('Targeted corpus:',len(files),'files;',objects,'Objects;',terrain,'Terrain; ManaGen:',dict(mana_gen),'; unsupported flag candidates:',len(native_flag_loss),'; type reduction:',len(type_loss))
    print('GUI limits:',dict(gui_limits),'; low16 narrowing:',dict(narrowing),'; speed narrowing20TPS:',len(speed_issues))

if __name__=='__main__':main()
