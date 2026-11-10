# Current DK2 editor user guide

Pre-final version, 25 October 2026. The application is built from the single
current `src/` tree. See [CURRENT_STATUS.md](CURRENT_STATUS.md) for validation
results and remaining work, and [the Russian guide](USER_GUIDE_RU.md) for the
expanded walkthrough.

## Start, language and document formats

Run `artifacts/PreFinal/App/DK2MapEditor.App.exe`. Keep all files in App together.
The binary requires Windows and the .NET 10 Windows Desktop Runtime. Building
requires the .NET 10 SDK: run `./build.ps1` from the repository root.
Use **Settings → Language → English / Русский** to change language.
Preferences are stored at `%LOCALAPPDATA%/DK2MapEditor/settings.json`.

Open a game's `.kwd` descriptor with its companion `.kld` files for complete
map editing. A `*Map.kld` contains terrain; a `.dkm` is the editable project.
Text `.map` files are imported into a project, and `.dkz` files represent
selections rather than complete maps. Use Edit commands to import a DKZ.
Opening and validation do not automatically repair source files.

When a KWD name differs from its companion names, resolved explicit KWD links
take precedence. Same-stem files are fallbacks for missing or unresolved links;
the loader does not combine both scenario collections.

## Editing workflow

1. Create a map with **File → New** (`Ctrl+N`), choosing size, background,
   keeper count, game version and optional starting hearts.
2. Choose terrain, owner and drawing tool on the map tab. Add rooms and objects.
3. Configure players, map properties, definitions and scenarios.
4. Validate and resolve blocking errors.
5. Save a DKM working project; compile a new game bundle in a separate directory.
6. Open the bundle in the official editor and test it in the game.

The map tools include brush, line, rectangles, ellipses, fill, selection and
sampling. Left drag performs the selected operation; right drag selects.
Escape cancels an unfinished operation. Random painting uses its mode selector
and Shift; redo replays the same result. Special footprints and protected
map boundaries follow the original placement rules.

Edit provides copy/cut/paste, numeric selection bounds, rotations, mirrors and
DKZ exchange. In-place rotation requires a square selection; rectangular copy
with rotation is separate. The clipboard survives changing maps. Scenario
transfer requires compatible formats. Display switches, zoom, minimap and
overlays change the view without changing map contents.

Direct selection export follows the original first-object rule for stacked
noncreature categories; buffer export keeps every object. The modern editor
reopens the complete saved snapshot in either case. See
[direct selection details](DIRECT_SELECTION_EXPORT.md).

## Records, players, map properties and definitions

The properties tab provides category selection, record editing, creation,
duplication, deletion and image-based type selection. Unknown imported IDs are
kept numeric; a catalogue label does not normalize them. The object manager
supports sorting, filtering, batch edits and removal. Inspect owner, positions,
selected flags and collision decisions before applying batch operations.

Regions, gates, effects and patrol routes have their own geometry and options.
Hero bands contain 16 member slots. Active members form a continuous prefix;
an inactive slot ends the band. The project's seventeenth marker is not another
member. Member health, level, gold, objectives, behavior and roots are editable.
Use explicit Apply or Cancel in draft dialogs.

Validation warns about missing entity targets, ambiguous references and duplicate
IDs within one category. Equal IDs in different categories are allowed. These
warnings retain imported bytes and do not automatically repair the map. Technical
details identify the field and offset; see [the scope](ENTITY_REFERENCE_DIAGNOSTICS.md).

**Region properties** opens a private draft for coordinates, project labels and
colours, with child area/effect options. Closing valid child options keeps them
in the draft. Outer Cancel discards everything; Apply commits one Undo. Native
KLD has no editor label/colour storage; these controls require DKM. See
[the implementation scope](REGION_PROPERTIES_SESSION.md).

Patrol **Append area** and **Insert before point** open a separate selector.
Insert lists only areas without an outgoing link; Append also offers linked
areas. Select an area, set its0–250 pause and Apply. Cancel or Escape leaves the
map unchanged. Selecting a route selects its root; select a point separately
for Insert or pause editing. See [area selection](PATROL_AREA_SELECTION.md).

**Modify pause** opens a separate OK/Cancel number window. Its initial value is
shown unchanged; the readonly limits are0 and250 seconds. Out-of-range OK shows
a warning and returns to the same draft for correction. Cancel, close or Escape
leaves the map unchanged; valid OK creates one Undo even for an unchanged pause.
An empty or nonnumeric pasted value converts to0. See [numeric pause](PATROL_PAUSE_REQUEST.md).

If a shared-area edit changes a displayed patrol indirectly, an informational
warning recommends reloading the route lists. OK acknowledges it; closing the
window leaves the completed edit in place. **Do not show this warning again
during this editor session** takes effect only after OK. **Show patrol
warnings again** resets that choice; restarting the editor also resets it. This
setting is independent of removal and copy-buffer confirmation. See
[warning requests](PATROL_WARNING_REQUESTS.md).

Removing a patrol point or route now asks **Yes/No** in a separate window.
**Always answer Yes to patrol removal requests during this editor session**
disables No while checked and is remembered only after Yes. Close or Escape
cancels. Later removals then use Yes without asking; **Ask before removing
patrol links again** restores the prompt. One Undo restores an accepted removal.
This removes links, leaving the area records in the map. See
[removal requests](PATROL_REMOVAL_REQUESTS.md).

