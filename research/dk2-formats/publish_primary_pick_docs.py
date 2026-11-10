"""Publish phase154 primary model query evidence with explicit producer boundary."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 154: пакет содержит 133 компонента, прежние132 схемы сохранены побайтно. '
        '`primary_pick_semantics`: полный572820→588A60→оригинальные dynamic582D30/animated585AD0, реальная распаковка/матрицы/кэш. '
        'Сначала точные треугольники, при полном отсутствии попаданий — центры моделей; выбирается меньшая средняя глубина вершин, равенство сохраняет ранний кандидат. '
        '27сценариев/60callbacks/54Creature+27Object-загрузок/8Ghidra/457областей/702282байта;76диапазонов/4433PC/3блока. '
        'Геометрия/камера/примитивы подготовлены, реальныеKMF/GPU/Actor→primitive-производитель ещё не исполнены. '
        'Priority/Jump загружены, но не читаются в этих27ветках; глобальные назначения остаются неизвестными. '
        'Новых физ/API-правок нет,328точных serializer-проверок этапа153 наследуются без повторного зачёта. '
        'SHA310источников/7EXE сохранены. См. `PRIMARY_PICK_RU.md`; оценка около75%, цель открыта.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 154:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап154: пакет содержит 133 компонента. Основной графический выбор572820 доведён до настоящих dynamic/animatedcallbacks: '
        'точные треугольники→центры, средняя глубина/строгие равенства/экранный реестр. '
        '27сценариев/60callbacks/8Ghidra/76диапазонов/4433PC. Подготовленная геометрия, не реальныеKMF/GPU/Actor→primitive-производитель. '
        'Priority/Jump остаются неизвестными; прежние132схемы/SHA310источников/7EXE сохранены. '
        'Новых сериализаторных изменений нет; оценка около75%, общая цель открыта. См. `PRIMARY_PICK_RU.md`.\n')
    if not rest.lstrip().startswith('Этап154:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 154\n'+rest,encoding='utf8');print('Published phase154 integration/readiness entries')
if __name__=='__main__':main()
