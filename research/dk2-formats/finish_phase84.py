"""Publish confirmed runtime UI semantics without closing remaining unknowns."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(n,text):
    p=ROOT/n;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')
def main():
    v=load('phase84-validation.json');assert v['native_emulation_invocations']==28 and v['raw_init_convert_insert_chains']==329
    insert('INTEGRATION_RU.md','Актуальный этап 84: `KEEPER_UI_ORDER_RU.md` / `keeper-pipeline-semantics.json.phase84_ui_order`. У rawDB установлен игровой UI-потребитель: перебор порядков1..count,фильтрация состояния игрока!=1 и исключение ID5/14. 28 native-композиций включают329raw-цепочек,420селекторов,полные обработчики вкладки/выбора и настоящую Session-очередь18-байтных действий. В Qz/Qz2 совпадение DB9 скрывает CreateGold из обычного списка;позиция9 выбирает RaiseDead. При единственном state0 уCreateGold вкладка отключается. Два варианта правки только в памяти восстанавливают выбор;карты не сохранены. 33PE/5data,82источника/4EXE сверены,4новые точечные пересборки;67компонентов. C8 и прочие существенные неизвестные открыты;реальная мышь/framebuffer/исполнениеqueuedaction не проверялись.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап 83:','Предыдущий этап 83:',1),encoding='utf8')
    insert('KEEPER_ARRAY_RU.md','Продолжение этапа 84 — `KEEPER_UI_ORDER_RU.md`: граница предыдущего этапа о последствиях DB в интерфейсе расширена. Полная raw→array→UI→Session композиция подтвердила пропуск CreateGold при совпадении DB9 в двух Qz-картах,поведение вкладки и выбранный ID в очереди. 28 новых сценариев учитываются отдельно от41 этапа83. Реальное окно/мышь и исполнение действия миром остаются вне проверки;C8 не получило назначения.')
    insert('KEEPER_PIPELINE_RU.md','Продолжение этапа 84 — `KEEPER_UI_ORDER_RU.md`: BYTE rawDB→mid153→def6C управляет порядком перебора обычной игровой панели. Подтверждены native UI-селектор,вкладка,обработчик выбора и локальная Session-очередь. Условие state!=1 и исключениеID5/14 проверены на оригинальном коде. C8 варьировалось в шести полных композициях без изменения UI/Session-результата;его глобальный игровой смысл остаётся неизвестным.')
    insert('SOURCES.md','## Этап 84: KeeperSpells DB в игровом UI\n\n`KEEPER_UI_ORDER_RU.md`,`keeper-ui-semantics.json`,`phase84-validation.json`,`phase84-primary-manifest.json`;`scan_keeper_order_calls.py`,`native_keeper_ui.py`,`emulate_keeper_ui.py`,`prepare_keeper_ui_primary.py`,`build_keeper_ui.py`,`audit_keeper_ui.py`,`finish_phase84.py`.28 native-сценариев,329raw-init/converter/insert,420селекторов,28tab/28click callbacks,20локальныхдействий;33диапазонафункций/5данных,82файла/4EXE сверены,4точечныхin-memory DB-пересборки реальных Qz-контейнеров. Код/World/UIvtable сохранены;originalSessionqueue выполняется. Звук задаётся внешним provider,реальныхGUI/network/framebuffer-вызовов нет. Исходные карты не изменены;C8 остаётся неизвестным.')
    s=dict(phase=84,date='2026-10-28',status='Keeper rawDB native game-UI ordering/selection and Session queue published;remaining substantial unknowns open',
      previous_goal_turn_classification='Progress:phase83 realarray insertion/overlay/accessors and exactsource duplicateOrder evidence published. Phase84 now resolves actual UI consumer rather than repeating local lookup.',
      schema='keeper-ui-semantics.json',supplement='keeper-pipeline-semantics.json.phase84_ui_order',validation='phase84-validation.json',
      findings=['28nativecompositions/329rawchains/420selectorcalls;actualtab/click/Sessionqueue,20actions.',
        'DB controlsordinarygameUIscan1..count;state1/missing excluded;IDs5/14excluded numerically;DB0/out-of-scan omitted.',
        'Qz/Qz2 Gold13 hidden byDB9collisionwithRaiseDead9;ordinal9queuesID9. OnlyGoldstate0 leavesemptytab;in-memoryDBchangesrecoverGold.',
        'Nativequeue32×18:fullcount32 dropsactionwhileUIchanges;validwrite31wraps0. Physicalmouse/framebuffer/worldactionexecution not claimed.',
        'SixGoldC8boundariesleaveverifiedUI/Sessionidentical;C8 globalmeaningunresolved.',
        '33PE/5data,82inputs/4EXEs unchanged,4newexactcontainerpatches,67components.'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase84-order-call-candidates.json','phase84-keeper-ui-emulations.json','phase84-primary-manifest.json','phase84-validation.json','keeper-ui-semantics.json']],
      significant_unknowns_remain=True,goal_achieved=False,
      scope='ExactDX raw/nativeUI/localqueue withpreparedvalidplayer,handmode0,cursorC,emptyobjectlists,World7291FF,nullGFX/fixedsound. No source edits,actualGUI/mouse/network/renderer or worldapplicationof action.',
      next_action='Do not repeat28nativecases/329rawchains/4containerpatches orprior83cases. DB ordinaryUI consumer established. Prioritize remaining true unknown fields Terrain198/224,Traps204,Objects34C,Doors236,CreatureAbilityJump/Priority,AP04/08,ElementB1/B2 orCreatureSpellE8/E9 upstream. C8scopecompletearray/cast/UI negative evidence stilldoesnotproveglobalnonuse. Avoid rediscovering knownlightningimmunity and do not silently changeQzmaporders. Full15familygoalremainsincomplete.')
    (ROOT/'phase84-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Phase84published;originalfullgoalnotcomplete;remainingunknownspreserved.')
if __name__=='__main__':main()
