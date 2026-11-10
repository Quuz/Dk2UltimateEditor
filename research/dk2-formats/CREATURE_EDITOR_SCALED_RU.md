# Creatures: дробные значения и условная потеря Torture Time

24 октября 2026, этап19. Восстановлены22 дополнительные скалярные привязки родного Editor Pro: DWORD файла → промежуточная структура → double модели → double панели → native-подпись → writer. Вместе с35 primitive-полями этапа18 получено57 разных числовых привязок. Полного восстановления всех свойств Creatures это не означает.

## Три пары масштабов

| Поля в этой выборке | Из raw в GUI | Из модели в native-writer |
|---|---|---|
| 20 полей, включая Width, Speed, длительности | raw /4096 | double ×4096 → integer helper → low32 |
| Air Friction+0xEAC | raw /65536 | double ×65536 → integer helper → low32 |
| Scale (Percent Of Normal)+0xFCD | raw ×100 /4096 | double ×40.96 → integer helper → low32 |

Callback0x4A01B0 перед преобразованием в double формирует временное64-битное значение с raw в младшем DWORD и нулём в старшем. В этой ветке raw рассматривается как **unsigned DWORD**; отрицательная baseline-трактовка старшего бита здесь не применяется. Это представление native-редактора, а не доказательство, что каждый игровой consumer использует ту же signedness и единицы.

Шесть double-констант непосредственно прочитаны из PE, сохранены вместе с физическими байтами, точными binary64-дробями и SHA-256. Например, raw Scale4096 отображается как100 процентов нормального размера. Для Scale константа40.96 в EXE — приближённый binary64; инструмент сохраняет именно её, не подменяет точным обратным рациональным числом. Проверка границ показывает, что на неизменённом loaded u32 положительная ошибка произведения меньше0.000001; это математическая проверка, не эмуляция всего x87.

Базовые имена вроде Unkea0/Unkea8/Unk3ab/Unkee0/Unk4 сохранены для аудита. Их native-подписи — Width, Mass, Air Friction, Ability Jump (Tiles), Scale (Percent Of Normal). Для части этих полей игровые потребители уже разобраны в `PHASE2_FINDINGS_RU.md`; native-интерфейс служит дополнительным независимым подтверждением, не заменяет предыдущий анализ.

## Torture Time: дробное поле обновляется через byte

Native-подпись ресурса0xF856 — **Torture Time To Convert (Seconds)**. Физическое поле — raw+0xEF4, DWORD. Editor converter передаёт его в intermediate+0x12F4, callback — в model+0x1698 с делением на4096, main fill — в panel+0x17D8. Extract копирует полные8 байт double в общий буфер ввода.

Но update0x43FD60 в case0xF856 выполняет:

```c
lVar2 = __ftol();
*(double *)(param_1 + 0x17d8) = (double)((uint)lVar2 & 0xff);
```

То есть после преобразования к целому сохраняется **младший байт**, и уже он становится новым double панели. Commit0x441160 переносит его в model+0x1698; writer0x4A7F00 умножает на4096 и записывает DWORD обратно в raw+0xEF4. Поле в файле остаётся четырёхбайтовым: сужение происходит в GUI-обработчике.

При исходном loaded-значении, без изменения текста и с обычным truncation `__ftol`, проекция такой ветки:

```text
raw_after = (((raw_before >>12) &255) <<12)
```

| До обновления, секунд в GUI | После этой ветки | Raw до → после |
|---:|---:|---|
| 255 | 255 | 1044480 →1044480 |
| 256 | 0 | 1048576 →0 |
| 280 | 24 | 1146880 →98304 |
| 300 | 44 | 1228800 →180224 |
| 360 | 104 | 1474560 →425984 |
| 32000 | 0 | 131072000 →0 |

Дробная часть также теряется: raw4097 соответствует1.000244140625 и в этой проекции становится raw4096. Это **условный результат исполнения конкретного update-case**, а не утверждение, что любое открытие или сохранение файла меняет время.

Main commit вызывает update-dispatcher для **выбранного** свойства. Property ID хранится в panel+0x1AA8; extract устанавливает его из текущего выбора, update читает его. Поэтому при смене других свойств нельзя автоматически применять маску к Torture Time. Сам writer читает double модели и не содержит этой byte-маски. Прочие22-полевые преобразования в этой выборке также не должны применяться как неявная нормализация при обычной пересборке.

