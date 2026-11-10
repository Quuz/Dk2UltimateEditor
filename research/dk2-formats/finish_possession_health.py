from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 93: `POSSESSION_HEALTH_RU.md` / `possession-health-semantics.json`, компонент `possession_health_semantics`. '
      'Оригинальная4B2CE0 подтверждает hardcoded вклад вселения−64/128 в множитель урона:−100→−50,−101→−50,−1→0. '
      '31 native-сценарий:raw224/65→setter→whole HP,два raw239,OR-незадвоение WORD443,WORD447/owner/Pit-factor, '
      'лечебный потолок отbaseHP/raw65-HealthLevel иусловная over-cap арифметика. 15 событий прошли настоящие World/Session wrappers '
      'ификсировались внешним bridge. Type224/Health иscalar98/113/114 не меняют результаты проверенных HP-путей; '
      'это не глобальная неиспользуемость. 132whole native spans/6PE blocks,0game patches;557Variables/9healthlevel/36exact API patches. '
      'Компонент72,прежние71 побайтово сохранены;lethal/death,полный possession cycle ипрочие существенные вопросы открыты.')
    if 'Актуальный этап 93:' not in text:
        text=text.replace('Актуальный этап 92:','Предыдущий этап 92:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 92','после этапа 93').replace('актуальный опубликованный этап — 92, пакет содержит 71 компонент','актуальный опубликованный этап — 93, пакет содержит 72 компонента')
    note=('**Этап93 уточнил выживаемость:** в обычной ветке HP флаг вселения уменьшает множитель урона128→64; '
      'это уменьшение урона вдвое, а не увеличение actorHP в setter. Предел лечения зависит отbaseHP иtype65/HealthLevel; '
      'type224/Health иscalar98/113/114 не изменили проверенные damage/heal-cap исходы. '
      'См. `POSSESSION_HEALTH_RU.md`:31 native-сценарий,условия сочетаниямодификаторов иover-cap пример. '
      'Полный процесс вселения и прочие возможные HP/stat потребители ещё не закрыты.\n\n')
    if '**Этап93 уточнил выживаемость:**' not in text:text=text.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'+note,1)
    p.write_text(text,encoding='utf8')
    p=ROOT/'POSSESSION_SPEED_RU.md';text=p.read_text(encoding='utf8')
    note='Обновление этапа93: общий HP-handler с флагом вселения и healing ceiling теперь проверен; см. `POSSESSION_HEALTH_RU.md`. Ниже сохранены границы этапа92. Полный possession cycle и глобальная семантика113/114 по-прежнему не закрыты.'
    if 'Обновление этапа93:' not in text:
        title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    print('Current integration/readiness93 saved; full15-format goal remains open')
if __name__=='__main__':main()
