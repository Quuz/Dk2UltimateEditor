from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 97: `NAVIGATION_SPEED_RU.md` / `navigation-speed-semantics.json`, компонент `navigation_speed_semantics`. '
      'Уточнено ограничение скорости: настоящийCreaturevslot8C/48D010 сохраняет min(unsigned request,4095) вWORD11C иcachedvalue<<4 вDWORD206. '
      'Это controller124+E2,названный вEXE max_speed и используемый4D4760/4D4F60. 27 native-композиций:raw224→actualflags/getter→realvslots→wholevectorlimiter. '
      'Getter4098/4096 становится4095;оси/диагональ/малые/нулевой векторы,границы/percent/overflow проверены. '
      'Полныйtick/step не исполнен:временное применение/восстановление скорости иCrypt108C описаны какstatic. '
      'Множители×3/×1,5 относятсяgetter;не обещать неограниченную фактическую дистанцию и не ограничивать этим runtime-потолком сыройKWD. '
      '168whole native spans/11blocks,0newrawpatches/0gamepatches. Компонент76,прежние75 побайтово сохранены;существенные вопросы15семейств открыты.')
    if 'Актуальный этап 97:' not in text:
        text=text.replace('Актуальный этап 96:','Предыдущий этап 96:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 96','после этапа 97').replace('актуальный опубликованный этап — 96, пакет содержит 75 компонентов','актуальный опубликованный этап — 97, пакет содержит 76 компонентов')
    note=('**Этап97 уточнил предел скорости:** после getter существует отдельный настоящий setter экземпляра с unsigned-потолком4095. '
      'CachedDWORD206 используется навигацией какmax_speed;27native-связок проверяют setter иполныйлимитер вектора. '
      '×3/×1,5 — результатыgetter,а не доказанное неограниченное увеличение пройденной дистанции. '
      'Не переносить runtime-потолок4095 на сырое поле KWD. См. `NAVIGATION_SPEED_RU.md`. Полныйtick/позиционное движение ещё открыты.\n\n')
    if '**Этап97 уточнил предел скорости:**' not in text:text=text.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'+note,1)
    p.write_text(text,encoding='utf8')
    for name in ['POSSESSION_SPEED_RU.md','POSSESSION_NEIGHBOURS_RU.md']:
        p=ROOT/name;text=p.read_text(encoding='utf8')
        note='Обновление этапа97: множители getter ниже не учитывают отдельный setter экземпляра с потолком4095 и навигационный max_speed. См. `NAVIGATION_SPEED_RU.md`. Полная пройденная дистанция в живой игре этим getter не доказана; сырое поле KWD нельзя ограничивать числом4095.'
        if 'Обновление этапа97:' not in text:
            title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    print('Integration/readiness97 saved; getter/movement scope clarified;fullgoalopen')

if __name__=='__main__':main()
