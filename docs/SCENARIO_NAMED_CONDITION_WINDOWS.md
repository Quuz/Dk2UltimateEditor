# Named scenario contexts and condition windows

The shared parameter window now implements 28 additional original condition
pages. Together with the previous 19 scalar pages it covers 47 of the 69
original parameter pages, plus the 26 repeat-only condition pages. The remaining
22 parameter pages, complete alias/camera parent composition and wider
FEATURE_PARITY requirements remain required.

## Source-specific values

ScenarioNamedCatalogues implements 55 contexts from 56 direct source list calls.
The three other source area-list calls fill coordinates through callbacks; they
are not fictitious serialized area-ID controls. Ordinary named suggestions now
use these contexts and current map player/band/area/effect identities.

Creature contexts distinguish Any and elite visibility; rooms store VarRooms
Variable IDs and distinguish Any/No room and special heart/portal choices.
Terrain stores high-byte physical IDs with source room/special flags. Player
lists preserve all seven IDs and source contextual Any; level controls store
1–10 rather than their zero-based combo indices. Band ID zero is selectable,
not a heading or None value. Dynamic action catalogues prepare later complete
parent pages; their presence alone does not close those pages. Native gate
selection still requires coordinate composition because no direct Param5 gate
ID exists in that format.

Interface controls have all 57 click, 54 show and 49 add entries in original
order, with category/ID pairs and source Hand-spell exclusion. A condition
interface picker changes both operands in one transaction. Unknown imported
pairs display their separate category and ID and survive browsing/Cancel.

Destroyed combines the original door and trap rows without deduplicating
overlapping IDs. Language changes preserve the pending selected row as well as
its value. A reopened stored ID remains inherently category-ambiguous: the map
does not encode a separate door/trap category for this field.

## Complete condition controls

The added pages cover creature experience/count/mood/loss/kill/level, room
count/type/size/content, doors/traps/destruction/spells, selected interface
element, heart destruction, possession/entry/claim/content/selection area
conditions and band creation/stun/jail/death counts. Source comparison/radio
encodings, four-digit counts and inactive/unused operands are preserved.

Destroyed-count uses the original unsigned GetDlgItemInt range without a text
limit: 0–4294967295. The full DWORD bit pattern is written to the actual LONG
operand, so 4294967295 persists as -1 bits and displays unsigned on reopening.
Untouched imported values are never coerced; the other numeric source limits
and physical byte/word bounds still apply. Unknown choices and unrelated map,
event, alias, link, clipboard and selection data stay unchanged. Apply uses one
shared immutable draft/history operation; outer Cancel discards pending input.

## Independent checks and current acceptance

`Test/capture_scenario_named_condition_pages.py` reads preserved original-source
audits, never production C#, and captures append-only
`tests/DK2MapEditor.Validation/ScenarioNamedPages.json`: 55 catalogue contexts,
source ordered values/check/store/resource bodies and all 28 condition schemas.
Original hashes are checked before and after capture.

Core tests compare every static catalogue to literal source values and check all
28 pages in both formats: every choice, count boundaries including full DWORD,
literal bytes, paired writes, sparse/hidden rejection, band zero, current player
names, source/storage guards, no-op and atomic Undo/Redo/batches. Actual WPF
checks all 28 commands/windows in EN/RU and both formats, 578 choice applications
per combination, unsigned display/write/reopen, overlapping-row translation,
unknown imports, Cancel and exact unrelated bytes/clipboard. Shared scalar
window lifetime/pending/raw/reentrance checks remain in the same focused run.

Final Debug/Release builds and Core focused checks have terminal exit 0, zero
build warnings/errors and three passing Core groups each. Both final actual WPF
runs are terminal at twelve passing cases, exit 0, including the unsigned and
duplicate-row refinements. Each configuration retains 16 named and 24 scalar
renders. EN project destroyed-count, EN native interface pair, RU project
creature-entry and RU native interface minimum-size layouts were inspected.
Focused proof is
`Test/editor3-scenario-named-pages-focused-verified-20261008-01.json`.

Full five-gate run `editor3-scenario-named-pages-20261008-01` is terminal and
accepted. `Test/verify_scenario_named_page_regression.py` returned exit 0 and
Verified true: Core 91/0, both UI configurations 328 PASS, corpus 1740/94 on
578 KWD with the complete raw-identical ordered failure list, and real
Ancients.map 92/0. All source/fixture/assembly hashes and file times were
captured, original source was unchanged, and all workers were verified gone.
Proof: `Test/editor3-scenario-named-pages-20261008-01-five-gates-verified.json`.

After that acceptance, two ordinary action captions were corrected using the
independent remaining-page oracle: SAY 0 displays text and 1 hides it;
CAMERAPATH 0 shows the ceiling and 1 hides it. A separate embedded semantic
fixture checks both languages and actual displayed WPF labels. Both builds,
focused Core and WPF pass; the caption verifier returned Verified true and
workers are gone. The full
proof above belongs to the accepted named-page snapshot before these caption
changes, and does not prove future action pages or the whole parity goal.
