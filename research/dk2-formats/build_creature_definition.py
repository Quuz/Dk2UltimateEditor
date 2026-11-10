"""Publish native full Creature-definition evidence without naming opaque bytes."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
from build_effect_semantics import pe_read
import emulate_creature_definition as partial
import emulate_raw_creature_definition as raw
import memory_readout_ymm as readout
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def spans(a,b):
    assert len(a)==len(b);out=[]
    for i,(x,y) in enumerate(zip(a,b)):
        if x==y:continue
        if out and out[-1]['offset']+out[-1]['bytes']==i:out[-1]['bytes']+=1
        else:out.append(dict(offset=i,bytes=1))
    return out

def primary():
    result=[]
    for group in ['functions','blocks']:
        rows=[]
        for r in load('phase85-primary-manifest.json')[group]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            row={**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)}
            if group=='functions':row['assembly_sha256']=digest((ROOT/'ghidra'/r['assembly']).read_bytes())
            else:row['hex']=b.hex()
            rows.append(row)
        result.append(rows)
    return tuple(result)

def snapshots():
    rows=[];captures=[];images={}
    for kind,module,manifest,prefix in [('partial',partial,'phase85-creature-definition-emulations.json','phase85-creature-definition-'),('raw',raw,'phase85-raw-creature-definition-emulations.json','phase85-raw-creature-definition-')]:
        index={(r['case'],r['page']):r for r in load(manifest)['replays']}
        for c in module.cases():
            chunks=[];counts=[]
            for page in range(9):
                row=index.pop((c['name'],page));p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());args,e=module.build(c,page);r=saved['result']
                assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['page']==page and saved['input']==args and saved['expected']==e
                assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items())
                chunks.append(readout.decode(r['registers']));rows.append(dict(row,kind=kind))
                if kind=='raw':counts.append([int(r['registers'][k],16) for k in ['EDX','ESI','EDI']])
            b=b''.join(chunks)[:2199];module.verify(c,b,e);name='ghidra/'+prefix+c['name']+'.bin';assert (ROOT/name).read_bytes()==b;images[c['name']]=b
            if counts:assert all(v==counts[0] for v in counts)
            captures.append(dict(case=c['name'],kind=kind,**link(name),bytes=2199,priority=b[0x6EB],jump=struct.unpack_from('<I',b,0x75C)[0],checksum=struct.unpack_from('<I',b,0x5AF)[0],sound_gfx_counts=counts[0][:2] if counts else [0,0]))
        assert not index
    comparisons=[]
    allowed={
      'portal-without-presence':[(0x6EC,96)],'seven-offsets':[(0x6EC,96),(0x6CD,2)],
      'offsets-presence-off':[(0x6EC,12)],'mid-eighth-only':[(0x6CD,2)],
      'priority-jump-max':[(0x6EB,1),(0x75C,4)],'priority-jump-signed-bit':[(0x6EB,1),(0x75C,4)],
      'priority-jump-presence-off':[],'overlay-checksum-retain':[(0x6EB,1),(0x75C,4)],
      'durations20':[(0x74C,20)],'durations60':[(0x74C,20)]}
    for n,sp in allowed.items():
        delta=spans(images['empty-mid'],images[n]);assert all(any(a<=i<a+s for a,s in sp+[(0x5AF,4)]) for d in delta for i in range(d['offset'],d['offset']+d['bytes']))
        comparisons.append(dict(before='empty-mid',after=n,differences=delta,allowed_spans=[dict(offset=a,bytes=s) for a,s in sp+[(0x5AF,4)]]))
    for n,sp in [('raw-imp-offsets',[(0x6EC,96),(0x5AF,4)]),('raw-imp-priority-jump',[(0x6EB,1),(0x75C,4),(0x5AF,4)]),('raw-imp-long-tail',[])]:
        delta=spans(images['raw-imp'],images[n]);assert all(any(a<=i<a+s for a,s in sp) for d in delta for i in range(d['offset'],d['offset']+d['bytes']))
        comparisons.append(dict(before='raw-imp',after=n,differences=delta,allowed_spans=[dict(offset=a,bytes=s) for a,s in sp]))
    seed=spans(images['ctor-zero'],images['ctor-a5']);assert sum(d['bytes'] for d in seed)==249
    assert images['mid-eighth-only'][0x6EC:0x74C]==images['empty-mid'][0x6EC:0x74C]
    assert images['mid-eighth-only'][0x6CD:0x6CF]==bytes(2) and images['empty-mid'][0x6CD:0x6CF]==b'\xA5\xA5'
    assert len(rows)==171 and len(captures)==19
    return dict(replays=rows,captures=captures,comparisons=comparisons,constructor_seed_dependent_spans=seed,constructor_seed_dependent_bytes=249)

def corpus():
    files=[];count=patches=0;fields=[0xE94+4*j for j in range(3)]+[0x127B+12*i+4*j for i in range(7) for j in range(3)];values=[0,0x80000000,0xFFFFFFFF];aggregate=hashlib.sha256()
    for r in load('creature-lookup-review.json')['corpus']['files']:
        p=Path(r['file']);data=p.read_bytes();assert digest(data)==r['sha256'];sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
        hits=[(i,s) for i,s in enumerate(sections) if s.signature==170];assert hits
        count+=sum(len(s.records) for _,s in hits);si,s=hits[0];assert s.records;original=s.records[0]
        for o in fields:
            for value in values:
                changed=bytearray(original);struct.pack_into('<I',changed,o,value);records=list(s.records);records[0]=bytes(changed)
                clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                packed=b''.join(clone.pack() if j==si else part.pack() for j,part in enumerate(sections));expected=bytearray(data);absolute=s.offset+len(s.header)+o;struct.pack_into('<I',expected,absolute,value)
                assert packed==expected and fmt.parse(packed)[si].records==records
                aggregate.update(hashlib.sha256(packed).digest());patches+=1
        files.append(dict(file=str(p),sha256=r['sha256'],records=sum(len(s.records) for _,s in hits)))
    assert len(files)==82 and count==3734 and patches==5904
    return dict(files=files,source_files_hashchecked=82,records=3734,raw_coordinate_dword_offsets=fields,values=values,new_exact_container_patches=patches,ordered_patch_digest=aggregate.hexdigest(),
      scope='All24 XYZ DWORD offsets ×3values in first Creature record of each of82 source containers. Exact whole-container equality outside selected4bytes;all3734 source records roundtrip. Physical copies counted;not3734 native emulations. No source writes.')

def diagnostics():
    out={}
    for family,name in [('Creatures','phase85-creature-index-diagnostics.json'),('Objects','phase85-object-index-diagnostics.json')]:
        j=load(name);events={e['event']['address']:e['event'] for e in j['indexed_events']}
        out[family]=dict(**link(name),roots=len(j['producer_roots']),root_states=j['root_states'],callee_summaries=j['callee_summaries'],control_candidates=len(j['candidates']),indexed_events=len(j['indexed_events']),unique_indexed_events=len(events),limited_callees=j['limited_callees'],limited_targets=[v['target'] for v in j['limited_contexts']])
    out['scope']='Pointer hints and bounded alias analysis produce leads,not universal type proof. Closest6EC event resolved as8XYZ accessor. Other14 unique events not allclassified. Limited492320/49DD60 are definition loaders;no global non-use conclusion forPriority/Jump.'
    return out

def main():
    functions,blocks=primary();native=snapshots()
    s=dict(phase=85,date='2026-10-28',family='Creatures',executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),primary_program_bytes=functions,primary_data_ranges=blocks,
      counts=dict(logical_scenarios=19,partial_cases=13,raw_cases=6,native_emulation_invocations=171,raw_init_convert_loader_chains=54,native_constructors=171,native_definition_loaders=153,coordinate_getter_calls=1710,code_spans_attested=17,original_vtable_ranges=1),native=native,corpus=corpus(),indexed_diagnostics=diagnostics(),
      definition=dict(bytes=2199,constructor='0x491DC0',loader='0x492320',game_intermediate_bytes=6457,raw_initializer='0x568820',raw_converter='0x568E30',editor_intermediate_bytes=6561,warning='Complete2199-byte captures do not decode all2199 bytes. 249 seed-dependent bytes observed for constructor seeds00/A5;do not label them all padding.'),
      coordinate_array=dict(getter='0x494650',definition_offset=0x6EC,count=8,stride=12,components=3,component_bytes=4,invalid_unsigned_indices=[8,0xFFFFFFFF],invalid_result='Pointer to12zero-byte static fallback6E4148;fixture guard6E4144 bit1 already initialized.',
        portal=dict(runtime_index=0,raw_offset=0xE94,intermediate_offset=0x1284,definition_offset=0x6EC,presence='Copied unconditionally byloader,even allpresence flags zero'),
        additional=dict(runtime_indices=[1,7],count=7,raw_offset=0x127B,raw_stride=12,intermediate_offset=0x166B,presence_dword_offset=0x2EC,presence_stride=4,definition_offset=0x6F8,definition_stride=12,condition='Original loader loops index0..6;copy only when corresponding presence DWORD nonzero'),
        helper_boundary=dict(presence_getter='0x56A120',coordinate_getter='0x56A140',accepted_unsigned_index_count=8,actual_loader_count=7,
          index7_pointer=0x16BF,index7_presence=0x308,overlap='mid16BF is animation resource slot38;mid308 is also presence flag0 for independent ten-WORD def6CD array. Artificial index7 presence updatesdef6CD frommid10F5;noXYZ row8 loaded.',
          warning='Do not model8 additional XYZ triples. Boundary mismatch in private helper is not proof of an editor-writing bug;actual inspected caller is bounded7. Invalid56A140 returns an expired stack-local pointer;invalid branch not emulated here.')),
      scalars=[dict(baseline_name='Pick-Up Priority',raw_offset=0xF55,raw_bytes=1,intermediate_offset=0x1345,presence_dword=0x1E8,definition_offset=0x6EB,definition_bytes=1,transfer='All8bits preserved when presence nonzero',gameplay_purpose='unresolved'),
        dict(baseline_name='AbilityJump',raw_offset=0xEE0,raw_bytes=4,intermediate_offset=0x12D0,presence_dword=0x204,definition_offset=0x75C,definition_bytes=4,transfer='All32bits copied;noTPS multiply',gameplay_purpose='unresolved')],
      durations=dict(raw_offsets=[0xED0,0xED4,0xED8,0xEDC],intermediate_offsets=[0x12C0,0x12C4,0x12C8,0x12CC],presence_dwords=[0x1F4,0x1F8,0x1FC,0x200],definition_offsets=[0x74C,0x750,0x754,0x758],formula='(low32(rawDWORD * TPS) >> 12),logical shift;TPS=DWORD[DWORD6E4154+40]',native_tps_cases=[20,60],jump_excluded=True),
      checksum=dict(definition_offset=0x5AF,bytes=4,input_bytes=6457,formula='Unsigned sum of every intermediate byte,mod2^32',gate='World DWORD+A3C7 ==0 recomputes;nonzero retains previous checksum',raw_verification='Independent synthetic sum over full post-converter shadow6457 bytes;loader preservesMID;no claim of independent full6457-byte decoder'),
      extended_raw=dict(short_bytes=5449,long_bytes=5537,case='raw-imp-long-tail',mutation='Append80A5 bytes,editor DWORD1599=1,last DWORD159D=FFFFFFFF',result='All2199 captured definition bytes equal short raw-imp for exact DX fixture',warning='Preserve every extension byte inserializer;this result does not prove global unused or anothereditor/version behavior'),
      capture='YMM0..7 expose256bytes per replay;9fresh identical-input replays assemble2199 bytes. Not an atomic process dump. Opaque remainder captured,notsemanticallynamed.',
      excluded_experiments=[link('phase85-raw-driver-diagnostic.json'),link('ghidra/phase85-wide-readout-zmm.json')],
      limitations=['Exact supplied DKII-DX.exe;empty World catalogue reference arrays and null main graphics provider. No complete Spell/Room/resource linking,live game render,AI or actualsound.',
        'Raw cases use IDs1Imp,5Warlock,21King plus3in-memory Imp variants;allrawcases invoked sound provider twice with fixed11223344 handle,gfxcallback0times. These providers are explicit external boundaries.',
        'Only structural storage/presence/bounds and stated arithmetic proved. Priority/Jump gameplay remains unknown;otherindexed pointer leads notall classified.',
        '19full2199-byte images have field assertions and restricted full-image difference checks;there is no independent full-definition semantic oracle.',
        'Original constructors/converters/loaders/vtable retained. Setup memory must remain disjoint from all executable PE sections and original World vtable.'])
    (ROOT/'creature-definition-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    import build_catalogue_loading
    build_catalogue_loading.main()
    print('Phase85:',len(native['replays']),'native replays;',len(native['captures']),'complete images;5904exact patches;Priority/Jump gameplay unresolved.')
if __name__=='__main__':main()
