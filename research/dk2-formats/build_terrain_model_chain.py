"""Publish actual Terrain model preservation with no guessed semantic labels."""
from pathlib import Path
import hashlib, json
import emulate_terrain_model_chain as native
import crosscheck_terrain_model_chain as independent
import prepare_terrain_model_primary as primary

ROOT = Path(__file__).resolve().parent


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def build():
    manifest = primary.build()
    assert manifest == load('phase128-terrain-model-primary.json')
    rows = load('phase128-terrain-model-emulations.json')['replays']
    assert len(rows) == 6
    visits = set()
    for case, row in zip(native.cases(), rows):
        fixture = native.build(case)
        path = native.path_for(case, fixture)
        saved = json.loads(path.read_text(encoding='utf8'))
        assert saved['case'] == case and saved['input'] == fixture and row['sha256'] == sha(path.read_bytes())
        assert row['outcome'] == native.verify(case, fixture, saved['result'])
        visits.update(saved['result']['visited_instructions'])
    assert all(hex(row['address']) in visits for row in manifest['fresh_functions'])
    controls = load('phase128-terrain-model-crosschecks.json')['replays']
    assert len(controls) == 2
    for row in controls:
        args, count, size = independent.build(row['case'])
        saved = load(row['file'])
        assert saved['input'] == args and row['sha256'] == sha((ROOT / row['file']).read_bytes())
        result = saved['result']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][key], 16) == value for key, value in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
        assert row['checked_regions'] == count and row['checked_bytes'] == size
    sources = load('terrain-catalogue-validation.json')['corpus']['files']
    for row in sources:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    executables = load('game-executable-compatibility.json')['original_executable_paths_preserved']
    for row in executables:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    previous = ROOT / 'phase128-prior-components.json'
    if not previous.exists():
        prior = load('editor-format-package.json')['components']
        assert len(prior) == 106
        previous.write_text(json.dumps(prior, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(previous.name)) == 106
    return dict(phase=128, date='2026-10-29', family='Terrain whole original editor model preservation',
        profile=dict(executable=str(native.engine.EXE), sha256=sha(native.engine.EXE.read_bytes())),
        full_chain=dict(converter=0x42ED90, callback=0x49F5E0, constructor=0x4250D0,
            writer=0x4A7690, catalogue_context=0x54F550, array_pointer=0x54F960,
            count=0x54F964, capacity=0x54F968,
            flow='Actual converter→native allocated/constructed/registered Terrain model→original full writer; internal malloc/CString/pool helpers retained.'),
        unknown_fields=[dict(raw=0x198, width=2, intermediate=0x250, model=0x3D0,
                meaning='unresolved', result='All selected raw WORD bits preserved through actual model andwriter.'),
            dict(raw=0x224, width=4, intermediate=0x2DC, model=0x320, model_type='double',
                raw_to_model='unsigned32/4096', model_to_raw='original__ftol(model*4096),low32', meaning='unresolved',
                boundaries=[0, 1, 0x7FFFFFFF, 0x80000000, 0xFFFFFFFF],
                result='Exact raw byte return in five whole model compositions. Editor scale doesnot establish runtime units.'),
            dict(raw=0x1AE, item_width=2, count=16, intermediate=0x266, model=0x328,
                getter=0x425400, setter=0x425420, meaning='unresolved',
                result='Sixteen nonzero high-bit values survive whole converter→model→writer. Getterindex>=16 returns0/setter ignores; no new UI/game consumer.')],
        native_executions=rows, independent_Ghidra_controls=controls,
        coverage=dict(whole_native_compositions=6, source_records=41, diagnostic_records=5,
            total_output_records=46, output_containers_reparsed_and_repacked=6,
            all_output_record_bytes_identical_to_input=True, record_bytes_verified=25392,
            selected_model_field_bytes_verified=1932, whole_model_bytes_fingerprinted=45264,
            native_catalogue_array_count=41, native_catalogue_array_capacity=41,
            model_comparison_scope='Native outputs independently checked at model198/224/16WORD fields; entire heap compared between emulators for two selected cases. Hashing full models isnot a separate semantic oracle.',
            original_parser_or_initializer_executed=False, original_GUI_commit_executed=False),
        inherited_environment=link('room-model-chain-semantics.json'),
        fixtures='Same declared initialized CRT/CString-pool/clock/TLS setup and12IATbindings asphase127; native functions unchanged. Terrain intermediate736 zero-seeded with64guard. No live Windows file/game/GUI opened.',
        primary=link('phase128-terrain-model-primary.json'),
        source_preservation=dict(Terrain_sources=sources, executable_paths=executables,
            source_writes=0, new_corpus_API_rebuilds=0,
            note='Six new original-writer output containers are reparsed/repacked; previous corpus API rebuild evidence remains historical, not recounted.'),
        helpers=[link(name) for name in ['emulate_terrain_model_chain.py', 'crosscheck_terrain_model_chain.py', 'prepare_terrain_model_primary.py']],
        limitations=['No gameplay names assigned to198/224/1AE. Broad literal searches ofthese fields already exist andwere not rerun.',
            'Only standalone41source records andfive diagnostic records execute; other Terrain files hash-checked only.',
            'UI/event commit, original parser initializer, model destructors, allocator failure and multithreading notexecuted.',
            'Header date/scratch differs from source; exact record roundtrip isnot whole source-file identity.',
            'Unassigned header42/43/47 omitted from independent emulator comparison; every Terrain record byte included.',
            'Prepared external adapters are not Windows implementations; all internal native code protected.',
            'All15family significant unknowns remain;goalnotachieved.'])


if __name__ == '__main__':
    value = build()
    (ROOT / 'terrain-model-chain-semantics.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')
    print('6wholeTerrain chains/46records/2Ghidra controls;198/224/1AE meanings stillunresolved.')
