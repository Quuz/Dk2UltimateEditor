# Фактическая проверка оригинального редактора — 10 октября 2026

Запущена точная отдельная копия 25 файлов из Binaries (14,3 MB),
`Test/original-editor-physical-20261010-01/Binaries/Editor.exe`.
Исходные Source/Binaries и установленные карты не редактировались.
Применён навык computer-use: настоящий ввод мышью и клавиатурой через @oai/sky,
наблюдение реальных окон и системного Save As. Французская локализация этого
исходного executable не является проверкой русского/английского нового App.

Фактически выполнено:

- Save As в отдельную тестовую папку.
- Выбор Crayon и лавы; штрих мышью по клеткам (3,3)—(8,3).
- Ctrl+S и второй Save As. Оба конечных файла содержат первый штрих.
- Второй штрих по (3,5)—(8,5), затем один Ctrl+Z: второй штрих исчез,
  первый остался. Эта отмена наблюдалась до сохранения.
- Просмотр лимита Undo: 200, закрытие без изменения. После сохранения
  список Undo пуст, как требует исходный Files_SaveMapQuick.
- Тестовая копия закрыта Alt+F4; открытых окон процесса больше нет.

Сохранены два снимка одного состояния ландшафта:
`original-blank-01.dkm` (историческая метка имени; Ctrl+S обновил его после
штриха, поэтому это НЕ пустая карта) и `original-pen-stroke-01.dkm`.
Реальные размеры в KMAP:80×80. Показание48×48 в строке состояния — масштаб
клетки; первоначальная ошибочная трактовка отклонена, script01 сохранён.
В обоих файлах ровно шесть клеток лавы в первой строке штриха;
второй штрих в файлы не записывался. SavedTimes2 и3.
Секции: KSTS v1, KINF v7, KPLY v3, KMAP v1, KTRG v3.

Текущая Core DLL Debug/Release действительно проверила оба снимка:
точное структурное чтение всех bytes, все6400 tile DWORD,
неизменные порядок/версии/все non-KSTS payloads после SaveCopy,
увеличение счётчика сохранений на1, сохранение paint/Undo/LMB counters,
чистое повторное открытие и неизменность входных файлов.
Это два состояния статистики ОДНОЙ карты, не две независимые карты.
Проверяющий проект ссылается на готовые DLL; основная версия не пересобиралась.
Первые две ошибки настройки probe (путь reference/API HasChanges) и доисправленные
исходники сохранены; фактический окончательный build/run03 чистый в обеих сборках.

Независимое доказательство:
`Test/original-editor-physical-20261010-01/actual-original-gui-dkm-probe-verified-03.json`.
Данные наблюдения: `original-stroke-observation-02.json`; файлы и DLL привязаны SHA256.

Открыто: физический ввод в новом App, его системные Open/Save/colour dialogs,
EN/RU, аудио, hardware/DPI, другие original GUI workflows, DKZ/старые форматы
и фактический запуск скомпилированной карты в игре. Полная цель не завершена.

Additional actual original hero-band acceptance, 2026-10-11:
The original GUI loaded the private v5 project, changed the second member of
the second band from assist-leader to independent, and used native Save As to
a new private DKM. A literal source-layout check proves exactly one band-section
bit changed (Options2 45→41); every other band/member byte and all five inputs
remain unchanged. The original app was then closed.
Ready main Core Debug/Release passed two clean probe builds/four actual gates,
three model/native groups each: actual-original clean load and typed fields,
private Cancel/equal Apply/one Undo, and eleven native compiler fields per member
with private save/reopen. Main DLLs were not rebuilt. Accepted proof:
`Test/original-hero-band-physical-20261011-01/actual-original-v5-model-native-accepted-01.json`.
This does not accept the modern physical GUI/audio/hardware/DPI/game, the
pending role-caption candidate, or the still-running combined full UI gates.
