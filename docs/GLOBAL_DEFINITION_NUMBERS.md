# Global definition numeric controls

The global-definition tab now uses ordinary units by default for the 42 fields proven fractional by the original controls. It retains a stored-integer column and an explicit raw-edit mode. The raw core API and source reset values are unchanged. Neither opening the tab nor switching modes modifies the map.

## Source evidence

- `Datas_GlobalVariables.c`, `GAttribs`: 38 attribute entries, eleven fractional attributes and thirteen explicitly signed integer attributes. Native offsets are checked against compiler-derived fields. All creature attributes are WORD-sized except the LONG hunger rate. Fractional attributes include height, eye height, sight/hearing distances, walking/running speed, hunger rate and four durations.
- `Properties_Global_Elements.c` and `Properties_Global_Creatures.c`, direct `Misc_GetDlgItemFloat`/`Signed` assignments: the other 31 fractional fields, including light positions/radii, room torch values, trap timings, keeper/creature spell timings and ranges, three attacks' delays/coordinates and possession scales. Both source files are embedded read-only; only explicit assignments are parsed, never evaluated as C.
- `Misc/Misc_Maths.c`: game fixed-point uses a 12-bit fraction, integer / 4096. Signed coordinates use two's-complement signed integers; unsigned LONG values are interpreted as DWORD bits. This is not IEEE float storage. The original decimal getter truncates fractional precision rather than rounding.
- `Properties_Global_Creatures.c`, attribute setter and `EN_CHANGE` handlers: source input limits. The original unsigned fixed-point attribute whole part is 0–32767, signed integer attributes have a maximum 32767 but no explicit lower clamp, and unsigned integer attributes are 0–32767. Attack delay/range whole parts are 0–1000; attack recharge is 0–255; signed attack/light coordinates are −32767–32767; possession scales 0–1; light radius 0–3; creature spell timing/range 0–1024.
- `Properties_Global_Elements.c`, `EN_CHANGE`: terrain light height and trap timings whole parts 0–16000, room torch height 0–8 and radius 0–3, keeper spell recharge 0–32767.
- Resistance `TBM_SETRANGE` is 0–100. These four rates are percentages, not fractional fields.

The original fixed-point handlers check the integer part, so fractions at the endpoint are accepted too. The displayed endpoint is the last representable /4096 step in that interval; a longer input fraction is truncated toward zero. Both comma and dot are accepted as decimal separators, with no grouping or exponent. Invalid input is rejected inline, not silently clamped.

## Storage and preservation

Project LONG fields retain the original editor's wider input range. Native WORD fields cannot represent all those values: the native control shows the intersection with its packed limit and rejects new values that would overflow. For example, native speed's maximum is 65535 / 4096, while the project accepts a whole part through 32767. The original project compiler casts to WORD; this existing source behavior remains separate from direct native editing and is not silently changed by display controls.

Signed WORD attributes may be positive aliases in project defaults (e.g. 65527 represents −9). Re-applying the same displayed value returns the exact original integer, rather than materializing an override or changing 65527 to −9. A changed negative input encodes a signed LONG in a project and the corresponding WORD bits in a native record. Project values beyond a signed WORD remain intact and editable within the original LONG input limits; direct native inputs must fit signed 16-bit.

The original generic attribute painter sign-extends every nonfractional value above 32767, even fields declared unsigned in `GAttribs`. The modern display follows the explicit source Signed flag instead of showing imported unsigned health/gold as negative; raw bits are preserved. This is a deliberate display correction, not a file-format conversion.

Imported/default values outside manual control limits are displayed without normalization or clamping. Re-applying the exact value is a no-op even outside those limits. Display uses exact decimal arithmetic with up to twelve fractional places, unlike the original eight-place rendering, so the smallest stored step 1 / 4096 = 0.000244140625 round-trips without losing a bit. Raw mode allows deliberate integer inspection/editing through the existing lossless API; it does not remove structural/layout or BOOL guards.

The current units layer does **not** infer a physical seconds/metres meaning or change file formats. Shot-data fields remain raw in this numeric table; the separate [projectile editor](GLOBAL_PROJECTILES.md) now interprets them according to the original type. The canonical project now includes the five complete element parents and specialized creature attack/job/level editors. Full ordinary numeric range, enum/unit inventory and physical acceptance remain open; the existence of those controls does not prove that broader inventory complete.

Names alone are not evidence for units: the original keeper-spell `RechargeTime` uses the fractional getter, but `UpgRechargeTime` uses ordinary `GetDlgItemInt`/`SetDlgItemInt`. `Properties_GlobalShotSave` has five parameter-dialog forms (two integers, one integer, creature/count, creature/level, object/gold) selected by `GShots`; their compound interpretation follows that table rather than treating all shot-data fields as fractional.

## Verification

The numeric core group has an independent literal field list, all 4096 possible fractional display steps, source/packed endpoints, mixed fractional/signed batch encoding, independent native byte reads, signed truncation, smallest-step edits, signed aliases, imported out-of-range/no-op preservation, source versus native width rejection, invalid/mixed/duplicate batch rollback, one transaction, exact undo/redo, project/native save/reopen, native compilation and missing-layout guards.

Four actual WPF EN/RU × native/project cases verify bound ordinary/raw cell values, comma edits, raw toggle/application, one-bit display/reapply, unchanged default arrays, signed input/aliases, width/integer/percent failures, language/draft retention, confirmation invalidation, undo and missing-layout/close cleanup. Separate offscreen ordinary/raw screenshots support rendered-layout review, not historical GUI or gameplay acceptance. Full parity is still incomplete.

Verified 2026-10-06: Debug/Release builds have zero warnings/errors; Debug core passes all 49 groups; Release core plus local corpus passes 112 cases (49 core + six text inputs + 57 native maps); Debug and Release WPF each pass 112 cases, including four optional loaded-map cases. Twenty-four offscreen definition/unit renders are saved under `Test/DefinitionUnitsUiQA-Final-20261006`; inspected EN/RU ordinary/raw views show exact fractional and negative displays, the raw column and unclipped reference header. `git diff --exit-code -- Source Binaries` confirms preserved original sources/default files. These checks are not in-game or historical-GUI acceptance.


Продолжение: [GlobalElementInputs-20261019](GLOBAL_ELEMENT_INPUT_CALLBACKS.md):33 numeric controls исправлены; Step/Rebuild/repeat Attributes/Light/Parameters code gap закрыт. I11 открыт: далее TypeProc/ShotProc/Resistances и конечная сверка оставшихся callers.
