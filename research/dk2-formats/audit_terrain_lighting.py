"""Recheck native evidence, every UI channel value and exact container patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_terrain_lighting import primary,corpus,digest,load
from native_terrain_lighting import GROUPS,view,patch_channel,flags_projection,game_colour_fragment
from emulate_terrain_lighting import cases,build_case
ROOT=Path(__file__).resolve().parent

def main():
    s=load('terrain-lighting-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==99
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(c);r=v['result'];assert v['case']==c and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0
    # Exhaust all512 GUI values per channel against three distinct opaque inputs.
    for seed in [0,165,255]:
        base=bytes([seed])*552;oldflags=struct.unpack_from('<I',base,0x190)[0]
        for name,g in GROUPS.items():
            for ch,mask in enumerate(g['masks']):
                for value in range(512):
                    raw=patch_channel(base,name,ch,value);w=bytearray(base);w[g['offset']+ch]=value%256;f=oldflags|mask if value>=256 else oldflags&~mask;struct.pack_into('<I',w,0x190,f);v=view(raw)
                    assert raw==w and v[name]['editor_values'][ch]==value and v[name]['game_loaded_words'][ch]==value-(value>=256) and v[name]['enabled']==bool(oldflags&g['enable'])
                    assert patch_channel(raw,name,ch,value)==raw;checks+=1
    # Independent bit-by-bit oracle for decode/encode projection, each enable state.
    for enable in [0,0x10000000,0x20000000,0x30000000]:
        for bit in range(32):
            flags=(1<<bit)|enable;expect=0
            for k in [*range(19),23,24,28,29]:expect|=flags&(1<<k)
            for g in GROUPS.values():
                if flags&g['enable']:
                    for m in g['masks']:expect|=flags&m
            assert flags_projection(flags)==expect;checks+=1
    base=bytes(552)
    invalid=[(base,'other',0,0),(base,[],0,0),(base,'terrain',True,0),(base,'ambient',-1,0),(base,'terrain',3,0),(base,'terrain',0,-1),(base,'terrain',0,512),(base,'terrain',0,True),(base,'terrain',0,1.0)]
    for args in invalid:
        try:patch_channel(*args)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid channel patch accepted')
    for raw in [b'',bytes(551),bytes(553),'x'*552]:
        try:view(raw)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid Terrain buffer accepted')
    for value in [-1,1<<32,True,1.0]:
        try:flags_projection(value)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid flag DWORD accepted')
    # Flag presence0 retains prior flags; channel presences vary independently.
    partial_checks=0
    for name,g in GROUPS.items():
        for ch,mask in enumerate(g['masks']):
            for flagpresent in [0,1]:
                for present in [0,1]:
                    mid=bytearray(736);previous=bytearray(460);struct.pack_into('<I',previous,0x17B,mask);struct.pack_into('<H',previous,g['runtime'][ch],65530);struct.pack_into('<I',mid,0xB0,flagpresent);struct.pack_into('<I',mid,g['presence']+ch*4,present);mid[g['mid']+ch]=128
                    result=game_colour_fragment(mid,previous);expected=(128 if present else 65530)+(0 if flagpresent else 255);assert struct.unpack_from('<H',result,g['runtime'][ch])[0]==expected%65536;partial_checks+=1
    patches=0
    for path in [DATA/'Terrain.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==110);section=parts[si]
        for ri in sorted({0,len(section.records)//2,len(section.records)-1}):
            raw=section.records[ri]
            for name,g in GROUPS.items():
                for ch in range(3):
                    for value in [0,255,256,511]:
                        changed=patch_channel(raw,name,ch,value);rs=list(section.records);rs[ri]=changed;sec=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);sec.replace_records(rs);packed=b''.join(sec.pack() if j==si else q.pack() for j,q in enumerate(parts));wanted=bytearray(original);absolute=section.offset+len(section.header)+ri*552
                        wanted[absolute+g['offset']+ch]=changed[g['offset']+ch];wanted[absolute+0x190:absolute+0x194]=changed[0x190:0x194];assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b
    captured={str(Path(r['file']).resolve()).lower():r for r in load('phase47-input-manifest.json')['main_files']};matches=0;drift=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        cap=captured[str(p.resolve()).lower()];b=Path(cap['snapshot']).read_bytes() if 'snapshot' in cap else p.read_bytes();sha=digest(b);assert sha==cap['sha256']
        if sha==row['sha256']:matches+=1
        else:drift.append(dict(file=str(p),historical_sha256=row['sha256'],phase47_snapshot_sha256=sha))
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');assert len(package['components'])>=59 and package['terrain_lighting_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=48,date='2026-10-26',native_emulations=99,native_case_kinds=dict(Counter(c['kind'] for c in cases())),api_boundary_cases=checks,additional_partial_presence_checks=partial_checks,exact_in_memory_container_patches=patches,whole_file_roundtrips=s['corpus']['file_count'],corpus_records=s['corpus']['records'],primary_function_ranges_matching_pe=len(s['primary_program_bytes']),evidence_instructions=sum(len(e['instructions']) for e in s['evidence']),main_inputs_matching_historical_baseline=matches,main_input_phase47_snapshot_drift=drift,original_executables_unchanged=3,format_package_components=len(package['components']),research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase48-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
