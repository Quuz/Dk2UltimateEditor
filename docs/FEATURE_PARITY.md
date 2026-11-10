# Editing parity requirements — current project

Актуально 4 ноября 2026: [native-настройки игрока](NATIVE_PLAYER_SETTINGS_20261104.md)
и [происхождение определений](DEFINITION_SOURCES_20261104.md).
M15-02 принят в DKM/native: частный draft/общая отмена, один полный UndoRedo,
source-профиль2/gold5432/health61/камера15,17, полные Players/Things/export7/reopen.
M01-01/02 приняты: общие16 и локальные8 буферов точны; у того же Goblin
ExpPts200 из Creatures.kwd и201 из Globals.kld, пути предупреждений/inspector согласованы.
Игра осталась на Flame unpackResources до наблюдаемой загрузки карты;
ресурсы не приняты, все746 Maps/config/logs восстановлены. Физические клавиши
камеры открыты: CU не вернул отдельное окно; MCP Cancel сохранил весь документ.
Редактор — чистый RU AcceptedNative без модальных окон.
[Перечень](MANUAL_REMAINING_20261104.md):105сценариев,6приняты/98открыты/1нет входа.
Оконная матрица EN/RU/DPI отдельно; группы целиком не объявлены закрытыми.
Цель активна, KWD-миграция не начата. Далее оставшиеся поля игрока и M02.
Ниже исторические записи, их очереди и counts не текущие.


Актуально 4 ноября 2026, следующий проход: [официальная камера и настройки игрока](OFFICIAL_CAMERA_AND_PLAYER_SETTINGS_20261104.md).
Официальный DK2Editor увидел Keeper1→Time>=45→Path50/AP1/ShowCeiling; файлы прежние.
В DKM/RU125% общие Cancel/Apply настроек игрока прошли:84source присваивания ИИ,
gold4321/health83, все7строк, один полный UndoRedo, Save/reopen/compile7 точны.
Native Players/Things целиком сверены с исходной C-разметкой; игрокам/сердцам
посторонних правок нет. Native-GUI ветка M15 остаётся открытой, игра не запускалась.
Редактор — чистый прежний RU AcceptedProject. Для дочерних окон использовать
возвращённый Window из sky.list_apps(), а не disabled owner из list_windows().
[Перечень](MANUAL_REMAINING_20261104.md):105сценариев,3приняты/101открыт/1нет входа;
частичный M15 учтён отдельно. Все23группы частичные; цель активна, KWD-миграция не начата.
Ниже — более ранние датированные отчёты; их counts/очереди исторические.

Актуально 4 ноября 2026: [два новых принятых маршрута](SAVE_CONSENT_AND_GAME_CAMERA_20261104.md).
В игре сработал player3→elapsed45→camera050, cinematic включился и вернулся HUD.
В настоящем RU125% SaveAs отмена предупреждения преобразования сохранила все
буферы/dirty/revision/Undo/target; DKM/ZIP не появились. Все746 игровых map-файлов
и прежние настройки восстановлены точно. Редактор — чистый RU AcceptedProject.
[Конкретный остаток](MANUAL_REMAINING_20261104.md):105сценариев (2приняты,
102открыты,1нет подлинного входа) и отдельная матрица66окон EN/RU/DPI со сверкой
прежних доказательств. Это сценарии с вариантами, не число кликов или процент.
Все23группы частичные; цель активна. KWD без DKM/ZIP ещё не начат.
Ниже — прежние датированные результаты; старые очереди/counts не текущие.

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

Актуально 3 ноября 2026: [M22 — камера и исправление вылета мастера](CAMERA_PATHS_LIVE_20261103.md).
Native RU125%:52номера, схемы/потолок050/150, Cancel; путь150/потолок1
применён одним UndoRedo, ровно2байта Triggers.kld, область1 и другие файлы прежние;
export7/reopen точны. Исправлен Owner дочерних камеры/флагов появления в мастере.
Новая регрессия: FAILдо/PASSпосле,4набора/24модальных открытия; build43 без ошибок.
Опубликованный RU125% мастер больше не вылетает; child050/общая Отмена
сохранили прежние150/1/1 и полный fingerprint/revision0/history0.
Все23группы частично,0полностью закрытых. Это не процент готовности.
Далее остатки M03(сохранение), M22(поиск/исполнение), M23(EN/DPI); остальные
остатки M01–M23 сохранены. РедакторRU, чистая CameraPathsLive/AcceptedAreaNative,
без модальных окон. Игровой слот не менялся, игра не запускалась.
Основной Save ещё DKM/ZIP; переход на KWD — после ручной приёмки.
Ниже — история; прежние очереди и counts не текущие.

Актуальный результат1ноября: [сценарная привязка участника и перенос](BAND_SCENARIO_20261101.md). Реальные мастера, moves/UndoRedo/Save/KWD подтвердили uniqueid1→root7; исходные записи сохранены.18групп частично/5без основного прохода/0полностью. Следом nativeGUI и остаткиM13, затемM16/M17. Ниже исторические очереди.

25 октября: [I18 закрыт по коду](I18_CODE_CLOSURE.md),25 классов/два исправления. Открытых кодовых пунктов нет; остаются23 ручные группы. [Ручной план](MANUAL_ACCEPTANCE_RU.md). Исторические counts ниже не задают новый scope; полная цель не завершена.
История до закрытия I18: 25 октября: [I10 закрыт по кодовой сверке](I10_CODE_CLOSURE.md); [103-row callback matrix](I10_CALLBACK_MATRIX.md),15 контекстов. Нового runtime/full прогона нет, product прежний. Остался1 кодовый пункт I18 и23 ручные группы; исторические counts ниже не задают scope.
История до закрытия I10: 25 октября: [I09 закрыт по коду](I09_CODE_CLOSURE.md),12 source/caller маршрутов,4 новых PASS. Product bytes прежние. Осталось2 кодовых пункта и23 ручные группы; исторические counts ниже не задают новый scope.
История до закрытия I09: 25 октября: [I08 закрыт по коду](I08_CODE_CLOSURE.md),15 source/caller маршрутов и5 новых логических PASS. Product bytes не менялись. Осталось3 кодовых пункта и23 ручные группы; исторические counts ниже не задают новый scope.
История до закрытия I08: 25 октября: [I07 закрыт по коду](I07_CODE_CLOSURE.md),14 source/caller маршрутов. Исправлена идентичность области заливки. Осталось4 кодовых пункта и23 ручные группы; исторические counts ниже не задают новый scope.
История до закрытия I07: 25 октября: [I04 закрыт по коду](I04_CODE_CLOSURE.md); все20 format rows сопоставлены. Осталось5 кодовых пунктов и23 ручные группы. Исторические дополнения ниже не задают новый объём.

История до закрытия I04: 25 октября: [historical inspection/unknown native file policy](HISTORICAL_SECTION_INSPECTION.md) реализованы. [I04 format matrix](I04_FORMAT_MATRIX.md): remaining F01 descriptor header/metadata guard и конечная сверка. Все23 ручные группы сохранены.

История до закрытия I04: 25 октября: [packed definition inspection и collection suffix policy](NATIVE_PACKED_INSPECTION.md) согласованы с loader. Unknown strides целиком opaque; четыре collection suffix types требуют существующего loss consent с exact source archive. I04 остаётся для исторических секций, неизвестных whole companions и итоговой route matrix; M-группы сохранены.

История до закрытия I04: 25 октября: [native D5-before-D6 insertion](NATIVE_SCRIPT_PARTITION.md) исправлена вместе с переносом private action bindings и общей проверкой split-counts. Неправильные входы raw-preserved/read-only. I04 остаётся для packed inspector, historical sections, opaque suffix conversion и итоговой route matrix; M-группы сохранены.

История до закрытия I04: 25 октября: [physical native collection layouts и owner projection](NATIVE_COLLECTION_GUARDS.md) исправлены; 57 выбранных headless групп PASS на текущей DLL. I04 открыт для D5-before-D6 insertion, packed inspector, historical sections и итоговой route matrix. Все ручные M-группы сохранены.

Актуальный конечный перечень: [OPEN_FUNCTIONS_RU](OPEN_FUNCTIONS_RU.md) —
0 кодовых пунктов и23 ручные группы,всего23 пункта работы. Это не число
дефектов. I03 закрыт [сверкой14маршрутов](I03_CODE_CLOSURE.md); I02 закрыт [сверкой writers/validators](I02_CODE_CLOSURE.md);
все23 возможности иполный исходный scope сохраняются. Ручная GUI/game/audio/
DPI приёмка остаётся обязательной;успешные проверки без изменений не повторяются.

Current requirement ledger, 17 October 2026. The canonical product is `src/`;
build the single `DK2MapEditor.slnx` with `./build.ps1`. See
[CURRENT_STATUS](CURRENT_STATUS.md) for the latest executed checks and remaining
failures, and [manual acceptance](MANUAL_ACCEPTANCE_RU.md) for user-run checks.

The implementation column describes available code; it does not prove full
historical GUI or gameplay parity. Earlier per-subsystem test counts below bind
their dated inputs only. Current model/build proof does not imply physical
mouse, system-dialog, audio, DPI or game acceptance.

Historical worktree checkpoints and failed/superseded runs were moved verbatim to
`Archive/historical-documents-20261012/FEATURE_PARITY-before-current-audit.md`.
They are not build instructions. No capability row or scope requirement below
has been removed. Archived source versions are never required to compile.

## Required completion gates

- Full original editing capability coverage in EN/RU, including all rows below;
  semantic parameter pages and compound controls are not accepted from raw fields alone.
- Native and historical DKM/DKZ/MAP/AREA workflows, source defaults, explicit
  upgrades, immutable imported bytes and independent source-companion persistence.
  AREA selection routes and the default boundary are source-audited; finish exact
  direct-selection export, historical variants and physical workflow acceptance.
- Exact source editor action/time/save/caller-counter behavior, including the
  nonpainting callers; byte/history retention alone does not close this requirement.
- Original GUI comparison and physical modern UI acceptance for drawing,
  managers, nested draft Cancel/Apply, scenarios/aliases, palettes and catalogues,
  recovery, system dialogs, audio and hardware/multimonitor/DPI behavior.
- Full integrated checks against the current DLLs, with terminal exit codes;
  compare the entire ordered 94 historical failures and all subsequent changes,
  keep unresolved failures visible and verify protected Source/Binaries/maps.
- Exactly seven official game-map files; edited-map save/reopen and actual
  official-editor/game behavior, including changed scenarios and rules.
  All our physical game tests use DK2Preview_6a88240eaf814f78 via DKII-DX.exe.
- Current unified source project, reproducible build, consistent ready application,
  source/runtime archives and detailed current documentation without recovery chains.

No Computer Use or automated editor/game launches. User-run acceptance is pending
until the user reports the actual result. Preserve Source/Binaries, installed maps
and uncommitted work; do not reset, commit/push or repair installed maps.
The full goal stays active while any required gate remains unproved.

The 13 October read-only source inventory verified all 262 field names and native
layouts across fourteen global pages, 73 project number formats, 22 ordered
catalogue bindings, all 103 scenario page/native operand schemas and 127 captured
semantic controls. This narrows the remaining audit without closing contextual
catalogues, compound callbacks, input behavior, historical versions or physical
acceptance. See [automatic closure inventory](AUTOMATIC_CLOSURE_INVENTORY.md).
Historical capture flags such as Implemented=false are not current product status.

The subsequent attraction fix uses the original type-selection callback: second
type changes reload the private quantity; inactive lists/text are empty; a first
visible catalogue fallback retains the stored ID until an explicit source write.
Native/project model checks cover private/common Apply, Cancel and exact Undo.
The real WPF checks were prepared and compiled only; physical input and remaining
context/compound catalogue requirements stay open. See
[attraction callbacks](CREATURE_ATTRACTION_CALLBACKS.md).

## Capability ledger

| Original capability | Modern implementation | Remaining acceptance work |
| --- | --- | --- |
| KWD + Map/Players/Things/Triggers/Variables loading | Native lossless bundle reader; variable KWD link tables; strict Western ANSI references with explicit-reference/fallback precedence shared by structural inspection; actual linked/shared definition header/version/size diagnostics, source-compatible tile/file-size metadata and retained opaque tails; referenced definitions and packed Globals.kld editable | More installations / native variants; deeper inspector collection-record and semantic checks; historical/game acceptance |
| New map | Original blank DKM or native bundle; all 23 backgrounds, 1–5 keepers, seven source-default player rows, project versions, square controls, large-map acknowledgement; optional capacity-checked heart starter; source-independent game definitions; default-backed global family creation/reset with undo; read-only shape/heart/keeper diagnostics | Historical-GUI and playable-game acceptance |
| Save / compile game map | Export NEW game-root copy; DKM-to-native compiler using original metadata/AI/event/global assignments; per-map definition overrides; explicit historical section upgrade before compilation | I18 remaining diagnostics; actual game acceptance and physical repeated-save UX. I02 writer/validator mapping is closed. |
| DKM project / DKZ selection / ASCII MAP | Bounded section reader; editable versioned metadata, players, things, rectangles, bands, scripts/aliases and globals; every documented older editable section can be upgraded with undo, including nested V1 scenarios; DKZ load/export with source KTGS1–6/KMAP projections and row-major/category object capture, stable buffer order, DKCP5 buffer/DKCP6 direct snapshots and DKCP1–5 read compatibility; MAP import/export with all original sections, the supported-scenario extension and explicit original-editor no-scenario policy; bilingual policy/loss confirmation and report plus complete editable snapshot  I03 source/owner/alias/graph/coordinate/loss routes and full32768-action KTRG1 upgrade closed by [14-route audit](I03_CODE_CLOSURE.md); native-copy editor-state boundary is explicit. | I04 format layouts closed by [20-row matrix](I04_FORMAT_MATRIX.md); I18 malformed identities/owners; M04 historical official-GUI/upgrade/text-export/game acceptance. Cross-format attached scripts use complete project compilation; no implicit alias merge. |
| Terrain, rooms, owned paths, reinforced walls, bridge variants | Native tile painting and ID/owner/variant palette; canvas painting/fill preserves protected edge bytes; source-catalogue world/hero-gate owner/variant normalization and heart/portal/gate cursor footprints; forced filled bounded gestures with EN/RU previews/status; read-only row-shape/size, duplicate-heart and keeper-sequence diagnostics | Gate-region interactions; asset previews; complete palette variant catalogue; original/game acceptance |
| Brush, line, outlined/filled rectangle, outlined/filled ellipse, flood fill | Ordinary tools and all five Shift/random shapes with source 1/2 probability/prefilter order, bilingual alternate checkbox, selection/protection and forced special footprints; Windows source GDI masks independently compared; atomic undo/redo without resampling; source pen initial/segment/release and39-row cursor-size route audited with78native/project footprint checks and current live-pen model checks; historical full synthetic/corpus/UI gates retained as dated evidence | Physical original/modern brush capture/timing/selection and historical PRNG/hardware/game comparison; remaining drawing callers; non-Windows masks not claimed GDI-equivalent |
| Rectangle selection, all/none, copy/cut/paste | Mouse and numeric rectangle selection, all/none; tiles and placed things with independently cloned reachable scripts and exact project scope ownership; transactional edge-clipped paste; internal copy buffer survives map changes; DKZ buffer load/save and direct selection load/save; original EN/RU replacement consent in actual menus/shortcuts/import, persisted suppression and stale/cancel/closed guards; UI copy excludes border objects; cut preserves border bytes/owners and uses a verified background for special tiles; separate protection mask follows crop/transforms/DKCP v3; paste skips protected destinations and fills masked sources with neutral solid | Band selection UX; special-placement rules; historical direct-export comparison; historical-GUI/game acceptance |
| Rotate / reflect | Original square-only in-place clockwise/counterclockwise rotation and axis-size guards for horizontal/vertical reflection; tiles, placed things and local script coordinates retain identities; explicit extra rotated-copy command supports rectangles without changing the map; DKCP v3 preserves external-coordinate provenance and protection masks; source-derived masked tile writes retain original all-object movement ordering | Gate orientation / footprint acceptance; historical-GUI/game comparison |
| Resize map / anchor / crop | Native and project resize; nine anchors; shift objects and script coordinates; source editor unclipped three-action targets with LONG/WORD projection and bound-project DK2N029 compiler check (EDITOR_RESIZE_SCRIPT_TARGETS); CPU-keeper-only player pairs with project X sentinels, native FFFF projection, retained V1/V2 opaque cameras/hearts and nonblocking DK2N030/031 warnings (EDITOR_RESIZE_PLAYERS); source-derived native crystal/gate targets during editor resize with preserved action bytes and DK2N032 warnings (NATIVE_RESIZE_DERIVED_TARGETS); guarded object crop; atomic undo; read-only terrain footprint diagnostics | Source specialized region-removal decisions, scope retention and predecessor rewiring; deeper native working-metadata semantics; native visual/game QA and other gameplay constraints |
| Undo / redo / history depth | Transactional byte snapshots including entity-aware selections and exact rollback; real read-only nested operation tree/counters and redo tree; original 200/default and 0–65535 depth with explicit zero confirmation, JSON/session preference and guarded lifetime; 64 MiB payload budget; full corrected synthetic/corpus/UI gates terminal | Historical modal/hardware/DPI comparison; memory/caller-counter differences documented in [history scope](EDIT_HISTORY.md) |
| Objects, special items, traps, doors, creatures | Typed native/project fields; named ID catalogue / bilingual search; add / delete / duplicate; attached script graphs and project scope links cloned independently; unique identity edits retain exact scope ownership; source-compatible single-placement preview and canvas brush with first same-category updates, door-lock toggles, creature stacking, all-opposite replacements, raw-owner terrain checks, script review and exact undo; native first-crystal target reprojection across field/add/delete/duplicate/batch/paint/selection/transform commands in the outer transaction with unchanged generic Resize policy (NATIVE_CRYSTAL_MUTATION); source first-free working gate IDs, deletion/reuse, DKM/compiler/generator identity retention, v1/v2 metadata and 255 capacity (NATIVE_GATE_IDENTITIES); source DKM gate deletion retaining incoming action IDs and other entity/script guards (PROJECT_GATE_REMOVAL); first shared region-removal Yes/No/remember/default/reset request, native/project one-kind batch, canonical immutable records and EN/RU inspector/panel integration (REGION_REMOVAL_CONFIRMATION); full source-default manager templates and EN/RU batch insertion with named flags/objectives, three modes, source terrain/owner rules, replacement decisions, historical consent and atomic undo; selected-object manager with original family views, five display sorts, bilingual/map-selection filters, grouped removal and all source masked modification/uint32 bit masks, relative/common coordinates, Keep/Remove/Modify collision re-review, retained identities/native signature migration, source category changes retaining literal old-scope points and UID with explicit-transition-only reconnection/D2OSv2 trap identity (MANAGER_CATEGORY_SCOPES), separate generic explicit-detachment contract and one-step undo; source Delete/Shift+Delete/Ctrl+Delete under-cursor family priority, stacked removal, creature-only shared0x100 prompt, per-record Undo/count and complete prior-order restoration (UNDER_CURSOR_REMOVAL); source shared manager/region requests0x2/0x10/0x100, default-only remember/reset, assigned-ID No filter, pre-filter ModifyMulti and guarded dialog lifetime (SHARED_SCENARIO_REQUESTS); shared manager-session templates/options/masks/coordinates, local single-record drafts and fresh consent; original-image type dropdown, grouped/searchable source-image chooser and manager row images with source mapping/pixel and full regression acceptance; original-image type/template/band controls with source 32-entry band choices and guarded Apply/Add; source main-map physical glyphs, per-owner creature counts and eight persisted view flags (complete 74-Core/216-WPF checkpoint; unchanged corpus refusals); read-only missing/ambiguous supported entity links and duplicate-identity warnings with source sentinels, active band prefix and opaque-layout guards | Other catalogue/definition/reference images; diagnostics for other catalogue references; historical/game acceptance |
| Areas, patrol paths, gates, effect generators | Native records / all documented fields; add / duplicate; typed identity remapping and deletion guards; named reference targets; bilingual map manager for rectangle/point add/select/show; private EN/RU region-property window with project labels/colours, area/effect children, guarded point/rectangle map picking and source equal-Apply oneUndo/count, private gate generation with child preview/confirmation, full command replay, outer Cancel, source allocation sum and final native gate bindings, native storage guards; coordinate changes with atomic undo; native destroy-target reprojection for geometry, field/type, deletion and parent replay with first-physical import lookup (NATIVE_GATE_MUTATION); persistent working gate links across coincident commands, private copies, Undo, clone/removal and DKM projection, with seven-file native reader policy (NATIVE_GATE_BINDINGS); inclusive endpoints, effect deltas and separate project category IDs; four independently named generator effect slots and integer frequency/slider with atomic options apply/undo and retained unknowns; patrol manager with draft/append/insert/remove/pause, loops/joins, quick map links and independent link display, source file-order manager grouping with predecessor/loop/branch assembly (PATROL_SOURCE_GROUPING), owned source local occurrence lists with empty roots, indirect-change warnings and explicit reload (PATROL_MANAGER_SESSION), guarded lifetime/capacity and atomic field writes, independent EN/RU Append/Insert area selector with candidate order/filter/linked indication, pause slider/draft, Cancel and one accepted manager operation plus root/point command availability (PATROL_AREA_SELECTION), owned EN/RU selected/all warning request with original question icon, read-only OK/close and session-only suppression/reset, stale-owner guards and nearest-monitor work-area placement (PATROL_WARNING_REQUESTS), owned EN/RU precommit point/route Yes/No request with independent session bit0x1000, checked-No disabling, zero-write No/close/reset, one guarded manager acceptance and retained postcommit status (PATROL_REMOVAL_REQUESTS), owned EN/RU numeric pause OK/Cancel with literal signed draft, readonly bounds, decimal-prefix conversion, nested range retry, original icons and lifetime guards (PATROL_PAUSE_REQUEST); explicit accepted-command counters/equal Undo and preserved raw no-op semantics (source missing-pause-Undo difference documented in PATROL_EDITOR_CALLERS); source-order hero-gate/area/event generation with keeper counts, project templates, read-only preview and confirmed atomic undo; source region labels/hidden/text colours, safe ordered gate expansion, integer paths and stored keeper camera icons/overlap with six persisted overlay flags | Actual nested generator modal focus/close/template/colour acceptance (see REGION_GATE_GENERATION); actual point-picking mouse/focus/Hide/Show/Escape and region/colour/ENRU/DPI acceptance (see REGION_GEOMETRY_PICKING); physical historical patrol grouping/topology/local-list interaction and other-path warning acceptance, physical selector filtering/slider/Cancel/root-point acceptance, physical native modal request timing/suppression/focus and implicit TreeView notification count; physical removal request Yes/No/remember/focus acceptance and execution of the newly retargeted patrol WPF harness; physical numeric pause request range/clipboard/keyboard/focus/audio acceptance; historical gate generation/footprint comparison; native/editor metadata integration; source used-area removal decisions, orphan scope retention, effect references, predecessor rewiring and region removal Undo ordering; historical-GUI/game acceptance |
| Hero bands / 16 hero slots | Correct byte-51 native layout; member selector and typed editing; named insertion, guarded removal and reorder in native/all five project versions; identities/scenario links travel with members; band duplication clones owned graphs; separate visible/unique identity remapping, including band zero; actual EN/RU whole-band New/Modify parent with private nested members, Cancel, exactly one leader,16-member and24-band New limits, equal Save owning one Undo, and source selector Delete retaining literal scripts; historical1–5 band-only private conversion with source defaults, selected ordinal mapping, exact old-byte Cancel/Undo, native compiler11-field save/reopen checks; current main Debug/Release each accepted83 model/62 loadedUI groups with16 clean builds (see [acceptance](HERO_BAND_ACCEPTANCE.md)) | Current combined five-gate regression; original physical GUI/hardware/DPI/game acceptance; additional real historical binary fixtures |
| Map metadata, options, soundtrack, speech and text IDs | Original GAMEINFO editable fields; Unicode display text; atomic EN/RU level-options panel with four scalars, twelve semantic flags, six rewards/all 48 source images, unknown-bit/value retention and verified partial historical layouts; named atomic speech/text panel with all thirty fields, original tables/creature choices, Mentor bounds, None asymmetry, current/briefing/caption browsing, ANSI/raw speech compiler copying, configured/default playback dispatch and resource settings; guarded general Core Name/keeper/target drafts and native transaction-only target, correct historical defaults, chosen New/compile targets and native reload default; source non-failing EN/RU version notices in actual Test/Compile; [named parent window](MAP_GENERAL_SETTINGS_UI.md) with independent player/rule/variable/level/speech drafts, outer Cancel/one Undo, nine anchors/source fill and guarded large/crop consent; focused EN/RU native/project/historical/import/fill checks passing | Other legacy version-specific fields; actual external-player/codec/locked-file behavior; original-GUI/game acceptance |
| Player settings, AI, camera, starting resources | All non-padding GAMEPLAYER and project MAPOPTIONS fields; bilingual player tab with name/gold/control/stored-heart-health controls, all nine source profiles, separate type-only changes and confirmed complete player reset; all 67 manual AI scalars/seven groups, source choices and fifteen-room construction order with atomic Apply/Discard and stale/unknown guards; numeric/map-picked camera and inclusive AI build bounds, existing-area/selection inputs and source-default commands; historical/NPC camera guards; read-only gold/treasury/mana summary and nonblocking draft-gold warning; 84 source assignments per profile, 91 compiler mappings, original names and active keeper identities; exact padding/scenario preservation | stronger player count and heart consistency; pathological imported-order behavior; historical-GUI/game acceptance |
| Availability, pools, alliances, creature modifiers | Typed 16-byte variable records; source-derived catalogues and project array positions; bilingual seven-category matrices with 866 canonical cells/all seven players, multicell editing, native last-wins display, exact override removal, symmetric alliances and confirmed source-default reset with atomic undo; elite/self guards, live selection retention and map/shared collection separation with missing-file guards; separate complete player reset restores exact pools/statuses, including NPC zero | Historical GUI/group-selection comparison; in-game behavior; additional malformed/unknown layouts |
| Global map variable values | All 193 original variables / 20 groups with bilingual names/search, source ranges/reference defaults and unsigned 20.12 display; 189 integer batch edits, known-key native creation, duplicate-preserving updates, exact override clearing and confirmed atomic selected/full reset; map/shared variable separation including custom paths and shared-last order; verified historical arrays and missing-array upgrade guards | Historical-GUI comparison; rendered visual QA; effective game/default behavior. Whole-map reset is source new/load initialization, not an existing-map command; arbitrary unknown-key creation is not an original capability |
| Conditions and actions / script tree | 63 condition types and 40 action types with original typed argument layouts; bilingual graph tree with original sibling/child layout, roots and unlinked component heads; shared-connection selection retained through shifts and refresh; scoped insertion, branch connect/detach and property navigation; original up/down/indent/outdent moves plus chosen-connection movement; confirmed subtree deletion retaining continuation and guarding shared links; atomic link/backlink edits; ID/link editing and automatic ID-link remapping with physical duplicate/zero-sentinel guards and explicit project condition/action range repair (SCRIPT_IDENTITY_SAFETY); connected graph duplication; 14 source-derived entity bindings and bilingual named targets; named owner selection for all nine scopes with transactional binding create/open, reassign and removal; read-only missing/ambiguous area/band/gate/effect links and duplicate identities with active-slot/opaque-layout guards; read-only cycle, reachable-scope, wrong-sign-ID, backlink, missing/ambiguous-owner and duplicate-binding diagnostics; explicit Test/Compile EN/RU unused-event preview and Keep/Remove/Cancel, source default Keep, full retained/non-script guards, unchanged all-entry Keep compilation, live exact Undo/Redo and pending/stale/closed/output lifetime checks; all 129 variable/17 timer names, source all-event counters, alias-aware ordinary property choices, raw reserved/unknown retention, guarded naming drafts, DKM original table and native editor-only alias companion with exact history/save/reopen; four actual EN/RU inline alias pages with private event-and-name drafts, source operands/comparisons/modes/time, stable IDs while typing, shared-slot names, imported/future guards, one Undo and actual pending-commit/cancel checks; all 103 source-derived complete EN/RU descriptions with contextual quoted aliases, signed duration/number semantics, repeats and native gate ambiguity in actual tree/link/move choices, full five-gate acceptance | I10 semantic labels/enum/compound controls and contextual callbacks; I04 layouts closed by [matrix](I04_FORMAT_MATRIX.md); I18 remaining malformed-input diagnostics; physical historical-GUI branch/expansion-state acceptance. |
| Global terrain / room / door / trap / keeper spell / creature / creature spell definitions | Bilingual tab with 184 named source types, 262 fields and fourteen source reset groups; 42 exact fractional fields, signed attributes, percent rates and source/native numeric limits, ordinary/raw input; 66 projectile types/five compound forms, four slot mappings, named targets and inactive compiler rules; 22 creature jobs/four groups with atomic apply; implemented five-page element and nine-page creature shared parents with private navigation/reset/nested controls and common Apply/Cancel, owned Continue/Cancel job warning with defaultContinue, session-only bit0x10000/remember/reset, signed LONG total projection and owner/plan guards (CREATURE_JOBS_WARNING_REQUEST), retained source Jobs page buffer with last-row callback selection, active KillActive/Apply trace and inactive/unvisited/no-op handling (CREATURE_JOBS_CALLBACK_SCOPE); all 23 attack fields/three spell slots plus melee with 42 spell/41 contextual melee choices and atomic all-slot apply; all 22 resistance/possession fields with four source lists, exact camera units, separate group drafts and atomic apply; door/trap/keeper-spell rules with eight source lists, ten named fields, retained trigger parameter and atomic apply; 24 effects/room-object/armour fields with 371 source effect names, all room masks, retained inactive/unknown values and atomic apply; reference defaults, default-backed new-project arrays, flag choices, confirmed selected/all group reset including stored no-ops, exact undo; bit-preserving native writes, map-scoped overrides and packed container parsing; verified 44/46-row creature common prefixes at 5449/5537 bytes, opaque extension preservation, missing-species guards and available-family reset with 40 focused EN/RU UI cases | Complete source catalogue/ordinary/compound unit/range inventory acceptance; physical native job-warning callback delivery and original potential partial-commit/history comparison, physical Continue/Cancel/remember/keyboard/audio/DPI acceptance; level-modifier source acceptance; historical-GUI/game acceptance |
| Search, managers, statistics | Category/record filtering and bilingual catalogue search; original general search is a [source stub, not a missing implemented workflow](SEARCH_SOURCE_SCOPE.md); bilingual read-only map/player/band statistics, terrain counts/percentages, gold categories, treasury/mana, species counts/averages/preferred types, band release-action counts; heart/property navigation and unknown-layout notices | Original activity/time/save counters; manager completeness; historical-GUI/game acceptance |
| Minimap / zoom / fullscreen / thumbnail | Original terrain BMPs and owner-frame OR composition; all eleven source zoom presets, independent User dimensions and terrain visibility; source object glyphs, keeper cameras, region names, expanded gates and integer paths/colour; all fifteen view options with retained source bits/defaults; separate original-colour one-pixel-per-tile minimap with visible-area frame, click/drag navigation, show/hide, four size presets and proportion toggle; fullscreen/minimap right-click with original animation/thirty hover icons and guarded navigation/lifetime; thumbnail tool with all 37 visibility controls, hidden RGB/key/original background, shared proportions, original 144→128 / 96→96 preview/BMP sizes, atomic no-overwrite 24-bit export, JSON/session settings and live EN/RU controls | original-GUI/native-dialog/hardware-mouse/multimonitor/DPI/game acceptance |
| Camera path library | Original DLL/source and all 52 diagrams preserved; bilingual managed visual selector with translated descriptions, search, source ShowCell-to-param3 inversion, guarded action drafts, atomic Apply/undo and Cancel; camera action numeric parameters remain editable | Historical-GUI, native modal/hardware/DPI and in-game acceptance. The original library selects predefined paths; it does not edit trajectory files (see [source scope](CAMERA_PATH_SELECTION_SCOPE.md)) |
| English / Russian | UI, minimap labels/tooltips, all 67 manual AI labels/seven groups/31 original enum choices, typed-field and all 103 event type labels; 236 source catalogue entries across 10 lists (including both special-item versions), all 184 global-definition names, 41 creature-spell names, 66 projectile names, 22 job names, four contextual melee names, four resistance/possession lists with 84 choices and eight door/trap/keeper-spell rule lists with 51 choices; common owner/level/flag/availability values; resource dictionaries with exact key parity | Remaining enums and value catalogues; polish technical labels; native visual QA |

