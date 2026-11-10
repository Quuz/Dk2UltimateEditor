from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 95: `POSSESSION_NEIGHBOURS_RU.md` / `possession-neighbour-semantics.json`, компонент `possession_neighbour_semantics`. '
      'Найден настоящий писатель actor3EA/08: owner4BFD00 при вселённом источнике проверяет XY-линию до прочих участников owner-списка; '
      'при успехе ставит флаг, иначе снимает. При подготовленном source без possession очищает флаг у списка. '
      'Raw224/21=200 даёт соседней цели×1,5;P0 даёт×0,5,половина полного множителя. 34 native-композиции: '
      'линии/препятствия/двери/очистка/skip/source/currentraw. Четыре дополняют цепочку настоящими rawDoorinit/converter/loader: '
      'rawDWORD228/40→mid29C→def8B→ray→скорость. Gate1 исключает visuals/audio/checksum. '
      '82Door sources/656records/1312exact bit patches;153whole native spans/9blocks. '
      'Компонент74,прежние73 побайтово сохранены. Полныйtick/Shot26/control/снятие вселения и существенные вопросы15семейств открыты.')
    if 'Актуальный этап 95:' not in text:
        text=text.replace('Актуальный этап 94:','Предыдущий этап 94:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 94','после этапа 95').replace('актуальный опубликованный этап — 94, пакет содержит 73 компонента','актуальный опубликованный этап — 95, пакет содержит 74 компонента')
    note=('**Этап95 раскрыл источник второго бонуса скорости:** во время вселения оригинальная4BFD00 проверяет линию XY до других существ '
      'в списке владельца и ставит actor3EA/08. При raw224/21=200 цель получает×1,5,при препятствии — обычную скорость. '
      'Проверены34native-сценария,включая4полных rawDoor-пути: flag40 по228 меняет прохождение проверки линии и результат скорости. '
      '1312 точечных пересборок/82Door sources. См. `POSSESSION_NEIGHBOURS_RU.md`. Полный цикл вселения и смысл прочих stat-полей не закрыты.\n\n')
    if '**Этап95 раскрыл источник второго бонуса скорости:**' not in text:text=text.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'+note,1)
    p.write_text(text,encoding='utf8')
    p=ROOT/'POSSESSION_SPEED_RU.md';text=p.read_text(encoding='utf8')
    note='Обновление этапа95: писатель вторичного флага actor3EA/08 и полная цепочка проверки линии до другого существа теперь найдены и выполнены; см. `POSSESSION_NEIGHBOURS_RU.md`. Ниже сохранены результаты и границы этапа92.'
    if 'Обновление этапа95:' not in text:
        title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    print('Integration/readiness95 saved;overallabout75%;full15-format goal open')

if __name__=='__main__':main()
