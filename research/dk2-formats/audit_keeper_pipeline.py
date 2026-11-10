"""Recheck native evidence, corpus bytes, and exact in-memory container edits."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_keeper_pipeline import *
from emulate_keeper_pipeline import cases,build_case,BOUNDARY
from build_keeper_pipeline import primary,corpus,digest
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('keeper-pipeline-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus()
    assert len(s['native_emulations'])==len(cases())==68
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result']
        assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0;base=bytes((i*31+7)&255 for i in range(406));expected_mid=intermediate(base)
    for gap in range(256):
        b=base[:224]+bytes([gap])*2+base[226:];assert intermediate(b)==expected_mid;checks+=1
    for seed in [0,165,255]:
        mid=intermediate(base,seed);assert mid[0x158:0x15A]==bytes([seed])*2;assert struct.unpack_from('<I',mid,0x154)[0]==(struct.unpack_from('<I',base,0xDC)[0]|seed*0x01010101);checks+=1
    for value in [*BOUNDARY,*[i*0x01010101 for i in range(256)]]:
        raw=patch_raw(base,0xC8,value);v=view(raw);assert v['unknown_c8_bits']==value and int(v['editor_c8_double']*4096)==value;assert raw[:200]==base[:200] and raw[204:]==base[204:];checks+=1
    for b in range(32):
        for enabled in [False,True]:
            raw=patch_flags(base,1<<b,enabled);old=struct.unpack_from('<I',base,0xDC)[0];assert struct.unpack_from('<I',raw,0xDC)[0]==(old|(1<<b) if enabled else old&~(1<<b));assert raw[:220]==base[:220] and raw[224:]==base[224:];checks+=1
    for o,n in WIDTHS.items():
        for value in [0,(1<<(8*n))-1]:
            b=patch_raw(base,o,value);assert b[:o]==base[:o] and b[o+n:]==base[o+n:] and int.from_bytes(b[o:o+n],'little')==value;checks+=1
    for args in [(base,0xC8,-1),(base,0xC8,1<<32),(base,0xE0,1<<16),(base,0x196,0),(base,0xEE,256)]:
        try:patch_raw(*args)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid patch accepted')
    for raw in [b'',bytes(405),bytes(407)]:
        try:intermediate(raw)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid stride accepted')
    for args in [(base,-1,True),(base,1<<32,True),(base,1,1)]:
        try:patch_flags(*args)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid flags accepted')
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b
    patches=0
    for p in [DATA/'KeeperSpells.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=p.read_bytes();parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==150);section=parts[si]
        for ri in sorted({0,len(section.records)//2,len(section.records)-1}):
            raw=section.records[ri];work=[]
            for o,n in WIDTHS.items():
                for value in [0,(1<<(8*n))-1]:work.append((patch_raw(raw,o,value),o,value.to_bytes(n,'little')))
            for mask in [1,2,16,0x80000000]:
                for enabled in [False,True]:
                    changed=patch_flags(raw,mask,enabled);work.append((changed,220,changed[220:224]))
            for changed,o,replacement in work:
                rs=list(section.records);rs[ri]=changed;sec=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);sec.replace_records(rs);packed=b''.join(sec.pack() if j==si else q.pack() for j,q in enumerate(parts));wanted=bytearray(original);absolute=section.offset+len(section.header)+ri*406+o;wanted[absolute:absolute+len(replacement)]=replacement
                assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=55 and package['keeper_pipeline_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=44,date='2026-10-26',native_emulations=68,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=checks,corpus_files=s['corpus']['file_count'],corpus_records=s['corpus']['records'],whole_file_roundtrips=s['corpus']['file_count'],exact_in_memory_container_patches=patches,primary_instructions=sum(len(e['instructions']) for e in s['evidence']),primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase44-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
