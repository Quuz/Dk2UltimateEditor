"""Publish phase146 integration/readiness entries without rewriting prior evidence."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    prefix='Актуальный этап 146: пакет содержит 125 компонентов, прежние 124 схемы сохранены побайтно. `trap_model_word_semantics` исполняет полный оригинальный Pro-путь Traps raw431530→выделенная модель4A1770/425C20→регистрация→writer4A8F40. Все579байт16исходных записей и5граничных копий сохранены;6разных сценариев/21raw-modelцепочка/6сохранений. 2Ghidra-контроля сравнивают142области/600370байт;47дополнительных и26унаследованных диапазонов покрывают2336оригинальныхPC. Новый publiccodec и effectiveTrapslayout объединяют два прежних байта204/205 в Unknown204:u16; layouts.json и dk2_formats_phase145.py сохраняют старое представление. Это физический WORD, не доказательство знака/ManaGen или порчи файлов старым видом. 82точечныепересборки/publicdecode→rebuild на1293записях сохраняют остальныебайты; DoorWORD-regression пройдена. SHA310источников/7EXE сохранены: добавлен Traps.kwd по прежнему историческомухешу. Подготовленные CRT/CString/IAT/CFile и нулевой MID не заменяют полныйGUI/внешнийпарсер/инициализатор/commit/bootstrap. См. `TRAP_MODEL_WORD_RU.md`; неизвестное назначение204 и общая готовность около75% остаются.\n\n'
    if not s.startswith('Актуальный этап 146:'):p.write_text(prefix+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');first,rest=s.split('\n',1)
    prefix='\nЭтап146: пакет содержит 125 компонентов. Полный оригинальный Traps raw→allocatedmodel→writer сохраняет16исходных записей и5граничных WORD204копий;6сценариев/21цепочка. 2независимых Ghidra-контроля,47дополнительных/26унаследованных диапазонов и2336PC;82точныепересборки на1293исходныхзаписях. CurrentAPI/effectiveTraps объединяют204/205 в Unknown204:u16; прежняяразметка/кодек заморожены, игровоеназначение/знак неизвестны. Старый байтовыйвид сампосебе не доказывает порчу. SHA310источников/7EXE ипрежние124схемы сохранены. ПолногоGUI/bootstrap нет. Новыхигровыхимёнunknownполей0; оценка около75% сохраняется. См. `TRAP_MODEL_WORD_RU.md`.\n'
    if not rest.lstrip().startswith('Этап146:'):rest=prefix+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 146\n'+rest,encoding='utf8')
    print('Published phase146 entries')
if __name__=='__main__':main()
