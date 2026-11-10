# Source projectile controls

The global-definition tab has a separate expandable **Projectile and parameters / Снаряд и параметры** editor. It covers traps, primary and upgraded keeper spells, and creature spells (four independent slot mappings). All 66 original `GShots` entries have curated English/Russian names, including None and Possession. The creature list has all 45 source species including elites/heroes; objects use the preserved source catalogue.

## Source forms

`Datas_GlobalVariables.c`, `GShots`, declares the dialog form, both parameter labels and signedness. Its source rows are parsed separately from generic element lists: ID zero is a valid None entry and the FFFFFFFF sentinel must not become a selectable type. The verified list is exactly IDs 0–65. `Properties_Global_Functions.c`, `Properties_GlobalShotOptions`, `Init`, `Proc` and `Save` specify five forms:

| Source form | Input and source limits | Inactive values |
| --- | --- | --- |
| 0 | Two integers, each 0–32767 or −32768–32767 according to source flags | Both active |
| 1 | One integer, same signedness/range rule | Stored second argument retained |
| 2 | Source creature ID plus count 0–100 | Both active |
| 3 | Source creature ID plus level 1–10 | Both active |
| 4 | Source object ID; gold 0–80000 only for object ID 1 | Actual change to a non-gold object clears gold |
| FF | No parameter dialog | Both stored arguments retained |

Parameter labels follow source codes: power, change, increase, duration, decrease, radius, or neutral numbered labels when the source leaves a caption unspecified. These are integers, not /4096 values. Health/mana/armour change types have signed first arguments. The UI exposes names and the full compound operation rather than requiring separate raw-field edits.

`Properties_Global_Elements.c`, `Properties_GlobalSetShotOptions`, maps trap/keeper primary and upgraded fields; `Properties_Global_Creatures.c` maps creature spell `Shot` (not `ShotType`). Compound apply changes type and active parameters in one fully preflighted transaction. Simply choosing a type/target or opening/changing the form does not write data; explicit apply does. Reset still follows the existing source group boundaries.

## Preservation and deliberate corrections

Native parameters are packed WORDs, projects use LONGs. A changed negative signed argument encodes the appropriate native WORD bits; an unchanged project WORD alias such as 65527/−9 retains its original integer. Native gold cannot exceed 65535; project gold retains the source 80000 maximum. Invalid/range/overflow input fails before any mutation; no implicit wrapping or partial compound write occurs.

The original dialog substitutes the first target/level when an imported value is not found. The current specialized editor follows those source target/level fallbacks in a private draft; opening and Cancel do not write them. Explicit acceptance stores the shown choices. The generic SetGlobalProjectile API and raw fields still retain unknown targets and imported values; source UI acceptance is described in [PROJECTILE_EDITOR_ACCEPTANCE](PROJECTILE_EDITOR_ACCEPTANCE.md). Unknown projectile **types** cannot be specialized-applied, but their stored fields remain available through the existing table/raw controls. Missing/unverified layouts have no inferred current values and remain read-only.

The source object save handler clears `Data2` for a non-gold object, even after simply accepting a dialog. The current inline and child editor routes follow this source clear rule on acceptance, including unchanged type/target. Generic SetGlobalProjectile retains its previous inactive-value preservation; imported bytes remain exact until an explicit editor Apply.

Language refresh retains the same definition/slot, chosen type, target/level and numeric drafts. Changing definition/slot or underlying compound values discards the stale draft and rebinds actual values. Closing/replacing the document clears compound state. Pending generic property commit is checked before applying: if it changes this projectile, compound apply stops for review instead of mixing an old selected type with refreshed parameters. Selection/drafts invalidate destructive group-reset confirmation.

## Compiler correction

`Compiler_Generate.c` has post-assignment rules not expressible as simple field mappings: trap and both keeper-spell slots clear `Data2` when the selected `GShots` form is 1 or FF. Their `Data1` is retained. Creature spells do **not** have this post-processing, so their inactive second argument remains. The modern compiler now implements those exact rules in its independent output bundle without changing project bytes or source definitions. Unknown types are not indexed into the source table or guessed to have one of those rules.

## Verification and remaining work

The core group has an independent literal list of all 66 forms/labels/signs and exercises all 66 types across all four slot mappings in native and project formats. It checks source endpoints, object gold/native limits, signed WORD encoding/aliases, count/level/target rejection, inactive/unknown preservation, atomic notifications/no-op/exact undo/redo, save/reopen, missing guards and compiler-specific second-argument zeroing. Source definitions are unchanged.

Four actual WPF EN/RU × native/project cases browse every source type and verify each bound form, all five parameter dialogs and no-options mode, named target counts, signed/count/level/object edits, limits/errors, primary versus upgraded isolation, creature-spell mapping, raw unknown types, actual pending DataGrid commit, confirmations, translation/drafts, undo and document cleanup. Six offscreen renders per case cover two values, signed value, creature/count, creature/level, object/gold and no-options forms. Rendering is distinct from historical-GUI or in-game acceptance.

This milestone does not finish level modifiers, other named enums, visual assets or remaining parity rows. Jobs and attacks are covered by the separate [creature-job editor](CREATURE_JOBS.md) and [creature-attack editor](CREATURE_ATTACKS.md). Historical GUI behavior/gameplay and additional unknown native variants remain unverified.

Verified 2026-10-06 including the creature-attack milestone and the reproduced no-op reset draft fix: Debug/Release builds have zero warnings/errors; Debug core passes all 52 groups; Release core plus corpus passes 115 cases (52 core + six text inputs + 57 native maps). Debug and Release WPF each pass 124 cases, including four optional loaded-map cases. Twenty-four final projectile offscreen renders are saved under `Test/ProjectileUiQA-Final-20261006`; inspected EN signed/level and RU two-value/count/object views show readable captions and full parameter controls. The dark Expander foreground was explicitly corrected and is regression-checked. Original `Source` and `Binaries` pass `git diff --exit-code -- Source Binaries` unchanged. These results do not establish historical GUI/gameplay acceptance or full parity.


## Current specialized acceptance — 15 October 2026

First-target/level1 initialization and accepted non-gold zero are now shared by
the inline panel and all three child window classes. 1320 source-form preview
combinations and96 native/project/four-slot acceptance cases pass; updated WPF
cases are compiled only. See [current source rules and manual steps](PROJECTILE_EDITOR_ACCEPTANCE.md).
Dated UI evidence above does not execute these updated routes.


Продолжение: [ProjectileChildInputs-20261019](PROJECTILE_CHILD_INPUT_CALLBACKS.md) — trigger/shot numeric input/step исправлены в трёх child classes и inline panel; source TypeProc spinner102. Два дефекта исходника исправлены по выбору пользователя. I11 открыт: Resistances и конечная сверка remaining callers.
