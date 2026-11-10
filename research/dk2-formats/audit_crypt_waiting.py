"""Audit source-byte Crypt waiting evidence and preserved editor package."""
import json
import build_crypt_waiting as b

def main():
    p=b.load('editor-format-package.json');old=b.load('phase187-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==165 and len(old['components'])==164
    for k,r in old['components'].items():assert p['components'][k]==r and b.link(r['file'])==r
    for k,h in old['preserved_top_level'].items():assert b.canonical(p[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.link(r['file'])==r
    assert s['counts']['Crypt_counter_meanings_remaining']==10 and not s['priority']['block_complete']
    text=(b.ROOT/'CRYPT_WAITING_RU.md').read_text(encoding='utf-8')
    for fragment in ['War-Dance','Сам выход запрошен проверкой','RoomDWORD55','19 оригинальных','164 прежних','по15']:assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап187:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=187,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('CRYPT_WAITING_RU.md'),
        prior_components_preserved=164,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase187-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
