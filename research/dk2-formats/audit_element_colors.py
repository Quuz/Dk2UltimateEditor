"""Audit native color/fade projections, emulations, corpus and exact byte patches."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_element_colors import initial_color,alpha_threshold,fade_after_lifetime_update,update_projection,record_view
from emulate_element_colors import build_case

ROOT=Path(__file__).resolve().parent
def digest(b):return hashlib.sha256(b).hexdigest()
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))

def main():
    schema=load('element-color-semantics.json');cases=0
    for kind in range(13):
        for flags in [0,2,0xFFFFFFFF]:
            for raw in [(0,1,2),(255,128,48)]:
                p=initial_color(kind,flags,raw,resolved_player_rgb=[0,48,255])
                expected=(255,255,255,255) if kind==1 else (48,48,255,255) if flags&2 else raw+(255,)
                assert p['channels']==expected and p['packed']==sum(c<<(8*i) for i,c in enumerate(expected));cases+=1
    for c in [0,1,47,48,49,254,255]:
        assert initial_color(3,2,[1,2,3],resolved_player_rgb=[c,c,c])['channels']==(max(c,48),)*3+(255,);cases+=1
    for h in [0,1,128,600,65535]:
        for percentage in [0,1,25,70,99,100,101,255]:
            p=alpha_threshold(h,percentage);expected=h-h*percentage//100
            assert p['signed']==expected and p['unsigned']==expected&0xFFFFFFFF;cases+=1
    rgba=(100,50,10,255)
    for kind in range(13):
        for flags in [0,8]:
            for h in [0,1,24,25,100,101,-1]:
                p=fade_after_lifetime_update(rgba,h,25,flags,kind,0x120)
                assert p['alpha_written']==bool(flags and h!=0 and h<25)
                assert p['renderer_flags']==(0x121 if p['alpha_written'] and kind in [2,3,5] else 0x120)
                assert p['rgb_branch_taken']==bool(flags and h!=0 and h<=100)
                if not flags or h==0:assert p['channels']==rgba
                cases+=1
    p=fade_after_lifetime_update(rgba,100,0,8,3);assert p['channels']==(99,49,9,255);cases+=1
    p=fade_after_lifetime_update(rgba,101,0,8,3);assert p['channels']==rgba;cases+=1
    p=fade_after_lifetime_update(rgba,1,0,8,3);assert p['channels']==(0,0,0,255);cases+=1
    p=fade_after_lifetime_update((100,200,250,255),-1,0,8,3);assert p['channels']==(200,144,244,255);cases+=1
    # A multi-tick call evaluates the fade block only once.
    single=update_projection(rgba,10,2,0,8,3)
    first=update_projection(rgba,10,1,0,8,3);twice=update_projection(first['channels'],9,1,0,8,3)
    assert single['channels'][0]==87 and twice['channels'][0]==77;cases+=1
    p=update_projection(rgba,-(1<<31),1,0,8,3)
    assert p['remaining_lifetime']==(1<<31)-1 and p['channels']==rgba;cases+=1
    for action in [lambda:initial_color(3,2,[1,2,3]),lambda:initial_color(True,0,[1,2,3]),
        lambda:initial_color(3,-1,[1,2,3]),lambda:initial_color(3,0,[1,2,256]),lambda:initial_color(3,0,[1,2]),
        lambda:initial_color(3,0,[1,2,True]),lambda:alpha_threshold(-1,100),lambda:alpha_threshold(65536,100),
        lambda:alpha_threshold(1,256),lambda:alpha_threshold(1,True),
        lambda:fade_after_lifetime_update(rgba,1<<31,0,8,3),lambda:fade_after_lifetime_update(rgba,1,-1,8,3),
        lambda:update_projection(rgba,1,-1,0,8,3),lambda:update_projection(rgba,1,True,0,8,3),
        lambda:record_view(bytes(181)),lambda:record_view(bytes(183)),lambda:record_view('x'*182)]:
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
    walk(schema)
    for name in ['init','tick']:
        before=(ROOT/'ghidra'/f'phase21-element-{name}-assembly.txt').read_bytes()
        after=(ROOT/'ghidra'/f'phase21-element-{name}-assembly-after-emulation.txt').read_bytes()
        assert before==after
    emulations=[]
    for row in schema['native_emulations']:
        path=ROOT/'ghidra'/row['file'];b=path.read_bytes();assert digest(b)==row['sha256'];v=json.loads(b)
        args,expected,adapter=build_case(v['case']);assert v['input']==args and v['expected_registers']==expected and v['adapter']==adapter
        result=v['result'];assert result['success'] and result['hit_return']
        for register,value in expected.items():assert int(result['registers'][register],16)==value
        emulations.append(row)
    assert len(emulations)==26
    patches=0;scopes=[]
    spans=[(0xAE,3),(0xB1,2),(0xB3,1),(0x9C,4),(0x60,4),(0x70,1)]
    for name,path in [('EffectElements',DATA/'EffectElements.kwd'),('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();sections=fmt.parse(b);records_count=0
        for si,s in enumerate(sections):
            if s.signature!=250:continue
            for ri,r in enumerate(s.records):
                records_count+=1;record_view(r)
                for offset,width in spans:
                    new=bytes(c^255 for c in r[offset:offset+width]);changed=bytearray(r);changed[offset:offset+width]=new
                    records=list(s.records);records[ri]=bytes(changed);clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else section.pack() for i,section in enumerate(sections))
                    absolute=s.offset+len(s.header)+ri*len(r)+offset
                    assert packed[:absolute]==b[:absolute] and packed[absolute+width:]==b[absolute+width:]
                    assert fmt.parse(packed)[si].records[ri]==bytes(changed);patches+=1
        assert digest(path.read_bytes())==digest(b);scopes.append(dict(name=name,path=str(path),records=records_count,sha256=digest(b)))
    total=0;files=[];index_pairs=Counter();fade_percentages=Counter();types=Counter();fade_flags=Counter();special=[];invalid_hp=[];threshold_projections=0
    for file in load('corpus-scan.json')['files']:
        if not any(s['family']=='EffectElements' for s in file.get('sections',[])):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];local_count=0
        for s in fmt.parse(b):
            if s.signature!=250:continue
            for r in s.records:
                total+=1;local_count+=1;v=record_view(r)
                index_pairs[(v['random_color_index'],v['table_color_index'])]+=1;fade_percentages[v['fade_percentage']]+=1
                types[(v['resource_type'],bool(v['resource_flags']&2))]+=1;fade_flags[bool(v['element_flags']&8)]+=1
                p=initial_color(v['resource_type'],v['resource_flags'],v['raw_rgb'],resolved_player_rgb=[0,48,255])
                assert p['channels'][3]==255 and 0<=p['packed']<=0xFFFFFFFF
                if v['min_hp']>v['max_hp']:invalid_hp.append(dict(file=file['file'],id=v['id'],name=v['name']))
                for h in set([v['min_hp'],v['max_hp']]):
                    t=alpha_threshold(h,v['fade_percentage']);threshold_projections+=1
                    if v['fade_percentage']==100:assert t['unsigned']==0
                    result=update_projection(p['channels'],h,1,t['unsigned'],v['element_flags'],v['resource_type'])
                    assert all(0<=c<=255 for c in result['channels'])
                if v['fade_percentage']!=100:special.append(dict(file=file['file'],**v))
        files.append(dict(file=file['file'],sha256=digest(b),records=local_count))
    assert len(files)==80 and total==47224 and index_pairs==Counter({(0,0):47224})
    assert fade_percentages==Counter({100:47144,70:80})
    assert all(v['id']==294 and v['name']=='Blood Pool Element' for v in special)
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    hashes={}
    for file,expected in schema['executable_sha256'].items():
        actual=digest((ROOT.parent.parent/file).read_bytes());assert actual==expected;hashes[file]=actual
    package_bytes=(ROOT/'editor-format-package.json').read_bytes();package=json.loads(package_bytes)
    assert len(package['components'])>=32 and package['element_color_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=21,date='2026-10-25',boundary_cases=cases,exact_in_memory_container_patches=patches,
        native_fragment_emulations=len(emulations),modified_gameplay_runs=0,
        primary_evidence=dict(excerpts_verified=excerpts,instructions_verified=instructions,sha256=evidence_hashes,
            original_ghidra_disassembly_unchanged_after_emulation=True),emulations=emulations,scopes=scopes,
        corpus=dict(files=len(files),records=total,index_pairs=[dict(random=k[0],table=k[1],count=v) for k,v in sorted(index_pairs.items())],
            fade_percentage_counts=dict(sorted(fade_percentages.items())),fade_flag_counts={str(k):v for k,v in fade_flags.items()},
            resource_type_and_player_colour_counts=[dict(type=k[0],player_coloured=k[1],count=v) for k,v in sorted(types.items())],
            selected_lifetime_threshold_projections=threshold_projections,non100_fade_examples=special,
            min_hp_above_max_hp=invalid_hp,file_details=files),
        schema_sha256=digest((ROOT/'element-color-semantics.json').read_bytes()),
        format_package=dict(components=len(package['components']),sha256=digest(package_bytes)),
        unchanged_inputs=inputs,unchanged_executables=hashes,limitations=schema['limitations'])
    (ROOT/'phase21-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','exact_in_memory_container_patches','native_fragment_emulations']}))
    print('Corpus:',len(files),'files;',total,'elements;',threshold_projections,'lifetime endpoint projections')
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions; original Ghidra disassembly unchanged')
    print('Unchanged:',len(inputs),'inputs and',len(hashes),'executables')

if __name__=='__main__':main()
