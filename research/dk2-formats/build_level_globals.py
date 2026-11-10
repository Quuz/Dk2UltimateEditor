"""Integrate full Level runtime bindings while preserving all earlier components."""
from copy import deepcopy
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_level_globals as native
import native_level_globals as api
import crosscheck_level_globals as cross
import validate_level_globals_storage as storage
from build_effect_semantics import pe_read
ROOT,load,dump,sha=native.ROOT,native.load,native.dump,native.sha
KEY,SCHEMA='level_global_parameters_semantics','level-global-parameters-semantics.json'
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))
canonical=lambda v:sha(json.dumps(v,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['native_level_globals.py','emulate_level_globals.py','crosscheck_level_globals.py','prepare_level_globals_primary.py','validate_level_globals_storage.py','build_level_globals.py','native_level_descriptor.py','native_unicorn_mapreader.py','emulate_tick_rate.py','crosscheck_conversion_score.py']


def static_primary():
    data=native.EXE.read_bytes();rows=[];queries=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for info in sorted((ROOT/'ghidra').glob('phase196-static-*-info.txt')):
        m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',info.read_text(encoding='utf8'));assert m
        a,b=[int(x,16) for x in m.groups()];memory=info.with_name(info.name.replace('-info','-memory'));asm=info.with_name(info.name.replace('-info','-assembly'))
        if not memory.exists():
            queries.append(dict(id=memory.stem,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=b-a+1),print_limit=0));continue
        raw=bytes(json.loads(memory.read_text(encoding='utf8'))['data']);assert raw==pe_read(data,a,b-a+1)
        starts=set(x.address for x in md.disasm(raw,a));actual=set(int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',asm.read_text(encoding='utf8'),re.M))
        # Capstone's WAIT may combine with the following x87 instruction.
        for pc in list(starts):
            ins=next(md.disasm(pe_read(data,pc,15),pc,count=1))
            if ins.mnemonic=='wait':starts.add(pc+ins.size)
        assert actual<=starts,(info.name,[hex(x) for x in actual-starts][:10])
        rows.append(dict(address=a,length=len(raw),bytes_sha256=sha(raw),memory=str(memory.relative_to(ROOT)),memory_sha256=sha(memory.read_bytes()),assembly=str(asm.relative_to(ROOT)),assembly_sha256=sha(asm.read_bytes())))
    if queries:dump('phase196-static-final-memory-queries.json',queries);return None
    assert len(rows)==18
    return dict(functions=rows,scope='Whole original function extents read statically. These file-loader, constructor and World scheduling functions were not executed by phase196 CPU scenes.')


def build():
    old=load('phase196-prior-package-state.json');assert len(old['components'])==173
    for ref in old['components'].values():assert link(ref['file'])==ref
    baseline=load('phase194-source-baseline.json')
    for ref in baseline['source_files']+baseline['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    accepted=load('phase196-Level-emulations.json');assert len(accepted['replays'])==9
    for c,row in zip(native.cases(),accepted['replays']):
        args,ch,saved,file=native.execute(c);post,out=native.verify(c,args,ch,saved['result']);assert canonical(row)==canonical(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
    controls=load('phase196-Level-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,n,size=cross.build(row['case']);saved=load(row['file']);r=saved['result'];assert saved['input']==args and (n,size)==(row['checked_regions'],row['checked_bytes']) and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert not saved['tool_is_error'] and r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    physical=storage.build();assert physical==load('phase196-Level-storage.json')
    primary=load('phase196-Level-primary.json');assert primary['unique_executed_original_PCs']==12745
    static=static_primary()
    if static is None:return None
    counts=dict(native_scenes=9,full_original_Level_callbacks=9,full_original_raw_Level_converters=9,
        independent_Ghidra_controls=3,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        unique_executed_original_PCs=12745,inherited_executed_function_extents=len(primary['inherited_functions']),new_executed_function_extents=len(primary['new_executed_functions']),
        static_load_order_function_extents=len(static['functions']),exact_field_inverse_edits=len(physical['exact_field_edits']),existing_binding_rows=len(api.BINDINGS),new_physical_fields=0,
        prior_components_preserved=173,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0)
    assert counts['Ghidra_compared_regions']==228 and counts['Ghidra_compared_bytes']==590997
    return dict(phase=196,status='full_original_Level_callback_and_selected_runtime_bindings_verified_static_finalpass_order_bounded',profile=primary['profile'],counts=counts,
        precedence='This component upgrades phase195 Level callback from static-only to executed for the stated9 scenes. Earlier evidence remains immutable.',
        entry=dict(converter=0x56C100,callback=0x51E6D0,raw_payload_bytes=25683,intermediate_bytes=25991,MID_base=0x134,source=physical['source']),
        bindings=api.BINDINGS,
        flags=dict(raw_offset=0x638C,raw_bytes=2,MID_offset=0x64C0,runtime_address=0x6F2957,runtime_bytes=4,
            formula='(previous_runtime & ~0x7FF) | ((raw_flags >> 2) & 0x7FF)',
            converter_seed='Original converter ORs its existing MID flags. Fixture explicitly seeds only this WORD0; opaque MID bytes remain A5.',
            ignored_here='Raw bits0,1,13..15 are not applied by this callback; preserve them on disk. This is not proof they are globally unused.'),
        narrowing=dict(field='SpeechHornyId',raw_offset=0x6407,physical_bytes=2,MID_offset=0x653B,runtime_address=0x6F295D,runtime_bytes=1,
            store_instruction=0x51E8A5,transform='low8',scope='Runtime transfer confirmed, not the subsequent speech consumer or a proof of a gameplay bug. Do not shrink the file field.'),
        strings=dict(hero_name=dict(raw_offset=0x6413,MID_offset=0x6547,World_offset=0x9B24,encoding='UTF-16LE',copy=0x6359A0),
            messages=dict(count=20,raw_offset=0x138C,raw_stride=1024,World_offset=0x7324,World_stride=512,conversion=0x5557E0,
                external_import='WideCharToMultiByte(ACP,0,src,-1,dst,511,NULL,NULL)',fixture_scope='Only explicitly prepared ASCII messages, success branch. Other ACP locales/non-ASCII/truncation/error branches not certified.'),
            terrain_path=dict(raw_offset=0x63E3,MID_offset=0x6517,external_GFX_slot=0x84,scope='Provider receives pointer; no terrain-resource load/render semantics asserted.'),
            sound_category=dict(raw_offset=0x638E,MID_offset=0x64C2,presence_MID_offset=0x6C,sprintf=0x634DB0,external_sound_slot=0x1C,World_handle_offset=0x7209,
                scope='Original format path executed with DK2 string and explicit provider. Real sound engine not executed.')),
        TPS=dict(global_address=0x6F2590,common_type=19,Level_raw_offset=0x1182,Level_physical_bytes=2,common_physical_bytes=4,
            executed_order='Prepared caller deliberately invokes each source in either order; latest write drives original collision/Freeze.',
            examples=[dict(order='common30 then Level4',effective=4,Data1=5,timer=20),dict(order='Level4 then common30',effective=30,Data1=5,timer=150)],
            static_natural_order=dict(root=0x50E620,preload=0x51DE10,count_allocate=0x51DD40,definitions=0x51DD80,late_pass=0x51DDC0,
                level_registration=0x51DE50,section_parser=0x56C4E0,reference_callback=0x51EA40,record_registration=0x51E0A0,
                Variables_section_parser=0x5716E0,Variables_record_callback=0x521790,
                sequence='In successful standard final pass,51DDC0 requires World7239 bits2 and4;51DE50 applies DF via51E6D0 before DE references. Reference callback chooses51E0A0 with bit2 set. Its bit4 branch enables Variables5716E0->521790. Per-World registration copied from6A5CD0 by5099A0/513870 provides Type19 callback51CD90.',
                inference='For this successful non-nested final-pass path, a reached Variables19 record after DF overrides Level TPS. Ordered later records/sections can override again.',
                status='static_control_flow_with_primary_PE_bytes; not a live-file or completeWorld-loader replay',
                limitations='Not an unconditional common-vs-map priority rule. Missing references, alternate modes, nestedLevel sections, failure exits, later writers and other EXEs are not resolved.'),
            source_caution='Current GlobalVariables lacks19; current AutoSave hasTPS4 but references a missing Maps Variables path. No final liveTPS inferred.'),
        fixture=dict(original_code_IAT_vtables='unchanged',Win32_provider_target=0x28C410,Win32_IAT_address=0x66C128,
            inputs='Copied and explicitly edited rawLevel; all20 message and sound presence slots explicitly selected by a declared caller after the real converter.',
            subsystem='7415B0 disabled in inherited fixture;553780/553800 return through their disabled branches, no downstream text/resource subsystem execution.',
            diagnostics='Rejected development fixtures exposed inherited Session overlapping World message storage, seed-OR flags and shared sound-slot contamination. Accepted fixture relocates Session and temporarily selects a separate external sound provider; no failures attributed to the game.',
            opaque_comparison='Ghidra compares full native post-context oracle including opaque bytes; such bytes receive no new semantic meaning.'),
        native_executions=accepted,independent_Ghidra_controls=controls,primary=primary,static_primary=static,physical_preservation=physical,
        API=dict(file=link('native_level_globals.py'),functions=['project','flags','patch']),helpers=[link(n) for n in HELPERS],source_baseline=link('phase194-source-baseline.json'),
        limitations=['No complete original file-I/O loader/editor writer or automatic external reference traversal executed in phase196.',
            'Runtime transport of rewards/text/intro fields does not certify later reward/speech/UI gameplay behavior.',
            'No all-Version proof, scheduler/FPS measurement or resistance fix. No overall completion-percent increase.',
            'UnknownGlobals29/186 remain unknown; Crypt deferred, ten counter meanings unresolved.'],
        next=['Original Variables reader through actual file provider and per-World registration; dynamic final-pass/reference traversal.',
              'Other unknownGlobals29/186 and record-order dependence forTPS-scaled variables.','Creature resistances/flags after global parameters.'])


NOTE='level_global_parameters_semantics phase196 supersedes phase195 static-onlyLevel scope:9 wholeconverter/Level/collision/Freeze scenes,3Ghidra/228regions/590997B/12745PC;136inherited+16newfunctionextents.21existingbindingrows,68exactinverseedits. SpeechHornyId raw6407u16->global6F295Dlow8; preservephysicalu16. Static standard final-pass DF-before-DE/lateVariables order only; no full naturalfileWorldload.173priorcomponents/codec/420sources/7EXEs preserved;Globalsactive,Cryptdeferred.'


def main():
    package=load('editor-format-package.json');path=ROOT/'phase196-prior-package-state.json'
    if not path.exists():
        assert len(package['components'])==173
        dump(path.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(path.name)
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    s=build()
    if s is None:print('Pending static primary bytes');return
    dump(SCHEMA,s);package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package)
    dump('phase196-search-status.json',dict(phase=196,goal_status='active',current_block='Global parameters',block_complete=False,counts=s['counts'],schema=link(SCHEMA),next=s['next']))
    print(json.dumps(s['counts']))


if __name__=='__main__':main()