Deleting an area record reconnects its patrol predecessor: deleting B from
A → B → C produces A → C. With multiple incoming links, only the first in map
order changes. Undo restores the previous records and links. A second question
offers Yes to delete all selected areas, No to keep those with scenario points
and delete the others, or Cancel. Points and events survive area deletion;
keep the DKM and native-source archive to retain these editor-only points.
See [used-area removal](USED_AREA_REMOVAL.md) and [patrol links](AREA_REMOVAL_PATROL.md).

Gate properties offer **Generate hero gates**. Review the preview, confirm its
additions and choose **Keep generation in draft**. The map changes only when the
parent applies. Child Cancel discards that child's additions; parent Cancel also
discards earlier accepted children. See [private generation](REGION_GATE_GENERATION.md).

The hero-gate generator previews new gates, areas and events before confirmed
application. One Undo restores the whole result; the change counter counts
each added structure separately. A rerun with no new additions changes nothing.
See [hero-gate generation](HERO_GATE_GENERATION.md).

**Edit → Map properties** uses an independent draft including child settings.
Choose name, version, keeper count, dimensions, background and one of nine resize
anchors. Review cropping before accepting it. Outer Cancel discards nested
changes; outer Apply commits one history operation. A draft becomes stale if
the live map changes while it is open.

Players expose gold, heart health, AI, camera and building area. Availability
is separate from physical placement. Definitions and variable/rule pages edit
terrain, rooms, traps, doors, creatures and spells, including nested controls.
Export packages map definitions in its Globals file rather than changing the
installed global definitions.

In the shared creature-definition window, a job probability sum other than0
or100% opens **Continue/Cancel**. Cancel returns to correction; Continue permits
the current draft operation. Remembering Continue disables Cancel, while close
or Escape still cancels without remembering. Only Continue stores this choice
for the editor session, including other maps. **Ask about nonstandard job totals
again** restores prompts. The reply itself never saves the map; common Apply
commits the draft. See [job requests](CREATURE_JOBS_WARNING_REQUEST.md).

## Scenarios and saving

Resizing through map properties keeps the scenario points of cropped areas,
including explicit empty points. Incoming patrol IDs and event operands stay
unchanged; repair missing targets explicitly. Crop uses the common crop consent
without a second used-area question. Undo restores the whole map. Keep the DKM
and native-source archive to retain deleted-area points. See
[region crop](REGION_CROP_SCOPES.md).

The scenario tab offers owners/scopes, event trees, condition/action creation,
parameter editing, branches and graph validation. Specialized wizard pages
handle camera, coordinates, spawning, availability, speech and messages.
Review the scope and target before finishing. Cancel does not create a partial
event. Aliases are editor labels; they do not replace game IDs. Exported alias
metadata belongs outside the seven map files, in `Data/Editor/EditorMetadata`.

Save (`Ctrl+S`) and Save As (`Ctrl+Shift+S`) write the editable project. Native
to DKM conversion reports errors and losses; blocking errors prevent saving.
Keep any adjacent `.native-source-…zip` with its DKM. It stores original map
data that project fields cannot express, allowing verified native-byte retention
without overwriting current supported edits. It is map data, not an older code
version or a build recovery step.

Auto-save settings are in Settings. Recovery files live in
`%LOCALAPPDATA%/DK2MapEditor/Recovery`; save recovered documents normally.
Export Copy creates a separate bundle without overwriting existing destinations.
Text MAP export includes a full editable copy and a loss report; use that copy
to continue editing without losing unsupported text-format data. Historical
DKM string fields use CP1252 even when the interface is Unicode.

## Seven-file game export

Save As offers DKM/MAP. After native-to-DKM saving, the open document is DKM.
**File → Export copy…** preserves the current format: a DKM remains DKM, with
its bound native source ZIP; an open native KWD produces a native copy. Choose
a parent folder; the command creates a new `DK2-export-<timestamp>-<suffix>`.
To obtain a game KWD from DKM, use **File → Compile DKM to game map**
(`Ctrl+Shift+C`). The output level name comes from the DKM filename; use
`DK2Preview_6a88240eaf814f78.dkm` for the fixed test slot. The success message
reports the full output path. Its `Data/Editor/Maps` contains the seven game
files; the shared `GlobalVariables.kwd` is outside `Maps`.

**File → Compile DKM to game map** asks for a valid game map KWD as the definition
basis, such as `Data/Editor/Maps/level1.kwd`. Do not select a creature catalogue,
a DKM or an arbitrary definition-directory AutoSave file.

The Maps directory contains exactly `Name.kwd` and six KLD files:
`NameGlobals`, `NameMap`, `NamePlayers`, `NameThings`, `NameTriggers`,
`NameVariables`. Shared `Data/Editor/GlobalVariables.kwd` and editor metadata
are outside this set. The user confirmed opening a seven-file bundle in both
the game and the official editor on 11 October 2026.

Our fixed manual test name is `DK2Preview_6a88240eaf814f78`. Close the game,
back up old files with that prefix, and copy the seven new files into
`Data/Editor/Maps`. Keep other maps and the shared catalogue unchanged. Run from
the game directory:

```text
DKII-DX.exe -q -level DK2Preview_6a88240eaf814f78
```

