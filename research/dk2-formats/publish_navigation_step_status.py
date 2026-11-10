"""Refresh reader-facing status without altering earlier component schemas."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent

def main():
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8')
    if 'Актуальный этап 98:' not in t:
        t=t.replace('Актуальный этап 97:','Предыдущий этап 97:',1)
        lead=('Актуальный этап 98: `NAVIGATION_STEP_RU.md` / `navigation-step-semantics.json`, компонент `navigation_step_semantics`. '
          '32 native-сценария выполняют целые4D5A40/4D4760 с настоящими map-callback/collision predicate и конечным пустым массивом сил. '
          'Четыре дополнительно применяют координаты через4B35C0/4B5560/4B5020, включая перенос tag между клетками; два вызывают настоящий48EBF0, '
          'связывающий definition686/10 с разрешением nav-category1. Подтверждены ограниченный шаг, stop и X-first скольжение. '
          'Raw CreaturesEFC→mid12EC→def686 подтверждено статически; API меняет только маски10/20, '
          '14936 exact-container пересборок на3734 записях/82 источниках сохраняют остальные биты и байты. '
          '190 полных диапазонов функций/12 блоков PE сверены;0 game code/vtable/pointer patches. '
          'Компонент77, прежние76 схем побайтово сохранены. Полныйtick,непустые силы,A*,построение NAV и существенные неизвестные15семейств открыты.\n\n')
        t=t.replace('# Комплект для разработчика редактора\n\n','# Комплект для разработчика редактора\n\n'+lead,1);p.write_text(t,encoding='utf8')
    p=ROOT/'READINESS_RU.md';t=p.read_text(encoding='utf8').replace('после этапа 97','после этапа 98',1)
    t=t.replace('актуальный опубликованный этап — 97, пакет содержит 76 компонентов','актуальный опубликованный этап — 98, пакет содержит 77 компонентов',1)
    old='Полныйtick/позиционное движение ещё открыты.'
    t=t.replace(old,'На этапе97 полный step/применение координат ещё не исполнялись; новое подтверждение этапа98 описано выше.',1)
    if '**Этап98 довёл' not in t:
        t=t.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'
          '**Этап98 довёл проверку до шага и применения координат:**32 native-сценария выполняют целиком4D5A40→4D4760 с настоящими callbacks и пустым массивом сил; '
          'четыре дополнительно изменяют actorXY/map-списки через оригинальные функции. При базе1000/P200 свободный шаг дал deltaX3000 со вселением, '
          '1000 без него и1500 с соседним флагом; база1366 ограничилась4095. Это ограниченный подготовленный шаг, не полныйtick с реальными силами. '
          'RawEFC-маски10/20 связаны статическим loader-path с runtime686 и проверенными разрешениями nav-категорий. '
          'См. `NAVIGATION_STEP_RU.md`; оценка всей цели остаётся около75%, число native-проверок не превращается в процент полной расшифровки.\n\n',1)
    p.write_text(t,encoding='utf8')
    for n in ['POSSESSION_SPEED_RU.md','POSSESSION_NEIGHBOURS_RU.md','NAVIGATION_SPEED_RU.md']:
        p=ROOT/n;t=p.read_text(encoding='utf8')
        if 'Обновление этапа98:' not in t:
            at=t.index('\n')+1;t=t[:at]+'\nОбновление этапа98: полный шаг4D5A40/4D4760 теперь выполнен в32 сценариях с подготовленной картой и пустым массивом сил; четыре дополнительно применяют координаты к существу. См. `NAVIGATION_STEP_RU.md`. Полныйtick и реальные силы остаются открыты; ограничения ниже относятся к исходному этапу.\n'+t[at:];p.write_text(t,encoding='utf8')
    print('Phase98 reader documentation updated')

if __name__=='__main__':main()
