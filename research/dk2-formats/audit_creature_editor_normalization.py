"""Four strict native thresholds, real data and exact first-person writer offsets."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from extract_editor_dialogs import extract
from extract_editor_strings import resource_leaves
from native_creature_editor_normalization import SCHEMA,FIELDS,project_raw,views,patch_raw,normalized_copy_scenario

ROOT=Path(__file__).resolve().parent
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    cases=0
    for size in SCHEMA['record_variants']:
        seed=bytes((i*23+37)&255 for i in range(size))
        for f in SCHEMA['fields']:
            offset=f['raw_offset'];width=f['raw_bytes'];maximum=f['normalization']['maximum_preserved_raw'];replacement=f['normalization']['replacement_raw']
            for raw in sorted({0,1,maximum-1,maximum,maximum+1,(1<<(width*8))-1}):
                p=project_raw(offset,raw);expected=replacement if raw>maximum else raw
                assert p['raw_after_model_callback']==p['native_writer_raw_if_model_unedited']==expected
                assert p['callback_changed_value']==(raw>maximum)
                changed=patch_raw(seed,offset,raw);before=bytes(changed);v=next(v for v in views(changed) if v['raw_offset']==offset)
                assert v==p and bytes(changed)==before and len(changed)==size
                assert changed[:offset]==seed[:offset] and changed[offset+width:]==seed[offset+width:]
                normalized=normalized_copy_scenario(changed);second=normalized_copy_scenario(normalized)
                assert normalized==second and int.from_bytes(normalized[offset:offset+width],'little')==expected;cases+=1
    for raw,expected,valid in [(0,0,False),(1,1,True),(12,12,True),(13,4,True),(255,4,True)]:
        p=project_raw(0xD3D,raw)
        assert p['raw_after_model_callback']==expected and p['within_declared_ddx_range']==valid;cases+=1
    for offset in [0xDBC,0xDC0]:
        for raw in [0,1,4095,4096,4097,0x80000000,0xFFFFFFFF]:
            p=project_raw(offset,raw);assert p['within_declared_ddx_range'] and p['raw_after_model_callback']==(0 if raw>4096 else raw);cases+=1
    for raw in [40959999,40960000,40960001,0xFFFFFFFF]:
        p=project_raw(0xCF9,raw);assert p['within_declared_ddx_range'] is None and p['raw_after_model_callback']==(raw if raw<=40960000 else 0);cases+=1
    for f in SCHEMA['fields']:
        for raw in [-1,1<<(f['raw_bytes']*8),True,1.0]:
            try:project_raw(f['raw_offset'],raw)
            except ValueError:pass
            else:raise AssertionError('Invalid raw input accepted')
            cases+=1
    for action in [lambda:views(bytes(5448)),lambda:views(bytes(5538)),lambda:project_raw(True,1),
        lambda:project_raw(0xD45,1),lambda:patch_raw(bytes(5537),0xDBC,-1)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    excerpts=0;instructions=0;evidence_hashes={}
    def walk(v):
        nonlocal excerpts,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                b=(ROOT/'ghidra'/v['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];excerpts+=1
                else:assert all(line in lines for line in v['instructions']);instructions+=len(v['instructions'])
                evidence_hashes[v['file']]=digest(b)
            for child in v.values():walk(child)
        elif isinstance(v,list):
            for child in v:walk(child)
    walk(SCHEMA)
    for f in SCHEMA['fields']:
        w=f['writer_stack'];assert w['destination_offset']-w['buffer_offset_before_pushes']-w['extra_pushed_bytes']==f['raw_offset']
        evidence=next(e for e in f['evidence'] if e['file']=='phase17-creature-writer-assembly.txt')
        store=evidence['instructions'][-1]
        assert hex(w['destination_offset']) in store and ('byte ptr' if f['raw_bytes']==1 else 'dword ptr') in store
        if f['raw_bytes']==1:
            assert sum(': PUSH ' in line for line in evidence['instructions'])==2
            assert not any(': POP ' in line or ': CALL ' in line for line in evidence['instructions'])
    editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    for c in SCHEMA['editor_constants']:
        b=pe_read(editor,c['address'],8);assert digest(b)==c['sha256'] and b.hex()==c['bytes_hex'] and struct.unpack('<d',b)[0]==c['value']
    dialog=next(d for d in extract(editor) if d['resource_path']==SCHEMA['dialog']['resource'])
    assert dialog['title']==SCHEMA['dialog']['title']
    leaf=next(x for x in resource_leaves(editor) if x[0]==SCHEMA['dialog']['resource'])
    assert digest(leaf[3])==SCHEMA['dialog']['resource_sha256']
    for f in SCHEMA['fields']:
        if f['native_dialog']:
            binding=f['native_dialog'];assert binding['control'] in dialog['controls'] and binding['caption_control'] in dialog['controls']
    for previous in ['creature-editor-scalars.json','creature-editor-scaled.json']:
        assert not set(FIELDS)&{f['raw_offset'] for f in load(previous)['fields']}
    patches=0;scopes=[]
    for name,path in [('Creatures',DATA/'Creatures.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();sections=fmt.parse(b);records_count=0
        for si,s in enumerate(sections):
            if s.signature!=170:continue
            for ri,r in enumerate(s.records):
                records_count+=1;assert normalized_copy_scenario(r)==r
                for f in SCHEMA['fields']:
                    offset=f['raw_offset'];width=f['raw_bytes'];raw=int.from_bytes(r[offset:offset+width],'little');new=raw^((1<<(width*8))-1)
                    changed=patch_raw(r,offset,new);records=list(s.records);records[ri]=changed
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else section.pack() for i,section in enumerate(sections))
                    absolute=s.offset+len(s.header)+ri*len(r)+offset
                    assert packed[:absolute]==b[:absolute] and packed[absolute+width:]==b[absolute+width:]
                    assert fmt.parse(packed)[si].records[ri]==changed;patches+=1
        assert digest(path.read_bytes())==digest(b);scopes.append(dict(name=name,path=str(path),records=records_count,sha256=digest(b)))
    counters={o:Counter() for o in FIELDS};total=0;files=[];changed=Counter();ddx_fail=Counter()
    for file in load('corpus-scan.json')['files']:
        if not any(s['family']=='Creatures' for s in file.get('sections',[])):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];local_count=0
        for s in fmt.parse(b):
            if s.signature!=170:continue
            for r in s.records:
                total+=1;local_count+=1;assert normalized_copy_scenario(r)==r
                for v in views(r):
                    counters[v['raw_offset']][v['raw_before']]+=1
                    if v['callback_changed_value']:changed[v['raw_offset']]+=1
                    if v['within_declared_ddx_range'] is False:ddx_fail[v['raw_offset']]+=1
        files.append(dict(file=file['file'],sha256=digest(b),records=local_count))
    assert total==3642 and not changed and not ddx_fail
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    hashes={}
    for file,expected in SCHEMA['executable_sha256'].items():
        actual=digest((ROOT.parent.parent/file).read_bytes());assert actual==expected;hashes[file]=actual
    package_bytes=(ROOT/'editor-format-package.json').read_bytes();package=json.loads(package_bytes)
    assert len(package['components'])>=31 and package['creature_editor_normalization']==SCHEMA
    for key,item in package['components'].items():
        b=(ROOT/item['file']).read_bytes();assert digest(b)==item['sha256'] and json.loads(b)==package[key]
    result=dict(phase=20,fields=len(FIELDS),first_person_ddx_bindings=3,boundary_cases=cases,
        exact_in_memory_container_patches=patches,native_emulations=0,modified_gameplay_runs=0,
        schema_sha256=digest((ROOT/'creature-editor-normalization.json').read_bytes()),
        format_package=dict(components=len(package['components']),sha256=digest(package_bytes)),
        primary_evidence=dict(excerpts_verified=excerpts,instructions_verified=instructions,sha256=evidence_hashes,
            dialog_resource_verified=True,double_constants_verified=4),scopes=scopes,
        corpus=dict(files=len(files),records=total,normalization_changes=0,outside_first_person_ddx_range=0,
            raw_value_counts={hex(o):{str(k):v for k,v in sorted(c.items())} for o,c in counters.items()},file_details=files),
        unchanged_inputs=inputs,unchanged_executables=hashes,limitations=SCHEMA['limitations'])
    (ROOT/'phase20-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','fields','first_person_ddx_bindings','boundary_cases','exact_in_memory_container_patches']}))
    print('Corpus:',len(files),'files;',total,'creatures; no callback changes or DDX range exceptions')
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions')
    print('Unchanged:',len(inputs),'inputs and',len(hashes),'executables')

if __name__=='__main__':main()
