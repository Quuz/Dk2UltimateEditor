"""Publish bounded original movement composition; keep untested steering explicit."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read
import dk2_formats as fmt
import emulate_navigation_step as native
import native_navigation_step as api
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,items in load('phase98-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text(encoding='utf8'));b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length']
            assert b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    exe=Path(load('possession-health-semantics.json')['profile']['executable']).read_bytes()
    assert struct.unpack('<I',pe_read(exe,0x6a11d0,4))[0]==0x4c8bd0
    return out

def results():
    manifest=load('phase98-navigation-step-emulations.json');rows=manifest['replays'];by={c['name']:c for c in native.cases()};out=[]
    assert len(rows)==len(by)==32 and manifest['scope']==native.__doc__
    for row in rows:
        path=ROOT/'ghidra'/row['file'];v=json.loads(path.read_text(encoding='utf8'));c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(path.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['xy'][0]).items())
        assert row['file'].endswith('-'+digest(json.dumps(args,sort_keys=True).encode())[:10]+'.json')
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out

def corpus():
    report=ROOT/'phase98-traversal-api-validation.json'
    dependencies=[link(n) for n in ['native_navigation_step.py','native_creature_extension.py','dk2_formats.py']]
    sources=load('creature-lookup-review.json')['corpus']['files']
    if report.exists():
        cached=load(report.name);assert cached['dependencies']==dependencies
        assert [(r['file'],r['sha256']) for r in cached['files']]==[(r['file'],r['sha256']) for r in sources]
        for r in cached['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
        return cached
    files=[];records=patches=0;aggregate=hashlib.sha256()
    for row in sources:
        p=Path(row['file']);data=p.read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data)
        assert b''.join(s.pack() for s in parts)==data
        file_records=0
        for si,s in enumerate(parts):
            if s.signature!=0xaa:continue
            for ri,raw in enumerate(s.records):
                assert api.patch_traversal_flags(raw)==raw
                before=struct.unpack_from('<I',raw,0xefc)[0]
                for water,lava in [(False,False),(False,True),(True,False),(True,True)]:
                    changed=api.patch_traversal_flags(raw,water=water,lava=lava)
                    flags=(before&~0x30)|(0x10 if water else 0)|(0x20 if lava else 0)
                    assert changed[:0xefc]==raw[:0xefc] and changed[0xf00:]==raw[0xf00:]
                    assert struct.unpack_from('<I',changed,0xefc)[0]==flags
                    rr=list(s.records);rr[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                    packed=b''.join(clone.pack() if i==si else a.pack() for i,a in enumerate(parts))
                    expected=bytearray(data);struct.pack_into('<I',expected,s.offset+len(s.header)+ri*len(raw)+0xefc,flags)
                    assert packed==expected and fmt.parse(packed)[si].records==rr
                    aggregate.update(hashlib.sha256(packed).digest());patches+=1
                records+=1;file_records+=1
        assert file_records;files.append(dict(file=str(p),sha256=row['sha256'],records=file_records))
    assert len(files)==82 and records==3734 and patches==14936
    count=0
    for call in [lambda:api.patch_traversal_flags(bytes(5448)),lambda:api.patch_traversal_flags('x'),lambda:api.patch_traversal_flags(bytes(5449),water=1),lambda:api.patch_traversal_flags(bytes(5537),lava='true')]:
        try:call()
        except ValueError:count+=1
        else:raise AssertionError('Missing API refusal')
    out=dict(dependencies=dependencies,files=files,file_count=len(files),records=records,api_exact_container_patches=patches,refusals=count,
      ordered_patch_digest=aggregate.hexdigest(),scope='Four flag combinations per record; all other30 bits,bytes,headers,sections,order and record extension retained. Sources never written; these are API checks,not native executions.')
    report.write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8');return out

def main():
    p=primary();rows,out=results();raw=corpus();prior=ROOT/'phase98-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==76
        prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=98,date='2026-10-28',profile=load('possession-health-semantics.json')['profile'],primary=p,native_replays=rows,outcomes=out,corpus=raw,
      current_speed_source=load('navigation-speed-semantics.json')['current_source'],
      api_helpers=[link(n) for n in ['native_navigation_step.py','emulate_navigation_step.py']],
      raw_traversal_flags=dict(raw_dword=0xefc,intermediate_dword=0x12ec,presence_dword=0x25c,runtime_dword=0x686,
        masks={'0x10':'CAN_WALK_ON_WATER (baseline name); native preflight permits entry into navigation category1','0x20':'CAN_WALK_ON_LAVA (baseline name); native wrapper permits entry into category3'},
        binding='Original568E30 ORs rawEFC into intermediate12EC and sets25C;492320 copies12EC to definition686 when25C!=0. Static original instructions,not a new fullrawloader replay. Initialized intermediate flags are required for exact OR equivalence.',
        serialization='Patch only chosen mask; retain all other30 flags and physical record bytes. This is the main flags DWORD,not Flags2 at1545.'),
      callbacks=dict(initializer='Whole original4D27D0 stores4D16B0 at6ED408 andWorld context at6ED418; executed in all32 cases.',
        map_query='Original4D16B0 uses Worldvslot1FC→509090 unsigned bounds;returns8 outside;otherwise reads one runtime navigation byte throughWorld9F2. Navigation buffer is not the raw4-byte KLD map cell.',
        collision_pointer='Original PE DWORD6A11D0 remains4C8BD0; never patched.',
        predicate='4C8BD0 blocks target bit8; low2 category3 uses6EC9DC,category1 uses6EC9E4; ownership highnibble and bit4 use owner/actor globals. The model preserves original decision order.',
        globals={'6EC9D0':'ownerBYTE10FC','6EC9D4':'actor3E6 bit17','6EC9D8':'actor3E6 bit18','6EC9DC':'definition686 bit5 in4D5A40','6EC9E0':'actor3E6 bit19','6EC9E4':'NOT written by4D5A40; native48EBF0 preflight uses definition686 bit4 ORvslot88'},
        preflight='Two original48EBF0→4C8BD0 cases toggle definition686/10; target category1 blocks without and passes with flag. No claim that all possible callers initialize this global identically.'),
      native_chain=dict(entry='Existing raw224/21→actual possession/neighbour producer→48ABB0→actual vslots8C/90',
        navigation='Original4D27D0→optional48EBF0→4D59A0 seeds position/vector→whole4D5A40→whole4D4760→4D5AC0/4D5AF0 read back.',
        units='Actor inputs/output Q12:4096 units per cell;controller position/vector Q16,seeding shiftsleft4,readback arithmeticshiftsright4.',
        speed='P200/B1000/possessed: getter3000,setter3000,max_speed48000; requestedvelocity4096 producesQ12 Xdelta3000 in the open fixture. Normaldelta1000;secondary1500;B1366 capped4095.',
        collision='In diagonal blocked-target fixture originalstep accepts X-only before Y-only: (2121,2121)→(2121,0). Blocked axes/bounds stop. Only these bounded neutral-force alternatives are projected.',
        zero_cap='4D5A40 returns before4D4760 when max_speed0; seeded coordinates andrequested velocity are retained,with no zero/zero vector division.',
        application='Four cases additionally execute original4B35C0 bounds/nav8 acceptance and,when accepted,4B5560→4B5020 changes real actorXY and map-cell lists. Successful open case moves tag100 fromold cell(5,6) tonew(6,6). Z remains8192 in this fixture.',
        preflight_cases=2,position_application_cases=4),
      scope=dict(source_actor=load('possession-health-semantics.json')['scope']['source_actor'],
        fixture='Prepared phase90 actor/map/World andphase95 sourceactor;Worldbounds32x32,existing real vtables,neutral controller anddefined navigation bytes.',
        force_provider_array='6ECAE0/E4 explicitly finiteempty (0/0),so original force loop has zeroiterations;4D56E0 registration and real forces not executed. No original force method patched.',
        comparisons='Whole1343-byte actor+256guard,sourceactor,World/map/owner/vars,definition/rawrecord,callbacks/globals/forces andoutput buffers checked;optionalposition12+64guard.',
        original_game_code_vtable_or_pointer_patches=0,new_actor_images=0,
        diagnostics=['Initial open expectation wrongly replaced a possession flag; corrected expected fixture,oldfailed record retained and excluded.',
          'Initial diagonal projection selectedY-first; native mismatch establishedX-first; corrected successful record counted once.',
          'Debug YMM readback covers only256 bytes,not a new fullactor image.']),
      counts=dict(native_cases=len(out),native_replays=len(rows),whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),
        position_application_cases=4,preflight_cases=2,creature_source_files=raw['file_count'],creature_records=raw['records'],
        new_raw_API_patches=raw['api_exact_container_patches'],new_actor_images=0,original_game_function_or_vtable_patches=0),
      limitations=['Exact supplied DX profile only;no new other-version execution or live game run.',
        'Native composition with explicit neutral forces,not complete488930tick,fullforce/path registration,A*,renderer or physics.',
        'RawEFC→mid12EC→definition686 binding statically verified here;not a new fullrawCreatureloader→movement execution.',
        'Navigation buffer construction from raw KLD/Terrain is not established by manually prepared navigation categories.',
        '48EBF0 contributes globalE4 in two explicit compositions;4D5A40 alone does not refresh it.',
        'Axis slide priority confirmed in documented fixture;rotated alternatives and all coordinate/overflow possibilities not covered.',
        'No raw max_speed field introduced and no rawCreature speed clamp4095 prescribed.',
        'Scalar98/113/114,otherpossession stats,Objects34C,Traps204,Doors236,Terrain198/224,CreaturePriority/Jump and remaining15-format questions keepgoalopen.'])
    (ROOT/'navigation-step-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))

if __name__=='__main__':main()