## Historical subsystem evidence and scope

- Source-derived C record layouts explicitly model Borland's 32-bit primitive alignment; no native-memory marshalling is used.

- Lossless native exports preserve unknown fields, record padding and opaque tails. They are byte-compared after reopening.

- Original input files and existing export directories are never overwritten.

- Dependency-free validation runner exercises geometry, schemas, signed/Unicode fields, transactions, undo/redo, new maps, every record template, count fields, and real map round trips.

- DKM compilation tests verify 49 metadata mappings, 91 player mappings, all 262 definition fields, metadata constants, non-contiguous active keeper IDs, collection categories, script links, map-scoped variables and source immutability after export/reopen. These use constructed legacy fixtures, not a real DKM corpus or an in-game test.

- Native application smoke tests check Debug and Release startup in both languages; they do **not** constitute mouse/keyboard visual QA or acceptance in Dungeon Keeper 2. Two additional in-process WPF cases construct and lay out the full MainWindow shell, verify its DrawingBrush uses a real Pen and exercise live language switching without visible windows or saved user settings. This directly covers the XAML startup failure reported by Windows.

- The separate Windows WPF integration executable constructs the actual editor control without visible windows or user-settings writes. Twelve native/project-version × language cases exercise member selection, the interpreted-value apply button, named insertion, reordering, undo, live language switching and document close. Four additional native/project × language cases verify semantic scenario labels, named targets, refreshed values/choices after identity edits and undo/redo, valid band-zero selection and document close. It verifies the selected field's actual owner/offset, not only the data model. Core band tests preserve unknown bytes and scenarios during moves, scope links during deletion, exact undo, all 16 slots and current-version compilation, and reject sparse layouts/active seventeenth end markers. These are automated control tests, not rendered-layout or in-game acceptance.

- Named value tests cover every original catalogue entry, missing ASCII names, elites, ID versus availability-variable ID versus array position, game-version-dependent special item 0x7F, unknown numeric values, project/native edits and byte-exact undo. Names are curated English/Russian UI labels, not extracted game-language resources. WPF startup also passes with blank state, Alcatraz KWD and Ancients MAP in both languages.

- DKM entity-scope tests cover creatures, doors, objects, special items, areas and bands (including active and inactive nested member identities), shared/cyclic graph cloning, independent ownership after native compilation, safe deletion, atomic undo/redo, save/reopen, unrelated overlapping player IDs, repeated-identity rejection and dangling-script rollback. Deleted owner links are detached; orphaned script records are retained until an explicit cleanup feature exists.

- Typed identity tests create all 103 events with deliberately coinciding argument values, then rename area/band/effect/project-gate identities and assert that only the 14 proven bindings and structural references change. Separate band/member unique identities update only their exact scopes; copied member scopes use the copied visible band ID. Tests cover collisions, reserved area zero, unknown-event fail-closed behavior, referenced deletion rejection, byte-exact undo/redo, current-version compilation, save/reopen and classic MAP import/export of band-zero release actions. These tests do not establish arbitrary unknown-format semantics or original/game acceptance.

- Historical upgrade tests exercise eighteen metadata/player/thing/band/V2-scenario version fixtures plus a combined metadata/V1-scenario fixture with all four old conditions and ten actions. They verify original reader defaults/fixes, separate player rows and array positions, ignored legacy values, preserved unique IDs, aliases, script IDs/chains/backlinks, compilation, save/reopen, source immutability, opaque section retention, idempotence and byte-exact undo/redo. Malformed markers/types/counts, truncated/trailing nested data, future/duplicate sections and active band end markers fail without partial mutation or history. The WPF cases also retain the second band's selected hero through stride-changing upgrade and undo/redo. These fixtures are constructed from original layouts, not a real historical binary corpus; original-GUI/game acceptance remains required.

- Scenario graph tests cover all 103 source-derived scope masks, native/project root and child insertion, chain preservation after DKM ACTION offsets shift, exact project backlinks, signed condition/action allocation and cloning, detach/reconnect without deletion, transactional undo, missing-target/cycle/scope rejection, iterative cyclic indexing, compilation and save/reopen. Four additional WPF tree cases exercise actual tree selection, filtered type choices, root/child insertion buttons, property navigation, detach/undo, live translation, cycle display and document close. Including the main-window, owner-selector, branch-operation, projection, copy-buffer, numeric-selection, transform, terrain-policy, protected-mask, terrain-placement, terrain-validation, new-map, map-region, patrol-route, hero-gate-generation, map-rule matrix, named-global-variable, map/shared-scope, player/profile, player-geometry, map-statistics, global-definition, definition-numeric, projectile, creature-job, creature-attack, resistance/possession and named-definition-rule cases there are 128 synthetic WPF cases; four optional loaded-map cases cover Alcatraz and Ancients in both languages without changing buffers (132 total). There are now 54 core regression groups. The actual definition controls also cover selected/all resets for all fourteen source groups and the reproduced no-op reset draft regression. This is not original-GUI or gameplay acceptance, nor a claim that every original tree workflow is complete.

- Tree-projection tests verify actual original sibling/child order, action leaves, empty/missing insertion contexts, shared versus cyclic markers, signed-ID orphan head ordering and read-only buffers/history. Two temporary native fixtures contain 6000 events each: a flat chain remains one level when the entry budget permits it, while a deep child chain is safely limited without inventing orphans. Four additional WPF EN/RU × native/project cases assert actual hierarchy and shared-connection selection through live translation, ACTION offset shifts, undo/redo and root-link compaction. An unresolvable ambiguous parent clears selection instead of retaining a stale node or selecting another connection. See [projection details](SCRIPT_TREE_PROJECTION.md). These are automated hierarchy checks, not visual/game acceptance.

- Branch-operation tests follow the original up/down/indent/outdent ordering, move an event with nested children while retaining its source continuation and destination chain, and delete exactly its AND subtree. They cover cross-owner movement, unchanged surviving payloads and reserved action bytes, exact undo/redo/backlinks, no-op and boundary behavior, shared root/child guards, cyclic/dangling rejection, failed-scope rollback, compilation and save/reopen. Four WPF cases exercise all direction buttons, destination choices, explicit deletion confirmation, undo, live localization and document close. See [branch operation details](SCRIPT_BRANCH_EDITING.md). Historical-GUI and in-game acceptance remain pending.

- Owner-binding tests exercise all nine scopes in native/project formats, seven actual player IDs, band zero, active-only member choices, ID allocation from zero, missing-target rollback, idempotent lookups, exact undo/redo, guarded reassignment, event retention on removal, save/reopen and compilation retaining each exact target/owner. V2 link fields are preserved where supported and door/object binding is refused without partial changes until upgrade. Band versions 2–5 retain member scopes across upgrades; V1 explicitly requires upgrade because it has no unique-ID fields. Four WPF cases test the actual scope/owner pickers and buttons in EN/RU for both formats: all nine choices, selected physical ownership, member-name search, reassignment, removal/undo, live translation and closed-document cleanup. Diagnostics flag duplicate bindings and roots with no resolved physical owner without rewriting buffers. Historical-GUI/game acceptance remains pending.

- Read-only scenario diagnostics tests cover cycles in linked and unlinked components, shared acyclic branches, original scope restrictions, unverified types/combined scopes, wrong-sign project IDs and stale backlinks. They assert unchanged bytes/history and no change notifications. Native reserved ACTION.ChildAnd bytes are ignored as links and preserved during ID remapping, graph duplication and selection copy/paste; they do not prevent deletion of an otherwise unreferenced event. Project ID remapping retains exact backlinks. These checks do not infer compatibility of unknown layouts or automatically repair imported data.

- DKZ tests verify original KMAP/KTGS compatibility, native/project exact-record extension round trips, scope links, external scripted coordinates, atomic paste undo, cross-format script-loss refusal, signed union values, malformed bounds and no-overwrite file export. The historical executable's GUI has not been used to verify these files.

- Selection-buffer tests additionally cover native/project edge clipping, nonzero crop origins, retaining only exact physical owner links and their reachable graphs, reserved native ACTION bytes, orphan owner-ID reservation, incompatible project scenario strides and V1-target rollback, save/reopen and compilation. Four WPF cases verify buffer persistence across close/open, direct DKZ insertion without replacing it, failed-cut retention, cross-format refusal and tile-only conversion. Four more WPF cases verify actual numeric-selection controls, positive dimensions, full-map and last-tile selection, invalid/overflowing input, live EN/RU switching, cancellation without map edits/history, one-tile defaults and selection cleanup after resize undo. Original copy/paste uses internal `MapCopy`, not the Windows clipboard; OS clipboard integration was incorrectly listed as an original parity requirement and is removed. See [selection details](SELECTION_BUFFER.md).

- Transform tests compare every byte against independently calculated native/project coordinates and tile permutations for clockwise/counterclockwise rotation and both reflections. They cover seven native placed categories, project physical things, unrelated area/band/gate records, IDs/scopes/backlinks/reserved bytes, local and external scenario targets, inverse/four-turn restoration, one-transaction undo/redo, shape rejection, save/reopen and compilation. Non-square buffer rotations, DKCP import/export and cropping retain external targets even when their numeric position enters the transformed bounds; formerly local targets excluded by a crop become external. V1/V2 DKCP remain readable; missing/invalid/truncated provenance and future versions fail. Four WPF cases exercise actual commands, inline localized validation, unchanged buffer on rejection, explicit rotated-copy and document cleanup. See [transform details](SELECTION_TRANSFORMS.md). Historical/game acceptance remains pending.

- Editor-terrain tests cover protected-edge paint/fill/copy/cut, original source-catalogue world/hero-gate owners and variants, preserved owned rooms/paths, selection-limited flood, failing-stroke rollback without consuming redo, all seven special cut terrain IDs, every map background preset, unsupported preset rejection, border objects/scopes, exact undo/redo, reserved-byte preservation, save/reopen and compilation. Four WPF cases exercise actual palette/canvas methods and copy/cut commands in both languages and formats. The full FillID preset is decoded correctly rather than reproducing the original cut routine's low-byte/shifted-comparison bug. See [terrain-policy evidence](EDITOR_TERRAIN_RULES.md). Raw lossless APIs remain distinct from original-editor UI policy; full gameplay and historical acceptance remain incomplete.

- Text import restores original tile/name catalogues, 84 default AI assignments, map-variable defaults, all six ASCII actions and three conditions, availability and creature pools. Six local text files (five distinct maps, including Ancients: 434 objects, 8 bands, 14 conditions, 24 actions) pass import, DKM byte-preserving record round trips and native compilation/reopening. Synthetic tests cover malformed input, scope chains, gate-to-area translation and encoding rejection. Heart-center lines remain informational hints, as in the original importer; actual heart tiles govern compilation. These checks do not establish original-GUI or in-game acceptance.

- Text export runs on all 57 native maps and the six text projects: the classic MAP parses again, text-compatible project scenarios retain their record counts, and the mandatory editable snapshots retain complete binary data. Synthetic tests exercise Cyrillic-name fallback, unrepresentable object IDs, bridge variants, native availability/pool projection, supported script conversion, zero-action conditions, cycles, unsupported conditions, source immutability and no-overwrite behavior. The classic MAP is explicitly a limited projection, never the full editable source; full data are kept in a separate copy. This does not claim that the historical ASCII grammar can encode arbitrary native data.

- Protected-selection tests independently verify masks across crop/all four transforms, including nonsquare buffers; protected destination preservation; neutral-solid source fallback and destination-object removal; filtering rejected owner graphs; raw API distinction; no-op/failed paste retaining redo; exact undo/redo, save/reopen, compilation and immutable source/buffers. DKCP v3 validates a separate mask, historical canonical boundary projection and raw masked-tile snapshots. V1/V2 and original DKZ import without inferred locks; malformed/truncated flags, inconsistent projection and unknown tails fail. Four WPF EN/RU × native/project cases exercise actual masked DKZ buffer reload/paste, rotated-copy and all four in-place transforms with original all-object movement before tile-mask checks. Historical direct-selection-export equivalence, original-GUI and gameplay acceptance remain pending.

- Terrain-placement fixtures cover all eight constrained heart/portal/gate terrain/variant entries and six shape modes, fixed/minimum/unbounded dimensions, reversed gestures, initial border rejection versus expanded border-skipping writes, modern selection clipping, normal tools, no-op/failed transaction rollback, duplicate-heart editability, exact unrelated records, save/reopen and compilation. Painting does not invent a separate gate-region identity; explicit gate records compile with the placed oriented variants. Four WPF EN/RU × native/project cases exercise actual palette/tool controls and canvas gesture/fill methods, dimension/preview/error state, sampled owner/orientation retention through palette callbacks, live language switching, undo/redo, unchanged object/scenario/copy buffers and closed-document cleanup. See [placement details](EDITOR_TERRAIN_PLACEMENT.md). Original-GUI/game and GDI-shape acceptance remain pending.

- Terrain diagnostics fixtures cover all nine original special terrain/variant entries, dimensions and unlimited gates, row-size versus nonrectangle precedence, minimum-width diagnostic regions, duplicate-heart exceptions for Heroes/Neutral, consecutive keeper positions and exact read-only buffer/history behavior. Two hundred deterministic randomized native/project maps match an independent direct-row reference; a 1024 × 1024 gate region is handled as one rectangle. Four WPF main-window cases verify actual result binding, error colours, live EN/RU summaries and undo/redo refresh. Full menu validation and compilation include these checks; editable saving and structural/lossless APIs remain available for unfinished maps. The local corpus (57 native maps plus six text inputs) retains identical results after export/reopening/compilation: 55 files have no terrain reports and eight have 13 size warnings in total; none has a terrain error. This is not an in-game acceptance claim. See [diagnostic rules and scope](TERRAIN_LAYOUT_DIAGNOSTICS.md).

New-map fixtures additionally cover 276 background/version/format/blank-or-starter combinations, all keeper counts, original metadata/player arrays and camera sentinels, native Unicode versus historical project text, normal compilation versus factory-only keeper inclusion, exact first-edit undo and selected full-buffer save/reopen. Four WPF EN/RU × native/project cases exercise actual controls, starter capacity, large acknowledgement, overflow/name validation, live translation, canvas and a synthetic unsaved-shell metadata fixture (without modal New/Discard automation). The native corpus verifies exact game-definition reuse and source immutability. Original blank behavior remains default; the optional starter is not a complete playable level. See [new-map evidence and remaining integration](NEW_MAP_CREATION.md).

Do not mark the parity goal complete while any required editing row above remains incomplete.

Global-variable fixtures cover all 193 source keys / 20 groups, both endpoints and out-of-range failures for all 189 editable integers, signed/unsigned bit patterns, four display-only 20.12 entries, the source-default 300/manual-maximum 255 anomaly, exact project batch/reset bytes, native record payloads, duplicate last-wins/update/clear, reserved and unknown key guards, unused-byte/unknown-slot retention, shared-game versus map collection separation, one-transaction undo/redo/no-op, source compiler omissions and save/reopen/re-export. Metadata versions 4–7 expose their verified arrays; versions 1–3 require explicit upgrade (default introduction and exact undo tested); future versions remain read-only. Four WPF cases verify actual EN/RU × native/project table bindings, selection, filters/search, apply/reset/clear, confirmations, live translation/input retention and map-selection/copy-buffer invariants. Read-only variable projections are compared across the native/text corpus. Historical GUI, rendered visual QA and effective game/default behavior remain unverified. See [global map variables](MAP_GLOBAL_VARIABLES.md).

Map-rule matrix tests cover all 866 projected cells and batch-edit all 807 editable targets in native/project formats. They independently encode expected project bytes and check all native/compiled catalogue-player-parameter assignments, corrected original availability states (1 unavailable, 4 locked), source presets, NPC availability retention, elite/self guards, atomic preflight rejection, native last-wins/reversed-duplicate handling, exact clear-all override removal, preserved unknown records, byte-exact undo/redo/no-op history, historical verified fields, unknown-layout refusal and lossless save/reopen/re-export. Four WPF cases test actual bound table text, seven category dimensions, multicell edits, confirmation/reset/clear controls, numeric/sentinel validation, translation/selection/input retention and untouched map selections/copy buffers. Corpus tests compare matrix projections before/after reopening without changing input buffers. See [matrix source rules and remaining acceptance](MAP_RULE_MATRICES.md). Full player reset is covered separately below; global-definition resets are covered separately below; historical GUI/gameplay remain open.

