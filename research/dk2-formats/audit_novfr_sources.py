"""Independent consistency checks for the pinned external-source review."""
import hashlib
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def main():
    d = json.loads((ROOT/'novfr-source-review.json').read_text(encoding='utf-8'))
    source = Path(d['source_root'])
    manifest = json.loads((ROOT/d['provenance']['manifest']).read_text(encoding='utf-8'))
    assert len(manifest) == 982
    for x in manifest + d['template_manifest']:
        b = (source/x['path']).read_bytes()
        assert len(b) == x['bytes']
        assert hashlib.sha256(b).hexdigest() == x['sha256']
        assert hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest() == x['git_blob']
    assert len(d['template_manifest']) == 7
    assert d['counts']['literal_typed_catalogue_writes'] == len(d['catalogue_writes']) == 281
    assert d['counts']['literal_typed_reset_reads'] == len(d['catalogue_reset_reads']) == 259
    assert d['counts']['writer_families'] == dict(Counter(x['family'] for x in d['catalogue_writes']))
    assert len(d['global_variable_labels']) == 193
    assert len({x['type'] for x in d['global_variable_labels']}) == 193
    assert all(x['current_runtime'] and 'UNKNOWN' not in x['current_runtime']['baseline_name']
               for x in d['global_variable_labels'])
    assert sorted(d['display_only_variable_types']) == [155,158,220,221]
    assert sorted(x['type'] for x in d['runtime_variable_types_not_exposed']) == [19,29,74,186,233,241]
    for group in ['catalogue_writes', 'catalogue_reset_reads']:
        for x in d[group]:
            b = (source/x['source']).read_bytes()
            try:
                txt = b.decode('utf-8-sig')
            except UnicodeDecodeError:
                txt = b.decode('cp1252')
            line = txt.splitlines()[x['line']-1].strip()
            assert hashlib.sha256(line.encode('utf-8')).hexdigest() == x['statement_sha256']
            assert int(x['offset_hex'],16) == x['offset']
    for x in d['open_field_review']:
        if x['label'] != 'unknown flag bits 0x800/0x1000':
            assert not x['explicit_reads'] and not x['explicit_writes'], x['label']
    assert not d['keyword_evidence']['work_modifier']
    assert len(d['keyword_evidence']['crypt']) == 2
    assert len(d['keyword_evidence']['lightning_unsupported']) == 1
    assert len(d['keyword_evidence']['emotionless_unsupported']) == 1
    generator = (source/'Source/Sources/Compiler/Compiler_Generate.c').read_bytes().decode('cp1252')
    assert 'if (MapVars[X].Icon) continue;' in generator
    assert 'if (Map.Properties.Variables[MapVars[X].Id] == MapVars[X].Variable) continue;' in generator
    assert '*((BYTE *)(Trap+0x1EC)) &= ~(0x01|0x02|0x20);' in generator
    assert 'if (MapGlobalVariables.Traps[Offset].Disarmable)' in generator
    assert 0x10 & ~d['trap_disarmable_export']['clear_mask'] == 0x10
    assert d['trap_disarmable_export']['templates_with_flag_set']
    package = (ROOT/'editor-format-package.json').read_bytes()
    assert hashlib.sha256(package).hexdigest() == d['package_preserved']['sha256']
    assert len(json.loads(package)['components']) == d['package_preserved']['components'] == 143
    assert (ROOT/'NOVFR_SOURCE_REVIEW_RU.md').is_file()
    out = {'passed': True, 'verified_source_blobs': len(manifest), 'verified_templates': 7,
           'literal_catalogue_writes': 281, 'literal_reset_reads': 259,
           'global_variable_rows': 193, 'new_gameplay_field_assignments': 0,
           'package_components_unchanged': 143, 'new_native_game_executions': 0}
    (ROOT/'novfr-source-review-validation.json').write_text(
        json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(out,ensure_ascii=False))


if __name__ == '__main__':
    main()
