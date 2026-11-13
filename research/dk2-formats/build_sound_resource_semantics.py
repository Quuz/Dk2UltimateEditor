"""Integrate the local sound corpus and DX music evidence into the format package."""
import hashlib
import json
from pathlib import Path
import struct

from build_effect_semantics import EXE, pe_read
import dk2_formats as kwd

ROOT = Path(__file__).resolve().parent
KEY = "sound_resource_semantics"
SCHEMA = "sound-resource-semantics.json"
BASE = ROOT / "phase204-sound-prior-package.json"
NOTE = ("sound_resource_semantics links KWD sound categories to installed BANK/SFX/SDT and verifies "
        "DX battle music thresholds with native file and creature definition loaders. "
        "Keep literal unresolved labels, duplicate links and unknown bytes.")


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read(name):
    return json.loads((ROOT / name).read_text(encoding="utf8"))


def save(name, value):
    (ROOT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf8")


def link(name):
    return dict(file=name, sha256=digest((ROOT / name).read_bytes()))


def physical_controls():
    directory = EXE.parent / "Data" / "editor"
    specifications = [
        ("Creatures.kwd", 170, 0xF12, 2, 0xF4B, 5, 500),
        ("GlobalVariables.kwd", 230, 4, 4, 0, 216, 501),
    ]
    rows = []
    for filename, tag, offset, width, key_offset, key, value in specifications:
        raw = (directory / filename).read_bytes()
        sections = kwd.parse(raw)
        section = next(section for section in sections if section.signature == tag)
        key_width = 4 if tag == 230 else 1
        index = next(i for i, record in enumerate(section.records)
                     if int.from_bytes(record[key_offset:key_offset + key_width], "little") == key)
        records = section.records.copy()
        record = records[index]
        encoded = value.to_bytes(width, "little")
        records[index] = record[:offset] + encoded + record[offset + width:]
        address = section.offset + len(section.header) + sum(map(len, records[:index])) + offset
        section.replace_records(records)
        rebuilt = b"".join(part.pack() for part in sections)
        assert rebuilt == raw[:address] + encoded + raw[address + width:]
        assert b"".join(part.pack() for part in kwd.parse(rebuilt)) == rebuilt
        assert rebuilt[:address] + raw[address:address + width] + rebuilt[address + width:] == raw
        rows.append(dict(file=filename, source_sha256=digest(raw), record=index,
                         absolute_offset=address, record_offset=offset, width=width, value=value))
    filename = "AutoSave.kwd"
    raw = (directory / filename).read_bytes()
    sections = kwd.parse(raw)
    level = next(section for section in sections if section.signature == 0xDC)
    field_offset = 0x63B7
    address = level.offset + len(level.header) + field_offset
    assert address == 0x63EF and raw[address] == 3
    changed = raw[:address] + bytes([2]) + raw[address + 1:]
    assert b"".join(part.pack() for part in kwd.parse(changed)) == changed
    assert changed[:address] + raw[address:address + 1] + changed[address + 1:] == raw
    rows.append(dict(file=filename, source_sha256=digest(raw), absolute_offset=address,
                     record_offset=field_offset, width=1, value=2))
    return rows


def primary():
    source = EXE.read_bytes()
    rows = []
    for path in sorted((ROOT / "ghidra").glob("sound-DX-*-memory.txt")):
        value = json.loads(path.read_text(encoding="utf8"))
        address = int(value["address"], 16)
        data = bytes(value["data"])
        assert pe_read(source, address, len(data)) == data
        rows.append(dict(file=path.name, sha256=digest(path.read_bytes()),
                         address=address, size=len(data), PE_bytes_sha256=digest(data)))
    assert {r["address"] for r in rows} >= {0x505310, 0x5674F0, 0x567BC0, 0x66F848, 0x6BDB70}
    assert struct.unpack("<4I", pe_read(source, 0x6BDB70, 16)) == (341, 343, 344, 345)
    assert struct.unpack("<I", pe_read(source, 0x66E3EC + 0x264, 4))[0] == 0x508C60
    assert pe_read(source, 0x508C60, 8) == bytes.fromhex("668b8143720000c3")
    return rows


def build():
    index = read("sound-index-validation.json")
    replays = read("sound-DX-music-replays.json")
    assert replays["original_EXE_sha256"] == digest(EXE.read_bytes()) and len(replays["replays"]) == 15
    stock = index["stock"]
    assert sum(row["matched"] for row in index["kwd_bindings"]) == 243
    return dict(
        phase=204,
        date="2026-11-13",
        external_reference=dict(**index["source"], scope="Steam executable and format hypotheses. Local claims are checked separately."),
        local_build=dict(profile=EXE.name, sha256=replays["original_EXE_sha256"]),
        corpus=dict(banks=stock["banks"], sfx_maps=stock["sfx_maps"], sdt_archives=stock["sdt_valid"],
                    clip_types=stock["clip_types"], sound_rows=stock["counts"]["mapped_sound_rows"],
                    alternate_structural_issues=len(index["alternate"]["issues"]),
                    primary_report=link("sound-index-validation.json")),
        layout=dict(
            bank=dict(signature="012c61e9d031d211b40900a0c993f203", count_offset=24,
                      opaque_row_bytes=11, name_length_bytes=4, index_base=1),
            sfx=dict(signature="002c61e9d031d211b40900b0c993f203", count_offset=24,
                     entry_bytes=24, group_bytes=20, item_bytes=42, sound_bytes=16, link_bytes=8,
                     directory_order=["entries", "groups for every entry", "item headers for one group", "sounds then links per item"],
                     sound_bank_index_offset=12, sound_clip_index_offset=0, indices_base=1,
                     opaque_fields="Preserve all bytes. Link duplicates and their order carry information."),
            sdt=dict(count_offset=0, absolute_entry_offsets_start=4, observed_entry_header_bytes=40,
                     name_offset=8, name_bytes=16, sample_rate_offset=24, sample_bits_offset=26,
                     type_offset=27, pcm_bytes_offset=32, payload_offset="entry offset + declared header size")),
        catalogue=dict(fields=index["kwd_bindings"], examples=index["resolved_examples"],
                       scope="Literal case-insensitive Global pair resolution on installed files. Runtime fallback remains open."),
        level_music=dict(raw_level_offsets={"25603": 0x63B7, "25683": 0x63B7}, first_section_file_offset=0x63EF,
                         runtime_offset=0x720D, choice_table_address=0x6BDB70,
                         choices={"0": 343, "1": 343, "2": 344, "3": 345},
                         local_track2_group_exists=False, counts=index["level_tracks"]["known_level_sections"],
                         scope="Native manager byte read and table lookup verified statically. Choices 0–3 only. Audio output untested."),
        battle_music=dict(
            consumer=0x505310, raiser=0x567BC0, manager_level_offset=0x2C,
            threshold_ids=[215, 216, 217], threshold_addresses=[0x6F29B5, 0x6F29B9, 0x6F29BD],
            source_thresholds=[100, 500, 1500], comparison="Unsigned DWORD, inclusive, tests 217 then 216 then 215.",
            threat_raw_offset=0xF12, threat_definition_offset=0x698, threat_bytes=2,
            growth_type=65, growth_stat=3, growth_word_formula="s16(globals + 0x105 + 2*level)",
            score_formula="Level 1 uses base u16. Other selected levels use signed product / 100 truncated toward zero. Accumulation and threshold comparison use u32.",
            ownership="Battle+0 points to prepared original-class World. vtable+0x264 -> 0x508C60 reads World+0x7243. Equal actor owner is excluded.",
            cases=replays["replays"], replay_report=link("sound-DX-music-replays.json"),
            scope="Prepared one-member battle and manager. Native Variables parser, raw Creature loader for three controls, scorer and original raise method. Scheduler, battle recruitment and audio output are open."),
        background=stock["background"],
        physical_preservation=physical_controls(), primary=primary(),
        helpers=[link(name) for name in ("dk2_sound_index.py", "audit_dk2_sound_index.py", "probe_sound_music_dx.py")])


def main():
    package = read("editor-format-package.json")
    if not BASE.exists():
        assert KEY not in package
        save(BASE.name, dict(components=package["components"], codec=link("dk2_formats.py")))
    old = read(BASE.name)
    assert all(package["components"][key] == value == link(value["file"]) for key, value in old["components"].items())
    assert old["codec"] == link("dk2_formats.py")
    schema = build()
    save(SCHEMA, schema)
    package[KEY] = schema
    package["components"][KEY] = link(SCHEMA)
    if NOTE not in package["precedence"]:
        package["precedence"].append(NOTE)
    save("editor-format-package.json", package)
    assert len(package["components"]) == len(old["components"]) + 1
    print("Sound schema verified", len(package["components"]), "components", len(schema["primary"]), "PE evidence ranges")


if __name__ == "__main__":
    main()

