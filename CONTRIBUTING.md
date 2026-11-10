# Contributing to UltimateEditor

Use issues to agree on larger changes and pull requests for review. Keep each change focused and include the reproduction case, before/after behavior and validation you actually ran. English and Russian reports are welcome.

## Start here

- Build the single `DK2MapEditor.slnx` solution with `./build.ps1` on Windows and .NET 10.
- Read [current status](docs/CURRENT_STATUS.md), [feature parity](docs/FEATURE_PARITY.md) and the [remaining acceptance cases](docs/MANUAL_REMAINING_20261104.md).
- The historical C code in `Source` is the behavior/format reference. Current implementation belongs in `src`; do not create another standalone version or recovery chain.
- Preserve original authorship and resources. Review [NOTICE.md](NOTICE.md) before adding third-party material.

## Editing and map formats

Use independent drafts for dialogs. Cancel must leave the live document untouched; a completed operation must have an exact, atomic Undo. Preserve record order, identities, script links and unknown bytes. Never silently repair a user map or discard unsupported data.

Export/compile official map bundles as seven files in Maps; shared definitions and editor metadata belong outside that directory. Normal Save still uses DKM/MAP with a companion source ZIP where needed. A KWD-only Save migration is a separate planned task after acceptance.

Update both `Strings.en.xaml` and `Strings.ru.xaml` for user-facing changes. Do not treat a source-level test or WPF harness as proof of physical keyboard, audio, DPI or in-game behavior.

## Verification

Run the checks relevant to your change and report command, exit code and scope. The repository has standalone validation executables rather than a single `dotnet test` suite. The broader runner is available with `./build.ps1 -Validate`; some cases need local game files. Use copies for game testing, preserve installed maps and restore any temporary test slot. Never commit personal game directories, logs, secrets, generated artifacts or private maps.

For a defect report, include the exact commit/build, steps and minimal shareable input. One issue per problem. For a PR, explain the trigger, resulting behavior, tests and remaining limitations. Do not mark manual checklist cases passed without direct evidence matching their scope.