Run Game (`Ctrl+Shift+E`) selects DKII-DX.exe, validates and publishes a separate
preview, then requests launch confirmation. It may use a generated preview
name; use the manual command above for the fixed comparison test.

## History, diagnostics and current limits

Undo is `Ctrl+Z`, redo is `Ctrl+Y`; history settings control retained operations.
Validation reports missing links, invalid roots/scopes, cycles, coordinates and
terrain issues without silently fixing them. Keep the whole bundle and launch
command when reporting a game failure.

Pre-final does not mean complete parity. The full corpus has known failures,
and hardware/DPI, audio, system dialogs and broader gameplay acceptance remain
separate requirements. See [CURRENT_STATUS.md](CURRENT_STATUS.md),
[FEATURE_PARITY.md](FEATURE_PARITY.md) and [INDEX.md](INDEX.md).

Global definition lists and the shared element/creature windows now show the
original icons beside names. Projectile options also show creature/object
icons; level choices remain numeric. Original bitmap identity is verified;
physical rendering and DPI acceptance remain pending. See
[GLOBAL_DEFINITION_IMAGES](GLOBAL_DEFINITION_IMAGES.md).

The map properties window retains shifted scenario targets when they fall
beyond a resized map. DK2N029 reports such a target; correct a bound project
action before compiling. [Target relocation and Undo](EDITOR_RESIZE_SCRIPT_TARGETS.md).

Map properties relocates starting cameras and build areas only for computer
keepers. Human keeper coordinates stay unchanged. DK2N030/031 report a right/
bottom-edge warning without blocking compilation. [Player relocation](EDITOR_RESIZE_PLAYERS.md).

Map properties resize reprojects native show-crystal and destroy-gate targets
from their source entities. DK2N032 warns about missing or stale targets.
[Rules and limitations](NATIVE_RESIZE_DERIVED_TARGETS.md).

Native gate geometry/properties updates its destroy action in the same Undo.
Deleting the selected gate leaves a missing target and warning; select an
existing gate before play. [Binding rules and limitations](NATIVE_GATE_MUTATION.md).

Native gate choices show an ordinal and coordinates. Selecting the second
coincident gate survives later moves and Undo even when game bytes are equal.
Save the working DKM to retain the chosen project ID; plain game KWD reload
uses the original first-position reader. [Working links and save policy](NATIVE_GATE_BINDINGS.md).

Object fields, managers, painting and selection commands update show-crystal
targets from the first remaining gem in insertion order. Undo restores objects
and targets together. [Projection rules](NATIVE_CRYSTAL_MUTATION.md).

Working gate IDs survive deletion of other gates. New gates take the first
free ID1–255 and recover actions retaining that ID. Use DKM to preserve IDs
between sessions; reopening a plain KWD assigns sequential IDs again.
[Details](NATIVE_GATE_IDENTITIES.md).

A DKM gate may be deleted while a destroy-gate action refers to it. The action
retains its ID and a missing-target warning. Saving the DKM draft is allowed;
the compiler writes the original1,1 fallback when the target is absent.
A new gate taking the freed ID becomes its target again.
[Deletion](PROJECT_GATE_REMOVAL.md), [compilation](COMPILER_GATE_FALLBACK.md).

Use Ctrl/Shift in the record list to select regions of one kind for batch
deletion. The shared question can remember Yes across areas/gates/effects
and documents in the current workspace. Undo does not reset this preference.
“Ask before deleting regions” in the map panel restores the question.
Closing a checked question does not remember consent.
[Details and limits](REGION_REMOVAL_CONFIRMATION.md).

## NativeOwnerScopes-20261016

Editor manager deletion and map cropping retain the removed object's scenario
point without an owner. A new object does not silently acquire it. Band deletion
and member removal in the parent dialog retain empty points too. Undo restores
the prior order and bindings. Keep the working DKM and native-source ZIP together;
the seven game files cannot store empty/orphan working points or their UIDs.
See [working-owner rules and limits](NATIVE_OWNER_SCOPES.md).

## SharedScenarioRequests-20261016

Manager Remove asks for confirmation after Preview. If selected creatures
are used in scenarios, Yes includes all; No keeps every selected object with
an assigned scenario ID; Cancel leaves the map unchanged. Modify asks before
opening values; an empty result after No opens no child. Remembered Yes for
scenario use is shared with areas and also covers creature modification.
“Ask before deleting objects/regions” restores questions. Undo does not reset
the preference. [Details](SHARED_SCENARIO_REQUESTS.md).

## UnderCursorRemoval-20261016

Point at a map tile: Delete prefers creatures, Shift+Delete doors/traps,
Ctrl+Delete ordinary/special objects. A sole occupied family overrides the
preference; otherwise the preferred family is used. Every stacked item in
that family is removed. Scenario-bound creatures use Yes/No/Cancel; No keeps
all assigned scenario IDs. Each removed record has its own Undo restoring
the prior order. Shift+Ctrl+Delete bypasses this command; right-click keeps
selecting the map. Delete in text controls edits text.
[Rules and limits](UNDER_CURSOR_REMOVAL.md).

## ManagerCategoryScopes-20261016