An additional map/shared-scope regression reproduces the former matrix defect before its fix, with every editable canonical rule/integer key in a separate shared fixture. Both reference orders, factory-shared-last order, custom non-Maps paths, missing map files (including diagonal alliance reset), exact shared/unrelated bytes, undo/redo and export/reopening are checked. Two EN/RU WPF cases exercise actual bound values, edit/clear/reset buttons, live translation and all-category missing-file guards. The source `Game_ExtractVariables` reads only map Variables.kld. `Properties_MapResetOptions` is called by new/load initialization, not an existing-map reset command; the ledger no longer requires that invented destructive workflow. This does not remove the real player-default button or global-definition reset requirements; see [source scope evidence](MAP_RULE_MATRICES.md#reset-scope-clarification).

Player fixtures independently read all nine source profile branches (84 assignments each), apply every profile to all seven players in both formats and compare independently encoded bytes. They distinguish type-only edits from profile defaults, verify complete per-player/seven-player reset, exact NPC/keeper pools/statuses, source names, CP1252, resource limits, unrelated player/graph/padding retention, no-op/undo/redo, duplicate/noncontiguous IDs, last-heart health, shared data, missing rule collections, historical V1/V2/future guards, explicit upgrades and saved/compiled copies. Four WPF EN/RU × native/project cases exercise actual controls, all nine choices/buttons, confirmation invalidation, inline errors, live drafts, property navigation, historical guards and document close. Real native/text corpus projections retain player values and immutable input buffers. Offscreen tab rendering exposed inherited light text on light pickers/headers; shared styles now provide dark picker/header text while ordinary text inherits its container foreground, also avoiding light text in light property tables. The new player tab's EN/RU rendered images are inspected separately from historical-GUI/game acceptance. See [player source evidence and remaining work](PLAYER_SETTINGS.md).

Player-geometry fixtures cover five keeper cameras, NPC camera guards and all seven build rows: inclusive boundary/interior/full-map/one-tile choices, independently encoded bytes and nonzero padding, source defaults and first-heart/centre lookup, malformed imported values and explicit repair, invalid/overflowing input, stale row payloads, native duplicate/noncontiguous IDs, old build aliases and ignored camera fields, upgrades/future guards, tiny-map defaults, no-op/atomic notifications, undo/redo, save/reopen and compilation. Four additional WPF EN/RU × native/project cases exercise actual numeric/default/area/selection buttons and canvas completion, reversed rectangles, cancellation, draft/player/external-edit invalidation, live translated hints and closed-document cleanup without changing ordinary selection/copy buffers. Corpus geometry projections remain identical after reopening. Historical-GUI, camera-marker display and gameplay acceptance remain open; see [coordinate source evidence](PLAYER_GEOMETRY.md).

Hero-gate manager now uses the explicit source allocation counter caller with one Undo; all400 cases verify gate/area/logical-scope/event totals, absent/empty/rooted bindings, no-work, rollback and counter Undo/Redo. See [counter scope](HERO_GATE_GENERATION_COUNTERS.md). Private nested generation now has400 model cases and source counter/topology/Cancel/Apply guards, with actual GUI/game acceptance remaining open (see [private child](REGION_GATE_GENERATION.md)). Hero-gate generation fixtures independently cover all 40 valid flag combinations, all five keeper counts and both formats (400 cases): four source gate types, greedy scan footprints, blocked perimeter rules, area matching/fallback, IDs reserved by malformed records, native ordinals excluding hearts, exact claim/destruction arguments and project backlinks. They verify templates/macros/COLORREF/encoding, event-only repair, appending existing scenarios, no-op reruns, stale/foreign plans, exhaustion/orphan guards and rollback after primitive additions, byte-exact undo/redo, save/reopen/re-export and native compilation. Four WPF cases exercise actual EN/RU × native/project controls, option dependencies, read-only previews, confirmation, invalid input, native template limitations, live translation, invalidation, application and retained selection/copy buffers. See [source evidence and deliberate fixes](HERO_GATE_GENERATION.md). Historical-GUI/game gate behavior and native/editor metadata integration remain pending.

Patrol fixtures cover all source append/insert/removal/pause and quick-link operations in both formats, including separate closing occurrences, self/full/partial loops, shared-tail joins and source-style effects on other incoming paths. An exhaustive 552-case removal sweep covers every occurrence in routes of lengths 1–8 with open endings and every cycle entry. Tests compare independently encoded expected bytes, exact undo/redo, no-op/failure history, stale/foreign/ambiguous identities, missing endpoints and save/reopen/compilation. Four WPF cases verify actual draft/point controls, confirmations, navigation, quick map drawing, invalid input, live translation, display toggling and offscreen link rendering, retaining areas/scenarios/copy buffers. Source grouping is now independently compared over all15,404 small graph/file-order cases in both formats, nine hand cases,255 IDs and independently packed actual Level10; no bytes/counters/history are written. Native/project manager checks now verify retained local occurrence lists, linked append followed by another append, shared-list warnings and Clear preserving the final outgoing chain, empty roots/navigation counters, atomic literal fields, owner/stale/structural/closed/capacity/observer guards and Save/reopen/game compilation. Physical original-GUI root order/local-list interaction, physical native modal warning/request timing/suppression/focus and implicit tree selection notification count, physical source removal-confirmation request, numeric pause range/clipboard/focus/audio acceptance and execution of the newly retargeted patrol WPF harness, physical candidate filtering/slider/Cancel/root-point controls, pointer behavior and gameplay remain unverified. The owned candidate selector now has64 independent source/native/project mask cases plus pause, cancel, acceptance and lifetime model evidence; these are not actual UI execution. Owned warning requests now have80 native/project flag/reply cases, full read-only state checks, shared-route selection/all predicates, suppression session/restart/reset and stale/foreign/closed guards, six independent cursor-work-area cases, unchanged original question icon embedding and static App integration. Actual warning UI and original implicit tree notification timing remain unexecuted. Owned point/route removal now has160 native/project default/remember/reply/flag cases, read-only cancellation, one exact packed manager command/Undo, retained empty roots/final chains, private/raw/revision/structure/foreign/nested/closed guards, observer-failure ownership and Save/reopen/compile evidence. The actual Yes/No window and source keyboard notifications remain unexecuted. Older executed WPF cases do not prove these new sessions. The current harness has been retargeted from the old inline/raw route controls and compiled, but has not been executed (PATROL_UI_HARNESS_SCOPE). See [patrol routes](PATROL_ROUTES.md), [source grouping](PATROL_SOURCE_GROUPING.md) and [manager session](PATROL_MANAGER_SESSION.md).

Map-region fixtures cover exact coordinate-only changes in both formats, retained patrol/scenario/effect properties, independent category IDs/duplication and exhaustion, malformed read-only geometry and explicit repair, stale/foreign/unknown-kind rejection, single-tile effect deltas, border bounds, undo/redo and serialized save/reopen/compilation. Shared gate orientation tests include all source solid-neighbour IDs, including boundary 254. Four WPF cases exercise actual region controls, shared canvas release operations, live EN/RU translation, overlap hit cycling, canceled/stale choices and unchanged terrain/copy buffers. This geometry-only group does not establish historical/game acceptance; generation has a separately documented test group. See [map-region details](MAP_REGION_EDITING.md).

Owned numeric pause is implemented and statically wired;26 literal parser cases
in native/project formats, range retry, zero-write Cancel, exact packed wait
fields24/56, equal/changed oneUndo/count, lifetime/observer guards and save/reopen/
compile are model-tested. The negative signed-project wait getter is repaired;
raw mutation method bodies are unchanged. Full regression adds one group while
retaining all1808 earlier passes and the literal ordered63 failures. This does
not close physical numeric request, original GUI, clipboard-edge conversion,
system sound, DPI, tree-notification or gameplay gates. See
[numeric pause scope](PATROL_PAUSE_REQUEST.md).

## Global catalogue images — 13 October 2026

Current code restores original 32×32 images for all184 source definition rows
in the general catalogue and both shared parents. Projectile forms2/3 use
GCreatures images; form4 uses all116 Objects images in trap, keeper-spell
(both slots) and creature-spell options. Numeric levels and imported unknown
IDs retain their existing behavior. Source bitmap IDs are distinct from game
IDs/indices; verified RC aliases share one physical resource.43 missing resources
are embedded from protected Source files. No bitmap editing or map format change.

Independent source capture/Core catalogue/compiled PE checks cover all300 bitmap
identities and bytes. All41 Release projects and focused checks pass; fresh
official export remains exactly seven identical test-map files. Full terminal
55746 retains1811 passes and63 failures, entire order compared with prior runs
and the original94. Published code and unified source/runtime packages refreshed.
These checks execute neither App/WPF nor bitmap rendering. Visual scaling,
selection, DPI and EN/RU physical navigation remain unproved; the other23 rows
and completion gates above are unchanged. See
[original definition images](GLOBAL_DEFINITION_IMAGES.md).

## Scenario dependent inputs checkpoint — 13 October

The original terrain-owner selection callbacks are now present on both
TRIGGER_AREACONTENT and ACTION_CHANGESQUARE. Source IDs1..7/31 forceNeutral2,
35 forcesHeroes1; otherknown terrain retains an editable private owner. Opening
imported data preserves its operands. ACTION_SETVARIABLE initializes the hidden
secondary selector to0 in literal modes, preserving numeric bits and variable
mode IDs. Source capture and896+84 native/project model cases verify these
behaviors, byte preservation, atomic history and guards. Named/alias/spatial,
all41 build, official export and publish exit0; the single post-block full corpus
is1811PASS/63 unchanged ordered failures, with original94 comparison retained.
WPF harnesses compiled only; original notification timing and actual EN/RU UI/
game acceptance remain open. Details: [dependent inputs](SCENARIO_DEPENDENT_INPUTS.md).
This closes two concrete gaps, not the remaining contextual/caller/history audit
or any full physical completion gate; all23 capability rows remain required.

## Scenario page hints checkpoint — 13 October

Three band-population condition labels now follow the original selection
callback and first-inactive count, including band0 and native species/behavior
sentinels. Malformed/duplicate identities never guess a population. Read-only
42-case model plus lifetime/opaque-marker guards pass; existing named/terrain
and alias groups pass on current DLLs. Source alias number/time text limits4/2
are restored without clipping programmatic imports. All41 build and publish
exit0, WPF hint smoke compiled only. No new full corpus or compiler round-trip
is claimed for this read-only/UI block; previous full proof remains bound to
its previous Core. Details: [page hints](SCENARIO_PAGE_HINTS.md).
AREA size/perimeter labels need physical selected-owner propagation across
shared graphs and wizard navigation. Remaining caller/history/source and all
physical gates stay required; all23 capability rows and whole scope retained.

## Scenario AREA information checkpoint — 13 October

Three source AREA information pages now show inclusive geometry/total, with
CLAIMAREA's original width1perimeter rule. Physical selected root context is
propagated through shared/nested tree nodes, ordinary page and private insertion/
editing wizard; no arbitrary first owner. Read-only explicit context choice,
Back/Next/ENRU retention and stale protection are present. Source capture,
30geometry cases and shared/private/lifetime models, existing103-type creation/
editing suites and all41build/publish pass. WPF smoke compiled only. No new full
corpus/official roundtrip claim for this read-only/Appselection block; previous
full remains tied to previousCore. See [AREA hints](SCENARIO_AREA_HINTS.md).
Physical tree selection/navigation and remaining caller/selection/history and
all completion gates stay open; every original capability row remains required.

## Scenario prerequisite checkpoint — 13 October

Four band conditions now enforce original absent-band prerequisites through
shared parameter-page Apply, including private creation/editing. Source capture
covers37condition/33action parameter-check cases and12explicit collection gates.
All103types x16masks x2formats3296read-only comparisons and private guards pass;
band0 retained. All41build/source/focus/creation/existing/official/publish0.
One new current full corpus1811PASS/63unchanged raw ordered failures, with full
original94 comparison; it integrates the current band/AREA information models.
WPF smoke compiled only; actual GUI callbacks/layout/game remain unproved.
See [prerequisites](SCENARIO_PREREQUISITES.md) for notification route inventory
and its physical limits. Next drawing/selection/caller/history gates and every
original capability row remain required; whole goal active.

## Pen source audit — 14 October

Five original files now bind the Down/Move/Up/cursor/paint/protection/Undo route,
including0,0sentinel, acceptedcall counters, no addedrelease segment and retained
usage statistics. All39selectable cursor rows and78native/project route/reservation
cases match current Core; existinglive-pen frame/history/lifecycle models pass.
Product/App unchanged, so lastcurrentCore full1811/63orderedproof retained
without rerun. Canonical [drawinghistory](EDITOR_DRAWING_HISTORY.md) replaces
obsolete candidate/recovery statuses with detailed current code and manualsteps.
Brush source-size/live-stroke scope now audited; physical capture/timing/input,
PRNG/hardware/game and other drawing/selection/caller gates remain open.

## Direct selection writer — 14 October

Direct selection export now follows source all-creatures/first-noncreature
per-cell writer semantics, distinct from buffer export. DKCP6 keeps all raw
records/graphs while KTGS projects source order/first members; buffer stillv5,
older1–5reads retained. Actual header byte lengths correct the original duplicate
count defect; emptyKTGS protects oldreader before extension. Twoformat models
verify12raw/8KTGS,literalcoordinates/pointers, graphs,tamper/empty/atomiclocked
save and lossless transformedbuffer. All41/selection/archive/official/publish0;
onecurrentfull1811/63rawexactorderedmatch,original94fullcomparison retained.
ActualGUI/oldreader/paste/game remain pending; globalbands/areas not added.
See [direct writer](DIRECT_SELECTION_EXPORT.md). All23capabilityrows/remaining
selection/caller/history/physical gates remain required.

## Selection acceptance callers — 14 October

Cut/Paste App routes now implement source1acceptance+each physicaltargetremoval,
insertedobjects0extra, unchangedactivity andoneparentUndo evenequal/protected.
Genericrawselection methods retained.32native/project/all5category combos plus
wrap/history0/invalidnestedformat/clipped insertion guards pass; source3files,
all41/Validation/Windowsbuild/focus/official/publish0. Onecurrentfull1811/63
rawexactorderedmatch+fulloriginal94comparison, no newfailures. WPF onlycompiled.
See [selection callers](EDITOR_SELECTION_CALLERS.md). Equal Rotate/Reflect
source acceptance still needs expliciteditorcaller; physicalcopyconsent/menu/
history/game andothernonpainting/realcorpus/fullgates remainrequired.23rowsintact.


## Предыдущая сборка: TransformCallers-20261014 (счётчик уточнён ниже)

14 октября 2026. Повороты и отражения в рабочем окне используют
`TransformEditorSelection`. Как в исходном Select_Rotate/Select_Symmetry,
принятая команда создаёт один шаг Undo и увеличивает счётчик изменений на 1,
даже если квадрат однообразный и байты не меняются. Перемещённые объекты
дополнительного увеличения не дают; статистика рисования не меняется.
Базовый TransformSelection сохранил прежнее поведение при равном результате.
Ограничения размеров, защищённые клетки, координаты, идентификаторы и связи
сценариев используют прежний алгоритм. Подробности и ручные шаги:
[EDITOR_TRANSFORM_CALLERS](EDITOR_TRANSFORM_CALLERS.md).

Все 41 проекта собраны: код 0, без ошибок и предупреждений. Проверка исходника,
проверки выделения, свежий семифайловый комплект и публикация завершены с кодом 0.
Новые модели проверяют 32 сочетания: два формата, четыре преобразования,
равный/неравный результат и край/внутренняя часть карты; отдельно проверены
отказы, квадрат 1×1, переполнение счётчика и нулевая глубина истории.
Дополненные проверки команд WPF скомпилированы; окна не запускались.

Полный запуск 15564 завершён с кодом 1: **1811 PASS / 63 отказа**.
Весь упорядоченный список результатов и входных файлов совпал с предыдущим
SelectionCallers. Новых отказов и потерянных PASS нет. Исходные 94 отказа
сопоставлены полностью: 31 устранён ранее, одна известная диагностика XXLevel14
изменилась ранее. 63 отказа остаются видимыми; установленный корпус не исправлялся.
Повторный полный прогон для этого блока не нужен.

Core SHA256: `fa03403be71f072d2bcb78135aa348046fae1b56cedc3b64a5cd713100faf810`.
Доказательства: `artifacts/Evidence/TransformCallers-20261014`.
Проверенные и опубликованные DLL совпадают. Пакеты исходников и приложения
обновляются из единого проекта; Source/Binaries, карты, fixtures и семь файлов
TestMap сохраняются. EN/RU: 2540 совпадающих ключей, прежние значения сохранены.

Ранее отмеченное расхождение равных Rotate/Reflect исправлено этим блоком.
До состояния «только ручные проверки» остаются контекстные/составные обработчики,
остальные маршруты счётчиков и истории, семантика компиляции и исторические
форматы. Все 23 строки FEATURE_PARITY и физические проверки сохраняются.
Общая цель ACTIVE. Окна редактора, игра, звук и системные диалоги не запускались.


## Текущая сборка: TransformRemoval-20261014

14 октября 2026. Исправлены два составных маршрута, использующих
Things_RemoveAtMapPosEx: поворот/отражение и кисть объектов. Предыдущая сверка
TransformCallers учитывала только верхнюю функцию и ошибочно утверждала,
что перемещаемые объекты добавляют 0 увеличений. Правило — **1 + N**, включая
объекты у защищённого края при преобразовании. Повторное размещение добавляет 0.
Одна принятая команда сохраняет один общий шаг Undo.

Кисть карты и окно одиночного размещения вызывают ApplyEditorObjectPainting:
1 + число удалений при создании, +1 при обновлении, включая равные байты,
и один UsageOfPaintThings. Базовые API сохранили прежние правила. Счётчик
откатывается с Undo; статистика остаётся накопительной.

Все 41 проекта собраны без ошибок и предупреждений. Окончательные сборки
Validation и WindowsSmoke, проверки выделения/кисти, захват трёх исходных
файлов, свежий семифайловый комплект и публикация завершены с кодом 0.
Дополнительно проверены 80 сочетаний количества объектов при преобразовании
и 58 случаев кисти; прежние 32 геометрических случая также сохранены.
Исходная ошибка новых тестовых данных (повторное пакетное размещение заменяло
объекты вместо создания скопления) исправлена явным дублированием. Журнал
painting-focus-fixture-failure сохранён; проверки продукта не ослаблялись.

Окончательный полный запуск 24412: terminal exit 1, **1811 PASS / 63 отказа**.
Полные упорядоченные списки PASS, отказов и входов совпали с TransformCallers.
Исходные 94 отказа сопоставлены полностью; новых отказов и потерянных PASS нет.
Промежуточный запуск 41711 осознанно остановлен после расширения блока на
кисть объектов. Его журналы сохранены с intermediate-префиксом и не считаются
доказательством окончательной приёмки. Повторять зелёные проверки этого блока
без новых правок или обнаруженных ошибок не требуется.

Core SHA256: `524b002147c056800e9d99954ac61625187151211cd3132c8da0c764ba2129ed`.
Доказательства: `artifacts/Evidence/TransformRemoval-20261014`.
Проверенные и опубликованные DLL совпадают; пакеты обновлены из единого
основного проекта. Source/Binaries, установленный корпус, fixtures, семь файлов
TestMap и все 2540 прежних значений EN/RU сохранены.

Подробности и ручные шаги: [преобразования](EDITOR_TRANSFORM_CALLERS.md),
[кисть объектов](EDITOR_OBJECT_PAINTING_CALLERS.md).
Редактор, WPF, игра, звук и системные диалоги не запускались.
Следующая работа — оставшиеся составные обработчики/история и исторические
форматы по полному FEATURE_PARITY. Все 23 строки и физические ворота сохраняются;
цель ACTIVE, состояние «остались только ручные» ещё не подтверждено.


## Текущая сборка: ProjectileAcceptance-20261015

15 октября 2026. Специализированные параметры снарядов теперь повторяют
исходные выборы: неизвестная цель → первый элемент каталога, неверный уровень
→ 1, принятие предмета ID≠1 → второй параметр0. Открытие и отмена не записывают
подстановки. Дочерние окна возвращают частный черновик до общего Apply;
встроенная панель использует явный SetEditorGlobalProjectile. Базовый API
сохраняет прежние правила удержания импортированных значений.

Охвачены ловушки, оба слота хранителя и заклинания существ. 1320 сочетаний
инициализации всех66 типов и96 native/project/four-slot принятия проходят;
полные байты, история, счётчики и Undo/Redo сверены. Захват исходных функций
и каталогов, all41 build без предупреждений/ошибок, окончательный WindowsSmoke
build, focused projectiles, свежий семифайловый комплект и publish: terminal0.
Дополненные проверки дочерних окон и встроенной панели скомпилированы;
окна, WPF/renderer, игра и официальный редактор не запускались.

Один полный запуск50749 после блока: terminal exit1, **1811PASS /63 отказа**.
Входы и полные ordered PASS/failure arrays совпали с TransformRemoval.
Исходные94 отказа сопоставлены полностью:31 устранён ранее, одна известная
диагностика XXLevel14 изменена ранее. Новых отказов/потерянных PASS нет.
Установленный корпус не исправлялся. Зелёные проверки этого блока не повторять
без новых изменений или обнаруженной ошибки.

Core SHA256: `a752d115ac092b74d9914c1423e9718ef1b089987ce94242358dd5cf43d0e307`.
Доказательства: `artifacts/Evidence/ProjectileAcceptance-20261015`.
Проверенные/published DLL совпадают, пакеты обновлены изcanonical41project.
SourceBinaries1006/maps4453/fixtures/TestMap7 и2540ENRUliteral values сохранены.
Подробная документация/ручные шаги: [PROJECTILE_EDITOR_ACCEPTANCE](PROJECTILE_EDITOR_ACCEPTANCE.md).

Остальные составные callbacks, caller/history differences, compiler/historical
семантика и физическая EN/RU GUI/audio/systemdialog/hardware/DPI/game приёмка
остаётся. Все23 capability rows/fullgates сохраняются; цельACTIVE, переход
к состоянию «только ручные проверки» ещё не подтверждён.

Следующий конкретный пробел подтверждён чтением: Properties_MapCreaturesChanged
показывает четыре средних только по ненулевым значениям выбранных неэлитных
видов; Properties_MapCreaturesSetParam пишет выбранный параметр выбранным видам.
Текущая RuleTable не имеет обработчика SelectedCellsChanged и отдельного
четырёхпараметрического выбора. Сериализация/диапазоны/elite guards уже есть;
требуется частный контекст выбора/ввода и compose с общим MapProperties Apply/
Cancel. Исходные методы, current Rules/XAML и хеши сохранены в
next-creature-modifier-scope.json этого evidence. Этот пробел пока не исправлен.


## Текущая сборка: CreatureModifiers-20261015

15 октября 2026. Выявленный пробел групповых модификаторов закрыт новым
частным контекстом и дочерним окном общих свойств карты: выбор нескольких
видов, четыре независимых средних только по ненулевым значениям, исключение
элитных строк17–29, per-parameter запись выбранным видам. Дочерний Cancel и
общий Cancel сохраняют живую карту; дочерний OK переносит в WorkingDocument,
общий Apply объединяет изменения в один исходный счётчик/Undo без resize.
Pending/stale/closed/foreign/nested guards и ревизия ребёнка при consent сохранены.

Все41 проекта собраны0 без предупреждений/ошибок. Окончательный WindowsSmoke
build пересобрал App после правки выбора и защиты ввода при consent, завершён0.
Source capture четырёх функций, focused parent/map-properties и свежий
семифайловый комплект:0. Новый Core проверяет32 буквальных сочетания выбора,
все строки/13 элитных, четыре поля, частную/общую отмену и полный Undo/Redo.
Новые WPF EN/RU проверки реальных классов подготовлены иcompiled only.
Добавлено8 ключей EN/RU; прежние2540 literal values сохранены, всего2548 ключей.

Один окончательный full42074: terminal exit1, **1811PASS /63 отказа**.
Входы и весь ordered PASS/failure arrays совпали с ProjectileAcceptance;
исходные94 сопоставлены полностью. Новых отказов и потерянных PASS нет.
Установленные карты не исправлялись. Повторять проверки без новых правок
или обнаруженной ошибки не требуется.

Core SHA256: `b08a1f54fccd56f65b34d202a20df1d64e797abb601c849b9a2961f613bb96c8`.
Evidence: `artifacts/Evidence/CreatureModifiers-20261015`.
Проверенные/published DLL совпадают; source/runtime пакеты обновлены из
единого41project без восстановления прежних версий. SourceBinaries1006,
maps4453, fixtures и TestMap ровно7файлов/bytes сохранены.
Подробное описание/ручные шаги: [MAP_CREATURE_MODIFIERS](MAP_CREATURE_MODIFIERS.md).

Окна, WPF/renderer, игра, официальный редактор, звук и системные диалоги
не запускались. Остальные составные callbacks/история, compiler/historical
семантика и физическая приёмка EN/RU GUI/audio/systemdialog/hardware/DPI/game
остаются; все23 строки FEATURE_PARITY/fullgates сохраняются. GoalACTIVE.


## Текущая сборка: ScriptIdentities-20261015

15 октября 2026. Ручное изменение сценарного ID использует общий remapper:
нельзя перенести ссылки от дублирующегося старого ID к произвольной записи;
однозначное событие обновляет все физические связи, в том числе из записей,
не вошедших в основной dictionary index из-за посторонних дубликатов.
Нулевой ID исправляется без переноса пустых связей. Project условия требуют
1–32767, действия32768–65535 для новых ID; неизменённые импортированные IDs
и native reserved ACTION bytes сохраняются. Никакие карты не ремонтируются
при загрузке, диагностике или этом этапе.

Все41 build0 без ошибок/предупреждений; окончательный Validation build0,
focused identities/diagnostics/event-layout groups0, official seven0, publish0.
Новый malformed fixture исправлен по исходному NODE ABI12 вместо8; исходный
failed journal сохранён. WPF identity projection использует ID своей категории;
собран, но не запускался. Общий full64684: terminal exit1, **1811PASS/63 отказа**.
Весь ordered PASS/failure arrays и Inputs совпали с CreatureModifiers;
исходные94 сопоставлены полностью, новых failures/lostPASS нет.

Read-only аудит63 отказов дал27карт:9 missing tile/27checks,6 truncatedVariables/
24checks,8 noncanonicalIDs/8checks,4 danglinglinks/4checks. Variables действительно
не имеют одной16-byte записи по count/size; исходный reader тоже EOF.
Для сценарных текстовых отказов игровой вылет не доказан. Это конкретные
причины/пути, а не63 независимых дефекта редактора. Все ошибки остаются видимыми.

Core SHA256: `0056e6fbe50a383482ab2c2c91f956aed801abd1b36955288ec40e81e28585ea`.
Evidence: `artifacts/Evidence/ScriptIdentities-20261015`.
Проверенные/published DLL совпадают; единые source/runtime пакеты обновлены.
SourceBinaries1006/maps4453/fixtures/TestMap7 и2548ENRUliteral values сохранены.
Документация: [ID safety](SCRIPT_IDENTITY_SAFETY.md),
[разбор27карт](CURRENT_CORPUS_REFUSALS.md). Окна/WPF/игра не запускались.

Другие составные обработчики/caller-history differences, real historical
corpus/compiler acceptance и физическая EN/RU GUI/audio/systemdialog/hardware/
DPI/game приёмка ещё остаются. Все23строки/fullgates сохраняются; goalACTIVE.

## 15 октября 2026 — согласованная история Undo

Пользователь выбрал сохранение нынешнего Undo для изменения паузы патруля
и параметров сценария. Не удалять эту возможность ради отсутствующего Undo
в соответствующих старых обработчиках и не запрашивать решение повторно.
Изменённые параметры создают один Undo; равный принятый сценарный Save
сохраняет нынешнее увеличение счётчика без нового шага. Принятая пауза,
включая равную, сохраняет нынешний шаг при ненулевой глубине.
Точная семантика и остальные отличия описаны в [EDIT_HISTORY](EDIT_HISTORY.md).

Текущая правка касается документации. Код, тесты, DLL и семифайловая карта
не менялись; сборка и полный прогон не повторялись. Унаследованы результаты
`ScriptIdentities-20261015`: все 41 проекта собраны, 1811 PASS / 63 отказа,
terminal exit 1, без новых отказов или потерянных PASS. Обновлены документы
обоих архивов поставки; доказательство — `artifacts/Evidence/HistoryDocumentation-20261015`.
Ни аппаратная приёмка, ни остальные требования полной цели этим решением
не закрываются; цель остаётся ACTIVE.


## Текущая сборка: ResizeScriptTargets-20261015

15 октября 2026. Исправлен исходный маршрут переноса координат трёх действий
при изменении размера через общее окно свойств: создание существа, изменение
клетки и владельца. Цель может остаться за границей карты; изменение размера
сохраняется одним общим Undo. Отдельные общие Resize/PrepareMapProperties
сохраняют строгий отказ. Проект сохраняет LONG, native — WORD-проекцию;
перенос целей не добавляет source counter increments к общему Apply/resize.

Добавлена EN/RU диагностика DK2N029: ошибка для привязанной выходящей за
верхние границы цели проекта, предупреждение для native/непривязанного действия.
Проверка не исправляет карту. Исходный compiler проверяет это после resize.
Все прежние2548 literal resource values сохранены, теперь2549 ключей EN/RU.

All41 build0 без предупреждений/ошибок; окончательная Validation build0,
focus-02 map-properties0, script identities/diagnostics/all103layouts0,
source properties caller и cross-caller interaction0, official seven0/publish0.
Новая фикстура исправлена: ID захватывается до вставки project binding,
которая перемещает ACTION record. Первый failed focus journal сохранён.
Проверены78 literal source cases, все100 других типов событий неизменны,
preview/Cancel/Undo/Redo/save/reopen, строгий API, валидная компиляция,
отказ привязанной неверной цели и нулевая история. WPF EN/RU/native/project
тест фактического класса окна собран, но не запускался.

Один окончательный полный прогон: terminal exit1, **1811 PASS /63 отказа**.
Входы и весь ordered PASS/failure arrays совпали с ScriptIdentities;
исходные94 полностью сопоставлены, новых failures/lostPASS нет.
Разбор27карт унаследован после проверки неизменности его входов;
новый аудит или ремонт установленных карт не заявляется.

Core SHA256: `fae04916102c077f22b1a073c40ba0f5021d58ddf335632e73e5e067f0ab4268`.
Доказательства: `artifacts/Evidence/ResizeScriptTargets-20261015`.
Проверенные/published DLL совпадают. Пакеты source/runtime обновляются из
единого41project с документацией и прежним семифайловым TestMap.
SourceBinaries1006/maps4453/fixtures сохраняются и проверяются упаковщиком.
Подробности и ручные шаги: [EDITOR_RESIZE_SCRIPT_TARGETS](EDITOR_RESIZE_SCRIPT_TARGETS.md).

Окна/WPF/renderer, официальный редактор, игра, звук и системные диалоги
не запускались. Полная цель ACTIVE; все23строки/fullgates сохраняются.
Остальные составные callbacks/caller-history, compiler/historical и физическая
EN/RU GUI/audio/systemdialog/hardware/DPI/game приёмка остаются.

Следующий подтверждённый остаток resize: исходный Properties_UpdateProperties
сдвигает camera/CPUArea только у компьютерных keeper3–7, сохраняет парные
sentinel и не ограничивает координаты границей. Общий Resize сейчас сдвигает
и ограничивает поля всех игроков. Требуется отдельный исходный оконный маршрут
с сохранением generic API, проверкой исходных пар и native проекции.
Снимок этого отличия: `next-player-resize-scope.json`. Оно не исправлено этим блоком.


## Текущая сборка: ResizePlayers-20261015

15 октября 2026. Общее окно свойств переносит камеру и область ИИ только
компьютерных хранителей3–7. Проектный X=-1 сохраняет всю пару/область;
прочие вторичные -1 сдвигаются вместе с координатами. Геометрия людей,
героя и нейтрального игрока не меняется. Учитывается частное переключение
управления перед общим Apply. Координаты не ограничиваются размером карты;
source LONG/игровая WORD-проекция сохраняются. Native65535 reader загружает
как положительный LONG, не как проектный -1. Общий generic Resize прежний.

V1/V2 player layouts сохраняют игнорируемые камеры V2 и сердца V1, обновляют
только поддержанную область ИИ без автоматического upgrade. Неоднозначный
native компьютерный keeper не даёт разрешения угадывать владельца.
DK2N030/031 — исходные неблокирующие предупреждения EN/RU о камере любого
хранителя и области компьютерного хранителя. Они не исправляют данные;
native диагностика оценивает WORD-проекцию, а не отсутствующий исходный LONG.

All41build0 без предупреждений/ошибок; focus map-properties0, identities/
diagnostics/all103eventlayouts0, source properties caller/cross-caller0,
official seven0/publish0. Новый блок прошёл с первой попытки. Проверены54
матрицы обоих форматов/9anchors/7players, независимые literal переполнения,
парные признаки/FFFF, V1/V2 opaque bytes, полный UndoRedo/save/reopen,
частная композиция/Cancel, предупреждения/компиляция и нулевая история.
Добавлены2 EN/RU ключа, прежние2549 literal values сохранены, всего2551.
Подготовлен и собран WPF-тест фактического окна EN/RU, но не запускался.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Весь ordered
PASS/failure arrays и Inputs совпали с ResizeScriptTargets; исходные94
полностью сопоставлены. Новых отказов или потерянных PASS нет. Существующий
разбор27карт унаследован после проверки неизменности его входов; установленные
карты не ремонтировались. Исходные raw reports сохранены полностью.

Core SHA256: `86ef97f7f4e1474d960faae1c37347f52839b35e24e6c79314e5f675921b29e6`.
Evidence: `artifacts/Evidence/ResizePlayers-20261015`. Current/published DLL
совпадают; единые source/runtime пакеты включают актуальную документацию.
SourceBinaries1006/maps4453/fixtures и ровно7 TestMap файлов проверяются
при обновлении пакетов; результат указан в delivery-verification.json.
Описание и ручные шаги: [EDITOR_RESIZE_PLAYERS](EDITOR_RESIZE_PLAYERS.md).

Окна/WPF/renderer, официальный редактор, игра, звук и системные диалоги
не запускались. Цель ACTIVE; все23 capability rows/fullgates сохраняются.
Остаются другие callbacks/caller-history, compiler/historical/native editor
metadata и физическая EN/RU GUI/audio/systemdialog/hardware/DPI/game приёмка.

Следующий конкретный остаток resize: native ACTION_SHOWCRYSTAL и
ACTION_DESTROYGATE содержат вычисляемые координаты. Исходный compiler
вычисляет их по предмету0x1C и прямоугольнику выбранных ворот; native resize
сейчас перемещает сущности, но меняет payload только трёх прямых координатных
действий. Требуется сверить native reader и порядок/однозначность разрешения
ссылок, не угадывая импортированные связи. Снимок: next-native-computed-target-scope.json.
Этот остаток не исправлен данным блоком и не заменяет остальные требования.


## Текущая сборка: NativeDerivedResize-20261015

15 октября 2026. Исправлен пересчёт вычисляемых native координат действий
«показать кристалл» и «уничтожить ворота» при изменении размера через общее
окно свойств. Первый кристалл выбирается в физическом порядке после удаления;
ворота разрешаются по точной прежней позиции, без выдуманного уникального ID.
Учитывается окончательная граница. Исходные результаты при отсутствии целей:
WORD65535/65535 и1/1 соответственно; они не объявляются исправленной целью.
Меняются только четыре байта координат, без дополнительных счётчиков;
частная подготовка и общий Undo сохраняют все остальные байты/связи/IDs.
Generic Resize, равное Apply, загрузка и простое сохранение не нормализуются.
DK2N032 EN/RU предупреждает об отсутствующей/устаревшей native цели без записи.

Все41 проекта собраны без ошибок и предупреждений. Первая сборка обнаружила
nullable-ошибку в новой тестовой фикстуре; она исправлена, первый журнал сохранён.
Фокусный блок, identities/diagnostics/all103 layouts, source properties caller,
cross-caller, source audit, официальный семифайловый compiler и publish: exit0.
Новые проверки:27 матриц привязок и9 буквальных случаев удаления/границы/
отсутствия/сердца/одинаковых позиций; полные action bytes, счётчики, Undo/Redo,
save/reopen, preview/Cancel, отказ от удаления, повторные физические IDs,
нулевая история, общий Resize и проектная компиляция. Прямые цели сохраняют
78 случаев, игроки54; остальные98 типов проверяются на неизменность отдельно.
Всего2552 EN/RU ключа, прежние2551 значения сохранены буквально.

Один полный прогон завершён: exit1, **1811 PASS /63 отказа**. Полные ordered
PASS/failure arrays и Inputs совпали с ResizePlayers. Все исходные94 отказа
сопоставлены:31 ранее исправлен, одно ранее известное изменение сообщения.
Новых отказов и потерянных PASS нет. Разбор63 отказов на27 картах унаследован
только после сверки хешей его входов; карты не ремонтировались.
Core SHA256: `47b9f84c93f32a4ff388405e5da16b987b96ac17fe6393ed90babce7407eca42`.
Evidence: `artifacts/Evidence/NativeDerivedResize-20261015`.
Текущая DLL и опубликованная совпадают. Упаковка проверяет Source/Binaries1006,
установленные maps4453, fixtures, все записи ZIP и неизменные7 файлов TestMap;
фактический результат записывается в delivery-verification.json.
[Правила, ограничения и ручные шаги](NATIVE_RESIZE_DERIVED_TARGETS.md).

Этот блок закрывает только общий resize. Другие перемещения/удаления/добавления
сущностей и native рабочие связи требуют отдельной сверки исходных маршрутов.
В частности, SetMapRegionBounds сам меняет геометрию ворот, но не пересчитывает
координаты зависимого действия. Снимок следующего участка:
next-other-computed-caller-scope.json. Общая политика компилятора для отсутствующих
проектных целей этим изменением не исправлялась.

Цель ACTIVE; все23 строки capability ledger сохранены. Остаток автоматической
работы: прочие связи объектов/сценариев и команды, семантика compiler/historical/
native metadata, оставшиеся зависимые callbacks и source counters/history.
Это группы аудита, а не конечное число подтверждённых ошибок. Физические EN/RU
окна, мышь, звук, системные диалоги, hardware/DPI, официальный редактор и игра
остаются отдельной приёмкой. WPF-тест нового окна собран, но не запускался.
Повторять зелёные прогоны из-за обновления документации не требуется.


## Текущая сборка: NativeGateMutation-20261015

15 октября 2026. Исправлено обновление native ACTION_DESTROYGATE при явной
геометрии ворот, редактировании поля, общем окне свойств области и его
вложенной генерации, смене ворот на сердце и удалении выбранной записи.
Разрешение до команды соответствует reader: первая физическая точная позиция,
сердца по type14 исключены независимо от bGateID. Список выбора и выбор
сохранённой цели используют те же правила; bGateID больше не считается ID.
При переносе меняются только четыре байта координат, при удалении выбранных
ворот writer даёт1/1. Это не исправленная отсутствующая цель. Preview/Cancel,
повторные IDs, резервные байты, исходные счётчики, общий Undo и raw generic crop
сохраняются. Вложенный replay обновляет и импортированные, и созданные действия.
[Подробности и ограничения](NATIVE_GATE_MUTATION.md).

Все41 проекта собраны с первой попытки,0 ошибок/предупреждений. Focus region
properties0; spatial, region generation, map properties, identities/diagnostics/
103 layouts, properties caller и cross-caller0; official seven/publish0.
Проверены11 сочетаний геометрии/полей/частного родителя,5 случаев удаления/типа,
физический порядок дубликатов, hearts и kind0/77/99, промежуточное совпадение,
полные action bytes, один счётчик/Undo/notification, save/reopen, zero history,
повторные IDs, raw generic crop и imported/generated replay.
Ресурсы EN/RU не менялись:2552 ключа и все2552 прежних literal values.
WPF-тест фактического RegionPropertiesWindow EN/RU подготовлен и собран;
окна, WPF, renderer, официальный редактор, звук и игра не запускались.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Все ordered PASS,
failures и Inputs буквально совпали с NativeDerivedResize; исходные94 полностью
сопоставлены,31 ранее исправлен и одно ранее известное сообщение изменено.
Нет новых отказов или потерянных PASS. Разбор63 отказов/27 карт унаследован
после сверки неизменных входов. Карты не ремонтировались.
Core SHA256: `a54db833d7bd02ac1c24944d2a5ee347368642004ec91dfa45bfe6ec3ce7d150`.
Evidence: `artifacts/Evidence/NativeGateMutation-20261015`.
DLL current/published совпадают; единые source/runtime архивы и документация
обновляются с проверкой всех ZIP entries, Source/Binaries1006, maps4453,
fixtures и неизменного комплекта ровно7 TestMap. Результат упаковки:
delivery-verification.json. Повторного прогона из-за документации нет.

Конкретный следующий остаток: постоянные рабочие ссылки native ворот между
разными командами при совмещении/разделении, с Undo/private copies/save policy;
эта правка сохраняет разрешение внутри одной команды, не вводит DKM IDs.
Также остаются другие команды изменения первого кристалла и полная политика
компилятора для отсутствующих проектных целей. Снимок next-working-bindings.json.
Цель ACTIVE; все23 строки capability ledger и физические gates сохранены.
Остаток теперь собран в [едином перечне](REMAINING_WORK.md): семь очередей
сверки/исправлений плюс ручная приёмка. Это не семь ошибок и не семь прогонов.


## Текущая сборка: NativeGateBindings-20261015

15 октября 2026. Рабочая native связь ACTION_DESTROYGATE сохраняет конкретную
физическую запись ворот между командами совмещения/разделения, отдельными X/Y,
Undo/Redo и нулевой историей. Можно выбрать вторые ворота с теми же игровыми
координатами: меняется рабочая ссылка, один source count и один современный
Undo, игровые байты остаются прежними. Равный повтор Save сохраняет прежнюю
counter-only семантику. bGateID и ID события не служат рабочей идентичностью.
Native список выбора показывает порядковый номер и координаты EN/RU.

Внутренний NativeFile D2GB/version1 участвует в TransactionFiles, fingerprint,
частных копиях и архиве исходных данных DKM; он исключён из Files/Records и
физического игрового экспорта. Удаления ворот/событий переадресуют записи,
удаление обычных вещей перед воротами их связь не меняет. Копирование графа
переносит связь явно. Родительский resize, окно области, генерация и мастер
сценария сохраняют её. Scope мастера разрешает только связь текущего события.
DKM-конвертация использует выбранную запись для проектного param5, даже при
совпадающей позиции. Обычный native KWD после открытия повторяет исходный
reader first-position; для редакторского ID сохраняется рабочий DKM.
[Подробное устройство и границы](NATIVE_GATE_BINDINGS.md).

Все41 проекта собраны0 warnings/errors. Первые две focus-попытки сохранили
ошибку новой экспортной фикстуры: неполная synthetic draft без игровых
определений была неверно принята за официальный комплект. Проверка усилена
семью точными именами и запретом выхода GateBindings в export, использует
полный игровой basis; focus-03 exit0. Два Validation rebuild0, Core не менялся.
Первый standalone scenario-save вызван без обязательного пути корня и завершился
ошибкой запуска; исходный журнал сохранён, исправленный scenario-save-02 exit0.
Неудачные попытки не скрываются и не считаются продуктовыми PASS.

Native conversion, unified save/source archive, source parameter/Finish callers,
native gate/record/payload retention, spatial, generation, map properties,
identities/diagnostics/all103 layouts, properties и cross-caller: exit0.
Official seven/publish0. EN/RU2552 ключа и все2552 прежних literal values сохранены.
Новый блок проверил физические коллизии, metadata-only выбор, отдельные X/Y,
полные Undo/Redo и count, zero history, удаления разных семейств и bulk unused,
clone, wizard Cancel/Apply, parent resize, настоящий DKM Save/reopen с ID2,
официальные7 имён и исходную native reload policy. WPF-тест второго совпадающего
прохода через фактическое окно EN/RU собран; окна/WPF/renderer, официальный
редактор, звук и игра не запускались.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Все ordered PASS,
failures и Inputs совпали с NativeGateMutation. Исходные94 полностью сопоставлены:
31 ранее исправлен, одно ранее известное изменение сообщения. Нет новых отказов
и потерянных PASS. Разбор63 отказов/27 карт унаследован только после сверки
хешей входов. Установленные карты не ремонтировались.
Core SHA256: `7cecdb893483ebcdd97f548cc5bbf73ee613baf4c10ee1e13242419a3ecda8bb`.
Evidence: `artifacts/Evidence/NativeGateBindings-20261015`.
Current/published DLL совпадают. Упаковка обновляет единые source/runtime архивы,
проверяет все ZIP entries, Source/Binaries1006, maps4453, fixtures и неизменные
ровно7 TestMap; результат в delivery-verification.json. Документация не запускает
полный тест повторно.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Следующие конкретные участки A: source выделение/освобождение/повторное применение
ID после удаления (таблица физических ссылок не является полным распределителем),
исходные вопросы использования ворот и прочие команды пересчёта первого кристалла.
Compiler tasks/writer/historical, прочие callbacks/counters/catalogues и физическая
GUI/audio/system-dialog/hardware/DPI/game приёмка остаются. Материалы следующего
участка: next-derived-work.json. Этот блок не закрывает всю очередь A или цель.


## Текущая сборка: NativeCrystalMutation-20261015

15 октября 2026. Native ACTION_SHOWCRYSTAL пересчитывается при изменении
упорядоченного списка кристаллов ID28 или их координат. Общая внешняя транзакция
охватывает прямые поля, добавление/удаление/дублирование, менеджеры, рисование
объектов, Cut/Paste и Rotate/Reflect, включая замену целого буфера коллекции.
Первая оставшаяся физическая копия даёт координаты без+1, последняя удалена —
WORD65535/65535. Только четыре байта действия, все IDs/связи/резервные поля
сохраняются. Производная запись не добавляет count/Undo/notification.
Undo/Redo и rollback содержат полные буферы; нулевая история поддерживается.
[Правила и границы](NATIVE_CRYSTAL_MUTATION.md).

Равное поле/preview/Cancel, посторонняя покраска и параметр золота не меняют
цель при неизменном списке кристаллов. Загрузка/save/диагностика не нормализуют
payload. Generic Resize и generic map-properties сохраняют raw-контракт,
включая crop и подавление до конца внешней транзакции, также после исключения.
Исходный оконный resize имеет прежний полный маршрут. DKM параметры сохраняются,
координату вычисляет compiler; task-политика отсутствующего кристалла не менялась.

Все41 проекта с первой попытки:0 предупреждений/ошибок. Focus прошёл с первой
попытки:13 буквальных командных случаев, полные action bytes и нецелевое действие,
точный count/один Undo/notification, whole-buffer UndoRedo/save/reopen, physical
first-order при массовом перемещении в конец, no-op/preview/raw Resize/generic
properties, raw crop/rollback, missing-last/add-new, duplicate event IDs, depth0
и DKM semantic/compiler policy. Первый вспомогательный stage generator остановлен
до запуска команд: неизвестного CLI-флага selection не существует. Проверен
реальный --selection-order-only, source-audit-02 exit0; оба журнала сохранены.
Неподдержанный флаг не запускался и не создал дополнительный полный прогон.

Native conversion, object painting, selection order/cut/paste/transform callers,
source parameter/Finish, unified Save/archive, gate/record/payload retention,
spatial, region generation, parent properties/resize, identities/diagnostics/
all103 layouts, properties/cross-caller: exit0. Official seven/publish0.
2552 EN/RU ключа и все2552 прежних literal values неизменны. Этот блок Core
не менял виджеты и не запускал WPF/редактор/renderer/официальный редактор/звук/игру.

Первый полный прогон: terminal exit1, **1810 PASS /64 отказа**. Новый отказ
оказался устаревшим ожиданием менеджера: тест требовал неизменного D2 даже при
удалении первого кристалла. Ожидание исправлено независимым разбором wire bytes:
рассчитываются только два координатных WORD, остальные буферы сравниваются
целиком; полный Undo проверяется отдельно. Validation rebuild0 и адресная
проверка этой карты0. Код Core не менялся. Оба сырых отчёта сохранены.
Повторный полный прогон: terminal exit1, **1811 PASS /63 отказа**. Весь ordered PASS,
failures и Inputs совпали с NativeGateBindings. Исходные94 сопоставлены целиком:
31 ранее исправлен и одно ранее известное сообщение изменено. Нет новых отказов
и потерянных PASS. Разбор63 отказов/27 карт унаследован после сверки входных
хешей. Установленные карты не ремонтировались.
Core SHA256: `206ba94293befa1f5145a889a4bdda941ab856f5b89e10b6d640120306b776bc`.
Evidence: `artifacts/Evidence/NativeCrystalMutation-20261015`.
Current/published DLL совпадают. Единые source/runtime архивы и документация
обновляются с проверкой всех entries, Source/Binaries1006, maps4453, fixtures
и прежних ровно7 TestMap; итог указан в delivery-verification.json.
Повторного полного прогона из-за документации нет.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
В очереди A закрыт конкретный пересчёт кристалла для перечисленных команд;
source gate-ID allocation/free/reuse и вопросы используемых ворот остаются.
Подтверждён source следующий участок: RectEx_GetFreeID ищет первый свободный ID
от1, Undo удалённых ворот добавляет запись обратно в MapGates. Рабочая таблица
пока хранит физические ссылки и отсутствующий target, не всю жизнь source IDs.
Точные функции и хеши: next-gate-id-lifecycle.json.
Отдельно подтверждён source Undo_SUBOBJECT: Undo_RestoreNode добавляет предмет
в конец MapObjects, тогда как текущий точный снимок восстанавливает прежний
порядок и прежнюю цель. 15 октября пользователь явно выбрал нынешний полный
Undo удаления предметов: это согласованное отличие, не остаток очереди E.
Выбор не распространяется автоматически на gate-ID lifecycle и другие отличия.
Политика compiler tasks/writer, остальные callbacks/counters/historical/catalogues
и физические GUI/audio/
systemdialog/hardware/DPI/game gates остаются отдельными требованиями.


## Текущая сборка: NativeGateIdentity-20261015

15 октября 2026. Реализованы source выделение/освобождение/reuse ID ворот:
первая свободная1–255, отсутствие0 не приобретает цель, удалённая ненулевая
цель сохраняет ID и снова разрешается при его повторном использовании.
Рабочие ID не перенумеровываются после удаления. Сохранены sparse DKM IDs,
ID в рабочем результате compiler и generator site/plan; EN/RU picker
показывает действительный ID. Игровые байты меняются только в двух WORD
зависимого действия, в общей операции. Клон отсутствующей цели сохраняет ID.
[Правила, версия2, совместимость версии1 и границы](NATIVE_GATE_IDENTITIES.md).

Все41 проекта: exit0,0 ошибок/предупреждений. Первые три focus exit1 — ошибки
новых тестов: добавление ворот через ограниченное окно свойств карты, подсчёт
всех элементов плана вместо добавляемых и заранее созданная export directory.
Ожидания/fixture исправлены; Core после общей сборки не менялся. Три Validation
rebuild exit0; focus-04 exit0. Все журналы сохранены. Проверены literal IDs,
полные bytes/metadata UndoRedo/rollback, history0, Cancel/resize Apply,
свободные1/2/7/9, generator, версия1,255/отказ256 и семифайловый reload.

Native conversion, painting, selection/cut/paste/transform, parameter/Finish,
unified Save/archive, gate/record/payload retention, spatial, nested generation,
parent properties/resize, identities/all103 layouts, properties/cross-caller:
exit0. Official seven и publish exit0.2552 EN/RU ключа и literal values сохранены.
App изменён и собран, но реальные окна/WPF/renderer/звук/официальный редактор/
игра не запускались. Ручная приёмка этих поверхностей остаётся.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Полные ordered
Passed/Failures/Inputs точно совпали с NativeCrystalMutation-20261015.
Исходные94 сопоставлены целиком:31 ранее исправленный, одно ранее известное
сообщение изменено, новых отказов/потерянных PASS нет. Разбор63 отказов на27
картах унаследован только после сверки входных хешей. Карты не ремонтировались.
Core SHA256: `ee4cd768da725a8e5c6e090eadfde6852078f5e7c520316922a8f3d0e3c3ea45`.
Evidence: `artifacts/Evidence/NativeGateIdentity-20261015`.
Source/runtime архивы обновляются из единого проекта: все ZIP entries,
Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и ресурсные
значения проверяются в delivery-verification.json. Документация не вызывает
повторный полный тест.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Следующий конкретный участок A — DKM gate removal guard: исходный SCOPE_GATE
не устанавливает Used при ссылке только из ACTION_DESTROYGATE, а текущий DKM
guard запрещает удаление. Общие подтверждения/remember/suppression тоже остаются;
E — порядок ворот после Undo
удаления. Согласованный полный Undo предметов сохраняется и не переобсуждается.
Остальные compiler/historical/catalogues/callbacks/counters и физическая
GUI/audio/systemdialog/hardware/DPI/game приёмка остаются требованиями.


## Текущая сборка: ProjectGateRemoval-20261015

15 октября 2026. Исправлен лишний guard удаления DKM ворот с входящим
ACTION_DESTROYGATE. Исходные Triggers_GetFirstTrigger/Triggers_IsUsed при
SCOPE_GATE возвращают-1 и не показывают отдельный used-dialog; удаление
сохраняет ACTION.param5. Текущая команда теперь сохраняет ID и все байты
действия; неизвестные layouts и другие entity/script guards не смягчены.
Предупреждение о ненулевой отсутствующей цели сохраняется; DKM draft можно
сохранить/открыть, compiler по-прежнему отказывает до устранения цели.
Повторное использование первого свободного ID восстанавливает цель.
[Полное описание](PROJECT_GATE_REMOVAL.md).

Все41 проекта: первая сборка exit0,0 ошибок/предупреждений. Focus с первой
попытки exit0: первые/средние/последние ворота с двумя действиями, независимое
ожидание полного KREC/размера +8, целые действия/другие разделы, один count/
Undo/notification, batch/rollback/history0, прочие entity/script/foreign guards,
read-only warning, реальный DKM Save/reopen, отказ compiler, клон отсутствующей
цели, reuse1, буквальные native WORD16/18 и семифайловый экспорт.
Прежнее ожидание общего typed-identity теста исправлено до прогона: ворота
удаляются с точным сравнением/Undo, остальные ссылочные сущности отказывают.

Native conversion, painting, selection/cut/paste/transform, parameter/Finish,
unified Save/archive, gate/record/payload retention, spatial, nested generation,
parent properties/resize, identities/all103 layouts, properties/cross-caller:
exit0. Official seven/publish exit0.2552 EN/RU ключа и literal values неизменны.
App widgets не менялись; окна/WPF/renderer/звук/системные диалоги/официальный
редактор/игра не запускались. Физическая приёмка остаётся.

Один полный прогон: terminal exit1, **1811 PASS /63 отказа**. Полные ordered
Passed/Failures/Inputs точно совпали с NativeGateIdentity-20261015.
Исходные94 сопоставлены целиком:31 ранее исправленный, одно ранее известное
сообщение изменено, новых отказов и потерянных PASS нет. Разбор63 отказов/27
карт унаследован после сверки входных хешей. Карты не ремонтировались.
Core SHA256: `8a77189c68333958312f5927321aa0136568b0737106bd7d7ec8ab4304947a1a`.
Evidence: `artifacts/Evidence/ProjectGateRemoval-20261015`.
Source/runtime архивы обновляются из единого проекта с проверкой всех ZIP
entries, Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и ENRU
literal values; итог в delivery-verification.json. Полный тест из-за docs не
повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Следующий конкретный A/G участок — общее подтверждение удаления регионов:
source REQUEST_RECTEXKILL=0x10, Yes/No/default/remember/suppression и полнота
менеджера. В текущем inspector есть обычный Yes/No, source remember нет.
Источник/текущий callback сохранены в next-region-removal-work.json.
E — source порядок ворот после Undo; согласованный полный Undo предметов
сохраняется. Остальные compiler/historical/catalogues/callbacks/counters и
физическая GUI/audio/systemdialog/hardware/DPI/game приёмка остаются.


## Текущая сборка: RegionRemovalConfirmation-20261015

15 октября 2026. Добавлен первый общий вопрос удаления областей/ворот/эффектов:
REQUEST_RECTEXKILL0x10, defaultYes, No/close, remember/default-only suppression
и reset. Preference вне карты/Undo, общий между видами и документами рабочего
окна. Immutable request фиксирует актуальные записи/поля/revision/fingerprint;
не доверяет переданному caller полевому описанию. Один пакет — один count/
Undo/notification, history0 и исключение/послеcommit failure различаются.
[Поведение, исходники и границы](REGION_REMOVAL_CONFIRMATION.md).

Подключены inspector (Ctrl/Shift для регионов одного вида) и региональная
панель; разные виды/сердца не принимаются как общий logical-region пакет.
Прочие категории сохраняют single-selection и старые команды. Список регионов
показывает рабочие native gate IDs, не bGateID, и исключает keeper hearts.
EN/RU dialog/remember/reset/stale/mixed/postcommit message/history:10 новых ключей,
всего2562. Все2552 прежних literal values сохранены.

Три сборки всех41 проектов: exit0,0 предупреждений/ошибок. Вторая завершила
UI labels/heart filtering; после первой зелёной полной проверки review выявил
необходимость canonical field-schema snapshot. Доработан Begin и добавлен
отдельный случай пустого caller Fields: действующий area-reference guard
не обходится. Третья общая сборка и focus-02 exit0. Первый focus тоже exit0.
Шесть native/project × area/gate/effect матриц, целые buffers/header oracle,
No/checked-No/close/default/remember/reset, cross-kind/document, immutable
selection/canonical fields, one operation/history0, stale/raw/foreign/mixed/
duplicate/nonregion/heart/nested/pen/closed guards, failure и committed observer.

Обе серии native conversion/painting/selection/parameter-Finish/unified Save/
gate-record-payload retention/spatial/generation/map-properties/identities103/
properties/cross-caller прошли exit0; вторая серия на конечной Core с отдельными
каталогами. Обе official seven/publish exit0. Четыре WPF EN/RU × format случая
подготовлены и собраны, но не запускались. Реальные окна, renderer, звук,
системные диалоги, исходный редактор, игра и hardware/DPI не проверены этим блоком.

Первый полный прогон: terminal exit1,1811 PASS/63 отказа на прежней Core этого
блока; после canonical-schema доработки второй полный прогон: terminal exit1,
**1811 PASS /63 отказа**. Оба ordered Passed/Failures/Inputs точно совпали с
ProjectGateRemoval-20261015, сырые отчёты сохранены. Исходные94 сопоставлены
целиком:31 ранее исправленный, одно ранее известное сообщение изменено, новых
отказов/потерянных PASS нет. Разбор63 отказов/27 карт унаследован после сверки
input hashes. Установленные карты не ремонтировались.
Core SHA256: `685419d754af27523d5794bb8057ba6499cd4312a08848280d18d5d2b6bd5a6c`.
Evidence: `artifacts/Evidence/RegionRemovalConfirmation-20261015`.
Единые source/runtime архивы обновляются с проверкой всех ZIP entries,
Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и EN/RU.
Итог — delivery-verification.json; полный тест из-за docs не повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Этот блок закрывает первый общий вопрос, не весь RectEx_RemoveRects.
Следующий A/G участок: specialized used-area Yes/No/Cancel, удержание orphan
scopes/ссылки эффектов и source переадресация первого nextid. Источники и
текущая команда сохранены в next-specialized-region-removal.json.
E — исходный append-порядок регионов после Undo; выбранный пользователем
полный Undo предметов сохраняется. Compiler/historical/catalogues/callbacks/
counters и физическая GUI/audio/systemdialog/hardware/DPI/game приёмка остаются.


## Текущая сборка: AreaRemovalPatrol-20261015

15 октября 2026. Подтверждённое удаление областей теперь обрабатывает строки
в порядке отображения и переадресует первого оставшегося предшественника
на следующую область удалённой записи. Работает для DKM и native; прочие
входящие патрульные связи не нормализуются. Низкоуровневый DeleteRecord
сохраняет прежнюю защиту. Остальные проверки ссылок и неизвестных layouts
остаются; второй used-area вопрос/orphan scopes/эффекты ещё не завершены.
[Поведение и исходный алгоритм](AREA_REMOVAL_PATROL.md).

Собраны все41 проекта: terminal exit0,0 предупреждений/ошибок. Focus exit0:
60 независимых packed-buffer сочетаний графа/порядка удаления, первая связь,
циклы/самоссылки/несколько предшественников/отсутствующие цели, все ожидаемые
байты и заголовки, полные Undo/Redo, один count/notification. Дополнительно
два late-failure случая подтверждают откат уже удалённой записи и изменённой
связи. Canonical field-schema и отдельный generic reference guard проверены.
Прежние шесть матриц запросов также прошли.

Native conversion, painting, selection, scenario Finish, unified Save,
gate/record/payload retention, spatial, generation, map properties, script
identities, properties callers и cross-caller: terminal exit0. Official
seven-file и publish: exit0. WPF harness собран; окна, renderer и игра
не запускались. Physical GUI/audio/dialog/hardware/DPI/game приёмка остаётся.

Полный прогон конечной Core: terminal exit1, **1811 PASS /63 отказа**.
Сырые упорядоченные Passed/Failures/Inputs точно совпали с предыдущим
RegionRemovalConfirmation-20261015/full-regression-02.json. Новых отказов
и потерянных PASS нет. Исходные94 сопоставлены целиком:31 ранее исправленный
и одно прежнее изменение сообщения XXLevel14. Аудит63 отказов на27 картах
унаследован после проверки входных SHA; установленные карты не исправлялись.
Core SHA256: `741f2c498db214771c508842cd172c22149f12780a598d696979f8d192fd1d30`.
Evidence: `artifacts/Evidence/AreaRemovalPatrol-20261015`.
Source/runtime ZIP обновляются из текущего единого проекта. Delivery проверяет
все entries, Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap,
новый official export и все2562 прежних EN/RU literal values. Итог —
delivery-verification.json. Полный тест из-за документации не повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md)
сохранены. Следующий участок A: второй Yes/No/Cancel используемых областей,
сохранение native orphan scopes в истории/приватных копиях/DKM и работа
ссылок эффектов. Это требует согласованной модели сценарных точек; нельзя
ограничиться снятием reference guard. Источники сохранены в предыдущем
next-specialized-region-removal.json. Исходный порядок регионов после Undo
остаётся отдельной E; полный Undo предметов согласован пользователем.


