"""Audit the supplemental Effects chain and Creatures priority interpretation."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_effect_chains import effect_record,project_chain,graph_audit
from native_creature_priority import priority_view,commit_converted_integer
from build_effect_semantics import pe_read
from extract_editor_strings import resource_leaves

ROOT=Path(__file__).resolve().parent

def digest(b):return hashlib.sha256(b).hexdigest()
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))
def save(file,value):(ROOT/file).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
def record(ident,next_id):
    b=bytearray(246);struct.pack_into('<H',b,0xB8,ident);struct.pack_into('<H',b,0xC0,next_id);return bytes(b)

def main():
    cases=0
    r=[record(1,40000),record(40000,65535),record(65535,0)]
    for start,expected in [(0,[]),(1,[1,40000,65535]),(40000,[40000,65535]),(65535,[65535])]:
        p=project_chain(r,start,step_budget=8);assert p['created_ids']==expected and p['stop_reason']=='zero_id'
        assert p['output_first_id']==(expected[0] if expected else None)
        assert p['global_head_last_id']==(expected[-1] if expected else None)
        assert p['new_global_list_ids']==expected[::-1] and p['native_return_if_modeled_exit']==int(bool(expected));cases+=1
    for capacity in range(4):
        p=project_chain(r,1,step_budget=8,allocation_successes=capacity)
        assert p['created_ids']==[1,40000,65535][:capacity]
        assert p['stop_reason']==('zero_id' if capacity==3 else 'allocation_failed_scenario')
        assert p['native_return_if_modeled_exit']==int(capacity>0);cases+=1
    p=project_chain(r,1,step_budget=8,creation_enabled=False)
    assert p['created_ids']==[] and not p['output_pointer_written'] and p['native_return_if_modeled_exit']==0;cases+=1
    for rows,start,stop,prefix in [([record(1,9)],1,'missing_definition_tool_stop',[1]),
        ([record(1,2),record(2,0),record(2,0)],1,'duplicate_id_tool_stop',[1]),
        ([record(1,1)],1,'cycle_tool_stop',[1]),
        ([record(1,2),record(2,3),record(3,2)],1,'cycle_tool_stop',[1,2,3]),
        ([record(0,0),record(1,0)],1,'zero_definition_tool_stop',[])]:
        p=project_chain(rows,start,step_budget=8)
        assert p['stop_reason']==stop and p['created_ids']==prefix and p['native_return_if_modeled_exit'] is None;cases+=1
    p=project_chain(r,1,step_budget=2)
    assert p['created_ids']==[1,40000] and p['pending_id']==65535 and p['stop_reason']=='step_budget_tool_stop';cases+=1
    # One canonical directed cycle, including tails reaching an already-seen component.
    g=graph_audit([record(1,2),record(2,3),record(3,2),record(4,3),record(5,6),record(6,5)])
    assert g['cycles']==[[2,3],[5,6]] and not g['missing_links'];cases+=1
    g=graph_audit([record(2,9),record(2,0),record(3,2)])
    assert g['duplicate_ids']==[2] and g['missing_links']==[dict(source_id=2,target_id=9)];cases+=1
    for size in [5449,5537]:
        b=bytearray((i*29+3)&255 for i in range(size))
        for value in [0,1,40,127,128,255]:
            b[0xF55]=value;b[0xF45]=77;before=bytes(b);v=priority_view(b)
            assert v['raw']==v['native_editor_display']==v['game_runtime_byte']==value and v['spell_swipe_raw']==77
            assert v['gui_validation_range'] is None and not v['gameplay_ordering_confirmed'] and bytes(b)==before;cases+=1
    for value,expected in [(-257,255),(-256,0),(-1,255),(0,0),(40,40),(255,255),(256,0),(257,1),((1<<63)-1,255),(-(1<<63),0)]:
        assert commit_converted_integer(value)==expected;cases+=1
    invalid=[lambda:effect_record(b''),lambda:priority_view(bytes(5448)),lambda:priority_view(bytes(5538)),
        lambda:commit_converted_integer(1.5),lambda:commit_converted_integer(True),lambda:commit_converted_integer(1<<63),
        lambda:project_chain(r,-1,step_budget=8),lambda:project_chain(r,65536,step_budget=8),
        lambda:project_chain(r,True,step_budget=8),lambda:project_chain(r,1,step_budget=0),
        lambda:project_chain(r,1,step_budget=True),lambda:project_chain(r,1,step_budget=8,allocation_successes=-1),
        lambda:project_chain(r,1,step_budget=8,creation_enabled=1)]
    for action in invalid:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
        cases+=1
    evidence_hashes={};lines_verified=0;instructions=0
    def walk(v):
        nonlocal lines_verified,instructions
        if isinstance(v,dict):
            if 'file' in v and ('excerpt' in v or 'instructions' in v):
                b=(ROOT/'ghidra'/v['file']).read_bytes();lines=b.decode('utf8').splitlines()
                if 'excerpt' in v:assert lines[v['line']-1].strip()==v['excerpt'];lines_verified+=1
                else:assert all(i in lines for i in v['instructions']);instructions+=len(v['instructions'])
                evidence_hashes[v['file']]=digest(b)
            for child in v.values():walk(child)
        elif isinstance(v,list):
            for child in v:walk(child)
    chain=load('effect-chain-semantics.json');creature=load('creature-priority-semantics.json');walk(chain);walk(creature)
    game=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    for row in chain['bridge_vtable']:assert pe_read(game,row['table_address'],4).hex()==row['bytes_hex'] and int.from_bytes(bytes.fromhex(row['bytes_hex']),'little')==row['target']
    editor_path=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe';editor=editor_path.read_bytes();label=creature['label_resource']
    text=editor[label['text_file_offset']:label['text_file_offset']+label['utf16_bytes']]
    assert text.decode('utf-16-le')==creature['native_label'] and digest(text)==label['text_sha256']
    leaf=next(x for x in resource_leaves(editor) if x[0]==label['resource_path'])
    assert digest(leaf[3])==label['block_sha256']
    assert label['id']==(label['resource_path'][1]-1)*16+label['id']%16==0xF855
    assert creature['native_editor']['writer']['field_stack_offset']-creature['native_editor']['writer']['buffer_stack_offset']==creature['field']['raw_offset']
    patches=0;scopes=[]
    for name,path in [('Effects',DATA/'Effects.kwd'),('Creatures',DATA/'Creatures.kwd'),
        ('AutoSaveGlobals',DATA/'AutoSaveGlobals.kld'),('kop4Globals',DATA/'maps/kop4Globals.kld')]:
        b=path.read_bytes();sections=fmt.parse(b);scope=dict(name=name,path=str(path),sha256=digest(b),creatures=0,effects=0)
        for si,s in enumerate(sections):
            if s.signature not in [170,270]:continue
            offset,width=(0xF55,1) if s.signature==170 else (0xC0,2)
            scope['creatures' if s.signature==170 else 'effects']+=len(s.records)
            if s.signature==270:scope['effect_graph']=graph_audit(s.records)
            for ri,r in enumerate(s.records):
                changed=bytearray(r);new=(int.from_bytes(r[offset:offset+width],'little')^((1<<(width*8))-1)).to_bytes(width,'little')
                changed[offset:offset+width]=new;records=list(s.records);records[ri]=bytes(changed)
                clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                packed=b''.join(clone.pack() if i==si else sec.pack() for i,sec in enumerate(sections))
                absolute=s.offset+len(s.header)+ri*len(r)+offset
                assert packed[:absolute]==b[:absolute] and packed[absolute+width:]==b[absolute+width:]
                parsed=fmt.parse(packed)[si].records[ri];assert parsed[offset:offset+width]==new
                if s.signature==170:assert parsed[0xF45]==r[0xF45]
                patches+=1
        assert digest(path.read_bytes())==digest(b);scopes.append(scope)
    files=[];total_effects=0;total_creatures=0;links=0;priorities=Counter();differ=0;longest=[]
    for file in load('corpus-scan.json')['files']:
        if not any(s['family'] in ['Creatures','Effects'] for s in file.get('sections',[])):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];row=dict(file=file['file'],sha256=digest(b),effect_graphs=[],creatures=0)
        for s in fmt.parse(b):
            if s.signature==270:
                g=graph_audit(s.records);total_effects+=g['records'];links+=g['nonzero_links']
                assert not g['duplicate_ids'] and not g['missing_links'] and not g['cycles'] and not g['zero_id_records']
                row['effect_graphs'].append(g)
                for r in s.records:
                    v=effect_record(r);p=project_chain(s.records,v['id'],step_budget=len(s.records)+1)
                    assert p['stop_reason']=='zero_id' and p['created_ids'][0]==v['id']
                if len(g['longest_chain_ids'])>len(longest):
                    index={effect_record(r)['id']:effect_record(r) for r in s.records}
                    longest=[index[k] for k in g['longest_chain_ids']]
            if s.signature==170:
                for r in s.records:
                    v=priority_view(r);priorities[v['raw']]+=1;differ+=v['raw']!=v['spell_swipe_raw'];total_creatures+=1;row['creatures']+=1
        files.append(row)
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    exe_hashes={}
    for file,expected in load('executable-hash-verification.json').items():
        actual=digest((ROOT.parent.parent/file).read_bytes());assert actual==expected;exe_hashes[file]=actual
    assert digest(game)==chain['executable_sha256']==creature['executable_sha256']['game']
    assert digest(editor)==creature['executable_sha256']['editor']
    result=dict(phase=17,boundary_cases=cases,exact_in_memory_container_patches=patches,native_emulations=0,
        evidence=dict(excerpts_verified=lines_verified,instructions_verified=instructions,sha256=evidence_hashes,
            bridge_table_entries_verified=4,label_resource_verified=True),scopes=scopes,
        corpus=dict(files=len(files),effects=total_effects,nonzero_next_links=links,creatures=total_creatures,
            priority_values=dict(sorted(priorities.items())),priority_differs_from_spell_swipe=differ,longest_chain=longest,
            graph_anomalies=0,file_details=files),
        unchanged_inputs=inputs,unchanged_executables=exe_hashes,
        limitations=['No game input files written; patch experiments are entirely in memory.',
            'No modified-map gameplay validation; modeled allocations are explicit test scenarios, not an emulated allocator.',
            'Further Ghidra requests paused after automatic approval review reported a usage limit.'])
    save('phase17-validation.json',result)
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','exact_in_memory_container_patches','native_emulations']}))
    print(json.dumps({k:v for k,v in result['corpus'].items() if k!='file_details'}))
    print('Verified unchanged:',len(inputs),'primary inputs and',len(exe_hashes),'executables.')

if __name__=='__main__':main()
