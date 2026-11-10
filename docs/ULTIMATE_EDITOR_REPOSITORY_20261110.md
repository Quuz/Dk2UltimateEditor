# UltimateEditor and repository launch — 10 November 2026

Product name: **Dungeon Keeper 2 Ultimate Editor**. Window/welcome/about name: **DK2 Ultimate Editor**, in both EN and RU. Published application: **UltimateEditor.exe**.

The application assembly and namespaces remain `DK2MapEditor.App` / `DK2MapEditor.*`. The published apphost is renamed by `build.ps1` and still loads `DK2MapEditor.App.dll`; no second codebase or wrapper implementation was introduced. Existing user settings remain under the previous DK2MapEditor settings path. Map formats, Undo and current save behavior are unchanged.

The about text credits NovFR, describes the current development status and links to https://github.com/Quuz/UltimateEditor. Product/file-description metadata also carries the new name. `Start-Editor-MCP.cmd` and current MCP launch instructions use `UltimateEditor.exe`.

## Public repository

The user initially selected private visibility, then explicitly changed the request to public. The target is **Quuz/UltimateEditor**, with English/Russian overviews, contribution guidelines, provenance notice, bug/PR templates and a Windows build workflow. The original NovFR history is retained. The historical root README was preserved in `PROJECT_HISTORY_RU.md` before replacing it with the contributor-facing overview.

Source inputs `Source/`, `Binaries/`, `src/`, tests and documentation belong to the same repository/solution. Generated `artifacts/`, archived working copies, build intermediates and local evidence are excluded. The credential used for GitHub access is read through the existing Git credential helper and is never written into project files, URLs or reports.

No new blanket license is assigned to the inherited source or game resources. `NOTICE.md` records provenance and the absence of an upstream license file.

## Verification

- Full `build.ps1`: exit0, zero compiler warnings/errors; app and MCP published.
- Actual published `UltimateEditor.exe --lang=en` and `--lang=ru`: both started and exposed the exact window title `DK2 Ultimate Editor`. These empty test processes were terminated after observation; no map was opened or edited. This is startup/title verification, not a new manual/game acceptance claim.
- Product metadata: `Dungeon Keeper 2 Ultimate Editor`; file description: `DK2 Ultimate Editor`.
- EN/RU resource dictionaries and README links are checked before publication; candidate files are scanned for common credential/private-key patterns and oversized Git objects.
- No unchanged broad regression suites are repeated for the branding change. The CI workflow builds and publishes; it does not declare full manual or game acceptance.

Local evidence and command results are under `artifacts/Evidence/UltimateEditor-20261110`, excluded from publication. The existing acceptance ledger remains **105 cases:6 passed,98 open,1 without a genuine historical input**. KWD-only normal Save is still planned after acceptance; the overall project goal remains active.