## Текущая сборка: UsedAreaRemoval-20261016

16 октября 2026. Подтверждённое удаление регионов включает второй used-area
Yes/No/Cancel. Пустая сохранённая точка тоже used; ID0 и action-only ссылки
не открывают второй вопрос, как в исходнике. No сохраняет used и удаляет
остальные, Yes удаляет все, Cancel ничего не меняет. Второй бит0x100 отдельно
от общего0x10; оба runtime preferences вне Undo и сбрасываются вместе.
Даже all-skipped No принимает один исходный счётчик и пустой Undo.
Полные [поведение и границы](USED_AREA_REMOVAL.md).

Сценарные точки и event/entity operands сохраняются при source-команде удаления
областей/эффектов. Generic DeleteRecord и неизвестные layouts сохраняют защиту.
Native #AreaScopes D2AS/v1 хранит явные пустые и orphan точки, входит в history,
private copies/fingerprints и DKM/native-source архив. Точки остаются корнями
unused planner, event-ID remapping, branch/wizard операций, явного удаления
точки и handoff владельца. ID reuse восстанавливает связь. Обычный KWD reload
не восстанавливает editor-only точки; игровой комплект по-прежнему7 файлов.
Map-properties child scope guard запрещает менять сценарии из своих настроек.
При позднем переполнении счётчика общий EditCore больше не теряет Redo.

