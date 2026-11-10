"""Publish targeted-spell findings without closing remaining format unknowns."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(n,text):
    p=ROOT/n;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')
def main():
    v=load('phase82-validation.json');assert v['native_emulation_invocations']==81 and v['raw_height_exact_container_patches']==246
    insert('INTEGRATION_RU.md','Актуальный этап 82: `TARGETED_SPELL_RU.md` / `spell-alternative-semantics.json.phase82_targeted_cast`. Полностью выполнен третий путь CreatureSpell `498870`: 27 сценариев, 81 native-запуск, 21 успешный Shot с сохранением всех 371 байт. Подтверждены расчёт XY-упреждения по текущей/предыдущей позиции и добавление половины реальной высоты цели к Z; у Creature это потребитель rawEA4 Height через definition648 с коэффициентом уровня. Shot ID30 здесь проходит обычную траекторию, в отличие от ранее проверенного4984D0. Все371 байт совпали при четырёх парах E8/E9; глобальное назначение альтернатив остаётся неизвестным. 50 диапазонов кода/17 данных сверены с PE, 246 точечных пересборок Height,83 исходных файла/4EXE без изменений,67 компонентов. Полные AI/анимационные события,попадания,renderer и прочие неизвестные не закрыты.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап 81:','Предыдущий этап 81:',1),encoding='utf8')
    insert('CREATURE_SHOT_REQUEST_RU.md','Продолжение этапа 82 — `TARGETED_SPELL_RU.md`: метод498870, отмеченный в исторической схеме этапа79 как static-only, теперь проверен отдельными27 полными сценариями. Оригинальныеsprintf/методы цели/фабрика/регистрация/траектория выполнены; установлен расчёт упреждения и высоты. Историческая граница этапа79 не переписывается. E8/E9 глобального назначения не получили.')
    insert('SPELL_ALTERNATIVES_RU.md','Дополнение этапа 82 — `TARGETED_SPELL_RU.md`: проверен третий полный путь498870. Четыре парыE8/E9 дали одинаковые371 байт Shot; доказательство ограничено описанными входами. Установлены обычная траектория дляID30 в этом методе,предсказаниеXY и половина высоты цели. Глобальная неиспользуемость альтернатив не утверждается.')
    insert('NATIVE_SHOT_SUCCESS_RU.md','Продолжение этапа 82 — `TARGETED_SPELL_RU.md`: добавлен полный третий путь498870 с оригинальнымsprintf,виртуальными методами цели и фабрикой. ДляID30 в этом методе исполняется обычная траектория; особая ветка4984D0 остаётся верной в своём контексте. Новые27 сценариев/81 запуск учитываются отдельно от117 запусков этапа80.')
    insert('SOURCES.md','## Этап 82: третий CreatureSpell и упреждение цели\n\n`TARGETED_SPELL_RU.md`, `targeted-spell-semantics.json`, `phase82-validation.json`, `phase82-primary-manifest.json`; `native_targeted_spell.py`, `emulate_targeted_spell.py`, `prepare_targeted_spell_primary.py`, `build_targeted_spell.py`, `audit_targeted_spell.py`, `finish_phase82.py`. 27 сценариев/81 запуск,21 успешное состояние Shot371,50 диапазонов функций/17 диапазонов данных,246 точечных пересборок Height,83 исходных файла и4EXE сверены. Все исходные инструкции и таблицы методов сохранены;GFX/sound и коэффициенты явно ограничивают опыт. Метаданные включены в существующие67 компонентов;неизвестныеE8/E9 и другие поля не объявлены расшифрованными.')
    s=dict(phase=82,date='2026-10-28',status='Third native CreatureSpell wrapper and target prediction published;substantial format unknowns remain',
      previous_goal_turn_classification='Progress: phase81 complete native EffectElement initialization published and audited. New phase82 adds actual498870 rather than repeating prior two wrappers.',
      schema='targeted-spell-semantics.json',supplement='spell-alternative-semantics.json.phase82_targeted_cast',validation='phase82-validation.json',
      findings=['27 logical cases/81 native replays;21 successful complete371byte Shot states and6 nonallocation states.',
        'Actual native target previous-position/height getters establish low32 XY prediction and half-height aimZ. Creature rawEA4 fullDWORD preserved;native height consumes low16 and signed level coefficient.',
        'Native sprintf executes even for valid primaryShot;formatted has-no-Shot-Type message alone is not failure evidence.',
        '498870 ID30 uses normal trajectory unlike the established4984D0 shortcut;no new ProcessType9/hit/immunity execution.',
        'All371bytes equal for4E8/E9pairs in third wrapper;global meaning unresolved.',
        '50 code ranges/17 data,246 exactHeightpatches,83 source files/4EXEs unchanged;67 package components.'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase82-targeted-spell-emulations.json','phase82-primary-manifest.json','phase82-validation.json','targeted-spell-semantics.json']],
      significant_unknowns_remain=True,goal_achieved=False,
      scope='Prepared definitions/targets,original target vtables and native sprintf/factory/math;externalgfx0/soundfixed,cardinal coefficients. No live game/AI/animation-event/tick/renderer or full raw-loader test.',
      next_action='Do not repeat27cases/81replays or246Heightpatches. All3 direct CreatureSpell wrappers now have native successful cases. Pursue upstream4950C0 animation-event selection or definition-lifetime/raw-path consumers if they can resolve E8/E9;previousF6 update timing is not yet proven by this phase. Alternatively prioritize true remaining unknowns KeeperC8,AP04/08,CreatureJump/Priority,Terrain198/224,Trap204,Object34C,Door236,ElementB1/B2. Treat same-offset hits as hypotheses and avoid rediscovering already-confirmed lightning immunity.')
    (ROOT/'phase82-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase82 published;original full goal not complete;remaining unknowns preserved.')
if __name__=='__main__':main()
