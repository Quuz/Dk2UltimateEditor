"""Reproduce saved native calls, byte provenance, source patches and package."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from build_camera_activation_semantics import ROOT,load,digest,primary
from build_effect_semantics import pe_read
from emulate_camera_activation import cases,build_case,initial,raw,BASE,CAP
from native_camera_activation import activate,get
from native_cameras import FIELD_WIDTHS,patch_raw,clamp_exclusive

def main():
    s=load('camera-activation-semantics.json');funcs,blocks=primary();assert s['primary_program_bytes']==funcs and s['primary_data_ranges']==blocks and len(funcs)==7 and len(blocks)==1
    exe=Path(funcs[0]['exe']).read_bytes();assert digest(exe)==s['executable_sha256']
    # Exact native duplicate-bound stores and source9 alias instruction.
    assert pe_read(exe,0x44B312,12)==bytes.fromhex('8985580d000089855c0d0000')
    assert pe_read(exe,0x44AF5F,6)==bytes.fromhex('8db580050000')
    selected=cases();assert len(selected)==len(s['native_emulations'])==68
    buffers={};activation_calls=0;chain_calls=0
    for c,row in zip(selected,s['native_emulations']):
        p=ROOT/'ghidra'/row['file'];saved=load('ghidra/'+row['file']);args,want=build_case(c);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['input']==args and saved['expected_registers']==want and saved['case']==c and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in want.items())
        mem={int(q['address'],16):bytes.fromhex(q['hex']) for q in json.loads(args['memory'])['regions']};buffers[c['name']]=mem
        for f in funcs:
            assert all(a+len(b)<=f['address'] or a>=f['address']+f['length'] for a,b in mem.items()),(c['name'],hex(f['address']))
        activation_calls+=1+int('next_mode' in c);chain_calls+=int('chain_index' in c)
    assert activation_calls==69 and chain_calls==19
    # These checks describe the native fixture outcome already byte-compared,
    # independently exposing the states used for concrete semantic assertions.
    for mode in [13,14,15]:
        m=buffers[f'mode-{mode}'][BASE+0x2000]
        assert get(m,0xD58)==get(m,0xD5C)==1024 and get(m,0xDD0)==1023
        assert get(m,0xDBE)==303+9*17
    for mode in [7,18]:
        c=next(c for c in selected if c['name']==f'mode-{mode}');before=initial(c);after=buffers[c['name']][BASE+0x2000]
        assert after[0xD38:0xDA8]==before[0xD38:0xDA8] and get(after,0xDA8)==get(after,0xDD4)==mode and get(buffers[c['name']][CAP],24)==0
    for name in ['blocked-3-1','blocked-1-2','blocked-2-1']:
        c=next(c for c in selected if c['name']==name);before=initial(c);after=buffers[name][BASE+0x2000];cap=buffers[name][BASE+0x11000]
        assert after!=before and after[:0xC00]==before[:0xC00] and get(cap,60)==0 and get(cap,24)==0
        assert struct.unpack_from('<H',after,0xDEC)[0]==1 and get(after,0xDAC)==c['old']
    cap=buffers['new-lock-blocks-next-call'][BASE+0x11000];assert get(cap,44)==1 and get(cap,60)==0 and get(cap,24)==1
    checks=0
    # Rawmin at INT_MIN is the explicit exception to the equal-bound lower-1
    # statement, because decrement wraps to INT_MAX before signed comparison.
    for value,lo in [(4096,1024),(4096,0),(0xFFFFFFFF,0xFFFFFFFF),(4096,0x80000000)]:
        got=clamp_exclusive(value,lo,lo)
        assert got==(4096 if lo==0x80000000 else (lo-1)&0xFFFFFFFF);checks+=1
    for mode in [0,6,8,12,16,17,19]:assert get(buffers[f'mode-{mode}'][BASE+0x11000],60)==0;checks+=1
    # Byte-preserving edits remain different from activation normalization.
    patches=files=records=0
    for row in s['corpus']['files']:
        source=Path(row['file']).read_bytes();assert digest(source)==row['sha256'];parts=fmt.parse(source);assert b''.join(q.pack() for q in parts)==source;files+=1
        assert parts[0].signature==230 and len(parts[0].records)==621
        for si,sec in enumerate(parts):
            if sec.signature!=190:continue
            relative=len(sec.header)
            for ri,r in enumerate(sec.records):
                assert fmt.u32(r)==205;records+=1
                for off,n in FIELD_WIDTHS.items():
                    value=int.from_bytes(r[8+off:8+off+n],'little')^((1<<(8*n))-1)
                    changed=patch_raw(r,off,value);rr=list(sec.records);rr[ri]=changed
                    new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rr)
                    packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));manual=bytearray(source);at=sec.offset+relative+8+off;manual[at:at+n]=value.to_bytes(n,'little')
                    assert packed==manual and packed[:sec.offset]==source[:sec.offset] and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                relative+=len(r)
    assert files==3 and records==117 and patches==2808
    # Separate bounded negative oldCreature search: no consumer purpose added.
    search_rows=[]
    for filename,states,leads,contexts in [('phase63-old-creature-thiscall-leads.json',14495,333,46),('phase63-old-creature-argument-leads.json',14506,335,77)]:
        q=load(filename);assert len(q['producer_roots'])==822 and q['root_states']==states and len(q['candidates'])==leads and q['callee_summaries']==contexts and q['limited_roots']==q['limited_callees']==0
        assert all(h['field'] in {'controlHealth','controlCreatureFlags'} for h in q['candidates']);search_rows.append(dict(file=filename,sha256=digest((ROOT/filename).read_bytes()),roots=822,root_states=states,control_reads=leads,callee_contexts=contexts,limited_roots=0,limited_callees=0))
    # Allfour original EXEs remain exactly the historical sourcehashes.
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];rc=load('camera-catalogue-semantics.json')['rc3']
    assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['camera_semantics']['phase63_activation']==s
    assert load('camera-semantics.json')['phase63_activation']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    validation=dict(phase=63,date='2026-10-27',native_scenarios=68,full_native_activation_calls=activation_calls,raw_to_activation_chains=chain_calls,complete_executed_native_functions=7,primary_full_pe_functions=7,primary_dispatch_table_ranges=1,whole_manager_bytes_compared=0xF00,source_files_roundtripped=files,camera_records_counting_copies=records,distinct_source_records=39,exact_in_memory_field_patches=patches,semantic_boundary_checks=checks,negative_old_creature_search=search_rows,source_executables_unchanged=4,package_components=67,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase63-validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in validation.items() if k!='limitations'}))
if __name__=='__main__':main()