Шесть сборок всех41 проектов и validation-build-06 завершились exit0 без
предупреждений/ошибок. Конечный focus-07 и hero-gate-focus-07 exit0.
Focus-04 и focus-05 также прошли на прежней
Core. Три ранних focus отказали на тестовых setup: неверно
повреждённый count исключал запись из списка, попытка изменить сценарий через
свойства карты корректно отклонялась, а native Save требовал единого
EditorSaveSession вместо project-only session. Фикстуры исправлены; сырые
логи и terminal codes сохранены. Два ранних source-audit отказа были вызваны
неполным regex return type и именем Request; source-audit-03 exit0.

Конечная проверка охватывает оба формата × пустые/непустые точки × ответы,
сохранённые operands/roots, обе настройки и stale second phase, полные native
game/working buffers и Undo/Redo, all-skipped/history0, неверный scope/ID0,
unused/identity/guard/ID reuse, private properties и wizard, DKM/archive/
compiler/seven-file. Прежние60 packed graph/order случаев сохранены; late
overflow проверяет также оба стека истории. EditorScenarioPointChecks32groups,
native conversion, painting/selection, scenario Finish103types, unified Save,
gate/record/payload retention, spatial/generation/map-properties/identities,
properties/cross-callers, official и publish: terminal exit0.
WPF EN/RU×format harness дополнен переходом двух окон/No/Cancel/remember,
только собран. Окна, renderer, игра, звук, исходный GUI и hardware/DPI не запускались.

Первый полный прогон:1810 PASS/64 отказа. Единственное добавление — прежнее
ожидание генератора, считавшее явно созданную пустую native точку отсутствующей.
Фикстура уточнена: отсутствующая точка добавляет отдельный счётчик, явно
сохранённая пустая — нет, в обоих форматах. Также после source review сохранён
прежний физический порядок живых roots при conversion среди других вещей;
добавлен независимый случай creature→area→band. После этой продуктовой правки
повторены integration-02/official-02/publish-02; все exit0. Второй полный
прогон1811/63 точно совпал с прежним ordered report. Последующий review выявил
конфликт DuplicateRecord с retained orphan ID и потерю пустого marker у копии.
Воспроизводящий focus-06 сохранил отказ Duplicate working area-scope address.
Исправлено: копия сохраняет свою пустую/клонированную точку, не присваивает
старую orphan и не теряет её; обычное создание с ID reuse восстанавливает
прежнюю точку. Четыре empty/nonempty × orphan/free-ID случая проверяют полные
буферы и одну операцию Undo/Redo. После этого integration-03/official-03/
publish-03 повторены на конечной Core, все exit0.
Полный-03 прогон конечной Core: terminal exit1, **1811 PASS /63 отказа**.
Упорядоченные сырые Passed/Failures/Inputs точно совпали с AreaRemovalPatrol.
Новых отказов или потерянных PASS нет. Исходные94 сопоставлены целиком:31
ранее исправленный, одно прежнее изменение XXLevel14. Аудит63 отказов/27 карт
унаследован после проверки входных SHA; установленные карты не ремонтировались.
Core SHA256: `a9d13669d5e6757a8854ed663b8cd48e9b057e899f3911a0b886e607213ce3ee`.
Evidence: `artifacts/Evidence/UsedAreaRemoval-20261016`.
Source/runtime архивы обновляются из единого проекта с проверкой каждого entry,
Source/Binaries1006, maps4453, fixtures, ровно7 прежних TestMap и нового official
export. EN/RU2565keys:3 новых, все2562 прежних literal values сохранены.
Итог — delivery-verification.json. Полный прогон из-за docs не повторяется.

Цель ACTIVE, все23 области и семь очередей [остатка](REMAINING_WORK.md) сохранены.
Далее A: caller обрезки Properties_RemoveRectEx. Исходник сохраняет точки и
operands, выставляет Nasty для used area, но не задаёт второй Yes/No/Cancel
и не переадресует nextid. Текущие resize callers ещё используют generic
DeleteRecord / DeleteLegacyRecord; их нужно перенести отдельно с общим parent
count/Undo. Nasty в полном исходном обработчике не читается, отдельного
предупреждения он не показывает. Нельзя вызывать direct-delete алгоритм с nextid
для crop. Исходные функции и современные callers сохранены в next-resize-crop.json.
Остальные compiler/historical/catalogue/callback/counter/native metadata и
физическая GUI/audio/systemdialog/hardware/DPI/game приёмка остаются открытыми.
Согласованный полный Undo предметов сохраняется; порядок регионов исходного
Undo остаётся отдельной E.


## Текущая сборка: RegionCropScopes-20261016

16 октября 2026. Source crop регионов сохраняет пустые/непустые сценарные
точки областей и operands, включая references областей/эффектов/ворот.
Подключены оба этапа фактического editor caller: outside ResizeCore и fixed
boundary после него. Предшественники nextid не переадресуются; второй вопрос
удаления не вызывается. Nasty исходником не читается и не требует окна.
Preview остаётся приватным, родительский Apply имеет один Undo/notification
и исходные счётчики. Generic native/project resize сохранили прежние отдельные
контракты. [Поведение и ручная проверка](REGION_CROP_SCOPES.md).

Две сборки всех41 проектов: terminal exit0,0 предупреждений/ошибок. Focus exit0:
72 native/project × kind/empty point × anchors × outside/frame случая,
полные event buffers и native working state при Undo/Redo, оба сохраняемых
предшественника, private Cancel/declined Apply, counts3/6, одна операция;
пакет трёх областей, DKM/native-source archive Save/reopen, first-free ID reuse,
compile/seven-file и прежние generic политики. Окно уже использует
PrepareEditorMapProperties. Старый быстрый Resize тоже направлен в то же
ShowMapProperties; отдельный ResizeMapDialog удалён после проверки отсутствия
других callers. Common Apply сохраняет точки из обеих UI-команд. Ресурсы не
менялись, все2565 прежних
EN/RU literal values сохранены. WPF проекты собраны, окна/игра не запускались.

Native conversion,32 point-operation groups, painting/selection, scenario
Finish103types, unified Save, gate/record/payload retention, spatial/generation/
map-properties/identities, properties/cross-callers, official и publish:
terminal exit0. Финальная App после перенаправления Resize повторно собрана/
published-02, Core hash не изменился; второй полный прогон ради UI delegation
не нужен. Окна не запускались. Полный прогон конечной Core: terminal exit1,
**1811 PASS /63 отказа**. Сырые ordered Passed/Failures/Inputs точно совпали
с UsedAreaRemoval/full-03. Новых отказов и потерянных PASS нет. Исходные94
сопоставлены целиком:31 ранее исправленный и одно прежнее изменение XXLevel14.
Аудит63 отказов/27 карт унаследован после проверки входных SHA; карты не правились.
Core SHA256: `06e669a3dea0acffb71add95bd8913e4640d854d60cab593291f5e5c347593f8`.
Evidence: `artifacts/Evidence/RegionCropScopes-20261016`.
Source/runtime ZIP обновляются с проверкой каждого entry, Source/Binaries1006,
maps4453, fixtures, ровно7 прежних TestMap, нового official export и всех2565
literal values. Итог — delivery-verification.json. Полный прогон ради docs
не повторяется. Физические GUI/audio/dialog/hardware/DPI/game проверки остаются.

Цель ACTIVE, все23 области и очереди A–G сохранены. Source crop регионов
закрыт; следующие связанные маршруты — Properties_RemoveThings / удаление
существ, дверей, обычных/специальных предметов и их native working scopes/
identity. Источник Things_RemoveAtMapPosEx сохраняет точки; существующие
generic paths ещё имеют другие scope contracts. Общий REQUEST_TRIGGERS0x100
между regional и creature callers остаётся в G. Не обобщать результат региона
на все физические владельцы. Источники и текущие callers — next-thing-scopes.json.
Остальные compiler/historical/catalogue/callback/counter и ручная приёмка
сохранены в [полном остатке](REMAINING_WORK.md).

## NativeOwnerScopes-20261016

16 октября: [рабочие точки владельцев](NATIVE_OWNER_SCOPES.md) охватывают
creature2/band8/member16/door64/object128/magical256. Явные пустые точки и
UID сохраняются независимо от raw event pointer. Source manager removal,
replacement, band deletion/private member compaction и Properties_RemoveThings
crop сохраняют orphan точки/операнды. Generic API сохраняют отдельные прежние
contracts. Полный Undo предметов с прежним порядком/целью подтверждён
пользователем; append-порядок исходного Undo предметов не воспроизводится.

Пересчитываются адреса владельцев после удаления любой BE-записи, переноса
пакета в конец файла, изменения размера и перестановки участников. Private
родитель отряда проверяет отображение слотов; Cancel не меняет карту. D2OS/v1
входит в dirty/history/rollback/private clones/fingerprints/DKM source archive
и compiler. Source Modify освобождает и перераспределяет ID, включая тот же
owner. Orphan ID зарезервирован: allocator сознательно безопаснее исходного,
пропускавшего objects и orphan roots; буквальный source allocator parity не заявлен.

Все41 проекта: build-final exit0, без предупреждений/ошибок. Последняя правка
тестовой фикстуры пересобрана point-build-08, Core неизменна. 33 point-operation
группы, включая12 six-family empty/nonempty матриц и32 thing-crop комбинации,
private band, replacement/generic distinction, full-buffer UndoRedo, UID
reallocation, DKM/archive/reopen/compiler и ровно7 игровых файлов: exit0.
Hero band24, historical20/compile5, inactive0/1/15/16, manager20, insertion8,
region focus, native conversion, painting/selection, scenario/unified save,
gate/record/payload retention, spatial/generation/map-properties/identities,
properties/cross-callers, official и publish: terminal exit0.

Полный корпус конечной Core: exit1, **1811 PASS /63 отказа**. Сырые ordered
Passed/Failures/Inputs совпадают с RegionCropScopes полностью. Исходные94
сопоставлены целиком; новых failures/lost passes нет. Аудит63/27 унаследован
после проверки исходных SHA; установленные карты не исправлялись. Перехода
к полностью ручной приёмке пока нет. Source/runtime ZIP проверены по каждому
entry, защищённые Source/Binaries1006, maps4453, fixtures и7 TestMap сохранены.
EN/RU2566: один новый ключ на язык, все2565 прежних literal values неизменны.
Core SHA256: `e128613d5109945b34108477927f66e9f4502aaefae04656f584dabcdf1e6e9b`.
Evidence: `artifacts/Evidence/NativeOwnerScopes-20261016`.

Цель ACTIVE; все23 области и очереди A–G сохранены. Текущий source thing crop
и шесть рабочих owner families реализованы. Далее остаются общий
REQUEST_TRIGGERS0x100 между creature/region callers, auxiliary member band
operand (native parent ID versus source physical ordinal), другие native metadata,
неоднозначные imported identities/layouts, compiler/historical/catalogue/
callback/counter coverage и отдельная ручная GUI/audio/dialog/hardware/DPI/game
приёмка. Полный перечень — [REMAINING_WORK](REMAINING_WORK.md). Окна и игра
не запускались. Полный корпус ради документации повторно не запускать.

## SharedScenarioRequests-20261016

16 октября: [общие запросы об использовании в сценариях](SHARED_SCENARIO_REQUESTS.md).
Менеджер удаления/изменения и регионы используют один REQUEST_TRIGGERS0x100
через workspace store. Общие запросы удаления0x2/0x10 независимы. Default-only
remember, reset, cross-policy lease и stale/schema/mutating-input guards входят
в модель. No фильтрует все assigned IDs; all-skipped delete сохраняет parent
Undo при source MapChanges+0. ModifyMulti фиксируется до фильтрации; дочернее
окно получает разрешённый текущий запрос. Старые manager consent checkbox
удалены из маршрута. Выбор фиксируется в display order; перевод сортированных
строк сохраняет selection и снимок, реальная смена выбора отзывает authority.

Auxiliary TRIGGERLINK.band участника теперь сохраняется буквально в D2OS,
DKM/archive, compiler и повторной конверсии. Native parent ID и physical source
ordinal различаются намеренно: простой игровой импорт использует ID, явный
Save точки обновляет ordinal. Пустая/непустая точки, metadata-only Modify,
ID77/ordinal1→0, удаление другого отряда, полный UndoRedo, реальные Save/reopen,
два compiler прохода и обычный семифайловый импорт проверены.

Core manager21 с24 request матрицами, point33 с новыми band-operand проверками,
hero-band24, historical20/compile5, inactive0/1/15/16, insertion8, region focus,
conversion/painting/selection/save/retention/spatial/generation/properties/
identities/cross-callers/official: exit0. Все41 проекта build-final/02/03:
exit0, без предупреждений/ошибок. Последние App-only исправления заново собраны
и опубликованы-03; Core не менялась. Bound WindowsSmoke App/Core совпадают
по SHA. WPF smoke код обновлён и собран; окна, renderer и игра не запускались.

Один полный прогон конечной Core: terminal exit1, **1811 PASS /63 отказа**.
Ordered raw Passed/Failures/Inputs совпали с NativeOwnerScopes полностью;
исходные94 сопоставлены целиком, новых failures/lost passes нет. Аудит63/27
унаследован после проверки входных SHA; установленные карты не правились.
Core SHA256: `7e62361c0ccdbc3552f244b6e7d8fb28c0853ad02f21f0f427b174171984ab4a`.
App SHA256: `2308248335c388b7f1359a551a785488d12ddf0cddcf4c1b448d1fb93d9b0d70`.
Evidence: `artifacts/Evidence/SharedScenarioRequests-20261016`.
Source/runtime ZIP сверены по каждому entry; Source/Binaries1006, maps4453,
fixtures, прежние7 TestMap и fresh official bytes сохранены. EN/RU2573:
семь новых ключей на язык, два прежних текста исправлены явно; остальные2564
literal values неизменны. Полный корпус ради docs/App-only правок не повторялся.

Цель ACTIVE, все23 области и семь очередей сохранены. Общий manager/region
request и auxiliary member operand реализованы; это не закрывает все G/E.
Далее — Delete/Shift+Delete/Ctrl+Delete → WM_KILLTHING →
Mouse_RemoveThingUnderCursor и фактический Canvas/keyboard caller,
прочие команды/Inspector routing, преобразования категорий/детачменты,
Config preference lifetime/persistence; затем прочий native/compiler/
historical/catalogue/callback/counter остаток и отдельная ручная физическая
GUI/audio/system-dialog/hardware/DPI/game приёмка. Системный звук и показ
новых запросов на этом этапе не подтверждены. См. [полный остаток](REMAINING_WORK.md).

## UnderCursorRemoval-20261016

16 октября: [удаление под курсором](UNDER_CURSOR_REMOVAL.md) подключено к
Delete/Shift+Delete/Ctrl+Delete в главном активном окне. Правый клик продолжает
выделять карту. Единственная занятая группа имеет приоритет над preference;
при нескольких группах выбирается предпочтительная либо ничего. Удаляется
вся стопка: creatures physical order, doors перед traps, objects перед specials.
Geometry-only, region pick, активный штрих/выделение, текстовый ввод,
outside Canvas, Alt/Windows и Shift+Ctrl не дают destructive authority.

Source safe question только creatures, общий0x100 с регионами/менеджером;
общего manager0x2 здесь нет. Frozen tile не зависит от движения указателя к
ответу или языка. No сохраняет все assigned UID. Points/events сохраняются.
Каждая удалённая запись даёт свой Undo и MapChanges+1; empty/no-all не создаёт
историю. Полный прежний порядок/цель предметов, выбранный пользователем,
сохранён. Независимая preflight копия и rollback обеих stacks/counters/buffers
не дают частичного отказа; UI refresh публикуется после готовности всей серии.
Observer failure после commit классифицируется как принятие всей серии.

Все41 проекта build-final: exit0, без предупреждений/ошибок. Manager22 groups
с48 occupancy/preference матрицами и crystal per-item Undo, shared requests,
replies/empty points/depth0/1/wrap/overflow/observer/DKM/compiler/seven-file:
exit0. Последняя test-only crystal проверка пересобрана manager-build-02,
manager-focus-02 exit0; Core не менялась. Focus, hero-band24, historical20/
compile5, inactive0/1/15/16, insertion8, point33, conversion/painting/selection/
save/retention/spatial/generation/properties/identities/cross-callers/official/
publish: terminal exit0. WPF keyboard/window harness подготовлен и собран,
его исполнение не заявлено; окна, renderer и игра не запускались.

Один полный прогон конечной Core: exit1, **1811 PASS /63 прежних отказа**.
Raw ordered Passed/Failures/Inputs совпали с SharedScenarioRequests полностью;
исходные94 сопоставлены целиком, новых failures/lost passes нет. Аудит63/27
унаследован после SHA входов; установленные карты не исправлялись.
Core SHA256: `b6670dc51c8c8c4e78fa03afa77f2cb2b7379cf2f0e19c2ec4275ff074a66808`.
App SHA256: `b4c0cea07dda6c14e3268b38b3f90b73e6b993531d30ffcb52551282731e9295`.
Evidence: `artifacts/Evidence/UnderCursorRemoval-20261016`.
Source/runtime ZIP проверены по каждому entry, Source/Binaries1006, maps4453,
fixtures, прежние7 TestMap и fresh official bytes сохранены. EN/RU2576:
три новых ключа на язык, все2573 прежних literal values неизменны.

Цель ACTIVE, все23 области и семь очередей остаются. Этот keyboard/cursor
caller реализован в модели/App; actual hardware/focus/viewport/DPI/audio и
игровая приёмка ещё ручные. Далее — Inspector deletion routing против source
object commands, manager category/signature/point detachment semantics и
полный Config preference lifecycle/persistence. Остальные native metadata,
historical/compiler, catalogue/compound definition/callback/counter и
GUI/system-dialog/game требования не закрыты. См. [остаток](REMAINING_WORK.md).
Не повторять полный корпус без изменения Core или новой причины.

## ManagerCategoryScopes-20261016

16 октября: [смена категории и literal points](MANAGER_CATEGORY_SCOPES.md).
Source manager сохраняет UID и все TRIGGERLINK/event bytes при door→trap,
ordinary→special и special→ordinary. Прежняя точка остаётся в своём scope
без владельца; обратная явная смена категории того же UID восстанавливает
связь. Creature keeper/hero/neutral остаётся scope2. Generic API отдельно
сохраняет прежнюю миграцию/explicit detachment, его guards не ослаблены.
Source child скрывает generic detachment и объясняет сохранение EN/RU.

Native D2OSv2 добавляет только живую identity-only строку trap: Scope0,
Member-1, Band/Root/Present0. Старыеv1 читаются; writer используетv2 только
при наличии таких строк. UID ловушки переносится в DKM/через compiler;
old point остаётся orphan со своим scope/root/ID. Reconnection ограничен
явным переходом door/object категорий; unrelated BE rewrite ничего не чинит.
Неверные версии, live address другого типа, Present/root/band/reserved и
orphan identity-only rows отвергаются. Семифайловый игровой формат не расширен.

Manager23 groups включает24 native/project × empty/nonempty × single/multiple
× category transitions/returns, real DKM/archive/reopen/compiler, retained
literal Events/UID, same-scope signatures, equal trap, allocator safety,
strict metadata и unrelated-rewrite guard. Core point33, under-cursor48,
shared requests24, hero-band24, historical20/compile5, inactive0/1/15/16,
insertion8, focus/integration/official/publish: final05 terminal exit0.
Все41 проекта build-05 exit0, без предупреждений/ошибок. Bound App/Core в
WindowsSmoke совпадают с поставкой по SHA. WPF тесты только собраны;
окна/renderer/игра не запускались.

Первая focus попытка нашла ошибку старого root snapshot в новой фикстуре;
исправлена. Первая полная сборка не прошла из-за internal UInt32 в новом
тесте и пересечения повторной сборки; обе закончились, затем отдельно
подтверждена build-03. Прогон старой DLL отменён до финального отчёта после
сужения reconnection authority; full-superseded.json и все старые логи
сохранены. Итог относится только к build-05/exit codes с тем же Core SHA.

Конечный полный прогон full-05: terminal exit1, **1811 PASS /63 прежних отказа**.
Raw ordered Passed/Failures/Inputs совпали с UnderCursorRemoval целиком;
исходные94 сопоставлены полностью, новых failures/lost passes нет. Аудит63/27
унаследован после SHA входов; установленные карты не правились.
Core SHA256: `c05f15d69ffc722edff0fde839ddf3d0cb604c6d2034b4fae609c80faba99db5`.
App SHA256: `eaa0f65ff0fb8a74b2edb5126b39dbee85fa8e0dae0f54c203b92520dbe0da34`.
Evidence: `artifacts/Evidence/ManagerCategoryScopes-20261016`.
Source/runtime ZIP проверены по каждому entry; Source/Binaries1006, maps4453,
fixtures, прежние7 TestMap и fresh official05 bytes неизменны. EN/RU2577:
один новый ключ на язык, все2576 прежних literal values сохранены.

Цель ACTIVE, все23 области/семь очередей сохранены. Реализована указанная
source category/point семантика; не заявлены весь manager inventory,
single-list membership pathological cases и все imported layouts. Далее —
Inspector deletion routing против source physical commands и остальные Config
request callers. Для реализованных bits2/16/256 подтверждён session-only
lifetime: source Config_SetToDefault обнуляет RequestsFlags, registry хранит
только Recent/SafeFlag/GamePath. Новому запуску не положено помнить эти bits;
записывать их в AppSettings не требуется. Это не закрывает остальные Config
настройки/requests. Далее остальные native/compiler/historical/catalogue/
compound/callback/counter требования и отдельная физическая GUI/audio/system
dialogs/hardware/DPI/official-editor/game приёмка. Полный corpus не повторять
ради docs или без новых изменений Core. См. [REMAINING_WORK](REMAINING_WORK.md).

## InspectorObjectRemoval-20261016

16 октября: [удаление из панели свойств](INSPECTOR_OBJECT_REMOVAL.md).
Физические категории native/DKM поддерживают extended selection. Delete
фиксирует видимый порядок и использует существующий EditorThingRequestPolicy
Remove и общие session bits2/256 менеджера. Первый запрос, used Yes/No/Cancel,
remember, source assigned-ID filtering и parent Undo/per-object counts
не обходятся generic DeleteRecord. Сохранён согласованный полный Undo
порядка предметов/цели кристалла. Mixed physical/raw не удаляет первую строку;
region/single band/other raw guards отдельно сохранены. Pending field commit
останавливает удаление по прежним адресам; selection/document/unload отменяют
старый запрос. EN/RU refresh сохраняет выбранную группу.

Build-02 всех41 проектов exit0, предупреждений/ошибок0. Manager23 и point33
CLI groups и publish exit0. Подготовлен/собран InspectorObjectRemovalSmoke;
WPF/renderer/окна/игра не запускались. Сверка source и DLL всего Core exact:
изменений нет. Полный корпус не повторялся; предыдущие1811 PASS/63 отказа
и исходные94 сопоставлены целиком с ordered raw Inputs/Passed/Failures,
SHA входов/старого аудита63/27 проверены. Это inherited Core evidence,
не физическая проверка нового App route. Тесты не подменяют ручную приёмку.

Core SHA256: `c05f15d69ffc722edff0fde839ddf3d0cb604c6d2034b4fae609c80faba99db5`.
App SHA256: `5e46b1a52dd8a4a07b4d892204135aebde62ad13660e5377ba74c8b797adc447`.
Evidence: `artifacts/Evidence/InspectorObjectRemoval-20261016`.
Поставка/source ZIP сверены по всем entries,41 project graph. Source/Binaries,
fixtures, карты и прежние7 TestMap не менялись. EN/RU2579: два новых ключа,
все2577 прежних literal values сохранены. Экспорт по-прежнему ровно7 файлов.

Цель ACTIVE,23 области/семь очередей сохранены. Дальше сверять остальные
manager single-list membership/команды и Config request callers, затем
остальные native/imported, historical/compiler, catalogue/compound/callback/
counter требования и ручную GUI/audio/system-dialog/hardware/DPI/game приёмку.
Не повторять полный корпус без изменения Core или новой причины.
См. [остаток](REMAINING_WORK.md).

## ScenarioDeletionRequests-20261016

17 октября, продолжение начатого16 октября этапа: [удаление сценариев](SCENARIO_DELETION_REQUESTS.md).
Point/condition/action используют отдельные source request bits0x800/0x200/0x400,
общий workspace EditorRequestPreferences и single-active lease. Default Yes,
No/checked-close/remember, centered questions и reset имеют EN/RU. Обязательный
checkbox перед branch delete заменён вопросом. Предпочтения вне Undo, карты
и AppSettings; действуют между документами, а не между запусками редактора.

Source extent уже был в Core: point удаляет весь AND/OR/next graph и point,
condition — self+AND descendants без своей continuation, action — только
self со splice continuation. Валидаторы выделены для общего Begin/Apply;
generic операции/guards и современный полный Undo сохранены. Source root-action
Unlink ограничен поиском condition/action predecessor; современная корректная
обработка root splice не заменена dangling link. Source direct commands
не создают Undo; современная история сохраняется, как и ранее.

Request snapshot включает revision/fingerprint/owner edge или eventID;
изменение графа, stale/raw/foreign/shared/pen/nested/overflow отвергается.
Commit followed by Changed observer failure остаётся принятым и освобождает
lease. App фиксирует выбранные Incoming/ScopeRoot/identity, а не только ID.
Любая пользовательская смена узла, в том числе shared-parent view, вкладки,
документа/unload отменяет запрос. Восстановление selection при EN/RU
перестроении дерева отдельно помечено и не считается новой целью.

Point model37 groups exit0 (прежние33 плюс четыре новые группы): восемь
native/DKM × empty point/nonempty point/condition/action shapes, exact
extent/continuation, No/checked close/Yes, independent shared bits, reset,
Save/reopen, full game/metadata UndoRedo; отдельные lease/stale/observer guards.
Point-02 исполнялся с тем же конечным Core SHA. После него менялся только App
selection-restore guard. Все41 проекта build-03 exit0, предупреждений/ошибок0;
owners/integrations/focus/official/publish final03 exit0. WindowsSmoke и
EditorScenarioPointUiChecks ретаргетированы и собраны; UI/renderer/игра
не запускались. Физическая работа окон/мыши/keyboard/audio/DPI ещё ручная.

Full-03: terminal exit1,1811 PASS/63 прежних отказа. Полный ordered raw
Inputs/Passed/Failures совпал с UnderCursorRemoval/ManagerCategoryScopes;
исходные94 сопоставлены полностью, новых failures/lost passes нет. Аудит63/27
и его SHA входов проверены. Установленные карты не правились.
Core SHA256: `396ef992c0a961db72b853d436924b11bffe7f200befb67e840ec53bbf185123`.
App SHA256: `ab718bffaf1f0af98bd1899ae47ba10b425b38d4c1eb3bf246167c5b6995a7d2`.
Evidence: `artifacts/Evidence/ScenarioDeletionRequests-20261016`.
Шесть global-definition scope документов уточняют текущую canonical parent
привязку и отделяют её от старых staging/Test текстов; физическая приёмка
и полный catalogue/unit/range inventory не объявлены закрытыми.
Все три build attempts завершились exit0; ранние App restore варианты
не объявлены физически прошедшими UI. Полный корпус запускался только для
конечного Core, логи/exit codes сохранены.

Source/runtime ZIP сверены по каждому entry и canonical41 project graph.
Source/Binaries1006, maps4453, fixtures, прежние7 TestMap и fresh official03
неизменны. EN/RU2586: семь новых ключей; все2579 прежних literal values
сохранены. Игра получает ровно7 файлов, новые request bits не сериализуются.

