"""Publish the phase191 search extension with explicit negative-result bounds."""
from pathlib import Path
from copy import deepcopy
import json
import check_crypt_pointer_forwarding as checks
import scan_crypt_verified_returns as search
import build_recovery_step as storage
ROOT=Path(__file__).resolve().parent
KEY,SCHEMA='crypt_pointer_forwarding_semantics','crypt-pointer-forwarding-semantics.json'
sha=checks.sha
load=checks.load
dump=checks.dump
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['scan_edx_crypt_pointers.py','scan_crypt_pointer_returns.py','scan_crypt_verified_returns.py',
         'check_crypt_pointer_forwarding.py','build_crypt_pointer_forwarding.py',
         'scan_deep_crypt_pointers.py','constant_branch_domain.py','native_shot_pair_trace.py']


def primary(traces):
    data=checks.EXE.read_bytes();proof=[]
    for address,memory,assembly in [
        (0x4C5F50,'ghidra/phase168-primary-4c5f50-memory.txt','ghidra/phase168-primary-4c5f50-assembly.txt'),
        (0x4DC7E0,'ghidra/phase157-primary-4dc7e0-memory.txt','ghidra/phase157-primary-4dc7e0-assembly.txt')]:
        raw=bytes.fromhex(search.HEX[address]);saved=load(memory)
        assert bytes(saved['data'])==bytes.fromhex(saved['hex'])==search.pe_read(data,address,len(raw))==raw
        assert saved['length']==len(raw)
        listing=(ROOT/assembly).read_text();assert listing[:8].lower()==f'{address:08x}'
        proof.append(dict(address=hex(address),length=len(raw),scope='complete_function',sha256=sha(raw),
                          memory=link(memory),assembly=link(assembly)))
    pcs={int(pc,16) for trace in traces for pc in trace['visited_instructions'] if 0x400000<=int(pc,16)<0x800000}
    assert all(any(int(p['address'],16)<=pc<int(p['address'],16)+p['length'] for p in proof) for pc in pcs)
    assert len(pcs)==13,len(pcs)
    return dict(program_sha256=sha(data),extents=proof,unique_executed_original_PCs=len(pcs),
                provenance='Reuse of frozen Ghidra byte captures, independently compared against current supplied PE; not fresh Ghidra byte queries.')


