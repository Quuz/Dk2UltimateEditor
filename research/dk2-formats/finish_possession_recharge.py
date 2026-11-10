from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 96: `POSSESSION_RECHARGE_RU.md` / `possession-recharge-semantics.json`, компонент `possession_recharge_semantics`. '
      'Подтверждены raw224/Stat31 и scalar210 для длительности CreatureSpell: базовое rawEA→mid13A→def2D, затем настоящий4F7AB0. '
      'При B20/P25/C100 normal20,possessed25,secondary12;scalar210 low8,отдельный чистый процент,только possessed. '
      '38 native-сценариев включают реальные readiness48C800/progress48C8B0,Worldlookup и5full rawCreatureSpell paths. '
      'Строгая граница готовности:наdeadline progress100,ready0;наследующемтике ready1. '
      'Арифметика3AF/3B3 влияет наready,нонеprogress;имена полей и полный боевой цикл не назначены. '
      '557Variables/475scalar210/3325точечных пересборок;82CreatureSpells/3535records/14140rawEAпересборок. '
      '162whole native spans/9blocks,0game patches. Компонент75,прежние74 побайтово сохранены;полныйShot26/control и существенные вопросы15семейств открыты.')
    if 'Актуальный этап 96:' not in text:
        text=text.replace('Актуальный этап 95:','Предыдущий этап 95:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 95','после этапа 96').replace('актуальный опубликованный этап — 95, пакет содержит 74 компонента','актуальный опубликованный этап — 96, пакет содержит 75 компонентов')
    note=('**Этап96 раскрыл перезарядку способностей:** raw224/Stat31 и scalar210 реально участвуют в4F7AB0. '
      'При базовом интервале20 и текущих25/100 получилось25 со вселением,12 с соседним бонусом и20 без обоих флагов. '
      'Scalar210 использует low8 значения поDWORD+4;100 означает чистый множитель1,50 —0,5. '
      '38native-сценариев,5full rawCreatureSpell paths,готовность иprogress проверены отдельно. '
      'См. `POSSESSION_RECHARGE_RU.md`. Это расчёт интервала,неполная частота всех атак в живом бою.\n\n')
    if '**Этап96 раскрыл перезарядку способностей:**' not in text:text=text.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'+note,1)
    p.write_text(text,encoding='utf8')
    print('Integration/readiness96 saved;overallabout75%;full15-formatgoalopen')

if __name__=='__main__':main()
