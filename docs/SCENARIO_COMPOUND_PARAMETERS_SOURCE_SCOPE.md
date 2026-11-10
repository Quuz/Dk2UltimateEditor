# Compound scenario pages: source oracle

`Test/capture_scenario_compound_pages.py` captures append-only
`Test/scenario-compound-pages-source-20261008-01.json`. Original source hashes
match the existing page audit; additional map-normalization/header inputs are
hashed before and after capture. This is an independent implementation oracle,
not acceptance of modern controls. Capture occurred while C#/assemblies were
frozen for `editor3-scenario-control-windows-20261008-01`; that regression is now
fully accepted and its freeze is lifted.

## Add-creature child flags

Original ACTION_ADDCREATURE button 313 edits param4 through
Things_SelectOptions mode 3 (THING_OPTIONSADD), resource 6111. Seven independent
checkboxes map to bits 1, 8, 16, 32, 64, 128 and 4, respectively. Bit 4 means
instant death in this context; it is not the band-leader option. Bit 64 is
displayed but disabled. Known mask is 0xFD; editable mask is 0xBD. Bit 2 and
all upper DWORD bits have no ADD controls. Native storage is one byte, while
the project retains the complete signed Int32 bit pattern.

The original IDOK and IDCANCEL share a block that zeroes CreatureOptions and
reconstructs known bits. The modern private-draft requirements deliberately
avoid that destructive behavior: child Apply modifies only the parent draft;
child Cancel and outer Cancel discard changes. Only explicitly edited active
bits change; disabled and unknown bits survive. Applying the complete parent
page produces one document history operation.

## Points and area callbacks

AddCreature stores param5/param7 directly. ChangeSquare and ChangeOwner display
the stored values minus one and store displayed values plus one. All three
original X/Y controls use three decimal digits. Physical native bounds are an
additional constraint, not a reason to change the source coordinate convention.

Their area selector 311 does not store an area ID. It looks up the current area,
normalizes reversed rectangle endpoints through Map_CorrectRect and fills X/Y
with the top-left point. Normalized dimensions are inclusive. The point picker
312 supplies raw map coordinates to the same inputs; it adds no offset itself.
Invalid/missing area selection leaves coordinates unchanged.

ChangeSquare's terrain callback also controls owner input: terrain IDs at most
7, or 0x1F, force Neutral (stored player ID 2) and disable the owner combo;
0x23 forces Heroes (ID 1) and disables it. Other selected terrain enables owner
selection; no selection disables it. Source combo indices 1 and 0 are not the
stored IDs. Untouched imported values must not be repaired merely by opening
the parent window; these dependencies apply to an explicit terrain choice.

## Change-square rectangle creation

Checkbox 315 is disabled when modifying an existing action. During creation it
switches point controls off and enables rectangle picker 314. The source save
loop creates the first action at the top-left, then visits each remaining cell
by X columns within Y rows, including both endpoints. Every new action gets a
fresh ID and goes through ordinary scenario linking. Stored X/Y each receive
the +1 offset. This is a sequence of actions, not extra rectangle operands in
one action record.

The modern creation workflow must preflight the complete expansion and commit
it atomically with one Undo/Redo operation, preserving existing links and all
unrelated bytes. It must also support outer cancellation without inserting
partial actions. This creation workflow, the three complete named parent pages,
map-backed pickers and actual EN/RU/native/project acceptance remain required.

The fixture retains full original check/store/default/resources, callback and
save bodies, native layouts and Map_CorrectRect so later tests can check literal
encodings independently of implementation helpers.
