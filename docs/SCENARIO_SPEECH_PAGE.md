# Original ACTION_SAY page

Implementation is in NEW `Test/scenario-speech-staging-20261009-01`, based on
accepted spatial02. Production is unchanged at64/69 pages. This candidate
implements all69 pages; complete candidate delivery/regression and wider parity
are still open. Original source scope is documented in
[the independent speech audit](SCENARIO_SPEECH_SOURCE_SCOPE.md).

The actual SAY window has original four-digit unsigned ID0..9999, suppress-text
checkbox, the current map's speech bank, named sound list, corresponding STR
caption and explicit/actual double-click playback. IDs come from the original
one-based SDT offset table, not alphabetic sound-name ordering. ID0 has no sound;
missing banks/captions do not invent IDs or prevent valid numeric editing.
Only verified40-byte audio headers play. Preview extracts exact MP2 payload
bytes into owned temporary files, uses the configured player and cleans up on
closure or invalidation. Captured payloads prove preparation and launcher
arguments; they do not establish real external player/audio hardware playback.

The imported ID/flag can be outside ordinary input ranges. Opening, Cancel and
no-op Apply preserve exact raw bytes. Only explicitly changed operands are
validated/written. Checkbox changes write original0/1 while untouched unknown
flags survive. Reset restores imports. Selection/typing hide an obsolete imported
ID hint; translation preserves pending inputs and hint state. Speech banks and
text files are read-only. Corrupt SDT/STR recover without crashing; valid audio
remains playable when only its caption is missing or corrupt.

The actual toolbar and creation wizard share this private page. Creation Back
stores submitted values; Cancel changes no live map bytes. Finish includes SAY
in one atomic creation edit. Live edit/Undo/raw mutation, closed/foreign owner,
resource reconfiguration and late generic field commits reject stale Apply or
playback authority. Resource configuration is captured with its owning scope.

Independent Core tests use captured original page7211/store/compiler and five
input hashes. Both native/project formats check ID0,1,2,9999 * flag0,1, exact
literal fields and compiler output, opaque/repeat bytes, missing-bank editing,
atomic/no-op history, save/reopen, stale/raw/foreign guards. WPF tests drive real
controls and double-click with owned synthetic SDT/STR, exact payload/player
arguments, private creation, translation and cleanup. Existing map speech/text
tests also cover actual corrupt SDT/STR and retained valid audio. Resource Core
checks independently read92 installed STR/99 SDT, checking all glyph entries,
headers and payloads without changing installed files.

Final01 automated checks passed16Core groups/48WPF cases/196renders per
configuration, but visual review rejected acceptance: selecting valid ID2 left
the obsolete imported-range warning visible. Snapshot01/logs/renders and
speech-final-01-visual-rejection.json are preserved diagnostic evidence.
The real list handler is corrected and WPF now checks import/list/typing/reset/
translation hint visibility. Final02 builds all have zero warnings/errors.
Snapshot02 fixes309C#/13fixtures/16DLL; SHA256:
bcd6ca0fec6bc5747609d343e25833c50af41c9b7ffb3c037c64be8544fae28f.
Final02 focus is terminal0, actual workers12424/6676 and test children gone.
Both configurations pass16Core groups/48WPF cases/196PNG with exact expected
names/dimensions. Minimum RU/project selected/creation and ReleaseRU/native
caption plus imported/missing bank renders were inspected. Immutable
speech-focused-verified-02.json confirms matching sources/project inputs/DLLs/
logs/renders and unchanged accepted spatial/production/original inputs.
This is focused speech acceptance, not production delivery or full-editor parity.

After speech acceptance, implement the
[full existing-event wizard/context behavior](SCENARIO_EXISTING_EVENT_WIZARD_SOURCE_SCOPE.md),
then complete candidate/full production regression and rebased source delivery.
Original GUI/external audio/system dialogs/hardware-DPI/game and other
FEATURE_PARITY requirements remain open.
