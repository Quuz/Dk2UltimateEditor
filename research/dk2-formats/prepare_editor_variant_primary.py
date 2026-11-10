"""Re-attest prior Pro Ghidra bytes against both exact supplied editor files.

This is byte evidence, not a new independent CPU replay of every editor path.
"""
import json
import re
from pathlib import Path
from scan_editor_variants import ROOT, read_profiles, pe_read, sha, build


def link(path):
    return dict(file=str(path.relative_to(ROOT)), sha256=sha(path.read_bytes()))


def prepare():
    profiles = read_profiles()
    result = build()
    found = {}
    def visit(value, manifest):
        if isinstance(value, dict):
            if value.get('program') == 'DK2 Editor pro.exe' and all(k in value for k in ['address','length','memory']):
                address, length = value['address'], value['length']
                key = address, length
                if key not in found:
                    memory = ROOT/'ghidra'/value['memory']
                    saved = json.loads(memory.read_text(encoding='utf8'))
                    raw = bytes(saved['data'])
                    assert len(raw) == length == saved['length']
                    assert int(saved['address'], 16) == address
                    assert raw == pe_read(profiles['pro'], address, length)
                    old = pe_read(profiles['stock'], address, length)
                    changed = [address+i for i, (a,b) in enumerate(zip(old,raw)) if a != b]
                    found[key] = dict(address=address, length=length,
                        original_manifest=link(manifest), memory=link(memory),
                        Pro_bytes_sha256=sha(raw), stock_bytes_sha256=sha(old),
                        equal=old == raw, different_addresses=changed)
            for item in value.values():
                visit(item, manifest)
        elif isinstance(value, list):
            for item in value:
                visit(item, manifest)
    for path in sorted(ROOT.glob('phase*-primary-manifest.json')):
        match = re.match(r'phase(\d+)-', path.name)
        if match and int(match[1]) < 117:
            visit(json.loads(path.read_text(encoding='utf8')), path)
    rows = [found[key] for key in sorted(found)]
    assert len(rows) == 232 and sum(r['equal'] for r in rows) == 231
    changed = [r for r in rows if not r['equal']]
    assert len(changed) == 1 and changed[0]['address'] == 0x442C40
    assert changed[0]['different_addresses'] == [0x442C49]
    # Fresh original instruction fragments are saved in the survey, with both
    # byte streams. Pending Ghidra availability does not turn them into replays.
    return dict(phase=117, scope=__doc__, inherited_Ghidra_ranges=rows,
        unique_range_pairs=232, identical_pairs=231, changed_dialog_constructor_pairs=1,
        fresh_original_PE_ranges=result['primary_ranges'],
        new_Ghidra_queries=0, new_CPU_replays=0,
        limitations=['Ranges can overlap; counts are not unique functions or all-branch coverage.',
                     'Saved Pro Ghidra bytes are matched to original files, not a stock Ghidra import.',
                     'No live GUI, entire save operation or unknown-field gameplay execution.'])


if __name__ == '__main__':
    data = prepare()
    (ROOT/'phase117-primary-manifest.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf8')
    print('232 prior Ghidra spans checked: 231 identical; one known dialog-ID difference.')
