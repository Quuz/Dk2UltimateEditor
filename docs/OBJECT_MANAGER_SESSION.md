# Object-manager session preferences

The original manager's preferences are process-session data, not part of a map. The modern editor now retains them in one `ObjectManagerSession` per workspace across window reopenings and map changes. An independent workspace starts with the original defaults. No registry or JSON persistence is implied for these preferences.

## Source evidence

- `Source/Sources/Editor.c:173–179`: five independent `Manager*` templates and destination X/Y initially 1.
- `Source/Sources/Properties/Editor/Properties_Editor_Config.c:63–74`: position sorting; modification of every property except coordinates; relative creature coordinates; replace-with-question policy; all-bit replacement and DWORD masks `0xFFFFFFFF`; separate addition/modification warning options; single addition and random frequency 2.
- `Source/Includes/Config.h`: one shared `ManagerSubType` for both door/trap and object/special-item managers.
- `Source/Sources/Things/Things_Manager.c`, `Things_ManagerInit`: restore sorting, templates, subtype and coordinates; clamp saved coordinates to the new map's last tile.
- `Things_ManagerModify`: multiple selected records use the global family template, not the first selected record's values; a single selected record uses its actual properties. Door/trap and object/special-item movement force relative coordinates without resetting the creature preference.
- `Things_ManagerModifyInit` / `Things_ManagerModifySave`: family-specific visible options update only their relevant fields; hidden family masks remain intact.
- `Source/Sources/Properties/Editor/Properties_Editor_Registry.c`: recent files, safe flag and game path are persisted; manager settings are not.

## Implemented behavior

Addition and multi-edit share the five full template drafts, including raw invalid input retained for repair. Their warning flags, replacement policies and operation modes are separate. The manager also retains its family, sort, search and selection-only filter; addition retains mode, whole-map scope and frequency. Coordinates are shared between batch addition and multi-edit and valid out-of-range coordinates are bounded when opening a smaller map. Invalid coordinate text remains visible rather than becoming zero.

Modification retains property masks, the creature coordinate mode, both bit-operation modes and both unsigned 32-bit mask drafts. Opening a noncreature family does not overwrite hidden creature settings. A random addition mode cannot disable multi-edit coordinates. Editing one actual object uses a separate local draft and cannot overwrite the shared template or multi-edit options. Single painting/brush sessions remain independent of the manager preferences.

Only value drafts are retained. Selected records, removal/modification plans, candidate decisions, scenario inclusion, scenario detachment, historical-upgrade consent and application confirmations are not carried into another window or document. Each operation must build a fresh document-bound preview and obtain current explicit consent. Changing preferences does not edit map bytes or create undo entries; document replacement revokes old windows and preserves the copy buffer.

The combined modern options/preview window deliberately keeps canceled value drafts, while Cancel still makes no map edits. The original separate modification-options modal copies its options back only after OK (`Things_ManagerModifyOptions`). This is a documented UI difference, not a claim of identical cancellation behavior. Likewise, retaining Ask Options / Preview flags does not suppress modern explicit application confirmation or destructive scenario review.

## Verification

`ObjectSessionSmoke.cs` exercises actual controls in EN/RU and native/project formats: source first-run defaults; shared versus actual templates; full/raw/high-bit drafts; separate option sets; hidden preferences; shared pair subtype; invalid-mask rejection; live translation; fresh consent; actual health-only group application and byte-exact undo; smaller-map clamps; stale-window rejection; independent workspaces; unchanged input maps/history/copy buffers. Four focused cases pass in both Debug and Release with zero-warning/error builds.

EN/RU rendered previews under `Test/session-ui-preview-20261007/` contain fresh candidates on the second map. The bit-mode controls were widened after Russian label clipping was observed. These are actual offscreen WPF controls, not proof of historical-GUI, hardware/DPI or game acceptance.

All final processes are terminal. Complete Debug Core passes **73 groups / zero failures**, report `Test/object-session-debug-20261007.json`. Both full loaded WPF configurations pass **204 cases each**, exit 0, including four session and four Alcatraz/Ancients cases per configuration; logs `Test/object-session-ui-debug-20261007.log` and `Test/object-session-ui-release-20261007.log`. Debug/Release solution builds have zero warnings/errors.

The full Release 578-map corpus has **1722 passes / 94 failed checks**, exit 1, report `Test/expanded-corpus-object-session-20261007.json`: 73 synthetic / 560 native / 530 supported-text / 559 original-policy text passes. The complete ordered failure array exactly equals `expanded-corpus-object-painting-temp-fixed-20261007.json`. Test temporary roots are process-local directories on D:. Source/Binaries remain unchanged, and whitespace checks pass. The pre-existing refusals are not suppressed or counted as accepted maps.

Original/game acceptance and the remaining [sprite workflow](OBJECT_SPRITE_SCOPE.md), catalogue and semantic workflows remain open; this is not full editing parity.

## ManagerCommandHistory-20261017

Локальные Add/Undo и double-click Modify подключены к общим source callers.
[Текущий scope, точное владение шагами, проверки и ручная приёмка](OBJECT_MANAGER_COMMANDS.md).

## ManagerNavigation-20261017

Общие X/Y теперь видны в менеджере и выбираются приватным point preview/OK;
Cancel сохраняет старые значения. [Навигация и границы приёмки](OBJECT_MANAGER_NAVIGATION.md).

## ManagerOptionsDialogs-20261017

Добавлены отдельные параметры добавления/изменения с private draft и Apply-only
commit. Их Cancel сохраняет прежние настройки; старый combined editor по-прежнему
сохраняет canceled raw drafts согласно своему датированному scope.
[Текущее поведение и ещё открытое sequencing](OBJECT_MANAGER_OPTIONS.md).

## ManagerExecutionFlow-20261017

Общий шаблон и source options/preview execution подключены к реальным Add/Modify. Single properties сохраняют локальный draft; групповое добавление использует выбранную область менеджера. Шаблон не имеет права применять данные карты. [Details](OBJECT_MANAGER_EXECUTION_FLOW.md).

## ManagerListCommands-20261018

[Текущее selected/map default, membership и direct menu routing](MANAGER_LIST_COMMANDS.md).
Прежняя незакрытая формулировка single-list membership для поддержанных physical
records снята этим сопоставлением. Unknown-layout/identity scopes остаются I01/I04/I18.
