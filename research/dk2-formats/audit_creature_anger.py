"""Audit native mood evidence, exact parameter patches and bounded search exclusions."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_creature_anger import ROOT,load,digest,primary,corpus
from emulate_creature_anger import cases,build_case
from native_creature_anger import threshold,project,mean,max_index,patch_raw,mood_action_gate
def main():
    s=load('creature-anger-semantics.json');p=primary();assert s['primary_program_bytes']==p['functions'] and s['primary_data_ranges']==p['blocks'] and s['corpus']==corpus() and len(cases())==len(s['native_emulations'])==129;counts=Counter();events=0
    for row,c in zip(s['native_emulations'],cases()):
        path=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert digest(path.read_bytes())==row['sha256'] and c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items());counts[c['kind']]+=1
        m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};expected=m[0x10030000];events+=struct.unpack_from('<I',m[0x10042000],0)[0]
        assert all(a not in m for a in [0x478B80,0x486E00,0x486E20,0x486ED0,0x486F80,0x487030,0x4870E0,0x4871E0,0x4871B0,0x518AD0])
        if c['kind']=='action':
            assert 'anger-v2-' in row['file'] and m[0x10038000][0x72A1]==mood_action_gate(c['gate'],c['available']);assert struct.unpack_from('<I',expected,0x26E+20)[0]==(2000 if c['available']==1 else 1000);assert m[0x10050000]==m[0x10050200]
        if c['kind']=='threshold':assert struct.unpack_from('<I',expected,0x286)[0]==(threshold(c['p']) if c['presence'] else 0xBEEF)
        if c['kind']=='gift' and c['emotionless']==4:
            vals=struct.unpack_from('<6I',expected,0x26E);assert vals[5]==40000 and vals[1 if c['route']=='chicken' else 4]==0
    patches=roundtrips=records=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,sec in enumerate(parts):
            if sec.signature!=170:continue
            rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                records+=1
                for off,n in [(0xF4F,1),(0xFCB,2)]:
                    value=int.from_bytes(raw[off:off+n],'little')^((1<<(8*n))-1);changed=patch_raw(raw,off,value);rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(b);absolute=sec.offset+rel+off;manual[absolute:absolute+n]=changed[off:off+n];assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                rel+=len(raw)
    creature_patches=patches;action_patches=troundtrips=0
    for row in s['corpus']['trigger_files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;troundtrips+=1
        for si,sec in enumerate(parts):
            if sec.signature!=210:continue
            rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                if fmt.u32(raw)==214 and raw[22]==44:
                    changed=bytearray(raw);changed[8]=0 if raw[8]==1 else 1;rs=list(sec.records);rs[ri]=bytes(changed);new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(b);manual[sec.offset+rel+8]=changed[8];assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;action_patches+=1
                rel+=len(raw)
    api=0
    for value,want in [(0,0),(50,16383),(70,22936),(100,32767),(101,33094),(200,65534),(201,325),(255,18019)]:assert threshold(value)==want;api+=1
    for bad in [-1,256,True,'50']:
        try:threshold(bad)
        except ValueError:api+=1
        else:raise AssertionError(bad)
    for old in [0,165,255]:
        for available in [0,1,2,255]:assert mood_action_gate(old,available)&254==old&254 and (mood_action_gate(old,available)&1)==int(available==1);api+=1
    for mode,kwargs in [('bad',{}),('add',dict(index=6)),('add',dict(gate=True)),('percent_add',dict(value=-1))]:
        try:project(bytes(32),mode,**kwargs)
        except ValueError:api+=1
        else:raise AssertionError(mode)
    for n in [31,33]:
        for fn in [mean,max_index]:
            try:fn(bytes(n))
            except ValueError:api+=1
            else:raise AssertionError(fn)
    for fn in [mean,max_index]:assert fn(struct.pack('<8I',5,5,5,5,5,5,1,0))==(5 if fn==mean else 0);api+=1
    for n in [5449,5537]:
        raw=bytes([165])*n
        for off,value in [(0xF4F,256),(0xFCB,65536),(0xF4F,-1),(0xFCB,True),(0,0)]:
            try:patch_raw(raw,off,value)
            except ValueError:api+=1
            else:raise AssertionError((off,value))
    for row in s['unknown_search']['evidence']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256'];rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['creature_anger_semantics']==s and pack['trigger_parameter_layouts']==load('trigger-layouts.json');assert pack['trigger_parameter_layouts']['0xD6']['44']['native_evidence']=='creature-anger-semantics.json;DX518AD0→519704'
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=57,date='2026-10-27',native_emulations=129,native_case_kinds=dict(counts),complete_primary_pe_functions=20,primary_data_ranges=7,observed_actor_notification_cases=events,api_boundary_checks=api,creature_whole_file_roundtrips=roundtrips,creature_records=records,creature_exact_patches=creature_patches,trigger_whole_file_roundtrips=troundtrips,trigger44_exact_patches=action_patches,total_exact_in_memory_patches=creature_patches+action_patches,package_components=67,original_executables_unchanged=4,research_writes_to_game_data=0,modified_gameplay_runs=0,unknown_fields_still_open=['AbilityJump75C','PickUpPriority6EB','Other phase56 unresolved fields'],limitations=s['limitations'])
    assert roundtrips==82 and records==3734 and creature_patches==7468 and troundtrips==110 and action_patches==9
    (ROOT/'phase57-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
