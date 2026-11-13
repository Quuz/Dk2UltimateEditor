"""Replay battle music requests through the installed DX loader and scorer."""
import json
import struct
import emulate_temple_creature_entry as creature

import emulate_temple_file_rewards as fixture
from emulate_element_colors import region
from emulate_resistance import post_context
from spell_state_tables import BASE as STATE_BASE, initialized_states

ROOT, EXE = fixture.ROOT, fixture.EXE
WORLD = fixture.f.WORLD
BATTLE, ACTOR, DEFINITION, MANAGER = [0x1C000000 + n * 0x10000 for n in range(4)]
VARIABLES = 0x6F2550
CONNECT_MANAGER = 0x1C040000


def cases():
    return [
        ("below-two", 99, 1, None, 8, 0xFF, (100, 500, 1500), 0xFF),
        ("equal-two", 100, 1, None, 8, 0xFF, (100, 500, 1500), 0x101),
        ("below-three", 499, 1, None, 8, 0xFF, (100, 500, 1500), 0x101),
        ("equal-three", 500, 1, None, 8, 0xFF, (100, 500, 1500), 0x102),
        ("below-four", 1499, 1, None, 8, 0xFF, (100, 500, 1500), 0x102),
        ("equal-four", 1500, 1, None, 8, 0xFF, (100, 500, 1500), 0x103),
        ("same-owner", 1500, 1, None, 7, 0xFF, (100, 500, 1500), 0xFF),
        ("level-two-file-growth", 400, 2, 125, 8, 0xFF, (100, 500, 1500), 0x102),
        ("level-two-truncates", 399, 2, 125, 8, 0xFF, (100, 500, 1500), 0x101),
        ("signed-growth-unsigned-score", 100, 2, -100, 8, 0xFF, (100, 500, 1500), 0x103),
        ("raise-keeps-highest", 100, 1, None, 8, 0x103, (100, 500, 1500), 0x103),
        ("file-changes-threshold", 500, 1, None, 8, 0xFF, (100, 501, 1500), 0x101),
        ("raw-creature-below", 499, 1, None, 8, 0xFF, (100, 500, 1500), 0x101),
        ("raw-creature-equal", 500, 1, None, 8, 0xFF, (100, 500, 1500), 0x102),
        ("raw-creature-growth", 400, 2, 125, 8, 0xFF, (100, 500, 1500), 0x102),
    ]


