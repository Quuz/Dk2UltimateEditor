"""Publish actual secondary speed flag producer, ray and raw Door dependency."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
import emulate_possession_neighbours as native
import native_possession_neighbours as api
import native_possession_stats as speed_api
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,items in load('phase95-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    exe=Path(load('possession-health-semantics.json')['profile']['executable']).read_bytes()
    assert struct.unpack('<I',pe_read(exe,0x66e3ec+0x1d8,4))[0]==0x508fc0
    return out

def results():
    rows=load('phase95-possession-neighbour-emulations.json')['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==34
    for row in rows:
        path=ROOT/'ghidra'/row['file'];v=json.loads(path.read_text());c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(path.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['speed']).items())
        assert speed_api.speed_modifier([4096,8192,2048][c['mode']],c['percent'],c.get('target_possessed',False),bool(e['target_bit08']))==e['speed']
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out

def corpus():
    files=[];records=patches=0;aggregate=hashlib.sha256()
    for row in load('door-loading-semantics.json')['corpus']['files']:
        path=Path(row['file']);data=path.read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data;local=0
        for si,s in enumerate(parts):
            if fmt.FAMILIES.get(s.signature)!='Doors':continue
            relative=len(s.header)
            for ri,raw in enumerate(s.records):
                assert len(raw)==616;records+=1;old=struct.unpack_from('<I',raw,0x228)[0]
                for enabled in [False,True]:
                    changed=api.patch_door_bit40(raw,enabled);rr=list(s.records);rr[ri]=changed
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
                    packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));expected=bytearray(data)
                    struct.pack_into('<I',expected,s.offset+relative+0x228,(old&~0x40)|(0x40 if enabled else 0))
                    assert packed==expected and fmt.parse(packed)[si].records==rr
                    assert all(((a^b)&(~0x40&255) if i==0x228 else a^b)==0 for i,(a,b) in enumerate(zip(raw,changed)))
                    aggregate.update(hashlib.sha256(packed).digest());patches+=1;local+=1
                relative+=len(raw)
        files.append(dict(file=str(path),sha256=row['sha256'],bit40_patches=local))
    assert len(files)==82 and records==656 and patches==1312
    return dict(files=files,source_files=82,door_records=records,API_exact_container_patches=patches,ordered_patch_digest=aggregate.hexdigest(),
      scope='All82 previously accepted Door sources hashchecked and byte-exact roundtripped. Only raw flagsDWORD228 bit40 changed;other31bits,unknown bytes,headers,order,neighbour records and sections preserved. No source writes.')

def current():
    out={}
    for family in ['Doors','GlobalVariables']:
        doc=load('decoded/'+family+'.kwd.json');data=Path(doc['source']).read_bytes();assert digest(data)==doc['sha256']
        row=doc['sections'][0]['records'][0] if family=='Doors' else next(r for r in doc['sections'][0]['records'] if r['words_u32'][:2]==[224,21])
        raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+len(raw)]==raw
        out[family]=dict(file=doc['source'],sha256=doc['sha256'],record_offset=row['offset'],raw_hex=raw.hex())
    assert struct.unpack('<4I',bytes.fromhex(out['GlobalVariables']['raw_hex']))==(224,21,200,10)
    return out

def main():
    p=primary();rows,out=results();c=corpus();source=current();prior=ROOT/'phase95-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==73;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=95,date='2026-10-28',profile=load('possession-health-semantics.json')['profile'],primary=p,current_sources=source,
      native_replays=rows,outcomes=out,corpus=c,api_helpers=[link(n) for n in ['native_possession_neighbours.py','emulate_possession_neighbours.py','native_possession_stats.py','native_door_loading.py']],
      flag_producer=dict(function='4BFD00',arguments='ECX=owner receiver;stackarg=source Creature;RET4',
        list='ReceiverWORD6→global6EE540;each listed entityWORD10 is next tag. No per-member owner/type test inside this function.',
        flag='TargetDWORD3EA bit08',source_gate='SourceDWORD3E6 bit80 possession;new source-on paths execute real48DEA0(1)',
        unpossessed='Clears bit08 on every listed entity,including source if listed;prepared source is already unpossessed in this branch,48DEA0(0) itself is not newly executed.',
        possessed='Skips source itself;for every other list member,set bit08 iff native4512F0(sourceXYZ,targetXYZ,0,1) returns nonzero,otherwiseclear.',
        static_calls=['488930 at48A7AE:when possessed and current tick unsigned>actorDWORD3EE,call owner4BFD00(source);then set3EE=currenttick+20.',
          '48DEA0(0) at48DED6:after earlier AI call,invoke owner4BFD00(source) to clear neighbour flags. Fullunpossession path not executed in this phase.'],
        no_radius_here='Function calculates squaredXY differences into unused stack locals;no distance threshold is read or compared in this native span. Tested open ray from(1.5,1.5)to(30.5,30.5)setsflag;this does not exclude upstream/global range rules.'),
      ray=dict(function='4512F0',inputs='Pointers to Q12-style signed32 XY positions;arguments0,1 passed to tile checks. Z is not read.',
        algorithm='Integer XY grid traversal with x/y multiplied16,step ratiosQ14 and boundary/corner handling;returns1 when remaining endpoint falls in current cell,0 when a tested cell blocks.',
        scope='Original complete native ray executes in34 compositions;no new general-purpose Python ray projection is provided.',
        same_cell='Returns1 before checking nav flags or Door objects. Initial source tile itself not rejected in tested multi-cell ray;blocked target cell does reject.',
        navigation='4511A0 first tests navBYTE&08;nonzero blocks. Nav pointer atWorld+9F2,width+8,dataPointer+10. Runtime bitmap is prepared input,not a claim about raw map flag encoding.',
        door_gate='If nav high nibble nonzero,4528E0→4D1A20→original World vslot1D8/508FC0 finds Door-kind4 in map cell.4511A0 then blocks anykind4 with definitionBYTE8B&40==0;bit40 passes under args0,1.',
        high_empty='High nav nibble alone with no Door does not block;Door list with no high nav nibble is not tested by this path.',
        corners='Diagonal/corner native path executed with open and blocked maps;general corner behaviour beyond documented fixtures not replaced by a simplified API.'),
      speed=dict(function='48ABB0',type224_stat_id=21,query='Same StatId21 forwalk/run/shuffle;loaded value is signed16.',
        formula='product=low32(baseAfterHealthTerrain*(100+signed16(P)));possessed→unsignedproduct//100;elsebit3EA08→unsignedproduct//200;elsebase.',
        current='Raw224/21/200/10 unchanged fromcurrentGlobalVariables gives ×3 on possessed source and ×1.5 on secondary-only target;new full target speed path observed4096→6144.',
        new_examples=['Open ray:target6144;nav or ordinaryDoor blockingray:4096.',
          'Run8192→12288,Shuffle2048→3072 underP200.',
          'P0→2048,P100→4096,P200→6144 forbase4096. Half of total multiplier,not just half ofaddedpercentage.',
          'Bothflags on target:possession wins→12288 atP200.',
          'Preparedscalar98/113/1140,6,30000 give identical new flag/speed underP200;no global-unused conclusion.'],
        before_modifier='Original low-health/Terrain branches execute with neutral fixture settings;their other conditions not newly claimed.'),
      door_raw_binding=dict(raw_flags_dword=0x228,mask=0x40,intermediate_dword=0x29c,runtime_definition_dword=0x8b,
        baseline_name='Door.DoorFlag.IS_BARRICADE frombaseline enum;this phase independently proves the ray consequence ofmask40,not all barricade gameplay.',
        native_chain='56A920 zero-init732mid→56AA40(raw616)→4A7100(def181,mid,0)→4BFD00→4512F0/4511A0→48ABB0',
        semantics='ConverterORs rawDWORD228 into midDWORD29C;true initializer starts it0;loader copiesDWORD into definition8B whenpresence70nonzero.',
        new_tests='Fourcomplete raw paths:zero-filled valid-size Door withflagoff/on andfirstcurrentDoors record patchedoff/on. Entire616raw,732mid,181definition plus64guard bytes each checked.',
        boundary='Loaderglobal6FCDCF=1 suppresses visual/audio/checksum path. Definition starts explicitlyzero;no resource/render behaviour inferred.',
        patch='native_possession_neighbours.patch_door_bit40 preservesother31flags,allotherrawbytes. This existing flag has other effects;UI should retainits establishedidentity,notrenameit solely forpossession.'),
      counts=dict(native_cases=len(out),native_replays=len(rows),raw_door_compositions=sum(bool(r['case'].get('raw_door')) for r in out),
        whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),source_files=c['source_files'],door_records=c['door_records'],API_exact_container_patches=c['API_exact_container_patches'],
        new_actor_images=0,original_game_function_or_vtable_patches=0),
      scope=dict(source_actor=load('possession-health-semantics.json')['scope']['source_actor'],
        setup='Target and explicitly prepared second/source actor reuse earlier native1343byteactor image,finite ownerlist101→100→0,32×32runtime nav/map. This is not two new native allocations or a live-world dump.',
        chain='Raw224 actualcallback→actual source-onsetter→actual4BFD00→originalray/tile/Worldmaphelpers→complete target speed;4cases prependfull rawDoorinit/converter/loader.',
        providers='Existing synthetic external GFX/sound/OSproviders retained fromphase90 setup;no new originalgame/vtable/table replacement. New rawDoorpaths suppress visual/audiobranch through explicit loadergate1.'),
      limitations=['Exact provided DX only;old/RC3/Pro not executed bythis phase.',
        'Full488930tick and48DEA0(0)/Shot26/control lifecycle not composed;theircallconditions arestaticoriginalcode evidence.',
        'Finite ownerlist andruntime navigation/Map/Door objects areprepared;automatic list/navconstruction not rerun.',
        'RawDoorloader gate1 excludes realresources,sound andchecksum. Newrawpaths checkallmid/definitionbytes butnot rendereddoorbehaviour.',
        'No radiusthreshold in4BFD00;longopenfixture doesnotproveglobalradiusabsence. Out-ofmap/cycliclists/extremecoordinateoverflow not executed.',
        'No generalray APIisprovided. Other rayargumentcombinations/doorstate/corner cases remainoutside34fixtures.',
        'Scalar98/113/114 andother224stat consumers not globallyassigned fromunchanged newflag/speed outcomes.',
        'Objects34C,Traps204,Doors236,Terrain198/224,CreaturePriority/Jump andother15-familyunknowns keepgoalopen.'])
    (ROOT/'possession-neighbour-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))

if __name__=='__main__':main()
