"""Bounded negative extraFlag/Crypt search and original loop-bound correction."""
from pathlib import Path
from collections import Counter
import json
import dk2_formats as fmt
import emulate_crypt_counter_boundary as native
import crosscheck_crypt_counter_boundary as controls
import prepare_unknown_search_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def corpus():
    refs=load('creature-lookup-review.json')['corpus']['files'];assert len(refs)==82
    total=enabled=0;examples=[]
    for row in refs:
        p=Path(row['file']);data=p.read_bytes();assert sha(data)==row['sha256']
        for s in fmt.parse(data):
            if s.signature!=0xAA:continue
            for index,raw in enumerate(s.records):
                assert len(raw) in [5449,5537];total+=1;flags=int.from_bytes(raw[0x1545:0x1549],'little');enabled+=bool(flags&2)
                if flags&2 and len(examples)<8:examples.append(dict(file=str(p),record_index=index,raw_id=raw[0xF4B],GameExtraFlags=flags))
    return dict(files=refs,records=total,bit2_set_records=enabled,examples=examples,new_raw_patches=0,
        meaning='Observedbit values do not establish a gameplaymeaning or historicalwriterorigin; no edits tosourcefiles.')

def build():
    attested=primary.build();assert attested==load('phase149-unknown-search-primary.json')
    extra=load('phase149-extra-creature-flags-leads.json');owner=load('phase149-owner-crypt-pointer-leads.json')
    assert extra['program_sha256']==owner['program_sha256']==attested['profile']['sha256']
    flagreads={r['read']['address']:r for r in extra['candidates'] if r['field']=='GameExtraFlags'}
    expected={'0x478b8e':4,'0x478c3e':4,'0x478ce9':4,'0x48b879':4,'0x4af28b':1,'0x4dc064':8}
    assert set(flagreads)==set(expected)
    for pc,mask in expected.items():assert flagreads[pc]['read']['instruction'].endswith(', '+str(mask))
    false=[h for h in owner['candidates'] if h['read']['address']=='0x4c7368'];assert len(false)==12
    assert all(h['field']=='CryptCounter01' and h['read']['offset']==0x108B and h['read']['callee']=='0x4c7360' for h in false)
    # The actual body is independently attested and its concrete loop bound
    # ischecked by completeCPU runs; pointer-onlyCFG paths are not evidence.
    asm=(ROOT/'ghidra/phase149-primary-4c7360-assembly.txt').read_text().lower()
    for s in ['add ecx,0xe97','add ecx,0x19','cmp eax,0x3','jc 0x004c7368']:assert s in asm
    assert 0xE97+20*0x19==0x108B and 0xE97+2*0x19==0xEC9
    manifest=load('phase149-crypt-counter-boundary-emulations.json');rows=manifest['replays'];assert len(rows)==8
    for c,row in zip(native.cases(),rows):
        args,checks,saved,p=native.execute(c);out=native.verify(c,args,checks,saved['result'])
        assert row['case']==c and row['outcome']==out and row['file']==str(p.relative_to(ROOT)) and row['sha256']==sha(p.read_bytes())
    cross=load('phase149-crypt-boundary-crosschecks.json')['replays'];assert [r['case'] for r in cross]==controls.CONTROLS
    for row in cross:
        args,wanted=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes())
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and row['checked_registers']==['EAX','ECX','ESP']
        assert all(int(r['registers'][k],16)==int(wanted[k],16) for k in row['checked_registers'])
    values=corpus();assert values['records']==3734
    preservation=load('shot-lifecycle-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    before=ROOT/'phase149-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==127
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==127
    summarize=lambda d:dict(decoded_instructions=d['decoded_instructions'],producer_roots=len(d['producer_roots']),root_states=d['root_states'],limited_roots=d['limited_roots'],
        callee_summaries=d['callee_summaries'],limited_callees=d['limited_callees'],limited_argument_roots=sum(r['limited_argument_contexts'] for r in d['producer_roots']),
        indexed_events=len(d['indexed_events']),candidate_counts=dict(Counter(r['field'] for r in d['candidates'])))
    return dict(phase=149,date='2026-10-31',families=['Creatures','Rooms'],profile=attested['profile'],primary=attested,
        extra_flags=dict(raw_offset=0x1545,definition_offset=0x68A,mask_under_investigation=2,legacy_label='StoneKnight',
            conclusion='No newbit2 consumer established. Sixuniqueproducer-anchoredreadsites test only1/4/8. Legacylabel isnot validated as gameplaybehavior; keepunknown.',
            search=summarize(extra),read_sites=[dict(address=pc,mask=mask,root_hint=flagreads[pc]['root']['function_hint']) for pc,mask in expected.items()],
            evidence=link('phase149-extra-creature-flags-leads.json'),corpus=values),
        crypt_counter_search=dict(search=summarize(owner),evidence=link('phase149-owner-crypt-pointer-leads.json'),
            known_gameplay_counter_offsets=[0x108C,0x108D,0x108E,0x108F,0x1090,0x1091,0x1094,0x1095],
            remaining_counter_offsets=[0x108A,0x108B,0x1092,0x1093,0x1096,0x1097,0x1098,0x1099,0x109A,0x109B],
            conclusion='No ninthCryptgroup assigned. NewCounter01 leadat4C7368 rejected byconcretebound; earlier8gameplayassignments unchanged.'),
        rejected_lead=dict(instruction=0x4C7368,function=0x4C7360,invented_owner_offset=0x108B,candidate_chains=len(false),
            error='Pointer-onlywalker explored theloopedge without constraining theEAX counter; generated20 increments although originalunsignedEAX<3 onlypermits3reads.',
            actual_read_offsets=[0xE97,0xEB0,0xEC9],actual_loop_bound=3,invented_loop_index=20,
            actual_contract='Return1 atfirst nonzerobyte amongthree; return0 whenallthreezero. No writes toownerbuffer. Broadermeaning ofthesethreeflags is not assigned.',
            native_controls='Changingowner108B through0/1/127/255 doesnotchange fullhelperresult/readaddresses. Activatingeachrealcheckedbyte changesresult to1; highbit128 isalso nonzero.',
            relation_to_data='Thefalselead is a researchanalyzerartifact, not evidence that originaleditoror game misuses aCryptcounter.',
            disposition='Do not labelUniqueWarlock counter108B as a3-slot bonus based onthisread.'),
        native_executions=manifest,independent_Ghidra_controls=cross,preservation=preservation,
        counts=dict(native_scenarios=8,independent_Ghidra_CPU_controls=4,Ghidra_register_controls_only=True,original_function_extents=len(attested['functions']),
            executed_function_extents=sum(not r['static_only'] for r in attested['functions']),static_function_extents=sum(r['static_only'] for r in attested['functions']),
            unique_executed_original_PCs=attested['unique_executed_original_PCs'],rejected_false_candidate_chains=len(false),newly_named_opaque_fields=0,
            crypt_groups_assigned_total=8,crypt_groups_remaining=10,new_raw_patches=0,preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0),
        helpers=[link(n) for n in ['scan_extra_creature_flags.py','scan_crypt_owner_pointers.py','emulate_crypt_counter_boundary.py','crosscheck_crypt_counter_boundary.py','prepare_unknown_search_primary.py']],
        limitations=['ExactsuppliedDX; otherprofiles/DLL/runtimepatches not newlysearched.',
            'Producer roots andfunctionhints are leads requiringprimarypointeridentity verification; directcall forwarding limited to3levels, statecapsandcontextcaps recorded.',
            'Extrabit2 scan leaves1limitedcallee; ownercounter scan leaves6limitedcallees. No globalnonuse or deadfieldproof.',
            'Genericindexedreads andarbitrarypointerreturns notexhausted. Originalpointerwalker stilldoesnotconstrainloopcounters; thisspecificfalselead isexplicitlyquarantined, not a universalwalkerfix.',
            'Only4C7360 executesnewly; other6wholefunctions provide staticbyte/mask evidence. GhidracontrolscompareEAX/ECX/ESP, not a fullindependentmemorydump.',
            'Ghidrausesanexplicitreturntrampoline toitsDEADBEEF sentinel; initialINT3stopfixture excluded. No gameplayreplacement orsourcebinarypatch.',
            'No newCreatureextraFlag2 orremaining10Cryptgroup meaning established; rawdata andallpriorsemanticviews unchanged; readiness about75%.'])

if __name__=='__main__':
    d=build();(ROOT/'unknown-search-review.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(d['counts']));print('Corpusbit2',d['extra_flags']['corpus']['bit2_set_records'],'/',d['extra_flags']['corpus']['records'])
