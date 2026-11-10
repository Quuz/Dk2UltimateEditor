"""Publish current integration/readiness notes for bounded Shot lifecycle evidence."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    note=('Актуальный этап 148: пакет содержит 127 компонентов, прежние 126 схем сохранены побайтно. '
        '`shot_lifecycle_semantics` выполняет оригинальные raw-конвертеры/загрузчики, фабрику и целые Shot tick/terminal: '
        'AT_START (5) наносит урон один раз в первом tick по guard149; AT_END (6) выполняется в 4AAF70 только при WorldDWORD723B == 0. '
        'Обнуление HP в tick запрашивает удаление отдельно от terminal callback. Data1 = -80 передаёт +80HP и в выбранной сцене даёт 1000→1080; '
        'физический Data1:u32 сохранён, трактовка зависит от обоих диспетчеров. '
        '22 сценария, 6 Ghidra-контролей: 354 области / 812784 байта; 120 диапазонов / 5020 оригинальных PC / 4 блока. '
        '164 точные пересборки и decode→rebuild на 82 контейнерах / 5551 Shot-записи сохраняют прочие байты. '
        'Режимов 5/6 в этом исходном корпусе нет: выполнены диагностические копии. '
        'Актуальный effective_shot_parameters уточнён, исторические схемы не изменены. '
        'Подготовленные World/map/targets и явные resource/audio IO; обычный World scheduler, pool free и смысл World723B остаются открыты. '
        'SHA310 источников / 7 EXE сохранены. См. `SHOT_LIFECYCLE_RU.md`. '
        'Новых названий непрозрачных полей нет; общая оценка около75%.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 148:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап148: пакет содержит 127 компонентов. Полные выбранные Shot tick/terminal подтвердили первый tick для AT_START, '
        'отдельный завершающий callback и exact-zero World723B для AT_END, а также прибавку80HP при Data1=-80. '
        '22 сценария / 6 Ghidra / 120 диапазонов / 5020PC; 164 точные пересборки в82 контейнерах, '
        'прежние126 схем и SHA310 источников / 7 EXE сохранены. Режимы5/6 отсутствуют в5551 исходной Shot-записи: '
        'это проверенные диагностические режимы. NormalWorldscheduler/poolfree и общий смысл World723B открыты; '
        'новых имён opaque-полей нет, CreatureFlags800/1000 остаются неизвестными. '
        'Общая экспертная оценка около75% сохраняется. См. `SHOT_LIFECYCLE_RU.md`.\n')
    if not rest.lstrip().startswith('Этап148:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 148\n'+rest,encoding='utf8')
    print('Published phase148 integration/readiness entries')

if __name__=='__main__':main()
