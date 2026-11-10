"""Publish complete active Room asset-request loading and bounded ignored-slot evidence."""
from pathlib import Path
import json
import emulate_room_resource_loader as native
import crosscheck_room_resource_loader as controls
import prepare_room_resource_loader_primary as primary
from validate_room_resource_loader import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase150-room-resource-loader-primary.json')
    manifest=load('phase150-room-resource-loader-emulations.json');rows=manifest['replays'];assert len(rows)==62 and len({r['file'] for r in rows})==62
    states={}
    for c,row in zip(native.cases(),rows):
        args,checks,saved,path=native.execute(c);post,out=native.verify(c,args,checks,saved['result']);states[c['name']]=post
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes()) and row['file']==str(path.relative_to(ROOT))
    pairs=[]
    for mode in range(1,14):pairs.append(dict(name=f'mode{mode}',differences=native.validate_pair(states[f'mode{mode}-source'],states[f'mode{mode}-four-filled'],native.SPANS)))
    for off in native.SPANS:pairs.append(dict(name=f'only-{off:x}',differences=native.validate_pair(states['mode7-source'],states[f'only-{off:x}-filled'],[off])))
    for name in ['no-service','update']:pairs.append(dict(name=name,differences=native.validate_pair(states[name+'-source'],states[name+'-filled'],native.SPANS,update=name=='update')))
    assert pairs==manifest['whole_memory_pairs'] and len(pairs)==19
    assert native.requests(states['mode7-source'])!=native.requests(states['complete-control'])
    cross=load('phase150-room-resource-loader-crosschecks.json')['replays'];assert len(cross)==6
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase150-room-resource-api-validation.json')
    preservation=load('unknown-search-review.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    known={str(Path(r['file']).resolve()).casefold():r['sha256'] for r in preservation['source_files']}
    for row in edits['files']:assert known[str(Path(row['file']).resolve()).casefold()]==row['sha256']
    before=ROOT/'phase150-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==128
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==128
    descriptions=['Straight Mesh','Inside Corner Mesh','unresolved resource','Outside Corner Mesh']
    bindings=[dict(raw_offset=ro,raw_bytes=84,intermediate_offset=mo,editor_label=label,
        native_game_loader='In all13selectedconstructionmodes these84MIDbytes arereadonly bythe1267-bytechecksumloop; no resource-service request/descriptor field isproducedfromthem.',
        editing='Preserve all84bytes. Existingnativeeditor labels areUIevidence; do not promise independentfloor-model replacement via these fields inthisloader.',
        scope='TemplateTemple record with boundeddiagnostic bytes; source26Roomrecords separatelyexecuteunaltered. No globaldeadfield/rendering proof.')
        for ro,mo,label in zip(native.SPANS,native.MID_SPANS,descriptions)]
    outcomes={r['case']['name']:r['outcome'] for r in rows}
    return dict(phase=150,date='2026-10-31',family='Rooms',profile=attested['profile'],primary=attested,
        native_chain=dict(Terrain_MID_ctor=0x56EB60,Terrain_raw_converter=0x56ED20,Terrain_loader=0x4F66E0,
            Room_definition_ctor=0x4EAB70,Room_MID_ctor=0x56DE40,Room_raw_converter=0x56DFD0,Room_loader=0x4EACC0,
            final_observer='OriginalWorldgetter508DF0 onlyreturns afterallsevenwholebeforecalls. NoRoomgeometry/Worldtick runs.'),
        resource_consumption=bindings,
        checksum=dict(instance_offset=0xA2,width=4,source='All1267bytes oforiginalRoomMID, includingpresence flags andthefourresourceblocks.',
            loop_read=0x4EACFC,loop_bound=0x4EAD02,formula='unsigned32 sum ofMIDbytes0..1266; valuesinacceptedcases do notoverflow.',
            source_Temple_checksum=outcomes['mode7-source']['checksum'],four_filled_checksum=outcomes['mode7-four-filled']['checksum'],
            update='WorldDWORD_A3C7!=0 skipsinitialization/recalculation andall84byte-readloops. Explicitpreparedpreviouschecksum12345678 ispreserved; no claim thatfreshctor initializeschecksum.',
            editing='A2 is anoffsetinruntimedefinition, not aKWDfilepatchoffset. Loaderrecalculates inordinarymode; no newserializedchecksumfield introduced.'),
        actual_asset_requests=dict(service_argument='Thirdstackargument of4EACC0; explicitnonNULLservice withslot54.',
            signature='slot54(name*,type,flags,outHandle*,1,0), calleeRET24. ResourceIOadapterpreservesEBX/ESI/EDI,logsboundedname/type/flags/returnPC,writes deterministicordinalhandle,andreturnssuccess.',
            metadata='Original504D80 unpacksrawArtResourcemetadata andstoresreturnedhandle inruntimedescriptors; itisnotstubbed.',
            floor='Acceptedrequestsequence matchesindependentexistingroom_floor_resource_plan forsourceRoomrecords andall13constructionmodes, excludingStart/End auxiliarycalls byprimaryPCs. Othercommonrequests areloggedandcomparedpairwise.',
            complete_control='ChangingCompleteMeshbasename toP150_Control changes actualrequestnames. OrdinalIOhandles represent perrunrequestedassets; no loadedfile/GPUimage guarantee.',
            resource_absent='NULLserviceargument preventstheloggedfloor/commonrequests; checksumstillchanges withfourrawresources.',
            source_special='UnchangedsourceHeroPortalID19 makes0requests inthisloader; no globalabsenceofitsgraphics implied.'),
        pairs=dict(count=19,compared_regions_per_pair=len(states['mode7-source']),compared_bytes_per_pair=sum(map(len,states['mode7-source'].values())),
            permitted_differences='Selectedraw84bytes, matchingMID84bytes, andatmost4bytes ofruntimechecksumA2. Allothercheckedbytes,includingrequestlog, identical; updatepair doesnotchangechecksum.',
            selected_modes=list(range(1,14)),separate_single_resource_pairs=native.SPANS),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
        counts=dict(native_scenarios=62,source_Room_records_executed=26,diagnostic_construction_modes=13,whole_memory_pairs=19,
            independent_Ghidra_CPU_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            original_function_extents=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],primary_data_blocks=len(attested['blocks']),
            new_exact_container_edits=edits['new_exact_container_edits'],public_decode_rebuild_controls=edits['public_decode_rebuild_controls'],source_Room_records=edits['source_Room_records'],
            clarified_resource_loader_spans=4,newly_named_opaque_fields=0,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0),
        helpers=[link(n) for n in ['probe_room_resource_loader.py','emulate_room_resource_loader.py','crosscheck_room_resource_loader.py','prepare_room_resource_loader_primary.py',
            'native_room_resource_loader.py','validate_room_resource_loader.py','native_room_resources.py']],
        limitations=['ExactsuppliedDX; no transfer toothergames/Flamepatches orRC3behavior.',
            'ExplicitpreparedWorld/terraincache/Session provider andsuccessfulassetIOadapter. No actualassetlookup/files/modelgeometry/GPU,RoomplacementorWorldscheduler.',
            'Originalcurrent26Roomrecords executedonce; all13mode/four-spanpairs useTempletemplate anddiagnosticboundedresources. Notall82containersnewlyruninCPU.',
            'FourresourceMIDblocks readbychecksumonly inacceptedloaderpaths. Thisdoesnotproveglobalnonuse, intendedpurposeorabsenceofunseenconsumers.',
            'Unchangedordinalhandles acrossdifferentlynamedrequests donotmean theassetsareequivalent; fullrequestlog isobservableandpositiveCompleteMeshcontrol changesnames.',
            'Updatefixture setsanexplicitpreviouschecksum; firstA5-seedexpectation wascorrected torecognize preservation, not agamebug.',
            'RawpatchAPIvalidatesstorage/preservation, not arbitraryresource-name/type validityforrendering. Unknown1C4 staysunnamed; readinessexpertestimateabout75%.'])

if __name__=='__main__':
    d=build();(ROOT/'room-resource-loader-semantics.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(d['counts']))
