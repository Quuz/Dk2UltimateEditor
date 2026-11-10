"""Publish full Door loaders under the explicit null-visual-provider contract."""
from pathlib import Path
import json
from build_trap_loading import digest
from build_effect_semantics import pe_read
from native_door_loading import PROFILES, CONVERT, RESOURCES, LOAD

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def primary():
    functions, blocks = [], []
    spec = load('phase67-primary-manifest.json')
    for row in spec['functions']:
        before = ROOT / 'ghidra' / row['before']
        after = ROOT / 'ghidra' / row['after']
        memory = ROOT / 'ghidra' / row['memory']
        v = json.loads(memory.read_text(encoding='utf8'))
        b = bytes(v['data'])
        assert before.read_text().rstrip() == after.read_text().rstrip()
        assert int(v['address'], 16) == row['address'] and len(b) == v['length'] == row['length']
        assert b == pe_read(Path(row['exe']).read_bytes(), row['address'], row['length'])
        functions.append({**row, 'before_sha256': digest(before.read_bytes()),
                          'memory_sha256': digest(memory.read_bytes()), 'program_bytes_sha256': digest(b)})
    for row in spec['blocks']:
        memory = ROOT / 'ghidra' / row['memory']
        v = json.loads(memory.read_text(encoding='utf8'))
        b = bytes(v['data'])
        assert int(v['address'], 16) == row['address'] and len(b) == v['length'] == row['length']
        assert b == pe_read(Path(row['exe']).read_bytes(), row['address'], row['length'])
        assert b.startswith(b'GLOBAL\\\0')
        blocks.append({**row, 'memory_sha256': digest(memory.read_bytes()), 'program_bytes_sha256': digest(b), 'hex': b.hex()})
    return functions, blocks


def main():
    functions, blocks = primary()
    emu = load('phase67-door-loading-emulations.json')
    assert len(functions) == 8 and len(blocks) == 2 and len(emu['cases']) == 58
    scalar = lambda rows: [dict(presence=p, source=a, destination=b, bytes=n) for p, a, b, n in rows]
    search = []
    for name in ['phase67-old-door-word236-leads.json', 'phase67-DX-door-word236-leads.json']:
        search.append(dict(file=name, sha256=digest((ROOT / name).read_bytes()),
                           scope='Bounded displacement/argument search. Entity definition pointer hints are untyped; seven overlapping DWORD9A reads per EXE do not establish a DoorWORD99 consumer. No new gameplay meaning or global absence claim.'))
    schema = dict(
        phase=67, family='Doors full raw conversion and null-resource definition loading',
        profiles=PROFILES, sizes=dict(raw=616, intermediate=732, definition=181),
        primary_program_bytes=functions, primary_data_ranges=blocks,
        native_emulations=emu['cases'], native_execution_scope=emu['scope'],
        version_scope='OLD means exact SHA of supplied executable from DKII13 directory, not independently certified retail1.3.',
        initializer='Both complete initializers clear all732 bytes, including eight intermediate padding bytes. Full chain initializes before conversion; this stage does not execute the filesystem parser.',
        converter=dict(scalars=scalar(CONVERT), resources=scalar(RESOURCES),
            strings=[dict(presence=0, raw=0, intermediate=0x74, bytes=32), dict(presence=8, raw=0x248, intermediate=0x2BC, bytes=32)],
            strings_rule='Native bounded strncpy32, followed by forced final NUL; earlyNUL and unterminated32-byte input both tested.',
            flags='midDWORD29C |= rawDWORD228; presence70=1. Isolated dirty buffers preserve prior flags. This is not a claim of accumulation in the normal initialized parser.',
            presence='All29 DWORD presence slots0..70 set to1.',
            raw_untransferred=[dict(raw=o, bytes=n) for o,n in [(0x21E,2),(0x220,4),(0x230,2)]],
            intermediate_unwritten=[dict(offset=0x292, bytes=6), dict(offset=0x2A4, bytes=2)],
            padding_rule='Converter retains eight intermediate bytes from its previous buffer. They are different physical spans from the eight raw holes. Full initializer clears them.'),
        loader=dict(scalars=scalar(LOAD),
            presence='Zero retains previous scalar field, any nonzero applies it; values0/2/FFFFFFFF tested for WORD236 presence38.',
            word236='rawWORD236 -> midWORD2AA, presenceDWORD38 -> definitionWORD99. All16 bits preserved. Gameplay purpose remains open.',
            height='rawDWORD218 -> midDWORD28C -> low16 at definitionWORD97.',
            research_time=dict(raw=0x224, intermediate=0x298, presence=0x40, baseline_label='ResearchTime',
                finding='Full observed loaders do not transfer this scalar to definition. In the null-resource contract, changing only this WORD changes only checksum when checksum is calculated; DXgate1 gives identical complete definitions. No claim of global non-use or correctness of baseline gameplay name.')),
        checksum=dict(definition=0x87, bytes=4, rule='sum of all732 unsigned intermediate bytes',
            old='Always replaces previous DWORD87.', dx='GlobalDWORD6FCDCF==0 replaces; nonzero retains.',
            collision='Native chains for WORD236=00FF/FF00 have different definitionWORD99 but equal checksumDWORD87; sum is not a uniqueness guarantee.'),
        sound=dict(presence=8, intermediate=0x2BC, definition=0xA9,
            rule='If present, OLD always and DX only at gate0: native strncpy32 into global buffer, force lastNUL; virtual audio slot1C(prefixGLOBAL\\,name); store returnedDWORD.',
            provider='Capture exact prefix pointer,32 name bytes,call count; returnA1B2C3D4. No asset lookup/playback or audio failure tests.'),
        raw_holes_result='Twelve full native chains, varying all8 raw bytes0/FF/pattern in both profiles and both gate values, produce identical complete intermediate/definition buffers per profile/gate. No other consumer or historical purpose is ruled out.',
        search=search, corpus=load('door-writer-semantics.json')['corpus'],
        api=dict(file='native_door_loading.py', operations=['intermediate', 'definition', 'sound_requested', 'patch_research_time'],
                 policy='Keep original raw record for saving. Runtime definition is a projection, never a replacement source for serialization. ResearchTime patch preserves every other byte.'),
        limitations=[
            'Eight complete original functions execute: two initializers, converters, loaders and bounded-string helpers. Complete bodies are re-read after emulation and matched to source PE.',
            'Visual resource provider is NULL. Full functions execute, but visual success/failure branches, renderer and resource allocation are outside the tested contract.',
            'No full filesystem parser, world insertion or interactive GUI/gameplay session in this stage.',
            'Traps204 and Door236 gameplay meanings remain open. Similar offsets in other native types do not prove bad editor serialization.',
            'Existing2624 phase66 Door patches are not rerun as new work; phase67 adds only ResearchTime224 WORD container patches.',
        ])
    (ROOT / 'door-loading-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    door = load('door-semantics.json')
    door['phase67_loading'] = schema
    (ROOT / 'door-semantics.json').write_text(json.dumps(door, ensure_ascii=False, indent=2), encoding='utf8')
    print('Door loading:58 native cases;8 full PE functions;2 data ranges;unknown gameplay meanings remain open')


if __name__ == '__main__':
    main()
