"""Reproduce the sound metadata survey on a local DK2 installation."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct

import dk2_formats as kwd
import dk2_sound_index as sound

ROOT = Path(__file__).resolve().parent
BACKGROUND = ("music", "ambience", "one_shot_atmos", "options_music")
REPO = "https://github.com/Dethware/DK2"
REF = "28adde55f91145abdaab2df900a0bb6994aa04db"


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def relative_key(path, root):
    return path.relative_to(root).as_posix().casefold()


def audit_sound_tree(root):
    archive_paths = {relative_key(p, root): p for p in sorted(root.rglob("*.sdt"))}
    archives, banks, effects = {}, {}, {}
    files, issues = {}, []
    clip_types = Counter()
    for name, path in archive_paths.items():
        raw = path.read_bytes()
        files[name] = digest(raw)
        try:
            clips = sound.read_sdt(raw)
        except sound.SoundFormatError as error:
            count = struct.unpack_from("<I", raw)[0]
            overruns = []
            if count <= (len(raw) - 4) // 4:
                for index, (offset,) in enumerate(struct.iter_unpack("<I", raw[4:4 + count * 4]), 1):
                    if offset + 8 > len(raw):
                        continue
                    header_size, data_size = struct.unpack_from("<II", raw, offset)
                    extra = offset + header_size + data_size - len(raw)
                    if extra > 0:
                        overruns.append(dict(entry=index, declared_end_past_EOF=extra))
            issues.append(dict(file=name, problem=str(error), entry_count=count, overruns=overruns))
            continue
        archives[name] = clips
        clip_types.update(f"{clip.type:#04x}" for clip in clips)
        spans = sorted((clip.offset, clip.offset + clip.header_size + clip.data_size) for clip in clips)
        assert all(end <= start for (_, end), (start, _) in zip(spans, spans[1:]))
    for path in sorted(root.rglob("*.map")):
        name = relative_key(path, root)
        raw = path.read_bytes()
        files[name] = digest(raw)
        try:
            if name.endswith("bank.map"):
                _, table = sound.read_bank(raw)
                banks[name[:-8]] = [sound.text(label).replace("\\", "/") for _, label in table]
            elif name.endswith("sfx.map"):
                _, _, effects[name[:-7]] = sound.read_sfx(raw)
        except sound.SoundFormatError as error:
            issues.append(dict(file=name, problem=str(error), signature=raw[:16].hex()))
    counts = Counter()
    bad_refs = Counter()
    for category, groups in effects.items():
        names = banks.get(category)
        for group in groups:
            for item in group.items:
                for row in item.links:
                    target, _, _, low, high = struct.unpack("<I4B", row)
                    assert 1 <= target <= len(group.items)
                    if low > high:
                        counts["reversed_link_ranges"] += 1
                if names is None:
                    continue
                for row in item.sounds:
                    clip, weight, duration, bank = struct.unpack("<4I", row)
                    counts["mapped_sound_rows"] += 1
                    assert 1 <= bank <= len(names)
                    prefix = names[bank - 1].casefold()
                    for suffix in ("hd", "hw"):
                        key = prefix + suffix + ".sdt"
                        if key not in archive_paths:
                            continue
                        if key not in archives:
                            counts[f"{suffix}_unparsed_archive"] += 1
                        elif 1 <= clip <= len(archives[key]):
                            counts[f"{suffix}_resolved"] += 1
                        else:
                            bad_refs[f"{category}/{suffix}"] += 1
    background = {}
    for category in BACKGROUND:
        key = "global/" + category
        rows = []
        for group in effects[key]:
            link_count = sum(len(item.links) for item in group.items)
            sounds = [struct.unpack("<4I", row) for item in group.items for row in item.sounds]
            empty = []
            for index, weight, duration, bank in sounds:
                path = banks[key][bank - 1].casefold() + "hd.sdt"
                clip = archives[path][index - 1]
                assert 1 <= index <= len(archives[path])
                if clip.data_size == 0:
                    empty.append([bank, index])
            rows.append(dict(id=group.id, flags=struct.unpack_from("<H", group.raw, 0x10)[0],
                             parameter_slot_0=group.raw[0x12], items=len(group.items),
                             sound_rows=len(sounds), links=link_count, empty_clip_refs=empty,
                             exact_duplicate_link_rows=sum(len(i.links)-len(set(i.links)) for i in group.items)))
        background[category] = dict(archive_names=banks[key], groups=rows)
    return dict(banks=len(banks), sfx_maps=len(effects), sdt_valid=len(archives),
                sdt_total=len(archive_paths), clip_types=dict(clip_types), counts=dict(counts),
                unpaired=sorted(banks.keys() ^ effects.keys()), issues=issues,
                out_of_range_clip_rows=dict(bad_refs), background=background, file_sha256=files), banks, effects, archives


def catalogue_bindings(data, banks, effects, archives):
    layouts = json.loads((ROOT / "layouts.json").read_text(encoding="utf8"))
    rows = []
    for tag, family in kwd.FAMILIES.items():
        fields = [f for f in layouts[family]["fields"] if "sound" in f["name"].casefold() and f["size"] == 32]
        if not fields:
            continue
        path = data / "editor" / f"{family}.kwd"
        raw = path.read_bytes()
        sections = kwd.parse(raw)
        assert b"".join(section.pack() for section in sections) == raw
        records = [record for section in sections if section.signature == tag for record in section.records]
        for field in fields:
            offset = field["offset"]
            labels = Counter(sound.text(record[offset:offset + 32]) for record in records)
            blank = labels.pop("", 0)
            missing = [label for label in labels if ("global/" + label.casefold()) not in (banks.keys() & effects.keys())]
            rows.append(dict(file=path.name, sha256=digest(raw), field=field["name"], offset=offset,
                             records=len(records), empty=blank, matched=len(records)-blank-sum(labels[x] for x in missing),
                             missing_pairs=sorted(missing), labels=dict(sorted(labels.items()))))
    examples = []
    for category, group_id in [("creature_warlock", 195), ("keeper_spell_heal", 239)]:
        key = "global/" + category
        group = next(group for group in effects[key] if group.id == group_id)
        for item in group.items:
            for raw in item.sounds:
                index, weight, duration, bank = struct.unpack("<4I", raw)
                archive = banks[key][bank - 1] + "HD.sdt"
                clip = archives[archive.casefold()][index - 1]
                examples.append(dict(category=category, group=group_id, bank=bank,
                                     archive=archive, index=index, clip=clip.name,
                                     sample_rate=clip.sample_rate, sdt_type=clip.type, duration_ms=duration))
    return rows, examples


def level_choices(data):
    result = Counter()
    unreadable = []
    files = sorted((data / "editor").rglob("*.kwd"))
    for path in files:
        try:
            sections = kwd.parse(path.read_bytes())
        except ValueError:
            unreadable.append(path.relative_to(data).as_posix())
            continue
        for section in sections:
            if section.signature != 0xDC:
                continue
            size = struct.unpack_from("<I", section.header, len(section.header) - 4)[0]
            layout = kwd.LEVEL_LAYOUTS.get(f"Level_{size}")
            if layout is None:
                continue
            offset = next(f["offset"] for f in layout["fields"] if f["name"] == "SoundTrack")
            result[section.data[offset]] += 1
    return dict(scanned_kwd=len(files), known_level_sections=dict(sorted(result.items())), rejected_kwd=unreadable)


def verify(data):
    stock, banks, effects, archives = audit_sound_tree(data / "Sound" / "Sfx")
    alternate, *_ = audit_sound_tree(data / "Sound" / "Sfx  rus")
    rows, examples = catalogue_bindings(data, banks, effects, archives)
    assert not stock["issues"] and stock["sdt_valid"] == 142
    assert stock["banks"] == 204 and stock["sfx_maps"] == 205
    assert stock["counts"]["mapped_sound_rows"] == 6772
    assert sum(row["matched"] for row in rows) == 243
    assert {label.casefold() for row in rows for label in row["missing_pairs"]} == {
        "object_heart", "object_training_target", "spell_wind"
    }
    assert [g["id"] for g in stock["background"]["music"]["groups"]] == [343, 345]
    assert [g["items"] for g in stock["background"]["music"]["groups"]] == [135, 156]
    assert [len(stock["issues"]), len(alternate["issues"])] == [0, 35]
    extent_issues = [row for row in alternate["issues"] if row["file"].endswith(".sdt")]
    assert len(extent_issues) == 32 and all(row["overruns"] == [
        dict(entry=row["entry_count"], declared_end_past_EOF=40)] for row in extent_issues)
    return dict(source=dict(repo=REPO, revision=REF, paths=["docs/file-formats.md", "docs/runtime-behaviour.md"]),
                stock=stock, alternate=alternate, kwd_bindings=rows,
                resolved_examples=examples, level_tracks=level_choices(data))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("data", type=Path, help="installed Dungeon Keeper 2 Data directory")
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    report = verify(args.data)
    args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf8")
    print("Stock", report["stock"]["banks"], report["stock"]["sfx_maps"], report["stock"]["sdt_valid"])
    print("Linked KWD references", sum(row["matched"] for row in report["kwd_bindings"]))
    print("Level tracks", report["level_tracks"]["known_level_sections"])
    print("Examples", report["resolved_examples"])


if __name__ == "__main__":
    main()


