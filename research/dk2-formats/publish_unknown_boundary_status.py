from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8')
    if 'Актуальный этап 100:' not in t:
        t=t.replace('Актуальный этап 99:','Предыдущий этап 99:',1)
        lead=('Актуальный этап 100: `UNKNOWN_BOUNDARIES_RU.md` / `unknown-instruction-boundary-review.json`, компонент `unknown_instruction_boundary_review`. '
          'Закрыты шесть оставшихся неиндексированных anchored-кандидатов old-Objects106 из этапа70: четыре начинаются после66-префикса настоящего WORD104, '
          'один внутри JMP и один внутри нулевого immediate. Восстановлены пять switch arms/317 инструкций4979B0; '
          '647-state localCFG и явно ограниченный stack spill/reload review отделяют настоящее экземплярное DWORD107 от definition Objects. '
          '6 полных code extents/1PEтаблица,84 sourceSHA/4EXE проверены;0 новых native/rawpatch. Компонент79,прежние78схем побайтово сохранены. '
          'Нового игрового имени не назначено;глобальная неиспользуемость поля не доказана,существенные неизвестные15семейств открыты.\n\n')
        t=t.replace('# Комплект для разработчика редактора\n\n','# Комплект для разработчика редактора\n\n'+lead,1);p.write_text(t,encoding='utf8')
    p=ROOT/'READINESS_RU.md';t=p.read_text(encoding='utf8').replace('после этапа 99','после этапа 100',1)
    t=t.replace('актуальный опубликованный этап — 99, пакет содержит 78 компонентов','актуальный опубликованный этап — 100, пакет содержит 79 компонентов',1)
    if 'Этап100 закрыл' not in t:
        t=t.replace('## Воспроизводимость\n\n','## Воспроизводимость\n\n'
          'Этап100 закрыл шесть оставшихся неиндексированных anchored-кандидатов Objects106 из набора этапа70 как ложные границы инструкций. '
          'См. `UNKNOWN_BOUNDARIES_RU.md`. Это статический результат и уточнение очереди поиска,не новая расшифровка свойства или основание повышать общую оценку.\n\n',1)
    p.write_text(t,encoding='utf8')
    p=ROOT/'OBJECT_INDEXED_REVIEW_RU.md';t=p.read_text(encoding='utf8')
    if 'Обновление этапа100:' not in t:
        at=t.index('\n')+1;t=t[:at]+'\nОбновление этапа100: шесть оставшихся неиндексированных anchored-кандидатов разобраны и отвергнуты по настоящим границам инструкций; пять switch arms восстановлены. См. `UNKNOWN_BOUNDARIES_RU.md`. Другие кандидаты и общее назначение Objects34C остаются открытыми.\n'+t[at:];p.write_text(t,encoding='utf8')
    print('Phase100 readerstatus updated')
if __name__=='__main__':main()