Changing a door to a trap, or an ordinary object to a special item, keeps
the scenario ID and its original-category point. The point remains without
an owner; events are retained. Explicitly return the same object to its former
category to reconnect it. Undo restores the full prior state. Keep the working
DKM and native-source ZIP together: the seven game files cannot store trap UIDs
or orphan/empty working points. [Details](MANAGER_CATEGORY_SCOPES.md).

## InspectorObjectRemoval-20261016

Physical creature, door, trap and object categories in the record inspector
support Ctrl/Shift selection. Delete uses the manager's confirmation and
Remember Yes preference. A second question appears for used scenario IDs:
No keeps all entries with assigned IDs; Cancel stops removal. Undo restores
the complete group, prior item order and crystal target. If a pending field
edit was just applied, check the selection and choose Delete again.
[Rules and limitations](INSPECTOR_OBJECT_REMOVAL.md).

## ScenarioDeletionRequests-20261016

Deleting an action point, condition or action asks a separate question.
Remember Yes applies independently to each kind until the editor closes.
No, Escape or closing the question leaves the map unchanged; closing a
checked question does not remember a reply. Restore scenario deletion
questions resets only these three preferences. The old checkbox before
branch deletion has been replaced by the question. A point removes all its
events; a condition removes its children while retaining following events;
an action removes only itself and retains its continuation. Undo restores
the previous state. [Detailed rules](SCENARIO_DELETION_REQUESTS.md).

## BigMapRequests-20261017

Choose Create with valid settings; large maps ask an information question.
The old checkbox in the form is no longer required. No/Escape/closing returns
to settings. Remember Yes is shared with map-properties resize, survives Undo,
and resets when restarting the editor or choosing Restore large-map question.
Cropping is confirmed separately. [Details](BIG_MAP_REQUESTS.md).

## PlayerGoldNotice-20261017

Saving keeper starting gold above treasury/heart capacity shows an OK-only
information window. Closing it or pressing Escape also saves current values;
gold is preserved exactly. Remember takes effect only after OK, for this run.
The player panel can restore the notice. Nested map properties share this
preference with the main workspace; map Undo remains available.
See [implementation and manual acceptance](PLAYER_GOLD_NOTICE.md).

## MapNameOverflow-20261017

A map-properties name longer than 64 characters opens Yes/No/Cancel.
Yes copies up to 63 characters; No keeps the previous draft name while saving
the other settings. Cancel, Escape and Close return to the form. As in the
source editor, exactly 64 characters become 63 without a question. Remember
applies only to Yes until restart; the form can restore this question.
Parent Undo and native Unicode names remain available.
[Details and manual acceptance](MAP_NAME_OVERFLOW.md).

## HistoryFailureAtomicity-20261017

Failed Undo/Redo snapshot preparation preserves the map and its prior
history. A failed stroke commit restores its cells and releases capture;
normal stroke completion and full Undo keep their existing behavior.
Allocation failures were simulated in Core without exhausting memory or
launching the GUI. [Details and limits](HISTORY_FAILURE_ATOMICITY.md).

## ScenarioChoiceCaptions-20261017

Scenario choices now name the action explicitly: open/close portals,
game messages, spoken countdown and cinematic view. Attack choices follow
the source radio captions; stopping forced running is distinct from a new
walk order. Stored values are unchanged. [Details](SCENARIO_CHOICE_CAPTIONS.md).

## ScenarioEntityCaptions-20261017

Area, gate and effect-generator choices include their existing names and
comments. Region line breaks appear as ` | `; stored map text stays literal.
Spawn/cell-change area choices also show names and comments. Switching EN/RU
retains the ID and does not translate user text. Native records without text
fields retain their numeric label. See [choice details](SCENARIO_ENTITY_CAPTIONS.md).

## CopyConfirmationLifetime-20261017

“Do not ask again” in the internal copy-buffer replacement question lasts
for the current editor session. Disabling confirmation explicitly in the
menu persists across launches; enabling it resets temporary suppression.
See [copy confirmation lifetime](COPY_CONFIRMATION_LIFETIME.md).

## GateGenerationRequests-20261017

After preview, Apply/Keep asks Yes/No with the new gate, area and event counts.
In region properties, Keep still updates only the parent draft; parent Cancel
discards it. Remembered Yes lasts for the session and is shared with the
workspace generator. Restore generation questions resets it. Capacity notices
have a separate remembered OK and never permit creation beyond the limit.
See [generation requests](GATE_GENERATION_REQUESTS.md).

## HistoryMemoryNotice-20261017

Undo-data allocation failure shows an OK-only notice. Remember suppresses
it for the session; Close/Escape does not remember. Restore Undo error notices
in History Settings resets this preference without clearing history. Other
already committed operations remain in history. See [details](HISTORY_MEMORY_NOTICE.md).

## RecentFileCommands-20261017

File → Recent files includes Clear and Remove unavailable entries. These commands change only the list, never delete files; hover over a filename to see its full path. [Details](RECENT_FILE_COMMANDS.md).

## ManagerCommandHistory-20261017

The object manager provides Add objects and local Undo. Undo covers commands performed from this open manager; closing it retains applied changes. Double-click a row to modify it. [Details](OBJECT_MANAGER_COMMANDS.md).

## ManagerNavigation-20261017

