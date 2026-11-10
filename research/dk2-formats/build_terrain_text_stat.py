"""Publish actualoriginaleditor bufferedTerrainStat19 -> raw224 save/reload behavior."""
from pathlib import Path
from copy import deepcopy
import json,struct
import emulate_terrain_text_stat as native
import crosscheck_terrain_text_stat as controls
import prepare_terrain_text_primary as primary
import validate_terrain_text_storage as storage
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def effective_layout():
    out=deepcopy(load('phase165-prior-effective-terrain.json'))
    field=next(f for f in out['fields'] if f['offset']==0x224)
    field['verified_editor_text_stat']=dict(status='confirmed_editor_text_stat19_binding_gameplay_unresolved',raw_offset=0x224,raw_bytes=4,
        editor_MID_offset=0x2DC,editor_model_offset=0x320,editor_model_kind='binary64',native_text_stat_code=19,
        callback=0x4A3D30,assignment_PC=0x4A400F,writer=0x4A7690,
        text_rule='OriginalbufferedTSVreader: ID/name/numericcell, preparedcolumncode19. Nativeatof ->model320double; writer trunc(value*4096)low32.',
        skip_rule='Inselectednumericcells, dashorleadingminusparsingas0skips; - and -0tested. Plain0writes0. Outofrangecode20ignored.',
        reload_rule='RawreloadedasunsignedDWORD/4096. Nativeinput-1 savesFFFFF000, reloads1048575; positive3.5reloads3.5.',
        UI_label_or_gameplay_units_confirmed=False,whole_file_header_or_IO_executed=False,schema='terrain-text-stat-semantics.json')
    return out

