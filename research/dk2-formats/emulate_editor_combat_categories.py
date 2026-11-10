"""Actual native lookup/setter functions on explicit models, without MFC/Win32 UI."""
from pathlib import Path
import json, struct
import emulate_editor_object_icons as engine
import native_combat_categories as categories

ROOT = Path(__file__).resolve().parent
MODEL, ARRAY = 0x12000000, 0x12010000


def build(kind, value, slot=0):
    model = bytearray([165]) * (0x1890 + 64)
    if kind in ['resistance_lookup', 'shot_lookup']:
        ids = list(range(16)) if kind == 'resistance_lookup' else list(range(4, 16))
        ptr, count, entry = (0x78, 0x7C, 0x4641E0) if kind == 'resistance_lookup' else (0x110, 0x114, 0x46A2A0)
        struct.pack_into('<I', model, ptr, ARRAY)
        struct.pack_into('<I', model, count, len(ids))
        memory = {MODEL: bytes(model), ARRAY: bytes(ids) + bytes([165]) * 64}
        expected = dict(memory)
        stack = [value]
        expected_eax = ids.index(value) if value in ids else 0xFFFFFFFF
    elif kind == 'setter':
        memory = {MODEL: bytes(model)}
        if slot < 4:
            model[0x1594 + 2 * slot:0x1596 + 2 * slot] = bytes([value, 100])
        expected = {MODEL: bytes(model)}
        entry, stack, expected_eax = 0x41E450, [slot, value, 100], None
    else:
        raise ValueError(kind)
    return dict(kind=kind, value=value, slot=slot, entry=entry, ecx=MODEL, stack=stack,
        expected_EAX=expected_eax, memory=[engine.region(a, b) for a, b in memory.items()],
        expected=[engine.region(a, b) for a, b in expected.items()])


def cases():
    return ([build('resistance_lookup', v) for v in [0, 1, 4, 6, 8, 15, 18, 41, 255]]
        + [build('shot_lookup', v) for v in [4, 6, 8, 15, 0, 1, 41, 255]]
        + [build('setter', v, slot) for slot, v in [(0, 4), (1, 6), (2, 8), (3, 255), (4, 8), (255, 6)]])


def verify(case, result):
    assert result['success'] and result['regions_match'] and not result['protected_failures']
    assert result['substitutions'] == result['external_API_adapters'] == 0
    if case['expected_EAX'] is not None:
        assert int(result['registers']['EAX'], 16) == case['expected_EAX']
    return dict(kind=case['kind'], value=case['value'], slot=case['slot'],
        whole_prepared_model_preserved_except_expected_pairs=True,
        return_EAX=result['registers']['EAX'], instructions=result['instruction_count'])


def main():
    rows = []
    for case in cases():
        result = engine.run(case)
        outcome = verify(case, result)
        name = f"{case['kind']}-{case['value']}-{case['slot']}"
        path = ROOT / 'ghidra' / f'phase125-editor-combat-{name}.json'
        path.write_text(json.dumps(dict(input=case, result=result), indent=2), encoding='utf8')
        rows.append(dict(case=name, file=str(path.relative_to(ROOT)), sha256=engine.sha(path.read_bytes()), outcome=outcome))
        print(name, 'passed')
    (ROOT / 'phase125-editor-combat-emulations.json').write_text(
        json.dumps(dict(scope=__doc__, replays=rows, full_MFC_dialog_executed=False), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
