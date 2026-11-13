"""Verify three original DX sound selectors against prepared runtime records."""
import json
from pathlib import Path
from struct import pack, pack_into, unpack_from

from build_effect_semantics import EXE
from emulate_element_colors import region
from emulate_resistance import post_context
import native_unicorn_mapreader as cpu

ROOT = Path(__file__).resolve().parent
PLAYER, GROUP, ITEMS, LINKS, SOUNDS, STOP = range(0x1D000000, 0x1D060000, 0x10000)
SEED = 0x7A53B0


def replay(address, memory, seed, stack=()):
    memory[SEED] = pack("<I", seed)
    memory[0x7FFEE000] = bytes(0x4000)
    args = dict(address=hex(address), registers=json.dumps(dict(ECX=hex(PLAYER))),
                stack_args=list(stack), before_calls=[], max_steps=10000,
                memory=json.dumps(dict(regions=[region(k, v) for k, v in memory.items()])))
    result = cpu.run(EXE, args, list(memory.items()), STOP, PLAYER, len(memory[PLAYER]))
    assert result["success"] and not result["protected_write_or_execution_failures"], result["trace_tail"]
    state = post_context(memory, result)
    return int(result["registers"]["EAX"], 16), state


def next_seed(seed):
    return (seed * 0x19660D + 0x3C6EF35F) & 0xFFFFFFFF


def rotated(seed):
    return (seed >> 13) | ((seed << 19) & 0xFFFFFFFF)


def player(group=0, item=0):
    state = bytearray(0x60)
    pack_into("<2I", state, 4, group, item)
    return state


def link_cases():
    rows = []
    entries = [(1, 20, 39), (2, 20, 39), (1, 20, 39), (3, 40, 80)]
    for intensity in [19, 20, 39, 40, 80, 81]:
        for seed in [0, 1, 2, 3, 0xFFFFFFFF]:
            matches = [target for target, low, high in entries if low <= intensity <= high]
            expected_seed = next_seed(seed) if matches else seed
            selected = matches[rotated(expected_seed) % len(matches)] if matches else 0
            items = bytearray(42 * 4)
            pack_into("<I", items, 4, len(entries))
            pack_into("<I", items, 38, LINKS)
            links = b"".join(pack("<I4B", ITEMS + target * 42, 0, 0, low, high)
                             for target, low, high in entries)
            retval, post = replay(0x6163C0, {
                PLAYER: player(group=GROUP), GROUP: pack("<3I", 0, 4, ITEMS),
                ITEMS: items, LINKS: links,
            }, seed, [intensity])
            current, = unpack_from("<I", post[PLAYER], 8)
            flags, = unpack_from("<I", post[PLAYER], 0x28)
            assert retval == bool(matches) and current == ITEMS + selected * 42
            assert flags == (0 if matches else 0x100)
            assert post[PLAYER][0x44] == (0 if matches else 0x7F)
            assert unpack_from("<I", post[SEED])[0] == expected_seed
            rows.append(dict(intensity=intensity, seed=seed, selected_item_index=selected + 1 if matches else None,
                             current_pointer_retained_on_no_match=not matches, after_seed=expected_seed))
    for name, initial, link_target in [("self_link", 0, 0), ("terminal_item", ITEMS + 42, None)]:
        items = bytearray(42 * 2)
        if link_target is not None:
            pack_into("<I", items, 4, 1)
            pack_into("<I", items, 38, LINKS)
        memory = {PLAYER: player(group=GROUP, item=initial), GROUP: pack("<3I", 0, 2, ITEMS), ITEMS: items}
        if link_target is not None:
            memory[LINKS] = pack("<I4B", ITEMS, 0, 0, 20, 39)
        retval, post = replay(0x6163C0, memory, 0, [20])
        current, = unpack_from("<I", post[PLAYER], 8)
        flags, = unpack_from("<I", post[PLAYER], 0x28)
        assert current == (ITEMS if link_target is not None else 0)
        assert retval == (1 if link_target is not None else 0)
        assert flags == 0 and post[PLAYER][0x44] == 0
        rows.append(dict(case=name, seed=0, current=current, flags=flags, intensity_byte=post[PLAYER][0x44]))
    return rows


