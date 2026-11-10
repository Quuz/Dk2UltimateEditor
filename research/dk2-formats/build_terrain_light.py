"""Publish bounded Terrain->Light semantics, exact storage controls and preserved sources."""
from pathlib import Path
from copy import deepcopy
import json,struct
import emulate_terrain_light as native
import crosscheck_terrain_light as controls
import prepare_terrain_light_primary as primary
import validate_terrain_light_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase159-prior-effective-terrain.json'))
    for f in out['fields']:
        off=f['offset']
        if off==0x18C:f['verified_tile_light_submission']=dict(definition_offset=0x1BF,Light_position_offset=0xDE,storage='signed32 Q12',
            rule='Selected441CD0 requests this Z at tile XY centers; original446C70 may raise actual Light Z to external floor. Tested actual Light F8=0, floor0/8192/-4096.',
            full_world_or_pixel_proof=False)
        elif off==0x190:f['verified_tile_light_submission']=dict(enable_mask=0x10000000,
            rule='452DC0 selection must succeed; tested room-tag branch additionally requires runtime tile+C==FF. Do not equate runtime tile+C with opaque rawKLD byte3.',
            source='terrain_light_submission_semantics')
        elif off==0x19A:f['verified_tile_light_submission']=dict(definition_offset=0x185,
            rule='If runtime tile9 bit20 clear and tileB bit40 set, zero WORD185 replaces selected Terrain RGB with global WORDs6F29F0/F2/F4; nonzero retains Terrain RGB.',
            scope='Prepared cell paths through original441CD0; not all GoldValue uses.')
        elif off==0x1DB:f['verified_tile_light_submission']=dict(definition_WORDs=[0x1AF,0x1B1,0x1B3],Light_WORDs=[0x111,0x113,0x115],
            rule='Actual loaded WORDs, including byte+255*highbit, reach allocated Light and external engine E0 sink, unless global RGB override.',
            editor_max511_loads510=True,visual_pixels_verified=False)
        elif off in [0x198,0x224]:f['verified_tile_light_submission']=dict(status='unresolved',definition_offset=0x183 if off==0x198 else 0x1B5,
            observation='No reads in15selectedraw/cache/Light/submission paths. Full unknowns pair differs only raw copies, runtime copies and loader checksum. Actual Light parameter117 remains bootstrap1200.',
            radius_assignment_proved=False,global_nonuse_proved=False)
    return out

def text_region(path):
    blob=path.read_bytes();pe=struct.unpack_from('<I',blob,60)[0];count=struct.unpack_from('<H',blob,pe+6)[0]
    opt=pe+24;table=opt+struct.unpack_from('<H',blob,pe+20)[0];base=struct.unpack_from('<I',blob,opt+28)[0]
    for i in range(count):
        row=table+40*i
        if blob[row:row+8].split(b'\0')[0]!=b'.text':continue
        va,size,raw=struct.unpack_from('<III',blob,row+12);return base+va,blob[raw:raw+size]
    raise AssertionError(path)

