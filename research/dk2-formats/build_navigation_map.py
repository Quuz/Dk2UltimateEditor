"""Publish Terrain flags→original nav producer→native traversal composition."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read
import dk2_formats as fmt
import emulate_navigation_map as native
import native_navigation_map as api
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,items in load('phase99-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text(encoding='utf8'));b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length']
            assert b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    exe=Path(load('navigation-step-semantics.json')['profile']['executable']).read_bytes()
    assert struct.unpack('<I',pe_read(exe,0x6a11d0,4))[0]==0x4c8bd0
    for slot,target in [(0x64,0x50cf80),(0x70,0x508dd0),(0x1d8,0x508fc0),(0x264,0x508c60)]:assert struct.unpack('<I',pe_read(exe,0x66e3ec+slot,4))[0]==target
    return out

def results():
    manifest=load('phase99-navigation-map-emulations.json');rows=manifest['replays'];by={c['name']:c for c in native.cases()};out=[]
    assert len(rows)==len(by)==20 and manifest['scope']==native.__doc__
    for row in rows:
        path=ROOT/'ghidra'/row['file'];v=json.loads(path.read_text(encoding='utf8'));c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(path.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['xy'][0]).items())
        assert row['file'].endswith('-'+digest(json.dumps(args,sort_keys=True).encode())[:10]+'.json')
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out

def corpus():
    report=ROOT/'phase99-terrain-flags-api-validation.json';dependencies=[link(n) for n in ['native_navigation_map.py','dk2_formats.py']]
    sources=load('terrain-lighting-semantics.json')['corpus']['files']
    if report.exists():
        v=load(report.name);assert v['dependencies']==dependencies
        assert [(r['file'],r['sha256']) for r in v['files']]==[(r['file'],r['sha256']) for r in sources]
        for r in v['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
        return v
    files=[];records=patches=0;aggregate=hashlib.sha256();counts={}
    for row in sources:
        p=Path(row['file']);data=p.read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data;file_records=0
        for si,s in enumerate(parts):
            if s.signature!=0x6e:continue
            for ri,raw in enumerate(s.records):
                before=struct.unpack_from('<I',raw,0x190)[0];category=api.terrain_navigation_byte(before);counts[str(category)]=counts.get(str(category),0)+1
                for mask in [1,0x80,0x100]:
                    for enabled in [False,True]:
                        changed=api.patch_terrain_navigation_flags(raw,mask,enabled);flags=(before|mask) if enabled else (before&~mask)
                        assert changed[:0x190]==raw[:0x190] and changed[0x194:]==raw[0x194:] and struct.unpack_from('<I',changed,0x190)[0]==flags
                        rr=list(s.records);rr[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                        packed=b''.join(clone.pack() if i==si else a.pack() for i,a in enumerate(parts));expected=bytearray(data)
                        struct.pack_into('<I',expected,s.offset+len(s.header)+ri*len(raw)+0x190,flags)
                        assert packed==expected and fmt.parse(packed)[si].records==rr
                        aggregate.update(hashlib.sha256(packed).digest());patches+=1
                records+=1;file_records+=1
        assert file_records;files.append(dict(file=str(p),sha256=row['sha256'],records=file_records))
    assert len(files)==82 and records==3306 and patches==19836
    calls=[lambda:api.patch_terrain_navigation_flags(bytes(547),1,True),lambda:api.patch_terrain_navigation_flags(bytes(552),2,True),lambda:api.patch_terrain_navigation_flags(bytes(552),1,1),lambda:api.patch_terrain_navigation_flags(bytes(552),0,True),lambda:api.terrain_navigation_byte(-1),lambda:api.terrain_navigation_byte(True)]
    refusals=0
    for call in calls:
        try:call()
        except ValueError:refusals+=1
        else:raise AssertionError('Missing API refusal')
    v=dict(dependencies=dependencies,files=files,file_count=len(files),records=records,api_exact_container_patches=patches,refusals=refusals,
      source_records_by_navigation_category=counts,ordered_patch_digest=aggregate.hexdigest(),scope='Six singlebit enable/disable patches per rawTerrain record; all other31 bits,bytes,header,sections,order preserved. No source writes; API checks,not native map or gameplay execution.')
    report.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');return v

def examples():
    d=load('decoded/Terrain.kwd.json');data=Path(d['source']).read_bytes();assert digest(data)==d['sha256'];rows=[]
    for s in fmt.parse(data):
        if s.signature!=0x6e:continue
        for i,raw in enumerate(s.records):
            ident=raw[0x1d6]
            if ident in [1,4,5,17,26,36]:
                flags=struct.unpack_from('<I',raw,0x190)[0]
                rows.append(dict(terrain_id=ident,name=fmt.string(raw[:32]),record_offset=s.offset+len(s.header)+i*len(raw),raw_flags=flags,navigation_byte=api.terrain_navigation_byte(flags)))
    assert {r['terrain_id'] for r in rows}>={1,4,5}
    return dict(file=d['source'],sha256=d['sha256'],records=rows,scope='Current source flags only; native rawcases are deliberately minimal records,not unmodified live assets. Bridge overlays and rawMap flags are separate.')

def main():
    p=primary();rows,out=results();raw=corpus();current=examples();prior=ROOT/'phase99-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==77;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=99,date='2026-10-28',profile=load('navigation-step-semantics.json')['profile'],primary=p,native_replays=rows,outcomes=out,
      corpus=raw,current_source=current,api_helpers=[link(n) for n in ['native_navigation_map.py','emulate_navigation_map.py']],
      physical_field=dict(family='Terrain',raw_dword=0x190,intermediate_dword=0x248,presence_dword=0xb0,runtime_dword=0x17b,
        flags=[dict(mask=1,baseline_name='SOLID',navigation_byte=8,priority=1),dict(mask=0x80,baseline_name='WATER',navigation_byte=1,priority=2),dict(mask=0x100,baseline_name='LAVA',navigation_byte=3,priority=3)],
        selection='If flags&1:8;else if flags&80h:1;else if flags&100h:3;else0. A complete byte is assigned,not ORed with previous navigation ownership/door bits.'),
      original_chain=dict(raw='Eight cases execute56EB60 initializer→56ED20 converter→whole4F66E0 loader on minimal552-byte rawrecords,gate1,zero resource descriptors.',
        cache='All20 call whole455D40 with actual World enumeration50CEB0,count508DF0,getter508DD0 and original Worldvtable. Prepared tablecapacity8,loadedcount2,IDs1and7;cache built natively.',
        placement='All20 call whole44FC40(this=World+9DA,x6,y6,Terrain7,owner0,flags0,flags0);actual50CF80 checks ID,455B90 writes tileTerrain,4559D0 handlesowner,4D1970 writes actual NAV.',
        loading_mode='WorldBYTE7239 bit40 explicitly set:44FC40 selects4D1970. Normalmode selects4D1930→4CE770 graph rebuild,statically reconstructed but not executed.',
        cell_side_effects='Target runtime16-byte cell getsTerrain7 andWORD6=3456h fromdefinition1AB;roomWORD2 low12 cleared,byte4/5 cleared. CMapDWORD1C/World9F6 becomes real Sessiontick100. Wholemap/world buffers compared.',
        traversal='Then inherited original raw224/21,flag/getter,vslot cap,4D27D0,optional48EBF0,whole4D5A40/4D4760 andXY/vector readback execute. Actual callback reads navbyte just produced fromTerrain.',
        priority='flags180h chooses category1. Lava-only creature permission does not permit it;water permission does. Any tested SOLID combination chooses8 and stops.',
        limits='No new actorXY application in these20 cases;controllerXY is checked. Priorphase98 four application cases remain separate. No fullrawKLDparser→placement composition.'),
      scope=dict(source_actor=load('navigation-step-semantics.json')['scope']['source_actor'],
        fixture='Prepared32x32 world;oldTerrain1 ordinary,allmapcellsTerrain1,unowned/no rooms. CurrentplayerFFFF avoidsmatchingowner0 visual branch; loadingmode skipsdynamicgraph/event paths. Native direction table retained.',
        forces='Finite empty forceprovider array retained fromphase98;no original game code/vtable/collisionpointer patched.',
        comparisons='WholeWorld,map,actors,definition arrays+64guard,cache1024,raw552+64guard,intermediate736+64guard andall inherited prepared buffers;no new completeactor images.',
        diagnostics=['One failed functioncreation at guessed interior5090B0 retained;correct Worldvtable entry5090C0 used afterward.',
          'Initial driver failed to preserve inherited callbackEAX;second diagnostic then exposed missing expectedWorld9F6 tick100. Both measuring-fixture issues fixed;old records excluded from20 accepted cases.']),
      counts=dict(native_cases=len(out),native_replays=len(rows),raw_terrain_loader_compositions=sum(r['case']['raw'] for r in out),
        whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),terrain_source_files=raw['file_count'],terrain_records=raw['records'],
        new_raw_API_patches=raw['api_exact_container_patches'],new_actor_images=0,original_game_function_or_vtable_patches=0),
      limitations=['Exact supplied DX only;not other-version or live-session proof.',
        'Loading-mode CMap path,unowned/no rooms,no matching localowner or terrainlight;normal4D1930 graph rebuild remains static.',
        'RawTerrain initializer/converter/loader composed in8 cases;no sourcefile parser or complete maploading replay.',
        'Creature permissions use prepared runtime definitions asphase98;new fullrawCreatureloader→terrain→movement not composed.',
        'Bridge placement,Door overlays,water/lava damage,fullA*,forces,physics andrenderer not executed.',
        'ControllerXY checked;new actualActorXY application not claimed.',
        'No Priority/Jump gameplay link established by this navigation work;their known storage and unresolved semantics remain.',
        'Objects34C,Traps204,Doors236,Terrain198/224,scalar98/113/114 andremaining15-family unknowns keepgoalopen.'])
    (ROOT/'navigation-map-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']));print('CurrentTerrain',json.dumps(current['records']))

if __name__=='__main__':main()
