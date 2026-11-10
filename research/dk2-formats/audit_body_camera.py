"""Check nativekind6camera/PRNG,primaryidentity,rawDWORD preservation andpackage."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_body_camera_semantics import ROOT,load,digest,link,primary
from build_effect_semantics import pe_read
from emulate_body_camera import cases,build_case,table,BASE
from native_camera_activation import get,word
from native_creature_editor_scaled import patch_raw

def main():
    s=load('body-camera-semantics.json');fs,bs=primary();assert s['primary_program_bytes']==fs and s['primary_data_and_fragments']==bs and len(fs)==10 and len(bs)==7
    data=Path(fs[0]['exe']).read_bytes();assert digest(data)==s['executable_sha256'];actuals={};counts=Counter()
    executed=[f for f in fs if f['execution_scope'].startswith('full function')];assert len(executed)==5
    for c,row in zip(cases(),s['native_emulations']):
        p=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert digest(p.read_bytes())==row['sha256'] and v['input']==args and v['expected_registers']==want and v['case']==c and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==n for k,n in want.items())
        m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};actuals[c['name']]=m;counts[c['kind']]+=1
        for f in executed:assert all(a+len(b)<=f['address'] or a>=f['address']+f['length'] for a,b in m.items())
        if c.get('loader'):
            assert {a for a in m if 0x400000<=a<0x600000 and a not in [0x5B5E90,0x571DA0,0x5589F0,0x44D6D0,0x5976D0,0x597690]}=={0x492320,0x4923D6}
    assert len(cases())==len(s['native_emulations'])==44 and counts==dict(body=24,rng=20)
    # Nativekind/setterchain and classRTTI giveindependentobjectidentity.
    rtti=s['body_identity']['rtti'];native_type=b'.?AVCDeadBody@@\0';assert pe_read(data,rtti['type_descriptor']+8,len(native_type))==native_type
    assert struct.unpack('<I',pe_read(data,rtti['vtable']-4,4))[0]==rtti['locator']
    assert pe_read(data,0x4B0F3A,2)==bytes.fromhex('6a06') and pe_read(data,0x4B4A32,3)==bytes.fromhex('894112')
    assert pe_read(data,0x4B0F5D,6)==bytes.fromhex('898629010000')
    assert pe_read(data,0x568E9D,6)==bytes.fromhex('8b83b00e0000') and pe_read(data,0x4923CF,7)==bytes.fromhex('6689835c060000')
    trig=table();assert len(trig)==0x2800 and digest(trig)==s['selection']['table']['sha256']
    tx=struct.unpack('<2560i',trig);assert [tx[i] for i in [0,512,1024,1536,2048]]==[0,65536,0,-65536,0]
    assert [tx[512+i] for i in [0,512,1024,1536]]==[65536,0,-65536,0]
    for a,pair in [(0,(11111,22222+4096)),(512,(11111-4096,22222)),(1024,(11111,22222-4096)),(1536,(11111+4096,22222))]:
        m=actuals[f'body-angle-{a}-eye-4096'];assert struct.unpack_from('<3I',m[BASE+0x2000],0xDB6)==(*pair,1024)
    # IMUL low32 thenSAR means65535*65536 becomesnegative,soprojectionmust
    # not replace native32bitmath with an unbounded/64bitproduct.
    assert get(actuals['body-angle-512-eye-65535'][BASE+0x2000],0xDB6)==11112
    hi=actuals['body-eye-high-word-loader'];lo=actuals['body-eye-low-word-loader']
    assert hi[BASE+0x2000]==lo[BASE+0x2000] and hi[BASE+0x19000]==lo[BASE+0x19000]
    assert get(hi[BASE+0x25000],0x12A0)==0xFFFF1000 and get(lo[BASE+0x25000],0x12A0)==0x1000
    assert word(actuals['body-eye-absent-loader'][BASE+0x19000],0x65C)==8192
    for name,m in actuals.items():
        if name.startswith('rng-'):continue
        before=m[BASE+0x16000];after=m[BASE+0x17000];valid=name!='body-invalid-node-fallback'
        expected=bytearray(before);expected[0xF3]&=(254 if valid else 255);assert after==expected
    b=actuals['body-force-still-transition'][BASE+0x2000];assert get(b,0xDD8)==123456+3000
    b=actuals['body-clock-wrap'][BASE+0x2000];assert get(b,0xDD8)==(0xFFFFFF00+3000)&0xFFFFFFFF
    # Changing all32physicalEyeHeightbits mustpreserveallotherspans,including
    # editor/gameflags,unknownlongsuffix,othercataloguesandcontainerheaders.
    files=records=patches=0;drift=[]
    for row in s['corpus']['files']:
        source=Path(row['input_path']).read_bytes();assert digest(source)==row['sha256'];parts=fmt.parse(source);assert b''.join(q.pack() for q in parts)==source;files+=1;count=0
        if row['sha256']!=row['historical_sha256']:drift.append(row)
        for si,sec in enumerate(parts):
            if fmt.FAMILIES.get(sec.signature)!='Creatures':continue
            relative=len(sec.header)
            for ri,raw in enumerate(sec.records):
                count+=1;records+=1;value=get(raw,0xEB0)^0xFFFFFFFF;changed=patch_raw(raw,0xEB0,value);rr=list(sec.records);rr[ri]=changed
                new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr)
                packed=b''.join(new.pack() if i==si else q.pack() for i,q in enumerate(parts));manual=bytearray(source);at=sec.offset+relative+0xEB0;manual[at:at+4]=value.to_bytes(4,'little')
                assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1;relative+=len(raw)
        assert count==row['records']
    assert files==82 and records==patches==3734
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];rc=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(old['original_path']).read_bytes())==old['sha256'] and digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['camera_semantics']['phase65_body_camera']==s
    assert package['dead_body_semantics']['phase65_camera_consumer']==link('body-camera-semantics.json')
    for key,r in package['components'].items():
        b=(ROOT/r['file']).read_bytes();assert digest(b)==r['sha256'] and json.loads(b)==package[key]
    for reference in [s['eye_height']['label_evidence'],*s['bounded_search_followup'].values()]:
        if isinstance(reference,dict):assert link(reference['file'])==reference
    v=dict(phase=65,date='2026-10-27',native_scenarios=44,native_cases=dict(counts),complete_activation_calls=24,body_kind6_activation_calls=23,invalid_node_fallback_calls=1,standalone_native_PRNG_calls=20,EyeHeight_assignment_fragment_chains=3,complete_executed_native_functions=5,full_primary_pe_functions=10,static_only_primary_functions=5,explicit_data_and_fragment_ranges=7,trig_PE_bytes=10240,creature_source_files_roundtripped=files,creature_records=records,exact_in_memory_EyeHeight_DWORD_patches=patches,source_files_changed_since_phase43=drift,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase65-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))
if __name__=='__main__':main()