Show selected object or destination temporarily hides the manager; use the return window to resume. Map clicks preview a private point: choose Use point or Enter to accept, Cancel/Escape to retain the old X/Y. [Details](OBJECT_MANAGER_NAVIGATION.md).

## ManagerOptionsDialogs-20261017

The manager has separate Addition options and Modification options. Apply stores session preferences only; Cancel keeps the previous values without editing the map. [Details](OBJECT_MANAGER_OPTIONS.md).

## ManagerExecutionFlow-20261017

Ask Options and Preview control Add and Modify independently. The template button edits preferences for later commands. Single Modify opens object properties including X/Y; a group uses the shared template. Unresolved warnings still require preview decisions. Direct execution retains the same local Undo. [Details](OBJECT_MANAGER_EXECUTION_FLOW.md).

## ViewVisibilityCommands-20261017

View → Toolbars hides or restores the command and map tool strips. View → Minimap remains available while toolbars are hidden. These are current-window preferences and do not edit the map. [Details](VIEW_VISIBILITY_COMMANDS.md).

## EditorSounds-20261017

Tool and terrain choices, brush/shape gestures and supported object placement use15 original WAV files from Sounds next to the application. Loading and language refresh do not play rebinding sounds. Audible playback and device behavior require manual acceptance. [Details](EDITOR_SOUNDS.md).

## WindowMapTitle-20261017

The main window title shows the internal map name. Save As with a different filename does not rename the map. Applying a name change or Undo/Redo updates the title; unsaved and recovery markers remain available. [Details](WINDOW_MAP_TITLE.md).

## DocumentReports-20261017

Loading, map checks and compilation reports show the full message list. Select a row for details and its available map location. Close/Cancel is the default; errors and stale documents cannot approve continuation. Save As retains its explicit loss and native-source archive consent. [Details](DOCUMENT_REPORTS.md).

## FileDialogPaths-20261017

Map, selection and BMP dialogs retain separate directories and filenames. AREA/BMP names are retained after chooser OK even if later I/O fails; Cancel keeps the previous choice. A confirmed BMP replacement now publishes through a staging file. [Details](FILE_DIALOG_PATHS.md).

## ThumbnailWindowLifetime-20261017

An open thumbnail follows the newly opened map while retaining its filters and preferences. Its position is retained for the editor session; closing the map closes the thumbnail. [Details](THUMBNAIL_WINDOW_LIFETIME.md).

## CompilerTaskWindow-20261017

Test, Compile and game preparation show a separate task window with phases, all diagnostics and a final result. Editing is disabled while a task runs. After successful preparation, press Run to launch the game. Normal window position and size are retained for the editor session. [Details](COMPILER_TASK_WINDOW.md).

## DiskProgress-20261018

Load and ordinary Save show a short-lived window with the path and current phase. Writing starts after all confirmations. On failure the previous target is retained, the indicator closes and the editor is enabled again. Autosave remains quiet. Counts describe observed files/sections or bytes in the current output stream, not time remaining. [Details](FILE_IO_PROGRESS.md).

## ManagerListCommands-20261018

Edit provides direct creature, door/trap and object/special-item manager commands. A new manager opens on the current selected area; without a selection it shows the whole map. Out-of-map records remain in the document and Inspector but are excluded from manager lists. [Details](MANAGER_LIST_COMMANDS.md).

## WorkspaceManagerRoutes-20261018

Edit also opens hero bands, areas, gates, effect generators, patrol paths and scenarios in their existing editing panels. The band list remains available when empty. Region commands scope the list by kind; All region types restores the combined list. Create an action area before opening patrol paths. Opening a panel does not create map records. [Details](WORKSPACE_MANAGER_ROUTES.md).

## SourceMenuInventory-20261018

View provides terrain visibility, physical object layers and region/path/camera overlays. These controls remain available when toolbars are hidden and share the existing toolbar preferences. [Details](MAIN_MENU_SOURCE_MAPPING.md).

## HistoryClear-20261018

Edit → Clear history discards retained Undo/Redo while keeping the current map and configured history depth. Future edits remain undoable. Discarded steps cannot be restored; the command is disabled when history is empty. [Details](HISTORY_CLEAR.md).

## ObjectBrushCommands-20261018

Edit → Object brush settings opens a separate template for creatures, doors, traps, objects or special items. Cancel keeps the previous brush. Accept selects placement; an object is painted only when you click a suitable map tile. Accepting a template does not edit the map or add Undo. [Details](OBJECT_BRUSH_COMMANDS.md).

## HelpSectionVersions-20261018

Help → User guide opens this guide inside the app. Local links open related Documentation pages; changing language returns to the selected main guide. Help → Project section versions shows nine DKM/DKZ sections with input, working and expected versions and status. These project versions do not apply to KWD/KLD maps. Viewing does not upgrade data. [Details](HELP_SECTION_VERSIONS.md).

## SourceKeyboard-20261018

Source shortcuts: Ctrl+Shift+N creates a map, Ctrl+Shift+C compiles, Ctrl+Shift+T tests, F1 opens help. Ctrl+R / Ctrl+Shift+R rotate; Ctrl+Shift+Y mirrors vertically. Ctrl+F1…F7 choose rectangle, filled rectangle, ellipse, filled ellipse, line, pen and fill. Ctrl+Shift+F1…F5 choose creature, door, trap, object and special-item brushes; repeating the current kind opens its settings. Ctrl+N and Ctrl+Y retain New and Redo, as approved by the user. [Details](SOURCE_KEYBOARD.md).

