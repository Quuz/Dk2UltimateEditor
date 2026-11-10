"""Publish complete native EffectElement spawn/initialization with explicit fixtures."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from native_element_initialization import patch_raw
from emulate_element_initialization import cases,build,verify_node
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    manifest=load('phase81-primary-manifest.json');groups=[]
    for name in ['functions','blocks']:
        rows=[]
        for row in manifest[name]:
            p=ROOT/'ghidra'/row['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==row['address'] and v['length']==len(b)==row['length']
            assert b==pe_read(Path(row['exe']).read_bytes(),row['address'],row['length'])
            r={**row,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)}
            if name=='functions':r['assembly_sha256']=digest((ROOT/'ghidra'/row['assembly']).read_bytes())
            else:r['hex']=b.hex()
            rows.append(r)
        groups.append(rows)
    return tuple(groups)

def corpus():
    historical=load('phase21-validation.json');sources={}
    for row in historical['corpus']['file_details']:
        sources[str(DATA/'maps'/row['file'])]=row['sha256']
    for row in historical['scopes'][:2]:sources[row['path']]=row['sha256']
    files=[];patches=records=0;high_bits={hex(o):0 for o in [0x80,0x84,0x88,0x8C,0x90,0x94]};palette_nonzero=0
    for file,sha in sources.items():
        original=Path(file).read_bytes();assert digest(original)==sha,(file,'Source changed; preserve historical evidence and inspect drift')
        sections=fmt.parse(original);assert b''.join(s.pack() for s in sections)==original
        count=0
        for si,s in enumerate(sections):
            if s.signature!=250:continue
            assert all(len(r)==182 for r in s.records)
            count+=len(s.records)
            for raw in s.records:
                for o in [0x80,0x84,0x88,0x8C,0x90,0x94]:high_bits[hex(o)]+=int(int.from_bytes(raw[o:o+4],'little')>>16!=0)
                palette_nonzero+=int(raw[0xB1]!=0 or raw[0xB2]!=0)
            if not s.records:continue
            ri=next((i for i,r in enumerate(s.records) if int.from_bytes(r[0xA0:0xA2],'little')==294),0);raw=s.records[ri]
            for offset,width in [(0x80,4),(0x84,4),(0x88,4),(0x8C,4),(0x90,4),(0x94,4),(0xA2,2),(0xA4,2),(0xB1,1),(0xB2,1)]:
                for value in [0,1<<(8*width-1),(1<<(8*width))-1]:
                    rr=list(s.records);rr[ri]=patch_raw(raw,offset,value)
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                    packed=b''.join(clone.pack() if j==si else other.pack() for j,other in enumerate(sections))
                    expected=bytearray(original);at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+offset
                    expected[at:at+width]=value.to_bytes(width,'little');assert packed==expected
                    assert fmt.parse(packed)[si].records[ri]==rr[ri];patches+=1
        assert count;records+=count
        assert digest(Path(file).read_bytes())==sha
        files.append(dict(file=file,sha256=sha,records=count))
    return dict(files=files,unique_source_files=len(files),records=records,new_exact_container_patches=patches,
      raw_dword_fields_with_nonzero_high16_counts=high_bits,records_with_nonzero_palette_indices=palette_nonzero,
      scope='Prior-hash-checked 80 map catalogues plus standalone EffectElements and AutoSaveGlobals. One selected ID294(or first) record per matching section,10 physical fields with0/signbit/max. Full-container equality outside selected raw span; original files never written. Counts describe file records,not unique definitions.')

def snapshots():
    rows=[]
    for c in cases():
        n='ghidra/phase81-element-'+c['name']+'.bin';b=(ROOT/n).read_bytes();_,expected=build(c,0);verify_node(c,b,expected)
        rows.append(dict(case=c['name'],allocated=expected['success'],file=n,bytes=len(b),sha256=digest(b),expected_plan=expected['plan']))
    return rows

def field_paths():
    joined=load('scalar-storage-paths.json')['families']['EffectElements']['joined_paths'];rows=[]
    for offset,name,interpretation in [(0x80,'MinSpeedXY','s16(low16(raw DWORD));inclusive upper selection'),
      (0x84,'MaxSpeedXY','s16(low16(raw DWORD));inclusive upper selection'),(0x88,'MinSpeedZ','s16(low16(raw DWORD));inclusive upper selection'),
      (0x8C,'MaxSpeedZ','s16(low16(raw DWORD));inclusive upper selection'),(0x90,'MinScale','u16(low16(raw DWORD));exclusive upper selection'),
      (0x94,'MaxScale','u16(low16(raw DWORD));exclusive upper selection'),(0xA2,'MinHp','u16 raw;inclusive upper selection into lifetimeDWORD109'),
      (0xA4,'MaxHp','u16 raw;inclusive upper selection into lifetimeDWORD109'),(0xB1,'RandomColorIndex','byte storage;global meaning unresolved'),
      (0xB2,'TableColorIndex','byte storage;global meaning unresolved')]:
        row=next(r for r in joined if int(r['raw_offset'],16)==offset)
        rows.append(dict(name=name,raw_offset=offset,raw_bytes=row['raw_bytes'],intermediate_offset=int(row['intermediate_offset'],16),
          definition_offset=int(row['runtime_offset'],16),definition_bytes=row['runtime_bytes'],interpretation=interpretation,
          lineage_source='scalar-storage-paths.json.families.EffectElements.joined_paths;existing raw converter and rechecked static4366A0. New full initialization starts with prepared79-byte definition,not raw loader.'))
    return rows

def main():
    functions,blocks=primary();replays=load('phase81-element-initialization-emulations.json')['replays'];assert len(replays)==78
    nodes=snapshots();assert len(nodes)==26 and sum(r['allocated'] for r in nodes)==23
    base=(ROOT/'ghidra/phase81-element-base.bin').read_bytes()
    assert all((ROOT/'ghidra'/('phase81-element-'+name+'.bin')).read_bytes()==base for name in ['palette-random','palette-table','palette-both','id-high-slot-bits','no-cull-flag'])
    s=dict(phase=81,date='2026-10-28',family='EffectElements',record_bytes=182,definition_bytes=79,instance_bytes=287,
      executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),
      primary_program_bytes=functions,primary_vtable_data=blocks,native_replays=replays,raw_snapshots=nodes,corpus=corpus(),fields=field_paths(),
      counts=dict(logical_scenarios=26,successful_allocation_scenarios=23,failed_allocation_scenarios=3,native_emulation_invocations=78,
        successful_allocation_replays=69,executed_path_function_spans=20,static_only_function_spans=2,file_backed_vtable_ranges=3,original_code_or_vtable_replacements=0),
      entry=dict(spawn='0x4352E0',constructor='0x435E70',base_constructor='0x4468E0',initializer='0x435F20',native_pool='0x554B30',
        path='Actual CBridge exists/get and visibility forwarder,bounds,pool allocation,constructors,model-ID registration,client-map insertion,velocity/lifetime/resource/scale/color setup and parent-child insertion run to real return.',
        request_bytes=287,aligned_payload=288,header_bytes=12,accounted_allocation=300,success='EAX1 and out pointer;parent childhead113 set;child113 copies parent155 without new meaning assigned.',
        failure='Checked pool296,no free model IDs,outside map: EAX0,out pointer unchanged,pool/table/parent/map/node and PRNG unchanged. Visibility occurs before bounds;obstruction/tile-touch before allocation guards.'),
      class_provenance=dict(global6DA458='CBridge,not CWorld. Native43AAD0 stores this pointer;original vtable66CC94.',
        global6DA45C='Connected engine interface from *( *(CBridge+4)+14 ),assigned by native4468E0.',
        bridge_slots={'0x80':'4412C0 forwards to connected engine v80','0x158':'440A70 EffectElement exists','0x15C':'4409A0 EffectElement getter'},
        engine_provider_slots=['0x80 visibility->1','0x20C obstruction->0','0x1D4 tile-touch->0','0x204 floor->0'],
        rejected_hypotheses=['446970 was mid-instruction,correct constructor4468E0.','CWorld.v15C50D9C0 is an unrelated Effects-exists method.','CBridge table reads at1D4/204/20C exceeded this interface;calls belong to connected engine.'],
        warning='Rejected type/address hypotheses are analysis corrections,not evidence of editor serialization defects.'),
      ranges=dict(speed=dict(definition_words=['31','33','35','37'],signed=True,formula='If max==min:sample=min without draw;otherwise sample=u32(min+rand(u32(max-min+1))). Ordered valid ranges include both endpoints.',
        xy_destination='Original signed64-bit coefficient multiplication then arithmetic shift16;fixture coefficients65536 give node8=sample,nodeC=-sample.',z_destination='node10=sample;added by native434E50.'),
        lifetime=dict(definition_words=['23','25'],signed=False,formula='If max==min:no draw;otherwise sample=u32(min+rand(u32(max-min+1))). Ordered valid ranges include both endpoints.',destination='DWORD109;alpha threshold DWORD10D'),
        scale=dict(definition_words=['2D','2F'],signed=False,formula='If resource flags&1 and max!=min:sample=u32(min+rand(u32(max-min)));equal endpoints use min without draw. Ordered unequal ranges exclude max.',destination='DWORD2A and2E',render_gate='resourceflags&1==0 returns before scale/color and final parent renderer-flags copy.'),
        malformed='Selected reversed intervals execute native unsigned-bound behavior and produce large values;there is no sorting/clamping on these paths. This does not establish safety for all malformed inputs or zero RNG bounds.',
        serialization='Keep full raw DWORDs for speed/scale;definition narrowing does not permit truncating/replacing high bits in the file.'),
      randomness=dict(entry='0x5558D0',state='DWORD6D6450',formula='state=u32(state*1664525+1013904223);result=ror32(state,13)%u32(bound)',
        sequence='Three ctor angle draws bound2048,optionalXY,optionalZ,unconditional direction2048,optionalHP,then optionalScale when render enabled. Parent spread0 in fixtures;other branches can add draws.',
        verification='Expected final native state compared in every replay;per-case draw trace stored with snapshot. This is deterministic runtime selection,not a file checksum.'),
      alpha_threshold=dict(raw_percentage='BYTEB3 -> definition3D',formula='u32(H - trunc_toward_zero(s32(low32(H*percentage))/100))',
        scope='New reversed-HP fixture exercises low32 product wrap. For ordered u16 HP and byte percentage this agrees with prior small-value rule. Percentage100 yields0 for ordinary H100.'),
      palette=dict(raw_offsets=[177,178],definition_offsets=[77,78],tested_pairs=[[0,0],[255,0],[0,255],[255,255]],
        result='All287 initialized object bytes and final RNG state equal across four pairs in the complete tested creation path. Definitions retained distinct bytes and were checked read-only. No new palette meaning assigned.',
        global_nonuse_proved=False,remaining='No live renderer,texture/palette asset path,nonzero parent spread,player-color branch or other binary is covered by this equality.'),
      capture=dict(method='Original code and original vtables remain intact. A synthetic post-return observer compares prepared memory and loads128bytes into XMM0..7.',
        snapshot='287 bytes assembled from3 independent fresh identical-input replays,not an atomic live-process dump.',
        checks='Pool,free/used sentinels,parent,map/model table,PRNG,model count,FS,inputs and provider counts compared each replay. Full new node retained;only explicit assertions assign meaning.'),
      limitations=['Prepared79-byte definition;raw converter and asset loader are lineage evidence,not newly executed end to end.',
        'Visibility/obstruction/touch/floor are explicit connected-engine providers;uniform coefficient data isolates selection and does not verify real angle-table population or visual trajectories.',
        'Parent spread0,empty childhead,one free model slot,empty runtime map,resource types0/1 and selected flags. No growth/shrink,player RGB lookup,late tick,deletion,collision,renderer or framebuffer.',
        'Client-map registration uses2-byte spatial cells;this does not reinterpret the raw4-byte AutoSaveMap.kld format.',
        'Some287-byte instance bytes remain opaque. Raw palette indices and other families unresolved;original full goal remains incomplete.'])
    (ROOT/'element-initialization-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('element-color-semantics.json');old['phase81_complete_initialization']=s
    (ROOT/'element-color-semantics.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(dict(phase=81,counts=s['counts'],corpus={k:v for k,v in s['corpus'].items() if k!='files'})))

if __name__=='__main__':main()
