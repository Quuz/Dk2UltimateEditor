# Original random painting: real remaining editing capability

This source audit identified a genuine missing feature before implementation. The [implemented random/GDI workflow](RANDOM_PAINTING.md) now supersedes that missing-status checkpoint; the source requirements below remain evidence.

The preserved `Map_Display.c::Map_PaintElement` implements five real randomized shape modes: outlined/filled rectangle, outlined/filled circle and line. `Map_Mouse.c::Mouse_LeftButtonUp` maps Shift on mouse release from each ordinary tool to its randomized mode, then records one rectangular undo operation. This is not a random terrain palette: each accepted cell receives the single selected translated terrain/owner/variant/control value.

The routine calls `randomize()` and samples `random(Config.RandomFreq)` once per cell of the corrected bounding rectangle, in row-major order, before testing shape pixels and protected cells. Only zero samples paint, so the probability is 1/frequency. `Properties_Editor_Config.c` initializes `Config.RandomFreq` to 2. The source calls that setting directly; a user-facing frequency control/range has not yet been established by this audit.

Special placements with minimum width or height other than one force a fully filled rectangle even when Shift selected a randomized mode. The ordinary temporary shape mask and original protected-cell rules still apply. Original source no-op operations may increment usage/change counters; the managed editor's no-op history policy should remain explicit rather than inventing pixel changes.

At the audit checkpoint, `MapCanvas.DrawGesture` mapped only ordinary drawing tools and did not pass Shift/random policy or sample a subset. It was therefore genuinely missing, unlike the original general-search stub. It is now implemented with native/project, EN/RU, source sampling/placement/protection, atomic undo/redo, save/reopen and actual-control tests. The independent source GDI-mask comparison is also implemented. Historical PRNG seed/sequence identity, physical input and gameplay remain separate evidence; random success is not inferred from ordinary-shape tests.
