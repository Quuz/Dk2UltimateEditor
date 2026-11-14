#!/usr/bin/env python3
"""Deterministic structural registry for the DK2 format publication JSON.

Reads exactly four pinned input files and reports a compact JSON registry plus a
Russian Markdown matrix. The CLI is read-only by default: nothing is written
unless --write is passed explicitly, and then only the two fixed files inside
this script's own directory.

The registry records what these four files actually contain. Missing literal
keys are listed as registry gaps; absence of a key here is never a claim that
the fact is unknown outside this snapshot. No format field names are invented.
"""

import argparse
import hashlib
import json
import os
import sys

EXPECTED_LAYOUTS = [
    "Terrain",
    "Rooms",
    "Doors",
    "Traps",
    "KeeperSpells",
    "CreatureSpells",
    "Creatures",
    "Creatures_5537",
    "Objects",
    "EffectElements",
    "Effects",
    "Shots",
]

EXPECTED_CONTAINERS = ["catalogue", "things", "triggers", "variables", "map", "level"]

EXPECTED_CATALOGUE_KEYS = [
    "Terrain",
    "Rooms",
    "Doors",
    "Traps",
    "Creatures",
    "Objects",
    "CreatureSpells",
    "KeeperSpells",
    "Shots",
    "Effects",
    "EffectElements",
]

SOURCE_FILES = [
    "format-package.index.json",
    "layouts.json",
    "coverage.json",
    "snapshot-manifest.json",
]

# Pinned input identities, copied from the review acceptance note
# (.bb/chats/thr_rhfc7scexe/notes/kilo-format-registry-acceptance.json,
# field input_sha256). Mismatch is a hard validation error, so a wrong
# --research-root cannot silently produce a plausible registry.
EXPECTED_SOURCE_SHA256 = {
    "format-package.index.json": "b87128dc941d1a5b1073e25a6638669d13f176094e8d09ef22bdfcf11d52aefe",
    "layouts.json": "d36009c8064b04073d46c186ee128506c84bfb4900f343db626ba9f2ccb7d767",
    "coverage.json": "77f04bf33b1a61762cb8030b949e2f875ae9433a0e9675687ca4256c2cb9aaf9",
    "snapshot-manifest.json": "0f40d12ec2b4494301b540e5cac60d0824a1b9ff435e93dc80be7ad2983f9e8b",
}

TERRAIN_EXPECTED_UNRESOLVED_ROWS = 18
EXPECTED_SCHEMA_VERSION = 1

OUTPUT_JSON = "format-registry.json"
OUTPUT_MD = "format-registry.RU.md"


class RegistryError(Exception):
    """Raised when the pinned inputs cannot be read or lack required keys."""


REGISTRY_GAPS = [
    "This registry does not audit a separate per-field endian/text-encoding table. "
    "Consult each schema kind and its effective component; package-wide byte_order is recorded.",
    "No rule selecting between Creatures (record_size 5449) and Creatures_5537 "
    "(record_size 5537) occurs in these four files. Both keys point to one "
    "effective_catalogue_layouts view and one replacement component "
    "combat_category_semantics (semantic_corrections[6]/[7]); catalogue_ids "
    "contains CreatureId only and no key for Creatures_5537.",
    "Labels marked unresolved refer to the historical baseline only. Source labels "
    "are preserved here; their current semantic status must be read in effective components.",
    "No '15 families' mapping key occurs in these four files; the only literal "
    "'family' key is effective_object_resource_bindings.family = 'Objects'.",
    "No primary runtime/native/roundtrip artifacts are carried by these four "
    "files; precedence statements only reference such work performed elsewhere "
    "(e.g. phase162 native counts, phase192 '15roundtrips').",
    "Effective value-range annotations and checksum rules need their referenced "
    "components. This structural registry does not independently validate those rules; "
    "catalogue_checksums references catalogue-checksums.json.",
    "No numeric coverage percentage occurs in these four files; coverage.json "
    "counts unresolved baseline rows and bytes only, and purpose explicitly "
    "disclaims verified gameplay for every named field.",
]

NOT_CLAIMS = [
    "Structural inventory of historical baseline layouts, not current semantic completeness.",
    "Absence of a metadata key here is a registry gap in these four files, not proof the "
    "fact is unknown elsewhere.",
    "Little-endian is cited from explicit package-wide package_metadata.byte_order, not "
    "inferred from a container key.",
    "Phase 205 / 182 machine components is a frozen snapshot; later supplements are not "
    "part of it.",
    "These four files carry no primary execution traces; precedence only references "
    "native/runtime/roundtrip work whose evidence lives outside this snapshot.",
    "No index file and no source schema was modified by this registry.",
    "No numeric coverage percentage is asserted here, and none is present in the inputs.",
]


def canonical_dumps(value):
    """Stable serialization used for every digest in this registry."""
    return json.dumps(
        value, sort_keys=True, ensure_ascii=False, separators=(",", ":")
    )


