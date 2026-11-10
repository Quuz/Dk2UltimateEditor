# Codex restart diagnostics — 2026-10-08

The editor goal is active in «Редактор 3.0». «Редактор 2.0» was archived with the app tool, not permanently deleted. Do not read the old chat again. Project sources, tests, continuation checkpoint, parity ledger and baseline corpus report were checked on disk.

## Current test evidence

Focused `--map-properties-only` completed with exit 0 and both groups passing. Full `editor3-map-properties-20261008-01-DebugCore` lost exec handle 84470, had no final JSON or terminal marker, and no matching live process. Full `editor3-map-properties-20261008-02-DebugCore` was started alone using hidden PowerShell PID 6448 at 01:24:17 Moscow. It was observed live, later disappeared along with its dotnet child; its final observed log ends after the original undo-depth group. No final JSON/exit marker exists. Neither full run is accepted. No replacement full run is currently presumed live.

## App and cache evidence

User confirmed repeated desktop-app exits, continuing after switching chats and archiving the old chat. Process start times and separate desktop logs confirm repeated launches during 01:23–01:32 Moscow. Installed Windows package: OpenAI.Codex 26.1002.7124.0; its desktop executable is ChatGPT.exe, while codex.exe is the backend. Checking only codex.exe does not inventory the desktop process.

At one observation, free RAM was 8693 MB of 16104 MB; C: had 9291808768 bytes free and D: 17645875200 bytes free. These are snapshots, not proof of resource use at the instant of failure. No matching recent Application 1000/1001/1026 events or System 2004 events were returned. No fatal, OOM, renderer-gone, GPU-process failure or cache/database-corruption message was found in the inspected latest logs. Repeated `ResizeObserver loop completed with undelivered notifications` interface errors are present; they do not prove the fatal cause.

Only file counts/sizes were inspected in named cache directories under `C:/Users/Quuz/AppData/Roaming/Codex/web/Codex`: Default/Cache 30.86 MB, Default/Code Cache approximately 0 MB, Default/GPUCache 5.93 MB, GPUPersistentCache approximately 0 MB, GraphiteDawnCache 0.53 MB, GrShaderCache 10.30 MB, ShaderCache 1.57 MB. These measured caches total approximately 49.19 MB. Crashpad had no .dmp files. The Windows Store virtualized Roaming/Local Codex folders also exist; their separate cache sizes have not yet been established. Cache size does not establish integrity or exclude a cache-related bug.

No cache, browser storage, credentials, sessions, app configuration or app installation was modified. No diagnostic information was sent externally. Official troubleshooting reference: https://learn.chatgpt.com/docs/reference/troubleshooting (read during this investigation; no specific verified cache fix was established).

## Next action

Avoid another full regression launch until the app interruption is understood or a durable independent run completes. Keep partial logs diagnostic only. If a cache reset is explicitly requested, first resolve the actual app-profile/cache paths, close the desktop app, preserve the named cache directories reversibly, and exclude Local Storage, IndexedDB, cookies, authentication and `.codex` databases/sessions. Clearing data while the app is running is not a verified fix.

The original full editing-parity objective and outstanding general-properties window remain unchanged.
