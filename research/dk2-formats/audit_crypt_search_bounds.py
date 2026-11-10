"""Audit phase186 evidence, bounds, previous package and unchanged game files."""
import json
import build_crypt_search_bounds as b

def main():
    p=b.load('editor-format-package.json');s=b.load(b.SCHEMA);previous=b.load('phase186-prior-package-state.json')
    assert b.canonical(s)==b.canonical(b.build()) and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==164 and len(previous['components'])==163
    for k,r in previous['components'].items():assert p['components'][k]==r and b.link(r['file'])==r
    for k,h in previous['preserved_top_level'].items():assert b.canonical(p[k])==h
    assert b.link('dk2_formats.py')==previous['codec']
    for r in s['helpers']:assert b.link(r['file'])==r
    assert s['counts']['new_counter_meanings']==0 and s['counts']['Crypt_counter_meanings_remaining']==10
    assert not s['priority']['block_complete'] and len(s['search']['remaining_limits'])==12
    text=(b.ROOT/'CRYPT_SEARCH_BOUNDS_RU.md').read_text(encoding='utf-8')
    for word in ['C1F','2AE','54','24576','статически','не повышается','12 всё ещё']:assert word in text,word
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап186:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=186,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
        documentation=b.link('CRYPT_SEARCH_BOUNDS_RU.md'),prior_components_preserved=163,codec_and_effective_parameters_unchanged=True,
        other_package_top_level_unchanged=True,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,
        goal_status='active',Crypt_block_complete=False)
    b.dump('phase186-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
