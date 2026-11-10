"""Prepend phase165 status without rewriting older research history."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent

def main():
    s=json.loads((ROOT/'terrain-text-stat-semantics.json').read_text(encoding='utf-8'))
    assert s['counts']['Ghidra_compared_bytes']==1468523
    note=('Этап165: оригинальный Pro импортирует числовой TERRAIN Stat Data19 в double модели320h и сохраняет DWORD224h. '
          '3.5→3800h→3.5; -1→FFFFF000h→1048575 после фактического повторного чтения. '
          '13native/5полныхGhidra,306областей/1468523байта включая низкуюстраницу;246точныхпересборок82Terrain-файлов. '
          '94дополнительныхдиапазона(1толькостатический)/34унаследованных/4552PC/3блока. '
          'Назначение224h в игре неизвестно; внешняя грамматика/GUI не исполнены. '
          'Прежние143компонента/codec сохранены;420текущихисточников/7EXE проверены. '
          'Четыре изменения карт относительно этапа164 записаны явно, файлы не откатывались и не менялись. '
          'См. `TERRAIN_TEXT_STAT_RU.md`; общая экспертная оценка около75%, цель15семейств активна.')
    for name,prefix in [('INTEGRATION_RU.md','Актуальный этап 165: пакет содержит 144 компонента. '),
                        ('READINESS_RU.md','# Оценка готовности на 2 ноября 2026 — после этапа 165\n\nпакет содержит 144 компонента. ')]:
        path=ROOT/name;text=path.read_text(encoding='utf-8')
        if text.startswith(prefix):continue
        assert text.startswith('Актуальный этап 164:') if name=='INTEGRATION_RU.md' else text.startswith('# Оценка готовности на 1 ноября 2026 — после этапа 164')
        path.write_text(prefix+note+'\n\n'+text,encoding='utf-8')
    path=ROOT/'SOURCES.md';text=path.read_text(encoding='utf-8')
    addition='Дополнение2026-11-02,этап165: оригинальныеPro42ED90/49F5E0/4250D0/4A3D30/4A7690,94дополнительных/34унаследованныхдиапазона и3таблицы сверены сPE/Ghidra.13native и5полныхGhidra подтверждаютTerrainStat19→raw224; новоеигровоеназначение неустановлено. Схема `terrain-text-stat-semantics.json`, описание `TERRAIN_TEXT_STAT_RU.md`, снимок текущих420источников/7EXE и4историческихрасхождений карт — `phase165-source-baseline.json`; старыеSHA сохранены.\n'
    if addition not in text:path.write_text(text+'\n'+addition,encoding='utf-8')
    print('Published phase165 integration/readiness/source notes.')

if __name__=='__main__':main()
