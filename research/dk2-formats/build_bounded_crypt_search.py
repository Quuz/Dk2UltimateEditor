"""Publish bounded Crypt searches and reject loop artifacts without naming bonuses."""
from pathlib import Path
from collections import Counter
import json
import emulate_owner_table_boundary as native
import crosscheck_owner_table_boundary as control
import prepare_bounded_crypt_primary as primary
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attest=primary.build();assert attest==load('phase157-bounded-crypt-primary.json')
    manifest=load('phase157-owner-table-emulations.json');assert len(manifest['replays'])==11
    for c,row in zip(native.cases(),manifest['replays']):
        args,ch,d,p=native.execute(c);_,out=native.verify(c,args,ch,d['result']);assert row['case']==c and row['outcome']==out and row['sha256']==sha(p.read_bytes())
    cross=load('phase157-owner-table-crosschecks.json')['replays'];assert len(cross)==5
    for row in cross:
        c=next(c for c in native.cases() if c['name']==row['case']);args,n,size=control.build(c);d=load(row['file']);r=d['result']
        assert d['input']==args and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    result=load('phase157-bounded-owner-crypt-leads.json');previous=load('phase149-owner-crypt-pointer-leads.json')
    assert result['constant_domain_sha256']==sha((ROOT/'constant_branch_domain.py').read_bytes()) and result['program_sha256']==attest['profile']['sha256']
    for c,expected in [('numeric_control',[0xE97,0xEB0,0xEC9]),('numeric_DEC_control',[0xB57+18*i for i in range(46)])]:
        assert not result[c]['limited'] and sorted(set(r['offset'] for r in result[c]['reads']))==expected
    key=lambda h:(h['root']['address'],h['field'],h['read']['address'],h['read']['offset'],h['read']['callee'],h['read']['call'])
    old={key(h) for h in previous['candidates']};new={key(h) for h in result['candidates']}
    removed=old-new;assert len(removed)==12 and not new-old
    assert all(r[1]=='CryptCounter01' and r[2]=='0x4c7368' for r in removed)
    assert not any(h['read']['address'] in ['0x4c7368','0x4dc7f5'] for h in result['candidates'])
    unknown={f'CryptCounter{i:02d}' for i in [0,1,8,9,12,13,14,15,16,17]}
    assert all(0x4D8E60<=int(h['read']['address'],16)<0x4D93E0 for h in result['candidates'] if h['field'] in unknown)
    oldroots={r['address'] for r in previous['producer_roots']};assert oldroots<={r['address'] for r in result['producer_roots']}
    oldcomponents=load('phase157-prior-components.json');assert len(oldcomponents)==135
    for row in oldcomponents.values():assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    priorreview=load('unknown-search-review.json')
    for row in priorreview['helpers']:assert sha((ROOT/row['file']).read_bytes())==row['sha256']
    preservation=load('ap-trigger-tree-semantics.json')['preservation']
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    return dict(phase=157,date='2026-10-31',families=['Creatures','Rooms.Crypt'],profile=attest['profile'],primary=attest,
        conclusion='No ninthCryptbonusassigned. Loop-awaresearchremoves12oldfalsechainsand retainsall33othercandidatechains. Newbroadtag/stridehintsdonotproduceadditionalcounterreads; globalnonuseisnotproved.',
        numeric_domain=dict(file='constant_branch_domain.py',sha256=sha((ROOT/'constant_branch_domain.py').read_bytes()),
            rule='Track provenintegerconstants separatelyfrompointeroffsets. CMP/TEST andknownarithmetic/DEC/INC flagsselectonlyprovenbranches. Memoryloadsunknown;unmodeledwritesinvalidateconstants/flags. Partialflags needonlybranch-relevantknownbits.',
            controls='Original4C7360CMP-bound3 and4DC7E0DEC-bound46 pointer-readsets exactlymatchnativecontracts. Thisisnotproofallx86instructionsorallsearchpathsmodeled.',
            legacy='Historicalpointer-onlyscanner retainedbyteexact; newscannerdoesnotrewriteoldevidence.'),
        search=dict(evidence=link('phase157-bounded-owner-crypt-leads.json'),decoded_instructions=result['decoded_instructions'],
            producer_roots=len(result['producer_roots']),root_types=dict(Counter(r['producer'] for r in result['producer_roots'])),
            old_root_hints_retained=len(oldroots),root_states=result['root_states'],limited_roots=result['limited_roots'],
            callee_contexts=result['callee_summaries'],limited_callee_contexts=result['limited_callees'],indexed_events=len(result['indexed_events']),
            remaining_candidate_chains=len(result['candidates']),removed_false_chains=len(removed),new_candidate_chains=len(new-old),
            removed_chains=[list(x) for x in sorted(removed)],global_nonuse_proved=False,
            remaining_unknown_offsets=[0x108A,0x108B,0x1092,0x1093,0x1096,0x1097,0x1098,0x1099,0x109A,0x109B],
            classification='Allremainingreadsto10unknowncountergroups occurwithinpreviouslynativesetter4D8E60; noadditionalgameplayconsumerassigned.'),
        regression_three=dict(function=0x4C7360,read_offsets=[0xE97,0xEB0,0xEC9],analyzer_states=result['numeric_control']['states'],
            existing_native_evidence=link('phase149-crypt-counter-boundary-emulations.json'),existing_Ghidra_evidence=link('phase149-crypt-boundary-crosschecks.json'),
            boundary='Earlier8native/4Ghidracontextregistercontrolspreservedandnotrerunorcountedasnew. Existingfullownerbuffer/no-write/read-addressobservationsarethereference.'),
        bounded_table=dict(function=0x4DC7E0,entry_count=46,stride=18,comparison_byte_offset=0xB57,first_DWORD_write_offset=0xB65,
            header_byte_write=0xB47,last_comparison_byte_offset=0xE81,last_possible_DWORD_write_offset=0xE8F,
            parameter='low8ofstackargument; highargumentbitsignored',value_written=0xA000,
            rule='AlwayswriteparameterBYTEtoheaderB47,thenforeach46recordscompareBYTEB57+18i. OnmatchwriteDWORDB65+18i=A000; allotherbytesunchanged.',
            native_scope='11wholeoriginalhelpercalls;nomemory/code/vtablesubstitution. Preparedownerbuffer+32guardbytes andcallerstack; notrawload/death/AI/world.',
            excluded_Crypt108B='Toaliascomparisonbyte108B requiresi74: B57+74*12hex=108B. Actuali0..45neverreachit.',
            caller=dict(function=0x45A0D0,callsite=0x45A10E,tag_load=0x45A107,
                static_identity='Creaturepointerfromstate+8; typeBYTE36F passedasarg,ownerTAGWORD28indexesglobal6EE540. Nativecallerbodyattestedbutnotexecuted.',
                boundary='No newbroadermeaningofowner46recordtableorrawfieldassigned. Rootisverifiedhere;other2611tagrootsremainuntypedhints.')),
        native_executions=manifest,independent_Ghidra_controls=cross,preservation=preservation,
        serialization=dict(new_raw_edits=0,codec_or_effective_schema_changes=0,inherited_AP_storage=link('phase156-AP-tree-storage-validation.json'),
            boundary='No newfieldmappingsoformatAPIunchanged. Prior852AP/triggereditsarenotrepeatedorcountedasnew; sourcehashesverified.'),
        counts=dict(native_scenarios=11,independent_full_Ghidra_controls=5,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            actual_record_comparisons=46*11,original_code_extents=len(attest['functions']),static_only_extents=sum(r['static_only'] for r in attest['functions']),unique_executed_original_PCs=attest['unique_executed_original_PCs'],
            producer_roots=len(result['producer_roots']),root_states=result['root_states'],callee_contexts=result['callee_summaries'],limited_roots=result['limited_roots'],limited_callee_contexts=result['limited_callees'],
            removed_false_candidate_chains=12,retained_candidate_chains=33,newly_named_opaque_fields=0,crypt_groups_assigned_total=8,crypt_groups_remaining=10,new_raw_edits=0,
            preserved_source_files=len(preservation['source_files']),preserved_executable_paths=len(preservation['executable_paths']),source_writes=0),
        helpers=[link(n) for n in ['constant_branch_domain.py','scan_bounded_crypt_pointers.py','emulate_owner_table_boundary.py','crosscheck_owner_table_boundary.py','prepare_bounded_crypt_primary.py']],
        limitations=['ExactsuppliedDXonly. Expandedglobaltag/strideproducerhintsarenotgenerallytypedowners; eachnewcandidatewouldneedprimaryidentityproof.',
            '43root/49calleecontextlimitsremain. Directcallforwardingdepth3,256contexts/root; arbitraryindirectcalls/pointerreturns/implicitstringaccesses/unknownindexedrangesnotexhausted.',
            'Legacywalkstackmodelstillresetsstacktrackingaftercall;thisphasefixesnumericloopbounds,notallABIorstack-spilllimitations. No globalabsenceproof.',
            'Only4DC7E0executesnewly;45A0D0and4C7360attestedstaticallyhere. Former8native/4Ghidra3-entrychecksnotnewruns.',
            'No newCryptbonus/rawpropertymeaning. E8/E9CreatureSpellordinaryloadboundarywasreviewedfromexistingphase55evidence,notclaimednew.',
            'All15-familyunknownsandgoalremainopen; readinessabout75%.'])

