"""Publish phase158 mode2 rules with diagnostic-source and opaque-field boundaries."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 158: пакет содержит 137 компонентов, прежние 136 схемы сохранены побайтно. '
        '`effect_adjacent_semantics`: полный mode2/4376E0 создаёт в четырёх соседних Q12-позициях, '
        'N партий на позицию, обычный обход всех8 слотов7→0;40 выбираетEffects, иначеElements без требования80. '
        '4000/scale50–255 не масштабируют выбранные ветки. 16native / 112Effect+2Element raw-loads; '
        '72 запросаCBridge /68 фактических Effect-factory calls,123Effects+8Elements зарегистрированы. '
        'Tileflags0 отказывает междуwrapper иfactory; пул1080 даёт3Effects без отката. '
        '7 полных Ghidra-контролей /301 область /1235346 байт;51 диапазон/2807PC/4блока. '
        '328 точных пересборок. В30207 исходных Effect-записях mode2 нет: родитель диагностический. '
        'EE/F0 остаются неизвестными; новый поиск устраняет6Misc+3ложныхcontrol цепочки и восстанавливает4378C2. '
        'Дополнены только effective Effects-аннотации; SHA420источников/7EXE сохранены. '
        'См. `EFFECT_ADJACENT_RU.md`; оценка около75%, цель открыта.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 158:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап158: пакет содержит 137 компонентов. Проверен mode2 Effects:4соседние позиции, '
        'N партий/8слотов/40-target/Next chains,123Effects+8Elements в16сценах;7полныхGhidra. '
        '328точныхпересборок; исходныйкорпусmode2не содержит,сценыдиагностические. '
        'EE/F0 неизвестны; прежние136 схем/420источников/7EXE сохранены. '
        'Оценка около75%, цель открыта. См. `EFFECT_ADJACENT_RU.md`.\n')
    if not rest.lstrip().startswith('Этап158:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 158\n'+rest,encoding='utf8');print('Published phase158 integration/readiness entries')
if __name__=='__main__':main()
