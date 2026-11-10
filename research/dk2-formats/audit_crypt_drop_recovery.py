"""Audit continuous bounded native release/recovery/Crypt and unchanged sources."""
import json
import build_crypt_drop_recovery as b

def main():
    p=b.load('editor-format-package.json');old=b.load('phase185-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and p[b.KEY]==s and p['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(p['components'])==163 and len(old['components'])==162
    for k,r in old['components'].items():assert p['components'][k]==r and b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(p[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    assert s['counts']['whole_Creature_ticks']==219 and s['counts']['checkpoint_snapshots']==63
    assert s['counts']['Crypt_counter_meanings_remaining']==10 and not s['priority']['block_complete']
    text=(b.ROOT/'CRYPT_DROP_RECOVERY_RU.md').read_text(encoding='utf-8')
    for fragment in ['9 принятых','raw128B','не означают','приращение','clock39','по15']:assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап185:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=185,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('CRYPT_DROP_RECOVERY_RU.md'),
        prior_components_preserved=162,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase185-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
