"""Publish bounded original-Pro Variables loading/defaults evidence."""
from pathlib import Path
from copy import deepcopy
import json,struct
import emulate_editor_global_variables as native
import crosscheck_editor_global_variables as cross
import prepare_editor_variables_primary as primary
import validate_editor_variable_storage as storage
ROOT=Path(__file__).resolve().parent
sha=native.sha
KEY='editor_variable_loading_semantics'
SCHEMA='editor-variable-loading-semantics.json'
def load(n):return json.loads((ROOT/n).read_text(encoding='utf-8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def dump(n,d):(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def build():
    manifest=load('phase166-editor-variables-emulations.json');assert len(manifest['replays'])==16
    traces=[]
    for case,row in zip(native.cases(),manifest['replays']):
        f,d,path=native.execute(case);out=native.verify(case,f,d['result'])
        assert row['case']==case and row['outcome']==out and row['sha256']==sha(path.read_bytes())
        assert d['backend_sha256']==sha((ROOT/'native_editor_text_executor.py').read_bytes())
        traces.append(d['result'])
    checks=load('phase166-editor-variables-crosschecks.json')['replays'];assert len(checks)==4
    for row in checks:
        args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result']
        assert args==saved['input'] and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert (count,size)==(row['checked_regions'],row['checked_bytes'])
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    attest=primary.build();assert attest==load('phase166-editor-variables-primary.json')
    serial=storage.validate();assert serial==load('phase166-editor-variable-storage.json')
    preservation=load('phase165-source-baseline.json')
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    prior=load('phase166-prior-components.json');assert len(prior)==144
    for row in prior.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    keys,rawdefaults,assigned=native.static_defaults();values=list(struct.unpack('<248d',rawdefaults))
    assert keys==manifest['literal_IDs'] and values==manifest['literal_defaults']
    visit=lambda entry:sum(r['visited_instructions'].get(hex(entry),0) for r in traces)
    counts=dict(native_scenarios=16,whole_default_initializers=visit(0x47FFB0),whole_Variables_callbacks=visit(0x49EC90),
        independent_full_Ghidra_controls=4,Ghidra_compared_regions=sum(x['checked_regions'] for x in checks),
        Ghidra_compared_bytes=sum(x['checked_bytes'] for x in checks),literal_default_slots=248,literal_numeric_IDs=193,
        additional_function_extents=len(attest['functions']),static_only_function_extents=2,reused_function_extents=len(attest['inherited']['reused_functions']),
        unique_executed_original_PCs=attest['unique_executed_original_PCs'],primary_data_blocks=len(attest['blocks']),
        exact_container_edits=serial['exact_container_edits'],newly_named_opaque_gameplay_fields=0,
        preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0)
    source_recipe=next(x for x in manifest['replays'] if x['case']['name']=='temple43')
    recipe_scene=load(source_recipe['file']);recipe=next(v for v in recipe_scene['input']['memory'] if int(v['address'],16)==native.RECORD)
    return dict(phase=166,date='2026-11-02',families=['GlobalVariables','level Variables'],profile=attest['profile'],primary=attest,
        editor_array=dict(context=0x54F550,double_vector_offset=0x6A0,double_count_offset=0x6A4,double_capacity_offset=0x6A8,
            ID_vector_offset=0x6B4,ID_count_offset=0x6B8,ID_capacity_offset=0x6BC,
            slot_kind='binary64',index='RawVariableType directly indexes vector, not ordinal in sortedIDlist.',
            field320_false_lead='48131F writes vector[100],5.0 in defaults; not TerrainModel320/raw224. Plain248doublevector allocation/zeroing and registeredVariablescallback establish type.'),
        numeric_loading=dict(initializer=0x47FFB0,record_callback=0x49EC90,
            input_scope='Prepared16-byteMIDprefix followedbyactual16-byteVariablepayload. Wholefileparser/headercallback/GUI notexecuted.',
            accepted_IDs=keys,scaled_IDs=[67,155,158,185,220,221],
            conversion='Signed32value, binary64; divide4096 for6specialIDs, otherwise unscaled integer. ExtraDWORDs notusedinselectednumericpaths.',
            membership='193-element sortedIDarray writtenbyinitializer; callback search acceptance staticallyattested. OnlyselectedIDs executed, not193inputtests.',
            reinitialize='Defaultdoublewrites skippedwhen currentdoublecount>0; selected loaded100=42 preserved aftersecondwholeinitializer. IDlist maystillbeinitialized.',
            tested_ignored_IDs=[19,29,186,242,43],
            ignored_scope='Wholecallbackreturn1 andnumericarrayunchanged inselectedscenes; type43notinterpretedhere. No claimwholefilesaver dropsrecipes.',
            unknown233=dict(accepted=True,default_editor_model=values[233],gameplay_meaning='unresolved',native_descriptive_name=None)),
        default_table=dict(values=values,assigned_DWORD_offsets=assigned,unassigned_slots='Zeroedbywholeoriginalarrayhelper beforeliteralstores.',
            oracle='Independentconstant/pointerpropagationthroughoriginalstraightlineMOVstores4800E7..480C96 and480DCA..481CC4; bothentirearrays compared.',
            scope='HardcodedoriginalPro fallback/initial values, not currentGlobalVariablesdata or automaticallyactivegamesettings.',
            illustrative_defaults=[dict(type=t,editor_default=values[t]) for t in [3,8,100,113,114,155,158,185,220,221,233]],
            possession_note='113/114fallback50 doesnotprove50percentbonus; realconsumer/formulastillunresolved.'),
        actual_recipe_control=dict(source=serial['source'],raw_hex=bytes.fromhex(recipe['hex'])[16:32].hex(),scene=source_recipe,
            interpretation='Actualfirsttype43recordfromsourcepassedunchangedtooriginalcallback; no numericdefaultmutation. Fullrecipeevent/wholefilewriter notexecuted.'),
        native_executions=manifest,independent_Ghidra_controls=checks,storage_validation=serial,preservation=preservation,counts=counts,
        helpers=[link(n) for n in ['probe_editor_global_defaults.py','emulate_editor_global_variables.py','crosscheck_editor_global_variables.py',
            'prepare_editor_variables_primary.py','validate_editor_variable_storage.py','native_editor_text_executor.py']],
        limitations=['ExactProSHA only; no newDX/RC3/oldgame behavior tested.',
            'Preparedcontext/CRT/heap/IATproviders andMIDenvelope. Whole47FFB0/49EC90execute; outerfileparser, originalwriter, GUI/Worldnotexecuted.',
            'Twofullarrays independentlypredicted; selectedremainingmemory comparedbyGhidra butnotallbaselinefields semanticallydecoded.',
            '6scaledIDs proveninone6-recordscene; acceptedIDlist193 isstaticrule, not193separatenativecases.',
            'Ignoringtype43inthiscallback isnotproofrecipe-lossbywholeeditor. StructuredVariables andallcallbacksoutsidechosennumericbranchesnotcovered.',
            'Hardcodeddefaultsareeditorinitialvalues, not livegamebalance orproofpossessionformula. Unknown233purpose stillunresolved.',
            'Sevenfreshfunctionextents include2staticregistration/header functions; theirwholebodies notexecuted.',
            '8newexactedits ofoneactualmixedcontainer; no newnativewholefileparse/serialization claim.',
            'Prior144componentsandphysicalcodec preserved. Current420sourcebaseline/7EXErechecked;4preexistingmapdifferences retained fromphase165. Other15familyunknowns remain.'])

def main():
    p=load('editor-format-package.json');snapshot=ROOT/'phase166-prior-components.json'
    if not snapshot.exists():
        assert len(p['components'])==144;dump(snapshot.name,deepcopy(p['components']))
    s=build();dump(SCHEMA,s);old=load(snapshot.name)
    assert set(p['components']) in [set(old),set(old)|{KEY}]
    for k,row in old.items():assert p['components'][k]==row
    p[KEY]=s;p['components'][KEY]=link(SCHEMA)
    note='editor_variable_loading_semantics phase166: originalPro default248doubles/193IDs andregistered49EC90load, signed32/6scaledIDs,16native/4wholeGhidra/8exactcontaineredits. Unknown233accepted/default40;19/29/186/242ignoredinselectedscenes;actualtype43notinterpretedhere,notproofwholefileloss. 48131F/320 isglobalnumericarray[100],notTerrain. No newopaquegameplaynames; prior144components/codec preserved,420currentbaseline sources/7EXEverified.'
    if note not in p['precedence']:p['precedence'].append(note)
    dump('editor-format-package.json',p);print(json.dumps(s['counts']))

if __name__=='__main__':main()
