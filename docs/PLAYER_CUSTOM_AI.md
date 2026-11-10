# Manual computer-player settings and room construction order

The Players tab now has a separate **Custom computer-player settings** expander. It exposes all 67 manually assigned scalar parameters in original `Properties_MapComputerProc2`–`8`, organized into seven bilingual groups, and a highest-first list of the fifteen original room build priorities. Both native version-4 players and verified current/V2 project players use this workflow. Profiles, general player settings, camera and build bounds remain separate commands.

## Source evidence and coverage

`Source/Sources/Properties/Map/Properties_Map_Computer.c` defines nine original pages: profile, build area/order, then Rooms / Traps and doors / Spells / Creatures / First attack / Battles / Miscellaneous. The first two pages' profile and coordinate settings were already implemented. The new manual scalar catalogue covers exactly the 67 assignments remaining after excluding `PlayerAI` and the four `PlayerCPUArea*` coordinates; this exact set is compared against the preserved source by the regression fixture.

- Rooms: 13 settings, including researched/lost-room delay, default size/gaps, distance, corridor angles, neutral-room dig distances, expansion mode, empty areas, enlarged lair, selling captured rooms and flexibility.
- Traps/doors: six settings, including the two source 0–4 scales, best/worst/random door, corridor/corner boulders and usage percentage.
- Spells: five settings: frequency/power 0–4 scales, Sight of Evil/Call to Arms targets and Lightning.
- Creatures: seven settings: unhappy/angry treatment, dismissal, portal/sacrifice method, imp limit and **do not slap** bit 0x20.
- First attack: thirteen settings: enabled level/threat requirements, creature count/level/threat, room/spell requirements, breach/dig choices, attack points, forces percentage, counterattack-only and no-attack.
- Battles: eleven settings: dropped-creature threat, spell style, enemy imprisonment percentage, outnumbered withdrawal, injured-creature health threshold, Call to Arms placement/removal conditions and attack interval.
- Miscellaneous: twelve settings: library use, prisoner fate, mining cap, secret search/find probabilities and explored tiles, `(Tiles/Imps)-1`, starting mana, speed, gold/mana valuation and exposure.

Ten original combo arrays (`Expand`, `Angle`, `Distance`, `SightOfEvil`, `CallToArms`, `Sack`, `Breach`, `Dig`, `Library`, `Jailed`) contain 31 entries. Their stored values preserve exact zero-based source order. Door usage has three choices; five original 0–4 sliders preserve their source endpoints (aggressive/defensive, traps/doors, often/rarely, powerful/weak). Intermediate scale values remain numeric rather than inventing unverified game meaning. Eighteen source boolean/masked controls have named Yes/No choices. Labels are curated EN/RU translations, not game-language resources.

Original numeric controls are respected: normal byte thresholds 0–255, specified percentages 0–100, creature level 0–10, speed 1–4, mining amount 0–65535, starting mana 0–64000, and long wait/placement/search fields 0–32767. The threat-related byte percentages remain 0–255 where the source permits that range; they are not incorrectly limited to 100. `Source/Includes/Strings.h` and `Source/Resources/Editor.rc` supply captions, units and slider direction.

`Datas_Map.c: CPURooms` has IDs **1,2,3,4,6,7,8,9,10,11,12,13,14,15,16**. These are construction-order room IDs, not terrain IDs or availability variables. `Properties_MapComputerChangeOrder` swaps the selected priority with its immediate predecessor/successor; the new buttons perform the same draft operation. Native compilation uses `bPlayerRoomToBuildPri1`–`15`; project fields use `PlayerRoomToBuildPri[0]`–`[14]`. The Portal bilingual catalogue name was added for this source list.

## Disabled original flag is not a guessed native field

The original count-condition checkbox (704) is a disabled `STATE3`, always set to `BST_INDETERMINATE`; no command assigns `PlayerUseCountInFirstAttack`. Profiles initialize that project field, but the preserved native compiler has no verified mapping. The new panel explains this rather than inventing a writable flag/native offset. The **creature-count threshold** (`PlayerFirstAttackCount`) is an actual source manual field and remains fully editable.

## Transactions, imported data and drafts

All changes remain drafts until explicit **Apply custom AI and room order**. Apply preflights every changed scalar/order entry, then writes them in one reversible byte transaction. Name, gold, health, control, profile type, camera/build area, player identity, script links, availability/pools, shared files and unrelated bytes are untouched. Applying identical values creates no history. Native writes use only the preserved compiler's verified scalar/priority mappings.

The captured full player-row payload must still match when Apply occurs. Foreign documents, missing/ambiguous player IDs, stale records, unsupported layouts and changed player values fail without a partial write. Live translation retains numeric/invalid text, choices, order selection and expanded groups. Player changes/document close discard that player's draft; external player changes retain the draft but remove Apply authority until explicit reload. A pending generic Properties commit is rechecked by the core's stale-payload guard. Profile/general/reset workflows remain independent and invalidate a conflicting custom draft rather than silently overwriting it.

Unknown imported choice values receive a visible numeric option; out-of-range stored numbers are shown as-is. Unchanged unknowns are retained, not clamped during opening, translation or Apply. Changed values must satisfy the original manual limits. Toggling the named no-slap bit preserves other imported bits (deliberately safer than the original full-byte assignment); applying a profile still uses its explicit full source defaults. Unrecognized/duplicate/missing room orders remain visible and byte-exact; scalar settings can still change, but normal reordering requires a valid fifteen-room permutation. A confirmed profile reset remains an explicit way to restore its original order, not an automatic repair.

V2 project AI fields are verified and editable; V1/future player layouts require explicit supported upgrade or remain read-only. The disabled count flag is retained in projects, including compilation omissions; no guessed native value is introduced.

## Verification

`PlayerAIOptionsChecks` independently reads the preserved source to compare the entire 67-field manual set and all ten enum-array orders/counts. It tests every named choice and every numeric endpoint/out-of-range refusal in native/project formats; all seven players' full scalar+reversed-room-order application is compared to independently encoded expected bytes. Tests cover unchanged repeats, one-step undo/redo, unrelated bytes, saved sections and serialized re-export, project-to-native compilation, unknown choices/numbers/slap bits/order, partial-batch rejection, foreign/stale drafts, duplicate IDs, V1/V2/future layouts and explicit upgrades. Project original container bytes are not confused with rebuilt serialization: edited virtual sections are compared individually and saved/reopened/re-exported container bytes must match exactly.

`PlayerAISmoke` adds four EN/RU × native/project actual-control cases: all 67 controls/seven groups/fifteen room rows, translated names, choice counts, numeric/choice inputs, whole Apply, up/down priority controls, inline malformed-input refusal, live invalid-text/draft retention, stale Apply prevention and explicit reload, player switching, unknown values/slap-bit retention, undo/redo and closed-document cleanup. Focused Debug core and WPF cases pass; full gates are tracked separately in the parity ledger. Optional `DK2_PLAYER_AI_UI_ARTIFACTS` creates four offscreen actual-control PNGs for visual inspection. Rendering found and corrected black expander labels on the dark panel; header and parameter foreground colours are now asserted by the WPF cases.

Full Debug and Release solution builds finish with zero warnings/errors. All 60 synthetic groups and all 152 WPF cases pass in both configurations, including loaded Alcatraz/Ancients maps. The repeated 578-map corpus still reports 560 native / 530 text passes and 75 failed checks; see the expanded-corpus report for the unresolved refusals.

Historical GUI, mouse/keyboard interaction, pathological imported-order behavior and effective Dungeon Keeper 2 AI behavior still require acceptance. This is not a full-editor/gameplay parity claim.
