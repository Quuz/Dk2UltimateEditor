"""Publish phase153 bounded selection evidence and preserve prior documentation."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 153: пакет содержит 132 компонента, прежние131 схема сохранены побайтно. '
        '`pickup_selection_semantics`: настоящие rawCreature/Object-загрузчики и полный44DEC0 запасной поиск под рукой. '
        'В режиме0 первое подходящее существо выигрывает, объекты выбираются по расстоянию при отсутствии существа; режим1 ищет ближайший объект. '
        'Строгие Q12-пороги800/400, приближённая метрика, порядокY→X→список подтверждены по реальным чтениям. '
        'Gold1/Key17/Heart13 подтверждают bit100/ID13-условие объектов. '
        '34сценария/68Creature+34Object-загрузки/8Ghidra/273области/653000байт;69диапазонов/3615PC/1блок;328точных пересборок. '
        'PriorityCreatures6EB/Object112 иCreatureJump75C здесь не читаются; основной графический572820 ещё не исполнен, глобальное неиспользование не доказано. '
        'Новый узкийpatch_priority поддерживает5449/5537Creature и894Object; старый общийscalarAPI5537 всё ещё отвергает, документация различает174/154контроля. '
        'Физические/effective представления сохранены; SHA310источников/7EXE сохранены. '
        'См. `PICKUP_SELECTION_RU.md`; общая оценка около75%, существенные неизвестные остаются.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 153:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап153: пакет содержит 132 компонента. Запасной поиск44DEC0 после настоящих rawCreature/Object-загрузок: '
        'первое подходящее существо, ближайший объект, точные ограничения/пороги/Y→X→список. '
        '34сценария/8Ghidra/69диапазонов/3615PC,328точных пересборок; основной графический выбор не исполнен. '
        'Priority/Jump остаются неизвестными вне этого пути; прежние131схема/SHA310источников/7EXE сохранены. '
        'Оценка около75%; цель открыта. См. `PICKUP_SELECTION_RU.md`.\n')
    if not rest.lstrip().startswith('Этап153:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 153\n'+rest,encoding='utf8');print('Published phase153 integration/readiness entries')
if __name__=='__main__':main()
