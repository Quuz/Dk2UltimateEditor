# Проверка ссылок каталогов

Three explicit scopes, not inferred effective AutoSave load order. Zero IDs skipped as null. Only listed field rules audited. Diagnostics do not authorize repairs.

Автозамена и перенумерация не выполнялись. Связи проверены в трёх отдельных наборах, включая действительно указанный в AutoSave kop4Globals.

## standalone editor catalogues

Проверено ненулевых ссылок: 1530. Диагностик: 0.

| Семейство | ID записи | Поле | Цель |
|---|---:|---|---|

## adjacent AutoSaveGlobals.kld snapshot

Проверено ненулевых ссылок: 1678. Диагностик: 0.

| Семейство | ID записи | Поле | Цель |
|---|---:|---|---|

## referenced maps/kop4Globals.kld

Проверено ненулевых ссылок: 1679. Диагностик: 0.

| Семейство | ID записи | Поле | Цель |
|---|---:|---|---|

Правила представлены в validate_references.py. Для CUBE_GEN (GenerationType4) GenerateIds разрешаются как Effects при бите0x40, иначе как EffectElements: это подтверждено веткой0x43895D. Для GOLD_COST(type14) проверяются десять derived ElementId=base+digit, а для SPRAY(type6) дополнительно проверяется SecondaryEffectId из raw+DA. Для остальных режимов оставлено прежнее консервативное правило0x40/0x80, неоднозначные комбинации и все внутренние ID числовых диапазонов здесь не проверяются. NextEffectId у EffectElements указывает на Effects, что подтверждено вызовом bridge+94 из0x436430. Графические имена ресурсов, текстовые таблицы, ссылки триггеров и все возможные Data1/Data2 здесь не проверяются. Наличие ссылки не доказывает достижимость или использование записи в игре.