## SpecialKeyboard-20261018

With map focus, Insert opens creature-brush objectives, Shift+Insert creature options and Ctrl+Insert hero options. These small windows keep choices when closed without changing the active tool. Arrows scroll one cell, Shift+arrows one page, Ctrl+arrows to an edge. PageUp/PageDown scroll vertically; Home/End reach horizontal edges and Ctrl+Home/End vertical edges. Scrolling during a stroke can continue the line under the pointer; the complete stroke remains one Undo. [Details](SPECIAL_KEYBOARD.md).

## ManagerCommandScope-20261018

In the scenario tree, Delete on an event opens branch removal confirmation; on a point root it opens point removal confirmation. Insert on an event starts condition insertion before the selected event. Insert on a DKM point root opens ordered point insertion; native game roots do not have that stored point list. Opening or cancelling the wizard does not edit the map. [Details](MANAGER_COMMAND_SCOPE.md).

## RegionCreationDraft-20261018

Add region opens a complete independent draft. Set its geometry and options, then choose Add. Each addition is one Undo operation; the window retains its values for another region. Closing discards only the unadded draft. Add from selection and Add on map also open this draft for acceptance. [Details](REGION_CREATION_DRAFT.md).

## RegionListModify-20261018

Double-click a row in the visible region list with the left mouse button to open that record’s properties. The Properties button uses the same route for the selected row. Finish or close an existing draft before editing a different record; its entered values are retained. [Details](REGION_LIST_MODIFY.md).

## ScenarioInsertionCommands-20261018

The scenario tree now has explicit New root, Append condition/action and Insert condition/action commands. Append uses the end of the appropriate chain; Insert uses the selected event’s incoming connection. All commands use the shared wizard and cancellation. Ctrl+N creates a map and Ctrl+Y performs Redo, as approved by the user. [Details](SCENARIO_INSERTION_COMMANDS.md).

## ScenarioExpansion-20261018

Condition expansion is retained in the working DKM. Switching language or tabs does not reopen collapsed branches. This presentation state adds no Undo step and does not mark the map modified. KWD documents retain it in memory; Save As DKM transfers it to the working project. Game files remain unchanged. [Details](SCENARIO_EXPANSION.md).

## RegionCoordinateClamp-20261018

Area, gate and effect generator coordinates are clamped to map bounds as you enter them. The ▲/▼ buttons and Up/Down within a coordinate field change it by one. A gate’s end follows its start. The map changes only when you apply; closing discards the draft. [Details](REGION_COORDINATE_CLAMP.md).

## ScenarioNewPointCommand-20261018

New action point… always opens the creation wizard regardless of the selected point. Open / create owner binding continues to select an existing binding. Add separate DKM points for available players or objects; occupied owners and a second level point are unavailable. Cancel retains the chosen type for the next opening without changing the map. [Details](SCENARIO_NEW_POINT_COMMAND.md).

## ManagerDestinationCoordinates-20261018

Object manager destination X/Y are clamped to map bounds as you type. The ▲/▼ buttons and Up/Down change a value by one. An empty field means 0 for Show destination and Add. Choosing a point on the map remains private until OK; Cancel retains the previous coordinates. [Details](MANAGER_DESTINATION_COORDINATES.md).

## Creature level percentages

Properties → Map properties → Creature level percentages. Select
GlobalVariables.kwd if a catalogue was not found automatically. The dialog
has23 statistics, levels2–10 and First Person, range−10000…10000. The native
format writes zero as100. Child Apply updates the private properties draft;
common Apply publishes one Undo; parent Cancel discards the draft.
Export catalogue copy writes a separate shared-catalogue copy without applying
the map or adding a game file. Choose a different name for the copy.
[Details and manual acceptance](CREATURE_LEVEL_MODIFIER_WINDOW.md).


On the creature Light page, typing clamps the whole part of X/Y/Z to −32767…32767 and radius to 0…3. Fractions within those whole-part limits remain; spin buttons change the whole part by one. Loading an imported value does not normalize it.


On Parameters, the active second attraction amount clamps to 0…16000 while typing. None leaves it blank and disabled. Spinning up from empty gives 1; changing the second attraction type reloads the captured private amount. Imported out-of-range values remain unchanged on opening or a no-op Apply.


On Attributes, all 38 numeric fields apply the source input bounds while typing. Spin buttons change the whole part by one and read empty input as zero. Native packed-width limits, private drafts and common Undo remain. Opening imported values does not normalize them.


On Attacks, all16 numeric fields clamp the whole part while typing. Hold the −/+ buttons or use unmodified Up/Down for one-unit whole-part steps. None disables fields and steps while preserving their values. The same panel serves standalone and shared creature windows.


Possession and CrSpells apply source bounds while typing in all seven numeric fields. Hold −/+ or use unmodified Up/Down for whole-part steps. Creature-spell steps retain the existing controls. Native WORD guards remain; shared Apply covers both pages with one Undo.


On Jobs, chance clamps to0…100 and mood/mana to−32767…32767 while typing. Hold −/+ or use unmodified Up/Down for whole-part steps; empty input steps from zero. Add/Change remain explicit. Hate has no numeric parameters.


