"""Audit active Crypt effects without promoting unknown counter meanings."""
import json
import build_crypt_active_action as b

def main():
    package=b.load('editor-format-package.json');old=b.load('phase180-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and package[b.KEY]==s and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==158 and len(old['components'])==157
    for k,r in old['components'].items():assert package['components'][k]==r and b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    assert s['counts']['Crypt_owner_counter_meanings_still_unassigned']==10 and not s['priority']['block_complete']
    integration=(b.ROOT/'INTEGRATION_RU.md').read_text(encoding='utf-8')
    assert 'текущий блок — Crypt' in integration[:600] and 'Актуальный этап180:' in integration[:2000]
    docs=b.ROOT/'CRYPT_ACTIVE_ACTION_RU.md';text=docs.read_text(encoding='utf-8')
    assert '59сцен' in text and 'счётчиков владельца' in text and 'в один прогон' in text
    d=dict(phase=180,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),
        preserved_prior_components=157,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        documentation=b.link(docs.name),source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase180-validation.json',d);print(json.dumps(d))

if __name__=='__main__':main()