## Что обнаружено в данных

В80 файлах карт проверены3642 записи Creatures. При проекции update Torture Time с точным исходным loaded-значением **941 запись изменилась бы**. Например, в `3DLevelGlobals.kld`: Prince Tristran300→44, Mistress280→24, King Reginald32000→0. Это результат статической проекции, а не наблюдение запуска GUI на этих картах.

У остальных21 свойства при этой же проверке исходные raw-значения сохраняются. Полные распределения raw, пары Torture Time before/after и примеры — `phase19-validation.json`. Никакие значения не исправлялись и игровые файлы не записывались.

## API и воспроизводимость

`creature-editor-scaled.json` — компонент `creature_editor_scaled` общего пакета. `build_creature_editor_scaled.py` строит его из сохранённых native-выгрузок и PE-констант. Алиас pcVar9 учитывается только до первого изменения указателя; alias iVar3 в fill — только пока он явно ссылается на модель. Это не глобальная замена совпадающих имён переменных.

`native_creature_editor_scaled.py` предоставляет read-only `views`, точное рациональное `loaded_value`, явно условную проекцию `project_loaded_property_update` и чистое точечное изменение `patch_raw` с возвратом копии записи:

```python
from native_creature_editor_scaled import views, project_loaded_property_update
values = views(creature_record)
scenario = project_loaded_property_update(0xEF4, 300 *4096)
assert scenario['display_after_update'] == 44
```

Проекция предполагает исходное значение без округления при текстовом отображении/парсинге и truncation `__ftol`. Она не моделирует произвольные отредактированные double, NaN/Infinity, режимы x87 или весь интерфейс. Native-диапазон ввода отдельно не восстановлен. Нельзя выдавать полученную маску за универсальное ограничение допустимых игровых длительностей.

Проверки:424 граничных случая,3036 точечных пересборок в памяти —22 поля всех138 существ в standalone Creatures, AutoSaveGlobals и kop4Globals. Прочие байты, заголовки и секции сохранены побайтно. Проверены native-цитаты, writer assembly,22 подписи и6 PE-констант; SHA-25617 исходных файлов и2 EXE не изменились. Новых Ghidra-запросов, native-эмуляции и запуска модифицированных карт не выполнялось.

## Полные22 привязки

| Raw DWORD | Property ID | Native-подпись | Raw → GUI |
|---|---|---|---|
| +0xCEE | 0xf862 | Work Modifier Default | raw /4096 |
| +0xCF2 | 0xf863 | Work Modifier Level Increase | raw /4096 |
| +0xD01 | 0xf86a | Perception Range [ Fog Of War ] | raw /4096 |
| +0xD37 | 0xf86b | Shuffle Speed (Tiles Per Second) | raw /4096 |
| +0xEA0 | 0xf66e | Width | raw /4096 |
| +0xEA4 | 0xf66f | Height (Tiles) | raw /4096 |
| +0xEA8 | 0xf722 | Mass | raw /4096 |
| +0xEAC | 0xf6b6 | Air Friction | raw /65536 |
| +0xEB0 | 0xf834 | Eye Height (Tiles) | raw /4096 |
| +0xEB4 | 0xf679 | Speed (Tiles Per Second) | raw /4096 |
| +0xEB8 | 0xf7a7 | Run Speed (Tiles Per Second) | raw /4096 |
| +0xEBC | 0xf67e | Hunger Rate | raw /4096 |
| +0xEC8 | 0xf67b | Distance Can See (Tiles) | raw /4096 |
| +0xECC | 0xf67c | Distance Can Hear (Tiles) | raw /4096 |
| +0xED0 | 0xf857 | Stun Duration (Seconds) | raw /4096 |
| +0xED4 | 0xf858 | Guard Duration (Seconds) | raw /4096 |
| +0xED8 | 0xf859 | Idle Duration (Seconds) | raw /4096 |
| +0xEDC | 0xf85a | Slap Fearless Duration (Seconds) | raw /4096 |
| +0xEE0 | 0xf85b | Ability Jump (Tiles) | raw /4096 |
| +0xEF4 | 0xf856 | Torture Time To Convert (Seconds) | raw /4096 |
| +0xFCD | 0xf860 | Scale (Percent Of Normal) | raw ×100 /4096 |
| +0x107B | 0xf867 | Sniper Zoom Multiplier | raw /4096 |