def build(case):
    name, threat, level, growth, owner, previous, thresholds, expected = case
    loader = fixture.files
    records = [loader.rec(kind, value, 0, 0) for kind, value in zip((215, 216, 217), thresholds)]
    if growth is not None:
        records.append([65, 3, growth & 0xFFFFFFFF, level])
    source = dict(loader.cases()[0], source=None, profile="capture", records=records)
    args, _ = loader.build(source)
    memory = {int(row["address"], 16): bytearray.fromhex(row["hex"])
              for row in json.loads(args["memory"])["regions"]}
    world = bytearray(0xA3D0)
    struct.pack_into("<I", world, 0, 0x66E3EC)
    struct.pack_into("<H", world, 0x7243, 7)
    registration, _ = loader.registration()
    world[0x1F24:0x1F24 + len(registration)] = registration
    battle = bytearray(0x26)
    struct.pack_into("<IH", battle, 0, WORLD, 100)
    actor = bytearray(1343)
    struct.pack_into("<H", actor, 0x28, owner)
    struct.pack_into("<I", actor, 0x25E, 0xC7)
    struct.pack_into("<I", actor, 0x370, DEFINITION)
    struct.pack_into("<I", actor, 0x37A, 1)
    actor[0x403] = level
    definition = bytearray(2199)
    struct.pack_into("<H", definition, 0x698, threat)
    manager = bytearray(0x40)
    struct.pack_into("<I", manager, 0, 0x66F7FC)
    struct.pack_into("<I", manager, 0x2C, previous)
    tags = bytearray(404)
    struct.pack_into("<I", tags, 400, ACTOR)
    memory.update({
        WORLD: world, BATTLE: battle, ACTOR: actor, DEFINITION: definition, MANAGER: manager,
        0x6EE540: tags, 0x75B468: struct.pack("<I", MANAGER),
        VARIABLES: bytearray(4096), 0x6E4154: struct.pack("<I", VARIABLES),
        STATE_BASE: bytes(len(initialized_states()[0])),
    })
    raw_calls = []
    if name.startswith("raw-creature-"):
        cm, calls = creature.memory_and_calls(creature.cases()[0])
        for address in (creature.RAW, creature.MID, creature.SOUND, creature.SV,
                        creature.SCB, creature.PLAY, creature.SESSION, creature.OUT, 0x75B468, 0):
            memory[address] = bytearray(cm[address])
        struct.pack_into("<H", memory[creature.RAW], 0xF12, threat)
        struct.pack_into("<I", memory[creature.SESSION], 0x14, WORLD)
        struct.pack_into("<I", world, 4, creature.SESSION)
        memory[DEFINITION] = bytearray([165]) * (2199 + 64)
        struct.pack_into("<I", memory[VARIABLES], 0x40, 20)
        raw_calls = [*calls[3:6], dict(address="0x492320", registers=dict(ECX=hex(creature.DEFINITION)),
                                        stack_args=[creature.MID, WORLD, 0])]
        memory[creature.DEFINITION] = memory.pop(DEFINITION)
        struct.pack_into("<I", actor, 0x370, creature.DEFINITION)
        # Supply the resource lookup service during loading, then point the global
        # at the prepared real-class manager before the battle scorer runs.
        memory[CONNECT_MANAGER] = bytes.fromhex("c705") + struct.pack("<II", 0x75B468, MANAGER) + bytes.fromhex("c3")
        raw_calls.append(dict(address=hex(CONNECT_MANAGER), registers=dict(ECX="0"), stack_args=[]))
        args["allowed_external_IO_code"] += [[a, len(memory[a])] for a in (creature.SCB, creature.PLAY, CONNECT_MANAGER)]
    struct.pack_into("<II", memory[loader.CFG], 0xD8, 0x521790, WORLD)
    args.update(
        address="0x505310", registers=json.dumps(dict(ECX=hex(BATTLE))),
        stack_args=[], before_calls=[
            dict(address="0x47bbe0", registers=dict(ECX="0"), stack_args=[]),
            *raw_calls, *args["before_calls"],
            dict(address="0x56cbd0", registers=dict(ECX="0"), stack_args=[loader.REF, loader.CFG]),
        ],
        observe_reads=[[VARIABLES, 0x500], [DEFINITION + 0x698, 2], [creature.DEFINITION + 0x698, 2], [WORLD + 0x7243, 2]],
        observe_writes=[[MANAGER, len(manager)]],
        memory=json.dumps(dict(regions=[region(address, data) for address, data in memory.items()])),
    )
    excluded = {0x7FFEE000} | {address for address, _ in args["allowed_external_IO_code"]}
    return args, {address: bytes(data) for address, data in memory.items() if address not in excluded}


def main():
    outcomes = []
    for case in cases():
        name, threat, level, growth, owner, previous, thresholds, expected = case
        args, checks = build(case)
        result = fixture.cpu.run(EXE, args, list(checks.items()), fixture.STOP, MANAGER, 0x40)
        assert result["success"] and not result["protected_write_or_execution_failures"], (
            name, result["emulation_error"], result["trace_tail"])
        assert len(result["before_calls"]) == len(args["before_calls"])
        assert all(c["returned"] for c in result["before_calls"])
        assert int(result["before_calls"][-1]["return_EAX"], 16) == 1
        post = post_context(checks, result)
        assert struct.unpack_from("<3I", post[VARIABLES], 0x465) == thresholds
        if growth is not None:
            assert struct.unpack_from("<h", post[VARIABLES], 0x105 + 2 * level)[0] == growth
        if name.startswith("raw-creature-"):
            assert struct.unpack_from("<H", post[creature.DEFINITION], 0x698)[0] == threat
            assert post[creature.RAW] == checks[creature.RAW]
            assert post[creature.DEFINITION][2199:] == checks[creature.DEFINITION][2199:]
        actual = struct.unpack_from("<I", post[MANAGER], 0x2C)[0]
        assert actual == expected, (name, actual, expected)
        pcs = result["visited_instructions"]
        assert pcs.get("0x508c60") == 1 and pcs.get("0x505310") == 1
        expected_calls = 0 if name in ("below-two", "same-owner") else 1
        assert pcs.get("0x567bc0", 0) == expected_calls
        writes = result["observed_writes"]
        assert len(writes) == int(previous != expected)
        if writes:
            assert writes[0]["pc"] == "0x567bcb" and writes[0]["value"] == expected
        outcomes.append(dict(case=name, threat=threat, level=level, growth=growth, actor_owner=owner,
                             world_owner=7, thresholds=list(thresholds), before=previous,
                             after=actual, raise_calls=expected_calls, instructions=result["instruction_count"],
                             verified_read_PCs=sorted({row["pc"] for row in result["observed_reads"]})))
        print(name, hex(actual), flush=True)
    fixture.dump("sound-DX-music-replays.json", dict(replays=outcomes, original_EXE_sha256=fixture.sha(EXE.read_bytes())))


if __name__ == "__main__":
    main()


