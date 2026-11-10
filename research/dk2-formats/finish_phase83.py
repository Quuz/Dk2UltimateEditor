"""Publish array/storage progress and keep original full-format goal uncompleted."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(n,text):
    p=ROOT/n;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')
def main():
    v=load('phase83-validation.json');assert v['logical_scenarios']==41 and v['exact_DB_byte_patches']==246
    insert('INTEGRATION_RU.md','Актуальный этап 83: `KEEPER_ARRAY_RU.md` / `keeper-pipeline-semantics.json.phase83_runtime_array`. Проверены 41 native-сценарий, полная raw→intermediate→runtime-вставка KeeperSpell и методы массива. Установлены сортировка ID, вставка дубликата перед равными без универсального last-wins, пропуск записи при заполненной ёмкости с возвратом1, сохранение прежних полей при частичном presence и отсутствие NULL-check в режиме обновления. В двух Battle for the Sun Qz Globals найден одинаковый DB-порядок9 у Raise Dead9/Create Gold13; две реальные строки проверены native-v120. Это не доказательство ошибки редактора. C8 игрового назначения не получило. 20 диапазонов кода/1vtable,82 исходных файла/4EXE сверены;246 DB-пересборок и82 перестановки записей в памяти. Исходные файлы не менялись;67 компонентов,остальные существенные неизвестные открыты.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап 82:','Предыдущий этап 82:',1),encoding='utf8')
    insert('KEEPER_PIPELINE_RU.md','Продолжение этапа 83 — `KEEPER_ARRAY_RU.md`: scalar-loader дополнен реальной raw→mid→array композицией и режимом обновления;41 native-сценарий,включая две неизменённые строки карты с одинаковым DB-порядком. Уточнены дубликаты,ёмкость,частичное наследование и NULL-путь. КопированиеC8 остаётся хранением,его игровое назначение не установлено.')
    insert('KEEPER_LOOKUP_RU.md','Продолжение этапа 83 — `KEEPER_ARRAY_RU.md`: нормальная вставка122-байтных Keeper definitions теперь выполнена,включая сортировку и дубликаты. Это уточняет прежнюю границу «порядок после loader не установлен». Дополнительно проверены first/next/previous/index/order lookup. Физический порядок raw и runtime-порядок различаются;автоматическая нормализация файла не вводится.')
    insert('SOURCES.md','## Этап 83: KeeperSpells runtime-массив\n\n`KEEPER_ARRAY_RU.md`,`keeper-array-semantics.json`,`phase83-validation.json`,`phase83-primary-manifest.json`;`scan_keeper_array_roots.py`,`native_keeper_array.py`,`emulate_keeper_array.py`,`prepare_keeper_array_primary.py`,`build_keeper_array.py`,`audit_keeper_array.py`,`finish_phase83.py`.41 native-сценарий,33 raw-конвертации/38 callback-вызовов,20 диапазонов кода/1vtable;82 файла/1066 записей и4EXE сверены,246 точечных DB-пересборок/82 перестановки целых записей. Две реальные строки Battle for the Sun Qz2 проверены по Order9. Исходные инструкции/vtable и данные сохранены. Старые18 PE-кандидатов остаются нетипизированными;C8 не объявлено расшифрованным.')
    s=dict(phase=83,date='2026-10-28',status='Keeper runtime insertion/overlay/accessors published;C8 and other substantive unknowns remain',
      previous_goal_turn_classification='Progress:phase82 actual third CreatureSpell wrapper and target prediction published;phase83 moves to new Keeper array roots and full raw composition.',
      schema='keeper-array-semantics.json',supplement='keeper-pipeline-semantics.json.phase83_runtime_array',validation='phase83-validation.json',
      findings=['41 native scenarios,33raw-init/converter pairs,38load callbacks;normal sorting/duplicate-selection/capacity drop verified.',
        'Overlay missing-ID passesNULL to scalar loader;3controlled low-memory guard cases,not live crashes or proof of a broken editor.',
        'Native rawDB→mid153→definition6C v120 byte-to-ID mapping;actual RaiseDead9/CreateGold13 bothOrder9 in2mapfiles. Exact2rows give lookup9→9,10→0.',
        'RuntimeC8 copied/retained in fullarray,still no gameplaypurpose. Worldarray roots18DX classified;18oldlinear candidates remain untyped.',
        '20PEspans/1originalvtable;246DBpatches/82recordpermutations;82sourcefiles/4EXEs unchanged;67components.'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase83-keeper-array-emulations.json','phase83-primary-manifest.json','phase83-array-root-candidates.json','phase83-order-duplicate-source.json','phase83-validation.json','keeper-array-semantics.json']],
      significant_unknowns_remain=True,goal_achieved=False,
      scope='ExactDX native composition with preallocatedWorld/arrays,nullgraphics,fixedsound. No rawstreamparser/heapallocator/fullWorldctor/livegame/renderer;partialintermediates/overlay gates explicit.',
      next_action='Do not repeat41nativecases,246DBpatches or82permutations. Follow actual World.v120 callers if assigning OrderInEditor-visible consequences,without automatically editing the2Qz maps. C8stillunresolved;newarray-root work doesnotproveglobalnonuse. Next prioritize actualremaining consumers Terrain198/224,Trap204,Object34C,Door236,CreatureJump/Priority,AP04/08,ElementB1/B2 or upstreamCreatureSpellE8/E9;read prior bounded-search limitations first. OldprogramGUI restriction isnot a fullgoalblock:DX and offlineoldPE remain usable. Lightning immunity already confirmed.')
    (ROOT/'phase83-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Phase83 published;substantial format unknowns remain;goal not achieved.')
if __name__=='__main__':main()
