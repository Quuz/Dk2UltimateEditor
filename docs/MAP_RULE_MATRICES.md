# Availability, creature pools/modifiers and alliances

The sixth workspace tab exposes seven table categories. Select cells (Ctrl/Shift selects multiple), choose a state or enter a number and press Apply. Clearing selected overrides, restoring selected defaults and restoring a whole table require confirmation and use one undo operation. Tables are read-only views: selection alone never edits data.

## Source evidence and scope

- `Source/Sources/Properties/Map/Properties_Map_Players.c`: availability radio IDs 118–122, `Properties_MapPlayerRoomsChange`, `Properties_MapPlayerRoomsDispInfo`, pool editing and selection functions. Pool input is bounded to 0–999; a separate checkbox writes the -1 sentinel.
- `Properties_Map_Creatures.c`: four columns Resistance, Courage, Charisma and Strength; 0–65535 input; rows 17–29 (one-based) are elite modifiers and cannot be changed through this dialog. Zero displays as not configured.
- `Properties_Map_Alliances.c`: read/view, set and clear modes; changes write both directions; self-alliance is forbidden; default alliances connect Neutral to every other player.
- `Properties_Map_Global.c`, `Properties_MapOptionsToDefault`: original keeper pool/status presets; NPC pools become -1. This function first zeroes the entire player row, so a complete player reset also zeroes NPC availability; the selected/table workflow intentionally retains NPC availability instead and is not a complete player reset. `Properties_MapResetOptions` initializes a new/loading map, not an existing-map reset command (see the scope clarification below).
- `Files_Game.c`, `Game_ExtractVariables`, `Game_SetCreatureCount`, `Game_SetVariable`, `Game_SetAlliance`, `Game_SetCreature`: native import assignments read only the map's `Variables.kld`; later matching scalar records overwrite earlier ones; alliances are symmetric existence records. The separate shared `GlobalVariables.kwd` is not an input to these map-property tables.
- `Datas_Things.c`, `Datas_Game.c`: row order and IDs. `Compiler_Generate.c` emits variable IDs for availability/pools and creature IDs for modifiers, skips elite modifiers, emits only configured values and upper-triangle alliance pairs.

| Table | Rows | Columns | Editable targets |
| --- | --- | --- | --- |
| Rooms | 15 | 7 players | 105 |
| Traps | 14 | 7 players | 98 |
| Doors | 8 | 7 players | 56 |
| Keeper spells | 12 | 7 players | 84 |
| Creature pools | 45 | 7 players | 315 |
| Creature modifiers | 45 | 4 parameters | 128; 52 elite cells are read-only |
| Alliances | 7 | 7 players | 21 unique pairs; both halves select the same target; diagonal is read-only |

There are 866 canonical projected cells, 807 editable targets in a verified current document. Project files lacking verified fields show read-only cells rather than guessed offsets. Historical fields are usable when their actual schema verifies them. Unknown numerical values remain visible and are never normalized merely by opening a table.

## States and storage

The original availability states are **255 = not configured, 1 = unavailable, 2 = researchable, 3 = available, 4 = locked**. The previous generic suggestion incorrectly combined locked with value 1; that is corrected, including the new value-4 suggestion. Legacy raw zero remains exposed as a separate old/not-specified suggestion; table reading does not rewrite it.

Project player rows store availability as BYTE arrays, pools as signed LONG arrays (-1 is unspecified), modifiers as DWORD arrays (0 is unspecified) and alliances as a 7 × 7 BYTE array. Native formats instead use exact 16-byte variable records. Lair's native availability ID is 2, not terrain ID 11 or the packed tile catalogue ID `0x0B000101`.

Native absent availability/pool/modifier entries display as not specified; this is not a claim about effective game defaults inherited elsewhere. Zero remains a real pool count. Modifier zero removes matching native overrides and is stored as zero in projects. Imported out-of-range/custom values are retained and visible; the specialized editor uses source UI limits, while raw verified fields remain available.

Native scalar changes update only the last matching record, preserving earlier duplicates and their payloads. The count is visible when there is more than one record. Clear removes every matching record so an earlier duplicate cannot reappear as an unintended value. Native alliances match both orderings; an existing pair is retained unchanged when set, and clearing removes both direct/reverse records. Project display ORs asymmetric imported directions without repairing bytes; an explicit edit writes both directions.

