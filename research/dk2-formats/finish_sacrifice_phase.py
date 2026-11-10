"""Integrate the new schema while preserving the previous 67 components."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
package=json.loads((ROOT/'editor-format-package.json').read_text(encoding='utf8'))
prior=ROOT/'phase88-prior-components.json'
if not prior.exists():
    assert len(package['components'])==67
    prior.write_text(json.dumps(package['components'],ensure_ascii=False,indent=2),encoding='utf8')
doc=ROOT/'INTEGRATION_RU.md';text=doc.read_text(encoding='utf8')
paragraph=('Актуальный этап 88: `SACRIFICE_RU.md` / `sacrifice-semantics.json`. '
 'Оригинальные загрузчик, история трёх жертв, сопоставление и полный spell-sacrifice dispatcher подтверждены 59 native-сценариями: '
 '57 завершений и 2 ожидаемых IDIV-by-zero. Подтверждены упорядоченный suffix, first-match в порядке записей, '
 'средний уровень существ с BYTE-суммой, полная очистка истории, четыре ветки наград и signed/capped mana. '
 'Типы наград 5–9 не имеют отдельной ветки в этом DX dispatcher. 16 полных PE-функций и 2 таблицы сверены; '
 'код и World vtable не заменялись. 1188 точечных пересборок всех 44 рецептов сохраняют остальные байты. '
 'Расширенный поиск вложенных Variables: 581 вход, 557 strict-roundtrip, 24 отказа сохранены без объявления файлов повреждёнными. '
 'Добавлен 68-й компонент; прежние 67 сохранены. Успешная выдача creature/object/gold, графика/речь и остальные существенные неизвестные остаются открытыми.')
if 'Актуальный этап 88:' not in text:
    text=text.replace('Актуальный этап 87:','Предыдущий этап 87:',1)
    title,rest=text.split('\n',1);doc.write_text(title+'\n\n'+paragraph+'\n'+rest,encoding='utf8')
readiness=ROOT/'READINESS_RU.md';text=readiness.read_text(encoding='utf8')
note=('Уточнение после этапа 88: правила порядка жертв, выбора первого рецепта, расчёта уровня, очистки истории и награды маной '
      'теперь подтверждены оригинальными функциями. См. `SACRIFICE_RU.md`. Оценка 75% ниже остаётся грубым ориентиром; '
      'описание храмового сопоставления как ещё не проверенного отражает состояние до этого этапа. Портал и бонусы вселения этим этапом не закрыты.')
if 'Уточнение после этапа 88:' not in text:
    title,rest=text.split('\n',1);readiness.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
print('Phase88 integration written; prior67 component manifest retained')
