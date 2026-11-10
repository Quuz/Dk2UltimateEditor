"""Audit scaled views, native evidence, conditional update loss and exact byte patches."""
from pathlib import Path
from fractions import Fraction
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from extract_editor_strings import extract,resource_leaves
from native_creature_editor_scaled import SCHEMA,FIELDS,views,loaded_value,project_loaded_property_update,patch_raw

ROOT=Path(__file__).resolve().parent
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def main():
    cases=0
    for size in SCHEMA['record_variants']:
        seed=bytes((i*19+7)&255 for i in range(size))
        for f in SCHEMA['fields']:
            offset=f['raw_offset'];scale=f['raw_to_display'];fraction=Fraction(scale['numerator'],scale['denominator'])
            for raw in [0,1,4095,4096,65535,65536,0x7FFFFFFF,0x80000000,0xFFFFFFFF]:
                changed=patch_raw(seed,offset,raw);v=next(v for v in views(changed) if v['raw_offset']==offset)
                exact=raw*fraction
                assert v['raw']==raw and Fraction(v['native_editor_value'])==exact
                assert changed[:offset]==seed[:offset] and changed[offset+4:]==seed[offset+4:] and len(changed)==size
                p=project_loaded_property_update(offset,raw)
                assert p['raw_after_projected']==((((raw>>12)&255)<<12) if offset==0xEF4 else raw)
                assert v['native_editor_value']>=0 and v['gui_validation_range'] is None;cases+=1
    for seconds,expected in [(0,0),(1,1),(255,255),(256,0),(280,24),(300,44),(360,104),(1000,232),(32000,0)]:
        p=project_loaded_property_update(0xEF4,seconds<<12)
        assert p['display_after_update']==expected and p['raw_after_projected']==expected<<12;cases+=1
    for raw,expected in [(4095,0),(4097,4096),(1048575,1044480),(1048576,0),(0xFFFFFFFF,1044480)]:
        assert project_loaded_property_update(0xEF4,raw)['raw_after_projected']==expected;cases+=1
    assert loaded_value(0xFCD,4096)==100 and loaded_value(0xEAC,65536)==1;cases+=2
    # Prove the sign and bound of the Scale writer's binary64 approximation error.
    scale=FIELDS[0xFCD];c=SCHEMA['constants'][hex(scale['display_to_writer']['constant_address'])]
    product=Fraction(25,1024)*Fraction(c['numerator'],c['denominator']);error=(product-1)*0xFFFFFFFF
    assert 0<error<Fraction(1,1000000);cases+=1
    seed=bytes(5537)
    for action in [lambda:views(bytes(5448)),lambda:views(bytes(5538)),lambda:views('x'*5537),
        lambda:loaded_value(0xF53,1),lambda:loaded_value(True,1),lambda:loaded_value(0xEF4,-1),
        lambda:loaded_value(0xEF4,1<<32),lambda:loaded_value(0xEF4,True),lambda:loaded_value(0xEF4,1.0),
        lambda:patch_raw(seed,0xEF4,1<<32),lambda:project_loaded_property_update(0xEF5,1)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    evidence_hashes={};excerpts=0;instructions=0
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
    editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    for row in SCHEMA['constants'].values():
        b=pe_read(editor,row['address'],8);assert b.hex()==row['bytes_hex'] and digest(b)==row['sha256']
        value=struct.unpack('<d',b)[0];assert value==row['binary64_value'] and Fraction(value)==Fraction(row['numerator'],row['denominator'])
    strings={s['id']:s for s in extract(editor)};leaves={tuple(x[0]):x for x in resource_leaves(editor)}
    for f in SCHEMA['fields']:
        label=f['label_resource'];assert label==strings[f['property_id']] and label['text']==f['native_label']
        assert digest(leaves[tuple(label['resource_path'])][3])==label['block_sha256']
    patches=0;scopes=[]
    for name,path in [('Creatures',DATA/'Creatures.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();sections=fmt.parse(b);records_count=0;torture_changed=0
        for si,s in enumerate(sections):
            if s.signature!=170:continue
            for ri,r in enumerate(s.records):
                records_count+=1;torture_changed+=project_loaded_property_update(0xEF4,fmt.u32(r,0xEF4))['changed']
                for f in SCHEMA['fields']:
                    offset=f['raw_offset'];new=fmt.u32(r,offset)^0xFFFFFFFF;changed=patch_raw(r,offset,new)
                    records=list(s.records);records[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else section.pack() for i,section in enumerate(sections))
                    absolute=s.offset+len(s.header)+ri*len(r)+offset
                    assert packed[:absolute]==b[:absolute] and packed[absolute+4:]==b[absolute+4:]
                    assert fmt.parse(packed)[si].records[ri]==changed;patches+=1
        assert digest(path.read_bytes())==digest(b)
        scopes.append(dict(name=name,path=str(path),sha256=digest(b),records=records_count,
            changed_if_torture_property_updated_with_original_value=torture_changed))
    raw_counts={offset:Counter() for offset in FIELDS};changes=Counter();files=[];examples=[];torture_pairs=Counter();total=0
    for file in load('corpus-scan.json')['files']:
        if not any(s['family']=='Creatures' for s in file.get('sections',[])):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];local_count=0;local_changes=0
        for s in fmt.parse(b):
            if s.signature!=170:continue
            for r in s.records:
                total+=1;local_count+=1
                for v in views(r):
                    raw_counts[v['raw_offset']][v['raw']]+=1;p=project_loaded_property_update(v['raw_offset'],v['raw'])
                    if p['changed']:changes[v['raw_offset']]+=1
                    if v['raw_offset']==0xEF4:
                        torture_pairs[(p['raw_before'],p['raw_after_projected'])]+=1
                        if p['changed']:
                            local_changes+=1
                            if len(examples)<20:examples.append(dict(file=file['file'],creature_id=r[0xF4B],name=fmt.string(r[:32]),**p))
        files.append(dict(file=file['file'],sha256=digest(b),records=local_count,torture_conditional_changes=local_changes))
    assert total==3642 and changes==Counter({0xEF4:941})
    primitive=load('creature-editor-scalars.json')
    assert not set(FIELDS)&{f['raw_offset'] for f in primitive['fields']}
    assert len(FIELDS)+len(primitive['fields'])==57
    distributions=[]
    for f in SCHEMA['fields']:
        counts=raw_counts[f['raw_offset']];assert sum(counts.values())==total
        distributions.append(dict(raw_offset=f['raw_offset'],native_label=f['native_label'],
            raw_values={str(k):v for k,v in sorted(counts.items())},
            raw_minimum=min(counts),raw_maximum=max(counts),
            changed_if_this_property_updated_with_original_value=changes[f['raw_offset']]))
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    exe_hashes={}
    for file,expected in SCHEMA['executable_sha256'].items():
        actual=digest((ROOT.parent.parent/file).read_bytes());assert actual==expected;exe_hashes[file]=actual
    package_bytes=(ROOT/'editor-format-package.json').read_bytes();package=json.loads(package_bytes)
    assert len(package['components'])>=30 and package['creature_editor_scaled']==SCHEMA
    for key,item in package['components'].items():
        b=(ROOT/item['file']).read_bytes();assert digest(b)==item['sha256'] and json.loads(b)==package[key]
    result=dict(phase=19,properties=len(FIELDS),boundary_cases=cases,exact_in_memory_container_patches=patches,
        native_emulations=0,modified_gameplay_runs=0,schema_sha256=digest((ROOT/'creature-editor-scaled.json').read_bytes()),
        format_package=dict(components=len(package['components']),sha256=digest(package_bytes)),
        primary_evidence=dict(excerpts_verified=excerpts,instructions_verified=instructions,sha256=evidence_hashes,
            constant_bytes_verified=len(SCHEMA['constants']),resource_strings_verified=len(FIELDS)),scopes=scopes,
        scale_inverse_bound=dict(maximum_positive_error_numerator=error.numerator,maximum_positive_error_denominator=error.denominator,
            scope='Exact rational error of the stored binary64 constant on unchanged loaded unsigned DWORD values. Not a complete x87 emulator.'),
        corpus=dict(files=len(files),records=total,field_distributions=distributions,conditional_torture_changes=941,
            torture_raw_pairs=[dict(before=k[0],after=k[1],count=v) for k,v in sorted(torture_pairs.items())],
            conditional_change_examples=examples,file_details=files),unchanged_inputs=inputs,unchanged_executables=exe_hashes,
        limitations=SCHEMA['limitations'])
    (ROOT/'phase19-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','properties','boundary_cases','exact_in_memory_container_patches','native_emulations']}))
    print('Corpus:',len(files),'files;',total,'creatures;',941,'conditional Torture Time changes')
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;',len(SCHEMA['constants']),'PE constants')
    print('Unchanged:',len(inputs),'inputs and',len(exe_hashes),'executables')

if __name__=='__main__':main()
