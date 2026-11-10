# Current native area, band and player retention

11 October 2026. Product implementation: `NativeRecordRetention.cs`, called from
`NativePayloadRetention.cs` after verifying immutable source archive data and
event identity mapping. This is part of the current Core project, not an overlay
or recovery script.

Unchanged imported AREA, BAND and PLAYER records are matched by their category
and byte ID. Every source/baseline/output group must be unique. Dimensions and
wire lengths must agree; generated current bytes must equal the generated
baseline. Source editable fields must also equal that baseline after root-ID
mapping. Band project and member unique identities are checked even when a
changed unlinked identity would produce the same root value.

Only source ranges excluded from editable field schemas are restored. AREA's
unknown byte27 and tail32..63 and PLAYER's Unused fields are kept literally.
The band header uses exactly51 wire bytes: trigger40..41, band ID42. Opaque band
header bytes, including its original version word, are preserved for eligible
unchanged imported bands. Header/size bytes0..7 are never restored over a
generated record. The aligned C sizeof must not replace the fixed51-byte layout.

Supported values and roots remain current. Changed parameters, geometry, names,
member/project identity or map dimensions leave freshly generated records.
New IDs do not inherit an original record's unknown fields. Duplicate identity
groups are never guessed; ordinary identity editing also rejects duplicates.
The whole source band is not copied: the compiler owns its16 generated member
slots and preserves the corrected zero inactive suffix policy.

Equal decoded player names permit literal restoration of their field bytes,
including data after the NUL terminator. A changed name invalidates restoration,
so a source text tail cannot replace the edited name.

NativeRecordRetentionChecks compares all97 whole official level1 records
(79 areas,15 bands,3 players), normalizing original event root IDs through the
verified source mapping. Whole records survive seven-file SaveCopy/reopen and
the user project/revision/history remain unchanged. Synthetic fixtures vary
opaque bytes/version, text tails, IDs, bounds, delay, health, gold and names;
member unique-ID edits, alias-only changes, Undo, resize, new records and duplicate
identity rejection are checked. The initially blocking synthetic fixture wrote
into trigger byte41; the fixture was corrected to modify only verified unknown
ranges. Product graph validation was not weakened.

All focused checks run against the same DLL as the pre-final application and
full corpus. The corpus has1767 passes and the same ordered94 historical failures;
it is not green. User confirmation of the prior seven-file bundle covers opening
in the official editor and game, not physical acceptance of this latest change.
Whole native fidelity for every possible map/variant remains a broader goal.
