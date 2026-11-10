# Заливка: идентичность клетки исходного редактора

25 октября 2026. Исправлен последний установленный дефект I07.

`Compiler_GameMapToEditorMap` читает terrain/owner/variant и переводит их в
каталожные внутренние клетки. Четвёртый внутренний byte выбирает World/Rooms/
Gates/Walls; native reserved byte не читается как этот discriminator.
`Map_FillArea` сравнивает внутренние клетки, затем заполняет связную область
по четырём соседям. Равенство начальной клетки выбранной не прерывает обход.

Раньше `FloodFillEditor` сравнивал все четыре native bytes. Из-за этого разные
reserved bytes разделяли одинаковую область. Исправление использует общий
`ConversionTile` для ключа сравнения, с локальным cache по первым трём bytes.
Учитываются fixed catalogue owners, owner/variant fallback и неизвестный terrain.
Разные владельцы owned paths/rooms и варианты bridge17 остаются разными.

Это вычисление ключа, а не ремонт карты при загрузке. Load сохраняет исходные
клетки; невыбранные клетки и все object/region/scenario buffers остаются точными.
Заполненные клетки получают полный selected MapTile, как и прежде. Защита
периметра и clipping по выделению сохраняются. Raw `FloodFill` использует прежнее
полное четырёхбайтовое сравнение и equal-target no-op. Его контракт не изменён.

Новые семь групп проверяют native и DKM, reserved0/A5/FF, fixed owners,
owned-path разделение, bridge17 variants, диагональную несвязность, import
fallback, равное начало, selection clipping, protected border, raw API,
Undo/Redo, фактическое Save/reopen и один accepted editor-call. Проверяется
полное содержимое tiles и всех остальных buffers. До исправления шесть групп
FAIL, одна PASS; после семь PASS. В уже проходившем bridge fixture ID10 заменён
на правильный17; шесть воспроизводивших дефект случаев не изменены.

Evidence: `artifacts/Evidence/EditorFloodComponents-20261025/`:
baseline.json, before-focus-01.log, focus-01.log, build-01/publish-01-exit.json,
i07-closure-proof.json, scoped-source-diff.patch и delivery.json.
Это headless Core checks. Физический ввод, окно редактора и игра не запускались.
Полный корпус не повторялся. [Конечная сверка I07](I07_CODE_CLOSURE.md).