Numeric fields on Terrain, Room, Door, Trap and Spells clamp typed values to their allowed range. Hold −/+ or use unmodified Up/Down for whole-part steps. Increasing an empty field gives1. Loading and changing language preserve imported values. Disabled fields remain unavailable.


For trigger and projectile numeric options, hold −/+ or use unmodified Up/Down for whole-part steps. Increasing an empty field gives1. Each spinner changes its own field; the signed minimum is−32768. Input clamps immediately. Non-gold amounts remain disabled; native gold above65535 fails without a partial write.


Resistances now uses four0–100 sliders with numeric readouts. Left/Up decrease by1; Right/Down increase by1; Home/End select0/100. None disables the slider/readout and retains its rate. An imported255 remains in its readout with the thumb at100 until an explicit scroll.


The shared Global Elements and Creatures windows save the current row privately before switching pages. Invalid input blocks the switch. Current/all reset replaces pending values only in the active section. If ownership changes while saving, navigation and Apply stop.


The bottom of CrSpells lists creatures that use the selected spell in any of their three attack slots, with source images and translated names. Selecting a row does not edit the map. Unverified definitions produce an incomplete-list notice.


Scenario context choices show the original images beside their names. Door and trap rows with the same numeric ID retain their own category and image across language changes. Unknown imported choices have no guessed picture.


Scenario band lists show each member beneath its band, with a source image and level. Leaders also show their objective and, when needed, area number. Member rows are informational; choose the band itself. Language changes retain the selected occurrence and member snapshot.


## Selected-file preview

Map Open/Save As, map-basis selection, DKZ import/export and BMP saving now use a native chooser with a lower preview pane. It shows the selected existing file, dimensions and available object counts. Browsing does not open the file or change the current map/buffer. Cancel accepts no path; OK continues through the existing readers, consent and save checks. A new or damaged file may have no preview without being forbidden as a chooser result. See [FILE_CHOOSER_PREVIEW](FILE_CHOOSER_PREVIEW.md) for limits and outstanding manual acceptance.


## Map name and file name

The map card reflects the live internal name, author and dimensions, including Undo/Redo. Save As changes the accepted filename/path without renaming the internal map; the full path is in the tooltip. Ambiguous numeric links are labelled without guessing the first target. Data and IDs are never repaired by display. [Details](DOCUMENT_REFERENCE_PRESENTATION.md).


## Player record diagnostics

Validation reports invalid/duplicate native player IDs and declared-count/slot mismatches. Gameplay validation explains hearts without a unique player and multiple keeper hearts; health retains the source last-matching rule. Validation does not repair, create or reorder records. Seven DKM slots do not imply seven active players. [Details](PLAYER_LAYOUT_DIAGNOSTICS.md).


When saving a native map as DKM, the player-slot message explains which of the seven positional slots use source-import defaults because no native record exists, including unused keepers. This is information before accepting Save; the original files and physical order remain in the map-data companion. [Import policy](I05_CODE_CLOSURE.md).

## Game definition references

Map checks DK2N039/040 are informational. They identify IDs outside verified
original editor catalogues. A game modification may provide valid custom
definitions; a notice alone does not establish a broken map or block saving
or export. Values are retained without automatic repair. Repeated unknown
terrain IDs are grouped with their cell count and first location.

Supported placed objects/books, active band members, effects, availability,
global definitions and contextual scenario operands are checked. Enum fields
and numeric arguments are not guessed to be links from their names. See
[the finite I06 scope](I06_CODE_CLOSURE.md) for details and opaque-layout limits.

## Working metadata and the game bundle

Keep the DKM and its native-source ZIP together for continued editing.
They retain original bytes and editor data such as working identities,
empty scenario points, orphan points and provenance. The ZIP contains map
data, not earlier code versions; the editor builds from one current src/ tree.

Game publication contains seven official files. The game format cannot store
all working identities and orphan state, so opening only the KWD does not
replace the saved working project. See [I01](I01_CODE_CLOSURE.md) for the
finite storage, history, saving and owner-association table.

Compilation stamps newly generated game headers with local creation and
modification time, as the source writer does. Unchanged copied definitions
keep their dates; raw native SaveCopy does not restamp them. Working DKM
statistics follow separate rules. [Details](COMPILER_GAME_DATES.md).

Returning all global-family values to the original defaults preserves the entire selected basis section, even when the DKM array already exists. Real changes still generate an override. [Details](COMPILER_DEFAULT_GLOBALS.md).

Compilation writes Things categories in the original writer order and preserves order within each category. Keeper hearts follow hero gates and precede effect generators. The source DKM is unchanged. [Details](COMPILER_THING_ORDER.md).

Full map checks now include the original placement, unsafe water/lava and hero-gate prerequisite notices. Enemy territory and two-square gate prerequisites are Information; the other new notices are Warning. Checks do not repair the map. [Details](COMPILER_PLACEMENT_DIAGNOSTICS.md).

Check/Compile retains negative project coordinates for Add creature, Change square and Change owner using the original upper-bound-only check. Game output narrows them to WORD; this does not make an off-map target playable. [Details](COMPILER_ACTION_BOUNDS.md).

Original level filenames are reserved for game compilation. Check shows Information DK2N046; use Save As with another filename before Compile. The internal map title is independent and can remain unchanged. [Details](COMPILER_RESERVED_NAMES.md).

