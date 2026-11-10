"""Two same-input cross-engine checks against retained phase102 Ghidra results."""
from pathlib import Path
import hashlib
import json
from build_effect_semantics import EXE
import emulate_element_graphics as previous
import memory_readout
from native_unicorn import run

ROOT = Path(__file__).resolve().parent


def main():
    cases = {c['name']: c for c in previous.cases()}
    earlier = json.loads((ROOT / 'phase102-element-graphics-emulations.json').read_text(encoding='utf8'))['replays']
    rows = []
    for name in ['sprite-base', 'quad-palette-0-0']:
        row = next(r for r in earlier if r['case'] == name)
        ghidra_path = ROOT / 'ghidra' / row['file']
        saved = json.loads(ghidra_path.read_text(encoding='utf8'))
        args = previous.build(cases[name])
        assert saved['input'] == args
        path = ROOT / 'ghidra' / f'phase103-cross-engine-{name}.json'
        if path.exists():
            value = json.loads(path.read_text(encoding='utf8'))
            assert value['input'] == args
            result = value['result']
        else:
            result = run(EXE, args, [], previous.OBSERVER, previous.HEAP, 128)
            path.write_text(json.dumps(dict(case=name, input=args, result=result), indent=2), encoding='utf8')
        expected = memory_readout.decode(saved['result']['registers'])
        assert result['success'] and result['hit_return']
        assert bytes.fromhex(result['captured_hex']) == expected
        assert not result['protected_write_or_execution_failures']
        rows.append(dict(case=name, same_input=True, complete128bytes_equal=True,
                         Ghidra=dict(file=str(ghidra_path.relative_to(ROOT)), sha256=hashlib.sha256(ghidra_path.read_bytes()).hexdigest()),
                         Unicorn=dict(file=str(path.relative_to(ROOT)), sha256=hashlib.sha256(path.read_bytes()).hexdigest())))
    (ROOT / 'phase103-cross-engine-validation.json').write_text(json.dumps(dict(
        scope=__doc__, replays=rows, limitations='Two heap outcomes only; not global emulator equivalence or model cross-engine execution.'), indent=2), encoding='utf8')
    print('Two complete128byte cross-engine outcomes match')


if __name__ == '__main__':
    main()
