# Actual Russian SDT MPEG decoding audit

`audit_sdt_decode.py` loads the already installed Windows x64 FFmpeg DLLs from `C:/Program Files/FFmpeg For Audacity`. No `ffmpeg.exe`, PyAV, download or installation is used. The Python implementation uses only ctypes and the standard library.

The code maps only the documented leading fields of AVPacket/AVFrame, checks pointer width and libavcodec59/libavutil57 major versions, and records DLL SHA-256 and field offsets. FFmpeg 5 public structure definitions: [AVFrame](https://github.com/FFmpeg/FFmpeg/blob/n5.1.2/libavutil/frame.h), [AVPacket](https://github.com/FFmpeg/FFmpeg/blob/n5.1.2/libavcodec/packet.h), [send/receive API](https://ffmpeg.org/doxygen/5.1/group__lavc__encdec.html).

Each source archive is checked against the prior `sound-russian-sdt-length-audit.json` SHA-256. Physical payloads are delimited by the next directory offset or EOF because these pinned files overstate the payload size by40. Their final NUL is excluded only after checking the known 720-byte frame pattern. Each MPEG frame is submitted as an AVPacket to FFmpeg's `mp2` decoder. Samples and sample formats come from returned AVFrame objects; sample rate/channel count come from the decoder context's `ar`/`ac` options after each successful receive. PCM plane bytes are hashed, not written. The decoder is flushed with a null packet and drained to EOF; contexts, packets and frames are freed in finally blocks.

```powershell
python -B Tools/dk2-format-research/sdt-decode/audit_sdt_decode.py 'Dungeon Keeper 2/Data/Sound/Sfx  rus' --limit 1 --report Tools/dk2-format-research/sdt-decode/first-clip-report.json
python -B Tools/dk2-format-research/sdt-decode/audit_sdt_decode.py 'Dungeon Keeper 2/Data/Sound/Sfx  rus' --report Tools/dk2-format-research/sdt-decode/russian-decode-report.json
```

Optional `--file speech_level1/speech_level1hd.sdt --index 1` selects one1-based record. `--dll-dir` and `--baseline` are configurable. Reports must be JSON outside the source root. Any unexpected decode error aborts with an exception instead of silently publishing a partial success report. Successful report totals therefore contain `decode_errors:0`.

## Observed result,2026-11-13

- First pinned clip:203 decoded frames,233856 samples/channel,467712 PCM bytes. Actual decoder output:s16p,32000Hz,mono.
- Complete pinned corpus:3385 clips from32 archives,150739 decoded frames,173651328 samples/channel,347302656 decoded PCM bytes.
- Every returned frame:1152 samples/channel,s16p,32000Hz,mono. All clips drained to EOF. Every decoded sample count matches the previous header-implied count.
- All3385 nominal SDT `pcmBytes` values differ from the actual untrimmed decoded PCM size; no game-side interpretation is established here.
- Negative control: a720-byte all-zero packet was rejected by `avcodec_send_packet` with `AVERROR_INVALIDDATA` (-1094995529). This demonstrates error propagation rather than assuming successful decode from header positions.
- Full corpus decode took approximately4seconds on this machine.

The DLL hashes and source hashes are in each report. PCM hash order is per returned frame then per plane, excluding linesize padding. No game files or audio output files were written. This is successful FFmpeg decoding, not game playback, CRC validation, perceptual fidelity proof or a specification for game-side sample-rate/pcmBytes handling. FFmpeg may conceal some damaged media; the present negative control is deliberately invalid framing. The original metadata audit remains a header-only experiment; this report is the separate decoding evidence.