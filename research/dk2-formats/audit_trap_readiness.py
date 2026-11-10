"""Audit saved native results, independent boundaries and raw/source preservation."""
import json
from pathlib import Path
from build_trap_readiness import ROOT,load,digest,primary
from emulate_trap_readiness import cases,build_case
import dk2_formats as fmt


def main():
    s = load('trap-readiness-semantics.json')
    functions,blocks = primary()
    assert s['primary_program_bytes'] == functions and s['primary_data_ranges'] == blocks
    assert len(cases()) == len(s['native_emulations']) == 42
    returns = {}
    for c,row in zip(cases(),s['native_emulations']):
        path = ROOT/'ghidra'/row['file']
        saved = json.loads(path.read_text())
        args,want,native = build_case(c)
        r = saved['result']
        assert row['sha256'] == digest(path.read_bytes()) and saved['case'] == c
        assert saved['input'] == args and saved['expected_registers'] == want and saved['expected_native_return'] == native
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items())
        returns[c['name']] = native
        regions = {int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        for fn in functions:
            if fn['program'] == args['program']:
                assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in regions.items())
    for prefix in ['d7a','93e']:
        for label,value in [('insufficient',2),('zero-cost',4),('equal-mana',4),('above-mana',2),('negative-cost',4),
                            ('minimum-cost',4),('negative-mana',2),('state-one',3),('state-high',3),('flag-off',4),
                            ('exempt-owner',4),('instance-flag',1),('future',0),('deadline-equal',4),('deadline-high',0),
                            ('tick-high',4),('early-null',1)]:
            assert returns[f'{prefix}-{label}'] == value
        for n in [0,1,32768,65535]:
            assert returns[f'{prefix}-unknown-{n}'] == 4
    source_files = trap_records = 0
    for row in s['corpus']['files']:
        raw = Path(row['file']).read_bytes()
        assert digest(raw) == row['sha256']
        parts = fmt.parse(raw)
        assert b''.join(q.pack() for q in parts) == raw
        source_files += 1
        trap_records += sum(len(q.records) for q in parts if fmt.FAMILIES.get(q.signature)=='Traps')
    assert (source_files,trap_records) == (82,1293)
    for file,sha in load('executable-hash-verification.json').items():
        assert digest((ROOT.parent.parent/file).read_bytes()) == sha
    old = load('creature-version-layout-semantics.json')['old_source']
    assert digest(Path(old['original_path']).read_bytes()) == digest(Path(old['path']).read_bytes()) == old['sha256']
    rc3 = load('camera-catalogue-semantics.json')['rc3']
    assert digest(Path(rc3['original_file']).read_bytes()) == digest(Path(rc3['research_copy']).read_bytes()) == rc3['sha256']
    pack = load('editor-format-package.json')
    assert len(pack['components']) == 67 and pack['trap_semantics']['phase68_readiness'] == s
    assert pack['door_semantics']['phase68_readiness_review']['sha256'] == digest((ROOT/'trap-readiness-semantics.json').read_bytes())
    for key,row in pack['components'].items():
        b = (ROOT/row['file']).read_bytes()
        assert digest(b) == row['sha256'] and json.loads(b) == pack[key]
    v = dict(phase=68,date='2026-10-27',native_full_readiness_calls=42,complete_executed_native_functions=6,
             complete_primary_pe_functions=10,primary_rtti_data_ranges=3,DX_candidate_reads_type_classified=2,
             DX_other_candidate_reads_still_require_type_review=5,source_files_roundtripped=82,trap_records=1293,
             source_executables_unchanged=4,package_components=67,new_raw_patch_cases=0,
             research_writes_to_game_data=0,modified_gameplay_runs=0,unknown204_and_door236_gameplay_open=True,
             limitations=s['limitations'])
    (ROOT/'phase68-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))


if __name__=='__main__':
    main()
