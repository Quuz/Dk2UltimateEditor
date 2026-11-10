"""Check full output/models,unassignedstackspans,patchpreservation andpackage."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_door_writer_semantics import ROOT,load,digest,link,primary
from build_effect_semantics import pe_read
from native_door_writer import PROFILES,GAPS,patch_word236,patch_untransferred
from emulate_door_writer import cases,build_case

def main():
    s=load('door-writer-semantics.json');fs,bs=primary();assert s['primary_program_bytes']==fs and s['primary_data_and_static_ranges']==bs and len(fs)==10 and len(bs)==4
    memories={};per=Counter();records=0
    for c,row in zip(cases(),s['native_emulations']):
        p=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert digest(p.read_bytes())==row['sha256'] and v['case']==c and v['input']==args and v['expected_registers']==want and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==n for k,n in want.items());per[c['profile']]+=1;records+=sum(bool(n) for n in c['slots'])
        m={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};memories[c['name']]=m
        for f in fs:
            if f['program']==args['program']:assert all(a+len(b)<=f['address'] or a>=f['address']+f['length'] for a,b in m.items())
    assert len(cases())==len(s['native_emulations'])==42 and per==dict(Pro=21,RC3=21) and records==50
    for profile in PROFILES:
        # Headers declarephysicalarrayslotcountwhilepayloadskipsnulls.
        for suffix,n,actual in [('slots-',0,0),('slots-0',1,0),('slots-102',3,2),('slots-21',2,2)]:
            out=memories[profile+'-'+suffix][0x10014000];assert len(out)==56+actual*616 and fmt.u32(out,20)==n and fmt.u32(out,8)==56+n*616 and fmt.u32(out,52)==n*616
        for value in [0,1,32767,32768,65535]:
            out=memories[f'{profile}-word236-{value}'][0x10014000];assert fmt.u16(out,56+0x236)==value
        for suffix,seed in [('scratch-0',0),('word236-65535',165),('scratch-255',255)]:
            out=memories[f'{profile}-{suffix}'][0x10014000]
            for ri in range((len(out)-56)//616):
                for off,n in GAPS:assert out[56+ri*616+off:56+ri*616+off+n]==bytes([seed])*n
        carry=memories[profile+'-name-carry-to-next-record'][0x10014000]
        for ri in [0,1]:
            for off,n in GAPS:assert carry[56+ri*616+off:56+ri*616+off+n]==b'Q'*n
        for flag in [0,32,64,128]:assert fmt.u32(memories[f'{profile}-flags-{flag}'][0x10014000],56+0x228)==flag
    # Nativehelpers areactuallyidenticalwhere claimed;writerdifferencesmust
    # remainexplicitprofilesratherthanuniversaleditorversionlabels.
    for helper in [0x41CE60,0x41F4E0,0x4ACDA8,0x4AB3B0]:
        a=next(r for r in fs if r['profile']=='Pro' and r['address']==helper);b=next(r for r in fs if r['profile']=='RC3' and r['length']==a['length'])
        assert pe_read(Path(a['exe']).read_bytes(),a['address'],a['length'])==pe_read(Path(b['exe']).read_bytes(),b['address'],b['length'])
    for b in bs[:2]:assert struct.unpack('<d',bytes.fromhex(b['hex']))[0]==4096
    old=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();assert struct.unpack('<I',pe_read(old,0x65F31C+0x2C8,4))[0]==0x504140
    q=load('phase66-old-ap-pointer-leads.json');review=load('phase66-old-ap-search-review.json');assert review['search']==link('phase66-old-ap-pointer-leads.json') and len(q['producer_roots'])==2553 and q['root_states']==121718 and len(q['candidates'])==234 and q['limited_roots']==8
    files=recordcount=patches=0
    for row in s['corpus']['files']:
        source=Path(row['file']).read_bytes();assert digest(source)==row['sha256'];parts=fmt.parse(source);assert b''.join(q.pack() for q in parts)==source;files+=1;count=0
        for si,sec in enumerate(parts):
            if fmt.FAMILIES.get(sec.signature)!='Doors':continue
            relative=len(sec.header)
            for ri,raw in enumerate(sec.records):
                count+=1;recordcount+=1
                for off,n in [(0x236,2),*GAPS]:
                    value=int.from_bytes(raw[off:off+n],'little')^((1<<(8*n))-1)
                    changed=patch_word236(raw,value) if off==0x236 else patch_untransferred(raw,off,value)
                    rr=list(sec.records);rr[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr)
                    packed=b''.join(new.pack() if i==si else q.pack() for i,q in enumerate(parts));manual=bytearray(source);at=sec.offset+relative+off;manual[at:at+n]=value.to_bytes(n,'little');assert packed==manual and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                relative+=len(raw)
        assert count==row['records']
    assert files==82 and recordcount==656 and patches==2624
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    oldsrc=load('creature-version-layout-semantics.json')['old_source'];rc=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(oldsrc['original_path']).read_bytes())==oldsrc['sha256'] and digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==67 and pack['door_semantics']['phase66_writer']==s
    for key,row in pack['components'].items():
        raw=(ROOT/row['file']).read_bytes();assert digest(raw)==row['sha256'] and json.loads(raw)==pack[key]
    v=dict(phase=66,date='2026-10-27',native_full_writers=42,full_writers_by_profile=dict(per),whole_emitted_records=50,header_bytes_compared_per_case=56,record_bytes_compared=616,complete_executed_native_functions=10,primary_data_and_static_ranges=4,unassigned_record_bytes_per_bounded_record=8,source_files_roundtripped=files,door_records=recordcount,exact_in_memory_span_patches=patches,oldAP_search_roots=2553,oldAP_search_states=121718,oldAP_search_leads=234,oldAP_limited_roots=q['limited_roots'],oldAP_limited_callees=q['limited_callees'],source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase66-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))
if __name__=='__main__':main()
