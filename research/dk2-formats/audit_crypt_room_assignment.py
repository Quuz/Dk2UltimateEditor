"""Audit generic Crypt assignment and bounded separate release/recovery paths."""
import json
import build_crypt_room_assignment as b

def main():
    package=b.load('editor-format-package.json');old=b.load('phase183-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and package[b.KEY]==s and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==161 and len(old['components'])==160
    for k,r in old['components'].items():assert package['components'][k]==r and b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    assert s['counts']['Crypt_counter_meanings_remaining']==10 and not s['priority']['block_complete']
    assert s['room_table']['Crypt']['HateJob_ID']==0 and s['room_table']['Crypt']['start_state']==274
    assert s['counts']['HateJob_checks']==s['counts']['HateJob_zero_early_returns']
    assert len(s['storage']['source_files'])==7 and len(s['storage']['Creature_hate1_exact_edits'])==4
    assert s['storage']['source_writes']==0
    text=(b.ROOT/'CRYPT_ROOM_ASSIGNMENT_RU.md').read_text(encoding='utf-8')
    for fragment in ['17 принятых','не новые поля','не доказан','не исполнена целиком','128B','Job ID0']:assert fragment in text,fragment
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:assert 'Актуальный этап183:' in (b.ROOT/name).read_text(encoding='utf-8')[:2000]
    result=dict(phase=183,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link('CRYPT_ROOM_ASSIGNMENT_RU.md'),
        prior_components_preserved=160,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        exact_source_roundtrips=7,Creature_raw_hate_exact_container_edits=4,source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase183-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
