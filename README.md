# Dungeon Keeper 2 Ultimate Editor

[Русский](docs/README_RU.md) · [Contributing](CONTRIBUTING.md) · [Documentation](docs/INDEX.md) · [DK2 format specifications](docs/formats/README.md)

**UltimateEditor** is a Windows map and scenario editor for Dungeon Keeper 2. It rebuilds [NovFR’s map editor](https://github.com/NovFR/DK2-Map-Editor) in C# and WPF, with English and Russian interfaces, Unicode text support, undo/redo and an optional local MCP bridge for automation.

**Status: development / pre-release.** The editor can create, open and edit maps, but complete feature parity and manual acceptance are still in progress. Please work on copies of valuable maps. The current limits and verified results are recorded in the [acceptance checklist](docs/MANUAL_REMAINING_20261104.md).

## What you can work with

- Map terrain, rooms, ownership, objects, creatures and player settings.
- Scenario conditions and actions, hero parties, areas, patrol routes and camera paths.
- Game definitions, availability, pools and map variables.
- Native KWD maps, legacy DKM projects, text MAP imports and selection archives.
- English and Russian UI, document history, validation and copy export.
- A local MCP interface that operates on the same document as the desktop editor.

**Saving currently uses DKM/MAP and may keep a companion ZIP for original native data.** Native export or compilation produces the official seven-file map bundle in `Data/Editor/Maps`; common definitions are separate. Moving normal Save to KWD without DKM/ZIP is a planned next stage, not a completed feature. See [save and export instructions](docs/MANUAL_ACCEPTANCE_RU.md).

## How to run

There is no tagged stable release yet. Build from source, or download `UltimateEditor-windows` from a successful [Windows build](https://github.com/Quuz/Dk2UltimateEditor/actions/workflows/windows-build.yml) when one is available.

1. Extract the complete build folder; keep its DLLs and other files together.
2. Install the **.NET 10 Desktop Runtime for Windows** if it is not already installed. The current build is framework-dependent.
3. Run `App/UltimateEditor.exe`.
4. Open a copy of your map. A Dungeon Keeper 2 installation supplies the game definitions and is needed for testing the result in game.

For MCP automation, run `Start-Editor-MCP.cmd` instead. MCP is opt-in; normal startup does not enable it. The [MCP guide](docs/EDITOR_MCP.md) describes the named pipe, workspace restrictions and tool protocol.

To test a level in the game, use `DKII-DX.exe -q -level <map-name>` after putting its complete compiled map bundle in the game’s `Data/Editor/Maps` directory. Launching a compiled file is not proof that every edited feature works in game.

## How to report a bug

Open a [GitHub issue](https://github.com/Quuz/Dk2UltimateEditor/issues) with:

1. The build or commit, Windows version, UI language and display scale.
2. Exact steps, expected result and actual result.
3. A screenshot or error text, and a minimal test map when you can share it.
4. Whether the same map opens in the official editor and in the game.

Use one issue for each problem. Data loss, crashes, wrong map output and broken editing operations take priority. Do not attach private maps or credentials to public issues.

## For developers

### Build requirements

- Windows with the **.NET 10 SDK**. `global.json` pins 10.0.100 with patch roll-forward.
- PowerShell and Git.
- A local Dungeon Keeper 2 installation for game/corpus acceptance; it is not needed just to compile the solution.

No additional NuGet feeds are needed: `NuGet.Config` clears external package sources.

### How to build

```powershell
git clone https://github.com/Quuz/Dk2UltimateEditor.git
cd Dk2UltimateEditor
./build.ps1
```

The resulting application is `artifacts/PreFinal/App/UltimateEditor.exe`; the bridge is under `artifacts/PreFinal/Mcp`. The build script compiles the complete solution and publishes both components. Run `./build.ps1 -Validate` for the broader validation runner; corpus and interactive checks have additional prerequisites and are documented separately. A successful build alone does not close manual acceptance.

### Repository layout

| Path | Purpose |
| --- | --- |
| `src/DK2MapEditor.App` | Current WPF UI and localization |
| `src/DK2MapEditor.Core` | Map formats, editing, history, validation and compiler |
| `src/DK2MapEditor.Mcp` | Local MCP server |
| `tests` | Validation programs, WPF checks and retained fixtures |
| `Source` | Preserved original C implementation and resources used as references/build inputs |
| `Binaries` | Preserved upstream assets and original editor files |
| `docs` | Format notes, implementation decisions and acceptance evidence summaries |
| `distribution` | Launch scripts |

`DK2MapEditor.slnx` is the single current solution. The internal project and assembly names remain `DK2MapEditor.*`; the product name and published executable are **UltimateEditor**. Historical implementation notes are [preserved here](docs/PROJECT_HISTORY_RU.md). Generated builds, local evidence logs and archives are excluded from Git.

Read [CONTRIBUTING.md](CONTRIBUTING.md) before changing map serialization. Current development context is in [CURRENT_STATUS.md](docs/CURRENT_STATUS.md); documentation counts describe dated acceptance scenarios, not a completion percentage.

## Credits and provenance

- **NovFR** — original French Dungeon Keeper 2 map editor and preserved C source/resources.
- **Quuz** — UltimateEditor maintainer: enhancements, ongoing development and new features, with contributions from the community.
- **DiaLight** — author of [Flame](https://github.com/DiaLight/Flame), joining the ongoing development of UltimateEditor.
- [Flame](https://github.com/DiaLight/Flame) — related Dungeon Keeper 2 community project. UltimateEditor is a separate project.

Dungeon Keeper 2 and third-party materials belong to their respective rights holders. This repository preserves upstream attribution; it does not grant a new blanket license over the original source, game assets or contributed material. See [NOTICE.md](NOTICE.md).
