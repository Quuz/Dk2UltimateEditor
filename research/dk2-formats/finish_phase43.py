"""Refresh current entry points and retain remaining research questions."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
def main():
    updates={'INTEGRATION_RU.md':[('Актуальный этап42: пакет53 компонента.','Предыдущий этап42 расширил пакет до53 компонентов.'),('53 компонента, SHA-256 каждой исходной схемы','54 компонента, SHA-256 каждой исходной схемы')],
             'REPORT_RU.md':[('Пакет для редактора содержит53 компонента.','Пакет для редактора содержит54 компонента.')]}
    for name,pairs in updates.items():
        p=ROOT/name;t=p.read_text(encoding='utf8')
        for a,b in pairs:t=t.replace(a,b)
        p.write_text(t,encoding='utf8')
    schema=json.loads((ROOT/'creature-extension-semantics.json').read_text(encoding='utf8'));validation=json.loads((ROOT/'phase43-validation.json').read_text(encoding='utf8'));assert len(schema['native_emulations'])==validation['native_emulations']==38
    status=dict(phase=43,status='creature_extension_native_provenance_pass_complete; remaining_catalogue_meanings_open',schema='creature-extension-semantics.json',validation='phase43-validation.json',native_facts=['Game1545+opaque80 preserve84 preexisting stack bytes in bounded-name writer; model flags encoded to separate1599.', 'Last4 source is savedcontextECX bytes1..3 plus probe return4A7F10 low byte10; native context54F550 matchesF5540010 in all3504 corpus long records.', 'Both converters ignore80/last4; full known-stride parsers tested including game adjacent mid overwrite88.'],unresolved=['Creature AbilityJump75C/PickUpPriority6EB gameplay consumers', 'Terrain198/224,Traps204,Objects34C gameplay consumers', 'KeeperSpellsC8 purpose', 'CreatureSpells Alternative Shot/Room gameplay replacement outside inspected loader', 'Historical producer/version of each opaque byte buffer; native writer mechanism does not certify every old file'],next_action='Return to remaining catalogue unknowns using native alias traces/other executable readers. Do not repeat the same displacement-only negative scan as new proof.',scope='No real GUI/gameplay run, original EXEs/data unchanged, sources only in research directory.')
    (ROOT/'phase43-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    package=json.loads((ROOT/'editor-format-package.json').read_text(encoding='utf8'));assert len(package['components'])==54 and package['creature_extension_semantics']==schema
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==package[key]
    for name in ['CREATURE_EXTENSION_RU.md','INTEGRATION_RU.md','REPORT_RU.md','ENGINE_FINDINGS_RU.md','SOURCES.md','KING_LIGHTNING_RU.md']:assert (ROOT/name).exists()
    print('Phase43 saved:38 native cases,2484 exact patches,54 verified package components')
if __name__=='__main__':main()
