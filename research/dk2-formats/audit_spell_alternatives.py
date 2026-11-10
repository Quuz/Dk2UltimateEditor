"""Phase42 primary evidence, saved native cases and raw preservation checks."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_spell_alternatives import *
from emulate_spell_alternatives import cases,build_case,raw_record
from build_spell_alternatives import primary,resources,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def main():
    s=load('spell-alternative-semantics.json');assert s['primary_program_bytes']==primary() and s['native_ui']==resources() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==63
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result'];assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    boundaries=0;ids=bytes([0,36,255,36]);baseline=full_intermediate(raw_record(tail=0))
    for value in range(256):
        assert lookup(ids,value)==next((i for i,x in enumerate(ids) if x==value),-1);boundaries+=1
        assert full_intermediate(raw_record(tail=value))==baseline;boundaries+=1
        for other in [0,4,255]:
            raw=raw_record(value,other);assert references(raw)==dict(alternative_shot_id=value,alternative_room_id=other,alternative_rule=2);boundaries+=1
            for p28,p2c in [(0,0),(1,0),(0,1),(1,1)]:
                v=model_reference_projection(value,other,p28,p2c);assert v['alternative_shot_id']==(value if p2c else 165) and v['alternative_room_id']==(other if p28 else 165);boundaries+=1
    for index in [-1,4,255]:
        try:checked_selection(ids,index)
        except ValueError:boundaries+=1
        else:raise AssertionError('Unchecked selection accepted')
    for index in range(len(ids)):assert checked_selection(ids,index)==ids[index];boundaries+=1
    for raw in [bytes(265),bytes(267)]:
        try:full_intermediate(raw)
        except ValueError:boundaries+=1
        else:raise AssertionError('Wrong raw width accepted')
    total=0;roundtrips=0;effect_checks=0
    for row in s['corpus']['files']:
        p=Path(row['file']);b=p.read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1;sec=next(q for q in parts if q.signature==160)
        for raw in sec.records:
            total+=1;mid=full_intermediate(raw);assert len(mid)==346 and not any(mid[319:]);original=runtime_projection(mid)
            for off in [232,233]:
                changed=patch_raw(raw,off,raw[off]^255);other=full_intermediate(changed);projection=runtime_projection(other);assert projection[:27]+projection[31:]==original[:27]+original[31:];assert int.from_bytes(projection[27:31],'little')==sum(other);effect_checks+=1
    patches=0
    for p in [DATA/'CreatureSpells.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        b=p.read_bytes();parts=fmt.parse(b);si=next(i for i,q in enumerate(parts) if q.signature==160);sec=parts[si]
        for ri,raw in enumerate(sec.records):
            for off in [232,233]:
                for value in [0,36,255]:
                    changed=patch_raw(raw,off,value);rows=list(sec.records);rows[ri]=changed;replacement=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);replacement.replace_records(rows);packed=b''.join(replacement.pack() if j==si else q.pack() for j,q in enumerate(parts));wanted=bytearray(b);absolute=sec.offset+len(sec.header)+ri*266+off;wanted[absolute]=value;assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed and changed[239:]==raw[239:];patches+=1
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])==53 and package['spell_alternative_semantics']==s
    assert package['spell_writer_semantics']['converter']['intermediate_bytes']==346 and package['spell_writer_semantics']['converter']['phase41_compared_prefix_bytes']==320
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    instructions=sum(len(e['instructions']) for e in s['evidence']);result=dict(phase=42,date='2026-10-26',native_emulations=63,native_case_kinds=dict(Counter(c['kind'] for c in cases())),boundary_cases=boundaries,corpus_files=roundtrips,corpus_records=total,nonzero_reference_records=s['corpus']['nonzero_reference_records'],missing_paired_targets=len(s['corpus']['missing_targets_in_available_paired_catalogues']),whole_file_roundtrips=roundtrips,complete_runtime_projection_checks=effect_checks,exact_in_memory_container_patches=patches,primary_instructions=instructions,primary_ranges_matching_pe=len(s['primary_program_bytes']),native_checkbox_bindings=8,native_combo_bindings=4,original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase42-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
