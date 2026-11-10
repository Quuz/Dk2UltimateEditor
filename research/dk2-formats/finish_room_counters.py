from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 94: `CRYPT_BONUSES_RU.md` / `room-species-bonus-semantics.json`, компонент `room_species_bonus_semantics`. '
      'Найден источник ownerBYTE1095: регистрация Monk19 в4D8E60 меняет счётчик владельца комнаты; Crypt подтверждён roomID26, '
      'двумя статическими entry-path и строкой Crypt.cpp в EXE. Полная новая композиция room setter→native HP: '
      '35 сценариев,18 групп счётчиков; обе switch-таблицы45 индексов/41 mappedID сверены с PE. '
      'Monk0→1 даёт HP907 без вселения/957 с ним при1000 и запросе−100; удаление возвращает950 со вселением; '
      'другой владелец эффекта не получает. Карта и вклад8/128 зашиты в EXE, нового редактируемого KWD-процента не найдено. '
      '143whole native spans/9PE blocks;0game patches/0new raw patches. Компонент73,прежние72 побайтово сохранены. '
      'Полная игровая Crypt-сцена,непустая cleanup-ветка,смысл прочих17 счётчиков и существенные вопросы15семейств открыты.')
    if 'Актуальный этап 94:' not in text:
        text=text.replace('Актуальный этап 93:','Предыдущий этап 93:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 93','после этапа 94').replace('актуальный опубликованный этап — 93, пакет содержит 72 компонента','актуальный опубликованный этап — 94, пакет содержит 73 компонента')
    note=('Этап94 установил источник дополнительного снижения урона ownerBYTE1095: регистрация Monk19 в комнате Crypt (ID26 в текущем файле; '
      'строка Crypt.cpp и два статических entry-path). 35 новых native-композиций подтвердили изменение счётчика владельца комнаты, '
      'вклад8/128,снятие эффекта и взаимодействие со вселением. Это отдельный механизм комнаты; нового редактируемого процента в KWD не найдено. '
      'См. `CRYPT_BONUSES_RU.md`. Полная автоматическая Crypt-сцена и остальные17 групп эффектов остаются открыты.\n\n')
    if 'Этап94 установил источник' not in text:text=text.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'+note,1)
    p.write_text(text,encoding='utf8')
    p=ROOT/'POSSESSION_HEALTH_RU.md';text=p.read_text(encoding='utf8')
    note='Обновление этапа94: источник ownerBYTE1095 найден в регистрации Monk19 для Crypt и проверен вместе с полным HP-handler; см. `CRYPT_BONUSES_RU.md`. Ниже сохранены результаты и границы этапа93.'
    if 'Обновление этапа94:' not in text:
        title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    print('Integration/readiness94 saved; overall estimate remains about75%;full15-format goal open')

if __name__=='__main__':main()
