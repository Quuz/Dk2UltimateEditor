"""Primary evidence, native emulation, corpus and raw preservation for Creatures EE4."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from native_creature_creep import *
from emulate_creature_creep import build_case

ROOT=Path(__file__).resolve().parent
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    schema=load('creature-creep-semantics.json');cases=0
    for raw in [0,1,50,100,255,256,300,32000,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        assert native_editor_projection(raw)==raw%256;cases+=1
        for level in [0,1,2,10,255]:
            for multiplier in [0,100,105,120,0x7FFF,0x8000,0xFFFF]:
                m=struct.unpack('<h',struct.pack('<H',multiplier))[0]
                product=struct.unpack('<i',struct.pack('<I',(m*s32(raw))&0xFFFFFFFF))[0]
                p=s32(raw) if level==1 else (product//100 if product>=0 else -((-product)//100))
                got=weighted_distance(409600,raw,level,multiplier,0x1000)
                assert got['effective_percentage']==p and got['distance']==((409600*p)&0xFFFFFFFF)//100;cases+=1
    for metric in [0,1,4096,409600,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
        assert weighted_distance(metric,50,2,105,0)['distance']==metric;cases+=1
    assert weighted_distance(409600,50,2,105,0x1000)['distance']==212992;cases+=1
    assert weighted_distance(409600,0,1,0,0x1000)['distance']==0;cases+=1
    assert weighted_distance(409600,50,1,100,0x1000)['distance']==204800;cases+=1
    assert within_numeric_sight(409600,10) is False and within_numeric_sight(409599,10) is True;cases+=1
    assert squared_distance_q12(0,0,40960,0)==409600;cases+=1
    for flags in [0,0x1000,0xFFFFFFFF,0xA5A5A5A5]:
        for value in [0,1,2,3,0xFFFFFFFF]:
            got=creep_flag_after_action(flags,value)
            assert got&0xFFFFEFFF==flags&0xFFFFEFFF and (got>>12)&1==value&1;cases+=1
            assert creep_flag_after_action(flags,value,False)==flags;cases+=1
    for value in [-300,-1,0,255,256,300,0x7FFFFFFFFFFFFFFF]:assert editor_integer_update(value)==value%256;cases+=1
    for length in [5449,5537]:
        r=bytearray(length);struct.pack_into('<I',r,0xEE4,300)
        assert record_view(r)==dict(raw_percentage=300,game_runtime_bits=300,native_editor_saved_projection=44);cases+=1
    for action in [lambda:weighted_distance(-1,0,1,100,0),lambda:weighted_distance(0,0,256,100,0),lambda:weighted_distance(0,0,1,65536,0),
        lambda:native_editor_projection(True),lambda:record_view(bytes(5448)),lambda:record_view(bytes(5538)),lambda:record_view('x'*5449),
        lambda:squared_distance_q12(0,0,1<<31,0),lambda:creep_flag_after_action(0,0,1),lambda:editor_integer_update(1<<63)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid native input accepted')
        cases+=1
    excerpts=0;instructions=0;hashes={}
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
    game=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();binding=schema['level_multiplier']['normal_pointer_binding']
    assert pe_read(game,binding['address'],10).hex()==binding['bytes']
    # Verify the stat26 growth-lane address algebra, not just its source label.
    for level in range(256):assert 0x6F261D+2*(26*9+level)+2==0x6F2550+0x2A3+2*level
    label=schema['label_resource'];editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    raw=editor[label['text_file_offset']:label['text_file_offset']+label['utf16_bytes']]
    assert digest(raw)==label['text_sha256'] and raw.decode('utf-16le')==label['text']
    for row in schema['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];saved=json.loads(p.read_text())
        args,expected=build_case(saved['case']);assert args==saved['input'] and expected==saved['expected_registers']
        assert saved['result']['success'] and saved['result']['hit_return'] and int(saved['result']['registers']['EAX'],16)==expected['EAX']
    assert len(schema['native_emulations'])==116
    compared=[]
    for name in ['a','b','c','d','e','f','g']:
        before=f'phase24-creep-move-{name}-assembly.txt';after=f'phase24-creep-move-{name}-after.txt'
        assert (ROOT/'ghidra'/before).read_text().rstrip()==(ROOT/'ghidra'/after).read_text().rstrip();compared.append(before)
    for before,after in [('phase24-creep-target-assembly','phase24-creep-target-after'),('phase24-creature-converter-assembly','phase24-creature-converter-after'),
        ('game-creature-type-assembly','phase24-creature-loader-after'),('phase24-creep-action-assembly','phase24-creep-action-after'),
        ('phase24-stat-setter-assembly','phase24-stat-setter-after'),('phase24-editor-creature-model-assembly','phase24-editor-creature-model-after'),
        ('phase17-creature-writer-assembly','phase24-editor-creature-writer-after')]:
        assert (ROOT/'ghidra'/(before+'.txt')).read_text().rstrip()==(ROOT/'ghidra'/(after+'.txt')).read_text().rstrip();compared.append(before)
    total=0;values=Counter();loss=0;files=[];variable_files=[];growth_values=Counter()
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family'] in ['Creatures','Variables'] for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];n=0;ngrowth=0;has_variables=False
        for section in fmt.parse(b):
            if section.signature==170:
                for r in section.records:
                    view=record_view(r);values[view['raw_percentage']]+=1;loss+=view['raw_percentage']!=view['native_editor_saved_projection'];n+=1;total+=1
            elif section.signature==230:
                has_variables=True
                for r in section.records:
                    if struct.unpack_from('<II',r)==(65,26):
                        _,_,value,level=struct.unpack('<4I',r);growth_values[(level,value)]+=1;ngrowth+=1
        if n:files.append(dict(file=file['file'],records=n,sha256=file['sha256']))
        if has_variables:variable_files.append(dict(file=file['file'],stat26_records=ngrowth,sha256=file['sha256']))
    default_growth=[]
    for section in fmt.parse((DATA/'GlobalVariables.kwd').read_bytes()):
        for r in section.records:
            if len(r)==16 and struct.unpack_from('<II',r)==(65,26):
                _,_,value,level=struct.unpack('<4I',r);default_growth.append(dict(level=level,value=value,signed16=s16(value&0xFFFF)))
    patches=0
    for path in [DATA/'Creatures.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,section in enumerate(sections):
            if section.signature!=170:continue
            for ri,record in enumerate(section.records):
                for value in [0,50,255,256,300,32000,0x80000000,0xFFFFFFFF]:
                    replacement=pack32(value);r=bytearray(record);r[0xEE4:0xEE8]=replacement;records=list(section.records);records[ri]=bytes(r)
                    changed=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix);changed.replace_records(records)
                    packed=b''.join(changed.pack() if i==si else s.pack() for i,s in enumerate(sections));expected=bytearray(original)
                    absolute=section.offset+len(section.header)+ri*len(record)+0xEE4;expected[absolute:absolute+4]=replacement
                    assert packed==expected and b''.join(s.pack() for s in fmt.parse(packed))==packed;patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=35 and package['creature_creep_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=24,date='2026-10-25',boundary_cases=cases,native_emulations=116,exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        primary_evidence=dict(excerpts=excerpts,instructions=instructions,sha256=hashes,unchanged_disassembly=compared),
        corpus=dict(creature_files=len(files),creature_records=total,raw_percentage_counts=dict(sorted(values.items())),native_editor_projection_changes=loss,file_details=files,
            variable_files=len(variable_files),variable_file_details=variable_files,stat26_counts=[dict(level=k[0],value=k[1],records=v) for k,v in sorted(growth_values.items())],standalone_stat26=default_growth),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=35,limitations=schema['limitations'])
    (ROOT/'phase24-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_emulations','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'creature files;',total,'records; raw values',dict(values),'; projected changes',loss)
    print('Variables:',len(variable_files),'files;',sum(growth_values.values()),'Stat26 records')
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(compared),'unchanged disassemblies')

def pack32(value):return struct.pack('<I',value)
if __name__=='__main__':main()
