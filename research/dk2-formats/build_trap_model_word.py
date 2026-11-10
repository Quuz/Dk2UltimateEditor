"""Publish original Trap model/save preservation and the current atomic WORD view."""
from pathlib import Path
import json
import emulate_trap_model_chain as native
import crosscheck_trap_model_chain as controls
import prepare_trap_model_primary as primary
from native_trap_field_layout import effective_trap_layout
from validate_trap_word_layout import validate
ROOT=Path(__file__).resolve().parent
sha=native.sha
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))
def build():
    attested=primary.build();assert attested==load('phase146-Traps-model-primary.json')
    manifest=load('phase146-Traps-model-emulations.json');rows=manifest['replays'];assert len(rows)==6 and len({r['file'] for r in rows})==6
    for c,row in zip(native.cases(),rows):
        fixture,saved,p=native.execute(c);out=native.verify(c,fixture,saved['result'])
        assert row['case']==c and row['outcome']==out and row['sha256']==sha(p.read_bytes()) and row['file']==str(p.relative_to(ROOT))
    cross=load('phase146-Traps-model-crosschecks.json')['replays'];assert len(cross)==2
    for row in cross:
        args,n,size=controls.build(row['case']);d=load(row['file']);r=d['result']
        assert args==d['input'] and row['sha256']==sha((ROOT/row['file']).read_bytes()) and (n,size)==(row['checked_regions'],row['checked_bytes'])
        assert not d['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase146-Trap-WORD-api-validation.json')
    assert edits['historical_codec']['sha256']=='45147811d5a2894263ba1a32c53ffcd10075001fa6cf0e85ac0dcbacb452461f'
    prior_pres=load('room-wander-semantics.json')['preservation'];sources={str(Path(r['file'])).lower():r for r in prior_pres['source_files']}
    for row in edits['files']:
        key=str(Path(row['file'])).lower()
        if key in sources:assert row['sha256']==sources[key]['sha256']
        sources[key]={k:row[k] for k in ['file','sha256']}
    preservation=dict(source_files=list(sources.values()),executable_paths=prior_pres['executable_paths'],
        previous_source_roster_count=len(prior_pres['source_files']),added_historical_Trap_sources=len(sources)-len(prior_pres['source_files']))
    for row in preservation['source_files']+preservation['executable_paths']:assert sha(Path(row['file']).read_bytes())==row['sha256']
    before=ROOT/'phase146-prior-components.json'
    if not before.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==124
        before.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(before.name))==124
    return dict(phase=146,date='2026-10-31',families=['Traps.kwd','Trap records inside other KWD containers'],profile=attested['profile'],primary=attested,
      field=dict(name='Unknown204',raw_offset=0x204,width_bytes=2,intermediate_offset=0x2AC,editor_model_offset=0x238,inherited_game_definition_offset=0x98,
        storage='One native16bit value; all bits preserved. u16 is a raw display/edit contract, not a native gameplay signedness claim.',
        meaning='Unresolved. No ManaGen or upkeep label assigned.',
        original_converter_instructions=[0x431607,0x431611],original_model_instructions=[0x4A1926,0x4A192D],original_writer_read=0x4A90B5,
        inherited_game_storage=link('trap-loading-semantics.json')),
      full_chain=dict(converter=0x431530,intermediate_bytes=747,callback=0x4A1770,model_constructor=0x425C20,allocated_model_bytes=744,
        registration_array=0x54FA40,writer=0x4A8F40,writer_context=0x54F550,record_bytes=579,file_header_bytes=56,
        steps='Raw records -> original converter -> original malloc/CString/resource model constructor -> original catalogue registration/growth -> native calendar/whole writer -> bounded CFile sink.',
        initialized_scope='Frozen phase127 CRT heap/CString pools/TLS/clock/IAT environment. MID zero-seeded; outer parser initializer, MFC GUI/application bootstrap and OS filesystem not executed.',
        observed='All579bytes of16original records and5diagnostic copies returned unchanged; output headers/sizes/counts valid, raw/MID guards and output guards preserved.'),
      boundary_values=[0,1,32767,32768,65535],
      codec=dict(current=edits['current_codec'],historical=edits['historical_codec'],
        current_field='Unknown204:u16',historical_fields=['unknown4[0].unknown4:u8','unknown4[1].unknown4:u8'],
        change='Current decoded/set_scalar view exposes the native WORD atomically. LAYOUTS remains historical and frozen phase145 codec preserves old API shape.',
        compatibility='Legacy byte aliases are no longer separate current fields. Use Unknown204 or the explicitly frozen historical codec for old dumps.',
        not_a_legacy_corruption_bug=True,
        signed_display='FFF6 may be shown as unsigned65526 or signed-10; storage tests establish neither gameplay interpretation.'),
      effective_trap_layout=effective_trap_layout(),native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,
      counts=dict(native_scenarios=6,original_source_records_in_complete_chain=16,diagnostic_records_in_complete_chain=5,total_raw_model_chains=21,complete_writer_runs=6,
        independent_Ghidra_CPU_controls=2,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
        additional_original_function_extents=len(attested['functions']),inherited_original_function_extents=len(attested['inherited_common']['reused_function_addresses']),
        unique_executed_original_PCs=attested['unique_executed_original_PCs'],original_constant_table_blocks=len(attested['blocks']),
        new_exact_container_edits=edits['new_exact_container_edits'],public_decode_rebuild_controls=edits['public_decode_rebuild_controls'],source_Trap_records=edits['source_records'],
        preserved_source_files=len(sources),preserved_executable_paths=len(preservation['executable_paths']),newly_named_opaque_gameplay_fields=0,source_writes=0),
      helpers=[link(n) for n in ['emulate_trap_model_chain.py','crosscheck_trap_model_chain.py','prepare_trap_model_controls.py','prepare_trap_model_primary.py',
        'native_trap_field_layout.py','validate_trap_word_layout.py','dk2_formats_phase145.py']],
      limitations=['Exact suppliedPro editor. New runtime gameplay consumers/units/signedness for204 not established; historicalDXstorage remains separate evidence.',
        '16original source records and5diagnostic raw copies execute. Other81containers are checked by publiccodec edits, not fully loaded/saved by original editor.',
        'Prepared native CRT/CString pools/heap, zero-seeded MID, declared Win32 IAT/CFile adapters; no original parser initializer/MFC event/UIcommit/destructors/threading/bootstrap.',
        'Complete record identity is proved; generated fileheader clock/scratch is not sourceheader identity. IndependentCPU comparison explicitly excludes unassigned headerbytes42/43/47.',
        'CRT Ghidra extents can contain disjoint shared tails and embedded data. Unlisted intervals are recorded, not linearly treated as code; all observed originalPCs covered.',
        'CurrentAPI shape groups two old rawbytefields intooneWORD, with historicalcodec retained. No evidence the old byteview itself corrupted data.',
        'Unknown204 meaning and significant remaining15family questions are open; readiness remains about75%. No sourceEXE/game/parallel editor changes.'])
if __name__=='__main__':
    d=build();(ROOT/'trap-model-word-semantics.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(d['counts']))
