"""Publish native Keeper array admission/overlay/accessors and bounded new roots."""
from pathlib import Path
import json,hashlib,struct
from collections import Counter
import dk2_formats as fmt
from build_effect_semantics import pe_read
from native_keeper_pipeline import patch_raw
import emulate_keeper_array as native
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase83-primary-manifest.json');groups=[]
    for key in ['functions','blocks']:
        rows=[]
        for r in spec[key]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            row={**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)}
            if key=='functions':row['assembly_sha256']=digest((ROOT/'ghidra'/r['assembly']).read_bytes())
            else:row['hex']=b.hex()
            rows.append(row)
        groups.append(rows)
    return tuple(groups)

def corpus():
    rows=[];patches=permutations=records=0;unordered=[];duplicates=[];zero_ids=[];order_counts=Counter();duplicate_orders=[]
    for old in load('keeper-lookup-review.json')['corpus']['files']:
        p=Path(old['file']);original=p.read_bytes();assert digest(original)==old['sha256'];parts=fmt.parse(original);assert b''.join(s.pack() for s in parts)==original;local=0
        for si,s in enumerate(parts):
            if s.signature!=150:continue
            local+=len(s.records);ids=[r[0xEE] for r in s.records]
            if ids!=sorted(ids):unordered.append(str(p))
            if len(ids)!=len(set(ids)):duplicates.append(str(p))
            if 0 in ids:zero_ids.append(str(p))
            order_counts.update(r[0xDB] for r in s.records)
            for value,n in Counter(r[0xDB] for r in s.records).items():
                if n>1:duplicate_orders.append(dict(file=str(p),order=value,ids=[r[0xEE] for r in s.records if r[0xDB]==value],
                  names=[r[:32].split(b'\0')[0].decode('latin1') for r in s.records if r[0xDB]==value]))
            if not s.records:continue
            raw=s.records[0]
            for value in [0,128,255]:
                rr=list(s.records);rr[0]=patch_raw(raw,0xDB,value);clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                packed=b''.join(clone.pack() if j==si else other.pack() for j,other in enumerate(parts));expected=bytearray(original);at=s.offset+len(s.header)+0xDB;expected[at]=value
                assert packed==expected and fmt.parse(packed)[si].records[0]==rr[0];patches+=1
            rr=list(reversed(s.records));clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
            packed=b''.join(clone.pack() if j==si else other.pack() for j,other in enumerate(parts));expected=bytearray(original);at=s.offset+len(s.header)
            expected[at:at+sum(map(len,s.records))]=b''.join(rr);assert packed==expected and fmt.parse(packed)[si].records==rr;permutations+=1
        records+=local;assert digest(p.read_bytes())==old['sha256'];rows.append(dict(file=str(p),sha256=old['sha256'],records=local))
    return dict(files=rows,file_count=len(rows),records=records,new_order_byte_exact_patches=patches,whole_container_record_permutations=permutations,
      raw_id_unsorted_files=unordered,raw_duplicate_id_files=duplicates,raw_id_zero_files=zero_ids,order_byte_counts={str(k):v for k,v in sorted(order_counts.items())},duplicate_order_examples=duplicate_orders,
      scope='82 prior-hash source files;new rawDB0/128/255 exact one-byte changes and reversed whole-record permutations in memory only. Record bytes/unknowns/header/order are preserved except explicitly chosen mutation. Runtime insertion order is separate from file serialization.')

def outcomes():
    result=[]
    for c in native.cases():
        _,e=native.build(c);result.append(dict(case=c['name'],kind=c['kind'],runtime_ids=e['runtime_ids'],count=e['count'],aggregate=e['aggregate'],steps=e['steps'],probes=e['probes'],output_hex=e['output_expected_hex']))
    return result