Цель ACTIVE, все23 области/семь очередей сохранены. Остались прочие Config
request callers, manager branches/single-list membership, native/imported
metadata и historical/compiler/catalogue/compound/callback/counter требования,
а также фактическая GUI/audio/system-dialog/hardware/DPI/official-editor/game
приёмка. Не повторять полный корпус ради документации или без новой причины.
См. [остаток](REMAINING_WORK.md).

## BigMapRequests-20261017

17 октября: [общий вопрос о большой карте](BIG_MAP_REQUESTS.md).
New Map и Map Properties используют общий REQUEST_BIGMAP0x20 и workspace
EditorRequestPreferences, single-active lease, default Yes, No/checked close/
remember/reset. Original informational icon и follow-mouse placement с
рабочей областью монитора. New form больше не требует обязательный checkbox:
валидный OK открывает вопрос; No оставляет форму. Crop consent отдельно.
Source threshold — площадь>14400, для resize только изменённые размеры.
Современные New Map1024/дополнительный>300 notice сохранены как расширение.

Approval не создаёт/не меняет карту или её Undo. Запомненный ответ общий
между new/resize/документами, вне Undo/AppSettings и только до нового запуска.
App сохраняет revision/settings/caller, Core resize request — live/working
plan/fingerprint и закрытие parent. Stale/raw/working/pen и callbacks,
закрывшие/сбросившие запрос, не дают старое разрешение и не крадут lease.
Current working/source map и семифайловый compiler формат не менялись.

Build-01 всех41 проектов exit0, предупреждений/ошибок0. Focus map-properties
exit0:26 source/new-extension matrices, shared reply в обе стороны, No/checked
close/reset, approval-only history, Apply/UndoRedo и stale/raw/working/lease
guards. PEReader проверил original Information32.ico внутри App DLL побайтно,
без Windows/WPF/renderer execution. Point37, manager23, owners/integrations/
focus/official/publish exit0 с тем же конечным Core SHA. WindowsSmoke новой
карты/общего вопроса обновлён и собран; окна/звук/игра не запускались.

Full-01 terminal exit1:1811 PASS/63 прежних отказа. Ordered raw Inputs/Passed/
Failures совпали с прежним отчётом целиком; исходные94 сопоставлены полностью,
новых failures/lost passes нет. Аудит63/27 и SHA его входов проверены.
Core SHA256: `1e4bc25f55338bc724a2da97ed60b32299704ca926e7e7ccb8c6ffcca8cae17f`.
App SHA256: `0a729a2a9711ae5360e0de990dd8052586f05fe4df6668a2155c512a2f007863`.
Evidence: `artifacts/Evidence/BigMapRequests-20261017`.
Source/runtime ZIP проверены по всем entries и canonical41 project graph;
Source/Binaries1006, maps4453, fixtures, прежние7 TestMap и fresh official01
неизменны. EN/RU2591: пять новых ключей, все2586 прежних literal values
сохранены. Никаких новых игровых файлов.

Описание новой карты обновлено: source question вместо старого checkbox;
устаревшая фраза об отсутствующей работе с project global overrides заменена
текущим lazy-family/reset поведением с сохранённой физической приёмкой.
Цель ACTIVE, все23 области/семь очередей сохранены. Остальные Config request
callers/manager branches, native/imported/historical/compiler, catalogue/
compound/callback/counter и GUI/audio/system-dialog/hardware/DPI/official/game
требования остаются в [остатке](REMAINING_WORK.md). Не повторять полный корпус
ради документации или без новой причины.

## PlayerGoldNotice-20261017

[PlayerGoldNotice](PLAYER_GOLD_NOTICE.md) добавляет source informational
предупреждение0x40 для общих настроек Хранителей: OK/Close Save без clamp,
session remember/reset, живую вместимость для приватных свойств, guarded
одноразовый Save и прежний Undo. Модель28 + два hosted-context случая,
build41/integration/official0 и raw full1811/63 подтверждены. Prepared WPF
только собран; физические GUI/audio/DPI/game и прочие требования всех23 строк
сохраняются в [REMAINING_WORK](REMAINING_WORK.md).

## MapNameOverflow-20261017

[MapNameOverflow](MAP_NAME_OVERFLOW.md) закрывает source-вызов вопроса о
длинном имени общих свойств: Copy/Keep/Cancel, exact ASCII64 boundary,
shared session0x4/reset, native Unicode extension, private/live session guards
и прежний общий Undo; generic writer guards не изменены. Name18/исторические
v1–v7/model02, build41/integration/official03 и raw full1811/63 подтверждены.
WPF только собран; все23 области и физические GUI/audio/DPI/game требования
сохраняются в [REMAINING_WORK](REMAINING_WORK.md).

## HistoryFailureAtomicity-20261017

[History failure atomicity](HISTORY_FAILURE_ATOMICITY.md) исправляет
подготовку root snapshots, публикацию метаданных, multi-file UndoRedo и
failed pen close. Четыре исходных сбоя воспроизведены до исправления;
108 safely injected allocation boundaries и все прежние drawing checks прошли.
Current build41/integration/official terminal0, raw full1811/63 exact.
Source error request UI/partial continuation остаются в E/G; physical memory/
GUI/capture/audio/DPI/game приёмка и все23 области не объявлены закрытыми.

## ScenarioChoiceCaptions-20261017

[Scenario choice captions](SCENARIO_CHOICE_CAPTIONS.md) уточняет EN/RU
radio-тексты, сохраняя codes/order100 choice fields. Build41, текущие
параметры/описания и official/publish прошли. Последний full1811/63 остаётся
предыдущим HistoryFailureAtomicity; наследование проверено неизменным кодом
writers/bounds и numeric projection, новый full не заявлен. Все23 области,
dynamic/compound/callback и физические требования сохраняются.

## ScenarioEntityCaptions-20261017

[Entity choice captions](SCENARIO_ENTITY_CAPTIONS.md) добавляет пропущенный
region info в контекстных списках и имена/info в area-coordinate pickers.
Source RectEx formatting применяется только к тексту, band name literal.
Numeric/order138 fields exact; build41/parameters/spatial/spawn/official
прошли. Full1811/63 наследуется от HistoryFailureAtomicity, не новый full DLL.
Все23 строки и полные gates, прочие catalogue/compound/callback и manual
GUI/audio/DPI/game требования сохраняются.

## RequestSourceAudit-20261017

[Source request inventory](SOURCE_REQUEST_AUDIT.md) maps all18 defined bits,
17 active and unused AREASIZE. Existing routes are not recertified by a
text search. Four missing request routes and COPY remembered lifetime yield
five concrete request tasks R01–R05. Actual generator logic already exists.
Manager error recovery has seven source call sites; last button maps to0.
The source destroy-parent/restore-previous hazard is recorded as an inference.
Product/DLL/tests/resources unchanged; no new build/full/GUI run is claimed.
All23 capability rows and full completion gates remain required.

## CopyConfirmationLifetime-20261017

[Copy confirmation lifetime](COPY_CONFIRMATION_LIFETIME.md) separates a
session-only remembered Yes from the persistent explicit menu preference.
Existing consent routes/guards are retained. Current build41 and focused/
official/publish passed; full1812/63 retains all1811 ordered previous passes
and the exact63 failures, with full original94 comparison. WPF compiled only.
R05 physical acceptance, R01–R04 and every23-row completion gate remain.

## GateGenerationRequests-20261017

[Generator requests](GATE_GENERATION_REQUESTS.md) implements R03/R04 source
confirmation/capacity routes across workspace and private parent/child.
Separate shared session flags/reset, default-only remember, 254+1/255+1,
area/gate/both messages and owner/revision/raw/pen/replay guards are verified.
Replies do not mutate documents or authorize exhausted generation. Current
build41/focused/old generator/official checks pass; full1813/63 preserves all
1812 ordered previous passes and63 failures with full original94 comparison.
WPF compiled only. R01/R02, all23 rows, other callers and physical gates remain.

## HistoryMemoryNotice-20261017

[Undo allocation notice](HISTORY_MEMORY_NOTICE.md) implements R01 typed,
post-unwind informational0x8 delivery, default-only remember and reset.
Normal/secondary committed observer semantics, 108 allocation boundaries,
read-only notification lease and session flags are verified. Current build41/
focused/official checks pass; full1814/63 preserves all1813 prior passes/order
and exact63 failures with complete original94 comparison. WPF compiled only.
No real heap exhaustion. R02 follows the user's approved full rollback policy;
12 staged manager faults verify insertion/modification/removal rollback.
All23 rows, other callers and physical gates remain.

## RecentFileCommands-20261017

Внутри I17 реализованы исходные команды очистки недавних файлов и удаления
недоступных записей; default menu label теперь basename, полный путь остаётся
tooltip/аргументом открытия. Source Config_RemoveObsolete probe access0/share3/
OPEN_EXISTING/SEQUENTIAL_SCAN повторён через закрываемый SafeFileHandle.
Только recent history/JSON; карты, Undo и LastOpenedFile не изменяются.
EN/RU2625 keys,6 новых на язык;2619 прежних literal values сохранены.
Все41 build/publish exit0, zero warnings/errors. Core `5079c00b06b555c9eb71ad5d075131bab93b22de1432caab4b2bc130cf397a2c` неизменна;
App `26800d62128e876879d1c1a8007f5d07d27c3f3818088ccdb6f272e10ea7d3e0`. Полный corpus не повторялся: последний1814/63 на этой Core
от HistoryMemoryNotice; это не новый full результат. GUI/game не запускались.
[Поведение и ручные шаги M20](RECENT_FILE_COMMANDS.md). I17 закрыт только в
этих трёх маршрутах; остальная сверка и все требуемые manual gates сохраняются.

## ManagerCommandHistory-20261017

Внутри I17 добавлены Add и локальный Undo самого менеджера; double-click
строки использует общий Modify/request route. Точный root ownership исключает
отмену чужой правки/шага до открытия; scope живёт один сеанс окна. Allocation
failure сохраняет retry, postcommit observers не теряют выполненный шаг.
Native/project CLI прошли; EN/RU WPF4 случая только compiled.
Build04 all41/publish02 exit0, zero warnings/errors. Full current Core:
1815PASS/63FAIL exit1; прежние1814 passes/order,63 failures и Inputs exact,
original94/audit63 hashes проверены. Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`;
App `9f28e7c41639b667665bfddf91dc8238ec84962033d3b5d1f4f42805ed716e77`. EN/RU2626/1new/2625old literals. Late App-only cleanup
не меняла Core; corpus повторно не запускался. GUI/game не запускались.
[Функции и ручные шаги M20](OBJECT_MANAGER_COMMANDS.md). I17 остаётся открыт:
CenterView/Show destination/ChoosePoint и прочие options/Config branches.
Все остальные пункты OPEN_FUNCTIONS_RU и physical gates сохраняются.

## ManagerNavigation-20261017

В I17 реализованы CenterView одного объекта, Show destination X/Y и
ChoosePoint: private repeated click preview, source OK/Cancel/RMB, shared
session coordinates; canvas/viewport/cursor, окно возврата и queued modal
restore. Revision/token/owner/close guards и очистка подписок предусмотрены.
[Поведение и ручные шаги M20](OBJECT_MANAGER_NAVIGATION.md).
Build03 all41/publish01 exit0, zero warnings/errors; WPF4 случая только compiled.
App `0ee675093dd7f604fcc01d730ba340408b23aff29fd47bbdecbc4e877aa5d725`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна. Последний full1815/63 — предыдущий
ManagerCommandHistory на этой же Core, не новый запуск. Успешные CLI/corpus/
official проверки без изменения Core не повторялись. EN/RU2635/9new/2626old.
GUI/editor/game не запускались. Эти три code routes переведены в M20;
I17 остаётся открыт по options/preview/прочим Config, все другие пункты и
physical gates сохраняются. Пакеты/документация обновлены, карты не записывались.

## ManagerOptionsDialogs-20261017

В I17 подключены отдельные AddOptions/ModifyOptions: private draft, Apply-only
запись runtime preferences, source семейные поля/hidden bits, EN/RU и guarded
Cancel/close/document lifetime. [Окна и M20](OBJECT_MANAGER_OPTIONS.md).
Build01 all41/publish01 exit0, zero warnings/errors. Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна;
App `60f482784d1fe650b6a87c31382e68079529b7eb620c52326e600fe4d96ec750`. EN/RU2641/6new/2635old literals. Tests/ не менялись; новые
повторяющие реализацию tests и повтор corpus/CLI/official не выполнялись.
Последний full1815/63 остаётся от ManagerCommandHistory на той же Core.
GUI/editor/game не запускались; окна требуют ручной приёмки.
I17 остаётся открыт: AskOptions/Preview execution sequencing, direct template
path и on-map/selection Add coupling, прочие Config branches. Эти параметры
пока не выданы за полную source flow parity. Остальные I/M/gates сохранены.

## ManagerExecutionFlow-20261017

Внутри I17 подключён фактический запуск Add/Modify: независимые AskOptions и
Preview, исходное число выбранных строк до фильтрации, single properties с X/Y,
общий шаблон и rectangle/selection scope. Неразрешённые предупреждения и
replacement Ask требуют решений; прямое применение использует общие Core-планы,
guards и локальный Undo. [Поведение и ручная M20](OBJECT_MANAGER_EXECUTION_FLOW.md).
Build02 all41/publish01 exit0, zero warnings/errors. Focus01:24 исхода source
options/preview policy, без WPF. EN/RU2647 keys,6 новых,2641 прежних literals
сохранены. Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна;
App `47452db7f7b15fab90b8e71a43675e6c23281790ba92ea8e58abf30b1b289a48`. Full/corpus/official не повторялись:
последний full1815/63 от ManagerCommandHistory на этой же Core. Это не новый
1816 full result. WPF4 EN/RU/native/project последовательности только compiled.
Source/Binaries/fixtures и прежние7TestMap bytes сохранены; GUI/game не запускались.
Пакеты обновлены из единого текущего проекта. I17 прочие команды/Config и
все остальные I/M пункты остаются открытыми; вся цель ещё не завершена.

## ViewVisibilityCommands-20261017

Внутри I17 исправлен пропуск IDM_TOOLBAR: «Вид → Панели инструментов» скрывает
полосу команд и инструменты карты. IDM_MINIMAP доступен из того же меню;
его отметка синхронизируется с прежним флажком workspace. Предпочтения действуют
в текущем окне, по исходным Toolbar_Hide/Minimap_Hide, без записи карты/Undo.
[Поведение и ручная M21/M23](VIEW_VISIBILITY_COMMANDS.md).
Build01 all41/publish01 exit0, zero warnings/errors. App `bb7f4a4a3305782dab3424a9f6082b9e7212351c0e3c61177bbc2d8c2dc9669a`;
Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa` неизменна. EN/RU2648 keys:
один новый ключ,2647 прежних literals сохранены. Tests/ не менялись; новые
повторяющие реализацию tests и повтор CLI/corpus/official не выполнялись.
Последний полный1815/63 — ManagerCommandHistory на этой же Core.
Source/Binaries/fixtures/7TestMap bytes сохранены. GUI/game не запускались.
Дополнительно сохранён lexical inventory42 CONFIG fields/337 refs/37 files;
он не доказывает semantic closure остальных полей. Следующая работа I17 —
их конечное сопоставление, без повторной реализации закрытых команд.
I14 ожидает уже заданный пользователю выбор порядка Undo регионов;
повторный вопрос не нужен. Остальные I/M и вся цель остаются открытыми.

## RegionUndoConfigScope-20261017

I14 закрыт явным ответом пользователя: полное восстановление прежнего места
и состояния областей, проходов и генераторов. Код уже делает это через
Before/After replay; имитация source List_AddEntry в конец не требуется.
[Решение](REGION_UNDO_POLICY.md). Остаток:17 кодовых пунктов и23 manual группы.
[Сопоставление42 CONFIG fields](CONFIG_SOURCE_MAPPING.md) заменяет неопределённые
«прочие настройки» конечной таблицей. Конкретно отсутствуют Sound_Play callers
для15 штатных WAV и source default title; дополнительные геометрия/пути/report
маршруты явно оставлены на сверку. Следующий кодовый объём — звуки интерфейса
и рисования, не повтор уже закрытых команд. I17 целиком не закрыт.
Только решение/чтение кода/документация/упаковка: src/tests/Core/App неизменны,
новых build/publish/CLI/corpus/WPF/game нет. Поставка приложения остаётся
ViewVisibilityCommands, последний full1815/63 на прежней Core сохраняется.
Source/Binaries/fixtures и прежние7TestMap bytes сохранены. Вся цель активна.

## EditorSounds-20261017

Внутри I17 подключены Sound_Play callers: выбор инструмента/terrain, pen start,
shape definition/completion и object brush.15 исходных WAV публикуются рядом
с EXE, bytes/RIFF PCM сверены. Categories interface/terrain и source−500
centibel volume сохранены; один player на sample перезапускается независимо
от остальных. Unloaded закрывает players; load/language/sample rebinding тихие.
[Правила и ручная M14/M21/M23](EDITOR_SOUNDS.md).
Build02 all41/publish01 exit0, zero warnings/errors. Focus01:42 исхода чистой
политики,15 имён WAV,3 громкости — PASS; без WPF/медиасистемы/устройства.
App `0831c7252f750f6d3855224722f780a4fc63f9bd7f6003911391956df800d061`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2648 keys/literals unchanged. Corpus/старые CLI/official не
повторялись; последний full1815/63 от ManagerCommandHistory на этой же Core.
Новые фокусные группы не считаются новым полным запуском. Source/Binaries/
fixtures/прежние7TestMap bytes сохранены. GUI/game/audio не запускались.
Кодовый пропуск SoundsVolume/SoundsFlags снят в указанном scope; слышимость,
latency/mixing/device/close остаются ручной приёмкой. I17 default title,
геометрия/пути/report и прочие manager callers открыты. I14 остаётся закрытым
выбранным полным Undo;17 кодовых пунктов/23 manual группы и вся цель сохранены.
Следующий конкретный кодовый маршрут — source default title через внутреннее
имя карты с обновлением после Rename/Undo, без полного graph scan на refresh.

## WindowMapTitle-20261017

Внутри I17 исправлен source default title: основное окно использует внутреннее
имя карты, не basename Save As. EditorWindowCaption читает проверенный native
GAMEINFO либо единственную текущую KINF layouts1–7 через существующие schemas;
не вызывает Records/CreateMapGeneralDraft/fingerprint. Rename/Undo видят живые
bytes; неизвестный/неполный header даёт fallback к filename. Dirty/recovery и
EN/RU app caption сохранены. [Поведение и ручная M03/M14/M23](WINDOW_MAP_TITLE.md).
Build01 all41/publish01 exit0, zero warnings/errors. Focus01 — PASS: native/
project live Rename/Undo, read-neutral bytes/history, Unicode,7 old layouts,
full-width/empty names и malformed/future bounds. Без WPF запуска.
App `916050b83ffafd03c975a3e65761169904939cc681aa9253d77c36ac7d80b230`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2648 keys/literals unchanged. Corpus/старые CLI/official не
повторялись; последний full1815/63 от ManagerCommandHistory на этой же Core.
Новые фокусные группы не являются новым full. SaveLifecycle WPF assertion
обновлён под internal title, только compiled.15 WAV bytes и Source/Binaries/
fixtures/прежние7TestMap сохранены. GUI/game/audio не запускались.
UseFilename/ShowFullName default route закрыт; I17 геометрия/пути/report и
прочие manager callers открыты. Следующий объём — исходные report callers:
Load/Save/Compile, ограничения severity/version и принятие/отмена отчёта.
I14 закрыт согласованным полным Undo;17 кодовых пунктов/23 manual группы и
вся цель сохраняются, стадия не выдаётся за полное соответствие.

## DocumentReports-20261017

Внутри I17 подключены полные Load/Test/Compile/Run reports вместо усечённого
MessageBox; список не ограничен12 строками и не объединяет одинаковые codes.
Выбор области перемещает viewport/cursor и временную подсветку без правки
карты/выделения; close восстанавливает прежний highlight того же документа.
Load/Test close-only; errors не разрешают Continue; stale/owner/revision/plan
guards сохранены. Default Close/Cancel, включая Save consent.
Source SaveReportFlags Version1/Warnings2/Alerts4/Infos8/default14 подключены
для дополнительных version notices в существующий SaveConfirmationWindow;
loss/blocking/native-source/archive consent не обходятся, autosave не менялся.
[Объём, исходник и ручные шаги](DOCUMENT_REPORTS.md).
Build03 all41/publish02 exit0, zero warnings/errors. Focus01:24 filtering/
continuation/region исхода,25 ordered rows — PASS, без WPF. WPF4 EN/RU×native/
project cases только compiled; реальные Root dialogs/отмена до writer не приняты.
App `6ad06973bb5f01b733aa989b178da5ce17f78ae39f9dbb883e3f6b5d4b79be3e`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2662 keys,14 новых на язык,2648 прежних literals сохранены.
Corpus/старые CLI/official не повторялись; последний full1815/63 на этой Core
от ManagerCommandHistory. Новые фокусные группы не являются новым full.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены; GUI/game/audio
не запускались. I02 полнота всех compatibility generators остаётся открыта;
I17 прочие manager/геометрия/пути остаются.17 code/23 manual и вся цель сохранены.
Следующая ограниченная работа — независимые MapPath/AreaPath/ThumbPath callers
из CONFIG_SOURCE_MAPPING, без повторной реализации закрытых report routes.

## FileDialogPaths-20261017

Внутри I17 подключены независимые MapPath/AreaPath/ThumbPath scopes:
working map следует принятому EditorSaveSession target, AREA/BMP запоминаются
после OK chooser перед IO, включая последующий отказ. Cancel не сохраняет
новое имя. AREA/BMP общие для своих дочерних окон и переживают смену карты;
не добавлены в JSON/Undo. Старый LastOpenedFile fallback каталога сохранён.
Исправлен BMP UI overwrite: после native consent подготовленные bytes пишутся
в staging и заменяют target; generic SaveTo/SaveNewBmp остаётся new-only.
Source/revision/options guards и EN/RU stale message добавлены.
[Поведение и ручные шаги](FILE_DIALOG_PATHS.md).
Build02 all41/publish02 exit0, zero warnings/errors. Focus02 — PASS: scope
independence/failed AREA path/new-native reset, new-only protection, bad BMP,
partial write/flush failures и exact new/replacement publication, private temp
only. Focus01 failed InvalidDataException assumption сохранён; отдельный catch
исправлен и в UI, и в check. WPF/native dialogs не запускались.
App `7899b03a8e2ee905e3a60c16d9a4f03e5d0bf11eff5ecc579743f5f61ab796fe`; Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2663 keys,1 новый,2662 прежних literals сохранены.
Corpus/старые CLI/official не повторялись; последний full1815/63 на этой Core
от ManagerCommandHistory. Новый focus не является новым full.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены; GUI/game/audio
не запускались. I17 scopes этих трёх путей закрыты по коду; native dialogs
M03/M04/M07/M21/M23 остаются. I17 геометрия и прочие manager callers открыты;
I02 compatibility completeness и все17 code/23 manual группы сохраняются.
Следующая ограниченная работа — служебные окна compiler/thumbnail:
геометрия, повторное открытие и source lifetime из CONFIG_SOURCE_MAPPING.

## ThumbnailWindowLifetime-20261017

Внутри I17 исправлены lifetime и положение миниатюры: при смене карты окно
остаётся открытым, переключает источник и сохраняет preferences; старый
документ отписан. Callback пропорций работает после переключения. Закрытие
документа закрывает окно и запрещает повторную привязку/экспорт. Ошибка
построения очищает старое изображение и отключает экспорт с сообщением EN/RU.
ThumbnailWindowX/Y сохраняются в памяти workspace; первое положение у правого
края owner, восстановление ограничено рабочей областью ближайшего монитора.
Размер не сохраняется, как в source. JSON/карта/Undo не меняются.
[Поведение и ручные шаги](THUMBNAIL_WINDOW_LIFETIME.md).
Build02 all41 и publish01 exit0, zero warnings/errors. После build01 уточнён
probe отписки: изменение старой карты обязательно реально меняет клетку.
WindowsSmoke расширен
и только скомпилирован; GUI/native HWND/DPI не запускались. CLI/corpus повторно
не запускались. App `a1e8ca32745fb9064918c23325115c922cb33fc12eddf6273804dee12edcf94c`, Core `1bc49ca6a4027a0da05bea364a4eab029f3bda665f4137315ef029a1abbf0eaa`
неизменна. EN/RU2664 keys:1 новый,2663 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены.
Самостоятельное compiler task window (фазы/текст/errors/итог/Close-Run и
CompilerWindowX/Y/Width/Height) ещё отсутствует. Это следующий конкретный
маршрут I17; запоминание геометрии DocumentReport не считается его заменой.
I02 и остальные I/M scopes, включая M21/M23, остаются открыты; цель не завершена.

## CompilerTaskWindow-20261017

18 октября2026: отдельное окно Test/Compile/Run подключено к реальным меню,
toolbar и Ctrl+Shift+E. Фазы, полный журнал и список сообщений без truncation,
severity/counts/details, навигация к области после завершения и итог EN/RU.
Background validation/compilation/writing используют независимую копию по
существующему ClonePropertiesDocument; live cleanup/consent остаются на UI.
Существующие revision/context/plan guards и предупреждения перед writer
сохранены. На время работы открытые editor windows отключаются, autosave
заблокирован, закрытие task window не прерывает writer. После работы states
восстановлены. Run запускает игру отдельной кнопкой; stale документ лишает её
права запуска. Normal X/Y/W/H сохраняются между задачами, monitor-clamped.
[Поведение, доказательства и ручные шаги](COMPILER_TASK_WINDOW.md).
Build04 all41/focus01/publish02 exit0, zero warnings/errors. После build03
уточнён source default: центр desktop/screen вместо owner. Новый Core
`f594915efcd4ce4a36c2131d71196e26a4a609229c4a4ff69896451560239a12`, App `81dba951daec269f0be73eed56b79f43623d128eb5d5f2faa2520f20667e98dd`.
Новый snapshot focus подтвердил независимость bytes/history/statistics,
aliases/target/missing references, active-edit refusal и одинаковые7 official
файлов из пользовательского DKM/native-source и их snapshots.
Полный прогон1821 PASS/63 прежних отказа, exit1. Все1815 прежних PASS сохранены
в том же порядке; добавлены5 уже реализованных App policy checks и1 snapshot.
Failures/Inputs совпадают с предыдущим full побайтно и по порядку. Полный
исходный список94 сопоставлен:31 ранее исправлены, остальные63 прежние;
одно известное уточнение XXLevel14 wMainTrigger=52 сохранено. Новых отказов нет.
EN/RU2676 keys,12 новых; все2664 прежних literals сохранены.
WindowsSmoke/GameRunUiChecks обновлены под async, только скомпилированы.
WPF/native dialogs/audio/game не запускались. Source/Binaries/fixtures,
15WAV и прежние7TestMap bytes сохранены. GUI/DPI и игра остаются M-приёмкой.
CompilerWindowX/Y/Width/Height закрыты по коду; I02 message completeness и
остаток I17 (HideSplash/HideDisk, manager single-list/pending-field/command
routes) не закрыты этим окном. Все17 I и23 M группы остаются конечным перечнем.

## DiskProgress-20261018

