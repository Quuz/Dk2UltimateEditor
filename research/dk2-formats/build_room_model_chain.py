"""Publish actual Room raw-to-allocated-model-to-native-writer compositions."""
from pathlib import Path
import hashlib, json
import emulate_room_model_chain as native
import crosscheck_room_model_chain as independent
import prepare_room_model_primary as primary

ROOT = Path(__file__).resolve().parent


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def build():
    manifest = primary.build()
    assert manifest == load('phase127-Room-model-primary-manifest.json')
    replays = load('phase127-Room-model-chain-emulations.json')['replays']
    assert len(replays) == 5
    for case, row in zip(native.cases(), replays):
        fixture = native.prepared(case)
        path = native.result_path(case, fixture)
        saved = json.loads(path.read_text(encoding='utf8'))
        assert saved['case'] == case and saved['input'] == fixture
        assert sha(path.read_bytes()) == row['sha256']
        assert native.verify(case, fixture, saved['result']) == row['outcome']
    visits = set()
    for row in replays:
        visits.update(load(row['file'])['result']['visited_instructions'])
    assert all(hex(row['address']) in visits for row in manifest['fresh_functions'] + manifest['inherited_functions'])
    controls = load('phase127-Room-model-chain-crosschecks.json')['replays']
    assert len(controls) == 2
    for row in controls:
        args, count, size = independent.build(row['case'])
        saved = load(row['file'])
        assert saved['input'] == args and row['sha256'] == sha((ROOT / row['file']).read_bytes())
        result = saved['result']
        assert not saved['tool_is_error'] and result['success'] and result['hit_return']
        assert all(int(result['registers'][key], 16) == value for key, value in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
        assert row['checked_regions'] == count and row['checked_bytes'] == size
    executables = load('game-executable-compatibility.json')['original_executable_paths_preserved']
    for row in executables:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    sources = load('phase122-room-api-validation.json')['files']
    for row in sources:
        assert sha(Path(row['file']).read_bytes()) == row['sha256']
    previous = ROOT / 'phase127-prior-components.json'
    if not previous.exists():
        prior = load('editor-format-package.json')['components']
        assert len(prior) == 105
        previous.write_text(json.dumps(prior, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(previous.name)) == 105
    return dict(phase=127, date='2026-10-29', family='Room original editor raw-to-model-to-save composition',
        profile=dict(file=str(native.EXE), sha256=sha(native.EXE.read_bytes())),
        executed_chain=dict(converter=0x42DF90, model_callback=0x49FC30, Room_constructor=0x424780,
            base_constructor=0x41C720, resource_constructor=0x41BB40,
            resource_setter=0x41BB90, resource_name_parser=0x41C0B0,
            malloc=0x4AC4CB, nh_malloc=0x4AC4DD, heap_router=0x4AC509,
            fixed_pool_allocate=0x4AC3C8, fixed_pool_release=0x4AC457,
            CString_allocate=0x4DDC56, CString_release=0x4DDCD8,
            global_registration=dict(context=0x54F550, array_pointer=0x54F998, count=0x54F99C, capacity=0x54F9A0),
            writer=0x4A7AC0, native_date=0x4AB320,
            source='Exact standalone Data/editor/Rooms.kwd26records, then4single-Treasury diagnostic unknown-resource inputs.',
            order='Converter→whole allocating/constructing/registering callback for each record,then whole original writer. No host-replaced model pointers or manual registration.'),
        native_executions=replays, independent_Ghidra_controls=controls,
        preserved_unknown_resource=dict(raw=0x1C4, bytes=84, intermediate=0x298,
            model_resource_object=0x280, model_raw_bytes=0x284,
            cached_name_index=dict(model=0x2D8, width=2, source='first# suffix parsed through real CString helpers and original atoi; low16'),
            diagnostics=[dict(case='named', suffix='9', type=7, cache=9),
                dict(case='type0', suffix='65537', type=0, cache=1),
                dict(case='negative', suffix='-1', type=7, cache=65535),
                dict(case='suffix', suffix='12junk', type=7, cache=12)],
            result='All84bytes survive converter→actual allocated MFC model→writer. Cached suffix is separate; original resource name/metadata are not normalized by this path.',
            meaning='unresolved; no new UI label or gameplay consumer found'),
        coverage=dict(whole_compositions=5, total_written_records=30, original_catalogue_records=26,
            diagnostic_records=4, complete_output_containers_reparsed=5,
            source_record_bytes_preserved=31650, resource_blocks=330, resource_bytes=27720,
            complete_model_bytes_checked=40560,
            catalogue_native_array_count=26, catalogue_native_array_capacity=29,
            catalogue_native_heap_allocations=47, catalogue_native_heap_allocated_bytes=40304,
            all_Catalogue_records_byte_identical_to_source=True,
            whole_output_files_identical_to_source=False,
            note='Native headers have date/scratch values, so record identity must not be described as whole-file identity.'),
        fixture=dict(CRT_bootstrap_executed=False, single_thread=True,
            heap='Initialized opaque handle1234; CRT small-block threshold0 selects real HeapAlloc path. External bounded bump provider has40000hex-byte arena and logs every size/result; HeapFree succeeds without arena reuse.',
            CString_pools='Four explicitly initialized valid CFixedAlloc capacities64/128/256/512 with4nodes per block. Native allocation/link/release code executes; not a claim about original application startup block counts.',
            intermediate='Zero-seeded1267byte intermediate for whole original converter; original parser initializer not executed.64-byte guards verified.',
            IAT_data_bindings=12,
            new_IAT_services=['HeapAlloc', 'HeapFree', 'lstrlenA', 'EnterCriticalSection', 'LeaveCriticalSection', 'InterlockedIncrement', 'InterlockedDecrement'],
            inherited_IAT_services=['GetLocalTime', 'GetSystemTime', 'GetLastError', 'TlsGetValue', 'SetLastError'],
            output='Explicit CFile virtual Write sink; no Windows file opened. Native writer/calendar remain original.',
            low_page='Only original FS:[0] exception chain reads permitted/verified; no NULL+offset read accepted.',
            code_and_readonly='Original PE instructions and native vtables are not overwritten; original executable/read-only ranges protected during execution.',
            Ghidra='Continuous call driver compared to Unicorn repeated-root invocation. Unassigned header bytes42/43/47 excluded because native stack origins differ; all record bytes and complete prepared heap included.',
            limitations='Prepared successful allocation and initialized CRT/timezone/TLS only; no heap-exhaustion handling, concurrency, GUI events or startup verified.'),
        primary=link('phase127-Room-model-primary-manifest.json'),
        inherited_writer_evidence=link('room-writer-semantics.json'),
        rejected_discovery='Initial lstrlen adapter branch encoded an interior-instruction target and hit step limit; corrected adapter, excluded preliminary result. No change to original PE code.',
        preservation=dict(original_executable_paths=executables, original_Room_sources=sources,
            source_writes=0, new_corpus_edit_rebuilds=0,
            reason='New native output containers5are reparsed/repacked; prior246RoomAPIrebuilds remain historical and are not recounted. No physical edit API changed.'),
        helpers=[link(name) for name in ['emulate_room_model_chain.py', 'crosscheck_room_model_chain.py', 'prepare_room_model_primary.py']],
        limitations=['Unknown resource1C4 still has no identified gameplay meaning.',
            'Standalone Rooms26records and4bounded synthetic resource names only; not every room in all82source containers executed through this chain.',
            'Full original raw parser/initializer, GUI commit/selection, destructor of all models, renderer and game World not executed.',
            'NUL-terminated bounded names used; arbitrary unterminated/overlong ANSI resource strings not certified.',
            'Additional cached index parsing doesnot mean underlying84serialized bytes may be normalized.',
            'The no-op/heap/clock providers are declared external boundaries; internal MFC/CRT functions and vtables remain native.',
            'Significant unknowns across all15families remain; goalnotachieved.'])


if __name__ == '__main__':
    value = build()
    (ROOT / 'room-model-chain-semantics.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')
    print('5wholeRoom compositions/30records/330resources;2Ghidra controls;Room1C4meaning unresolved.')
