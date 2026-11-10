"""Publish bounded RC3/old-PE resistance compatibility, without assigning unknown IDs."""
from pathlib import Path
import hashlib, json
import emulate_old_resistance_storage as native
import crosscheck_old_resistance_storage as independent
import prepare_old_resistance_primary as primary
import inspect_resistance_version_tables as tables

ROOT = Path(__file__).resolve().parent


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def build():
    version_tables = tables.build()
    assert load('phase126-resistance-editor-version-tables.json') == version_tables
    old_primary = primary.build()
    assert load('phase126-old-primary-manifest.json') == old_primary
    runs = load('phase126-old-resistance-emulations.json')['replays']
    assert len(runs) == 14
    for case, row in zip(native.cases(), runs):
        args, checks, _, _, eax = native.build(case)
        path = native.path_for(case)
        saved = json.loads(path.read_text(encoding='utf8'))
        assert saved['case'] == case and saved['input'] == args
        assert row['sha256'] == sha(path.read_bytes()) and row['outcome'] == native.verify(case, args, checks, eax, saved['result'])
    controls = load('phase126-old-resistance-crosschecks.json')['replays']
    assert len(controls) == 2
    for row in controls:
        args, count, size = independent.build(row['case'])
        saved = load(row['file'])
        assert saved['input'] == args and row['sha256'] == sha((ROOT / row['file']).read_bytes())
        result = saved['result']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][key], 16) == value for key, value in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
        assert row['checked_regions'] == count and row['checked_bytes'] == size
    sources = load('game-executable-compatibility.json')['original_executable_paths_preserved']
    for row in sources:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    data_sources = load('phase125-category-api-validation.json')['files']
    for row in data_sources:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    prior_file = ROOT / 'phase126-prior-components.json'
    if not prior_file.exists():
        previous = load('editor-format-package.json')['components']
        assert len(previous) == 104
        prior_file.write_text(json.dumps(previous, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior_file.name)) == 104
    setup = (ROOT / 'old-headless-import.log').read_text(encoding='utf8')
    assert 'REPORT: Import succeeded' in setup
    assert load('ghidra/phase126-old-start-analysis.txt')['success']
    return dict(phase=126, date='2026-10-29', family='Creature resistance preservation across exact supplied binaries',
        old_profile=dict(file=str(native.EXE), sha256=sha(native.EXE.read_bytes()),
            certified_retail_version=False, name_caveat='local13/folder1.3 is a supplied filename, not independent retail1.3 authentication.'),
        editor_version_tables=version_tables,
        unknown_types=dict(observed_source_IDs=[16, 30, 32], meaning='unresolved',
            no_extra_RC3_labels_in_three_tables=True, raw_u8_preservation_required=True,
            old_converter_preserves=True, old_getters_return_same_u8=True,
            old_definition_copy_preserves_all2199bytes=True,
            interpretation='Forward storage is confirmed; no new attack names, damage effects or percentage formula inferred.'),
        old_storage=dict(raw_offset=0xE0C, slots=4, stride=2, intermediate_offset=0x11FC,
            presence_offset=0x284, runtime_offset=0x61C, runtime_count_offset=0x78A,
            converter=0x55F170, getters=dict(present=0x5600B0, type=0x5600D0, value=0x5600F0),
            full_loader_static=0x48A150, definition_copy=0x512FA0, definition_bytes=2199,
            boundary='Whole converter/getters and whole prepared definition copy execute as separate paths. Full old raw-to-World loader/gameplay chain not executed here.'),
        finite_old_consumer_review=dict(evidence=link('phase126-old-primary-manifest.json'),
            direct_operands=20, classified_functions=11, conclusion=old_primary['conclusion'],
            no_global_nonuse_claim=True),
        native_old_executions=runs, independent_Ghidra_controls=controls,
        primary=link('phase126-old-primary-manifest.json'),
        rejected_preliminary_fixture=dict(case='copy-zero with presence-getter root',
            reason='The original copy reuses its stack argument as a loop counter. Assuming that the source pointer survives in that argument gave an invalid root-EAX expectation.',
            resolution='Copy cases now end through original canonical no-argument59DEB0 XOR EAX,EAX/RET. No game instruction or vtable replaced; failed preliminary result excluded.'),
        analysis_environment=dict(server_port=8091, project='old-headless-project/DriverResearch.gpr',
            program='/DK2-format-research/DKII-local13.exe', separate_from_DX_and_Pro=True,
            import_method='Native AnalyzeHeadless import saved in workspace after empty-server GUI-import endpoint reported unsupported; confirmed import success, then normal run_analysis.',
            game_Windows_process_executed=False, DLLs='Ghidra read system DLL metadata while importing; no game DLL execution in CPU cases.'),
        source_preservation=dict(executable_paths=sources, Creature_files=data_sources,
            new_container_rebuilds=0, source_writes=0,
            reason='No physical file-edit rule changed. Prior phase125 category-edit rebuild evidence retained without recounting.'),
        helpers=[link(name) for name in ['scan_old_resistance_leads.py', 'prepare_old_resistance_primary.py',
            'emulate_old_resistance_storage.py', 'crosscheck_old_resistance_storage.py',
            'inspect_resistance_version_tables.py', 'ghidra_old_probe.py', 'start_old_research.py']],
        limitations=['No names or application semantics assigned to16/30/32.',
            'No old melee/Shot/AoE/status damage CPU execution in thisphase; current DX damage evidence is not silently transferred.',
            'Bounded direct operands do not exclude adjusted/indexed aliases, dynamic copies, other modules or versions.',
            'RC3 tables/resource read from original PE; no fresh RC3 full GUI or Ghidra CPU claim.',
            'Converter opaque baseline bytes are measured; paired changes are independently predicted only for explicit raw/intermediate resistance fields.',
            'Copy cases use explicit prepared2199-byte definitions, not a full raw-to-World chain.',
            'All15family significant unknowns remain; goalnotachieved.'])


if __name__ == '__main__':
    value = build()
    (ROOT / 'resistance-version-semantics.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')
    print('RC3 tables unchanged;14old-native cases/2Ghidra controls;16/30/32 meanings unresolved.')
