# Original scenario speech page scope

ACTION_SAY is the remaining parameter page in the spatial candidate.
It is not implemented there or in production. The independent immutable
Test/scenario-speech-source-scope-20261009-01.json captures page7211,
original Init/Check/Store/callback/default/compiler branches and complete
voice-list/playback/corresponding-text helpers. Five original input hashes
are captured; this audit is source evidence, not feature acceptance.

Original page7211 contains the sound list401, read-only caption preview402,
numeric speech identifier307 limited to four digits, and suppress-text
checkbox308. Stored parameter1 is the unsigned speech ID,0..9999 in human
input. Sound identifiers start at1;0 is an editable no-sound value. Stored
parameter5 is1 for checked and0 for unchecked. Native compiler payload is
WORD parameter1 at8, two zero bytes, BYTE parameter5 at12, then three zeros.

Init reads the current map's SpeechFile bank and creates the named list.
Selection writes the one-based ID and reads the corresponding STR caption.
Double-click invokes the actual sound payload. Missing resources do not
invent an ID or change the map. The voice helper enumerates SDT table order;
its playback indexes Table[Identifier-1], requires a40-byte sound header,
and extracts the original payload for a configured player. Corresponding
text uses the bank suffix after "_" plus ".STR"; MENTOR uses Speech.STR.

Existing GameSpeechBank, GameTextResource, MapTextCatalogues and
GameAudioPreview provide verified parsing and preview infrastructure.
MapSpeechTextWindow already uses these for map properties and supplies an
injectable PlaybackLauncher. Scenario editing must use the map's bank,
private parameter inputs and the creation wizard's lifetime rather than
repurposing a map-properties edit.

Required next work:

- Complete named speech list, caption preview, typed0..9999 ID, suppress-text
  checkbox and explicit playback/double-click in EN/RU.
- Preserve original sparse/unknown imported data on opening/Cancel; audit
  explicit Store semantics, exact native/project fields and padding.
- Compose privately through outer creation Back/Next/Cancel/Finish, and use
  the ordinary toolbar for existing events with one atomic Undo.
- Check missing bank/text, malformed SDT/STR, high/zero IDs, real names,
  one-based selection and exact independently supplied audio bytes.
- Check translation, generic pending commit, current/stale/raw/foreign/closed
  owner and preview-file ownership/cleanup without changing game resources.
- Verify actual installed bank/player/dialog behavior as part of the broader
  original-GUI/audio/hardware-DPI/in-game acceptance; mocked playback does
  not establish audible playback.

Use a NEW copy after the spatial focus is accepted. Never edit accepted
creation/spatial snapshots or production source while full regression is live.