def build():
    attest=primary.build();assert attest==load('phase159-terrain-light-primary.json')
    manifest=load('phase159-terrain-light-emulations.json');assert len(manifest['replays'])==15
    posts={}
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,raws,d,file=native.execute(c);post,out=native.verify(c,args,ch,raws,d['result']);posts[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(file.read_bytes())
    pair=native.compare_unknown_pair(posts['dirt'],posts['unknowns']);assert manifest['paired_controls']==[pair]
    cross=load('phase159-terrain-light-crosschecks.json')['replays'];assert len(cross)==8
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert row['sha256']==sha((ROOT/row['file']).read_bytes()) and d['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    serial=storage.validate();assert serial==load('phase159-terrain-light-storage-validation.json')
    prior=load('phase159-prior-components.json');assert len(prior)==137
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('effect-adjacent-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    normal=ROOT.parent.parent/'Dungeon Keeper 2/DKII.exe';dx=native.p.fx.EXE
    na,nb=text_region(normal);da,db=text_region(dx);assert na==da and nb==db
    comparison=load('phase159-terrain-light-producers.json');assert comparison['normal_text_sha256']==comparison['DX_text_sha256']==sha(db)
    assert sha(db)=='575af252831bcb9fddb4858cda990a02c34f4d2a77031db20d3d4fec2b7388e1'
    table=native.p.fx.pe_read(dx.read_bytes(),0x66D0D8,0x80)
    assert struct.unpack_from('<II',table,0x28)==(7,0x447D50)
    assert struct.unpack('<I',native.p.fx.pe_read(dx.read_bytes(),0x66CC94+0xA4,4))[0]==0x4397E0
    bad='ghidra/phase159-terrain-light-probe-3e42af71a1a6.json';excluded=load(bad)
    oldmem={int(r['address'],16):bytes.fromhex(r['hex']) for r in json.loads(excluded['input']['memory'])['regions']}
    assert int.from_bytes(oldmem[native.p.fx.base.BRIDGE][0x35B:0x35D],'little')==2
    counts=dict(native_scenarios=15,raw_Terrain_loads=30,native_cache_fills=15,actual_Light_factory_calls=15,actual_Lights_created=15,
        full_tile_light_calls=15,submitted_lights=sum(r['outcome']['submitted'] for r in manifest['replays']),
        independent_full_Ghidra_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        whole_memory_pairs=1,pair_compared_bytes=pair['compared_bytes'],original_code_extents=len(attest['functions']),
        static_only_function_extents=3,unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        exact_changed_container_rebuilds=serial['exact_changed_container_rebuilds'],source_Terrain_records=serial['records'],source_Terrain_files=len(serial['files']),
        newly_named_opaque_fields=0,excluded_invalid_discovery_fixtures=1,preserved_source_files=len(preservation['source_files']),
        preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=159,date='2026-11-01',family='Terrain',profile=attest['profile'],primary=attest,
        native_pipeline=dict(raw_record_size=552,intermediate_size=736,definition_size=460,
            loads='Actual4F6570/56EB60/56ED20/4F66E0 for current and fallback38; original455D40 cachefill through World60/70/74 slots.',
            selected_paths='Full441CD0 with original452DC0. Prepared runtime tile, sparse two-entry Terrain catalogue and CBridge prefix; originalWorldvtable.',
            actual_light='Original4397E0->554B30->43A0D0->4468E0->43A1E0;326-byte Light, actual tag1 registered in6D6458, listheadBridge33A and original66CC14vtable checked.',
            connected_engine='External providers: visibility1, obstruction0, prepared tile flags20, chosen floor, sound lookup0, captured E0 request. No GPU or pixels.'),
        static_bootstrap=dict(callback_index=7,callback=0x447D50,table=0x66D0D8,registration=0x43AAD0,
            light_tag_Bridge_WORD=0x35B,original_vtable=0x66CC94,creation_slot=0xA4,factory=0x4397E0,
            arguments='position[800,800,1200]hex, RGB500/500/500,128,1200hex,flags0,&out; resulting nodeWORD F2 stored atBridge35B.',
            executed=False,scope='Original packed dispatch-table target plus original vtable target and whole instruction bodies. Fullbootstrap/cleanup not executed.'),
        light_position=dict(raw_LightHeight=0x18C,definition=0x1BF,Light_XYZ=0xD6,
            requested='[x*4096+2048,y*4096+2048,signed32LightHeight]',
            applied='For actual freshly initialized Light/F8=0, original446C70 uses externalfloor and raises Z if below it. Floors0/8192/-4096 tested; negativeheight -2048 remains -2048 withfloor-4096.',
            Light_parameter117='Remains bootstrap1200hex throughout. Not bound to Terrain224; no radius or complete illumination claim.'),
        light_colour=dict(raw=[0x1DB,0x1DC,0x1DD],high_flag_masks=[0x80000,0x100000,0x200000],definition=[0x1AF,0x1B1,0x1B3],Light=[0x111,0x113,0x115],
            arithmetic='Loader byte+255*highbit reaches actual Light WORD; UI byte+256*highbit is separate. Diagnostic UI511->native510 verified.',
            override='tile9&20==0 AND tileB&40!=0 AND selecteddefinitionWORD185==0 => globalWORDs6F29F0/F2/F4; otherwise selected Terrain RGB.',
            GoldValue_binding='raw19A -> selecteddefinition185; normal Dirt zero overrides, Gold nonzero does not. Fallback38 respects selected38 GoldValue.'),
        submission=dict(enable_mask=0x10000000,gate='Original452DC0 must returntrue; preparedroomtag branch also requires runtime tile+C==FF. Otherwise Light stays at factory state; no E0 call.',
            fallback='Prepared no-room tile9no20/tileB40 with currentterrain lacking01000000 chooses actual World getter38. Diagnostic38flags patched enable on/off;38GoldValue zero/one controls override.',
            flags='Light134 OR8052 afterposition update; initial20 becomes8072.',engine_slot=0xE0,arguments='[x,y,actualLightPointer,0,FF000000hex]'),
        opaque_review=dict(raw_WORD=0x198,raw_DWORD=0x224,definition_WORD=0x183,definition_DWORD=0x1B5,
            status='unresolved',native_reads_in_selected_paths=0,whole_memory_pair=pair,global_nonuse_proved=False,radius_assignment_proved=False),
        profile_comparison=dict(normal=dict(file=str(normal),sha256=sha(normal.read_bytes())),DX=dict(file=str(dx),sha256=sha(dx.read_bytes())),
            same_text=True,text_address=na,text_raw_bytes=len(nb),text_sha256=sha(nb),
            scope='Only complete raw.text sections are byte-identical; other PE data/imports/settings/runtime equivalence not established.'),
        discovery_review=dict(evidence=[link(n) for n in ['phase159-normal-terrain-literal-leads.json','phase159-terrain-light-producers.json',
            'phase159-rc3-terrain-resource-review.json','phase159-light-creation-call-roots.json']],
            RC3='Raw143/320 Terrain dialog resources have same Light/Ambient/Height labels; no new Radius binding established. Captions are not model bindings.',
            invalid_fixture=dict(**link(bad),excluded=True,reasons=['Prepared Bridge35B=2 but actual Light tag1; caller used null pointer.',
                'Initial external sink used MOV instead of LEA for stackargs; non-FS lowpage accesses. CPU success flag alone insufficient.'],not_game_or_format_bug=True)),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,
        effective_terrain_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['probe_terrain_light.py','emulate_terrain_light.py','crosscheck_terrain_light.py','prepare_terrain_light_primary.py','validate_terrain_light_storage.py']],
        limitations=['Prepared CBridge/sparseWorld/runtime tile; whole native World/KLD loader, bootstrap and scheduler not executed.',
            'External engine/sound/floor providers declared; no real assets/GPU/pixel/intensity/radius validation.',
            'Only selected452DC0 room-tag and no-room fallback38 paths; no new exhaustive geometric branch or allTerrain proof.',
            'Light freshlyinitializedF8=0; floor adjustment is not a rawfile clamp or a proof for all entity types.',
            'Unk198/224 remain unresolved; no reads in tested paths does not prove globalnonuse. Raw1AE table untouched.',
            'Normal/DX.text equality does not prove whole runtime equivalence. RC3 captions do not establish fieldbinding.',
            'Old137 schemas and physical codec preserved; only currentTerrain annotations extended. Significant unknowns in15families remain.'])

def main():
    s=build();(ROOT/'terrain-light-submission-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase159-prior-components.json');key='terrain_light_submission_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('terrain-light-submission-semantics.json');p['effective_catalogue_layouts']['Terrain']=s['effective_terrain_layout']
    note='terrain_light_submission_semantics phase159 confirms originalrawTerrain->nativecache->actualLightfactory->full441CD0->externalE0submission.15native/30rawloads/8wholeGhidra/328exactcontaineredits. LightHeightQ12/selectedRGB/GoldValuezero globaloverride/runtimecellCgate/fallback38 verified; nativebyte+255highbit reachesLightWORD. Light117 remainsbootstrap1200; Unk198/224 unresolved andselectedwholememorypairdiffersonlystorage/checksum. Fullbootstrapstaticonly; preparedWorld/providers/noGPU. Normal/DX.textbyteequalonly. Old137schemas/420sources/7EXEpreserved; effectiveTerrainannotationsextended.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
