# Зарезервированные имена игровых комплектов

25 октября2026, CompilerReservedNames-20261025. Добавлено пропущенное правило
Compiler_TaskIsReserved:63 имени штатных campaign/MPD/secret/multiplayer levels.
Каталог читается из встроенного сохранённого Compiler_Compile.c; отдельная
переписанная вручную копия списка не используется. Сравнение точное без учёта
регистра, без trim/prefix эвристик.

Source Test выдаёт Information REPORT_RESERVED2, а Generate останавливается
до первого writer. Теперь полная проверка DKM показывает DK2N046 Information
с инструкцией «Сохранить как». Core CompileLegacyProject отказывает для
зарезервированного **переданного имени игрового комплекта** до создания output.
Команда App использует тот же helper и показывает локализованное сообщение
перед генерацией. Display title не переименовывается, карту никто не исправляет.

В текущем редакторе filename и внутренний title разделены. App компилирует
под stem исходного DKM; исходный C использовал Map.Properties.Name и для
title, и для naming references. Поэтому проверяется фактический output stem,
а не независимое внутреннее название. Изменение только title не снимает
ограничение. После Save As под другим именем проверка видит новый stem.
Для non-ASCII/>32/пустого имени используется прежний CompiledMap fallback
(пустое имя теперь также обработано явно). Допустимые ASCII stems прежние.

Открывать, редактировать, сохранять DKM и native input со штатным именем
можно. Ограничение относится к новой DKM→game generation. Прежняя private
factory includeDeclaredKeepers, создающая native draft, и raw native SaveCopy
не превращаются в такой publish; их прежнее поведение сохранено. Вся запись
остальных полей, UID/scopes, даты и формат семи файлов не менялись.

## Подтверждение и границы

Шесть headless groups: все63 source names и upper-case варианты; exact
non-prefix/non-trim matching и filename fallbacks; Test Information против
structural-only mode; независимость внутреннего title; actual DKM Save As,
реальный seven-file export с разрешённым тестовым именем и сохранением title;
native factory и буквальный raw SaveCopy.

Три проверки отсутствующего правила дали FAIL на прежнем Core; допустимый
Save As/export уже PASS. Финальные6 PASS/0 FAIL, ExitCode0. При подготовке
fixture исправлены имя API, передача всех полей general draft и выбор нового
подкаталога для SaveCopy. Product rules ради этих ошибок не ослаблялись;
неуспешные логи сохранены. Проверки source/history/revision/dirty/counters
подтверждают, что чтение и отказ не меняют документ.

Все41 проекта собраны без warnings/errors. App/tests/published Core совпадают.
Source proof отдельно подтверждает до-writer guard и общий stem App/validator;
реальный WPF dialog не запускался и остаётся ручной приёмкой. Нет нового
полного corpus run и повторов несвязанных успешных групп.

Evidence: `artifacts/Evidence/CompilerReservedNames-20261025/`.
2879 EN/RU keys, прежние2878 literals сохранены. Source/Binaries/Fixtures,
установленные карты и samples не менялись; архивы обновлены. I02 остаётся
открытым до завершения общей validator/version matrix;8I/23M и исходная цель
прежние. Исторические1840/63 не являются full текущей DLL.
