"""Verify Object C2 proofs, native fixtures, corpus and exact raw patching."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_placed_objects import *
from emulate_placed_objects import build_case,cases
from build_placed_objects import tables
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('placed-object-semantics.json');assert schema['game']['tables']==tables();boundaries=0
    for oid in range(256):
        for spell in [0,2,17,18,258,0xFFFFFFFF]:
            result=game_created_id(oid,spell)
            if oid==28:assert result is None
            elif oid!=7:assert result==oid
            else:assert result=={2:117,17:129,18:128}.get(spell,7)
            boundaries+=1
    numbers=[0,1,55,0x7FFFFFFF,0x80000000,0xFFFFFF00,0xFFFFFFFF]
    for initial in numbers:
        for amount in numbers:
            # Signed two's-complement interpretation of the wrapped sum.
            wrapped=(initial+amount)%2**32;signed=wrapped if wrapped<2**31 else wrapped-2**32
            assert gold_add(initial,amount)==max(0,signed);boundaries+=1
    assert game_created_id(7,258)==7 and editor_load_projection(7,8,258,0)['object_id']==117;boundaries+=1
    evidence={};instructions=0
    def walk(v):
        nonlocal instructions
        if isinstance(v,dict):
            if 'file' in v and 'instructions' in v:
                p=ROOT/'ghidra'/v['file'];lines=p.read_text().splitlines();assert all(s in lines for s in v['instructions']);instructions+=len(v['instructions']);evidence[v['file']]=digest(p.read_bytes())
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(schema)
    assert len(schema['native_emulations'])==len(cases())==99
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        assert all(int(saved['result']['registers'][r],16)==v for r,v in expected.items())
    unchanged=[]
    for base in ['object-callback','object-converter','things-writer','object-editor-load','object-editor-flags','object-money-setter','object-definition-getter','object-spell-getter','object-money-getter','object-spell-setter','object-money-model-setter','object-spell-crate-getter']:
        before=ROOT/'ghidra'/('phase30-'+base+'-before.txt')
        if not before.exists():before=ROOT/'ghidra'/('phase30-'+base+'-assembly.txt')
        after=ROOT/'ghidra'/('phase30-'+base+'-after.txt');assert before.read_text().rstrip()==after.read_text().rstrip();unchanged.append(before.name)
    stats={n:Counter() for n in ['object_id','spell_bits','money_bits','opaque_position_word','trigger_id']};files=[];remaps=Counter();selectors=Counter();skips=0;total=0
    for f in load('corpus-scan.json')['files']:
        if 'sections' not in f or not any(s['family']=='Things' for s in f['sections']):continue
        b=(DATA/'maps'/f['file']).read_bytes();assert digest(b)==f['sha256'];n=0
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for raw in s.records:
                if fmt.u32(raw,0)!=194:continue
                v=record_view(raw);n+=1;total+=1
                for key in stats:stats[key][v[key]]+=1
                if v['spell_bits']:selectors[(v['object_id'],v['spell_bits'])]+=1
                if v['object_id']==28:skips+=1
                if v['created_id'] is not None and v['created_id']!=v['object_id']:remaps[(v['object_id'],v['spell_bits'],v['created_id'])]+=1
        files.append(dict(file=f['file'],sha256=f['sha256'],object_records=n))
    patches=0;patched_files=[]
    for path in [DATA/'AutoSaveThings.kld',DATA/'maps/kop4Things.kld']:
        original=path.read_bytes();sections=fmt.parse(original);local=0
        for si,s in enumerate(sections):
            if s.signature!=190:continue
            running=0
            for ri,raw in enumerate(s.records):
                if fmt.u32(raw,0)==194:
                    for off,width in [(0,4),(4,4),(8,4),(12,4),(16,4),(20,2),(22,1),(23,1)]:
                        for value in [0,(1<<(8*width))-1]:
                            new=patch_raw(raw,off,value);records=list(s.records);records[ri]=new
                            changed=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);changed.replace_records(records)
                            packed=b''.join(changed.pack() if j==si else p.pack() for j,p in enumerate(sections));expected=bytearray(original)
                            absolute=s.offset+len(s.header)+running+8+off;expected[absolute:absolute+width]=value.to_bytes(width,'little')
                            assert packed==expected and b''.join(p.pack() for p in fmt.parse(packed))==packed;patches+=1;local+=1
                running+=len(raw)
        patched_files.append(dict(file=str(path.relative_to(DATA)),patches=local,sha256=digest(original)))
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    exes=load('executable-hash-verification.json')
    for file,sha in exes.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=41 and package['placed_object_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    names=[]
    for section in fmt.parse((DATA/'Objects.kwd').read_bytes()):
        if section.signature==240:
            for raw in section.records:
                if raw[0x35A] in {7,28,*stats['object_id'],*SPELL_OBJECT_IDS.values(),*TRAP_CRATE_OBJECT_IDS,*DOOR_CRATE_OBJECT_IDS}:
                    names.append(dict(id=raw[0x35A],name=raw[:32].split(b'\0',1)[0].decode('cp1252'),flags=int.from_bytes(raw[0x344:0x348],'little')))
    result=dict(phase=30,date='2026-10-26',boundary_cases=boundaries,native_emulations=len(cases()),native_case_kinds=dict(Counter(c['kind'] for c in cases())),exact_in_memory_container_patches=patches,patch_files=patched_files,
        corpus=dict(thing_files=len(files),object_records=total,file_details=files,raw_counts={n:dict(sorted(v.items())) for n,v in stats.items()},skipped_id28=skips,
            game_remaps=[dict(raw_id=o,spell_dword=s,created_id=i,count=n) for (o,s,i),n in sorted(remaps.items())],
            nonzero_selector_pairs=[dict(object_id=o,selector_dword=s,count=n) for (o,s),n in sorted(selectors.items())]),
        standalone_objects_name_guide=dict(source='Objects.kwd only, not a reference scope for all maps',objects=names),
        primary_evidence=dict(instructions=instructions,sha256=evidence,unchanged_disassembly=unchanged),unchanged_inputs=inputs,unchanged_executables=exes,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=schema['limitations'])
    (ROOT/'phase30-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','native_case_kinds','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'Things files;',total,'Objects;',skips,'skip28;',sum(remaps.values()),'hardcoded remaps')
    print('Raw spell:',stats['spell_bits'],'money:',stats['money_bits'],'opaque:',stats['opaque_position_word'])
    print('Evidence:',instructions,'instructions;',len(unchanged),'unchanged disassemblies;17 inputs/2 EXEs unchanged')
if __name__=='__main__':main()
