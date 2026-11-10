"""Verify35 complete editor chains, real values and lossless in-memory patches."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_creature_editor_scalars import SCHEMA,FIELDS,views,commit_converted_integer,patch_raw
from native_creature_priority import priority_view
from extract_editor_strings import extract,resource_leaves

ROOT=Path(__file__).resolve().parent
def digest(b):return hashlib.sha256(b).hexdigest()
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))

def main():
    cases=0
    for size in SCHEMA['record_variants']:
        seed=bytes((i*17+91)&255 for i in range(size))
        for f in SCHEMA['fields']:
            offset=f['raw_offset'];width=f['raw_bytes'];bits=width*8
            for raw in sorted({0,1,40,100,(1<<(bits-1))-1,1<<(bits-1),(1<<bits)-1}):
                changed=patch_raw(seed,offset,raw);view=next(v for v in views(changed) if v['raw_offset']==offset)
                expected=raw-(1<<bits) if f['editor_display_kind'].startswith('s') and raw&(1<<(bits-1)) else raw
                assert len(changed)==len(seed) and changed[:offset]==seed[:offset] and changed[offset+width:]==seed[offset+width:]
                assert view['raw']==raw and view['native_editor_value']==view['native_editor_double']==expected
                assert view['gui_validation_range'] is None
                p=commit_converted_integer(offset,expected)
                assert p['raw']==raw and p['native_editor_value_after_reload']==expected;cases+=1
            for value in [-1,-(1<<bits),-(1<<bits)-1,1<<bits,(1<<bits)+1,-(1<<63),(1<<63)-1]:
                p=commit_converted_integer(offset,value);assert p['raw']==value% (1<<bits);cases+=1
    seed=bytes(5537)
    for offset,raw,value in [(0xF20,65535,65535),(0xF53,188,188),(0xF53,200,200),(0xF53,255,255),
        (0xF2C,65535,-1),(0xF30,60536,-5000),(0xF32,64786,-750),(0xEC0,0xFFFFFFFF,0xFFFFFFFF)]:
        v=next(v for v in views(patch_raw(seed,offset,raw)) if v['raw_offset']==offset)
        assert v['native_editor_value']==value;cases+=1
    invalid=[lambda:views(bytes(5448)),lambda:views(bytes(5538)),lambda:views('x'*5537),
        lambda:patch_raw(seed,0xF54,1),lambda:patch_raw(seed,True,1),lambda:patch_raw(seed,0xF53,256),
        lambda:patch_raw(seed,0xF53,-1),lambda:patch_raw(seed,0xF53,1.5),lambda:patch_raw(seed,0xF53,True),
        lambda:commit_converted_integer(0xF53,1<<63),lambda:commit_converted_integer(0xF53,True),
        lambda:commit_converted_integer(0xF53,1.0),lambda:commit_converted_integer(0xF54,1)]
    for action in invalid:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    evidence_hashes={};lines_verified=0;instructions=0
    def walk(value):
        nonlocal lines_verified,instructions
        if isinstance(value,dict):
            if 'file' in value and ('excerpt' in value or 'instructions' in value):
                b=(ROOT/'ghidra'/value['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in value:assert lines[value['line']-1].strip()==value['excerpt'];lines_verified+=1
                else:assert all(i in lines for i in value['instructions']);instructions+=len(value['instructions'])
                evidence_hashes[value['file']]=digest(b)
            for child in value.values():walk(child)
        elif isinstance(value,list):
            for child in value:walk(child)
    walk(SCHEMA)
    editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    strings={s['id']:s for s in extract(editor)};leaves={tuple(x[0]):x for x in resource_leaves(editor)}
    for f in SCHEMA['fields']:
        label=f['label_resource'];assert label==strings[f['property_id']] and label['text']==f['native_label']
        raw=editor[label['text_file_offset']:label['text_file_offset']+label['utf16_bytes']]
        assert digest(raw)==label['text_sha256'] and raw.decode('utf-16-le')==f['native_label']
        assert digest(leaves[tuple(label['resource_path'])][3])==label['block_sha256']
        for proof in f['evidence']:
            if 'instructions' in proof:
                instruction=proof['instructions'][-1];stack=int(instruction.split('[ESP + ')[1].split(']')[0],16)
                assert stack-0x94==f['raw_offset']
    priority=load('creature-priority-semantics.json');f=FIELDS[0xF55]
    for key,other_key in [('raw_offset','raw_offset'),('raw_bytes','raw_bytes'),('editor_intermediate_offset','editor_intermediate_offset'),
        ('editor_model_offset','editor_model_offset'),('editor_panel_offset','editor_panel_offset'),('property_id','property_id')]:
        assert f[key]==priority['field'][other_key]
    assert f['native_label']==priority['native_label']
    patches=0;scopes=[]
    for name,path in [('Creatures',DATA/'Creatures.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();sections=fmt.parse(b);records_count=0
        for si,s in enumerate(sections):
            if s.signature!=170:continue
            for ri,r in enumerate(s.records):
                records_count+=1;raw_views=views(r);assert next(v for v in raw_views if v['raw_offset']==0xF55)['raw']==priority_view(r)['raw']
                for f in SCHEMA['fields']:
                    offset=f['raw_offset'];width=f['raw_bytes'];raw=int.from_bytes(r[offset:offset+width],'little');new=raw^((1<<(width*8))-1)
                    changed=patch_raw(r,offset,new);records=list(s.records);records[ri]=changed
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else section.pack() for i,section in enumerate(sections))
                    absolute=s.offset+len(s.header)+ri*len(r)+offset
                    assert packed[:absolute]==b[:absolute] and packed[absolute+width:]==b[absolute+width:]
                    assert fmt.parse(packed)[si].records[ri]==changed;patches+=1
        assert digest(path.read_bytes())==digest(b);scopes.append(dict(name=name,path=str(path),sha256=digest(b),records=records_count))
    counts={f['raw_offset']:Counter() for f in SCHEMA['fields']};files=[];total=0;negative=Counter();negative_examples=[]
    for file in load('corpus-scan.json')['files']:
        if not any(s['family']=='Creatures' for s in file.get('sections',[])):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];local_count=0
        for s in fmt.parse(b):
            if s.signature!=170:continue
            for r in s.records:
                total+=1;local_count+=1
                for v in views(r):
                    counts[v['raw_offset']][v['native_editor_value']]+=1
                    if v['native_editor_value']<0:
                        negative[v['raw_offset']]+=1
                        if len(negative_examples)<12:negative_examples.append(dict(file=file['file'],creature_id=r[0xF4B],name=fmt.string(r[:32]),**v))
        files.append(dict(file=file['file'],sha256=digest(b),records=local_count))
    fields=[]
    for f in SCHEMA['fields']:
        c=counts[f['raw_offset']];assert sum(c.values())==total
        fields.append(dict(raw_offset=f['raw_offset'],raw_offset_hex=hex(f['raw_offset']),native_label=f['native_label'],
            native_kind=f['editor_display_kind'],native_values={str(k):v for k,v in sorted(c.items())},
            minimum=min(c),maximum=max(c),negative_records=negative[f['raw_offset']],
            above100_records=sum(count for value,count in c.items() if value>100)))
    fly=next(f for f in fields if f['raw_offset']==0xF53);assert fly['above100_records']==total
    assert fly['native_values']=={'188':3482,'200':160}
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    exe_hashes={}
    for file,expected in SCHEMA['executable_sha256'].items():
        actual=digest((ROOT.parent.parent/file).read_bytes());assert actual==expected;exe_hashes[file]=actual
    package_bytes=(ROOT/'editor-format-package.json').read_bytes();package=json.loads(package_bytes)
    assert len(package['components'])>=29 and package['creature_editor_scalars']==SCHEMA
    for key,item in package['components'].items():
        b=(ROOT/item['file']).read_bytes();assert digest(b)==item['sha256'] and json.loads(b)==package[key]
    result=dict(phase=18,properties=len(FIELDS),boundary_cases=cases,exact_in_memory_container_patches=patches,
        schema_sha256=digest((ROOT/'creature-editor-scalars.json').read_bytes()),
        format_package=dict(components=len(package['components']),sha256=digest(package_bytes)),
        primary_evidence=dict(excerpts_verified=lines_verified,instructions_verified=instructions,sha256=evidence_hashes,resource_strings_verified=len(FIELDS)),
        native_emulations=0,scopes=scopes,corpus=dict(files=len(files),records=total,field_distributions=fields,
            negative_examples=negative_examples,files_verified=files),
        unchanged_inputs=inputs,unchanged_executables=exe_hashes,
        limitations=['Saved Ghidra evidence only; no new Ghidra requests or game input writes.',
            'Native labels alone do not prove game usage, units or a validated GUI range.'])
    (ROOT/'phase18-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','properties','boundary_cases','exact_in_memory_container_patches','native_emulations']}))
    print('Corpus:',len(files),'files;',total,'creatures; Fly Height Percentage:',fly['native_values'])
    print('Evidence:',lines_verified,'excerpts;',instructions,'instructions;',len(FIELDS),'resource strings')
    print('Unchanged:',len(inputs),'inputs and',len(exe_hashes),'executables')

if __name__=='__main__':main()
