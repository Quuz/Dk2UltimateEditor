"""Publish typed Room-intensity render consumer and bounded original executions."""
from pathlib import Path
import json,struct
import emulate_room_torch_light as native
import crosscheck_room_torch_light as cross
import prepare_room_torch_primary as primary
import native_room_torch_light as api
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=native.sha((ROOT/n).read_bytes()))

def build():
    manifest=load('phase123-light-render-emulations.json');assert manifest['scope']==native.__doc__ and len(manifest['replays'])==16
    for case,row in zip(native.cases(),manifest['replays']):
        args,expected=native.build(case);path=native.path_for(args);saved=json.loads(path.read_text(encoding='utf8'))
        assert saved['input']==args and saved['case']==case and row['case']==case
        assert row['file']==str(path.relative_to(ROOT)) and row['sha256']==native.sha(path.read_bytes())
        outcome=native.verify(case,args,expected,saved['result']);assert row['outcome']==outcome
        if outcome['created_render_record']:
            b=bytes.fromhex(saved['result']['captured_hex']);x,y=struct.unpack_from('<2f',b,20+0x30)
            assert x==y==api.normalized_intensity(case['intensity'])==outcome['normalized_intensity']
    controls=load('phase123-light-render-crosschecks.json');assert len(controls['replays'])==2
    for name,row in zip(cross.CONTROLS,controls['replays']):
        args,count,size=cross.build(name);saved=load(row['file']);r=saved['result']
        assert row['case']==name and row['checked_regions']==count and row['checked_bytes']==size
        assert saved['input']==args and row['sha256']==native.sha((ROOT/row['file']).read_bytes())
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    assert load('phase123-primary-manifest.json')==primary.build()
    source=load('phase122-room-api-validation.json');views=0
    import dk2_formats as fmt
    for row in source['files']:
        data=Path(row['file']).read_bytes();assert native.sha(data)==row['sha256']
        for section in fmt.parse(data):
            if section.signature!=0x78:continue
            for raw in section.records:
                state=api.view(raw);assert state['raw_u8']==raw[0x36E] and state['opaque_neighbor_u8']==raw[0x36F]
                views+=1
    assert views==2075 and len(source['files'])==82
    executables={r['file']:r['sha256'] for r in source_executables()+load('editor-variant-semantics.json')['profiles']}
    for file,h in executables.items():assert native.sha(Path(file).read_bytes())==h
    old=ROOT/'phase123-prior-components.json'
    if not old.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==101
        old.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(old.name))==101
    return dict(phase=123,date='2026-10-29',family='Rooms TorchIntensity typed light-render consumer',
        executable=str(primary.EXE),executable_sha256=native.sha(primary.EXE.read_bytes()),
        original_executables_unchanged=[dict(file=p,sha256=h) for p,h in executables.items()],
        primary=link('phase123-primary-manifest.json'),native_executions=manifest['replays'],
        independent_Ghidra_controls=controls['replays'],
        physical_width=load('room-writer-semantics.json')['torch_intensity'],
        typed_static_path=dict(Room_definition_intensity=0x45E,Room_entity_definition_pointer=0x19,
            map_cell_Room_tag=dict(offset=2,mask=0xFFF,lookup=0x4559B0,entity_registry=0x6EE540),
            wall_light_builder=0x439B90,Bridge_constructor=0x43A740,Bridge_vtable=0x66CC94,
            Bridge_vtable_create_slot=0xA4,Bridge_create=0x4397E0,Light_constructor=0x43A0D0,
            Light_initializer=0x43A1E0,Light_vtable=0x66CC14,Light_intensity=0x145,
            data_flow='439B90 readsRoomdefinition45E andpasses fifth ofsevenCLightinit arguments;4397E0allocates326bytes,constructor then43A1E0 storeslow8 at145. This Room-placement part is static,not newCPUexecution.'),
        render_parameter=dict(source_width=1,signed=False,divisor=128,constant_address=0x670524,
            output_type='float32',output_record_bytes=56,output_offsets=[0x30,0x34],
            examples=[dict(raw=n,value=api.normalized_intensity(n)) for n in [0,1,64,128,255]],
            range=[0,255/128],scope='Exact input→render-record parameter,not a measured percentage of finalpixel luminance.',
            zero_rule='Intensity0 stillallocates andqueues a56byte record whenkind2 andradiusnonzero;bothfloatfields zero.'),
        original_execution=dict(whole_render_functions=[0x599960,0x599C60],
            allocator_wrapper=0x57CEB0,allocator=0x57CD30,mode_flags_helper=0x599BD0,
            queue_append=[0x571A90,0x571AB0],queue_offsets=[0x34,0x38],queue_global=0x760B80,
            fixture='PreparedCLight326bytes+64guard;realvtable66CC14;kind2,radius8192,RGB64/128/255,no meshresource,motion12E=0.',
            allocator_fixture='128byte nativeblock,class28 in765B00;56requestedbytes,no exhaustion. Originalallocator writesusedbyte18 andremovesbuckethead;remainingpayloadslack/guard predicted.',
            result='Wholepreparedmemorymatches,including192byteheap,CLightguard,engine,queue,allocator,originalLightvtable andglobals.',
            gates='kind!=2 orradius0 skipsallocation inselectedpaths. Meshresource disabled;motion/two-endpoint branch notselected.',
            substitution_count=0,external_API_adapters=0,low_page_reads='Only verified4byte FS:[0] chain allowed;NULL+offsetreads rejected.'),
        preservation=dict(previous_verified_rebuilds=link('phase122-room-api-validation.json'),
            new_container_rebuilds=0,source_files=source['files'],source_records=views,
            rule='Width/preservation correction fromphase122 remainscurrent. Do not clamp,normalize raw36F orrewrite otherRoomfields merely to displaynormalized intensity.'),
        helpers=[link(n) for n in ['native_room_torch_light.py','emulate_room_torch_light.py',
            'crosscheck_room_torch_light.py','prepare_room_torch_primary.py']],
        rejected_routes=dict(wrong_vtable_base='66CCA0 isinterior,notvtablestart;real66CC94 storedat43A85D. Wrongslotinference43A910 isgetterandnotused.',
            guessed_constructor='4EAB30 failedasdefineddata;notaccepted evidence.'),
        limitations=['No newlyexecuted whole rawRoom→loader→wallplacement→CLightfactory chain;those links supportedbyverifiedprimaryinstructions andearlierloadingwidth evidence.',
            'Preparedscene branch has no meshresource ormotion;no liveGPU/Software frame output,final attenuation orpixel-brightness formula established.',
            'Originalallocator runs prepared128bytebucket;initialheapbootstrap,exhaustion,free/reuse andfullframe lifecycle notexecuted.',
            '16bounded cases coverselected intensity,gates andmodeflags,not everyRoom/terrain/lighttype/version.',
            'IndependentGhidra agreement uses same explicitfixtures,not independent reconstruction of liveWorld state.',
            'Rooms1C4,Traps204 andother significant15familyunknowns remain;goalnotachieved.'])

if __name__=='__main__':
    v=build();(ROOT/'room-torch-light-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print('TorchIntensity u8→N/128renderparameter:16originalcases/2Ghidracontrols;source82filespreserved.')
