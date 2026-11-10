"""Publish verified phase44 entry points while retaining open format questions."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent

def insert_after_title(name,paragraph):
    p=ROOT/name;t=p.read_text(encoding='utf8')
    if paragraph not in t:
        first,rest=t.split('\n',1);t=first+'\n\n'+paragraph+'\n'+rest
    p.write_text(t,encoding='utf8')

def main():
    v=json.loads((ROOT/'phase44-validation.json').read_text(encoding='utf8'));assert v['native_emulations']==68 and v['exact_in_memory_container_patches']==522
    insert_after_title('INTEGRATION_RU.md','Актуальный этап44: пакет55 компонентов. `keeper_pipeline_semantics` / `KEEPER_PIPELINE_RU.md`: полный writer406, converters526, unsigned C8 double-view и scalar loader122. RawE0/E1 writer оставляет stack bytes, converters игнорируют; C8 gameplay meaning остаётся неизвестным.68 native cases,522 точечные пересборки,82 файла1066 записей. `native_keeper_pipeline.py` сохраняет raw и неизвестные flags.')
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8').replace('Актуальный этап43: пакет54 компонента.','Предыдущий этап43 расширил пакет до54 компонентов.').replace('54 компонента, SHA-256 каждой исходной схемы','55 компонентов, SHA-256 каждой исходной схемы');p.write_text(t,encoding='utf8')
    insert_after_title('REPORT_RU.md','Этап44,26 октября: `KEEPER_PIPELINE_RU.md` —68 native cases подтвердили полный writer Keeper406, initializer/converters526, unsigned C8 model-path и loader122. E0/E1 оказались unassigned stack bytes writer и ignored raw bytes обоих converters; в13 standalone записях они6B77. C8FE0C0000 точно сохраняется, назначение неизвестно.646 API checks,522 in-memory patches,82 файла1066 записей,15 livePE ranges; пакет55 компонентов. Исходные данные не изменены.')
    p=ROOT/'REPORT_RU.md';t=p.read_text(encoding='utf8').replace('Пакет для редактора содержит54 компонента.','Пакет для редактора содержит55 компонентов.');p.write_text(t,encoding='utf8')
    insert_after_title('ENGINE_FINDINGS_RU.md','## KeeperSpells: полный writer и два stack bytes, этап44\n\n`KEEPER_PIPELINE_RU.md`: full4A99B0 не назначает rawE0/E1 при bounded имени, converters56BAB0/42B720 игнорируют их. Сохранены22 полных writer snapshots, включая четыре seeds и null/reordered arrays; N/S/D использует count slots, null payload пропускается. C8→mid140→runtime3A без TPS; editor zero-extendsDWORD, /4096→double178, full writer восстанавливает low32, включаяFE0C0000/FFFFFFFF. Имя C8 не установлено.68 native cases,522 patches,82files1066records;15 full livePE ranges. Raw/unknown flags сохранять.')
    insert_after_title('SOURCES.md','## Этап44: KeeperSpells writer, C8 storage и opaqueE0/E1\n\n`KEEPER_PIPELINE_RU.md`, компонент55 `keeper-pipeline-semantics.json`. Primary: game56B990/56BAB0/4F7CE0, editor42B600/42B720/4A25E0/4A99B0, flags420230, resources41CE60, getters424EA0/424EC0, string4AB3B0/6361A0, ftol4ACDA8; date4AB320 inspected/substituted.15 full disassembly/livePE ranges,1496 instructions.68 native cases compare complete writer output,526-byte converters and122-byte scalar definitions. Constructor/GUI/gameplay/resource success branches not certified.646 API checks,522 exact patches,82files1066records.17inputs2EXE unchanged; C8 gameplay purpose remains unresolved.')
    status=dict(phase=44,status='keeper_pipeline_storage_pass_complete; remaining_catalogue_meanings_open',schema='keeper-pipeline-semantics.json',validation='phase44-validation.json',native_facts=['RawE0/E1 preserved stack bytes of full bounded-name Pro writer; both full converters ignore them.', 'C8 full unsigned editor double/ftol path retains all32 bits at tested boundaries; game copies to3A without TPS.', 'Complete writer emits physical array order but skips null slots without correcting N/S/D.'],unresolved=['KeeperSpellsC8 gameplay consumer/purpose remains unresolved; storage proof does not name it','Creature AbilityJump75C/PickUpPriority6EB gameplay consumers','Terrain198/224,Traps204,Objects34C gameplay consumers','CreatureSpells Alternative Shot/Room effective gameplay replacement','Historical producer/version of each opaque buffer; no universal origin inference'],next_action='Trace remaining unknown fields through aliases, exact definition getters or older executable consumers; do not count repeated displacement-only negative searches as decoded fields.',scope='Research files only, no GUI/gameplay run, original EXEs/data unchanged.')
    (ROOT/'phase44-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    p=json.loads((ROOT/'editor-format-package.json').read_text(encoding='utf8'));assert len(p['components'])==55
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==p[key]
    assert (ROOT/'KEEPER_PIPELINE_RU.md').exists()
    print('Phase44 published:68 native cases,522 patches,55 schema components; purposeC8 remains open')
if __name__=='__main__':main()
