# Source named parameter controls

Current canonical project (17 October 2026): these workflows are in the common
`src/DK2MapEditor.App/Editing/ScenarioParameterWindow.cs` and current Core.
No Test/staging copy or held patch is needed to build. Dated evidence below
belongs to its historical snapshot; current results and manual GUI limits are
in [CURRENT_STATUS](CURRENT_STATUS.md) and [REMAINING_WORK](REMAINING_WORK.md).

The scalar/repeat-window full regression is now accepted across all five gates
(`editor3-scenario-control-windows-20261008-01`); C# and assembly changes may
resume. This source audit is independent preparation for completing the
remaining 50 original parameter pages, not an editing-parity claim.

`Test/audit_scenario_named_catalogues.py` captures
`Test/scenario-named-catalogues-source-audit-20261008-02.json` from unchanged
original source. Use -02: -01 predates correct separation of combo item data
and selection indexes and does not bind direct dynamic-list getters.

The audit records 59 creation calls in 45 pages, fourteen helpers and 748
selectable row instances across those contexts. Counts include repeated rows
in different pages; they are not unique capabilities or completed controls.
All source check/store bodies, complete common callbacks, helper bodies, call
arguments and original hashes are retained. Named parameters are checked
against literal native compiler operands; no absent native operand is invented.

## Exact identities and contexts

- Creature choices use source creature IDs from 16 ordinary, 13 elite and 16
  hero rows. Six page calls select their own Any/elite flags. Creature-count
  conditions enable Any and exclude elites; AddCreature/Generate exclude Any
  and include elites. Category headings have item data zero and are not values.
- Rooms use `VarRooms.Variable`, not the packed terrain ID in `VarRooms.ID`.
  Lair is stored as 2 in a room field, while its physical terrain ID is 11.
  Fifteen ordinary rooms are offered; heart 5 and portal 3 are only present
  when SpecialRooms is true. A -1 HasAny flag on ShowNextRoom means No room,
  while true in condition pages means Any room; both list sentinels store zero.
- Terrain-list values are `Terrain.ID >> 24`. The two page calls have different
  ShowRooms/SpecialRooms flags. Signed-char tests on the Variable column filter
  room entries, not a guessed numeric interval over physical terrain IDs.
- Door/trap/spell values use their Variable column. Destroyed appends traps
  into the existing door list; stored values 1..8 occur in both categories.
  That field cannot preserve a separate selected category. Do not invent one,
  silently deduplicate labels or reinterpret the stored numeric ID.
- Player lists expose source player IDs and a contextual Any sentinel. The
  four player combos have source start 1 and no Auto/Any entry. Their item data
  is the player ID, while Store uses selection index + 1. Four level combos
  similarly store index + 1 for literal levels 1..10; those string combos do
  not set item data. CREATURESLEVEL param4 is a level, not a player reference.
- Interface selection is a packed pair: category in the high word, ID in the
  low word. ClickElement/ShowElement/AddElement contexts contain respectively
  57/54/49 selectable rows. ShowElement omits spell 5 (Hand); AddElement omits
  interface entries and uses nonselectable headings. Ordinary room/spell/door/
  trap values share this list without becoming one untyped catalogue.

## Dynamic and compound source requirements

Five hero-band lists, ten area lists, one gate list and one effect list require
current map-backed rows and their own source sentinel/selection behavior.
Hero-band list item data is ID + 1; Store subtracts one, retaining the actual
band identity. Gate selection is ordinal in the source combo; project gate
IDs compile to native coordinates, so DestroyGate has no direct native Param5.

Three area-list calls have no direct ID getter in Store: AddCreature,
ChangeSquare and ChangeOwner. They fill point/rectangle controls through the
shared callbacks and then store coordinates. The audit explicitly flags their
callback binding for review. Do not substitute an area-ID input for that flow.
Common callback/default/save bodies remain authoritative for dependent inputs,
coordinate conversion, nested creature flags and alias/camera composition.

These contexts now compose in the common private parent draft. Unknown imports
and inactive bytes must remain intact, and complete pages apply atomically.
Current implementations and model results do not replace physical EN/RU
GUI/audio/dialog/hardware/DPI/game acceptance.