Converting a game map with a missing destroy-gate target to DKM requires consent to possible coordinate loss. The missing ID and source files are retained. Recompilation uses (1,1) unless safe source-payload retention applies; archive restoration cannot attach an old missing target to a newly inserted gate. [Details](NATIVE_MISSING_GATE_CONVERSION.md).

Supported-scenario MAP export preserves the selected physical gate, including duplicate coordinates. A missing target is omitted with Scripts loss. The native EditableCopy preserves game-file bytes; editor-only working IDs and bindings require DKM with its source companion, as the explicit export warning explains. [Details](TEXT_MAP_GATE_IDENTITIES.md).

KTRG1 scenario upgrade supports all32768 action IDs, including a single condition containing the entire range. The final ID−32768 survives Save/reopen and game compilation. Overflow is rejected without partial document changes; the complete upgrade can be undone. [Details](HISTORICAL_SCENARIO_COUNTS.md).

Selection paste remaps scenario owners by thing category and UID. A creature and door sharing a numeric UID keep separate owners; only the active owner field changes. Same-category ambiguity is refused. Paste is fully undoable, and DKZ/DKM Save-reopen preserve the links. [Details](SELECTION_OWNER_IDENTITIES.md).

Native definition sections with unverified versions or headers remain opaque: field editing is refused and DKM conversion is blocked. A raw game-file copy retains their bytes unchanged. Verified44/46-row creature layouts at5449/5537 bytes keep their common prefix and opaque88-byte extension. [Layout table](NATIVE_DEFINITION_LAYOUTS.md).

Raw native copies retain unknown definition frames and trailing bytes. DKM conversion requires consent to their possible omission from game recompilation; the source companion retains the complete original bytes. Official packing refuses unsafe opaque-data relocation. [Details](NATIVE_DEFINITION_TAILS.md).

Native collection edits use the counted records to locate the insertion boundary even when stored payload sizes are stale. Existing record order and opaque suffixes survive; Undo restores the exact original bytes. Unknown semantic layouts remain subject to I04. [Details](NATIVE_COLLECTION_BOUNDARIES.md).

Unknown native collection headers and unsupported record sizes are retained as opaque data. The inspector and editor agree on warnings; semantic operations are refused with atomic rollback, while raw copies preserve every byte. Compiled working-owner restoration now follows the compiler category order. [Details and limits](NATIVE_COLLECTION_GUARDS.md).

New native conditions are inserted before the action block. Working gate targets and complete Undo survive shifted action addresses. Existing malformed D2 order/split-counts are preserved by raw copying; semantic editing and DKM conversion are refused rather than silently repaired. [Format and checked cases](NATIVE_SCRIPT_PARTITION.md).

KWD inspection now uses the editable loader’s packed-definition parser. Native-to-DKM conversion requires existing loss consent for opaque collection suffixes that game recompilation may omit; the complete original bytes remain in the source archive. [Details](NATIVE_PACKED_INSPECTION.md).

DKM/AREA preview now validates the same inner sections as opening and shows map dimensions. Enhanced DKZ still uses the dedicated clipboard route. Unknown native companions are visible as opaque files; raw copies preserve them, while DKM conversion requires consent to their possible omission from game recompilation and retains the complete source archive. [Support boundaries](HISTORICAL_SECTION_INSPECTION.md).

KWD preview and opening now share the information-header and minimum metadata-prefix checks. Available text is still displayed for an invalid map, while bad headers or references block editing. The supported [20-row format matrix](I04_FORMAT_MATRIX.md) closes I04 in code; manual acceptance remains open. Parent map properties accept the same safe stale-size fallback as native variable-override deletion and retain complete Undo.

Flood fill compares the original editor cell identity: native reserved bytes do not split connected regions. Owned paths with different owners and the two bridge variants remain separate. Selection limits and the protected perimeter apply; Undo/Redo restores exact data. See [flood components](EDITOR_FLOOD_COMPONENTS.md).

Spatial selection carries placed creatures, doors, traps, ordinary objects and magical objects. Global band definitions, their members/scopes and independent area/gate/effect records remain separate. Paste copies literal terrain/object payloads; heart/portal/gate cursor minimums do not resize pasted terrain. See [band and selection policy](I08_CODE_CLOSURE.md).

Selection transforms keep each cell terrain variant literally, matching the original editor. Independent gate-region anchors remain stationary. DKM compilation derives gate orientation from current terrain and neighbouring solid cells; direct native editing retains independent stored gate records. See [gate transform rules](I09_CODE_CLOSURE.md).

Scenario editing has69 parameter pages,26 additional repeat-only conditions and8 actions without parameter pages. Alias and nested flags/point/camera/speech workflows compose through private Apply/Cancel. The current [I10 callback matrix](I10_CALLBACK_MATRIX.md) lists every type and context; physical UI acceptance remains separate.

Scenario owners are resolved by scope+UID; inactive band slots do not create false ambiguity. Selection copy checks only its reachable graph, leaving unrelated malformed source events untouched. An actually selected ambiguous/missing target is refused explicitly. The code audit is complete; [23 manual acceptance groups](MANUAL_ACCEPTANCE_RU.md) remain, beginning with opening and seven-file Save/reopen.
