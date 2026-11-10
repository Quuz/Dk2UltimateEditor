"""Audit full buffer evidence, expected faults and exact new light-byte patches."""
from pathlib import Path
from collections import Counter
import json
import struct
import dk2_formats as fmt
from build_object_loading_semantics import ROOT,load,digest,primary
from native_object_loading import *
from emulate_object_loading import cases,build_case,TOKEN


def main():
    s=load('object-loading-semantics.json');functions,blocks,boundaries=primary()
    assert s['primary_code_extents']==functions and s['primary_data_ranges']==blocks and s['canonical_boundary_review']==boundaries
    assert len(cases())==len(s['native_emulations'])==90
    counts,buffers,soundcalls=Counter(),{},0
    for c,row in zip(cases(),s['native_emulations']):
        path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text());args,want=build_case(c);r=saved['result']
        assert row['sha256']==digest(path.read_bytes()) and saved['input']==args and saved['case']==c and saved['expected_registers']==want
        assert row['expected_fault']==(c['kind']=='fault');counts[c['kind']]+=1
        m={int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        for fn in functions:
            if fn['program']==args['program']:
                assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in m.items())
        if c['kind']=='fault':
            assert not r['success'] and not r['hit_return'] and int(r['final_pc'],16)==profile(c['sha'])['divide_address']
            assert r['emulation_error']=='Remainder by 0'
            continue
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items())
        buffers[c['name']]=m;soundcalls+=int.from_bytes(m[0x10006400][:4],'little')
        mid,prev,out=m[0x10003000],m[0x10004000],m[0x10005000]
        if c['kind']=='init':assert mid==bytes(1022) and prev==out
        elif c['kind']=='converter':
            assert prev==out and all(struct.unpack_from('<I',mid,i)[0]==1 for i in range(0,128,4))
            assert mid[0x3C1:0x3C4]==bytes([c['seed']])*3
        else:
            applied=struct.unpack_from('<I',mid,0x50)[0]
            assert out[0x106:0x108]==(mid[0x3CC:0x3CE] if applied else prev[0x106:0x108])
            if struct.unpack_from('<I',mid,0x30)[0]:
                expected=struct.unpack_from('<I',mid,0x3A4)[0]|int(any(mid[0x3A8:0x3AB]))
                assert struct.unpack_from('<I',out,0xE8)[0]==expected
            if c['kind']=='chain':assert mid[0x3C1:0x3C4]==bytes(3)
    assert counts==dict(init=6,converter=12,loader=18,chain=50,fault=4)
    for sha in PROFILES:
        for gate in [0,1]:
            base=buffers[f'chain-{sha[:3]}-pad-0-gate-{gate}']
            for pad in [255,'pattern']:
                other=buffers[f'chain-{sha[:3]}-pad-{pad}-gate-{gate}']
                assert base[0x10003000]==other[0x10003000] and base[0x10005000]==other[0x10005000]
        for flags in [0,0x80000000]:
            for color in ['00000000','01000000','00010000','00000100','000000ff']:
                out=buffers[f'chain-{sha[:3]}-light-{flags}-{color}'][0x10005000]
                assert int.from_bytes(out[0xE8:0xEC],'little')==flags|int(any(bytes.fromhex(color)[:3]))
        out=buffers[f'chain-{sha[:3]}-light-black-flag1'][0x10005000];assert int.from_bytes(out[0xE8:0xEC],'little')==1
        out=buffers[f'chain-{sha[:3]}-speed-1310720'][0x10005000];assert int.from_bytes(out[0x100:0x102],'little')==1
        out=buffers[f'chain-{sha[:3]}-speed-0'][0x10005000];assert int.from_bytes(out[0x100:0x102],'little')==0
    coverage=Counter(range(128))
    for _,src,n in CONVERT:coverage.update(range(src+128,src+128+n))
    for _,src in RESOURCES:coverage.update(range(src+128,src+128+84))
    for a,n in [(128,32),(0x3DE,32),(0x394,24),(0x3C4,4)]:coverage.update(range(a,a+n))
    assert set(range(1022))-set(coverage)==set(range(0x3C1,0x3C4)) and set(coverage.values())=={1}
    files=records=patches=0
    for row in s['corpus']['files']:
        source=Path(row['file']).read_bytes();assert digest(source)==row['sha256']
        parts=fmt.parse(source);assert b''.join(q.pack() for q in parts)==source;files+=1
        for si,sec in enumerate(parts):
            if fmt.FAMILIES.get(sec.signature)!='Objects':continue
            relative=len(sec.header)
            for ri,raw in enumerate(sec.records):
                color=bytes(v^255 for v in raw[0x328:0x32B]);flags=fmt.u32(raw,0x324)^1
                for off,value,changed in [(0x328,color,patch_light_color_bytes(raw,color)),(0x324,flags.to_bytes(4,'little'),patch_light_flags(raw,flags))]:
                    rr=list(sec.records);rr[ri]=changed
                    new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr)
                    packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts))
                    manual=bytearray(source);at=sec.offset+relative+off;manual[at:at+len(value)]=value
                    assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                records+=1;relative+=len(raw)
    assert (files,records,patches)==(84,11756,23512)
    checks=0
    invalid=[lambda:patch_light_color_bytes(bytes(894),bytes(4)),lambda:patch_light_flags(bytes(894),True),
             lambda:patch_light_flags(bytes(894),-1),lambda:intermediate(bytes(893),bytes(1022)),
             lambda:definition(bytes(1022),bytes(285),'1.3',20),lambda:sound_requested(bytes(1022),DX,True)]
    present=bytearray(1022);struct.pack_into('<I',present,0x44,1)
    invalid.append(lambda:definition(present,bytes(285),DX,0))
    for fn in invalid:
        try:fn()
        except ValueError:checks+=1
        else:raise AssertionError('Invalid API input accepted')
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['object_property_semantics']['phase71_loading']==s
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=71,date='2026-10-27',successful_native_cases=86,expected_native_DIV0_stops=4,native_cases=dict(counts),captured_sound_lookups=soundcalls,
        complete_executed_native_functions=8,primary_code_extents=15,primary_data_ranges=3,canonical_false_anchors_rejected=6,
        source_files_roundtripped=files,object_records=records,new_exact_light_span_patches=patches,api_boundary_checks=checks,
        source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,Objects34C_gameplay_still_open=True,limitations=s['limitations'])
    (ROOT/'phase71-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))


if __name__=='__main__':main()