def weight_cases():
    rows = []
    for kind, address in [("sound", 0x60CFF0), ("item", 0x60D060)]:
        for name, weights in [
            ("inclusive", [rotated(next_seed(0)) >> 16, 65535]),
            ("full_DWORD", [0x00010000, 65535]),
            ("second_wins", [0, 65535]),
            ("fallback_first", [0, 1]),
            ("only_one", [0]),
        ]:
            stride, offset = (16, 4) if kind == "sound" else (42, 30)
            values = bytearray(stride * len(weights))
            for i, weight in enumerate(weights):
                pack_into("<I", values, i * stride + offset, weight)
            expected_seed = 0 if len(weights) == 1 else next_seed(0)
            random_value = rotated(expected_seed) >> 16
            chosen = next((i for i, value in enumerate(weights) if value >= random_value), 0)
            directory = pack("<3I", len(weights), len(weights), SOUNDS)
            ret, post = replay(address, {
                PLAYER: player(group=GROUP, item=GROUP), GROUP: directory, SOUNDS: values,
            }, 0)
            selected = ret if kind == "sound" else unpack_from("<I", post[PLAYER], 8)[0]
            assert selected == SOUNDS + stride * chosen
            assert unpack_from("<I", post[SEED])[0] == expected_seed
            rows.append(dict(kind=kind, case=name, weights=weights, index=chosen + 1, after_seed=expected_seed))
    return rows


def delay_cases():
    rows = []
    for low, high in [(0, 0), (8000, 8000), (8000, 20000)]:
        for seed in [0, 42, 0xFFFFFFFF]:
            item = bytearray(42)
            pack_into("<2H", item, 0x10, low, high)
            ret, post = replay(0x60CCF0, {PLAYER: player(item=ITEMS), ITEMS: item}, seed)
            after = next_seed(seed) if high > low else seed
            expected = low + (rotated(after) % (high - low) if high > low else 0)
            assert ret == expected and unpack_from("<I", post[SEED])[0] == after
            rows.append(dict(minimum=low, maximum=high, seed=seed, return_ms=ret, after_seed=after))
    return rows



def parameter_cases():
    rows = []
    for parameter in (2, 9):
        state = player()
        state[0x40:0x44] = bytes([2, 1, 2, 0])
        state[0x44:0x48] = bytes([20, 5, 30, 0])
        _, post = replay(0x60C750, {PLAYER: state}, 0, [parameter, 80])
        wanted = bytes([80, 5, 80, 0]) if parameter == 2 else bytes([20, 5, 30, 0])
        assert post[PLAYER][0x44:0x48] == wanted
        rows.append(dict(parameter=parameter, values=list(wanted)))
    return rows


def range_cases():
    rows = []
    specs = [("volume", 0x60CAA0, 12, 20, 80, 1),
             ("signed_range", 0x60CB90, 14, -20, 30, 2)]
    for name, address, offset, minimum, maximum, mask in specs:
        for mode in ("random", "reverse", "constant", "slot1", "slot2"):
            lower, upper = minimum, maximum
            raw = bytearray(42)
            if mode == "reverse":
                lower, upper = maximum, minimum
            if mode == "constant":
                upper = lower
            raw[offset] = lower & 255
            raw[offset + 1] = upper & 255
            if mode == "slot1":
                raw[0x18] = mask
            if mode == "slot2":
                raw[0x1C] = mask
            state = player(item=ITEMS)
            state[0x45:0x47] = bytes([50, 25])
            value, post = replay(address, {PLAYER: state, ITEMS: raw}, 42)
            low, high = sorted((lower, upper))
            advanced = high > low and mode not in ("slot1", "slot2")
            after = next_seed(42) if advanced else 42
            if mode in ("slot1", "slot2"):
                percent = 50 if mode == "slot1" else 25
                wanted = low + (high-low) * percent // 100
            else:
                wanted = low + (rotated(after) % (high-low) if advanced else 0)
            signed = value if value < 0x80000000 else value - 0x100000000
            assert signed == wanted
            assert post[ITEMS][offset:offset+2] == bytes([low & 255, high & 255])
            assert unpack_from("<I", post[SEED])[0] == after
            rows.append(dict(kind=name, mode=mode, original_min=lower, original_max=upper,
                             value=signed, after_seed=after, range_bytes_after=list(post[ITEMS][offset:offset+2])))
    return rows


def main():
    result = dict(profile=EXE.name, exe_sha256=cpu.sha(EXE.read_bytes()),
                  links=link_cases(), weights=weight_cases(), delays=delay_cases(), ranges=range_cases(), parameters=parameter_cases(),
                  scope="Prepared pointer-resolved data, whole selector functions. No real audio output.")
    (ROOT / "dethware-DX-selector-replays.json").write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf8")
    print("Verified", *(f"{k}={len(result[k])}" for k in ("links", "weights", "delays")))


if __name__ == "__main__":
    main()

