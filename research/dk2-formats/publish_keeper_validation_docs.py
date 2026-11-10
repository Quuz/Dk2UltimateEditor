"""Add phase152 integration/readiness entries without changing historical evidence."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 152: пакет содержит 131 компонент, прежние130 схем сохранены побайтно. '
        '`keeper_validation_semantics` выполняет настоящие rawKeeper→definition→инициализацию правил→полные проверки координат/существ. '
        'OWN_LAND принимает также союзную землю; OWN_CREATURES в обычной ветке сравнивает владельцев. '
        'Бит4 аргумента вызова инвертирует TargetRule1/2, бит2 добавляет условие cell2000 для CastRule2/3/4; это не доказанные rawKeeperFlagsDC. '
        'Для Possession проверены запреты IS_HORNY, Actor flags8008, WORD3CB/44B и8 состояний. '
        '82сценария/10Ghidra/260областей/599450байт;32диапазона/1407PC/2блока;246точных пересборок82контейнеров/1066записей. '
        'Исходный Heal Secret1 подтверждает редкий CastRule2. C8 не читается в выбранных82ветках и остаётся неизвестным. '
        'Специальные комнаты/переносчики/нормальныйWorldscheduler не покрыты; физические/effective представления сохранены. '
        'SHA310источников/7EXE сохранены. См. `KEEPER_VALIDATION_RU.md`; общая оценка около75%.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 152:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап152: пакет содержит 131 компонент. Проверены настоящие загрузчики/инициализаторы и полные Keeper-проверки координат/существ: '
        'своя/союзная земля, инверсия TargetRule1/2 аргументом4, конкретные ограничения Possession, мана/перезарядка. '
        '82сценария/10Ghidra/32диапазона/1407PC,246точных пересборок. C8 остаётся неизвестным; новые непрозрачные поля не названы. '
        'Прежние130схем иSHA310источников/7EXE сохранены. Оценка около75%; общая цель открыта. См. `KEEPER_VALIDATION_RU.md`.\n')
    if not rest.lstrip().startswith('Этап152:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 152\n'+rest,encoding='utf8');print('Published phase152 documentation entries')
if __name__=='__main__':main()
