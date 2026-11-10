"""Replay saved native results and verify raw-preserving container edits."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_spell_writer import *
from emulate_spell_writer import cases,build_case
from build_spell_writer import primary,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('spell-writer-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus()
    assert len(s['native_emulations'])==len(cases())==61
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result']
        assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0;base=bytes(range(256))+bytes(range(10));original_mid=intermediate(base)
    for tail in range(256):
        raw=base[:239]+bytes([tail])*27;assert intermediate(raw)==original_mid;checks+=1
    for bit in range(32):
        mask=1<<bit;assert flags_model_projection(mask)==(mask if mask&0x1EF else 0);checks+=1
        for enabled in [False,True]:
            raw=patch_flags(base,mask,enabled);a=bytearray(base);old=int.from_bytes(a[212:216],'little');struct.pack_into('<I',a,212,old|mask if enabled else old&~mask);assert raw==a and raw[239:]==base[239:];checks+=1
    for off,n in FIELD_WIDTHS.items():
        for value in [0,(1<<(8*n))-1]:
            raw=patch_raw(base,off,value);wanted=bytearray(base);wanted[off:off+n]=value.to_bytes(n,'little');assert raw==wanted and raw[239:]==base[239:];checks+=1
    for raw in [b'',bytes(265),bytes(267)]:
        try:intermediate(raw)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid width accepted')
    for args in [(base,239,0),(base,212,-1),(base,230,256),(base,212,1<<32)]:
        try:patch_raw(*args)
        except ValueError:checks+=1
        else:raise AssertionError('Invalid patch accepted')
    roundtrips=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b;roundtrips+=1
    patches=0
    for p in [DATA/'CreatureSpells.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=p.read_bytes();parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==160);section=parts[si]
        for ri in sorted({0,len(section.records)//2,len(section.records)-1}):
            raw=section.records[ri];work=[]
            for off,n in FIELD_WIDTHS.items():
                for value in [0,(1<<(8*n))-1]:work.append((patch_raw(raw,off,value),off,value.to_bytes(n,'little')))
            for mask in [1,16,0x80000000]:
                for enabled in [False,True]:
                    changed=patch_flags(raw,mask,enabled);work.append((changed,212,changed[212:216]))
            for changed,off,replacement in work:
                rows=list(section.records);rows[ri]=changed;sec=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix,section.stride);sec.replace_records(rows);packed=b''.join(sec.pack() if j==si else q.pack() for j,q in enumerate(parts));wanted=bytearray(original);absolute=section.offset+len(section.header)+ri*266+off;wanted[absolute:absolute+len(replacement)]=replacement
                assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed;assert changed[239:]==raw[239:];patches+=1
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=52 and package['spell_writer_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    instructions=sum(len(e['instructions']) for e in s['evidence'])
    result=dict(phase=41,date='2026-10-26',native_emulations=61,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=checks,corpus_files=roundtrips,corpus_records=s['corpus']['records'],nonzero_tail_records=s['corpus']['nonzero_tail_records'],distinct_tail_buffers=s['corpus']['distinct_tail_buffers'],whole_file_roundtrips=roundtrips,exact_in_memory_container_patches=patches,primary_instructions=instructions,primary_ranges_matching_pe=len(s['primary_program_bytes']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase41-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
