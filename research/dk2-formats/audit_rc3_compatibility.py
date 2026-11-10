"""Audit RC3 nativeevidence andprofile-aware raw-preserving patches."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_rc3_compatibility import primary,converter_pairs,inputs,load,digest
from native_rc3_compatibility import PROFILES,RC3,PRO,extra_flags_view,patch_extra_flags
from emulate_rc3_creatures import cases,build_case
ROOT=Path(__file__).resolve().parent
def main():
    s=load('rc3-compatibility-semantics.json');assert s['primary_program_bytes']==primary() and s['converter_pairs']==converter_pairs() and s['inputs']==inputs();assert len(s['native_emulations'])==len(cases())==166
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(c);r=v['result'];assert c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0
    for sha,profile in PROFILES.items():
        off=profile['raw_offset']
        for length in [5449,5537]:
            for seed in [0,165,255]:
                base=bytes([seed])*length;view=extra_flags_view(base,sha)
                assert view['field_physically_present']==(off+4<=length) and view['raw_flags']==(seed*0x01010101 if off+4<=length else 0);checks+=1
                if off+4>length:
                    try:patch_extra_flags(base,sha,1,True)
                    except ValueError:checks+=1
                    else:raise AssertionError('Implicitlongextensionaccepted')
                    continue
                for bit in range(32):
                    mask=1<<bit
                    for enabled in [False,True]:
                        b=patch_extra_flags(base,sha,mask,enabled);expected=bytearray(base);prior=seed*0x01010101;wanted=prior|mask if enabled else prior&~mask;struct.pack_into('<I',expected,off,wanted);v=extra_flags_view(b,sha);assert b==expected and len(b)==length and v['raw_flags']==wanted and v['native_model_flags']==wanted&15 and b[:off]==base[:off] and b[off+4:]==base[off+4:];checks+=1
    for sha in ['rc3','pro','',RC3.upper(),None,[]]:
        try:extra_flags_view(bytes(5449),sha)
        except ValueError:checks+=1
        else:raise AssertionError('Guessededitorprofileaccepted')
    for raw in [b'',bytes(5448),bytes(5538)]:
        try:extra_flags_view(raw,RC3)
        except ValueError:checks+=1
        else:raise AssertionError('Invalidstrideaccepted')
    for mask,enabled in [(-1,True),(1<<32,True),(True,True),(1,1),(1,None)]:
        try:patch_extra_flags(bytes(5449),RC3,mask,enabled)
        except ValueError:checks+=1
        else:raise AssertionError('Invalidflagpatchaccepted')
    patches=0;byprofile=Counter();roundtrips=0
    for row in s['inputs']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1;si=next(i for i,q in enumerate(parts) if q.signature==170);sec=parts[si]
        for ri in sorted({0,len(sec.records)//2,len(sec.records)-1}):
            raw=sec.records[ri]
            for sha,p in PROFILES.items():
                off=p['raw_offset']
                if off+4>len(raw):continue
                for mask in [1,8,0x80000000,0xFFFFFFFF]:
                    for enabled in [False,True]:
                        changed=patch_extra_flags(raw,sha,mask,enabled);rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(b);absolute=sec.offset+len(sec.header)+sum(len(r) for r in sec.records[:ri])+off;expected[absolute:absolute+4]=changed[off:off+4];assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1;byprofile[p['name']]+=1
    # All82 historically inspectedCreaturecatalogues,notthenestedexporttree.
    corpus=load('creature-extension-semantics.json')['corpus']['files']
    for row in corpus:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b
    for row in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/row[0]).read_bytes())==row[1]
    rc3source=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3source['original_file']).read_bytes())==digest(Path(rc3source['research_copy']).read_bytes())==RC3
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==61 and package['rc3_compatibility_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=50,date='2026-10-26',native_emulations=166,native_case_kinds=dict(Counter(c['kind'] for c in cases())),whole_main_converter_pairs_equivalent_except_calls=10,external_helper_pairs_byte_identical=2,primary_function_ranges_matching_pe=37,api_boundary_cases=checks,exact_in_memory_container_patches=patches,patches_by_profile=dict(byprofile),whole_creature_catalogue_roundtrips=len(corpus),original_executables_unchanged=4,format_package_components=61,research_writes_to_game_data=0,modified_gameplay_runs=0,input_scope='UnchangedCreaturecatalogues+3explicitinputs;phase47/49AutoSavesnapshotsarenotrewrittenorclaimedcurrent.',limitations=s['limitations'])
    (ROOT/'phase50-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
