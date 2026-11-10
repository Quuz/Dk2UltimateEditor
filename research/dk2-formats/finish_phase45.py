"""Publish AP consumer evidence without promoting unknown flags to decoded."""
from pathlib import Path
import hashlib,json
from finish_phase44 import insert_after_title
ROOT=Path(__file__).resolve().parent

def main():
    v=json.loads((ROOT/'phase45-validation.json').read_text(encoding='utf8'));assert v['native_emulations']==36 and v['exact_in_memory_container_patches']==60
    insert_after_title('INTEGRATION_RU.md','Актуальный этап45: пакет56 компонентов. `action_point_visibility_semantics` / `ACTION_POINT_VISIBILITY_RU.md`: runtime AP10 раскрывает inclusive rectangle через454B80; tile2000 нельзя переносить в rawMapFlags. Перекрытия не повторяют cell events, area event остаётся для каждой AP. ToolBox20 возвращает последний matching runtime ID, включая0.36 native cases/60 patches. Unknown04/08 и KeeperC8 остаются открытыми.')
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8').replace('Актуальный этап44: пакет55 компонентов.','Предыдущий этап44 расширил пакет до55 компонентов.').replace('55 компонентов, SHA-256 каждой исходной схемы','56 компонентов, SHA-256 каждой исходной схемы');p.write_text(t,encoding='utf8')
    insert_after_title('REPORT_RU.md','Этап45,26 октября: `ACTION_POINT_VISIBILITY_RU.md` — реальный AP reveal10 обходит inclusive bounds, сохраняет прочие tile bits при OR2000, отдельно делегирует room types3/4/8. Per-tile events не повторяются при перекрытии; area event6 остаётся. Toolbox20 выбирает последний matching linked-list ID, включая0.36 native cases,625 boundary checks,60 raw FlagsWORD patches;106 файлов3429 AP,227 reveal/10 toolbox.4 livePE ranges, пакет56. Room handler/rendering/session event consumer — fixtures, неизвестные04/08 не названы.')
    p=ROOT/'REPORT_RU.md';t=p.read_text(encoding='utf8').replace('Пакет для редактора содержит55 компонентов.','Пакет для редактора содержит56 компонентов.');p.write_text(t,encoding='utf8')
    insert_after_title('ENGINE_FINDINGS_RU.md','## ActionPoint: native reveal10 и toolbox20, этап45\n\n`ACTION_POINT_VISIBILITY_RU.md`: fragment50C05D..50C170 + full454B80/517480 подтверждает inclusive rectangle и OR runtime tileWORD08 bit2000 с сохранением прочих bits. Already-set пропускает callbacks; room definitionBYTE452=3/4/8 делегирует4E8B80 (fixture). Cell event3 один раз при переходе, area event6 на каждую AP. Full510E20 выбирает последний matching20 ID37, включая0.36 cases,60 raw patches,4 fullPE ranges. Это runtime map16, неKLD cell4. Unknown04/08 остаются открытыми.')
    insert_after_title('SOURCES.md','## Этап45: AP visibility consumers\n\n`ACTION_POINT_VISIBILITY_RU.md`, компонент56 `action-point-visibility-semantics.json`. Primary whole50B5B0,454B80,517480,510E20;4 repeated disassembly/livePE ranges. Исполнен только world fragment50C05D..50C170, полные tile/helper/toolbox;36 native cases,30 reveal/6 toolbox. Combined result buffer16384 bytes; event/update/room callbacks — fixtures.625 API checks,60 exact raw patches,106 AP-bearing files3429 records. Failed event capture4/16 preserved/excluded. Unknown scans phase45 are bounded candidates, not proof of non-use.')
    for name in ['ACTION_POINTS_RU.md','ACTION_POINT_EDITOR_RU.md']:
        insert_after_title(name,'Продолжение этап45 — `ACTION_POINT_VISIBILITY_RU.md`: native gameplay consumer10 подтверждает inclusive reveal bounds и tile events;20 выбирает последнюю подходящую точку runtime-списка. Unknown04/08 остаются без установленного назначения.')
    unresolved=['KeeperSpellsC8 purpose; only exact unsigned storage path established','AP04/08 gameplay meaning','Creature AbilityJump75C/PickUpPriority6EB gameplay consumers','Terrain198/224,Traps204,Objects34C gameplay consumers','CreatureSpells Alternative Shot/Room effective gameplay replacement','WanderRadius418 effect and camera global759AEC meaning']
    searches=[]
    for n in ['phase45-keeper-getter-alias-leads.json','phase45-ap-overlapping-read-leads.json']:
        p=ROOT/n;searches.append(dict(file=n,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),scope='Candidate search only; no assigned unknown meaning or proof of unused field'))
    status=dict(phase=45,status='AP_visibility_consumers_verified; remaining_meanings_open',schema='action-point-visibility-semantics.json',validation='phase45-validation.json',searches=searches,unresolved=unresolved,next_action='Continue unknown field identity/alias traces or older executable consumers. Preserve unknown fields and avoid re-counting known storage/getter paths as new meanings.',scope='Research only; original EXEs/data unchanged, no GUI/gameplay run.')
    (ROOT/'phase45-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    p=json.loads((ROOT/'editor-format-package.json').read_text(encoding='utf8'));assert len(p['components'])==56
    for key,row in p['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==p[key]
    print('Phase45 published:36 native cases,60 raw patches,56 components; goal meanings remain open')
if __name__=='__main__':main()