All native projections, updates, insertions, clears and resets target the per-map variable collection exclusively. Shared installation records do not contribute values or duplicate counts, regardless of descriptor order or the factory's copied-definition order. Explicit insertion into the chosen map file also supports a custom non-Maps reference path without falling back to the shared file. If the per-map collection is missing, every native table cell is read-only; even the special self-alliance reset is refused without changing bytes or history. Existing raw shared-definition editing remains a separate workflow.

Defaults are explicit original editor presets, not inferred engine values: keeper pools use 32 for the source-listed ordinary creatures and 20 for Dark Angel/Maiden; other pools are unspecified. Keeper rooms, ordinary traps and doors are available; unsupported trap/door entries are unspecified. Create Imp, Possession and Summon Horny are available; other spells are researchable. NPC availability survives reset unchanged. Elite modifier data remain visible/preserved but are excluded from table edits/reset, following the original restriction. Alliance reset removes self-relations and restores Neutral's links. The modifier reset is explicit override removal, not a claim of inferred creature-definition defaults.

## Safety, UI and verification

All selected keys and values are checked before changing buffers. Mixed invalid batches and duplicate requests fail atomically. Unknown categories/IDs/players/parameters are never selected by numeric coincidence. Project edits touch only the verified fields, preserving padding, pointers and unrelated values. Native clear operates on current offsets in descending order; file counts/sizes follow normal collection mutation. Undo/redo restores exact bytes; no-op edits/reset/clears create no history. Confirmation is consumed after successful commands and cleared on document/category/property changes.

Projection indexes records/fields once, rather than rescanning the entire document for each cell. English/Russian names, player/parameter headers, options and status labels switch live; selection identity and entered values survive. Closed documents clear rows/confirmation and disable operations. The table does not consume map selections or copy buffers.

The core regression group exercises all 807 editable targets in both formats, independent project-field byte encodings, native/compiled key-player-parameter mappings, source-default values, exact unrelated-buffer preservation and one-transaction undo/redo. It covers every category, NPC retention, elite/self guards, unknown/invalid values, duplicates, reverse alliances, last-wins projection, clear-all behavior, no-op history, save/reopen/re-export, verified old layouts, unknown-layout refusal and source immutability. The real-map corpus compares read-only matrix projections after reopening without changing inputs.

Four in-process WPF EN/RU × native/project cases exercise actual bound DataGrid cell text, category dimensions, multicell selection/apply, confirmation, all reset/clear controls, numeric/sentinel validation, elite/self guards, live selection/label retention, no edits on mere view, undo/redo, map/copy-buffer retention and document close.

An additional core scope group supplies every editable rule key and every editable scalar key in a shared fixture. It checks all 807 rule targets / 189 integers with both reference orders, factory-shared-last order and custom non-Maps paths, exact unrelated/shared bytes, clear/reset, undo/redo and save/reopen. Missing-map fixtures reject every editable rule write/clear plus full/diagonal reset and scalar commands. Two EN/RU WPF cases exercise the actual table/button bindings for both orders and missing-file guards without modifying source files. The new test reproduced the original shared-record projection defect before its fix.

### Reset scope clarification

`Properties_MapResetOptions` starts with `ZeroMemory(Map,sizeof(MAP))`, clearing the map's buffers/pointers as well as properties. Its callers are `Windows_MainWindowProc` initialization, `Map_Setup` new-map setup and the three file readers (`Files_Editor`, `Files_Game`, `Files_Text`). It is not a command to reset the properties of an existing edited map. The separate `Properties_MapReset(...,Save)` commits/cancels the properties dialog rather than restoring defaults. New/load initialization belongs to those existing workflows; an invented destructive whole-map reset is not required for original editing parity. The real per-player default button (`Properties_MapPlayerToDefault`) now has a separate [complete player workflow](PLAYER_SETTINGS.md); global-definition reset acceptance remains outstanding.

These are automated data/control tests. Original GUI visual/group-selection comparison, game behavior and additional malformed/unknown layouts remain acceptance work. Full player reset is implemented by the separate player tab, not by the selected matrix reset; global-definition reset and other parity rows remain open. Arbitrary unknown scalar keys are not an original UI capability.


## Grouped creature modifiers — 15 October 2026

The parent map-properties window now opens a private four-parameter species
selector with source nonzero means, elite exclusion, per-parameter writes,
child OK/Cancel and outer atomic Apply/Cancel. The existing matrix remains
available. Source/model proof and compiled-only WPF/manual steps are in
[MAP_CREATURE_MODIFIERS](MAP_CREATURE_MODIFIERS.md).
