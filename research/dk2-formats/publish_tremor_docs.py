"""Publish bounded mode8/TerrainHP/nativeCMap/NAV results for editor integration."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    note=('Актуальный этап 151: пакет содержит 130 компонентов, прежние 129 схем сохранены побайтно. '
        '`tremor_damage_semantics` выполняет rawShot/Terrain→factory→mode8→cellHP→CMap→NAV, включая полный Shot tick. '
        'Data2=s32>>12 задаёт полуоткрытый заполненный квадрат, X затем Y; SOLID1 без IMPENETRABLE2. '
        'Передаётся cell owner low12. Data1 отрицается вDWORD, HP-применение используетlow16:65536→0,65537→1, проверенное-80 лечит80. '
        'Rock2/HP165 при165 урона становится Dirt3/HP1/nav0; selfDestroyedType2 сохраняетtype2/HP0/nav8. '
        '24 сценария/74HP-запроса/7смен клеток;3сценария целогоtick;6Ghidra/450областей/2093064байта; '
        '177диапазонов/11453PC/5блоков;246 точных пересборок на5551Shot/3306Terrain-записях. '
        'Настоящие4NAVинициализатора устранилиNULLgraph-доступы; принятыеlowreads толькоFS:0. '
        'effective_shot_parameters уточняет толькоmode8, физическийData1/Data2:u32 сохранён. '
        'В исходном корпусеmode8 нет: диагностические копии/ProcessNONE. RuntimecellHP+6 не относится кKLDbyte3. '
        'Реальных ассетов/GPU/Worldscheduler/всехTerrain нет. SHA310 источников/7EXE сохранены. '
        'См. `TREMOR_DAMAGE_RU.md`; общая оценка около75%, существенные неизвестные остаются.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 151:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап151: пакет содержит 130 компонентов. Mode8Tremor доведён через настоящие cellHP/CMap/NAV и полные выбранные Shot tick: '
        'Q12квадрат/XзатемY, low16HP-применение, Rock2→Dirt3/HP1; selfDestroyedType удерживаетHP0/type2. '
        '24сценария/6Ghidra/177диапазонов/11453PC,246 точных пересборок. '
        'Mode8 отсутствует в5551 исходнойShot-записи; новые случаи диагностические. '
        'Прежние129 схем иSHA310 источников/7EXE сохранены. Непрозрачные поля новых имён не получили, '
        'KLDbyte3/другиеTerrain/Worldscheduler открыты; оценка около75%. См. `TREMOR_DAMAGE_RU.md`.\n')
    if not rest.lstrip().startswith('Этап151:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 151\n'+rest,encoding='utf8');print('Published phase151 integration/readiness entries')

if __name__=='__main__':main()
