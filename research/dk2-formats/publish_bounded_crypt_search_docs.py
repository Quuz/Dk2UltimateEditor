"""Publish scoped loop-aware Crypt evidence without increasing semantic readiness."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 157: пакет содержит 136 компонентов, прежние 135 схем сохранены побайтно. '
        '`bounded_crypt_search_semantics`: числовые границы CMP/DEC отделены от смещений указателей. '
        '3171 начальная подсказка / 195046 состояний / 690 контекстов; 43 пути и49 контекстов ограничены. '
        '12 ложных цепочек 4C7368→Crypt108B автоматически исключены, остальные33 сохранены, новых потребителей0. '
        'Новый полный native4DC7E0 читает46 записей B57+18i, последний E81; 11 сценариев / 5 полных Ghidra-контролей '
        '(15 областей / 22130 байт), 3 диапазона / 11 исполненных PC. '
        'До Crypt108B цикл не доходит; игровое назначение самой46-записной таблицы не назначено. '
        'Crypt остаётся8 раскрытых / 10 неизвестных групп; новых raw-полей/правок0. '
        'Codec/effective/enum и SHA420 источников / 7 EXE сохранены. См. `BOUNDED_CRYPT_SEARCH_RU.md`; '
        'оценка около75%, цель открыта.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 157:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап157: пакет содержит 136 компонентов. Улучшен поиск Crypt с границами циклов; '
        '12 ложных цепочек исключены,33 сохранены, новых бонусов0. '
        '11 native / 5 полных Ghidra-контролей ограниченного46-записного массива. '
        'Crypt8/18, существенные вопросы 15 семейств открыты. Прежние135 схем, '
        '420 источников / 7 EXE сохранены. Оценка около75%. См. `BOUNDED_CRYPT_SEARCH_RU.md`.\n')
    if not rest.lstrip().startswith('Этап157:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 157\n'+rest,encoding='utf8');print('Published phase157 integration/readiness entries')
if __name__=='__main__':main()
