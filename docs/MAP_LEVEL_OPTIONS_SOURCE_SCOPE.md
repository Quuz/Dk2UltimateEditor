# Source scope: named level options

Read-only audit baseline, 2026-10-07. This is a concrete component of map-metadata editing, not a claim that all metadata/speech/text workflows are implemented or audited. Generic project/native fields already permitted raw edits; the named level-options workflow was missing at the audit baseline and is now implemented below.

Subsequent [named EN/RU implementation and acceptance](MAP_LEVEL_OPTIONS.md) now covers this component. The audit below retains its original requirements/provenance, not a claim that the named panel is still absent. Separate speech/text/general metadata and whole-editor/game requirements remain open.

## Original controls and values

`Source/Resources/Editor.rc:1664–1721` defines dialog 6807. `Source/Sources/Properties/Map/Properties_Map_Options.c:156–276` initializes/saves its controls; `Source/Includes/Strings.h:463–475` defines their French meaning. The original exposes talisman pieces 0–4, game ticks 1–50 (read-only edit with up/down; the command handler clamps endpoints), soundtrack 1–3, secret-level number 0–16, twelve checkboxes and six previous/next reward slots. Three previous and three next selectors share **None + 48 active reward entries**, in exact `Rewards[]` order from `Source/Sources/Datas/Datas_Game.c:24–72`.

Rewards store the explicit source `Rewards[].Id`, not the list ordinal and not ordinary room/door/trap/spell IDs. The catalogue mixes rooms, doors, traps, keeper spells and talisman pieces; source bitmap IDs are attached to entries. Unknown imported reward bytes must be retained unless explicitly changed, rather than becoming None because the old dialog failed to select a match.

The flags are mapped exactly as follows. The final possession flag is **inverted** in the original checkbox, unlike the other eleven.

| Original control | Source field / mask | Checked meaning |
| --- | --- | --- |
| 110 | Options / 0x20 | Secret level |
| 113 | Options / 0x40 | Special level |
| 114 | Options2 / 0x04 | Multiplayer level |
| 115 | Options2 / 0x08 | Skirmish level |
| 116 | Options2 / 0x20 | My Pet Dungeon |
| 118 | Options / 0x08 | Always imprison enemies |
| 119 | Options2 / 0x02 | Last destroyed dungeon generates a gem |
| 120 | Options / 0x10 | Permit only one Horny use |
| 121 | Options2 / 0x01 | Show objectives at startup |
| 122 | Options2 / 0x10 | Lock game options |
| 123 | Options / 0x04 **clear** | Prevent leaving possession |
| 124 | Options / 0x80 | Show heroes-killed count |

The source resource accidentally reuses control 124 for the rewards heading; this is not another independently editable flag. Source save clears both whole flag bytes before rebuilding masks, losing unsupported bits. A modern named editor must preserve unrelated imported bits with masks **Options 0xFC / Options2 0x3F**, while leaving raw editing available. Its final secret-number reset checks Options2 bit 0x40 immediately after clearing/rebuilding Options2 without that bit; do not infer a reachable source rule or silently erase a stored secret number from that dead test.

## Persistence and surrounding workflow

These are real game metadata, unlike scenario aliases. `Source/Includes/Map.h:31–59` stores project MAPPROPERTIES values. `Files_Game.c:217–245` maps native bMainGameTicks/bMainOptions/bMainOptions2/talisman/rewards/soundtrack/special-number; `Compiler_Generate.c:82–111` maps them back. Modern compiler metadata assignments are source-derived through `LegacyCompilerRules`; verify the actual target fields/layouts before writes, including historical guards, rather than guessing offsets.

`Properties_Map_Global.c:40–93` copies map/player settings into a dialog draft and only applies the accepted parent dialog with an undo entry. The nested level-options dialog changes that draft, not the live map before parent acceptance. A modern explicit whole-panel Apply/Discard should retain this atomic user-visible contract, commit pending generic fields safely, preserve input on language changes and reject stale/foreign/closed authority. Resize, player defaults and the other properties pages are separate workflows; their existing controls must remain available.

## Required implementation/acceptance

Add EN/RU named controls for all four scalar values, twelve mapped/inverted flags and six source-image reward choices, with retained unknown/raw values, strict original edit ranges and atomic undo. Verify independent project/native bytes, all flag combinations and unknown-bit preservation, each reward slot/entry/None, no-op/failed transactions, historical layouts, compiler/save/reopen, actual WPF drafts/language/pending fields/lifetime and rendered layouts. Exercise corpus read-only projections without editing installed maps. Speech selection/playback and text-table browsing/editing still require their own full source audits; this component does not close those requirements or whole-editor/game parity.
