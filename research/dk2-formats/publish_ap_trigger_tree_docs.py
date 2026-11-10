"""Publish phase156 without promoting scoped AP results to whole-game claims."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    note=('Актуальный этап 156: пакет содержит 135 компонентов, прежние 134 схемы сохранены побайтно. '
        '`ap_trigger_tree_semantics`: 33 native сценария / 99 полных AP tick / 33 bind / 93 условия / 57 действий; '
        '12 независимых полных Ghidra-контролей, 98 областей / 358952 байта. '
        'Подтверждены signed16-переходы, возврат первого узла при отсутствующем ID, отключение действия только при унаследованном RepeatTimes=1; '
        'декремент 508640 в обычной AP-рекурсии не достигается, RepeatTimes=2 дал 3 срабатывания. '
        'Три сцены с неизменными payload 3DLevelTriggers подтвердили signed32 LEVEL_TIME. '
        'AP04/08 в выбранных путях не читаются, общее назначение остаётся неизвестным. '
        '12 диапазонов / 367 PC / 3 блока; 852 точные пересборки, 2 пустые Triggers тождественно. '
        'Подготовлены runtime World/AP/массивы; raw parser/loader/полный scheduler не исполнены. '
        'Исторические/effective/API схемы сохранены; SHA420 источников / 7 EXE сохранены. '
        'См. `AP_TRIGGER_TREE_RU.md`; оценка около75%, цель открыта.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 156:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап156: пакет содержит 135 компонентов. Полный AP-обход: 99 native tick / 12 Ghidra-контролей; '
        'ссылки, fallback первого узла и ограниченное правило повторов подтверждены. '
        '852 точные пересборки; 12 диапазонов / 367 PC, сохранены 420 источников / 7 EXE. '
        'AP04/08 остаются неизвестными; остальные существенные вопросы 15 семейств открыты. '
        'Оценка около75%. См. `AP_TRIGGER_TREE_RU.md`.\n')
    if not rest.lstrip().startswith('Этап156:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 156\n'+rest,encoding='utf8');print('Published phase156 integration/readiness entries')
if __name__=='__main__':main()
