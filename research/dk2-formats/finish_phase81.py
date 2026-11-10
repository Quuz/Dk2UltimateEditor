"""Publish phase81 references and leave all substantial unresolved questions open."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(n,text):
    p=ROOT/n;s=p.read_text(encoding='utf8');heading,rest=s.split('\n',1)
    if text not in s:p.write_text(heading+'\n\n'+text+'\n'+rest,encoding='utf8')

def main():
    v=load('phase81-validation.json');assert v['native_emulation_invocations']==78 and v['exact_container_patches']==2460
    insert('INTEGRATION_RU.md','Актуальный этап 81: `ELEMENT_INITIALIZATION_RU.md` и `element-color-semantics.json.phase81_complete_initialization`. Проверена полная цепочка создания EffectElement: 26 сценариев, 78 эмуляций, 23 успешных состояния с сохранением всех 287 байт. Скорости читаются как signed16; HP и масштаб — unsigned16. Верхняя граница скорости/HP включается, масштаба — исключается. Четыре пары индексов B1/B2 дали одинаковый объект в проверенных условиях; их глобальное назначение остаётся открытым. Проверены 22 диапазона кода, три диапазона таблиц, 82 исходных файла и четыре EXE; 2460 точечных пересборок сохраняют остальные байты. Пакет содержит 67 компонентов. Внешние проверки движка и коэффициенты движения заданы явно; отрисовка, поздние тики и остальные неизвестные не закрыты.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап80:','Предыдущий этап80:',1),encoding='utf8')
    insert('ELEMENT_COLORS_RU.md','Продолжение этапа 81 — `ELEMENT_INITIALIZATION_RU.md`: дополнительно проверены полное создание/регистрация частицы, диапазоны скорости, HP и масштаба, 23 успешных состояния и полные 287 байт объекта. Прежние 26 фрагментных проверок не пересчитываются как новые. Равенство объекта при B1/B2 подтверждено на полной проверенной цепочке, но назначение индексов палитры остаётся неизвестным. Уточнено разделение CBridge и подключённого интерфейса движка.')
    insert('SOURCES.md','## Этап 81: полное создание EffectElement\n\n`ELEMENT_INITIALIZATION_RU.md`, `element-initialization-semantics.json`, `phase81-validation.json`, `phase81-primary-manifest.json`; `native_element_initialization.py`, `emulate_element_initialization.py`, `prepare_element_initialization_primary.py`, `build_element_initialization.py`, `audit_element_initialization.py`, `finish_phase81.py`. 26 сценариев / 78 native-запусков, 23 успешных состояния; полные 287 байт собраны из трёх независимых запусков. Исходные инструкции и таблицы не подменялись. 22 диапазона функций (20 содержат исполненные пути), три диапазона таблиц, 2460 точечных пересборок, 82 файла / 48416 записей и четыре EXE сверены. Полные сырые записи сохраняются; глобальное назначение B1/B2 остаётся открытым.')
    status=dict(phase=81,date='2026-10-28',status='Native EffectElement creation published;remaining format semantics unresolved',
      previous_goal_turn_classification='Progress: complete native spawn,allocation,constructors,ID/map/parent registration and initialization;not just isolated color fragments.',
      schema='element-initialization-semantics.json',supplement='element-color-semantics.json.phase81_complete_initialization',validation='phase81-validation.json',
      findings=['Speed signed16 vs HP/scale unsigned16;upper bounds inclusive for speed/HP,exclusive for scale.',
        'Original CBridge exists/get and engine-interface separation confirmed. Earlier wrong addresses/types are rejected analysis hypotheses,not editor bugs.',
        '26 logical cases/78 replays;23 successful complete287-byte object states,3 allocation/bounds failures.',
        'Palette pairs00,FF00,00FF,FFFF preserve all287 object bytes and RNG under explicit fixtures;global meaning remains unknown.',
        '2460 exact container patches;82 prior-hash inputs/48416 records,4EXEs unchanged,67 package components.'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase81-element-initialization-emulations.json','phase81-primary-manifest.json','phase81-validation.json','element-initialization-semantics.json']],
      scope='Prepared definitions,explicit engine callbacks,uniform motion coefficients,parent spread0,empty map/child list. No raw loader execution,renderer,late ticks,deletion,collision or player-color branch.',
      significant_unknowns_remain=True,goal_achieved=False,
      next_action='Do not repeat26cases/78replays or2460patches. To advance B1/B2 inspect actual nonzero-index resource/renderer consumers with proven object lineage,or choose another unresolved family field. Avoid global conclusions from same-offset hits. KeeperC8,AP04/08,AbilityJump/Priority,Terrain198/224,Traps204,Objects34C,Doors236 and spellE8/E9 global meaning remain open. Existing lightning immunity mask is already confirmed and must not be rediscovered.')
    (ROOT/'phase81-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase81 references published;substantial unknowns remain;original goal not complete.')

if __name__=='__main__':main()
