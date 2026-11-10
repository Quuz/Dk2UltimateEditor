"""Describe remaining semantic uncertainty without claiming unknown bytes are padding."""
from pathlib import Path
import json
import dk2_formats as fmt

ROOT=Path(__file__).resolve().parent
def main():
    result={}
    lines=['# Покрытие схем и оставшиеся неизвестные','',
      'Полное покрытие диапазона байтов означает, что известны границы всех полей/областей и их можно сохранить. Это не процент понимания игровой семантики. В базовой схеме сохранены имена OpenKeeper; дополнительные имена и уровень доказательств указаны в verified-overrides.json.',
      'Неизвестные поля нельзя считать резервом только потому, что они нулевые в этих файлах. Отсутствие ветки Stat ID в setter не доказывает отсутствие её использования в других системах.', '',
      '| Вариант | Размер записи | Строк схемы | Строк с неустановленным смыслом в OpenKeeper | Их байт |','|---|---:|---:|---:|---:|']
    for name,layout in {**fmt.LAYOUTS,**fmt.LEVEL_LAYOUTS}.items():
        fields=layout['fields'];cursor=0;unknown=[]
        for f in fields:
            assert f['offset']==cursor,(name,f)
            cursor+=f['size']
            if f.get('semantics')=='unresolved' or f['name'].startswith('Unknown'):
                unknown.append({k:f[k] for k in ('name','offset','size','kind')})
        assert cursor==layout['record_size']
        result[name]=dict(record_size=cursor,byte_span_covered=True,field_rows=len(fields),
            unresolved_baseline_rows=unknown,unresolved_baseline_bytes=sum(f['size'] for f in unknown))
        lines.append(f"| {name} | {cursor} | {len(fields)} | {len(unknown)} | {result[name]['unresolved_baseline_bytes']} |")
    lines+=['', 'В Creatures_5537 значительная часть строк — развёрнутые 80 неизвестных байтов расширения; это не 80 доказанно независимых свойств. Общие структуры ArtResource/Light раскрыты отдельно, поэтому их вложенные поля не увеличивают число строк этой таблицы.',
      '', '## Что ещё требует проверки смысла','',
      '- Назначение неизвестных областей Terrain, дополнительных полей Doors/Traps и хвостов Creatures; часть может быть устаревшими данными редактора.',
      '- Проверка в игре сочетаний 39 ProcessType и 9 DamageType снаряда. Статические пути Data1/Data2 уже описаны в SHOT_PARAMETERS.md; не все сочетания проверены экспериментально.',
      '- Все эффекты неизвестных флагов и перечислений, а также ограничения конкретных ID и массивов игры при расширении каталогов.',
      '- Единицы и знаковость полей, для которых доказан только перенос целого числа. Их нельзя автоматически переводить через /4096.',
      '- Действие runtime-патчей Flame и переопределений других карт; выводы по оригинальному EXE привязаны к SHA-256 из SOURCES.md.',
      '- Проверка изменённых копий карт внутри игры: здесь выполнены статический анализ, валидация границ, ссылок и точная обратная сборка.',
      '', 'Список неизвестных полей по каждому варианту доступен машинно в coverage.json. Дальнейшая проверка строится как поиск использования конкретного поля в движке и управляемый эксперимент на копии карты. Из неизвестных областей не создавались вымышленные игровые настройки.', '']
    (ROOT/'coverage.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    (ROOT/'COVERAGE.md').write_text('\n'.join(lines),encoding='utf-8')
    # First table ID is statically initialized in PE; callback set by constructor.
    path=ROOT/'variable-dispatch.json';rows=json.loads(path.read_text())
    if not any(r['address'].lower()=='0x6a5cd0' for r in rows):
        rows.insert(0,dict(address='0x6a5cd0',type_expression='1',handler='FUN_0051c9c0',arguments=['0','0','0'],
            evidence='First DWORD from game-variable-first-table-row.txt; callback from game-variable-dispatch-init.txt'))
    for r in rows:
        if r['type_expression']=='0':r['note']='Table terminator, not a supported variable type'
    path.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Contiguous byte spans verified:',len(result),'catalogue/level variants')

if __name__=='__main__':main()
