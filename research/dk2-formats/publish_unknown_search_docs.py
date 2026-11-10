"""Publish a bounded negative-search review without inventing new field meanings."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    note=('Актуальный этап 149: пакет содержит 128 компонентов, прежние 127 схем сохранены побайтно. '
        '`unknown_search_review` фиксирует новые поиски Creature GameExtraFlags2 и десяти оставшихся Crypt-групп. '
        'Шесть найденных чтений дополнительных флагов проверяют маски1/4/8; назначение2 не подтверждено. '
        'Бит2 установлен лишь в3 из3734 исходных Creature-записей, каждый раз уID28; это корреляция, не смысл флага. '
        'Для Crypt отвергнуты12 ложных цепочек кowner108B: оригинальная4C7360 читает лишьE97/EB0/EC9, '
        'а анализатор указателей не учитывал границу цикла3. 8 новых исполнений / 4 Ghidra-контроля регистров; '
        '7 диапазонов функций, из них6 статических, и12 исполненныхPC сверены. '
        'Одно ограничениеcallee для extraFlags и шесть дляowner-поиска сохраняют запрет на вывод о глобальной неиспользуемости. '
        'Новых значений opaque-полей, правок формата и изменений effective/API/enum нет. '
        'SHA310 источников / 7 EXE сохранены. См. `UNKNOWN_SEARCH_REVIEW_RU.md`; '
        'Crypt остаётся8 раскрытых / 10 нераскрытых групп, общая оценка около75%.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 149:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап149: пакет содержит 128 компонентов. Новые bounded-поиски не раскрыли Creature GameExtraFlags2 '
        'или девятую Crypt-группу. Исключены12 ложных цепочек кowner108B из-за реальной границы цикла3; '
        '8 исполнений / 4 Ghidra-контроля регистров / 7 диапазонов / 12PC. '
        'В корпусе3734 Creature-записей бит2 встречается3 раза уID28; его смысл этим не установлен. '
        'Прежние127 схем, effective/API и SHA310 источников / 7 EXE сохранены. '
        'Новых расшифрованных opaque-полей нет; Crypt остаётся8/18, общая оценка около75%. '
        'См. `UNKNOWN_SEARCH_REVIEW_RU.md`.\n')
    if not rest.lstrip().startswith('Этап149:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 149\n'+rest,encoding='utf8');print('Published phase149 integration/readiness entries')

if __name__=='__main__':main()
