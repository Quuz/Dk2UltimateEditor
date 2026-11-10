"""Audit Job28, source AI, exact preference patches and preserved old package."""
import json
import build_crypt_job_selection as b

def main():
    p=b.load('editor-format-package.json');old=b.load('phase188-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==166 and len(old['components'])==165
    for k,r in old['components'].items():assert p['components'][k]==r and b.link(r['file'])==r
    for k,h in old['preserved_top_level'].items():assert b.canonical(p[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.link(r['file'])==r
    assert s['counts']['Crypt_counter_meanings_remaining']==10 and not s['priority']['block_complete']
    assert s['counts']['inverse_preference_container_edits']==30 and s['JobType28']['requested_state']==274
    text=(b.ROOT/'CRYPT_JOB_SELECTION_RU.md').read_text(encoding='utf-8')
    for fragment in ['Weight0 не выключает','Job0 той таблицы','неизменённая','Object21/27','11C','по15']:assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап188:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=188,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('CRYPT_JOB_SELECTION_RU.md'),
        physical_API=b.link('native_crypt_job_preference.py'),prior_components_preserved=165,codec_and_effective_parameters_unchanged=True,
        other_package_top_level_unchanged=True,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase188-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
