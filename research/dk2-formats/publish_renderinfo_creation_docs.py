"""Publish phase155 native creator results without overstating Ghidra coverage."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 155: пакет содержит 134 компонента, прежние133 схемы сохранены побайтно. '
        '`renderinfo_creation_semantics`: настоящий59CCF0→allocator57CEB0/57CD30→580480 после rawCreature/Object-загрузок; '
        '10сценариев/20созданных примитивов, масштабы/знаковые смещения/alpha/token/передачаRIAC→primitive8C. '
        'DWORD5C/70 сохраняют прежние байты до отдельнойrenderer-staging; неудачный немедленныйpick исключён как неполная сцена, не ошибка игры/редактора. '
        'Полная Ghidra-проверка НЕ прошла из-заFCOS41C61D;4успешных независимых контроля покрывают только allocator/координаты (264области/356404байта). '
        '79диапазонов/4234PC/4блока; оригинальные инструкции не заменялись. '
        'RenderInfo подготовлен, Actor/AI→RenderInfo/полныйrenderer ещё не выполнены; Priority/Jump остаются неизвестными, RIAC не переименован в приоритет. '
        'Новых storage/API-изменений нет; SHA310источников/7EXE сохранены. См. `RENDERINFO_CREATION_RU.md`; оценка около75%, цель открыта.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 155:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап155: пакет содержит 134 компонента. Нативное создание20dynamicпримитивов через настоящий allocator/конструктор: '
        'масштаб, координатные поправки, alpha/token/RIAC. Ghidraполнаяцепочка ограниченаFCOS (0успешныхполныхконтролей); '
        '4контроля отдельныхhelpers прошли. 79диапазонов/4234PC. Renderer-staging отдельно; первичныйfailedpick — неполная сцена. '
        'Priority/Jump остаются неизвестными, прежние133схемы/SHA310источников/7EXE сохранены. '
        'Оценка около75%, цель открыта. См. `RENDERINFO_CREATION_RU.md`.\n')
    if not rest.lstrip().startswith('Этап155:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 155\n'+rest,encoding='utf8');print('Published phase155 integration/readiness entries')
if __name__=='__main__':main()
