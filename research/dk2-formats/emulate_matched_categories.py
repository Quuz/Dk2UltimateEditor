"""Replay real Shot damage with resistance category equal to native Shot category.

Frozen phase114 fixtures remain unchanged. Correct category8 replaces reference
spell18 for Knives; category6 replaces spell11 for Hail Storm. Full output images
are predicted from prior measured baselines by explicit physical field changes.
"""
from pathlib import Path
import hashlib, json
import emulate_shot_damage as native

ROOT = Path(__file__).resolve().parent


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def cases():
    return [dict(c, res_type=c['category'], name='matched-' + c['name'])
        for c in native.probe.cases()
        if c['name'] in ['hit-res0', 'hit-res100', 'hit-res255',
            'area-res0', 'area-res100', 'area-res255',
            'range-res0', 'range-res100', 'range-res255']]


def expected(case):
    wanted = native.expected(case)
    for address, offset in [(native.probe.target.RAW, 0xE0C),
            (native.probe.target.MID, 0x11FC),
            (native.probe.target.attack.TARGET_DEF, 0x61C)]:
        wanted[address] = bytearray(wanted[address])
        for index in range(4):
            wanted[address][offset + 2 * index] = case['res_type']
    address = native.probe.cf.DEFINITION
    wanted[address] = bytearray(wanted[address])
    wanted[address][0x77A] = case['res_type']
    return wanted


def path_for(args):
    digest = sha((json.dumps(args, sort_keys=True) + sha(Path(__file__).read_bytes())).encode())[:12]
    return ROOT / 'ghidra' / f'phase125-matched-categories-{digest}.json'


def verify(case, args, checks, result):
    assert case['res_type'] == case['category']
    post = native.verify(case, args, checks, result)
    assert post == expected(case), case['name']
    loaded = post[native.probe.target.attack.TARGET_DEF]
    assert loaded[0x61C:0x624] == bytes([case['category'], case['value']]) * 4
    return dict(category=case['category'], resistance_type=case['res_type'],
        value=case['value'], HP=native.outcome(case)['HP'], HP_calls=1,
        resistance_pairs_read_during_damage=False,
        full_context_regions=len(post), full_context_bytes=sum(map(len, post.values())))


def main():
    rows = []
    for case in cases():
        args, checks = native.probe.build(case)
        path = path_for(args)
        if path.exists():
            saved = json.loads(path.read_text(encoding='utf8'))
            assert saved['input'] == args and saved['case'] == case
        else:
            result = native.probe.target.cpu.run(native.EXE, args, list(checks.items()),
                native.probe.f.OBS, native.probe.target.attack.TARGET, 1599)
            saved = dict(case=case, input=args, result=result)
            path.write_text(json.dumps(saved, indent=2), encoding='utf8')
        outcome = verify(case, args, checks, saved['result'])
        rows.append(dict(case=case['name'], file=str(path.relative_to(ROOT)),
            sha256=sha(path.read_bytes()), outcome=outcome))
        print(case['name'], outcome)
    (ROOT / 'phase125-matched-category-emulations.json').write_text(
        json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
