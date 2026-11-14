#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Bounded (correction pass 2) read audit for GlobalVariables scalars 113/114.

Targets (WORD, runtime, installed DX build):
  113 -> 0x006F28F2  (base 0x006F2550 + 0x3A2 = 930)
  114 -> 0x006F28F4  (base 0x006F2550 + 0x3A4 = 932)

This pass replaces the previous negative conclusion and its unsound proof:

  * instruction size / read-write classification is done with the installed
    Capstone 5.0.7 engine on the pinned EXE, never with a hand opcode table;
  * alias arithmetic uses the saved structured `expression`
    [base_symbol, disp, [[index_symbol, scale, 0, mask], ...]] plus the saved
    operand width `bytes` and exact half-open interval overlap, and reports
    candidate counts WITHOUT any gameplay-reachability or absence claim;
  * the table-base store at 0x51CDB0 is described by its verified bytes only;
    the earlier "Type112" label is withdrawn (phase101 puts Type112 at 0x51D13F
    -> 0x6F28F0), and base identity is treated as conditional;
  * the loader stores and the 0x47BB4E DWORD are labelled historical
    reproduction, not new semantics;
  * the actual investigation is limited to the two saved overlap-candidate
    functions 0x457880 (loads 0x4578DA / 0x457988) and 0x458F60 (load 0x459066):
    primary bytes are validated on Capstone instruction boundaries, the index
    producer and its supported bounds are derived, and the loaded value is
    followed downstream.

Reads: pinned EXE, this folder's inputs, phase200 alias JSON, the two saved
phase200 function excerpts (only as a locator aid; bytes come from the EXE),
and GlobalVariables.kwd (read-only) for the type-192 bound value.
No installs, no child processes, no game/old-research writes.

Exit code 2 on any inconsistency (hash mismatch, bad instruction boundary,
unexpected operand, malformed input). `audit(write=False)` / --no-write
computes everything and writes nothing.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import struct
import sys

try:
    import capstone
    from capstone import x86_const as xc
except Exception as exc:  # pragma: no cover - environment check
    sys.stderr.write("Capstone 5 is required for this audit: %r\n" % (exc,))
    raise SystemExit(2)

EXE_SHA256 = "d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a"
EXE_DEFAULT = r"D:\Games\DK2\Dungeon Keeper 2\DKII-DX.exe"
KWD_DEFAULT = r"D:\Games\DK2\Dungeon Keeper 2\Data\editor\GlobalVariables.kwd"
BASE = 0x006F2550
T113 = 0x006F28F2
T114 = 0x006F28F4
OFF113 = T113 - BASE  # 930
OFF114 = T114 - BASE  # 932
TARGETS = [(113, OFF113, OFF113 + 1), (114, OFF114, OFF114 + 1)]

CTX = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
CTX.detail = True


