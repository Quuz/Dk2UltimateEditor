"""Integrate all reviewed Dethware sound topics and local follow-up evidence."""
from copy import deepcopy
import gzip
import hashlib
import json
from pathlib import Path
import zipfile

from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent
KEY = 'dethware_sound_review'
SCHEMA = 'dethware-sound-review.json'
PRIOR = 'phase205-prior-package-state.json'
REVISION = '28adde55f91145abdaab2df900a0bb6994aa04db'
SOURCE_URL = f'https://github.com/Dethware/DK2/blob/{REVISION}/'
NOTE = ('dethware_sound_review phase205 records all36sourcefiles, background graphs and open questions. '
        '63nativeDXcontrols refine linked transitions, unsignedDWORDweights, parameter slots and ranges. '
        'A zero-link node clears current without the no-matching-link stop marker; self-links can select item1. '
        'Qt implementation choices remain source implementation details. Music decay is gated by the DX music branch.')


def sha(data):
    return hashlib.sha256(data).hexdigest()


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf-8-sig'))


def save(name, value):
    (ROOT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf8')


def link(name):
    return dict(file=name, sha256=sha((ROOT / name).read_bytes()))


def canonical(value):
    return sha(json.dumps(value, ensure_ascii=False, sort_keys=True).encode('utf8'))


def source_coverage(index):
    purposes = {
        '.gitignore': 'Local game data and build/extraction exclusions',
        'README.md': 'Repository scope, dependencies and player/test entry points',
        'docs/file-formats.md': 'BANK/SFX/SDT structures, PCM boundaries and music graph',
        'docs/runtime-behaviour.md': 'Native sound methods, sequencing, music level and battle requests',
        'docs/open-questions.md': 'All unresolved fields, gameplay conditions and unanalysed player classes',
        'player/CMakeLists.txt': 'C++20, CMake3.21, Qt6.8 with Multimedia/Widgets/Test',
        'player/src/app/CMakeLists.txt': 'Standalone UI target and core linkage',
        'player/src/app/main.cpp': 'Qt application entry and optional game directory argument',
        'player/src/app/mainwindow.cpp': 'Track selection, battle slider, reset, per-layer gain and event log',
        'player/src/app/mainwindow.h': 'UI/Engine interface',
        'player/src/core/CMakeLists.txt': 'Core dependency and source list',
        'player/src/core/clipcache.cpp': 'Decoder queue, float conversion, PCM resize and LRU eviction',
        'player/src/core/clipcache.h': 'Threaded cache, pending/failed clips and512MiB choice',
        'player/src/core/engine.cpp': 'Three layers, start/stop, threat tick and sound output orchestration',
        'player/src/core/engine.h': 'Group IDs and50ms standalone ticker choice',
        'player/src/core/gamedata.cpp': 'Case-insensitive lookup, HD/HW selection and threshold scanning',
        'player/src/core/gamedata.h': 'Archive and group reference interfaces',
        'player/src/core/layer.cpp': 'Prefetch, retry/wait timing, sequence start and gain mixing',
        'player/src/core/layer.h': '22050Hz stereo rendering contract for the standalone layer',
        'player/src/core/mixer.cpp': 'Linear resampling, format conversion and clipping',
        'player/src/core/mixer.h': 'QAudioSink pull interface and mixer state',
        'player/src/core/musiclevel.h': 'Standalone normalized level model and per-update decay',
        'player/src/core/sdtarchive.cpp': 'SDT metadata reading and payload extraction',
        'player/src/core/sdtarchive.h': 'Declared PCM/type/name fields',
        'player/src/core/sequencer.cpp': 'Link matching, Qt item/sound choices and fallback',
        'player/src/core/sequencer.h': 'mt19937,250ms padding and candidate-prefetch interface',
        'player/src/core/soundmaps.cpp': 'Bounds-checked map parsing and selected field projection',
        'player/src/core/soundmaps.h': 'SoundRef/Item/Group representation',
        'player/tests/CMakeLists.txt': 'Qt test target and conditional installed-data tests',
        'player/tests/tst_dk2music.cpp': 'Graph, weighting, level, mixer and90second-render checks',
        'tools/ghidra/Decompile.java': 'Missing-function creation and recursive callee decompilation',
        'tools/ghidra/FindDisp.java': 'Conjunction of displacement/scalar filters',
        'tools/ghidra/FindRefs.java': 'Reference, scalar and defined-string searches',
        'tools/ghidra/run.sh': 'Read-only headless analysis wrapper',
        'tools/sdt.py': 'Raw sound directory reader/extractor',
        'tools/sfxmap.py': 'BANK/SFX directory reader and graph printout',
    }
    assert set(purposes) == {row['path'] for row in index['files']}
    return [dict(**row, url=SOURCE_URL + row['path'], reviewed=True, useful_content=purposes[row['path']])
            for row in index['files']]


def topics():
    rows = []

    def add(identifier, status, path, finding):
        rows.append(dict(id=identifier, status=status, source=SOURCE_URL + path, finding=finding))

    runtime = 'docs/runtime-behaviour.md'
    formats = 'docs/file-formats.md'
    add('binary-profile', 'source_scope', runtime,
        'Author inspected Steam DKII.EXE b5e4e2f6b76335e2a2e7f43bb6903bfc6f4170eabf04598955b00f512a47b449 with Ghidra12.1.2. Local DX has its own SHA and verification records.')
    add('bank-sfx-sdt-layout', 'local_data', formats,
        'All published structures, offsets and background graph details are retained through the phase204schema and full raw-header metadata graphs.')
    add('audio-encodings', 'source_observation', formats,
        'SDT0/24h/25h labels and published background MPEG descriptions are preserved with actual local sample rates, type bytes and payload prefixes. Local02h/03h meanings remain open.')
    add('pcm-crop', 'author_waveform_check', formats,
        'Author reports cropping decoded16bitPCM to pcmBytes from decoder offset0. The Qt cache resizes its float sample list to pcmBytes/2. Local decoding and waveform validation remain open.')
    add('bank-variant-preference', 'source_static', formats,
        'Author and standalone GameData choose HD first, then HW. The local catalogue audit resolves each variant independently.')
    add('entry-distance-fields', 'source_interpretation', formats,
        'Entry0C/10/14 are labelled minDistance/maxDistance/scale via OpenKeeper. Entry4..B retains opaque storage.')
    add('player-class-flags', 'DX_static', runtime,
        'Factory60DA70 checks4, then10h, then2/400h to select creator6157F0,616630,6165E0,6162F0,616090 or615EE0. Full allocation/audio construction remains open.')
    add('parameter-slots', 'DX_executed', runtime,
        'Player40..43 stores four IDs and44..47 four values.60C750 updates every matching ID. Two controls cover duplicate ID2 and absentID9 with playback-update gate24=0.')
    add('linked-choice', 'DX_executed', runtime,
        '6163C0 selects uniformly among inclusive intensity links in file order. Repeating a link changes its chance. Current=NULL starts from the first item.')
    add('shared-RNG', 'DX_executed', runtime,
        'Seed7A53B0 advances by seed*19660Dh+3C6EF35Fh modulo2^32, followed by ROR13. Link and range selection use modulo; sound/item weighted selection uses upper16 of rotated value.')
    add('self-link', 'DX_executed_refinement', 'player/src/core/sequencer.h',
        'A first-item self-link can select that item for normal playback. The header comment that the entry point is never played needs this qualification; the C++ algorithm itself accepts self-targets.')
    add('no-matching-links', 'DX_executed', runtime,
        'With nonzero linkCount but no eligible link,6163C0 sets intensity7Fh and flag100h, returns0 and retains current item.')
    add('zero-link-count', 'DX_executed_refinement', runtime,
        'With linkCount0,6163C0 sets current=NULL and returns0. This early path leaves the intensity byte and stop flag unchanged.')
    add('resume-callback', 'DX_static', runtime,
        '616560 tests stop flag100h and intensity different from7Fh before invoking original vslot3C. Full stream resume remains open.')
    add('sound-weight-width', 'DX_executed_refinement', formats,
        '60CFF0 compares the full unsignedDWORD atSound+4 with a16bit random value using inclusive>=. One sound returns directly without RNG advance; no hit falls back to first.')
    add('item-weight-width', 'DX_executed_refinement', formats,
        '60D060 compares aDWORD beginning atItem+1E. The source layout describes aWORD at1E and opaqueWORD20; both participate in this read. Preserve upperWORD.')
    add('weights-local-range', 'local_data', formats,
        'All6772local Sound weights have high16zero.435of643multi-sound items finish below65535. Source data therefore exercises the fallback interval.')
    add('delay-range', 'DX_executed', runtime,
        '60CCF0 returns min+ROR13(nextSeed)%(max-min) for increasing ranges, min for equal bounds,0for0/0.250ms is added by callers, including615B94 and615EB0.')
    add('delay-clock', 'DX_static', runtime,
        'Caller scheduling combines current clock, clip duration and250ms. The tick subtracts clip duration before checking the next-start threshold. This supports start-to-start timing.')
    add('volume-range', 'DX_executed', runtime,
        '60CAA0 treats0C/0D as unsigned bytes, swaps reversed bounds in runtime data, chooses half-open random range or interpolation from slot1/2.')
    add('signed-range', 'DX_executed_arithmetic', formats,
        '60CB90 uses signed bytes0E/0F and swaps reversed bounds. The source pan interpretation remains a semantic lead; numeric behavior is executed.')
    add('range-slot-flags', 'DX_executed', formats,
        'Item18/1C bit0 selects slot1/2 for volume and bit1 for the signed range. Prepared percent50/25 controls and fixed ranges preserve RNG state.')
    add('music-manager', 'DX_static_and_phase204', runtime,
        'LevelFFh is silent;100h..103h map to20/40/60/80. Raise567BC0 keeps maximum; set567BE0 assigns directly. Three layers use music, ambience341 and atmosphere746.')
    add('music-decay-gate', 'DX_static_refinement', runtime,
        '56750E skips to5675E1 when the music optionDWORD75AD37 is zero. Clamp5675DE lies inside the enabled music branch. Whole-manager audio execution remains open.')
    add('ambience-option', 'DX_static', runtime,
        '567651..567665 adds20 when optionDWORD75AD27 equals1. Menu text IDs5A3/119/5A2 from source remain to be resolved to a setting name.')
    add('track-choice', 'phase204_local', runtime,
        'Level SoundTrack raw63B7 ->World720D ->table341/343/344/345 at6BDB70;0aliases1. Local music groups343/345 exist and344 is absent.')
    add('fight-start-requests', 'source_static', runtime,
        'Source lists creature statesA4/B4/B8/BA and functions458370/4586B0/482010/485AB0/485B80 requesting100h. Exact roles and ownership filtering remain open.')
    add('battle-threat', 'phase204_DX_executed', runtime,
        '505310 uses Threat rawF12 ->definition698, growth65Stat3 and215..217 thresholds. Same owner as World7243 is excluded. Native phase204includes15scenes.')
    add('horny-music', 'phase204_DX_static', runtime,
        '488930 tests definitionFlags4000h. Owner+76 bit40h chooses setFFh rather than raise103h. Meaning of that owner flag remains open.')
    add('graph-partitions', 'source_and_local_data', formats,
        'Track1 partitions1..52/53..87/88..109/110..125 with transition126..135; track3partitions1..87/88..110/111..128/129..156. Full local links are retained for direct inspection.')
    add('graph-transition-boundaries', 'source_static', runtime,
        'Linked player consults intensity when selecting a segment. Native full audio queue and audible transition timing remain open.')
    add('qt-random-model', 'implementation_choice', 'player/src/core/sequencer.h',
        'Standalone player uses per-sequencer mt19937 and uniform_int_distribution. DX selectors use one shared LCG seed and observable early-return consumption.')
    add('qt-nonlinked-selection', 'implementation_difference', 'player/src/core/sequencer.cpp',
        'Standalone nonlinked next() uniformly chooses an item, while native60D060 uses cumulativeDWORD weights. Its atmosphere group has one item.')
    add('qt-threshold-model', 'implementation_difference', 'player/src/core/musiclevel.h',
        'Standalone model counts crossed thresholds and clamps toFour. Native505310 prioritizes217,216,215 with unsigned comparisons. Source default positive ordered thresholds match ordinary scenes.')
    add('qt-timer-cache-resampler', 'implementation_choice', 'player/src/core/engine.h',
        '50ms ticker,22050Hz stereo layers,100msretry/10msdecodewait,512MiBLRUcache, linearresampling and clampedoutput are standalone implementation choices.')
    add('qt-codec-and-gains', 'implementation_choice', 'player/src/core/clipcache.cpp',
        'QAudioDecoder producesfloatPCM, supports sample-format conversions and PCM-length resize. Layer/mastergain, prefetch and device-format fallback are reusable preview design references.')
    add('upstream-tests', 'source_test_description', 'player/tests/tst_dk2music.cpp',
        'Qt tests cover graphs, duplicated-link ratios, delay bounds, thresholds, decay,48kHzmixer, installed data and90secondtrack1render with waveform-join curvature. These tests were reviewed, not run locally.')
    add('ghidra-tools', 'source_tooling', 'tools/ghidra/Decompile.java',
        'Recursive decompilation with missing-function creation, scalar/string/reference searches and AND-of-displacements scans provide reusable analysis techniques.')
    add('russian-lengths', 'local_follow_up', 'docs/open-questions.md',
        '32alternateSDTs have every3385declared payload length overstated by40bytes. Header-only in-memory correction is exactly reversible.150739completeMPEG-frame extents were checked; playback remains open.')
    return rows


def open_questions():
    return [
        dict(id='fight-states', state='open', subject='Exact roles ofA4/B4/B8/BA and requests from other keepers'),
        dict(id='battle-recruitment', state='open', subject='Ownership selection,8battle-slot recruitment and spatial separation of fights'),
        dict(id='threat-identity', state='locally_resolved_for_selected_DX', subject='RawF12/definition698 and Type65Stat3 feed505310 in phase204'),
        dict(id='horny-owner-flag', state='open', subject='Meaning and producers of owner+76 bit40h'),
        dict(id='ambience-menu', state='open', subject='Name corresponding to75AD27 and textIDs5A3/119/5A2'),
        dict(id='level-overrides', state='loader_confirmed', subject='NativeVariables accepts215..217 replacements; actual level-specific sound playback remains open'),
        dict(id='sound-tick-rate', state='open', subject='Measured scheduler rate and interaction with variableTPS'),
        dict(id='group0c', state='open', subject='GroupDWORD0C often labelled priority'),
        dict(id='item14-15', state='open', subject='Meaning of two Item bytes at14/15, commonly50/50inbackground'),
        dict(id='item22', state='open', subject='Meaning of Item float22, observed20/100/0.1inbackground'),
        dict(id='signed-range-role', state='arithmetic_confirmed_semantics_open', subject='Pan interpretation and its downstream audible effect'),
        dict(id='entry4-b', state='open', subject='Meaning of EntryDWORD4 and WORD8/A'),
        dict(id='track2-origin', state='open', subject='Other releases or intended content of absent group344'),
        dict(id='options-player', state='factory_branch_static', subject='Full6741C8options-loop behavior'),
        dict(id='other-player-classes', state='open', subject='674140/6740F8and673F40subclass615680paths'),
        dict(id='front-end', state='open', subject='Menu ambience and object sound orchestration'),
        dict(id='DX-profile', state='partially_resolved', subject='Thirteen local sound byte ranges,63selectorcontrols and phase204battle chain; remaining engine paths retain source status'),
        dict(id='audio-PCM', state='open', subject='Local decoding, offset0crop, physical sample-rate/SDTheader discrepancies and waveform joins'),
    ]


def primary():
    blob = EXE.read_bytes()
    rows = []
    for path in sorted((ROOT / 'ghidra').glob('dethware-DX-*-memory.txt')):
        data = json.loads(path.read_text(encoding='utf8'))
        address = int(data['address'], 16)
        raw = bytes(data['data'])
        assert raw == pe_read(blob, address, len(raw))
        rows.append(dict(file=path.name, sha256=sha(path.read_bytes()), address=address,
                         length=len(raw), bytes_sha256=sha(raw)))
    assert len(rows) == 13
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    factory = list(md.disasm(pe_read(blob, 0x60DA70, 0x10F), 0x60DA70))
    calls = {int(ins.op_str, 16) for ins in factory if ins.mnemonic == 'call' and ins.op_str.startswith('0x')}
    assert calls >= {0x6157F0, 0x616630, 0x6165E0, 0x6162F0, 0x616090, 0x615EE0}
    gate = next(md.disasm(pe_read(blob, 0x56750E, 6), 0x56750E, count=1))
    assert gate.mnemonic == 'je' and int(gate.op_str, 16) == 0x5675E1
    return rows



def format_fields():
    def field(offset, size, kind, meaning, confidence='local_storage'):
        return dict(offset=offset, size=size, kind=kind, meaning=meaning, confidence=confidence)
    return {
        'Entry24': [
            field(0, 4, 'u32', 'groupCount'),
            field(4, 4, 'u32', 'opaque'),
            field(8, 2, 'u16', 'opaque'),
            field(10, 2, 'u16', 'opaque'),
            field(12, 4, 'f32', 'minDistance', 'source_name'),
            field(16, 4, 'f32', 'maxDistance', 'source_name'),
            field(20, 4, 'f32', 'scale', 'source_name'),
        ],
        'Group20': [
            field(0, 4, 'u32', 'event ID within category'),
            field(4, 4, 'u32', 'itemCount'),
            field(8, 4, 'u32', 'items pointer at runtime, preserve raw file value'),
            field(12, 4, 'u32', 'priority candidate, meaning open'),
            field(16, 2, 'u16', 'class-selection flags', 'DX_static'),
            field(18, 1, 'u8', 'parameter slot0 ID', 'source_static'),
            field(19, 1, 'u8', 'opaque'),
        ],
        'Item42': [
            field(0, 4, 'u32', 'soundCount'), field(4, 4, 'u32', 'linkCount'),
            field(8, 4, 'u32', 'sounds pointer at runtime, preserve file value'),
            field(12, 1, 'u8', 'volume lower bound', 'DX_executed'),
            field(13, 1, 'u8', 'volume upper bound', 'DX_executed'),
            field(14, 1, 's8', 'signed lower bound, pan candidate', 'DX_executed_arithmetic'),
            field(15, 1, 's8', 'signed upper bound, pan candidate', 'DX_executed_arithmetic'),
            field(16, 2, 'u16', 'delay lower bound', 'DX_executed'),
            field(18, 2, 'u16', 'delay upper bound', 'DX_executed'),
            field(20, 1, 'u8', 'opaque, source background often50'),
            field(21, 1, 'u8', 'opaque, source background often50'),
            field(22, 2, 'u16', 'parameter slot1 ID', 'source_static'),
            field(24, 2, 'u16', 'slot1 use, bit0 volume and bit1 signed range', 'DX_executed'),
            field(26, 2, 'u16', 'parameter slot2 ID', 'source_static'),
            field(28, 2, 'u16', 'slot2 use, bit0 volume and bit1 signed range', 'DX_executed'),
            field(30, 2, 'u16', 'lowWORD of cumulative item selection value'),
            field(32, 2, 'u16', 'upperWORD included by nativeDWORDread at1E', 'DX_executed'),
            field(34, 4, 'f32', 'meaning open, source background20/100/0.1'),
            field(38, 4, 'u32', 'links pointer at runtime, preserve raw file value'),
        ],
        'Sound16': [
            field(0, 4, 'u32', 'archive entry,1based'),
            field(4, 4, 'u32', 'cumulative selection weight', 'DX_executed'),
            field(8, 4, 'u32', 'duration milliseconds', 'source_static'),
            field(12, 4, 'u32', 'bank index,1based'),
        ],
        'Link8': [
            field(0, 4, 'u32', 'target item,1based in group; pointer at runtime'),
            field(4, 1, 'u8', 'opaque'), field(5, 1, 'u8', 'opaque'),
            field(6, 1, 'u8', 'minimum inclusive intensity', 'DX_executed'),
            field(7, 1, 'u8', 'maximum inclusive intensity', 'DX_executed'),
        ],
        'SDT40': [
            field(0, 4, 'u32', 'header byte size'), field(4, 4, 'u32', 'payload byte size'),
            field(8, 16, 'bytes', 'bounded sound name'), field(24, 2, 'u16', 'nominal sample rate'),
            field(26, 1, 'u8', 'sample bits'), field(27, 1, 'u8', 'sound type'),
            field(28, 4, 'u32', 'opaque'), field(32, 4, 'u32', 'PCM byte count', 'source_static'),
            field(36, 4, 'u32', 'opaque'),
        ],
    }


def factory_dispatch():
    return [
        dict(condition='flags&4==0', creator=0x6157F0, vtable_from_source=None, role='plain sound'),
        dict(condition='flags&4 and flags&10h', creator=0x616630, vtable_from_source=0x6741C8, role='options music'),
        dict(condition='flags&4 and !(flags&10h) and flags&2 and flags&400h',
             creator=0x6165E0, vtable_from_source=0x674180, role='linked streaming'),
        dict(condition='flags&4 and !(flags&10h) and flags&2 and !(flags&400h)',
             creator=0x6162F0, vtable_from_source=0x674140, role='remaining source class'),
        dict(condition='flags&4 and !(flags&10h) and !(flags&2) and flags&400h',
             creator=0x616090, vtable_from_source=0x6740F8, role='remaining source class'),
        dict(condition='flags&4 and !(flags&10h) and !(flags&2) and !(flags&400h)',
             creator=0x615EE0, vtable_from_source=0x6740B0, role='delay chained'),
    ]


def build():
    index = load('dethware-source-index.json')
    assert index['revision'] == REVISION and len(index['files']) == 36
    snapshot = load('dethware-source-snapshot.json')
    archive = ROOT / snapshot['source_archive']
    assert sha(archive.read_bytes()) == snapshot['sha256']
    with zipfile.ZipFile(archive) as source:
        assert set(source.namelist()) == {row['path'] for row in index['files']}
        for row in snapshot['files']:
            raw = source.read(row['path'])
            assert sha(raw) == row['sha256']
            assert hashlib.sha1(b'blob ' + str(len(raw)).encode() + b'\0' + raw).hexdigest() == row['git_blob']
    selectors = load('dethware-DX-selector-replays.json')
    counts = {key: len(selectors[key]) for key in ('links', 'weights', 'delays', 'ranges', 'parameters')}
    assert counts == dict(links=32, weights=10, delays=9, ranges=10, parameters=2)
    assert selectors['exe_sha256'] == sha(EXE.read_bytes())
    survey = load('dethware-local-sound-survey.json')
    graph = json.loads(gzip.decompress((ROOT / 'sound-background-graphs.json.gz').read_bytes()))
    assert graph['summary'] == survey and survey['exact_background_map_roundtrips'] == 8
    assert survey['all_SFX_counts']['sounds'] == 6772
    russian = load('sound-russian-sdt-length-audit.json')
    assert russian['total_records'] == 3385 and russian['exact_inverse']
    return dict(
        phase=205, date='2026-11-13', source=index, source_coverage=source_coverage(index),
        topics=topics(), open_questions=open_questions(), selectors=selectors,
        format_fields=format_fields(), factory_dispatch=factory_dispatch(),
        counts=dict(source_files_reviewed=36, native_selector_controls=sum(counts.values()),
                    graph_groups=survey['graph_groups'], graph_items=survey['graph_items'],
                    background_archives=survey['background_archives'], prior_components_preserved=181),
        local_survey=survey, background_graph=link('sound-background-graphs.json.gz'),
        russian_follow_up=link('sound-russian-sdt-length-audit.json'), primary=primary(),
        interpretation='Statuses describe each finding independently. Source tests were inspected. Native controls use prepared pointer-resolved structures. Live audio/Qt tests remain open.',
        schema_previous=link('sound-resource-semantics.json'),
        helpers=[link(name) for name in ['probe_dethware_dx_selectors.py', 'survey_dethware_sound_data.py',
                                        'audit_russian_sdt_lengths.py', 'build_dethware_review.py']])


def main():
    package = load('editor-format-package.json')
    if not (ROOT / PRIOR).exists():
        assert len(package['components']) == 181
        save(PRIOR, dict(components=deepcopy(package['components']), codec=link('dk2_formats.py'),
                         preserved_top_level={key: canonical(value) for key, value in package.items()
                                              if key not in ('components', 'precedence', KEY)},
                         precedence=package['precedence']))
    before = load(PRIOR)
    for key, ref in before['components'].items():
        assert package['components'][key] == ref == link(ref['file'])
    for key, expected in before['preserved_top_level'].items():
        assert canonical(package[key]) == expected
    assert before['codec'] == link('dk2_formats.py')
    schema = build()
    save(SCHEMA, schema)
    package[KEY] = schema
    package['components'][KEY] = link(SCHEMA)
    if NOTE not in package['precedence']:
        package['precedence'].append(NOTE)
    assert package['precedence'] == before['precedence'] + [NOTE]
    save('editor-format-package.json', package)
    assert len(package['components']) == 182
    save('phase205-validation.json', dict(passed=True, counts=schema['counts'], schema=link(SCHEMA),
                                         package=link('editor-format-package.json'), goal_status='active',
                                         source_audio_writes=0))
    print(schema['counts'], 'topics', len(schema['topics']), 'open questions', len(schema['open_questions']))


if __name__ == '__main__':
    main()

