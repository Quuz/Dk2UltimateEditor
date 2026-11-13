# SDT structural roundtrip audit

Standalone Python 3.9+ standard-library tool. It reads SDT archives and rebuilds their directory and parsed record components **in memory**. It writes only a JSON metadata report; it cannot write a rebuilt SDT file.

The strict model checks the count/table extent, strictly increasing unique offsets, fixed/extended header boundaries and payload boundaries against the **next record**, not only EOF. This is a conservative policy for a future writer, not a complete specification of native game loader acceptance.

The model preserves unknown header bytes, header extensions, opaque bytes before the first record, payload bytes, inter-record gaps and trailing bytes. `Archive.rebuild()` regenerates count/offsets and concatenates those components. It does not retain and return an opaque copy of the whole input file.

```powershell
python -B -m unittest discover -s Tools/dk2-format-research/sdt-roundtrip -v
python -B Tools/dk2-format-research/sdt-roundtrip/audit_sdt_roundtrip.py 'Dungeon Keeper 2/Data/Sound/Sfx' --report Tools/dk2-format-research/sdt-roundtrip/stock-report.json
python -B Tools/dk2-format-research/sdt-roundtrip/audit_sdt_roundtrip.py 'Dungeon Keeper 2/Data/Sound/Sfx  rus' --report Tools/dk2-format-research/sdt-roundtrip/russian-report.json
```

`--report` must be a `.json` path outside the input directory. The tool exits 2 for unexpected structural rejections, 0 for exact roundtrips and separately recognized length40 packaging patterns. Recognition checks all records against the previous Russian audit's fixed MPEG header-slot pattern, but does not decode audio or validate CRC. It is not a general-purpose repair heuristic.

For the known length40 pattern, all physical record lengths are 40 bytes below their declared payload lengths. A candidate is corrected, parsed and rebuilt only in memory, then all changed fields are restored and compared to the original input. Original malformed files are **not** reported as strict-valid roundtrips. Candidate SHA-256 is recorded separately.

## Observed corpus, 2026-11-13

- Stock `Sfx`: 142 files, 8,959 records; all exact identity roundtrips.
- Alternate `Sfx  rus`: 145 files, 9,139 records; 113 exact identity roundtrips and 32 length40 packaging-pattern files (3,385 records).
- 20 synthetic boundary checks passed, including payload overlap within EOF, duplicate/unordered offsets, header truncation, extended headers/gaps pattern/inverse behavior and offset recalculation after a synthetic payload size change.

Reports contain per-file SHA-256 and counts, no audio payloads. No game files were written. No decoding, playback, replacement encoding or native loader was executed. Identity roundtrip establishes byte preservation, not successful game playback or completeness of metadata semantics.