def main():
    s=build();(ROOT/'bounded-crypt-search-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=load('editor-format-package.json');old=load('phase157-prior-components.json');assert set(p['components']) in [set(old),set(old)|{'bounded_crypt_search_semantics'}]
    for k,row in old.items():assert p['components'][k]==row
    p['bounded_crypt_search_semantics']=s;p['components']['bounded_crypt_search_semantics']=link('bounded-crypt-search-semantics.json')
    note='bounded_crypt_search_semantics phase157 tracksprovennumericCMP/DECbranches separatelyfrompointeroffsets.3171producerhints/195046rootstates/690calleecontexts;43/49limitsremain.12false4C7368->Crypt108Bchainsremoved,33otherchainsretained,newgameplaycounterconsumers0. Original4DC7E0checks46recordsB57+18i,lastE81,writesA000tomatchingB65+18iandheaderB47;11native/5wholeGhidracontrols. NewCryptgroups0,rawedits0,8known/10open.135priorschemas/effective/API/source420/7EXEpreserved; nofullglobalnonuseclaim.'
    if note not in p['precedence']:p['precedence'].append(note)
    (ROOT/'editor-format-package.json').write_text(json.dumps(p,ensure_ascii=False,indent=2),encoding='utf8');print(s['counts'])
if __name__=='__main__':main()