Внутри I17 закрыт активный HideDisk caller:5 source методов загрузки DKM/KWD/MAP,
обычного сохранения DKM и MAP сопоставлены,70 create/update/destroy references
записаны в source-scope.json. В App подключено краткоживущее EN/RU окно с путём,
текущим разделом/файлом, фактическими counts/bytes; без оценки общего времени.
Save начинается после согласия и использует прежний atomic writer с наблюдаемым
stream; durable Flush и владение сохранены. Load observer не меняет данные;
принятие документа защищено context. Корневые окна временно отключены, redraw
через Render priority, cleanup в Dispose. Загрузка/Save остаются синхронными,
как source. Recovery подключён; autosave тихий и заблокирован во время явного I/O.
HideSplash caller исключён source #define SPLASH_DISABLED1: новый UI не нужен.
[Поведение, source mapping и ручные шаги](FILE_IO_PROGRESS.md).
Build04 all41/focus01/publish01 exit0, zero warnings/errors. Build02 typo
TickAutoSave в новом UI case сохранён как failed evidence, исправлен в build03.
Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`, App `72c00770ab44beefb8b945d1722d0145e83353dfe3f27fa1f4936698d2ca37e8`.
CLI: DKM/KWD/MAP exact observer/no-observer loads; observer interruption closes
input and never reports Done; array/span/byte stream writes, position/ownership;
DKM/MAP partial-write/flush failures retain old target/history and remove staging.
Полный1822 PASS/63 прежних отказа, exit1. Все1821 прежних PASS сохранились по
порядку, Failures/Inputs совпали точно. Исходный94-list сопоставлен:31 ранее
исправлены,63 прежние; уточнение XXLevel14 wMainTrigger52 сохранено, новых нет.
EN/RU2696 keys:20 новых,2676 прежних literals сохранены. WindowsSmoke добавлен,
только скомпилирован. Физические GUI/dialogs/audio/DPI/game не запускались.
Source/Binaries/fixtures/15WAV/прежние7TestMap bytes сохранены. Все42 Config fields
имеют конкретную маршрутизацию либо неактивность; остаточная полнота MapViewFlags
остаётся I13/I17. Остаток I17 — конечная сверка manager commands, single-list
membership, pending field/menu/shortcut routes; весь I17 не закрыт.17 I/23 M
группы остаются конечным перечнем, цель не завершена.

## ManagerListCommands-20261018

Внутри I17 исправлены3 list-init/whole-map routes: открытие выбирает текущее
выделение; без него scope switch отключён и показана карта. WholeMap теперь
отсекает координаты вне0..W−1/0..H−1, не удаляя raw records из документа.
Selection/family predicates вынесены в канонический pure App helper.
Три source команды «Правка» IDM_CREATURES/DOORSTRAPS/OBJECTS подключены к
общему workspace factory с явной family; toolbar использует тот же route.
Нет выдуманных accelerators. Entry commit/live-pen/closed-owner guards сохранены.
Конечная таблица Things_Manager controls и single-list membership разобраны:
normal source multi move сначала unlink через Undo_StoreMoveThing, затем add;
modern physical projection не дедуплицирует UID и не склеивает разные records.
R02/I14, сценарные scope policies, Core writer/Undo не менялись.
[Сопоставление и ручная приёмка](MANAGER_LIST_COMMANDS.md).
Build02 all41/focus01/publish01 exit0, zero warnings/errors. Новый pure focus
прошёл: WholeMap/selection boundaries, invalid imported coordinates,
empty/missing/overflow-safe selection и unique family пяти kinds.
WindowsSmoke добавлен для actual menu/factory, defaults, reopening, exclusion
без потери raw data, EN/RU, live pen и закрытого owner; прежние initial-scope
ожидания ObjectManagerSmoke обновлены. WPF cases только скомпилированы.
App `1262e270626a02113bafcc04d20f845daf46bacdedf11da28fb4bc75f8c1d908`. Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`
неизменна; full corpus/старые checks не повторялись. Последний существующий
full1822/63 — DiskProgress, на той же Core. Новый focus не является full1823.
EN/RU2697 keys:1 новый, все2696 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. GUI/audio/game/DPI
не запускались. Thing list/entry routes закрыты по коду; I17 целиком открыт.
Следующие конкретные menu callers: HEROS, AREAS/GATES/EFFECTS, PATHS, EVENTS
и их own command/pending-field routes. Все17 I/23 M scopes остаются в перечне.

## WorkspaceManagerRoutes-20261018

Подключены шесть source Edit-команд HEROS/AREAS/GATES/EFFECTS/PATHS/EVENTS
к существующим полным редакторам. Стабильная пустая категория отрядов,
правильный Band template, списки регионов по виду и «Все виды областей»,
отключённые действия пустой строки; патруль без областей только сообщает
причину, сценарный вход выбирает глобальный контекст32 без новой привязки.
Общие CommitFields/pen/same-owner/closed-window guards. Навигация не является
операцией карты. [Поведение, source mapping и ручные шаги](WORKSPACE_MANAGER_ROUTES.md).
Build03 all41 и publish01 exit0, zero warnings/errors. Build02 имел2 ошибки
только нового smoke (Selection у workspace вместо Canvas); исправлены,
неуспешный лог сохранён. Четыре новых WPF cases только скомпилированы,
UI/renderer/game не запускались. I14 остаётся закрытым решением пользователя.
App `893a238fbb3b25eee35b6eb6fdf65a6c594ec6ef9e91e4b5ec72b59ed1b6acb7`; Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`
не менялась. Full corpus и прежние CLI-focus не повторялись: последний
фактический full1822/63 — DiskProgress на той же Core. EN/RU2705 keys,
8 новых на язык, все2697 прежних literals сохранены. Source/Binaries/fixtures,
15WAV и7TestMap bytes сохранены. I17 целиком открыт: остаточная полнота
собственных команд/callbacks и MapViewFlags входит в прежний конечный список,
подключённые девять menu entries повторно не ставятся на разработку.
Все17 I и23 M пунктов остаются; цель в целом не завершена.

## SourceMenuInventory-20261018

Закончен конечный inventory входов Window_Command:70 явных IDM cases,
8 диапазонов и wall/claimed pair. [Полная таблица и конкретный остаток](MAIN_MENU_SOURCE_MAPPING.md).
Это сопоставление входов, не подмена полноты внутренних callbacks I02/I07/I10/I11.
Из остатка I13/I17 снят состав/битовые маршруты всех15 MapViewFlags:
значения/defaults, EN/RU, restore/capture и render consumers сопоставлены.
Исправлен доступ при скрытом toolbar: все15 переключателей теперь также
в постоянном меню «Вид», используют прежние Configure* и прежние settings.
Нет второго состояния, записи карты или нового Undo. Actual rendering/DPI — M21/M23.
Остались8 конкретных source IDs главных команд: UNDOFREE (очистить без
отключения tracking), пять MODIFY* входов настройки кисти по виду, HELPCONTENT
и HELPVERSIONS (сводка девяти section versions). Они остаются внутри I17;
новые верхнеуровневые задачи не создаются. Собственные manager callbacks и
shortcut caller completeness не закрыты одним перечнем menu entry points.
Build02 all41/publish02 exit0, zero warnings/errors. Build01/publish01 тоже
успешны; build02 убрал лишний Separator в новой части меню. Core `73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`
не менялась; App `8aba7df588f20b350deea690d9aa1eb7812a60d57dbdf1fb7b702c77c0d645ef`. Full corpus,
прежние CLI checks и WPF не запускались. Последний full1822/63 — DiskProgress
на той же Core. Tests/ не менялись; для простого обратимого routing новые
зеркальные tests не писались. Все2705 EN/RU literals, Source/Binaries/fixtures,
15WAV и7TestMap bytes сохранены. Цель активна,17 I и23 M scopes остаются.
Следующий конкретный дефект по коду — source UNDOFREE с сохранением depth,
счётчиков, live bytes и save state; не использовать SetHistoryDepth(0) как подмену.

## HistoryClear-20261018

IDM_UNDOFREE подключён как «Правка → Очистить историю». Отдельный ClearHistory
сохраняет bytes карты, dirty/save baseline, ChangeCount/EditorChangeCount,
EditRevision и HistoryDepth; очищает оба стека и snapshot cost. Следующее
изменение получает Undo по прежней глубине. Empty clear — no-op; active pen
и transaction отклоняются; current-owner/closed-window guards в MainWindow.
Один HistoryChanged, без Changed и без записи settings. Старые методы Core
не менялись — проверено сравнением с packaged source после удаления только
нового property/method. [Поведение, source mapping и ручные шаги](HISTORY_CLEAR.md).
Build02 all41/focus02/publish01 exit0, zero warnings/errors. Первый focus
ошибочно считал новую карту чистой; fixture изменён на save/reload. Исходный
failure сохранён, продукт из-за него не менялся. Focus проверяет native/project,
mixed/redo-only, exact bytes/counters/depth/dirty/revision, future Undo,
transaction rollback, active pen, immutable snapshots, observer exception и
manager-local Undo authority после очистки. Новые4 WPF cases только compiled.
Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935`, App `7ea314d2d34d0f76f0e8e7979c1617eddf07058e8f561e17b4cd4844a6b2fc75`.
Это additive API, ни один существующий Core caller/read/write/history replay
не переписан; новый focus покрывает затронутые границы. Полный корпус не
повторялся. Последний фактический full1822/63 — DiskProgress на прежней Core
`73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`; он не выдаётся за новый full.
Следующая обоснованная общая интеграция должна отдельно включить новые checks
manager-list и history-clear; полный PASS count сейчас не увеличивается.
Все2705 прежних literals EN/RU сохранены, новый History.Clear, итого2706/язык.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. I17 целиком открыт:
в таблице главных команд осталось7 IDs — пять MODIFY* brush-kind callers,
HELPCONTENT и HELPVERSIONS; внутренние callbacks/shortcut completeness остаются.
17 I/23 M верхних scope не переименованы. UI/renderer/game не запускались.
Предыдущий goal turn — progress; цель в целом активна.

## ObjectBrushCommands-20261018

Подключены пять MODIFY* команд: «Правка → Настройка кисти объектов».
Явный kind не зависит от Inspector. Прежний ObjectBatchWindow получил
template-only mode: locked family/private copy, без placement preview/coords,
accept выбирает PlaceObject, Cancel не публикует. Шаблоны пяти видов независимы.
Обычный single-placement workspace editor также получает copy; успешный Apply
и UseBrush публикуют принятый template, Cancel больше не меняет source defaults.
Manager multi-session не переписана. Map bytes/counters/history/selection/buffer
настройкой кисти не записываются; будущий paint использует прежние Core plans.
Документ, live-pen, expectedOwner, pending fields и closed-window guards.
[Source mapping, граница и ручные шаги](OBJECT_BRUSH_COMMANDS.md).
Build03 all41/publish01 exit0, zero warnings/errors. Build01/02 имели один
nullable error только нового smoke; исправлено, failures сохранены. Четыре
EN/RU × native/project WPF cases для всех пяти видов только скомпилированы;
GUI/renderer/game не запускались. Зеркальные CLI tests для UI routing не добавлялись.
App `fec04c195aca4a3d35847c5ea7c3ca1ad6790d1ec9c2066f8f1eb11608afa482`; Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935`
не менялась, прежний history-clear focus не повторялся. Последний фактический
full1822/63 — DiskProgress на более ранней Core
`73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`, не новый результат этой сборки.
Core writer/reader/history/painting не изменены, full corpus не повторялся.
EN/RU2708 keys:2 новых, все2706 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. В главном dispatch
I17 остались2 IDs: HELPCONTENT и HELPVERSIONS. Собственные callbacks менеджеров
и shortcut completeness остаются в прежнем I17;17 I/23 M верхних scope не закрыты.
Предыдущий goal turn — progress; вся цель активна.

## HelpSectionVersions-20261018

Подключены IDM_HELPCONTENT и IDM_HELPVERSIONS. Справка открывает актуальный
RU/EN USER_GUIDE внутри приложения из shipped Documentation или canonical docs;
local links/heading/inline formatting, missing/failure notices, live language,
modeless owner lifecycle; shell/browser не требуется. Сводка9 source sections
сверяет literal signatures/current versions, раздельно показывает входной
контейнер и working headers, generated-only/old/new/duplicate/malformed,
native KWD/KLD N/A без угадывания. Это read-only без record parsing/upgrade.
[Поведение, source mapping и ручные шаги](HELP_SECTION_VERSIONS.md).
Build03 all41/focus02/publish01 exit0, zero warnings/errors. Focus01 тоже прошёл;
после него добавлен guard короткого рабочего header и независимый case,
поэтому выполнен focus02. Четыре WPF cases только скомпилированы.
App `8524b2c01ef522f864a567d12e70e8f22b2648ab3573ab67062cd73499a5fe59`; Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935`
не менялась. Старые CLI checks/full corpus/WPF/renderer/game не запускались.
Последний full1822/63 — исторический DiskProgress на прежней Core
`73ecea159e7d4a6d298307ad44d9c20a873f4650335e5d196db6542665b5f38b`, не новый full этой сборки.
EN/RU2736 keys:28 новых, все2708 прежних literals сохранены.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены. В главном Window_Command
не осталось неподключённых входов:70 IDs имеют route либо source Find stub;
8 range dispatch и wall/claimed mapping сохранены. I17 целиком открыт:
конечная сверка own commands/callbacks managers и keyboard callers ещё нужна;
она не заменяется перечнем primary entries.17 I/23 M scopes остаются.
Предыдущий goal turn — progress; вся цель активна.

## SourceKeyboard-20261018

Составлен конечный source inventory30 menu chords и special keys. Подключены
19 бесконфликтных комбинаций: source New/Compile/Test/Help, повороты/vertical
reflection,7 drawing tools,5 explicit placement kinds. Первое placement chord
выбирает принятый/default template, повторный тот же вид открывает настройку,
как Toolbar_ChangeMiscMode. Real PreviewKeyDown сохраняет text-focus gate;
live-pen/current-owner/closed guards. [Таблица и ручные шаги](SOURCE_KEYBOARD.md).
Два конфликта Ctrl+N/New vs source SelectNone и Ctrl+Y/Redo vs source horizontal
reflection заданы пользователю, ответа пока нет; прежние команды не заменены.
3 Insert callers и8 scroll families остаются конкретным keyboard остатком I17;
самостоятельный WPF scroll не объявлен source parity. Внутренние manager callbacks
сохраняются отдельно; primary dispatch70 уже сопоставлен предыдущими этапами.
Build02 all41/focus01/publish01 exit0, zero warnings/errors. Build01 неверно
называл DrawingTool.Pen вместо Brush; исправлено, failed evidence сохранён.
Pure linked-App focus19 routes прошёл;4 WPF cases только compiled.
Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935` не менялась, App `0b64ed2fb0d6a0e90ff69b8dcbc60a4d485208dcaa57cdcb73ba67e3038e573e`.
Corpus и старые CLI/WPF/renderer/game не запускались. Последний full1822/63
остаётся историческим DiskProgress на прежней Core.2736 EN/RU literals без изменений.
Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены.17 I/23 M scopes открыты;
вся цель активна, предыдущий goal turn — progress.

## SpecialKeyboard-20261018

Подключены3 Insert callers и8 scroll families исходного Keyboard_ProcessSpecialKeys.
Insert окна имеют source close-as-accept: в RC нет Cancel, IDOK/IDCANCEL собирают
маски0xF9/0x0F либо objective/area/enemy. Только Creature template; активный tool
не меняется, карта/Undo не пишется. Stale/doc replacement/unload → Abort без записи.
Полный brush dialog сохраняет настоящий Cancel, не смешан с quick windows.
Scroll использует source integer origin/page=floor(viewport/cell)+1/end clamp;
пустой margin допускает последний source origin при неполной клетке, Canvas tile
Width/Height не меняются. Pointer refresh вызывает прежний MoveMapPointer;
при живом pen прокрутка может продолжить прежний штрих одним Undo, без второго pen.
Map focus не даёт перехватывать ввод полей/дерева; Ctrl+Shift special/Alt отказ.
[Source mapping и ручные шаги](SPECIAL_KEYBOARD.md).
Build03 all41/focus01/publish01 exit0, zero warnings/errors. Build02 nullable
error только нового smoke исправлен, failure сохранён. Pure focus покрывает
literal geometry/margins, routes, modifiers, borders,256 option words/high bits,
objective Area clearing и CB_ERR.4 WPF cases только compiled, UI/renderer/game
не запускались. После focus01 менялись только actual App adapters/новый WPF case;
pure helper и linked focus не менялись, зелёный focus повторно не запускался.
Core `44f2ee832a45064b7455189dde2922f0d8be3744ff5ba57b76cbfbdb250f5935` не менялась, App `1a22e5d0c6ccc6edecdc05c907ae0221f388669ad9d8e52568ca549b998343fd`.
Corpus/старые CLI не повторялись. Full1822/63 — исторический DiskProgress на
прежней Core, не новый full этой сборки. EN/RU2740,4 новых/язык, все2736 прежних
literals сохранены. Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены.
В keyboard остатке I17 остаются2 конфликта Ctrl+N/Ctrl+Y с ожидаемым ответом
человека; нынешние New/Redo не заменены и не объявлены согласованными.
Собственные manager/scenario callbacks остаются I17;17 I/23 M scopes открыты.
Предыдущий goal turn — progress; вся цель активна.

## ManagerCommandScope-20261018

Закончен конечный parent command inventory четырёх менеджеров:46 numeric IDs,
11 named notifications. У каждой строки есть current path/symbol и результат,
совпадения не запущены на новый прогон. [Таблица и шесть конкретных различий](MANAGER_COMMAND_SCOPE.md).
Реализован source LVN/TVN Insert/Delete через ScriptTree PreviewKeyDown:
point/event removal uses прежние requests, condition Insert привязан к выбранной
incoming edge, ordered DKM point Insert прежний; native no-list-order отказ не скрыт.
Type мастера берётся из прежнего retained category context. Core получила только
readonly InitialType(signature); доказано сравнением packaged source после
удаления accessor, что все старые Create/Store/reader/writer методы без изменений.
App malformed-graph errors идут через RecordOperation, tabs/current owner,
CommitFields/selection/closed/live-pen guards сохранены. Script parameters I10
и остальные child pages не объявляются закрытыми parent таблицей.
Build03 all41/focus01/publish02 exit0, zero warnings/errors. Build01 test wrongly
referenced internal ScenarioTypeDefaults; заменён независимыми literal source
initial names, failed log сохранён.4 WPF native/project EN/RU cases только compiled.
Core `b381b548d3c1c188442594d1f6302e1021c8c505017fa1ce6d54e20515487138`, App `3b6687b84908adb40d18b2adcc26e97b2fa91aec590cab3c8c78494377226398`.
Full corpus/старые CLI/WPF/renderer/game не запускались. Новый focus проверяет
readonly accessor, initial names, active/closed category retention, invalid
signature и no live bytes/counters/history/revision changes. Исторический
full1822/63 относится к DiskProgress на более ранней Core, не этой сборке.
2740 EN/RU literals без изменений. Source/Binaries/fixtures/15WAV/7TestMap bytes сохранены.
Внутри I17 осталось6 конкретных parent groups MC01–MC06: полный pre-add region
draft; region list double-click; четыре explicit root/append/action insertion
callers; stored expanded tree state; source coordinate clamp; distinct New Point.
Это подпункты прежнего I17, не новая верхняя очередь.2 Ctrl+N/Y конфликта всё
ещё ожидают ответа человека, нынешние New/Redo сохранены.17 I/23 M scopes открыты.
Следующее конкретное исправление — MC01: общий независимый region creation
draft и один принятый Add с прежними fields/options/Undo/counters.
Предыдущий goal turn — progress; вся цель активна.

## RegionCreationDraft-20261018

В I17 закрыт MC01: создание области, прохода или генератора из полного
независимого черновика. [Поведение и ручная инструкция](REGION_CREATION_DRAFT.md).
Общее окно использует прежний RegionPropertiesSession и его geometry/options/
generation children. До Add живые bytes/counters/history/activity не меняются;
принятые параметры и генерация публикуются одной транзакцией/Undo. Окно остаётся
открытым: геометрия, пользовательские name/info, цвета, flags/effects сохраняются,
ID и исходное автоматическое имя обновляются. Генерация сама не повторяется.
Три Add entry points используют этот route. Закрытие отменяет только unadded
draft; уже принятые записи отменяются обычным Undo. Два цвета сохраняются по
категории до конца приложения, без новых JSON fields. Native labels по-прежнему
не хранятся; новый gate type/orientation берётся из окончательной клетки.
Старые Core методы не переписаны; добавлен отдельный RegionCreationSession.
Build01 all41/focus01/publish01 exit0, zero warnings/errors. Проверены literal
полные records, defaults, repeated Add/names, save/reopen, generation, stale/
foreign/raw/closed/active-edit guards, observer failure и injected post-action
history allocation failure с полным rollback и последующим retry.
Полный интеграционный прогон выполнен из-за нового Core writer route:
**1829 PASS /63 прежних отказа**, exit1. Прежние1822 PASS в прежнем
порядке; все63 Failures и Inputs совпали точно.7 накопленных новых групп с
последнего full, включая MC01, добавились отдельно; это не7 новых возможностей.
Исходные94 сопоставлены полностью:31 ранее исправлен, одно прежнее изменение
диагностики XXLevel14/root52. Новых failures/lost passes нет; корпус не зелёный.
Core `4b1d804ce7a46d961b9fa60739268449c2a356e16d9cd1457b637f7306c35cd1`, App `d1363225543c72a355d8b19fcad1e9df9a8366dc40e8eb67ed8bfa0aca8ac532`.
2746 EN/RU keys,6 новых на язык; все прежние literals неизменны. Source/Binaries/
fixtures,15 WAV и TestMap ровно7 файлов сохранены.4 новых WPF cases только
скомпилированы; GUI/Computer Use/renderer/audio device/game не запускались.
MC02–MC06 и2 конфликта Ctrl+N/Y остаются внутри I17; I14 остаётся закрытым.
17 I scopes/23 M groups открыты. Следующий конкретный маршрут — MC02: double-click
именно выбранной строки региона в существующее private Modify окно.
Предыдущий goal turn — no progress (повторная выдача списка); текущий — progress.
Полная цель активна, физическая приёмка M01–M23 не подменяется сборкой.

## RegionListModify-20261018

MC02 закрыт по коду внутри I17: [двойной щелчок строки региона](REGION_LIST_MODIFY.md).
RegionPicker заменён ComboBox→ListBox с двумя полосами прокрутки и явными
читаемыми цветами. MouseDoubleClick проверяет левую кнопку, настоящий row
container/принадлежность/ссылку на текущую проекцию и передаёт именно его запись
в общий с кнопкой OpenRegionProperties. Blank/scroll/right/foreign/stale
отказывают; full record payload, owner/context/revision, live pen и CommitFields
проверяются до/после commit и после смены выбора. Другой открытый черновик не
подменяется: показано новое EN/RU сообщение, значения сохраняются.
Готовы4 новых WPF cases EN/RU×native/DKM; только compiled, не executed.
Также изменены только типы RegionPicker в4 прежних WPF callers.
Build01 failed: helper Control<T> затенил имя WPF Control в новом тесте;
исправлено полное имя класса. Build02 all41/publish01 exit0, zero warnings/errors.
Core `4b1d804ce7a46d961b9fa60739268449c2a356e16d9cd1457b637f7306c35cd1` без изменений. App `20041aa845510ccd32f2ff3c8fe4297fac8d8ea600dc632fb3e8e8e8927da2f3`.
2747 EN/RU keys,1 новый на язык; прежние literals, Source/Binaries/fixtures,
TestMap7 и15WAV bytes сохранены. Новых CLI/full/WPF/renderer/game/audio прогонов
нет. Последний full1829/63 RegionCreationDraft на той же Core сохраняется
в фактическом объёме; GUI этой поставки им не проверен.
MC01 иMC02 закрыты по коду; MC03–MC06 и2 Ctrl+N/Y conflicts остаются внутри I17.
17 I scopes/23 M groups сохраняются. Physical M12/M20/M21/M23 открыты.
Следующий маршрут — MC03: четыре explicit NewRoot/AppendCondition/AppendAction/
InsertAction callers и их source tail/incoming destinations к общему wizard.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## ScenarioInsertionCommands-20261018

MC03 закрыт по коду внутри I17: [пять явных сценарных команд](SCENARIO_INSERTION_COMMANDS.md).
NewRoot append к хвосту выбранного point root; AppendCondition/AppendAction под
condition идут к хвосту его children, под action — к хвосту его siblings;
InsertCondition/InsertAction используют incoming выбранного occurrence.
Источник IDs122/123/124/127/128, Trigger/ActionSave TVI_LAST и SelectLink.
Сохраняется source запрет действия перед корневым условием. Новый корень не
подменяет New Point (MC06). Shared graph сохраняет actual visual occurrence,
не ищет произвольный unique parent. Read-only resolver проверяет current root,
path/preceding edges и хвосты, cycle/dangling/duplicate/zero/foreign/stale.
App source/context/revision/resources/selection/CommitFields/pen guards;
уже открытый wizard сохраняется с EN/RU notice. Существующий общий мастер,
writer, aliases, repeat, rectangle batching и orphan generic route сохранены.
Core добавлена только новая функция чтения и enum; все прежние Core files
побайтово совпали с предыдущим source package. Новая focus04 проверяет26
native/DKM destination/insertion cases, полные bytes прежних events с packed
offsets, Cancel, source count/Undo/Redo и malformed/shared/empty boundaries.
Build04 all41/focus04/publish01 exit0, zero warnings/errors. Build01 имел ошибку
только нового WPF test: ushort CreatedEventID принят за nullable. Focus01 test
сравнивал UsageOfUndo с началом серии после реального Undo; исправлен snapshot
перед resolver. Focus02/03 выявили неверный test oracle: после вставки DKM
condition старый ACTION offset уже указывает другую запись. Идентичности и
incoming source IDs теперь фиксируются до insertion; byte assertions сохранены.
Все failed logs сохранены.4 WPF EN/RU×native/DKM cases только compiled.
Core `7f530dc5997406195e4f6fb5be557bd6eabc8a50f48d7e203b831232dd18f2ed`, App `cdfe901eeb06cda580fdffd95125fb2626eca1a35b1ca9cdfec53bc5d80278f5`.
2754 EN/RU keys,7 новых на язык; прежние literals, Source/Binaries/fixtures,
15WAV/TestMap7 bytes неизменны. Full corpus/старые checks/GUI/Computer Use/
renderer/audio device/game не запускались. Последний full1829/63 относится
к прежней Core RegionCreationDraft; новый readonly API проверен focus04,
не выдаётся за full новой DLL. Нового reader/writer behavior нет.
18 октября человек ответил «оставить как у нас»: Ctrl+N = новая карта,
Ctrl+Y = Redo. Оба конфликта закрыты, code этих сочетаний не менялся;
source clear-selection/horizontal reflection остаются в меню.
MC01/MC02/MC03 закрыты по коду; MC04–MC06 остаются внутри I17.
17 I scopes/23 M groups открыты, полная цель активна. Следующее — MC04:
сохранённое раскрытие condition tree, с проверенным DKM/native layout.
Предыдущий goal turn — progress; текущий — progress.

## ScenarioExpansion-20261018

MC04 закрыт по коду: [сохранённое раскрытие условий](SCENARIO_EXPANSION.md).
TRIGGER56/expanded short14 подтверждены исходником; KTRG2/3 read/write, любое
ненулевое imported значение читается без нормализации. Source TVN_ITEMEXPANDED
не создаёт MapChanges/Undo: наш setter также не создаёт command count, revision,
history/Changed/activity. HasChanges игнорирует только подтверждённые2 байта
условия в bounded KTRG2/3, прочие параметры/links/opaque bytes остаются dirty.
Native24 не имеет expanded: хранится только состояние документа по condition
ID, клон независим; native→DKM переносит true в short14 до remap IDs. Native
game/alias bytes не меняются, новый companion не создаётся. Save fingerprints
учитывают presentation и отклоняют stale preflight. KWD fresh load имеетfalse,
рабочий DKM сохраняет флаг. Дерево использует TwoWay IsExpanded, generation/
owner/revision/closed guards и не раскрывает свёрнутый путь ради старого выбора.
build-03 all41/focus-03/publish01 exit0, zero warnings/errors. Новый focus
проверяет exact2-byte delta/noop, no command/history/events/activity, clone,
Save/Conversion staleness, DKM/native→DKM save/reopen, nonzero/malformed/zero/
active-edit guards, dirty-state exclusion и весь официальный семифайловый
экспорт до/после переключения флагов: bytes совпали.4 WPF cases только compiled.
Первый focus имел ошибочную границу нового теста: Begin/Dispose pen испускает
собственные availability callbacks. Следующий focus ожидал KTRG restoration
от Undo terrain, хотя его delta содержит только tiles. Тест теперь проверяет
отказ setter внутри pen lifetime и Undo правки самого condition record;
byte/count/activity assertions сохранены, failed logs не удалены.
Полный прогон обоснован изменениями Core Save guard/conversion/dirty logic:
**1831 PASS/63 прежних отказа**, exit1. Прежние1829 PASS в прежнем порядке,
все63 Failures/Inputs совпали точно.2 добавленные группы: MC03 и MC04.
Исходные94 сопоставлены полностью:31 ранее исправлен, одно прежнее уточнение
XXLevel14/root52. Новых failures/утраченных PASS нет; корпус не зелёный.
Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`, App `18dfc9ee014005ae842b6b4b200560a3b05ced6098eb5c6bb41e88b5316cd88b`.
2754 EN/RU keys без изменений; Source/Binaries/fixtures/TestMap7/15WAV сохранены.
GUI/Computer Use/renderer/audio device/game не запускались. MC01–MC04 закрыты
по коду, MC05/MC06 остаются в I17. Ctrl+N = новая карта, Ctrl+Y = Redo сохраняются
по явному выбору пользователя.17 I scopes/23 M groups открыты; вся цель активна.
Следующий маршрут — MC05: source coordinate clamp в private region draft.
Предыдущий goal turn — progress; текущий — progress.