# --------------------------------------------------------------------------- #
# PE / file helpers (unchanged mechanics, no decoding)
# --------------------------------------------------------------------------- #
def sha256_file(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def parse_pe(blob: bytes):
    if blob[:2] != b"MZ":
        raise ValueError("inconsistency: not a PE file")
    e_lfanew = struct.unpack_from("<I", blob, 0x3C)[0]
    if blob[e_lfanew:e_lfanew + 4] != b"PE\0\0":
        raise ValueError("inconsistency: missing PE signature")
    coff = e_lfanew + 4
    nsec, = struct.unpack_from("<H", blob, coff + 2)
    opt_size, = struct.unpack_from("<H", blob, coff + 16)
    opt = coff + 20
    image_base, = struct.unpack_from("<I", blob, opt + 28)
    sec = opt + opt_size
    sections = []
    for i in range(nsec):
        off = sec + i * 40
        name = blob[off:off + 8].rstrip(b"\0").decode("latin-1")
        vsize, vaddr, rsize, rptr = struct.unpack_from("<IIII", blob, off + 8)
        sections.append({"name": name, "va": image_base + vaddr, "vsize": vsize,
                         "raw": rptr, "raw_size": rsize})
    return {"image_base": image_base, "sections": sections}


def va_to_off(pe, va):
    for s in pe["sections"]:
        span = max(s["vsize"], s["raw_size"])
        if s["va"] <= va < s["va"] + span:
            return s["raw"] + (va - s["va"]), s["name"]
    return None, None


def off_to_va(pe, off):
    for s in pe["sections"]:
        if s["raw"] <= off < s["raw"] + s["raw_size"]:
            return s["va"] + (off - s["raw"]), s["name"]
    return None, None


def find_pattern(blob, pat, start=0, end=None):
    end = len(blob) if end is None else end
    out = []
    i = blob.find(pat, start, end)
    while i != -1:
        out.append(i)
        i = blob.find(pat, i + 1, end)
    return out


# --------------------------------------------------------------------------- #
# Capstone helpers.  These decode, they do not guess operand sizes.
# --------------------------------------------------------------------------- #
def _is_read(op) -> bool:
    return bool(op.access & capstone.CS_AC_READ)


def _is_write(op) -> bool:
    return bool(op.access & capstone.CS_AC_WRITE)


def insn_record(insn):
    return {
        "pc": insn.address,
        "bytes": bytes(insn.bytes).hex(),
        "size": insn.size,
        "mnemonic": insn.mnemonic,
        "op_str": insn.op_str,
        "mem_ops": [
            {"base": insn.reg_name(o.mem.base) if o.mem.base else None,
             "index": insn.reg_name(o.mem.index) if o.mem.index else None,
             "scale": o.mem.scale, "disp": o.mem.disp,
             "size": o.size, "read": _is_read(o), "write": _is_write(o)}
            for o in insn.operands if o.type == xc.X86_OP_MEM
        ],
    }


def decode_linear(blob, pe, start_va, stop_va):
    """Decode every instruction boundary from start_va through stop_va."""
    off, _ = va_to_off(pe, start_va)
    if off is None:
        raise ValueError("start VA not mapped: 0x%08x" % start_va)
    end_off, _ = va_to_off(pe, stop_va)
    if end_off is None:
        raise ValueError("stop VA not mapped: 0x%08x" % stop_va)
    raw = blob[off:end_off + 16]
    out = []
    for insn in CTX.disasm(raw, start_va):
        if insn.address > stop_va:
            break
        out.append(insn)
    return out


def decode_local_boundary(blob, pe, target_va, back=16):
    """Find a decode start in [target-back, target-1] that puts target on a
    Capstone instruction boundary.  Returns (insn, attempts)."""
    attempts = []
    for start in range(target_va - back, target_va):
        off, _ = va_to_off(pe, start)
        if off is None:
            continue
        raw = blob[off:off + 48]
        seen = []
        for insn in CTX.disasm(raw, start):
            seen.append(insn.address)
            if insn.address == target_va:
                return insn, attempts + [hex(start)]
            if insn.address > target_va:
                break
        attempts.append({"start": hex(start), "boundaries": [hex(a) for a in seen]})
    return None, attempts


def decode_containing(blob, pe, va, back=12):
    """Return the instruction that contains address `va` (e.g. a disp32 field)
    when that instruction references the literal table base 0x6f2550."""
    for start in range(va - back, va):
        off, _ = va_to_off(pe, start)
        if off is None:
            continue
        raw = blob[off:off + 48]
        for insn in CTX.disasm(raw, start):
            if insn.address > va:
                break
            if insn.address <= va < insn.address + insn.size:
                refs = False
                for o in insn.operands:
                    if o.type == xc.X86_OP_MEM and o.mem.disp == BASE:
                        refs = True
                    elif o.type == xc.X86_OP_IMM and (o.imm & 0xFFFFFFFF) == BASE:
                        refs = True
                if refs:
                    return insn, hex(start)
    return None, None


def expect_insn(insn, contains, failures, label):
    if insn is None:
        failures.append("instruction boundary not found: %s" % label)
        return False
    if contains not in insn.op_str:
        failures.append("operand mismatch at %s: got %r, expected %r" %
                        (label, insn.op_str, contains))
        return False
    return True


def require_pcs(decoded, required, failures, label):
    by_pc = {i.address: i for i in decoded}
    recs = []
    for item in required:
        pc, contains = item[0], item[1]
        want_mnem = item[2] if len(item) > 2 else None
        insn = by_pc.get(pc)
        ok = expect_insn(insn, contains, failures, "%s @0x%x" % (label, pc))
        if ok and want_mnem and insn.mnemonic != want_mnem:
            failures.append("mnemonic mismatch at %s @0x%x: got %r, expected %r" %
                            (label, pc, insn.mnemonic, want_mnem))
            ok = False
        if ok:
            recs.append(insn_record(insn))
    return recs


# --------------------------------------------------------------------------- #
# Inputs
# --------------------------------------------------------------------------- #
def read_kwd_type192(path):
    """Return the type-192 (0xC0) record from GlobalVariables.kwd Section 0xE6.

    This uses dk2_formats.parse on bytes; the record layout u32[0]=type,
    u32[1]=Value for the generic variable layout (verified by dk2_formats).
    No value is inferred when the section/record is absent.
    """
    sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    import dk2_formats  # local research reader, read-only
    data = open(path, "rb").read()
    sections = dk2_formats.parse(data)
    variables = [s for s in sections if s.signature == 0xE6]
    if len(variables) != 1:
        return None, data, "expected exactly one Variables (0xE6) section"
    rec = None
    for b in variables[0].records:
        if len(b) >= 8 and struct.unpack_from("<I", b, 0)[0] == 192:
            rec = b
            break
    if rec is None:
        return None, data, "type 192 record missing"
    value = struct.unpack_from("<I", rec, 4)[0]
    return {"type": 192, "value": value, "raw_hex": rec.hex(),
            "runtime_address": "0x006f2976", "runtime_bytes": 1,
            "source": "GlobalVariables.kwd Section 0xE6, generic layout (Type, Value, Extra1, Extra2)"}, data, None


# --------------------------------------------------------------------------- #
# Audit
# --------------------------------------------------------------------------- #
def audit(write: bool = True, exe_path: str = EXE_DEFAULT,
          phase200_path: str | None = None, kwd_path: str = KWD_DEFAULT,
          out_dir: str | None = None) -> int:
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(here)
    out_dir = out_dir or here
    phase200_path = phase200_path or os.path.join(root, "phase200-global-alias-search.json")
    failures = []

    blob = open(exe_path, "rb").read()
    digest = sha256_file(exe_path)
    if digest != EXE_SHA256:
        failures.append("EXE sha256 mismatch: %s" % digest)
    try:
        pe = parse_pe(blob)
    except ValueError as exc:
        failures.append(str(exc))
        if os.environ.get("DK2_AUDIT_DEBUG"):
            sys.stderr.write("FAILURES:\n" + "\n".join(" - " + f for f in failures) + "\n")
        return 2

    # ------------------------- inputs ------------------------------------ #
    if not os.path.exists(phase200_path):
        failures.append("phase200 alias search not found")
        p2 = {}
    else:
        p2 = json.load(open(phase200_path, "r", encoding="utf-8"))
    if not os.path.exists(kwd_path):
        failures.append("GlobalVariables.kwd not found")
        kwd = None
        kwd_bytes = b""
        kwd_err = "missing"
    else:
        kwd, kwd_bytes, kwd_err = read_kwd_type192(kwd_path)
        if kwd is None:
            failures.append("type192 bound unavailable: %s" % kwd_err)

    # ------------------- historical reproduction ------------------------ #
    loader_specs = {"113": (0x51D14D, "0x6f28f2"), "114": (0x51D15B, "0x6f28f4")}
    loader_stores = []
    for name, (va, addr_text) in loader_specs.items():
        insn, attempts = decode_local_boundary(blob, pe, va)
        ok = expect_insn(insn, addr_text, failures, "loader store %s" % name)
        rec = insn_record(insn) if insn else None
        if ok and not (insn.size == 6 and any(m["write"] and m["size"] == 2 for m in rec["mem_ops"])):
            failures.append("loader store %s is not a verified 2-byte write" % name)
        loader_stores.append({"global": name, "va": hex(va), "decode_start": attempts[-1] if attempts else None,
                              "instruction": rec, "verified": ok})

    insn, attempts = decode_local_boundary(blob, pe, 0x47BB4E)
    overlap_ok = expect_insn(insn, "0x6f28f0", failures, "known overlapping DWORD load")
    overlap = {"va": "0x47bb4e", "decode_start": attempts[-1] if attempts else None,
               "instruction": insn_record(insn) if insn else None, "verified": overlap_ok}

    # ------------------- structured alias candidate census --------------- #
    reads = p2.get("alias_reads", [])
    triples = []
    pair_keys = set()
    load_pcs = set()
    used_reads = 0
    for r in reads:
        expr = r.get("expression") or []
        if not expr:
            failures.append("alias read without structured expression: access 0x%x" % r.get("access", 0))
            continue
        width = int(r.get("bytes") or 0)
        if width <= 0:
            failures.append("alias read without operand width: access 0x%x" % r.get("access", 0))
            continue
        used_reads += 1
        terms = expr[2] or []
        starts = []
        if terms:
            for term in terms:
                scale = int(term[1]); mask = int(term[3])
                # Enumerate the encoding domain only; this is NOT a supported
                # runtime domain and is never used as a reachability proof.
                for idx in range(0, min(mask, 0xFFFF) + 1):
                    starts.append((idx, expr[1] + scale * idx))
        else:
            starts.append((None, expr[1]))
        for target, lo, hi in TARGETS:
            for idx, start in starts:
                end = start + width - 1
                if start <= hi and end >= lo:
                    key = (r["access"], target)
                    pair_keys.add(key)
                    load_pcs.add(r["access"])
                    triples.append({
                        "access": hex(r["access"]), "function": hex(r["function"]),
                        "target": target, "index": idx,
                        "start_offset": start, "byte_interval": [start, end],
                        "operand_width": width, "instruction": r["instruction"],
                        "expression": expr,
                    })
    census = {
        "source": os.path.basename(phase200_path),
        "source_sha256": sha256_file(phase200_path) if os.path.exists(phase200_path) else None,
        "method": ("structured expression [1, disp, [[symbol, scale, 0, mask]]] + operand width "
                   "'bytes'; candidate iff [disp+scale*idx, +width-1] intersects a target byte"),
        "counts": {"alias_reads_total": len(reads), "reads_with_structured_expression": used_reads,
                   "load_pcs": len(load_pcs), "target_load_pairs": len(pair_keys),
                   "load_target_index_triples": len(triples)},
        "pairs": sorted(({"access": hex(a), "target": t} for a, t in pair_keys),
                        key=lambda d: (d["access"], d["target"])),
        "index_constraints": {
            "mask_domain": "0..mask (encoding bound only)",
            "note": ("A 0..255 mask does NOT prove that a normal gameplay index reaches that "
                     "value. Every triple below is arithmetic only and is not a reachability "
                     "or non-use proof."),
        },
        "candidates": triples,
        "scope": ("Candidate arithmetic over the saved phase200 alias reads. No absence claim: "
                  "unsupported expressions stay unresolved and indirect/pointer-copied reads "
                  "outside the saved set are not covered."),
    }

    # ------------------- literal base census (bytes only) ---------------- #
    base_hits = []
    for pat_off in find_pattern(blob, struct.pack("<I", BASE)):
        va, sec = off_to_va(pe, pat_off)
        if va is None:
            continue
        insn, start = decode_containing(blob, pe, va)
        rec = None
        role = "unclassified"
        if insn is not None:
            rec = insn_record(insn)
            decoded_from = start
            text = (insn.mnemonic + " " + insn.op_str).lower()
            if "0x6f2550" in text and "0x6e4154" in text:
                role = "store of literal table base into alias pointer global 0x6e4154 (value identity only)"
            elif insn.mnemonic == "mov" and rec["mem_ops"]:
                if any(m["write"] and m["disp"] == BASE and m["size"] == 4 for m in rec["mem_ops"]):
                    role = ("verified 4-byte store to table base offset 0 (dword 0); stored value is "
                            "the runtime accumulator and is NOT traced; previous 'Type112' label withdrawn")
                elif any(m["read"] and m["disp"] == BASE for m in rec["mem_ops"]):
                    role = "absolute DWORD read at table base offset 0"
                else:
                    role = "instruction contains literal base 0x6f2550"
            elif insn.mnemonic == "mov" and insn.op_str.startswith("edi,") and "0x6f2550" in insn.op_str:
                role = "literal table base loaded into EDI for bulk zero-fill (write only)"
            else:
                role = "literal base operand"
        else:
            decoded_from = None
            role = "no validated instruction containing this value references the table base"
        base_hits.append({"va": hex(va), "file_offset": hex(pat_off), "section": sec,
                          "decode_start": decoded_from,
                          "instruction": rec, "role": role})

    # ------------------- index producer: creature level byte ------------- #
    level_writers = []
    writer_specs = [
        (0x4875C9, "0x1", "creature instance init: level = 1"),
        (0x48B1E6, "al", "level-up: AL = byte[+0x403]; INC AL; byte[+0x403] = AL"),
        (0x48B06A, "al", "level-down: AL = byte[+0x403]; DEC AL; byte[+0x403] = AL"),
    ]
    for va, want, effect in writer_specs:
        insn, attempts = decode_local_boundary(blob, pe, va)
        ok = expect_insn(insn, "byte ptr [esi + 0x403]", failures, "level writer @0x%x" % va)
        rec = insn_record(insn) if insn else None
        if ok and not any(m["write"] and m["size"] == 1 and m["disp"] == 0x403 for m in rec["mem_ops"]):
            failures.append("level writer @0x%x is not a verified 1-byte write to +0x403" % va)
        level_writers.append({"va": hex(va), "decode_start": attempts[-1] if attempts else None,
                              "instruction": rec, "effect": effect, "verified": ok})

    level_up = decode_linear(blob, pe, 0x48B120, 0x48B1F0)
    level_up_check = require_pcs(level_up, [
        (0x48B13C, "0xa"), (0x48B1DC, "[esi + 0x403]"), (0x48B1E2, "al", "inc"),
        (0x48B1E4, "al"), (0x48B1E6, "[esi + 0x403]")], failures, "level-up")
    level_down = decode_linear(blob, pe, 0x48AFA0, 0x48B075)
    level_down_check = require_pcs(level_down, [
        (0x48AFBC, "byte ptr [esi + 0x403], 1"), (0x48B05C, "[esi + 0x403]"),
        (0x48B068, "al", "dec"), (0x48B06A, "[esi + 0x403]")], failures, "level-down")

    level_domain = {
        "field": "creature instance byte +0x403 ('Experience level' per the older creature-record "
                 "notes; the same object type is addressed in both candidate functions)",
        "located_writers": level_writers,
        "level_up_cap": {
            "instructions": level_up_check,
            "derivation": ("entry check CMP byte[+0x403],0x0a: if level == 10 the function returns "
                           "without a write; otherwise it writes AL = level+1, so a write can only "
                           "produce 2..10 from a 1..9 predecessor"),
        },
        "level_down_floor": {
            "instructions": level_down_check,
            "derivation": ("entry check CMP byte[+0x403],0x1: if level == 1 the function returns "
                           "without a write; otherwise it writes AL = level-1, so a write can only "
                           "produce 0..9 from a 1..10 predecessor; combined with init=1 the "
                           "reachable floor is 1"),
        },
        "inductive_supported_domain": [1, 10],
        "caveat": ("The domain 1..10 is an inductive invariant over the three writers located by "
                   "the saved decompile corpus (init=1, +1 capped at 10, -1 floored at 1). It is "
                   "NOT a proof that no other writer exists (for example a bulk copy/load path); "
                   "writer enumeration completeness is not established here."),
    }

    # ------------------- two candidate functions ------------------------- #
    c1 = decode_linear(blob, pe, 0x457880, 0x4579EE)
    c1_recs = require_pcs(c1, [
        (0x457887, "[esi + 0x3e6]"), (0x45788D, "0x8000"), (0x457892, "0x8000"),
        (0x457897, "0x4578ac"), (0x457899, "[esi + 0x403]"), (0x45789F, "0x6f2976"),
        (0x4578A4, "cl"), (0x4578A6, "0x4579e3"), (0x4578AC, "[esi + 0x403]"),
        (0x4578B2, "[esi + 0x370]"), (0x4578DA, "[eax + edx*2 + 0x1b9]"),
        (0x4578F0, "ecx"), (0x4578F3, "ecx"), (0x4578F5, "edx, 5"),
        (0x457909, "0xfffe"), (0x45791C, "0xfffe"), (0x457924, "0x4579e3"),
        (0x45792E, "[esi + 0x3ff]"), (0x457954, "ecx"), (0x457988, "[ecx + eax*2 + 0x1b9]"),
        (0x4579AE, "0xfffe"), (0x4579C3, "0xfffe"), (0x4579CE, "ebp"),
        (0x4579D4, "0x48b120")], failures, "function 0x457880")
    c1_load = next(i for i in c1 if i.address == 0x4578DA)
    c1_load2 = next(i for i in c1 if i.address == 0x457988)
    c1_mem = insn_record(c1_load)["mem_ops"][0]
    if c1_mem["disp"] != 0x1B9 or c1_mem["scale"] != 2 or c1_mem["size"] != 2:
        failures.append("0x4578da operand is not word [base + idx*2 + 0x1b9]")
    if insn_record(c1_load2)["mem_ops"][0]["disp"] != 0x1B9:
        failures.append("0x457988 operand displacement differs from 0x1b9")

    c2 = decode_linear(blob, pe, 0x458F60, 0x459127)
    c2_recs = require_pcs(c2, [
        (0x45902A, "0x5558d0"), (0x459039, "[edi + 0x403]"), (0x45903F, "[edi + 0x370]"),
        (0x459045, "cl, 1"), (0x459056, "edx, dword ptr [esp + 0x10]"), (0x459060, "ff"),
        (0x459066, "[ecx + edx*2 + 0x237]"), (0x459070, "[eax + 0x5e0]"),
        (0x459094, "0xfe"), (0x4590A5, "0xfe"), (0x4590B0, "ecx"), (0x4590B2, "0x4590ec"),
        (0x4590BB, "0x4a7d90"), (0x4590D4, "0x517490")], failures, "function 0x458f60")
    c2_load = next(i for i in c2 if i.address == 0x459066)
    c2_mem = insn_record(c2_load)["mem_ops"][0]
    if c2_mem["disp"] != 0x237 or c2_mem["scale"] != 2 or c2_mem["size"] != 2:
        failures.append("0x459066 operand is not word [base + idx*2 + 0x237]")

    def overlap_indices(disp, target_intervals):
        found = {}
        for name, lo, hi in target_intervals:
            for idx in range(0, 256):
                s = disp + 2 * idx
                if s <= hi and s + 1 >= lo:
                    found.setdefault(idx, {"index": idx, "byte_interval": [s, s + 1],
                                           "targets": []})["targets"].append(name)
        return [found[k] for k in sorted(found)]

    target_intervals = [(name, lo, hi) for name, lo, hi in TARGETS]

    two = [
        {
            "function": "0x00457880",
            "saved_candidate_loads": ["0x004578da", "0x00457988"],
            "primary_instructions": c1_recs,
            "index_producer": {
                "source": "byte[ESI + 0x403] (ESI = this; also reloaded as BL at 0x4578ac)",
                "mask": "AND EDX,0xff at 0x4578d4 / AND EAX,0xff at 0x45797a",
                "domain": "creature experience level, supported 1..10 (see level_domain)",
            },
            "in_function_guard": {
                "condition": "([ESI+0x3e6] & 0x8000) == 0x8000",
                "bounded_path": "byte[ESI+0x403] < byte[0x006f2976]; else JNC 0x4579e3 (returns 0)",
                "bound_value": ("byte[0x006f2976] is the 1-byte runtime copy of Variables type 192 "
                                "(COMBAT_PIT_MAX_EXPERIENCE_LEVEL); the installed KWD record has "
                                "Value=%s" % (kwd["value"] if kwd else "unavailable")),
                "unbounded_path": "the bound is skipped entirely when flag bit 0x8000 is clear",
            },
            "address_arithmetic": {
                "table_pointer": "[0x006e4154], conditional identity to 0x6f2550 (see base note)",
                "load": ("word[base + (level & 0xff)*2 + 0x1b9]; with base = 0x6f2550 the "
                         "address is 0x6f2709 + 2*level, so index 244 starts at 0x6f28f1 and "
                         "index 245 at 0x6f28f3 (one-byte shifts across the word targets)"),
                "required_index_for_overlap": overlap_indices(0x1B9, target_intervals),
            },
            "downstream": {
                "scale": "IMUL by word[EDI+0x6b5] (EDI = [ESI+0x370] definition), then /100 via magic 0x51eb851f",
                "clamp": "negative -> 0; value >= 0xfffe -> 0xfffe",
                "consume": ("accumulator [ESI+0x3ff] += stack arg; the scaled/clamped value is compared "
                            "with the accumulator (CMP EBP,EDI at 0x4579ce); when accumulator >= value "
                            "the code calls 0x48b120 (creature level-up)"),
                "zero_path": "if the scaled value is 0 the function returns the accumulator unchanged",
            },
            "verdict": "candidate_resolved_as_non_consumer_under_supported_index_domain",
            "reason": ("The saved byte-overlap candidates need level 244..246. The located writers give an "
                       "inductive supported domain 1..10, and when flag 0x8000 is set the function itself "
                       "rejects any level >= byte[0x6f2976] (=%s). Therefore neither load reads bytes "
                       "0x3a2/0x3a4 in the supported domain. Residual uncertainty: writer enumeration is "
                       "not proven complete, and the alias base identity is conditional."
                       % (kwd["value"] if kwd else "type192")),
        },
        {
            "function": "0x00458f60",
            "saved_candidate_load": "0x00459066",
            "primary_instructions": c2_recs,
            "index_producer": {
                "source": "byte[EDI + 0x403] where EDI = [ESI+0x8] (same creature object)",
                "mask": "AND EDX,0xff at 0x459060",
                "domain": "creature experience level, supported 1..10 (see level_domain)",
            },
            "in_function_guard": {
                "condition": "none on the index; the function only checks CALL 0x496d00 and the "
                             "word[creature+0x43d] global-table lookup at 0x458f7e",
                "note": "no upper bound on byte[+0x403] exists inside 0x458f60",
            },
            "address_arithmetic": {
                "table_pointer": "[0x006e4154], conditional identity to 0x6f2550 (see base note)",
                "load": "word[base + (level & 0xff)*2 + 0x237]",
                "required_index_for_overlap": overlap_indices(0x237, target_intervals),
            },
            "downstream": {
                "scale": "IMUL by byte[[EDI+0x370]+0x5e0] definition byte, then /100 via magic 0x51eb851f",
                "clamp": "negative -> 0; value >= 0xfe -> 0xfe (byte accumulator)",
                "consume": ("compared with the roll returned by CALL 0x5558d0 (range argument 0x64); "
                            "roll < value takes the success branch that calls 0x4a7d90 and schedules "
                            "0xdc, otherwise the failure branch calls the virtual +0x20 and 0x481610"),
            },
            "verdict": "candidate_resolved_as_non_consumer_under_supported_index_domain",
            "reason": ("The saved overlap candidates need level 181..183. The same level field is bounded "
                       "by its located writers to 1..10, and 0x458f60 adds no bound of its own. In the "
                       "supported domain the load cannot reach bytes 0x3a2/0x3a4. Residual uncertainty: "
                       "writer enumeration completeness and the conditional base identity."),
        },
    ]

    # ------------------- type192 / base conditional note ----------------- #
    type192 = {
        "kwd": {"path": kwd_path, "sha256": sha256_file(kwd_path) if os.path.exists(kwd_path) else None},
        "record": kwd,
        "guard_site": "0x0045789f mov al,[0x006f2976] ; 0x004578a4 cmp cl,al ; 0x004578a6 jnc 0x004579e3",
        "note": ("The guard value is file-controlled. The installed editor file yields %s; it is a "
                 "runtime 1-byte copy of the type-192 payload, not a hard-coded constant."
                 % (kwd["value"] if kwd else "unavailable")),
    }

    base_note = {
        "base": "0x006f2550",
        "hits": base_hits,
        "hit_address_note": ("Each hit 'va' is the first byte of the 4-byte little-endian literal "
                             "0x006f2550 (the moffs/disp32/imm32 field), which can sit inside the "
                             "instruction. 'instruction.pc' is the Capstone-validated instruction "
                             "boundary."),
        "identity_status": ("CONDITIONAL. One verified instruction stores the literal 0x006f2550 into "
                            "[0x006e4154] (0x509a96), and a bulk zero-fill materialises the same literal "
                            "in EDI. That does not prove the pointer at [0x006e4154] is never rewritten by "
                            "another path, so alias-based address arithmetic stays conditional."),
        "withdrawn_label": ("The store at 0x51cdb0 (a3 50 25 6f 00, mov dword ptr [0x006f2550],eax) was "
                            "previously labelled 'Type112'. phase101 puts the Type112 store at 0x51d13f -> "
                            "0x6f28f0. No selector was traced for 0x51cdb0 here; only its verified bytes are "
                            "reported."),
    }

    retractions = [
        "The '346 parity exclusions' and '133 scale-2 reads excluded' counts are invalid: they compared "
        "operand START equality and ignored operand width. They are replaced by 33 load PCs / 66 target-load "
        "pairs / 132 load-target-index triples of byte-interval candidates with no exclusion claim.",
        "The hand classify_abs_reference decoder is removed from the proof. It mis-sized moffs/movzx/movups "
        "and missed CMP/ADD memory reads; instruction size/access now comes from Capstone on the pinned EXE.",
        "The claim 'exhaustive wide-read absence' for 113/114 is withdrawn. The census is raw candidate "
        "arithmetic over the saved phase200 reads only.",
        "The 'Type112' label for the store at 0x51cdb0 is withdrawn; only verified store bytes are reported.",
        "Base identity 0x006e4154 == 0x006f2550 is conditional, not unconditional proof.",
        "The loader stores and 0x47bb4e DWORD analysis are historical reproduction (phase200), not new "
        "findings and not new semantics.",
        "The earlier derivation '0x6F2976 = type 179' is wrong; VARIABLE_RUNTIME maps type 192 to 0x6f2976 "
        "and type 179 to 0x6f295e.",
    ]

    result = {
        "audit": "globals-113-114-read-audit",
        "pass": "correction-pass-2",
        "scope": ("Bounded static primary-byte/dataflow pass on two candidate functions. Not a native "
                  "runtime observation and not a whole-game non-use claim."),
        "exe": {"path": exe_path, "sha256": digest, "pinned": EXE_SHA256,
                "match": digest == EXE_SHA256, "size": len(blob),
                "image_base": hex(pe["image_base"]),
                "capstone": capstone.__version__},
        "targets": {
            "113": {"va": hex(T113), "base_plus": hex(OFF113), "decimal": OFF113, "size": 2},
            "114": {"va": hex(T114), "base_plus": hex(OFF114), "decimal": OFF114, "size": 2},
        },
        "retractions": retractions,
        "historical_reproduction": {
            "note": ("Repeat primary-byte checks of evidence already established in phase200/phase101; "
                     "kept for reproducibility only, not presented as new semantics."),
            "loader_stores": loader_stores,
            "known_overlap_dword": overlap,
        },
        "broad_candidate_census": census,
        "literal_base_census": base_note,
        "type192_bound": type192,
        "level_domain": level_domain,
        "two_function_investigation": two,
        "verdicts": {
            "function_0x457880": ("Loads 0x4578da/0x457988 are creature-level table lookups. Byte overlap "
                                  "with WORD 113/114 is arithmetic only (indices 244..246) and is outside the "
                                  "supported level domain 1..10; the 0x8000-flag path additionally rejects "
                                  "level >= byte[0x6f2976]. Not shown to be a 113/114 consumer. Static only."),
            "function_0x458f60": ("Load 0x459066 is the same creature-level table lookup (indices 181..183 for "
                                  "the byte overlap). No index bound exists inside the function, but the same "
                                  "supported level domain 1..10 excludes those indices. Not shown to be a "
                                  "113/114 consumer. Static only."),
            "global": ("No supported gameplay consumer of WORD 113/114 was established in this pass. The "
                       "broad alias list remains unvalidated candidate arithmetic, so no absence claim is made."),
        },
        "open_paths": [
            "Writer-enumeration completeness for creature +0x403 is not proven; a bulk copy/load writer "
            "would change the 1..10 supported domain.",
            "The pointer at [0x006e4154] is assumed to equal 0x006f2550 for both candidate functions; only "
            "one literal store was verified, so this is conditional.",
            "The 132 triples from the saved phase200 reads are arithmetic candidates; no per-read index "
            "producer/bound was derived outside the two selected functions.",
            "No native (runtime) observation was performed in this pass; all conclusions are static.",
        ],
        "failures": failures,
    }

    if write:
        json_path = os.path.join(out_dir, "globals-113-114-read-audit.json")
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(result, f, indent=2, ensure_ascii=False)
            f.write("\n")
        excerpt_dir = os.path.join(root, "ghidra")
        inputs = {os.path.basename(phase200_path): sha256_file(phase200_path)}
        if os.path.exists(kwd_path):
            inputs[os.path.basename(kwd_path)] = sha256_file(kwd_path)
        for name in ("phase200-DX-function-457880-assembly.txt",
                     "phase200-DX-function-458f60-assembly.txt",
                     "phase200-DX-function-4872a0-assembly.txt",
                     "phase200-DX-function-48afa0-assembly.txt",
                     "phase200-DX-function-48b120-assembly.txt"):
            p = os.path.join(excerpt_dir, name)
            if os.path.exists(p):
                inputs["ghidra/" + name] = sha256_file(p)
        manifest = {
            "exe": {"path": exe_path, "sha256": digest, "pinned": EXE_SHA256},
            "script": {"file": os.path.basename(__file__), "sha256": sha256_file(os.path.abspath(__file__))},
            "inputs": inputs,
            "outputs": {os.path.basename(json_path): sha256_file(json_path)},
            "capstone": capstone.__version__,
            "failures": failures,
        }
        with open(os.path.join(out_dir, "AUDIT_MANIFEST.json"), "w", encoding="utf-8") as f:
            json.dump(manifest, f, indent=2, ensure_ascii=False)
            f.write("\n")
        result["_written"] = json_path
    if os.environ.get("DK2_AUDIT_DEBUG") and failures:
        sys.stderr.write("FAILURES:\n" + "\n".join(" - " + f for f in failures) + "\n")
    return 2 if failures else 0


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--no-write", action="store_true", help="compute only, write nothing")
    ap.add_argument("--exe", default=EXE_DEFAULT)
    ap.add_argument("--phase200", default=None)
    ap.add_argument("--kwd", default=KWD_DEFAULT)
    ap.add_argument("--out-dir", default=None)
    ap.add_argument("--print", dest="do_print", action="store_true",
                    help="print the compact result to stdout")
    a = ap.parse_args(argv)
    rc = audit(write=not a.no_write, exe_path=a.exe, phase200_path=a.phase200,
               kwd_path=a.kwd, out_dir=a.out_dir)
    if rc != 0:
        sys.stderr.write("audit inconsistencies:\n")
    if a.do_print:
        json.dump({"rc": rc}, sys.stdout)
        sys.stdout.write("\n")
    return rc


if __name__ == "__main__":
    sys.exit(main())