def build():
    attest=primary.build();assert attest==load('phase165-terrain-text-primary.json')
    manifest=load('phase165-terrain-text-emulations.json');assert len(manifest['replays'])==13
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        fixture,s,path=native.execute(c);out=native.verify(c,fixture,s['result'])
        assert s['backend_sha256']==sha((ROOT/'native_editor_text_executor.py').read_bytes())
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(path.read_bytes()) and s['input']==fixture
        traces.append(s['result'])
    cross=load('phase165-Terrain-text-crosschecks.json')['replays'];assert len(cross)==5
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert args==d['input'] and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        assert row['unassigned_header_bytes_excluded']==[42,43,47]
    serial=storage.validate();assert serial==load('phase165-terrain-text-storage.json')
    prior=load('phase165-prior-components.json');assert len(prior)==143
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('phase165-source-baseline.json')
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    assert serial['codec']['sha256']==load('phase159-terrain-light-storage-validation.json')['codec']['sha256']
    exe=native.engine.EXE.read_bytes();table=struct.unpack('<19I',pe_read(exe,0x4A40C4,76));assert table[18]==0x4A4007
    assert pe_read(exe,0x4A400F,6)==bytes.fromhex('898e20030000')
    assert pe_read(exe,0x53A330,8)==b'TERRAIN\0' and struct.unpack('<d',pe_read(exe,0x50AC50,8))[0]==0
    no_name='ghidra/phase165-Terrain-text-ee644b1ad08c.json';bad=load(no_name)['result']
    assert bad['success'] and bad['visited_instructions'].get('0x4a3d30')==1 and not bad['visited_instructions'].get('0x4a400f')
    visits=lambda a:sum(r['visited_instructions'].get(hex(a),0) for r in traces)
    counts=dict(native_scenarios=13,original_raw_conversions=visits(0x42ED90),allocated_model_callbacks=visits(0x49F5E0),
        whole_Terrain_model_constructors=visits(0x4250D0),whole_text_callbacks=visits(0x4A3D30),selected_Stat19_assignments=visits(0x4A400F),
        complete_writer_runs=visits(0x4A7690),actual_saved_output_reload_cases=2,
        independent_full_Ghidra_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        new_original_function_extents=len(attest['functions']),reused_original_function_extents=len(attest['inherited_common']['reused_function_addresses']),
        static_only_function_extents=1,unique_executed_original_PCs=attest['unique_executed_original_PCs'],constant_or_vtable_blocks=len(attest['blocks']),
        exact_changed_container_rebuilds=serial['exact_changed_container_rebuilds'],source_Terrain_files=len(serial['files']),source_Terrain_records=serial['records'],
        new_editor_text_stat_bindings=1,newly_named_opaque_gameplay_fields=0,preserved_source_files=len(preservation['source_files']),
        preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    return dict(phase=165,date='2026-11-02',family='Terrain',profile=attest['profile'],primary=attest,
        field=dict(raw_offset=0x224,raw_bytes=4,physical_type='u32bits',editor_MID_offset=0x2DC,editor_model_offset=0x320,
            editor_model_kind='binary64',native_text_stat_code=19,native_editor_label=None,gameplay_meaning='unresolved',gameplay_units='unresolved',
            inherited_game_definition_offset=0x1B5,inherited_game_store='All32rawbitscopied; no newgameconsumerexecutionhere.'),
        native_path=dict(converter=0x42ED90,allocated_model_callback=0x49F5E0,model_constructor=0x4250D0,model_bytes=0x3D8,
            text_callback=0x4A3D30,reader_next_line=0x4AB590,reader_next_cell=0x4AB630,native_atof=0x4AD4EB,
            assignment_PC=0x4A400F,writer=0x4A7690,catalogue_vector=0x54F960,
            stages='ActualrawRockID1 -> originalconverter/modelallocation/registration -> wholebufferedTerraintextcallback -> wholewriter/CFile sink -> optionalconverter/modelreloadfromactualsavedoutput.',
            prepared_reader='7DWORDreaderstate/cachedASCIIbuffer,end,currenttoken/delimiter; initialheaderstubalreadydelimited. RootadvancestodataID1/nameTerrain1/cell. No fileopen/readorwholeheaderparserexecuted.',
            prepared_columns='Context54F550+650 points[19], +654=1. Firstnumericcellusescolumnindex1; skippednamecellindex0. Fullnativevalueparser, nothostparsedinput.'),
        text_contract=dict(row_fields=['TerrainID','ignored_name_cell','numeric_stat_cell'],delimiters='TABcells,CRLFrows; originaltokenizernullsTAB/CRinplace.',
            select='OriginalcallbackfindsID1intheactualregisteredTerrainmodelarray; actualTERRAINdispatch/columncodeheaderpath4A39B0isstaticbyte-attestedonly.',
            column_code='Numericcode19 subtracts1 anduses19entryjumptable4A40C4, entry18=4A4007; thenstoresbothDWORDsofdouble320.',
            save_formula='Nativeatofresultstoredasbinary64; originalwriter multiplies4096, native_ftol truncatestowardzeroandstoreslow32.',
            skip='Nativeleadingminusandnumeric0causesnofieldassignment; dashand-0tested. Plain0assigns0. Code20outside1..19skipsassignment.',
            reload='Originalrawloaderrebuildsmodel320fromunsignedDWORD/4096. Negativeimportisnotcanonicalsignedmodelroundtrip.',
            limitations='Onlyselected13cells/onepreparednumericcolumn/ID1tested. NotallTSVquoting/locales/NaN/Inf/exponents/malformedinputorfullfilegrammarvalidated.'),
        runtime_outcomes=[dict(case=r['case'],**r['outcome']) for r in manifest['replays']],
        roundtrip_examples=[dict(text='3.5',model_before_save=3.5,raw_saved=0x3800,model_after_reload=3.5),
            dict(text='-1',model_before_save=-1.0,raw_saved=0xFFFFF000,model_after_reload=1048575.0)],
        source_and_fixture=dict(source='OriginalstandaloneRockID1record552bytes; initialraw224=1234hexdiagnosticeditinmemory. Exactly548otherrecordbytesunchanged; sourcefilesnotwritten.',
            environment='Frozenphase127CRT heap/CString pools/TLS/clock/nativeallocation/resource parsing/registration; explicitboundedWin32IAT/CFile providers. No code/vtablepatches.',
            stored_output='Exactlyone552bytereCordinside608bytecontainer. WholeoutputrecordequalsinputexceptexpectedDWORD224; raw198/16WORDtable/allotherfields preserved.',
            reload='Twocasesreadactualwriter.OUTPUT+56throughoriginalconverter/callbackintoasecondmodel; no hostpatch/reconstructedrawusedasreloadinput.',
            controls='5wholeindependentGhidraemulationscompareallselectedoutputmemoryincludingheap, raw/MID/reader/cachedtext, allocators/modelarray/fileoutput. Onlyunassignedheaderbytes42/43/47excludedasinthepriorwritercontrols.',
            low_page='Whole4096bytelowpagepreservedandcomparedinthe5Ghidracontrols. Nativeobserverlogsreadsandwrites; onlybyte-attestedFS0DWORDSEHaccessesaccepted. Frozenpriorbackendunchanged.',
            profile_scope='ExactProSHA989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09; no GUI or ordinaryapplication/filebootstrap/GPU.'),
        discovery_review=dict(offset198_leads=link('phase165-terrain-editor-198-leads.json'),offset224_leads=link('phase165-terrain-editor-224-leads.json'),
            result='Direct320textwrite4A400FconfirmedTerrainbycontext410/414registeredvectorandTERRAINdispatch. 4A4460isRoomsdispatch; sharedoffsetaloneisnotfamilyproof. 48131Finitializerremainstypedpointerlead,notnewmeaning.',
            inherited_Score='F4CisalreadyScoreincreature-editor-scalars,notnewopaque-fielddiscovery. Fourlimitedgamefunctionsreviewedstatically; oldboundedlimitsnotclaimedresolved.',
            rejected_incomplete_discovery=dict(**link(no_name),reason='Initialpreparedrowomittedignorednamecell; numericvaluewasconsumedasskippedcolumn0. Callbackreturned1withoutStat19assignment; excludedfrom13acceptedcases.')),
        native_executions=manifest,independent_Ghidra_controls=cross,storage_validation=serial,preservation=preservation,effective_terrain_layout=effective_layout(),counts=counts,
        helpers=[link(n) for n in ['probe_terrain_text_stat.py','emulate_terrain_text_stat.py','crosscheck_terrain_text_stat.py','prepare_terrain_text_primary.py','validate_terrain_text_storage.py',
            'native_editor_text_executor.py','snapshot_terrain_text_sources.py','emulate_room_model_chain.py','emulate_room_writer.py']],
        limitations=['Bufferedreader/columnmap/registeredcatalogueprepared; filenameopen/read and4A39B0wholeheaderdispatchnotexecuted. Stat19bindingprovenbutnoofficialdescriptivefieldnamefound.',
            '13selectednumericcells,onemodelID1/onecolumn. Noallmalformedinput/locales/exponents/NaN/Inf/overflowclaim. Nativecode20ignoreonlyselectedoutofrangecode.',
            'Nativeeditornegativeimportwrapsrawbitsandunsignedreloadchangesdisplay; no gameplaynegative-valuecontractorbrokenmechanicclaim.',
            'Originalmodelallocation/constructor/CString/resourceparsing/registration/wholewriterexecuteonfrozenpreparedCRT/clock/IAT/CFileproviders; no fullGUI/app/OSfilebootstrap.',
            'Ghidracontrolsexcludeonlyunassignedheaderbytes42/43/47; fullrecordsandotherselectedmemoryverified. Staticheaderfunctionattestedseparatelyfromexecuted4552PC.',
            'Raw198/224and16WORD1AEgameplaypurposesstillunresolved; no newopaquegameplaynames. Prior143schemasandphysicalcodecunchanged;420currentbaseline sources/7EXEverified. Fourhistoricalphase164mapdifferencesrecordedexplicitly,notrolledback;remaining15familyquestionsopen.'])

def main():
    s=build();(ROOT/'terrain-text-stat-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase165-prior-components.json');key='terrain_text_stat_semantics'
    assert set(p['components']) in [set(old),set(old)|{key}]
    for k,row in old.items():assert p['components'][k]==row
    p[key]=s;p['components'][key]=link('terrain-text-stat-semantics.json');p['effective_catalogue_layouts']['Terrain']=s['effective_terrain_layout']
    c=s['counts']
    note=f"terrain_text_stat_semantics phase165 confirmsactualoriginalProbufferedTERRAINStatData19->model320double->wholewriterraw224:3.5->3800,-1->FFFFF000andactualreload1048575;dash/-0skip,plain0writes,code20ignored.13native/15rawconversions15models/13textcallbacks13writers;5wholeGhidra{c['Ghidra_compared_regions']}regions{c['Ghidra_compared_bytes']}bytes(includingwholelowpage);94newextents(including1staticheader)/34inherited/4552PC/3blocks;246exactraw224edits82files3306records. Preparedbuffer/headercolumns,wholefile/GUIbootstrapnotrun; gameplaymeaningunknown. Prior143schemas/codec preserved;420currentbaseline sources/7EXEverified,4preexistingmapdifferencesrecorded. EffectiveTerrain224addsverified_editor_text_statonly."
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])

if __name__=='__main__':main()