def main():
    functions,blocks=primary();cases=load('phase83-keeper-array-emulations.json')['cases'];assert len(cases)==41
    rows=outcomes();root_calls=sum(len(r['steps']) for r in rows);low_cases=sum(any(s['null_write'] for s in r['steps']) for r in rows)
    paths=load('scalar-storage-paths.json')['families']['KeeperSpells']['joined_paths'];order=next(r for r in paths if int(r['raw_offset'],16)==0xDB)
    assert order['runtime_offset']=='0x6c'
    classifications={0x50DC80:'First physical ID',0x50DCA0:'Next physical ID with wrap',0x50DD00:'Previous physical ID with wrap',0x50DD60:'Established by-ID pointer getter',
      0x50DDD0:'FullDWORD-index to physical ID',0x50DE00:'FullDWORD order argument compared to unsignedBYTE definition6C;returns first matching ID',0x50DE50:'By-ID existence',
      0x511680:'Zeros catalogue pointer/capacity/count/aggregate fields;symbol name clearChecksums alone is misleading',
      0x511760:'Array destruction and zeroing',0x51E2C0:'Preallocation and constructors',0x51FCD0:'Insertion/overlay callback',
      0x55C020:'Address-only embedded resource-map initialization,not a Keeper definition read',0x55C700:'Address-only resource-path/map access,not a Keeper definition read',0x55C7B0:'Address-only embedded resource-map destruction,not a Keeper definition read'}
    search=load('phase83-array-root-candidates.json');dx=search['profiles'][0]
    typed=[dict(address=h['address'],instruction=h['instruction'],function=h['function_hint']['address'],classification=classifications[h['function_hint']['address']]) for h in dx['hits']]
    s=dict(phase=83,date='2026-10-28',family='KeeperSpells',raw_record_bytes=406,intermediate_bytes=526,runtime_definition_bytes=122,
      executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),primary_program_bytes=functions,primary_vtable_data=blocks,
      native_emulations=cases,outcomes=rows,corpus=corpus(),
      counts=dict(logical_scenarios=41,load_scenarios=29,accessor_scenarios=12,native_emulation_invocations=41,load_callback_invocations=root_calls,
        raw_init_converter_pairs=33,partial_intermediate_inputs=5,controlled_null_target_scenarios=low_cases,code_spans_attested=20,executed_path_spans=13,static_only_spans=7,original_code_or_vtable_replacements=0),
      native_chain=dict(entry='Original4F7C70 scratch constructor;returns to synthetic call-sequence driver,not patched entry bytes.',
        raw='Native56B990 clears526-byte intermediate;56BAB0 converts406-byte raw;51FCD0 inserts/updates with originalWorld.v110=50DD60 and actual4F7CE0 scalar loader.',
        constructor='4F7C70/504D60 clear definition0..35 and3A..79;DWORD36 checksum retains previous buffer bits. This is initializer behavior,not a default checksum guarantee.',
        providers='Prepared World/preallocated array and null graphical provider. Sound v1C external fixed11223344 with call count. No file IO,heap allocator,World ctor,real resources or sound subsystem.'),
      array_fields=dict(pointer='World1604',capacity='World1608',count='World160C',checksum_aggregate='World1610',overlay_mode='WorldDWORD A3C7 !=0',id='definitionBYTE75 / rawBYTEEE'),
      insertion=dict(entry='0x51FCD0',capacity='Unsigned count<capacity required. Otherwise returns1 without loader,mutation or insertion;not an allocation attempt.',
        ordering='Scan backwards while previousID>=newID;move complete122-byte definitions one position right;load into resulting slot;incrementcount. Incoming duplicate is placed before equal rows,not merged.',
        partial='Load overlays the bytes already present at insertion slot. No new default constructor call here. An absent presence retains that slot byte,including C8;ordinary complete raw converter supplies all30 presence flags.',
        duplicates='Selection by existing50DD60 depends on numeric ID starting index min(id-1,count-1). Duplicate3 scenario selects earlier insertedC8FE0C0000 though newerduplicateFE0C0001 is physically before it;duplicate1 selectsnewer. No universal last-wins rule.',
        zero='Normal insertion permits physicalID0;by-ID getter still returnsNULL for0. This does not authorize renumbering original file records.'),
      overlay=dict(mode='WorldA3C7 nonzero:find byID and invoke loader in place;capacity/count/aggregate unchanged.',
        missing='No NULL test after getter before4F7CE0. In3 controlled cases,loader wrote expected definition fields into low-address guard,including checksum36/C8 3A. Native callback stillreturned1.',
        scope='Emulator low-address writes are diagnostics. No live process crash was triggered;reachability of mode/checksum combinations through normal game/editor inputs not established.'),
      checksums=dict(record='Gate6FCDCF==0:sum all526 unsigned intermediate bytes into definition36;otherwise retain previous DWORD.',
        aggregate='Normal insertion adds resulting definition36 modulo2^32. Overlay branch returns before aggregate update. This behavior had static evidence in catalogue-checksums.json and is now dynamically composed.',
        distinction='Runtime sum is separate from raw file headerN/S/D and any arbitrary CRC/hash guess;preserve all32 C8 bits.'),
      accessors=dict(native_world_vtable='0x66E3EC',slots={'FC':'50DC80 first','100':'50DCA0 next','104':'50DD00 previous','108':'50DDD0 index','10C':'50DE50 exists','110':'50DD60 get','120':'50DE00 order-byte-to-ID'},
        byte_return='First/next/previous/index/order functions return meaningful AL only;upper EAX mayretain pointer/arithmetic bits. Observer masks low8 before comparisons.',
        order_lineage=order,order_lookup='DefinitionBYTE6C is zero-extended and compared to fullDWORD argument. First matching physical runtime row returns itsID. Highargumentbits are not truncated,unlike next/previous ID arguments.',
        scope='Field baseline name OrderInEditor retained;native evidence proves byte→ID lookup,not a newly invented gameplay priority.'),
      C8=dict(raw_offset=0xC8,definition_offset=0x3A,purpose='unresolved',new_scope='All rawC8 boundary bits copied through actual fullraw→array path. Scalar-loader assignment and122-byte shifts/partial retention are storage behavior,not a new gameplay consumer.',global_nonuse_proved=False),
      source_order_example=dict(source=link('phase83-order-duplicate-source.json'),
        result='Actual unmodified406-byte RaiseDeadID9 andCreateGoldID13 records both order9 were converted/inserted thenqueried natively. v120(9) returned9;v120(10) returned0. Other source-record fields are unmodified.',
        scope='Two exact source rows,not a full streamed map or proof which visible UI/game action uses this call. Duplicate order is not duplicateID and is not automatically repaired.'),
      array_root_search=dict(source=link('phase83-array-root-candidates.json'),dx_decoded=dx['decoded'],dx_hits=typed,
        old_secondary=dict(decoded=search['profiles'][1]['decoded'],untyped_candidates=len(search['profiles'][1]['hits']),scope='Local exact-old-PE linear candidates only. No old native emulation/new Ghidra attestation or type conclusion in phase83.'),
        limits='Only literal displacement1604 linear decode roots. Does not cover arbitrary offset aliases,dynamic virtual calls or every consumer. No global C8 nonuse inferred.',
        environment='Headless currently opens DX only;list_project_files/open_program require GUI mode. Failed old-program probes retained;server not restarted and DX native analysis proceeded.'),
      verification=dict(full_state='Every invocation compares completeWorld,fullcapacity*122array+256guard bytes,allraw/mid buffers,controlledlowmemory,globals,outputs,sound count and scratch ctor.',
        readout='One128-byte XMM readout of array start per case,plus equality comparisons of entire prepared array. No claim that full arrays are raw128-page snapshots.',
        source_order='New exactDBpatches and explicit full-record permutations preserve all other bytes. Runtime sort never changes the source files.'),
      limitations=['Exact DX dynamic scope;old candidate list stays untyped. No full streaming parser,World array allocator/ctor or renderer/resource-success branches.',
        'Partial526-byte intermediates and overlay-mode combinations are explicit inputs;not assumed reachable from every ordinary406-byte raw record or normal game flow.',
        'C8 remains unnamed and all other substantive unknowns remain open;original full format goal is incomplete.'])
    assert root_calls==38 and low_cases==3
    (ROOT/'keeper-array-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('keeper-pipeline-semantics.json');old['phase83_runtime_array']=s
    (ROOT/'keeper-pipeline-semantics.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(dict(phase=83,counts=s['counts'],corpus={k:v for k,v in s['corpus'].items() if k!='files'})))
if __name__=='__main__':main()