def sha256_text(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def sha256_file(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_documents(research_root):
    """Read the four pinned inputs; return (sources, documents)."""
    sources = {}
    documents = {}
    for name in SOURCE_FILES:
        path = os.path.join(research_root, name)
        if not os.path.isfile(path):
            raise RegistryError("missing pinned input: %s" % path)
        try:
            raw = open(path, "rb").read()
            documents[name] = json.loads(raw.decode("utf-8"))
        except (OSError, UnicodeDecodeError, ValueError) as exc:
            raise RegistryError("cannot read pinned input %s: %s" % (path, exc))
        sources[name] = {
            "sha256": hashlib.sha256(raw).hexdigest(),
            "bytes": len(raw),
        }
    return sources, documents


def hash_sources(research_root):
    sources = {}
    for name in SOURCE_FILES:
        path = os.path.join(research_root, name)
        if not os.path.isfile(path):
            raise RegistryError("missing pinned input: %s" % path)
        sources[name] = {
            "sha256": sha256_file(path),
            "bytes": os.path.getsize(path),
        }
    return sources


def field_layout_stats(fields):
    """Extents, contiguity and nesting derived only from source offsets/sizes."""
    ordered = sorted(fields, key=lambda item: int(item["offset"]))
    sizes = [int(item["size"]) for item in ordered]
    offsets = [int(item["offset"]) for item in ordered]
    extents = [offset + size for offset, size in zip(offsets, sizes)]

    contiguous = bool(ordered) and offsets[0] == 0
    previous_end = 0
    for offset, size in zip(offsets, sizes):
        if offset != previous_end:
            contiguous = False
        previous_end = offset + size

    nested_names = sum(
        1 for item in fields if "." in str(item.get("name", "")) or "[" in str(item.get("name", ""))
    )

    return {
        "min_field_size": min(sizes) if sizes else None,
        "max_field_size": max(sizes) if sizes else None,
        "max_field_extent": max(extents) if extents else None,
        "sorted_offsets_monotonic": offsets == sorted(offsets),
        "contiguous_from_zero": contiguous,
        "nested_name_fields": nested_names,
    }


def build_registry(research_root, documents=None, sources=None):
    """Compute the registry. Raises RegistryError only for unreadable inputs."""
    if documents is None:
        sources, documents = load_documents(research_root)
    elif sources is None:
        sources = hash_sources(research_root)

    errors = []

    index = documents["format-package.index.json"]
    layouts_doc = documents["layouts.json"]
    coverage_doc = documents["coverage.json"]
    manifest = documents["snapshot-manifest.json"]

    try:
        package_metadata = index["package_metadata"]
        containers_doc = package_metadata["containers"]
        catalogue_ids = package_metadata["catalogue_ids"]
        components_doc = package_metadata["components"]
        precedence = package_metadata["precedence"]
        corrections = package_metadata.get("semantic_corrections", [])
        effective_layouts = package_metadata.get("effective_catalogue_layouts", {})
    except KeyError as exc:
        raise RegistryError(
            "format-package.index.json lacks required key: %s" % exc
        )

    # ---- pinned source identity -------------------------------------------
    source_identity_ok = True
    for name in SOURCE_FILES:
        expected = EXPECTED_SOURCE_SHA256[name]
        actual = sources[name]["sha256"]
        if actual != expected:
            source_identity_ok = False
            errors.append(
                "source sha256 mismatch for %s: expected %s got %s"
                % (name, expected, actual)
            )

    index_component_sha = components_doc.get("catalogue_layouts", {}).get("sha256")
    index_layout_hash_ok = index_component_sha == sources["layouts.json"]["sha256"]
    if not index_layout_hash_ok:
        errors.append(
            "index component catalogue_layouts sha256 does not match layouts.json"
        )

    snapshot_files = {
        entry.get("file"): entry
        for entry in manifest.get("files", [])
        if isinstance(entry, dict)
    }
    snapshot_checks = {}
    for name in ("layouts.json", "coverage.json"):
        entry = snapshot_files.get(name)
        if entry is None:
            snapshot_checks[name] = {"entry_present": False}
            errors.append("snapshot-manifest.json has no files entry for %s" % name)
            continue
        sha_ok = entry.get("sha256") == sources[name]["sha256"]
        bytes_ok = entry.get("bytes") == sources[name]["bytes"]
        snapshot_checks[name] = {
            "entry_present": True,
            "sha256_match": sha_ok,
            "bytes_match": bytes_ok,
        }
        if not sha_ok or not bytes_ok:
            errors.append(
                "snapshot-manifest.json files entry for %s disagrees with the file"
                % name
            )
    snapshot_checks["format-package.index.json"] = {"entry_present": False}
    snapshot_checks["snapshot-manifest.json"] = {"entry_present": False}

    package_identity_ok = (
        index.get("original_package_sha256") == manifest.get("original_package_sha256")
        and index.get("original_package_bytes") == manifest.get("original_package_bytes")
    )
    if not package_identity_ok:
        errors.append(
            "index and snapshot-manifest disagree on original_package_sha256/bytes"
        )

    schema_versions = {
        "format-package.index.json": index.get("schema_version"),
        "snapshot-manifest.json": manifest.get("schema_version"),
    }
    if any(value != EXPECTED_SCHEMA_VERSION for value in schema_versions.values()):
        errors.append(
            "unexpected schema_version values: %s" % canonical_dumps(schema_versions)
        )

    # ---- catalogue ids and containers -------------------------------------
    if len(catalogue_ids) != 11:
        errors.append("catalogue_ids count is %d, expected 11" % len(catalogue_ids))
    if set(catalogue_ids) != set(EXPECTED_CATALOGUE_KEYS):
        errors.append("catalogue_ids keys differ from the eleven accepted keys")
    missing_catalogue_id = sorted(set(EXPECTED_LAYOUTS) - set(catalogue_ids))
    if missing_catalogue_id != ["Creatures_5537"]:
        errors.append(
            "unexpected layouts without catalogue id: %s"
            % ", ".join(missing_catalogue_id)
        )

    if len(containers_doc) != 6:
        errors.append("containers count is %d, expected 6" % len(containers_doc))
    if set(containers_doc) != set(EXPECTED_CONTAINERS):
        errors.append("container keys differ from the six expected container classes")

    # ---- layouts ----------------------------------------------------------
    layout_keys = list(layouts_doc.keys())
    if len(layout_keys) != 12:
        errors.append("layouts.json key count is %d, expected 12" % len(layout_keys))
    if set(layout_keys) != set(EXPECTED_LAYOUTS):
        errors.append("layouts.json keys differ from the twelve expected layout keys")
    if any(key.startswith("Level_") for key in layout_keys):
        errors.append("layouts.json unexpectedly contains a coverage-only Level variant")

    coverage_level_variants = sorted(
        key for key in coverage_doc.keys() if key.startswith("Level_")
    )
    if len(coverage_level_variants) != 2:
        errors.append(
            "coverage-only Level variant count is %d, expected 2"
            % len(coverage_level_variants)
        )

    layouts_out = []
    for key in EXPECTED_LAYOUTS:
        if key not in layouts_doc:
            errors.append("missing layout key in layouts.json: %s" % key)
            continue
        entry = layouts_doc[key]
        fields = entry.get("fields", [])
        record_size = entry.get("record_size")
        coverage_entry = coverage_doc.get(key, {})

        coverage_rows = coverage_entry.get("unresolved_baseline_rows", [])
        declared_bytes = coverage_entry.get("unresolved_baseline_bytes")
        computed_bytes = sum(int(row.get("size", 0)) for row in coverage_rows)

        field_rows = len(fields)
        coverage_field_rows = coverage_entry.get("field_rows")
        stats = field_layout_stats(fields)

        record_size_crosscheck = coverage_entry.get("record_size") == record_size
        field_rows_crosscheck = coverage_field_rows == field_rows
        bytes_crosscheck = declared_bytes == computed_bytes
        extent_crosscheck = stats["max_field_extent"] == record_size
        unresolved_names = [
            item.get("name") for item in fields if item.get("semantics") == "unresolved"
        ]
        coverage_names = [row.get("name") for row in coverage_rows]
        unresolved_names_crosscheck = unresolved_names == coverage_names

        if not record_size_crosscheck:
            errors.append(
                "record_size mismatch for %s: layouts=%s coverage=%s"
                % (key, record_size, coverage_entry.get("record_size"))
            )
        if not field_rows_crosscheck:
            errors.append(
                "field_rows mismatch for %s: len(fields)=%d coverage=%s"
                % (key, field_rows, coverage_field_rows)
            )
        if not bytes_crosscheck:
            errors.append(
                "unresolved byte mismatch for %s: declared=%s computed=%d"
                % (key, declared_bytes, computed_bytes)
            )
        if not extent_crosscheck:
            errors.append(
                "max field extent %s does not equal record_size %s for %s"
                % (stats["max_field_extent"], record_size, key)
            )
        if not stats["contiguous_from_zero"]:
            errors.append(
                "fields are not contiguous from offset 0 for %s" % key
            )
        if not unresolved_names_crosscheck:
            errors.append(
                "unresolved field names disagree with coverage rows for %s" % key
            )

        layouts_out.append(
            {
                "key": key,
                "catalogue_id": catalogue_ids.get(key),
                "record_size": record_size,
                "field_rows": field_rows,
                "coverage_field_rows": coverage_field_rows,
                "field_rows_crosscheck": field_rows_crosscheck,
                "record_size_crosscheck": record_size_crosscheck,
                "byte_span_covered": coverage_entry.get("byte_span_covered"),
                "unresolved_baseline_rows": len(coverage_rows),
                "unresolved_baseline_bytes_computed": computed_bytes,
                "unresolved_baseline_bytes_declared": declared_bytes,
                "unresolved_bytes_crosscheck": bytes_crosscheck,
                "unresolved_names_crosscheck": unresolved_names_crosscheck,
                "field_extents": stats,
                "extent_crosscheck": extent_crosscheck,
                "schema_source": "layouts.json#/%s/fields" % key,
                "coverage_source": "coverage.json#/%s" % key,
            }
        )

    # ---- Terrain 18 -------------------------------------------------------
    terrain_rows = coverage_doc.get("Terrain", {}).get("unresolved_baseline_rows", [])
    terrain_breakdown = {
        "Unk198": sum(1 for row in terrain_rows if row.get("name") == "Unk198"),
        "unk1ae_group": sum(
            1
            for row in terrain_rows
            if str(row.get("name", "")).startswith("unk1ae")
        ),
        "Unk224": sum(1 for row in terrain_rows if row.get("name") == "Unk224"),
    }
    terrain_computed = len(terrain_rows)
    terrain_ok = terrain_computed == TERRAIN_EXPECTED_UNRESOLVED_ROWS
    if not terrain_ok:
        errors.append(
            "Terrain unresolved row count is %d, expected %d"
            % (terrain_computed, TERRAIN_EXPECTED_UNRESOLVED_ROWS)
        )
    if sum(terrain_breakdown.values()) != terrain_computed:
        errors.append(
            "Terrain breakdown %s does not sum to %d"
            % (canonical_dumps(terrain_breakdown), terrain_computed)
        )

    terrain_layout = next(
        (item for item in layouts_out if item["key"] == "Terrain"), None
    )
    if terrain_layout is None:
        errors.append("Terrain layout missing from computed layouts")
    elif terrain_layout["unresolved_baseline_rows"] != terrain_computed:
        errors.append("Terrain registry row count disagrees with coverage.json")

    # ---- containers -------------------------------------------------------
    containers_out = {}
    for name in EXPECTED_CONTAINERS:
        if name not in containers_doc:
            errors.append("missing container: %s" % name)
            continue
        containers_out[name] = {
            "json_key": "format-package.index.json#/package_metadata/containers/%s"
            % name,
            "properties": containers_doc[name],
        }

    # ---- coverage-only Level variants ------------------------------------
    level_variants_out = []
    for key in coverage_level_variants:
        entry = coverage_doc[key]
        level_variants_out.append(
            {
                "key": key,
                "record_size": entry.get("record_size"),
                "field_rows": entry.get("field_rows"),
                "byte_span_covered": entry.get("byte_span_covered"),
                "unresolved_baseline_rows": len(
                    entry.get("unresolved_baseline_rows", [])
                ),
                "unresolved_baseline_bytes_declared": entry.get(
                    "unresolved_baseline_bytes"
                ),
                "layout_present": key in layouts_doc,
                "coverage_source": "coverage.json#/%s" % key,
            }
        )

    # ---- version linkage (Creatures / Creatures_5537) ---------------------
    variant_corrections = []
    for position, correction in enumerate(corrections):
        pointer = correction.get("json_pointer")
        if pointer in ("/Creatures", "/Creatures_5537"):
            variant_corrections.append(
                {
                    "index": position,
                    "json_pointer": pointer,
                    "replacement_component": correction.get("replacement_component"),
                    "replacement_member": correction.get("replacement_member"),
                    "effective_view": correction.get("effective_view"),
                    "scope": correction.get("scope"),
                }
            )
    if len(variant_corrections) != 2:
        errors.append(
            "expected two catalogue_layouts corrections for Creatures/Creatures_5537, "
            "found %d" % len(variant_corrections)
        )

    shared_views = sorted(
        {item["effective_view"] for item in variant_corrections if item["effective_view"]}
    )
    if shared_views != ["effective_catalogue_layouts"]:
        errors.append(
            "unexpected effective view for Creatures corrections: %s"
            % ", ".join(shared_views)
        )
    replacement_components = sorted(
        {
            item["replacement_component"]
            for item in variant_corrections
            if item["replacement_component"]
        }
    )
    if len(replacement_components) != 1:
        errors.append(
            "Creatures corrections do not share one replacement component: %s"
            % ", ".join(replacement_components)
        )
    for name in ("Creatures", "Creatures_5537"):
        if name not in effective_layouts:
            errors.append("effective_catalogue_layouts lacks %s" % name)

    version_linkage = {
        "keys": ["Creatures", "Creatures_5537"],
        "record_sizes": {
            name: layouts_doc.get(name, {}).get("record_size")
            for name in ("Creatures", "Creatures_5537")
        },
        "field_rows": {
            name: len(layouts_doc.get(name, {}).get("fields", []))
            for name in ("Creatures", "Creatures_5537")
        },
        "catalogue_ids": {
            "Creatures": catalogue_ids.get("Creatures"),
            "Creatures_5537": catalogue_ids.get("Creatures_5537"),
        },
        "effective_view": shared_views[0] if shared_views else None,
        "replacement_components": replacement_components,
        "corrections": variant_corrections,
        "json_key": "format-package.index.json#/package_metadata/semantic_corrections",
        "note": (
            "These files link both keys to one effective view and one correction "
            "component; they state no rule that selects record_size 5449 vs 5537 "
            "for a given input file."
        ),
    }

    # ---- components and precedence ---------------------------------------
    if not isinstance(components_doc, dict) or not components_doc:
        errors.append("package_metadata.components is absent or empty")
        components_doc = {}

    component_entries = {}
    component_shape_ok = True
    for component_key, component_value in components_doc.items():
        if not isinstance(component_value, dict):
            component_shape_ok = False
            errors.append("component %s is not an object" % component_key)
            continue
        file_name = component_value.get("file")
        file_sha = component_value.get("sha256")
        if not file_name or not file_sha:
            component_shape_ok = False
            errors.append("component %s lacks file/sha256" % component_key)
        component_entries[component_key] = {"file": file_name, "sha256": file_sha}

    if len(components_doc) != manifest.get("component_count"):
        errors.append(
            "component count %d disagrees with snapshot-manifest component_count %s"
            % (len(components_doc), manifest.get("component_count"))
        )

    unresolved_correction_components = sorted(
        {
            correction.get("replacement_component")
            for correction in corrections
            if correction.get("replacement_component")
            and correction.get("replacement_component") not in components_doc
        }
    )
    if unresolved_correction_components:
        errors.append(
            "corrections reference unknown components: %s"
            % ", ".join(unresolved_correction_components)
        )

    precedence_refs = []
    for position, statement in enumerate(precedence):
        if not isinstance(statement, str):
            errors.append("precedence entry %d is not a string" % position)
            continue
        mentioned = sorted(
            component_key
            for component_key in components_doc
            if component_key in statement
        )
        if mentioned:
            precedence_refs.append({"index": position, "components": mentioned})

    precedence_out = {
        "count": len(precedence),
        "json_key": "format-package.index.json#/package_metadata/precedence",
        "sha256": sha256_text(canonical_dumps(precedence)),
        "component_refs": precedence_refs,
        "statements_with_component_refs": len(precedence_refs),
    }

    # ---- assemble ---------------------------------------------------------
    linked = [item["key"] for item in layouts_out if item["catalogue_id"]]
    if len(linked) != 11:
        errors.append("linked catalogue ids count is %d, expected 11" % len(linked))

    registry = {
        "schema": "dk2-format-registry/2",
        "kind": "structural registry of historical baseline layouts",
        "not_claims": NOT_CLAIMS,
        "sources": {
            name: {
                "sha256": sources[name]["sha256"],
                "bytes": sources[name]["bytes"],
                "sha256_expected": EXPECTED_SOURCE_SHA256[name],
                "sha256_match": sources[name]["sha256"]
                == EXPECTED_SOURCE_SHA256[name],
                "role": "pinned input",
            }
            for name in SOURCE_FILES
        },
        "source_identity": {
            "all_match_expected": source_identity_ok,
            "expected_sha256_source": (
                ".bb/chats/thr_rhfc7scexe/notes/"
                "kilo-format-registry-acceptance.json#/input_sha256"
            ),
            "index_component_layouts_sha256_match": index_layout_hash_ok,
            "snapshot_entries": snapshot_checks,
            "original_package_identity_match": package_identity_ok,
            "original_package_sha256": index.get("original_package_sha256"),
            "original_package_bytes": index.get("original_package_bytes"),
            "schema_versions": schema_versions,
            "note": (
                "format-package.index.json and snapshot-manifest.json carry no "
                "snapshot entry for themselves; that is expected, not a gap."
            ),
        },
        "snapshot": {
            "latest_integrated_phase": manifest.get("latest_integrated_phase"),
            "component_count": manifest.get("component_count"),
            "documentation_count": manifest.get("documentation_count"),
            "python_source_count": manifest.get("python_source_count"),
            "export_date": manifest.get("export_date"),
            "source": "snapshot-manifest.json",
            "note": "Frozen snapshot; later supplements are not part of it.",
        },
        "package_metadata": {
            "package_version": package_metadata.get("package_version"),
            "byte_order": package_metadata.get("byte_order"),
            "packed_records": package_metadata.get("packed_records"),
            "purpose": package_metadata.get("purpose"),
            "json_key": "format-package.index.json#/package_metadata",
        },
        "encodings": {
            "byte_order": package_metadata.get("byte_order"),
            "packed_records": package_metadata.get("packed_records"),
            "per_field_encoding_table_audited": False,
            "json_key": "format-package.index.json#/package_metadata/byte_order",
            "note": "Only explicit package-wide byte order is summarized; per-field encoding was not audited here.",
        },
        "components": {
            "count": len(components_doc),
            "json_key": "format-package.index.json#/package_metadata/components",
            "all_entries_have_file_and_sha256": component_shape_ok,
            "entries": component_entries,
        },
        "precedence": precedence_out,
        "catalogue_ids": catalogue_ids,
        "catalogue_ids_key": "format-package.index.json#/package_metadata/catalogue_ids",
        "containers": containers_out,
        "layouts": layouts_out,
        "coverage_only_level_variants": level_variants_out,
        "version_linkage": version_linkage,
        "coverage_scope": {
            "numeric_percentage": None,
            "row_and_byte_counts_only": True,
            "json_keys": ["coverage.json#/<layout>/unresolved_baseline_rows"],
        },
        "opaque_field_policy": {
            "rule": (
                "Preserve source field labels verbatim; unresolved rows stay "
                "opaque and are never renamed."
            ),
            "unresolved_rows_are": (
                "historical baseline only, not a claim of unknown format elsewhere"
            ),
            "new_field_names_introduced": 0,
        },
        "registry_gaps": REGISTRY_GAPS,
        "validation": {
            "layouts_expected": 12,
            "layouts_found": len(layout_keys),
            "catalogue_ids_expected": 11,
            "catalogue_ids_found": len(catalogue_ids),
            "containers_expected": 6,
            "containers_found": len(containers_doc),
            "coverage_only_level_variants_expected": 2,
            "coverage_only_level_variants_found": len(coverage_level_variants),
            "linked_catalogue_ids": len(linked),
            "components_found": len(components_doc),
            "precedence_statements": len(precedence),
            "precedence_statements_with_component_refs": len(precedence_refs),
            "correction_components_resolved": len(corrections)
            - len(unresolved_correction_components),
            "corrections_total": len(corrections),
            "terrain_unresolved_rows_computed": terrain_computed,
            "terrain_unresolved_rows_expected": TERRAIN_EXPECTED_UNRESOLVED_ROWS,
            "terrain_breakdown": terrain_breakdown,
            "terrain_ok": terrain_ok,
            "errors": errors,
            "ok": not errors,
        },
    }

    registry["output_digest"] = {
        "algorithm": "sha256",
        "value": sha256_text(canonical_dumps(registry)),
        "covers": "normalized registry excluding output_digest",
        "self_hash": False,
    }
    return registry


def render_russian_markdown(registry):
    lines = []
    lines.append("# Реестр форматов DK2 (структурный, исторический baseline)")
    lines.append("")
    lines.append(
        "Детерминированный структурный реестр по четырём закреплённым JSON. "
        "Не является текущей семантической полнотой и не содержит процента покрытия."
    )
    lines.append("")
    lines.append("## Источники (пиннинг по SHA-256)")
    lines.append("")
    lines.append("| Файл | SHA-256 (факт) | Совпадает с ожидаемым | Байт |")
    lines.append("| --- | --- | --- | --- |")
    for name, meta in registry["sources"].items():
        lines.append(
            "| %s | %s | %s | %d |"
            % (name, meta["sha256"], "да" if meta["sha256_match"] else "НЕТ", meta["bytes"])
        )
    lines.append("")
    lines.append("## Снимок")
    lines.append("")
    lines.append(
        "- Фаза: %s; компонентов машины: %s; документации: %s; python-источников: %s; дата: %s"
        % (
            registry["snapshot"]["latest_integrated_phase"],
            registry["snapshot"]["component_count"],
            registry["snapshot"]["documentation_count"],
            registry["snapshot"]["python_source_count"],
            registry["snapshot"]["export_date"],
        )
    )
    lines.append(
        "- Оригинальный пакет: %s байт, sha256 %s"
        % (
            registry["source_identity"]["original_package_bytes"],
            registry["source_identity"]["original_package_sha256"],
        )
    )
    lines.append("")
    lines.append("## Макеты (12 ключей)")
    lines.append("")
    lines.append(
        "| Ключ | ID каталога | record_size | field_rows | сверка строк | "
        "неизв. строк | неизв. байт (факт/объявл.) | min/max поле | макс. extent = record_size | "
        "непрерывно с 0 | вложенные имена |"
    )
    lines.append(
        "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"
    )
    for item in registry["layouts"]:
        extents = item["field_extents"]
        lines.append(
            "| %s | %s | %s | %s | %s | %s | %s/%s | %s/%s | %s | %s | %s |"
            % (
                item["key"],
                item["catalogue_id"] if item["catalogue_id"] else "—",
                item["record_size"],
                item["field_rows"],
                "да" if item["field_rows_crosscheck"] else "нет",
                item["unresolved_baseline_rows"],
                item["unresolved_baseline_bytes_computed"],
                item["unresolved_baseline_bytes_declared"],
                extents["min_field_size"],
                extents["max_field_size"],
                "да" if item["extent_crosscheck"] else "нет",
                "да" if extents["contiguous_from_zero"] else "нет",
                extents["nested_name_fields"],
            )
        )
    lines.append("")
    lines.append(
        "Terrain: неизвестных строк = %d из %d ожидаемых (%s)."
        % (
            registry["validation"]["terrain_unresolved_rows_computed"],
            registry["validation"]["terrain_unresolved_rows_expected"],
            ", ".join(
                "%s=%d" % (key, value)
                for key, value in registry["validation"]["terrain_breakdown"].items()
            ),
        )
    )
    lines.append("")
    lines.append("## Варианты Level только в coverage (2)")
    lines.append("")
    lines.append(
        "| Ключ | record_size | field_rows | неизв. строк | неизв. байт | макет в layouts.json |"
    )
    lines.append("| --- | --- | --- | --- | --- | --- |")
    for item in registry["coverage_only_level_variants"]:
        lines.append(
            "| %s | %s | %s | %s | %s | %s |"
            % (
                item["key"],
                item["record_size"],
                item["field_rows"],
                item["unresolved_baseline_rows"],
                item["unresolved_baseline_bytes_declared"],
                "да" if item["layout_present"] else "нет",
            )
        )
    lines.append("")
    lines.append("## Связка версий Creatures / Creatures_5537")
    lines.append("")
    linkage = registry["version_linkage"]
    lines.append(
        "- record_size: Creatures=%s, Creatures_5537=%s; field_rows: %s / %s"
        % (
            linkage["record_sizes"]["Creatures"],
            linkage["record_sizes"]["Creatures_5537"],
            linkage["field_rows"]["Creatures"],
            linkage["field_rows"]["Creatures_5537"],
        )
    )
    lines.append(
        "- catalogue_ids: Creatures=%s, Creatures_5537=%s"
        % (
            linkage["catalogue_ids"]["Creatures"] or "—",
            linkage["catalogue_ids"]["Creatures_5537"] or "—",
        )
    )
    lines.append(
        "- Общий effective view: %s; компонент правок: %s"
        % (
            linkage["effective_view"],
            ", ".join(linkage["replacement_components"]),
        )
    )
    for correction in linkage["corrections"]:
        lines.append(
            "- semantic_corrections[%d]: %s -> %s / %s"
            % (
                correction["index"],
                correction["json_pointer"],
                correction["replacement_component"],
                correction["replacement_member"],
            )
        )
    lines.append("- %s" % linkage["note"])
    lines.append(
        "- По-русски: оба ключа привязаны к одному effective view и одному компоненту "
        "правок; правила выбора record_size 5449 или 5537 для конкретного файла "
        "в этих четырёх файлах нет."
    )
    lines.append("")
    lines.append("## Контейнеры (6 классов)")
    lines.append("")
    lines.append("| Класс | Ключ JSON | Свойства |")
    lines.append("| --- | --- | --- |")
    for name, entry in registry["containers"].items():
        props = ", ".join(entry["properties"].keys())
        lines.append("| %s | %s | %s |" % (name, entry["json_key"], props))
    lines.append("")
    lines.append("## Компоненты и precedence")
    lines.append("")
    lines.append(
        "- Компонентов: %d (все с file+sha256: %s); ключ: %s"
        % (
            registry["components"]["count"],
            "да" if registry["components"]["all_entries_have_file_and_sha256"] else "нет",
            registry["components"]["json_key"],
        )
    )
    lines.append(
        "- Precedence: %d строк, sha256 %s; строк со ссылками на компоненты: %d"
        % (
            registry["precedence"]["count"],
            registry["precedence"]["sha256"],
            registry["precedence"]["statements_with_component_refs"],
        )
    )
    lines.append(
        "- Кодировки: byte_order=%s, packed_records=%s; кодировки отдельных полей здесь не проверялись."
        % (
            registry["encodings"]["byte_order"],
            registry["encodings"]["packed_records"],
        )
    )
    lines.append("")
    lines.append("## Политика непрозрачных полей")
    lines.append("")
    lines.append("- %s" % registry["opaque_field_policy"]["rule"])
    lines.append(
        "- Новых имён полей не введено: %d."
        % registry["opaque_field_policy"]["new_field_names_introduced"]
    )
    lines.append(
        "- Числовой процент покрытия: отсутствует (только строки/байты unresolved)."
    )
    lines.append("")
    lines.append("## Пробелы реестра (отсутствующие литеральные ключи)")
    lines.append("")
    for gap in registry["registry_gaps"]:
        lines.append("- %s" % gap)
    lines.append("")
    lines.append("## Границы утверждения")
    lines.append("")
    for claim in registry["not_claims"]:
        lines.append("- %s" % claim)
    lines.append("")
    lines.append("## Проверка")
    lines.append("")
    validation = registry["validation"]
    lines.append(
        "- Макеты: %d/%d" % (validation["layouts_found"], validation["layouts_expected"])
    )
    lines.append(
        "- ID каталога: %d/%d; связанных: %d"
        % (
            validation["catalogue_ids_found"],
            validation["catalogue_ids_expected"],
            validation["linked_catalogue_ids"],
        )
    )
    lines.append(
        "- Контейнеры: %d/%d"
        % (validation["containers_found"], validation["containers_expected"])
    )
    lines.append(
        "- Level-варианты: %d/%d"
        % (
            validation["coverage_only_level_variants_found"],
            validation["coverage_only_level_variants_expected"],
        )
    )
    lines.append(
        "- Компоненты: %d; precedence: %d; правки с разрешёнными компонентами: %d/%d"
        % (
            validation["components_found"],
            validation["precedence_statements"],
            validation["correction_components_resolved"],
            validation["corrections_total"],
        )
    )
    lines.append(
        "- Ошибок: %d; статус: %s"
        % (len(validation["errors"]), "ok" if validation["ok"] else "fail")
    )
    for error in validation["errors"]:
        lines.append("  - ОШИБКА: %s" % error)
    lines.append("")
    lines.append("- digest: %s" % registry["output_digest"]["value"])
    lines.append("")
    return "\n".join(lines)


def write_outputs(registry, output_dir):
    """Write exactly two fixed artifacts; returns their paths."""
    if not registry["validation"]["ok"]:
        raise RegistryError("validation failed; refusing to publish outputs")
    json_path = os.path.join(output_dir, OUTPUT_JSON)
    md_path = os.path.join(output_dir, OUTPUT_MD)
    with open(json_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(
            json.dumps(registry, ensure_ascii=False, indent=2, sort_keys=True) + "\n"
        )
    with open(md_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(render_russian_markdown(registry))
    return [json_path, md_path]


def run_self_test(research_root):
    """In-memory negative controls over the already-loaded documents."""
    sources, documents = load_documents(research_root)

    baseline = build_registry(research_root, documents=documents, sources=sources)
    baseline_again = build_registry(research_root, documents=documents, sources=sources)
    checks = []

    checks.append(
        (
            "baseline_ok",
            baseline["validation"]["ok"],
            "baseline validation passes",
        )
    )
    checks.append(
        (
            "digest_deterministic",
            baseline["output_digest"]["value"]
            == baseline_again["output_digest"]["value"],
            "two builds produce one digest",
        )
    )

    def mutated(mutator):
        import copy

        clone = copy.deepcopy(documents)
        mutator(clone)
        return build_registry(research_root, documents=clone, sources=sources)

    def corrupt_record_size(clone):
        clone["coverage.json"]["Terrain"]["record_size"] += 1

    def corrupt_unresolved_bytes(clone):
        clone["coverage.json"]["Terrain"]["unresolved_baseline_bytes"] += 1

    def drop_terrain_row(clone):
        clone["coverage.json"]["Terrain"]["unresolved_baseline_rows"].pop()

    def drop_catalogue_id(clone):
        clone["format-package.index.json"]["package_metadata"]["catalogue_ids"].pop(
            "Shots"
        )

    controls = [
        ("record_size_mismatch_detected", corrupt_record_size),
        ("unresolved_bytes_mismatch_detected", corrupt_unresolved_bytes),
        ("terrain_18_detected", drop_terrain_row),
        ("catalogue_ids_11_detected", drop_catalogue_id),
    ]
    for name, mutator in controls:
        result = mutated(mutator)
        checks.append(
            (
                name,
                not result["validation"]["ok"] and bool(result["validation"]["errors"]),
                "; ".join(result["validation"]["errors"][:2]) or "no error raised",
            )
        )

    failed = [name for name, passed, _ in checks if not passed]
    print("dk2-format-registry self-test (in-memory negative controls)")
    for name, passed, detail in checks:
        print("  %-38s %s  %s" % (name, "pass" if passed else "FAIL", detail))
    print(
        "  baseline digest=%s" % baseline["output_digest"]["value"]
    )
    return 0 if not failed else 1


def main(argv=None):
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_root = os.path.join(
        os.path.dirname(os.path.dirname(script_dir)),
        "UltimateEditor-format-export",
        "research",
        "dk2-formats",
    )

    parser = argparse.ArgumentParser(
        description=(
            "Build the DK2 structural format registry. Read-only unless --write."
        )
    )
    parser.add_argument(
        "--research-root",
        default=default_root,
        help="Directory holding the four pinned JSON inputs.",
    )
    parser.add_argument(
        "--write",
        action="store_true",
        help=(
            "Write %s and %s next to this script. Refused if validation fails."
            % (OUTPUT_JSON, OUTPUT_MD)
        ),
    )
    parser.add_argument(
        "--no-write",
        action="store_true",
        help="Explicit read-only mode (already the default).",
    )
    parser.add_argument(
        "--self-test",
        action="store_true",
        help="Run bounded in-memory negative controls and exit.",
    )
    args = parser.parse_args(argv)
    if args.write and args.no_write:
        parser.error("--write and --no-write are mutually exclusive")

    try:
        if args.self_test:
            return run_self_test(os.path.abspath(args.research_root))

        registry = build_registry(os.path.abspath(args.research_root))
        validation = registry["validation"]

        print("dk2-format-registry: research-root %s" % os.path.abspath(args.research_root))
        for name, meta in registry["sources"].items():
            print(
                "source %s sha256=%s bytes=%d expected=%s"
                % (
                    name,
                    meta["sha256"],
                    meta["bytes"],
                    "match" if meta["sha256_match"] else "MISMATCH",
                )
            )
        print(
            "layouts=%d catalogue_ids=%d containers=%d level_variants=%d linked_ids=%d"
            % (
                validation["layouts_found"],
                validation["catalogue_ids_found"],
                validation["containers_found"],
                validation["coverage_only_level_variants_found"],
                validation["linked_catalogue_ids"],
            )
        )
        print(
            "components=%d precedence=%d precedence_refs=%d corrections=%d"
            % (
                validation["components_found"],
                validation["precedence_statements"],
                validation["precedence_statements_with_component_refs"],
                validation["corrections_total"],
            )
        )
        print(
            "terrain_unresolved_rows=%d expected=%d breakdown=%s ok=%s"
            % (
                validation["terrain_unresolved_rows_computed"],
                validation["terrain_unresolved_rows_expected"],
                canonical_dumps(validation["terrain_breakdown"]),
                validation["terrain_ok"],
            )
        )
        print("output_digest=%s" % registry["output_digest"]["value"])

        if args.write:
            written = write_outputs(registry, script_dir)
            print("written: %s" % ", ".join(written))
        else:
            print("read-only mode (no files written); pass --write to publish")

        for error in validation["errors"]:
            print("error: %s" % error)
        return 0 if validation["ok"] else 1
    except RegistryError as exc:
        print("error: %s" % exc, file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