## RegionCoordinateClamp-20261018

MC05 закрыт по коду в I17: [координаты и стрелки](REGION_COORDINATE_CLAMP.md).
Общие creation/modify поля101–104 немедленно ограничивают X/Y отдельными
Width−1/Height−1. RepeatButton и Up/Down дают шаг1; gate endpoints и их
стрелки disabled, X2/Y2 следуют X1/Y1. ES_NUMBER typing filter, unsigned
decimal prefix/UINT→LONG/overflow и промежуточное пустое поле сопоставлены.
Меняются только поля окна, не импортированные bytes. Owner/current/pick/child
guards, независимые sessions и существующий атомарный Apply/Undo сохранены.
Build01/02 all41 exit0 без warnings/errors. Focus01 exit0: границы/шаг/prefix,
отдельные размеры X/Y, native/DKM×3 kinds private Cancel/Apply/Undo/Redo.
После focus добавлен только ES_NUMBER WPF typing filter и compiled assertion;
чистая policy/Core/CLI checks не менялись, успешный focus не повторялся.
Publish01/02 exit0; окончательная поставка соответствует build02/publish02.
4 WPF EN/RU×native/DKM suites (в каждой create/modify×3 kinds) только compiled.
Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347` и все Core sources без изменений; App `9ece9079bae761e44bd872fe8ecd35f5e80e306dda00360ecd68dd5062c44f54`.
Последний фактический full этой Core — ScenarioExpansion1831 PASS/63 прежних
отказа, original94 сопоставлен целиком. Full не повторялся, новый focus не
прибавлен к историческому full. GUI этой App им не проверен.
2756 EN/RU keys,2 новых на язык; прежние literals/Source/Binaries/fixtures,
TestMap7/15WAV bytes сохранены. Редактор/GUI/Computer Use/renderer/audio/game
не запускались. MC01–MC05 закрыты по коду, parent остаток MC06. I17 остаётся
открытым;17 I scopes/23 M groups и все23 FEATURE_PARITY сохранены. I14 и
Ctrl+N=новая карта/Ctrl+Y=Redo не переоткрываются. Полная цель активна.
Следующий маршрут — MC06: отдельный безусловный New Point, не OpenScope.
Предыдущий goal turn — progress; текущий — progress.

## ScenarioNewPointCommand-20261018

MC06 закрыт по коду: [отдельная команда New Point](SCENARIO_NEW_POINT_COMMAND.md).
Source103 Modify=FALSE/Insert=FALSE всегда открывает private type page, не
выбирает уже существующий root. Open existing/Insert/Modify/New Root сохранены.
Default Player и retained type после Cancel принадлежат workspace context;
смена/выгрузка документа сбрасывает тип. New без Existing/InsertBefore и
preferred old owner, новый trigger0. Existing wizard сохраняется с EN/RU notice.
Loaded/tab/owner/context/revision/pen/CommitFields guards; shared Finish
проверяет lifetime, закрытый context не пишет новую карту.
Build01/02 all41/focus01/publish01 exit0, warnings/errors0. Focus проверяет
private Cancel/preflight, две дополнительные DKM player points с существующим
Level, append/exact previous point+event bytes, empty target, count/Undo/Redo,
occupied level/player refusal. Это модельный контракт общего мастера;
actual New button и четыре WPF EN/RU×native/DKM suites только compiled.
После focus добавлен только loaded-workspace guard App, чистые Core/CLI
не менялись; успешный focus не повторялся. Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347` и все его sources
неизменны; App `87d5afa9a7e8941fa5453f7257cd0dd6fb7a3be0b7106d73bf9f21c34f205e9d`. Full не повторялся: последний фактический full той же
Core — ScenarioExpansion1831 PASS/63 прежних отказа, original94 сопоставлен.
Новый focus не добавлен к историческому full. Source/Binaries/fixtures,
TestMap7/15WAV bytes сохранены;2758 EN/RU keys,2 новых на язык, старые literals
сохранены. Редактор/GUI/Computer Use/renderer/audio device/game не запускались.
MC01–MC06 конечной parent table закрыты по коду. Это не I17 complete: найден
конкретный прежний Things Manager destination callback106/107/186/187 — source
ранний clamp против сохранённого raw input. Source/App тела сопоставлены,
remaining-i17-site.json хранит точные paths/hashes/границы. Следующее — этот
callback в прежнем I17; новый верхний scope/неопределённая очередь не добавлены.
Native no-list-order/empty-root boundary остаётся I01/I03; новая кнопка не
создаёт придуманный companion. Duplicate Level/player не разрешён исходником;
additional global DKM points означают других players, а не дубли одного owner.
17 I scopes/23 M groups и все23 FEATURE_PARITY сохранены. I14 и согласованные
Ctrl+N=новая карта/Ctrl+Y=Redo не переоткрываются. Полная цель активна.
Предыдущий goal turn — progress; текущий — progress.

## ManagerDestinationCoordinates-20261018

Исправлен последний установленный Thing Manager numeric callback I17:
[destination106/107 и spinners186/187](MANAGER_DESTINATION_COORDINATES.md).
Общая прежняя RegionCoordinateRules даёт immediate X/Y bounds и шаг1;
typing ES_NUMBER, empty/prefix/UINT принимаются по source numeric правилам.
Промежуточный raw text остаётся в поле, session хранит число. Add/show используют
общую принятую точку, smaller-map BoundPosition ограничивает оба измерения.
Private picker Cancel не пишет session; Confirm после token/owner/revision
явно публикует его. Old callbacks блокируются navigating/busy/request/owner.
ObjectBatchSession вынесен без изменения тела в один канонический файл;
настоящие App session models linked в CLI без WPF. Второй модели нет.
Build01 failed только в новом тесте: PlacedThings не существует, linked unused
fields CS0649. Тест использует ObjectManagerEntries и задаёт/проверяет эти
реальные settings, warnings не подавлены, журнал сохранён. Build02 all41,
focus01 и publish01 exit0, warnings/errors0. Focus: canonical session axes,
empty/prefix/UINT/smaller-map, unchanged options/templates/requests, workspace
isolation и native/DKM Add planning/no bytes/event/revision, atomic Add/Undo.
4 WPF EN/RU×native/DKM suites и старый обновлённый navigation case только
compiled. Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347` и все его sources неизменны; App `7ed36da9124001c295d25dc7a696dce3480f8ef0b9d98795062a3236590ba246`.
Full не повторялся, старые green checks не повторялись. Исторический full
той же Core — ScenarioExpansion1831 PASS/63 прежних отказа, original94
сопоставлен полностью; новый focus к нему не добавлен. Source/Binaries/fixtures,
TestMap7/15WAV bytes сохранены;2758 EN/RU keys/literals без изменений.
Редактор/GUI/Computer Use/renderer/audio device/game не запускались.
Конечная кодовая сверка [I17 закрыта](I17_CODE_CLOSURE.md):42 Config fields,
70 primary IDs+8ranges/wall pair,15 MapViewFlags,30 chords+3Insert+8scroll,
Thing Manager controls/membership,46 parent rows+11 notifications,MC01–MC06,
R01–R05 и явные human Undo/keyboard решения. i17-closure-audit.json привязывает
все inventories/current tables/source hashes/последние build/focus/publish.
Физическая приёмка остаётся в M, границы native I01/I03, nested I10–I13,
исторических layouts/IDs I04/I18, compiler I02, counters I15/activity I16
сохраняются. Теперь16 I scopes/23 M groups=39 открытых пунктов, не39 bugs.
Все23 FEATURE_PARITY сохранены. I14 и Ctrl+N/New/Ctrl+Y/Redo не переоткрыты.
Следующее — I12: level modifiers определения существа, отдельные от модификаторов
видов в свойствах карты; закончить конечное source/control/storage сопоставление.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifiers-20261018

I12 конкретизирован по настоящему официальному editor binary:
[проценты характеристик по уровням](CREATURE_LEVEL_MODIFIERS.md).
Доступный C editor не имеет этой таблицы в GLOBALCREATURE/9 pages;
она не равна4 map-species modifiers и не Shot1/2/3Level. Статически прочитаны
обычный/Pro EXE resources: dialogs213/238/318, fields1851/53…1867 и spin
1852/54…1868 для уровней2–10. Оба EXE только читались, не запускались.
GlobalVariables.kwd: normal type0x41,32 stats×9 levels=288 literal records;
0xE0 —32 отдельных possession tuples на10 уровне. Layout16 bytes:
type/statistic/unsigned-percent-bits/level. Declared section9992, actual13636;
opaque trailer3644 не сканируется как records. Первый inspection неверно
ожидал равенства file length и первого section; граница исправлена без записи
исходного файла. Raw resource/shared snapshots и hashes сохранены.
Добавлен только новый read-only Core API: bounded version4/header/count,
ordered occurrences/duplicates/unknowns/unsigned bits, отдельные SharedDefaults/
MapVariables/OtherCollection/DKM RetainedNativeSource. Никакого last-wins,
guessed default/перенумерации/правки bytes/count/revision/history/events.
Все прежние Core/App files byte-exact относительно baseline, generic schema,
Save/compiler/clone и provenance guard неизменны. Корректный writer должен
обойтись без ослабления immutable-source validation; его ещё нет.
Build01/02 all41/validationbuild01/publish01 exit0, warnings/errors0. Focus01
failed только из-за нового literal fixture: generic schema намеренно не имеет
Unused полей. Fixture использует свои явные bytes, product не менялся. Focus02
exit0: literal/truncation/headers/opaque trailer/unknown/duplicate/possession,
real32×9+32, map и DKM retained, без source mutation/events. Failed log сохранён.
Core `636efcf900e58cad541077cf33d809bb0849bf7fce791f6acd8add719b669c32`, App `c1d828b6e01860f186005cedefaffe993555b0802f9e3c366e63d300fd6a3558`. Нового WPF case/GUI/renderer/game/audio device/
Computer Use не было.2758 EN/RU keys/literals, Source/Binaries/fixtures,
TestMap7/15WAV сохранены. Full/старые green checks не повторялись: только новый
readonly API; старый full1831/63 относится к прежней Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`,
не к новой DLL. Original94 сопоставлен в том full, результаты не смешиваются.
I12 остаётся открытым: official callback/range/default/type mapping, частный
EN/RU draft/writer с общим Apply/Undo и DKM source-provenance/save/reopen/
семифайловая компиляция. Это конечные части прежнего I12, не новая очередь.
I14/I17 закрыты,16 I scopes/23 M groups и все23 FEATURE_PARITY сохранены.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifierEditing-20261018 — 19 октября

I12 продвинут до [официальных controls и частного draft](CREATURE_LEVEL_MODIFIER_DRAFT.md).
Обычный DK2 Editor.exe исследован статически Capstone5.0.9 в evidence-only
analysis-deps; EXE/GUI не запускался. MFC maps1841/1842→0046c520: storePrevious
0046c550, loadNext0046c680; DDX0046bf90 вызывает signed DDV004e4b31 десять раз
с−10000/10000. Store WORD/load movsx SHORT,32 catalogue entries,9 excluded IDs,
23 visible captions/order подтверждены direct binary references и Core names.
Corrected resource mapping: level2 Edit1847, levels3–10 Edit1851…1865 odd,
FirstPerson1867; прошлый порядок resource entries был ошибочно принят за
порядок levels. Read decoder wire layout от этого не зависел, не менялся.
Имеется отдельное отличие old Cancel: ранее посещённые selections могли уже
писать global arrays. Новый independent draft не выдаётся за доказательство
старых side effects; общий частный Apply ещё не подключён.
Новый CreatureLevelModifierDraft хранит230 private cells, исходные DWORD bits
и signed SHORT view. Duplicate occurrences читаются последовательно как loader;
hidden IDs не редактируются. Все10 values/shape/range проверяются до публикации,
invalid transition не меняет selection/values. No-op не нормализует upper bits,
imported outside GUI range читается без ремонта, incomplete source и closed
draft отвергаются. Нет owner/writer/mutable archive ссылки.
Build01 all41/focus01/publish01 exit0, warnings/errors0. Focus проверяет23 IDs,
ENRU/corrected controls/signed bit patterns/230 values/selection, все10 slots
с−10000/10000 и внешними границами, immutable caller buffers/noop/duplicates/
missing/closed. Нового WPF case/GUI/renderer/Computer Use/game/audio device нет.
Core `e461ec4adb6fd49487e8a7d51864c125a4c00f717d5ff5e1636b22ca0cfc53b9`, App `c4e15526f24282b325651fb2c27741073002c767d653cc320c5e29eec9813918`. Только новый независимый Core file и2 test files;
все прежние Core/App sources/2758 resource literals/Source/Binaries/fixtures/
TestMap7/15WAV bytes сохранены. Full/прежние green checks не повторялись.
Исторический full1831/63 — ScenarioExpansionCore `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`,
не текущая DLL; original94 сопоставлен там, новые focus counts не прибавлены.
I12 открыт: effective shared/map composition/default/reset, native signed
DWORD writer/omission, mutable DKM delta отдельно от immutable archive с
history/fingerprint/clone/Save/reopen, EN/RU UI/common Apply и7-file compile.
Provenance validation не ослаблена. Это прежний конечный scope I12.
I14/I17 закрыты;16 I scopes/23 M groups и все23 FEATURE_PARITY сохранены.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifierStorage-20261019

I12: [native collection writer](CREATURE_LEVEL_MODIFIER_WRITER.md) добавлен как
независимое преобразование буфера, без публикации в рабочий документ/архив.
Official004aa6e0/004ab0be..004ab152: SHORT→signed DWORD, zero→100,
288normal+32FirstPerson записываются без omission. Caller0047d0c0 затем
пишетThings; ранее opaque3644 bytes после E6 действительно section0xBE.
Этот путь отличается от packed map Globals0047d510. Все EXE/shared только
читались. PatchCollection сохраняет headers/order/ранние duplicates/hidden/
unknown/extension; пишет только последний edited DWORD, проверяет весь batch
и original bits перед output, не нормализует semantic no-op. Missing keys
без подтверждённого effective-source insertion plan отвергаются.
Build01 all41/focus01/publish01 exit0, warnings/errors0. Новый focus:
literal negative DWORD/zero100/bounds и real23-page draft→write→read,
last duplicate, byte-exact scope, stale/duplicate/missing/invalid batch,
source/output independence и installation SHA. Static check01 неверно обрезал
CALL004ab0fd на004ab0ff; диапазон исправлен до004ab100, failed script сохранён.
Core `bcaaa132f882ae2a87b38e8469c31251a87b2184ee9f671efea7c05783764e59`, App `0030930c793a50259d3c4840acb99dd7a73a48609fa4b0ea075e4ccd3e36a633`. Новый один Core file +2 test files; прежние
Core/App sources/2758 resource literals/Source/Binaries/fixtures/TestMap7/15WAV
сохранены. Full/старые green checks не повторялись: новая независимая API
ещё не вызывается существующими документами. Full1831/63 остаётся историческим
для Core `bfc72c8b581010fa98c058add84ce7225fda918b338ba332933533279bb64347`, original94 сопоставлен там.
I12 открыт: effective shared/map/default/reset, mutable DKM state отдельно от
immutable archive, history/fingerprint/clone/Save/reopen, EN/RU UI/common Apply/
Cancel/Undo/7-file compile и physical acceptance. I14/I17 закрыты,16 I scopes/
23 M groups и все23 FEATURE_PARITY сохранены. Нового WPF case/GUI/game/audio
или Computer Use нет. Предыдущий goal turn — no progress (подтверждён уже
записанный выбор клавиш); текущий — progress. Полная цель активна.

## CreatureLevelModifierSession-20261019

I12: [существующие map tuples подключены к private context и DKM](CREATURE_LEVEL_MODIFIER_SESSION.md).
Native writer меняет только map Variables.kld. DKM хранит отдельныйD2LM v1:
52-byte header/hash исходного retained stream +24-byte records, максимум230.
Проверяютсяlast occurrence/key/original bits/hash/offset/signed value; immutable
ZIP/retained variables и прежний provenance validator не изменены. Внутренний
буфер участвует вTransactionFiles/Undo/Redo/dirty/fingerprint/clone и DKM Save;
в обычных Files/Records/игровых ссылках его нет. Empty delta не сериализуется.
Cancel/no-op не меняют owner; один Apply — один Undo. Save/Save As/recovery/
SaveCopy/project writer/MAP EditableCopy сохраняют необходимый исходныйZIP;
повторное открытие даёт тот же effective результат. Полный rollback после
поздней history allocation failure и outer transaction exception подтверждён.
Сквозное сравнение двух компиляций с фиксированнымDK2Preview_6a88240eaf814f78:
ровно7 файлов, изменились только2 выбранных DWORD вVariables.kld.
Build01 failed только на nullable dictionary key нового test; validationbuild01/02
exit0. Focus01 имел неверный Undo.Count oracle после320 fixture вставок до
history cap; setup очищает только подготовительную историю. Focus02/build02
all41/publish01 exit0, warnings/errors0. Failed logs сохранены.
Full terminal exit1: **1838PASS/63 прежних отказа** на Core
`85a5c8af8cc45821c3575b004bb978da32e4166642e3e40529786b2fcf7e7ec1`; все1831 прежних orderedPASS и63 rawFAIL/Inputs совпали, добавлены7
известных групп с более поздних этапов. Original94 полностью сопоставлен:
31 ранее исправлены,1 прежнее сообщениеXXLevel14 уточнено,63 сохранены.
Protected corpus/source/resolver hashes совпали. Это текущий full, не сумма
focus counters. Нового WPF case/GUI/game/audio device/Computer Use нет.
App `e157861e31c6971d9b1e4040bef4c6d8539539394280bd44160c57763d6ece48`.2758 EN/RU literals, Source/Binaries/fixtures/TestMap7/15WAV
сохранены. I12 **открыт**: shared/map/default/reset и публикация общих каталогов,
missing override insertion, EN/RU parent/child integration и physical acceptance.
Методы этого этапа редактируют существующие tuples, не угадывают отсутствующие
shared values. I14/I17 закрыты;16 I scopes/23 M groups и все23 FEATURE_PARITY
сохранены. Предыдущий goal turn — progress; текущий — progress. Полная цель активна.

## CreatureLevelModifierDefaults-20261019

I12: [shared defaults и недостающие записи подключены в ядре](CREATURE_LEVEL_MODIFIER_DEFAULTS.md).
Каталог берётся из загруженных данных/immutable archive либо явного снимка;
эффективные map values накладываются последними. No-op/Cancel не создают записи.
Private reset копирует9+1 значений captured страницы; это современное явное
действие, в original dialog238 direct Reset отсутствует. Static PE/IAT proof:
1846/1849/1841/1843/1844/1845 скрываются черезShowWindow(0), не EnableWindow;
EXE не запускался. Дополнительная primary-reference OpenKeeper читает refs
по порядку и обновляет level/stat keys; это не actual game execution.
Native добавляет typed16-byte tuples в Variables.kld. DKM хранит новые user
records отдельно вD2LO v1 (16-byte header+16-byte records,max230); они не
выдаются за исходные данные. D2LM/immutable retained bytes/старый provenance
validator сохранены. D2LO проверяетcount/key/range/canonical bits/duplicates/
collision with original source. Fresh DKM не требует выдуманного nativeZIP.
Mixed existing update/new insertion, history/fingerprint/clone/Save/reopen/
recovery/copies и seven-file compile проверены. ParentMapProperties scope
разрешает толькоtyped level DWORD/new rows/D2LM/D2LO; unknown/early duplicates/
shared/source archive защищены. Общий parent Apply остаётся одним Undo.
Focus01/validationbuild01/build01 all41/publish01 exit0, warnings/errors0.
Full terminal exit1: **1839PASS/63 прежних отказа**, Core
`46aaca15d19d50a4c2d9618129eda9886506382d7cf9dfd46f976faa5c7f695e`. Все1838 orderedPASS/63 rawFAIL/Inputs совпали с предыдущим full;
добавлена1 новая группа. Original94 полностью сопоставлен:31 ранее исправлены,
1 прежнее сообщениеXXLevel14 уточнено,63 сохранены. Protected hashes совпали.
Новый late-memory case отказывает после owned insertion и полностью откатывает
owner; все230 visible values сохраняются/компилируются. Старый injection helper
считаетTransactionFiles динамически; прежний отказ MapProperties level edits
заменён intentional private Prepare/общим typed parent Apply.
App `471740122b12613cb838fcc2807709473ec58bc73aba57de4495d554f4b666d6`,2758 EN/RU literals, Source/Binaries/fixtures/TestMap7/15WAV
сохранены. Нового WPF case/GUI/game/audio device/Computer Use нет.
I12 открыт: EN/RU child window вMapProperties.WorkingDocument, выбор source
черезGameResources, явный экспорт копии общего GlobalVariables.kwd, корректные
conversion captions для поддержанных tuples, затем физическая приёмка.
Это конечные части прежнегоI12, не новая очередь. I14/I17 закрыты;16 I scopes/
23 M groups и все23 FEATURE_PARITY сохранены. Предыдущий goal turn — progress;
текущий — progress. Полная цель активна.

## CreatureLevelModifierWindow-20261019

[Окно EN/RU и экспорт копии каталога](CREATURE_LEVEL_MODIFIER_WINDOW.md)
подключены в MapProperties.WorkingDocument.23 statistics/10 signed fields,
spinners ±1/clamp, store-before-selection, private reset, ENRU labels without
rewriting pending strings, source priority/picker, retained catalogue on reopen.
Child Apply пишет только в private parent; pending child блокирует commonApply;
parent Cancel/oneUndo/source lifecycle/две consent revision guards сохранены.
Каталог ограничен64MiB, snapshot independent. Private all230 view экспортируется
через verified writer в выбранную копию: exact hidden/unknown/header/following
section bytes; atomic staging/flush/late owner+input guard; partial I/O сохраняет
прежний target. UI запрещает исходный путь каталога. Экспорт не применяет карту.
Добавлены23 matching ENRU keys, теперь2781; все прежние literals сохранены.
Save.Issue.CreatureLevelModifiers различает2visible valid tuples и4unknown/
hidden/invalid в новой проверке. Неизвестные сохраняют прежний read-only caption.
В inspection исправлен catch: InvalidDataException наследуетSystemException,
неIOException; новый UI path перехватывает его/ошибки доступа, сохраняя контекст.
Build01/02/03/04 all41 exit0; итоговыйbuild04 warnings/errors0; focus02/publish01
exit0. Focus01 до последнего conversion-case тожеpassed, повтор только из-за
изменённого кода/новогоcase. WPF4suite ENRU×native/DKM добавлены/скомпилированы,
**не исполнены**. Ни редактор, ни игра, ни native dialogs, ни Computer Use не
запускались. Статическая route/resource сверка и full привязаны к hashes.
Full terminal exit1: **1840PASS/63 прежних отказа**, Core
`d04a2f1830eb581e05b617fb8b1704c4a60d18e44fbba13ace5e318b57636ef1`. Все1839 orderedPASS/63rawFAIL/Inputs совпали с прошлымfull,
добавлена1 export-group. Original94 полностью сопоставлен:31 ранее исправлены,
1 прежнееXXLevel14 сообщение уточнено,63 сохранены. Protected hashes совпали.
App `9f9105a967e2b59b6c135dc1923b6e8830900f25a2f6a238cc607d840f6ae5fc`.Source/Binaries/fixtures/TestMap7/15WAV не изменены.
Конечная source/control/storage/UI/export сверкаI12 закрыта **по коду**:
[таблица требований](CREATURE_LEVEL_MODIFIER_WINDOW.md) иsource-scope.json.
Физическая приёмка не объявлена успешной: M19/M21/M23 сохраняют23stat fields,
фокус/Tab/Enter/Escape/ENRU/DPI/native dialogs и actual game effect. Это реальные
ручные gates уже существовавшей очереди, не перенос незавершённого кода.
Открыто15I scopes и23M groups=38 работ; все23 FEATURE_PARITY сохранены.
I14/I17 и утверждённые Ctrl+N/New,Ctrl+Y/Redo/полныйUndo сохранены.
ДалееI11: конечное сопоставление оставшихсяglobal callers. I12 tests не повторять
без кода/новогоdefect; ручная инструкция дана пользователю вfocused doc.
Предыдущий goal turn — progress; текущий — progress. Полная цель активна.


CreatureLightCallbacks-20261019: [четыре обработчика ввода света, граница I11 и ручные шаги M19/M21/M23](CREATURE_LIGHT_INPUT_CALLBACKS.md). Кодовая сверка I11 и фактическая приёмка остаются открыты.


CreatureParameterInputs-20261019: [числовые обработчики количества привлечения, следующий Attributes callback и ручные шаги M19/M21/M23](CREATURE_PARAMETER_INPUT_CALLBACKS.md). I11 и физическая приёмка остаются открыты.


CreatureAttributeInputs-20261019: [38 атрибутов, исходные callbacks и ручные шаги M19/M21/M23](CREATURE_ATTRIBUTE_INPUT_CALLBACKS.md). I11 и физическая приёмка остаются открыты.


CreatureAttackInputs-20261019: [16 числовых полей Attacks, исходные шаги, обе панели и ручная приёмка](CREATURE_ATTACK_INPUT_CALLBACKS.md). I11/M19/M21/M23 остаются открыты.


CreatureAuxiliaryInputs-20261019: [семь числовых Possession/CrSpells controls, сохранение controls при шаге и ручная приёмка](CREATURE_AUXILIARY_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


CreatureJobInputs-20261019: [Jobs9 numeric controls/3 shared fields, явный Add/Change и ручная приёмка](CREATURE_JOB_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


GlobalElementInputs-20261019: [33 числовых обработчика Global Elements и сохранение controls при шаге](GLOBAL_ELEMENT_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


ProjectileChildInputs-20261019: [Trigger/shot numeric callbacks, два согласованных исправления и ручные шаги](PROJECTILE_CHILD_INPUT_CALLBACKS.md). I11/M19/M21/M23 открыты.


CreatureResistanceSliders-20261019: [четыре Slider/readout, source None masks, imported raw и ручные шаги](CREATURE_RESISTANCE_SLIDERS.md). I11/M19/M21/M23 открыты.


GlobalCallbackClosure-20261019: [I11 закрыт по коду](I11_CODE_CLOSURE.md);14I/23M остаются. Все23 возможности исходного ledger сохранены; physical UI/game ещё не приняты.


CreatureSpellUsage-20261019: [source list123: какие существа используют заклинание](CREATURE_SPELL_USAGE.md). [Конечная дальнейшая граница I13](I13_CATALOGUE_SCOPE.md); I13/M19/M21/M23 открыты.


ScenarioChoiceImages-20261019: [изображения контекстных списков сценария](SCENARIO_CATALOGUE_IMAGES.md). I13 открыт; вложенный состав отрядов ещё не сопоставлен. Новые WPF suites толькоcompiled.


ScenarioBandPreviews-20261019: [состав отрядов в списках сценария](SCENARIO_BAND_PREVIEWS.md). Пять source contexts реализованы;4 WPF suites толькоcompiled. I13 остаётся открытым по остальным разделам.
