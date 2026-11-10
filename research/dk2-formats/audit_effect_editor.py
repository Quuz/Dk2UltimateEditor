"""Evidence, emulations, corpus distributions and lossless Effects patches."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_effect_editor import *
from emulate_effect_editor import build_case
from effect_writer_stack_audit import inspect
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    schema=load('effect-editor-semantics.json');boundaries=0
    assert schema['opaque_f4']['stack_audit']==inspect()
    for raw in list(range(256))+[0xFFFF,0x10000,0x80000000,0xFFFFFFFF]:
        expected=sum((raw&(1<<bit)) for bit in FLAG_MODEL_BITS)
        assert flag_encode_decode(raw)==expected
        assert native_loaded_flags(raw)==(expected if expected&0xC0 else expected|0x80);boundaries+=1
        assert native_loaded_flags(native_loaded_flags(raw))==native_loaded_flags(raw);boundaries+=1
    assert native_loaded_flags(0)==0x80 and native_loaded_flags(8)==0x80
    assert native_loaded_flags(0xC0)==0xC0;boundaries+=2
    for value in [0,1,0x7FFF,0x8000,0xFFFF]:
        previous=[100+i for i in range(12)];incoming=[value]*12
        assert tail_update(previous,incoming)==([value]*12 if value else previous);boundaries+=1
    for invalid in [-1,65536,True]:
        try:patch_raw(bytes(246),0xF4,invalid)
        except ValueError:pass
        else:raise AssertionError('Invalid WORD accepted')
        boundaries+=1
    hashes={};excerpts=0;instructions=0
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                p=ROOT/'ghidra'/v['file'];lines=p.read_text().splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(s in lines for s in v['instructions']);instructions+=len(v['instructions'])
                hashes[v['file']]=digest(p.read_bytes())
            for x in v.values():walk(x)
        elif isinstance(v,list):
            for x in v:walk(x)
    walk(schema)
    converted=[];writer_tails=[]
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text());args,expected=build_case(saved['case'])
        assert args==saved['input'] and expected==saved['expected_registers'] and saved['result']['success'] and saved['result']['hit_return']
        for reg,value in expected.items():assert int(saved['result']['registers'][reg],16)==value
        if saved['case']['kind']=='converter':converted.append(saved['result']['registers'])
        if saved['case']['kind']=='writer':writer_tails.append(dict(seed=saved['case']['poison'],written=int(saved['result']['registers']['EAX'],16),fnv=expected['ESI']))
    assert len(schema['native_emulations'])==31 and all(r==converted[0] for r in converted)
    assert all(r['seed']==r['written'] for r in writer_tails)
    unchanged=[]
    for name in ['effect-writer','effects-flags','effects-flags-decode','effects-model','effect-generation-default','effect-converter','effect-loader','effect-resource-getter','effect-string-writer']:
        before=ROOT/'ghidra'/('phase27-'+name+'-assembly.txt');after=ROOT/'ghidra'/('phase27-'+name+'-after.txt')
        assert before.read_text().rstrip()==after.read_text().rstrip();unchanged.append(before.name)
    total=0;flags=Counter();f4=Counter();changes=0;files=[];tail_nonzero=Counter();tail_zero=Counter()
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family']=='Effects' for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];n=0;localf4=Counter()
        for section in fmt.parse(b):
            if section.signature!=270:continue
            for r in section.records:
                v=record_view(r);n+=1;total+=1;flags[v['raw_flags']]+=1;f4[v['opaque_f4']]+=1;localf4[v['opaque_f4']]+=1
                changes+=v['native_loaded_saved_flags']!=v['raw_flags']
                for i,w in enumerate(v['tail_words']):(tail_nonzero if w else tail_zero)[i]+=1
        if n:files.append(dict(file=file['file'],records=n,sha256=file['sha256'],f4_counts=dict(sorted(localf4.items()))))
    standalone=[]
    for path in [DATA/'Effects.kwd',DATA/'AutoSaveGlobals.kld']:
        count=Counter()
        for section in fmt.parse(path.read_bytes()):
            if section.signature==270:
                for r in section.records:count[record_view(r)['opaque_f4']]+=1
        standalone.append(dict(file=path.name,f4_counts=dict(sorted(count.items()))))
    patches=0
    for path in [DATA/'Effects.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,section in enumerate(sections):
            if section.signature!=270:continue
            for ri,record in enumerate(section.records):
                for off,values in [(0xB4,[0,8,0x40,0xC0,0xFFFFFFFF]),(0xF4,[0,3880,0xA55A,0xFFFF]),(0xEE,[0,1,0x8000,0xFFFF])]:
                    for value in values:
                        width=4 if off==0xB4 else 2;r=patch_raw(record,off,value);assert r[:off]==record[:off] and r[off+width:]==record[off+width:]
                        records=list(section.records);records[ri]=r;changed=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix);changed.replace_records(records)
                        packed=b''.join(changed.pack() if i==si else s.pack() for i,s in enumerate(sections));expected=bytearray(original)
                        absolute=section.offset+len(section.header)+ri*len(record)+off;expected[absolute:absolute+width]=value.to_bytes(width,'little')
                        assert packed==expected and b''.join(s.pack() for s in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=38 and package['effect_editor_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=27,date='2026-10-26',boundary_cases=boundaries,native_emulations=31,exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        corpus=dict(effect_files=len(files),effect_records=total,file_details=files,raw_flags_counts=dict(sorted(flags.items())),raw_f4_counts=dict(sorted(f4.items())),native_editor_flag_projection_changes=changes,
            tail_word_nonzero_counts=dict(sorted(tail_nonzero.items())),tail_word_zero_counts=dict(sorted(tail_zero.items())),standalone=standalone),
        writer_captures=writer_tails,converter_f4_fixtures_same_readouts=True,
        primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=hashes,unchanged_disassembly=unchanged,stack_cfg_instruction_states=schema['opaque_f4']['stack_audit']['visited_instruction_states']),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=38,limitations=schema['limitations'])
    (ROOT/'phase27-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'files;',total,'Effects; F4:',dict(f4),'; flag projection changes',changes)
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(unchanged),'unchanged disassemblies')
if __name__=='__main__':main()