def build():
    baseline=load('phase165-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:
        assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    prior=load('phase191-prior-package-state.json')
    assert len(prior['components'])==168
    for ref in prior['components'].values():assert link(ref['file'])==ref
    edx=load('phase191-edx-search-delta.json');returns=load('phase191-verified-return-delta.json')
    for delta in [edx,returns]:
        assert not delta['new_candidates'] and not delta['new_indexed_events'] and not delta['lost_candidates'] and not delta['lost_indexed_events']
        assert delta.get('old_candidates_retained',delta.get('retained_candidates'))==33
    for ref in [edx['previous'],edx['current'],edx['helper'],edx['frozen_helper'],returns['current'],returns['prior']]:
        assert link(ref['file'])==ref
    assert edx['summary_counts']==dict(root_states=9961,limited_roots=1,callee_summaries=199,limited_callees=12)
    assert returns['counts']==dict(root_states=10057,limited_roots=1,callee_summaries=199,limited_callees=12)
    old=load(edx['previous']['file']);new=load(returns['current']['file'])
    assert old['callee_summaries']==164 and len(old['producer_roots'])==len(new['producer_roots'])==539
    assert len(new['candidates'])==33 and len(new['indexed_events'])==84
    hints=load('phase191-pointer-return-leads.json');assert hints['EDX_search_exactly_reproduced'] and hints['return_contexts']==13
    assert hints['helper']==link(hints['helper']['file'])
    controls=load('phase191-pointer-controls.json')
    assert controls['synthetic_controls']==checks.synthetic() and len(controls['synthetic_controls'])==5
    runs=[];traces=[]
    for c,row in zip(checks.cases(),controls['original_PE_scenes']):
        _,saved,out=checks.execute(c);assert row==out;runs.append(row);traces.append(saved['result'])
    assert len(runs)==4
    cross=controls['independent_Ghidra_register_controls'];assert len(cross)==4
    for c,row in zip(checks.cases(),cross):
        assert c==row['case'] and link(row['file'])['sha256']==row['sha256']
        saved=load(row['file']);r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert int(r['registers']['EAX'],16)==checks.OWNER+search.CONTRACTS[c['entry']]
        assert int(r['registers']['ECX'],16)==(checks.OWNER if c['entry']==0x4C5F50 else 0)
    proof=primary(traces)
    counts=dict(typed_hint_roots=539,previous_callee_contexts=164,EDX_callee_contexts=199,additional_callee_contexts=35,
        previous_root_states=9961,root_states_with_returns=10057,additional_root_states=96,
        counter_candidate_witnesses_preserved=33,indexed_witnesses_preserved=84,new_counter_candidate_witnesses=0,
        limited_roots=1,limited_callees=12,verified_pointer_arithmetic_return_contracts=2,
        synthetic_analyser_controls=5,native_PE_scenes=4,native_exact_owner_bytes=4*8192,
        independent_Ghidra_register_controls=4,independent_Ghidra_memory_comparison_bytes=0,
        primary_extents=2,unique_executed_original_PCs=proof['unique_executed_original_PCs'],
        new_raw_fields=0,new_raw_edits=0,new_counter_meanings=0,source_writes=0,
        prior_components_preserved=168,source_files_preserved=420,EXE_paths_preserved=7,
        Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10)
    return dict(phase=191,status='verified_bounded_search_extension_no_additional_counter_meanings',counts=counts,
        profile=dict(file=str(checks.EXE),sha256=sha(checks.EXE.read_bytes())),
        searches=dict(EDX_delta=link('phase191-edx-search-delta.json'),verified_return_delta=link('phase191-verified-return-delta.json'),
                      return_hints=link('phase191-pointer-return-leads.json'),
                      transfer='Frozen178 plus EDX call argument collection and exactly two byte-attested EAX arithmetic returns; no arbitrary EAX survivors promoted to a contract.',
                      limits=edx['limits'],retained_limited_contexts=[dict(target=c['target'],registers=c['registers'],arguments=c['arguments'],states=c['summary']['states'],limited=c['summary']['limited']) for c in new['limited_contexts']]),
        return_contracts=[dict(entry=hex(a),receiver='ECX',return_register='EAX',returned_offset=v,
                             meaning='Arithmetic address relative to input receiver; receiver type not proved by this contract.') for a,v in search.CONTRACTS.items()],
        diagnostic_return_hints=dict(contexts=13,not_universal_contracts=True,
            rejected_example='4FAC40 first stack argument reaches EAX only on observed null-return path. It cannot certify a non-null Owner-pointer return.',
            other_hints='Earlier root witnesses in48FA70/4B32A0/4D8E60/4DA560 are conditional/midfunction; they remain hints, not whole-function contracts.'),
        primary=proof,native_executions=runs,independent_Ghidra_register_controls=cross,
        analyser_controls=controls['synthetic_controls'],source_baseline=link('phase165-source-baseline.json'),
        storage=dict(new_raw_fields=0,new_raw_edits=0,rechecked=storage.storage()),helpers=[link(n) for n in HELPERS],
        priority=dict(current='Crypt groups',block_complete=False,next=[
            'Ten unassigned runtime counter gameplay consumers; unknown pointers, storage aliases and indirect dispatch remain open.',
            'Full naturally scheduled lifecycle and further Crypt interruptions; conditional return witnesses require caller-specific validation.',
            'Globals remain the next user-priority block after Crypt; no readiness percentage increase.']),
        limitations=['Bounded static search, not proof of global absence of consumers.',
            'Only two arithmetic return contracts; unknown/indirect return values and pointer-escaped memory remain unresolved.',
            'Entity+2A roots are type hints; no Owner type is inferred merely from a numeric offset.',
            'Four original scenes have exact8192-byte output verification. Four independent Ghidra controls compare EAX/ECX only, not memory.',
            'Five synthetic controls test the search model, not original gameplay.',
            'This stage adds no KWD field/API or counter bonus; all15 families and Crypt goal remain active.'])


NOTE='crypt_pointer_forwarding_semantics phase191: EDX adds35callee contexts(164->199); two PE-attested arithmetic returns add96rootstates.539roots/33counter/84indexed witnesses retained, no newcountermeaning;1root/12callee limits.5synthetic/4original exact8192B/4Ghidra register-only controls,2spans/13PC.168priorcomponents/420sources/7EXE/codec preserved.10countermeanings remain open.'


def main():
    package=load('editor-format-package.json');p=ROOT/'phase191-prior-package-state.json'
    if not p.exists():
        assert len(package['components'])==168
        dump(p.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),
             package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
             preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    previous=load(p.name)
    for key,digest in previous['preserved_top_level'].items():assert canonical(package[key])==digest
    assert set(package['components']) in [set(previous['components']),set(previous['components'])|{KEY}]
    schema=build();dump(SCHEMA,schema);package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase191-search-status.json',dict(phase=191,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='progress',reason='Cleaned1599obsoletefiles/regenerablecache; compressed retained evidence and passed phase190 audit.'),
        current_turn=dict(classification='progress',reason='Extended bounded search via EDX and two byte-attested pointer returns; verified original execution and retained explicit unresolved consumer scope.'),
        counts=schema['counts'],remaining=schema['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))


if __name__=='__main__':main()
