"""Audit bounded Crypt participant chains and preserve full unfinished goal."""
import json
import build_crypt_participant_chain as b

def main():
    package=b.load('editor-format-package.json');old=b.load('phase181-prior-package-state.json');s=b.load(b.SCHEMA)
    assert b.canonical(s)==b.canonical(b.build()) and package[b.KEY]==s and package['components'][b.KEY]==b.link(b.SCHEMA)
    assert len(package['components'])==159 and len(old['components'])==158
    for k,r in old['components'].items():assert package['components'][k]==r and b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    for k,h in old['preserved_top_level'].items():assert b.canonical(package[k])==h
    assert b.link('dk2_formats.py')==old['codec']
    for r in s['helpers']:assert b.sha((b.ROOT/r['file']).read_bytes())==r['sha256']
    assert s['counts']['Crypt_counter_meanings_remaining']==10 and not s['priority']['block_complete']
    docs=b.ROOT/'CRYPT_PARTICIPANT_CHAIN_RU.md';text=docs.read_text(encoding='utf-8')
    assert '19сцен' in text and 'не доказательство' in text and 'state6' in text
    assert 'Актуальный этап181:' in (b.ROOT/'INTEGRATION_RU.md').read_text(encoding='utf-8')[:2000]
    result=dict(phase=181,passed=True,counts=s['counts'],schema=b.link(b.SCHEMA),package=b.link('editor-format-package.json'),documentation=b.link(docs.name),
        prior_components_preserved=158,codec_and_effective_parameters_unchanged=True,other_package_top_level_unchanged=True,
        source_writes=0,goal_status='active',Crypt_block_complete=False)
    b.dump('phase181-validation.json',result);print(json.dumps(result))

if __name__=='__main__':main()
