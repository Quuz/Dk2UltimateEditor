# Historical project overview

Preserved on 10 November 2026 before the UltimateEditor repository launch. Dated status entries below are historical; use CURRENT_STATUS.md and the acceptance ledger for the latest recorded evidence.

# Dungeon Keeper 2 Map Editor

4ноября2026: [native-настройки игрока](NATIVE_PLAYER_SETTINGS_20261104.md) и [происхождение общих/локальных определений](DEFINITION_SOURCES_20261104.md). M15-02/M01-01/M01-02 приняты;6принятых/98открытых/1нет входа. Цель активна.

4ноября2026: [официальная камера и настройки игрока](OFFICIAL_CAMERA_AND_PLAYER_SETTINGS_20261104.md). В журнале3принятых/101открытый/1нет исторического входа; native-GUI игрока остаётся, полный scope сохранён.

4ноября2026: [камера в игре и Cancel преобразования](SAVE_CONSENT_AND_GAME_CAMERA_20261104.md), [конкретный остаток](MANUAL_REMAINING_20261104.md). Два новых PASS; полная цель активна, основной Save пока DKM/ZIP.

Актуально 3 ноября 2026: [M22 — поиск камеры, DKM и MCP25](CAMERA_SEARCH_LIVE_20261103.md).
Исправлен поиск видимого050: новая регрессия FAILдо/PASSпосле,4набора/8модальных
окон EN/RU×native/DKM. Добавлен ограниченный MCP для уже открытой библиотеки.
Опубликованный EN125% DKM: unknown9999, Search050/portal, выбор050, Cancel,
один UndoRedo; Save/reopen всех7действий, compile7/чтение KWD50/1/0 точны.
43проекта и MCP transport25 PASS. РедакторRU, чистый AcceptedProject, без модальных окон.
Все23группы частично/0полностью. Далее движение камеры/сценарий в игре и остатки
M23 EN/DPI/hardware; игровые эффекты M03 и прочие остатки M01–M23 сохранены.
Миграция KWD без DKM/ZIP ещё не начата. Ниже — предыдущие результаты/старые очереди.

Актуально 3 ноября 2026: [M03 — диалоги сохранения и реальная ошибка записи](SAVE_DIALOGS_LIVE_20261103.md).
Настоящий GUI RU125%: SaveAs Cancel, overwrite Нет/Да, реальная блокировка замены
файла/ошибка/повторный Ctrl+S; буферы/Undo/прежние DKM+ZIP при отказе точны.
GUI ExportCopy создал DKM+точный ZIP; копия открыта, изменена, Ctrl+S/reopen точны.
Все1672 клетки проверены по фактическому KMAP. Исправлено неверное обещание Data
в сообщении экспорта EN/RU. Вся цель открыта:23группы частично,0полностью.
Следом M22 поиск/исполнение и M23 EN/DPI; игровые эффекты M03 и остальные
остатки M01–M23 сохранены. Миграция KWD без DKM/ZIP ещё не начата.
Ниже — предыдущие результаты; старые очереди не текущие.

Текущая [инструкция ручной приёмки](MANUAL_ACCEPTANCE_RU.md)
обновлена 3 ноября 2026. [Текущие результаты M22 и остаток](CAMERA_PATHS_LIVE_20261103.md).
Исправления EN/RU, Jobs и вылета дочерних окон мастера установлены в прежнюю PreFinal/App. Все23 группы частично проверены, полностью закрытых нет.
Сначала завершаем23 ручные группы текущей сборки; затем [переходим на KWD без рабочего DKM и исходного ZIP](KWD_NATIVE_TRANSITION_PLAN.md).
Переход пока не реализован. Рабочие DKM сохранять вместе с исходным ZIP.

Один основной проект редактора, предфинальная версия с локальным MCP от 2 ноября 2026.
Текущий код находится в `src/`; решение `DK2MapEditor.slnx` содержит приложение,
ядро и тесты. Старые поставки не являются входом сборки. Восстанавливать исходники
по цепочке предыдущих версий не требуется.

Локальный MCP добавлен отдельным opt-in мостом: [запуск и подключение](EDITOR_MCP.md).
Для него есть launcher `artifacts/PreFinal/Start-Editor-MCP.cmd` и stdio сервер
`artifacts/PreFinal/Mcp/DK2MapEditor.Mcp.exe`. Обычный запуск App не включает MCP.

## Использование

Готовый редактор: `artifacts/PreFinal/App/DK2MapEditor.App.exe`.
Для запуска нужны Windows и .NET 10 Windows Desktop Runtime. Все файлы App
нужно хранить вместе. Для сборки нужен .NET 10 SDK.

- [Подробное руководство на русском](USER_GUIDE_RU.md)
- [English user guide](USER_GUIDE_EN.md)
- [Текущее состояние и ограничения](CURRENT_STATUS.md)
- [Индекс технической документации](INDEX.md)
- [Архитектура и сборка](ARCHITECTURE.md)
- [Полный список требований соответствия оригиналу](FEATURE_PARITY.md)

## Сборка из текущего проекта

Из корня репозитория в PowerShell:

```powershell
./build.ps1
```

Команда собирает всё решение в Release и публикует единственный основной
редактор в `artifacts/PreFinal/App`. Интернет-пакеты не используются;
NuGet.Config отключает внешние источники. Требуются установленный SDK и
Windows Desktop targeting/runtime components.

Проверки ядра, без запуска редактора и игры:

```powershell
./build.ps1 -Validate
```

Полный установленный корпус можно указать параметром `-CorpusRoot`. Известные
ошибки корпуса дают ненулевой код завершения; смотрите полный отчёт,
не считайте любой ненулевой результат новой ошибкой компиляции.
GUI/игру проверяет пользователь вручную.

## Форматы и размещение

Рабочий документ — DKM. При импорте игровой карты храните рядом её исходный
`.native-source-…zip`: это данные карты для точного сохранения, а не код редактора.
Игровой комплект в Maps содержит ровно семь файлов: KWD, Globals, Map, Players,
Things, Triggers, Variables. Общий GlobalVariables и редакторские метаданные
располагаются вне этого комплекта. Пользователь подтвердил открытие комплекта
в игре и официальном редакторе. Полная функциональная цель ещё не завершена.

`Source/` и `Binaries/` — сохранённый оригинал и необходимые исходные ресурсы.
`src/` — единственный актуальный код нового редактора. `tests/` — проверки и
стабильные fixture-данные. `docs/` — документация. `artifacts/` — результат сборки и текущие доказательства;
разовые инструменты очистки сохранены только вместе с этими доказательствами.
`Archive/` — архив прежних экспериментов и документов, не участвующий в сборке.
Никакие commit/push/reset не выполнялись.

## English

This repository has one canonical application source tree in `src/`.
Run `./build.ps1` with the .NET 10 SDK on Windows. The pre-final executable is
`artifacts/PreFinal/App/DK2MapEditor.App.exe`; keep its companion files together.
Read [USER_GUIDE_EN.md](USER_GUIDE_EN.md). Archived experiments are not build
inputs. Seven-file game bundles have been opened by the user in the game and
the official editor; complete editor parity remains an active goal.
