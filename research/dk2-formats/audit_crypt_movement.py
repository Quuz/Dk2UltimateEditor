"""Audit actual AI Crypt walking proof, prior159 components and source preservation."""
import json
import build_crypt_movement as b

def main():
    package=b.load('editor-format-package.json');old=b.load('phase182-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and package[b.KEY]==s and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==160 and len(old['components'])==159
    for k,r in old['components'].items():assert package['components'][k]==r and b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    assert s['counts']['Crypt_counter_meanings_remaining']==10 and not s['priority']['block_complete']
    assert s['counts']['whole_Creature_ticks']==388 and s['storage']['inverse_edit_restores_source']
    assert len(s['storage']['source_files'])==7 and s['storage']['source_writes']==0
    text=(b.ROOT/'CRYPT_MOVEMENT_RU.md').read_text(encoding='utf-8')
    for fragment in ['Девять принятых сцен','не доказательство','через ИИ','handle0','World+A3C7=0','десяти']:assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап182:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=182,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('CRYPT_MOVEMENT_RU.md'),
        prior_components_preserved=159,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        exact_source_roundtrips=7,exact_in_memory_edit_bytes=4,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase182-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
