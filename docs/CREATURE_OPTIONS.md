# Creature resistance and possession options

The EN/RU global-definition tab now edits the complete **Creatures → Resistances** (eight fields) and **Creatures → Possession** (fourteen fields) source groups through named choices, exact numeric controls and whole-group apply/discard. This is editing progress, not full original-editor or gameplay acceptance.

## Source evidence

`Source/Sources/Datas/Datas_GlobalVariables.c` defines four lists whose first zero entry is selectable and whose FFFFFFFF entry is a terminating sentinel:

| Source list | Source choice count | Mapped creature fields |
| --- | --- | --- |
| `GCrRes` | 33 (0–32), order 0, 1, 2, 3, 15, 4…14, 16…32 | `Res1`…`Res4` |
| `GCrAbilities` | 11 (0–10) | `SpecialAb1`, `SpecialAb2` |
| `GCrSwipes` | 23 (0–22) | `Melee1`, `Melee2`, `Special1`, `Special2`, `Spell` |
| `GCrGammas` | 17 (0–16) | `GammaEffect` |

Names come from `Source/Sources/Datas/Texts_Things.c`, with curated English/Russian labels. Resistance IDs 16–32 are explicitly **unknown source elements**, but still selectable in the original: their labels retain that uncertainty. They are distinct from an imported ID not in a source list. Possession's swipe catalogue must not be confused with the attack page's creature-spell or melee-damage catalogue: for example ID 15 means Dark arrow here, but Scythe in the attack-melee list.

`Properties_Global_Creatures.c`, initialization/save/get pages 4 and 5, proves these field mappings. `Properties_GlobalChangeRes` disables each rate when its type is zero; it does not clear that value. Page-4 `Properties_GlobalSaveChangesCr` still stores all four types and all four rates, including disabled ones. Rates are ordinary integer percentages 0–100, not fractional values.

Possession page-5 input handlers limit each special-ability data integer to 0–32767 and `WalkScale` to 0–12. Waddle/oscillation use `Misc_GetDlgItemFloat`, so exact display is stored integer / 4096; their whole-part limit of one accepts through 1.999755859375 according to the original [numeric rules](GLOBAL_DEFINITION_NUMBERS.md). `CameraRolls` is a boolean. The ability data controls are not guessed to have different meanings or automatic clearing for each ability.

`Properties_Global_Functions.c` and `Compiler_Generate.c` independently map native resistance type/rate pairs to bytes 0xE0C–0xE13. Possession IDs map to bytes 0xF46/0xF47, 0xF42/0xF43, 0x1025/0x1026, 0xF45 and 0xD3C. Ability data WORDs are at 0x14E9/0x14ED, waddle/oscillation WORDs at 0xDBC/0xDC0, walk scale BYTE at 0xD3D, and camera rolls bit 0x10 at 0xEFF. Projects use source UINT/LONG/BOOL fields; compilation retains the original packed casts.

## Editing guarantees

Each group's immutable draft is bound to its document, creature, format and exact complete loaded baseline. Apply preflights all eight or fourteen inputs before any mutation, then writes changed fields in one undo transaction. Invalid input cannot partially save another field. An unchanged apply does not create history or a fresh-project global array.

The UI caches both groups independently. Language changes, switching groups and applying one group retain an unchanged other group's inputs. None retains loaded and pending rate values while disabling their editor. Named choices preserve an explicit imported unknown, including native BYTE 255 and project UINT 4294967295. Imported out-of-range rates, ability parameters and scales can be retained; repairing one type does not normalize those other fields. Newly changed values must satisfy source choices/ranges and physical storage width.

A pending generic property commit anywhere in the group invalidates the complete old draft. Other source groups remain independent. Confirmed reset also clears unapplied group inputs even if stored defaults make the reset a byte-exact no-op. Closing/replacing the document clears cached drafts and dynamically registered controls. Missing/unverified native definitions show blank current controls and are read-only; reference defaults are not substituted.

## Verification

`CreatureOptionChecks` independently asserts all four catalogue ID sequences, source unknowns and contextual names; re-enters all 45 creatures' defaults in both formats; edits every source option in every mapped field; compares literal native offsets and complete expected native buffers (including unrelated flag bits); tests exact undo/redo/no-op, complete-group writes, None retention, all numeric endpoints/malformed/range failures, missing/duplicate inputs, saved/reopened maps, native compilation, full-width unknown IDs and imported parameters, explicit type repair, stale/foreign drafts, unrelated-group preservation and missing-native guards. `--creature-options-only` runs this group alone.

Four actual WPF EN/RU × native/project cases exercise all 22 controls and all source choices, rate enabling/None retention, both cached group drafts, exact units, live translation, atomic/no-op apply, invalid whole-group input, unknown imports/type repair, actual pending DataGrid commits in another slot, explicit no-op reset, undo and missing/closed-document cleanup. The same switch runs these cases alone. `DK2_OPTIONS_UI_ARTIFACTS` renders both groups in four language/format combinations (eight views).

Historical Win32-dialog and in-game acceptance remain pending. Other named catalogues/compound controls, native variants and unrelated incomplete rows remain in [the parity ledger](FEATURE_PARITY.md).

Verified 2026-10-06 on the final code: Debug/Release builds have zero warnings/errors; Debug core passes all 53 groups; Release core plus local corpus passes 116 cases (53 core + six text inputs + 57 native maps). Debug and Release WPF each pass 128 cases, including four loaded-map cases. Eight final views are saved under `Test/CreatureOptionsUiQA-Final-20261006`; inspected RU possession and EN resistance views show readable complete controls, contextual swipe captions, exact fractional values and disabled retained rates. `git diff --exit-code -- Source Binaries` confirms preserved original source/default files. These are automated regression and rendered-control results, not historical-GUI/gameplay acceptance or full parity.


Продолжение: [Possession/CrSpells7 numeric callbacks](CREATURE_AUXILIARY_INPUT_CALLBACKS.md), CreatureAuxiliaryInputs-20261019. Далее Jobs9 source controls/3 shared numeric fields; I11 открыт.


Продолжение: [CreatureResistanceSliders-20261019](CREATURE_RESISTANCE_SLIDERS.md) — rate TextBox заменены четырьмя Slider/readout, сохранены None и imported raw. I11 открыт: конечная сверка14-page selection/reset/child callers.
