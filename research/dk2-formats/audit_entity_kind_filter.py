"""Audit complete native buffers, pointer origins, package and original hashes."""
from pathlib import Path
import json
from build_entity_kind_filter import ROOT,load,digest,primary
from emulate_entity_kind_filter import cases,build_case
from native_entity_kind_filter import first_match


def main():
    s = load('entity-kind-filter-semantics.json')
    functions,blocks,match = primary()
    assert s['primary_program_bytes'] == functions and s['primary_data_ranges'] == blocks and s['whole_function_equivalence'] == match
    assert len(cases()) == len(s['native_emulations']) == 32
    for c,row in zip(cases(),s['native_emulations']):
        path = ROOT/'ghidra'/row['file']
        saved = json.loads(path.read_text())
        args,want,native = build_case(c)
        r = saved['result']
        assert row['sha256'] == digest(path.read_bytes()) and saved['input'] == args and saved['expected_native'] == list(native)
        assert saved['case'] == json.loads(json.dumps(c)) and saved['expected_registers'] == want
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in want.items())
        regions = {int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        for fn in functions:
            if fn['program'] == args['program']:
                assert all(a+len(b)<=fn['address'] or a>=fn['address']+fn['length'] for a,b in regions.items())
    checks = 0
    invalid = [lambda:first_match(65536,{},3,3,0),lambda:first_match(0,{},True,3,0),
               lambda:first_match(0,{},3,-1,0),lambda:first_match(0,{0:dict(pointer=1,kind=3,owner=65536,next=0)},3,3,0),
               lambda:first_match(0,{0:dict(pointer=1,kind=4,owner=3,next=0)},3,3,0)]
    for fn in invalid:
        try:
            fn()
        except ValueError:
            checks += 1
        else:
            raise AssertionError('Invalid/cyclic fixture accepted')
    assert len(s['pointer_review']['previously_type_rooted_DX_reads']) == 2
    assert len(s['pointer_review']['newly_type_rooted_DX_reads']) == 4 and s['untyped_route']['read'] == 0x4A7EBF
    files = 0
    for row in s['corpus_hashes']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
        files += 1
    assert files == 82
    for file,sha in load('executable-hash-verification.json').items():
        assert digest((ROOT.parent.parent/file).read_bytes()) == sha
    old = load('creature-version-layout-semantics.json')['old_source']
    assert digest(Path(old['original_path']).read_bytes()) == digest(Path(old['path']).read_bytes()) == old['sha256']
    rc3 = load('camera-catalogue-semantics.json')['rc3']
    assert digest(Path(rc3['original_file']).read_bytes()) == digest(Path(rc3['research_copy']).read_bytes()) == rc3['sha256']
    pack = load('editor-format-package.json')
    assert len(pack['components']) == 67 and pack['trap_semantics']['phase69_entity_filter'] == s
    assert pack['door_semantics']['phase69_consumer_review']['sha256'] == digest((ROOT/'entity-kind-filter-semantics.json').read_bytes())
    for key,row in pack['components'].items():
        b = (ROOT/row['file']).read_bytes()
        assert digest(b) == row['sha256'] and json.loads(b) == pack[key]
    v = dict(phase=69,date='2026-10-27',native_full_filter_calls=32,complete_executed_native_functions=2,
             complete_primary_pe_functions=9,primary_dispatch_data_ranges=1,native_filter_whole_bytes=91,
             newly_type_rooted_DX_reads=4,total_type_rooted_DX_reads=6,untyped_DX_command_routes=1,
             source_files_hashchecked=files,source_executables_unchanged=4,package_components=67,
             new_raw_patch_cases=0,api_boundary_and_cycle_diagnostics=checks,
             research_writes_to_game_data=0,modified_gameplay_runs=0,
             unknown204_and_door236_gameplay_still_open=True,limitations=s['limitations'])
    (ROOT/'phase69-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))


if __name__=='__main__':
    main()
