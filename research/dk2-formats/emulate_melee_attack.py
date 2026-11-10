"""Raw224/stat4 -> original possession setter -> full498E30 -> target HP.

Prepared post-factory Creature/Object instances retain original class vtables.
Native59D380/59D260 runs the absent-model fallback. Sound and Session-event
providers are explicit external IO captures, not replaced gameplay functions.
"""
from pathlib import Path
import asyncio
import hashlib
import json
import os
import struct
import sys
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
import emulate_possession_speed as sp
import emulate_creature_factory as cf
import emulate_object_factory as f
from emulate_element_colors import region, pack32
from emulate_hero_parties import compare_code
from native_possession_damage import project
from native_possession_recharge import s32, tz
import native_room_counters as rc

ROOT = Path(__file__).resolve().parent
TARGET = 0x10300000
TARGET_DEF = 0x10310000
CAP = 0x10320000
EVENT = 0x10330000
RAW65 = 0x10340000
ROOM = 0x10400000
MEMBER = 0x10410000
OTHEROWNER = 0x10420000


def cases():
    base = dict(percent=60, possessed=True, target_kind='object', target_possessed=False,
                base_damage=20, level=1, coefficient=125, hp=1000, dx=0, dy=-4096,
                owner1090=0, owner1091=0, owner1095=0, target_word443=0, target_word447=0,
                secondary=False, target_state=0, target_exists=True, cooldown=1, last_attack=0,
                object_damageable=True, target_word445=0, attacker_heading=0, target_heading=0)
    variants = [('object-possessed60', {}), ('object-normal60', dict(possessed=False)),
                ('creature-possessed60', dict(target_kind='creature')),
                ('creature-both-possessed', dict(target_kind='creature', target_possessed=True)),
                ('creature-normal60', dict(target_kind='creature', possessed=False)),
                ('object-possessed100', dict(percent=100)),
                ('object-secondary60', dict(possessed=False, secondary=True)),
                ('object-level2', dict(level=2)),
                ('creature-level2', dict(level=2, target_kind='creature')),
                ('object-owner1090-2', dict(owner1090=2)),
                ('object-owner1091-2', dict(owner1091=2)),
                ('object-owner-both2', dict(owner1090=2, owner1091=2)),
                ('creature-owner-reduction2', dict(target_kind='creature', owner1095=2)),
                ('creature-target443', dict(target_kind='creature', target_word443=1)),
                ('creature-target447', dict(target_kind='creature', target_word447=1)),
                ('creature-target443-447', dict(target_kind='creature', target_word443=1, target_word447=1)),
                ('creature-north', dict(target_kind='creature', dy=4096)),
                ('creature-east', dict(target_kind='creature', dx=4096, dy=0)),
                ('creature-west', dict(target_kind='creature', dx=-4096, dy=0)),
                ('creature-heading-changed', dict(target_kind='creature', attacker_heading=1024, target_heading=1024)),
                ('object-not-damageable', dict(object_damageable=False)),
                ('creature-target445', dict(target_kind='creature', target_word445=1)),
                ('target-state2', dict(target_state=2)), ('target-missing', dict(target_exists=False)),
                ('normal-cooldown-gate', dict(possessed=False, cooldown=101)),
                ('possessed-skips-cooldown', dict(cooldown=101)),
                ('normal-cooldown-boundary', dict(possessed=False, cooldown=100)),
                ('object-zero-damage', dict(base_damage=0)),
                ('object-high16-percent', dict(percent=0xABCD003C)),
                ('crypt-darkangel-insert', dict(crypt_species=23, crypt_initial=0, crypt_operation='insert')),
                ('crypt-darkangel-clear', dict(crypt_species=23, crypt_initial=1, crypt_operation='clear')),
                ('crypt-darkangel-two', dict(crypt_species=23, crypt_initial=1, crypt_operation='insert')),
                ('crypt-unique-darkangel-insert', dict(crypt_species=44, crypt_initial=0, crypt_operation='insert')),
                ('crypt-unique-darkangel-clear', dict(crypt_species=44, crypt_initial=1, crypt_operation='clear')),
                ('crypt-darkangel-normal', dict(possessed=False, crypt_species=23, crypt_initial=0, crypt_operation='insert')),
                ('crypt-darkangel-other-owner', dict(crypt_species=23, crypt_initial=0, crypt_operation='insert', crypt_owner=8)),
                ('crypt-unique-darkangel-other-owner', dict(crypt_species=44, crypt_initial=0, crypt_operation='insert', crypt_owner=8)),
                ('creature-only-target-heading', dict(target_kind='creature', target_heading=1024)),
                ('creature-only-attacker-heading', dict(target_kind='creature', attacker_heading=1024)),
                ('current-variable-records', dict(level=2, source_current=True))]
    return [dict(base, **change, name=name) for name, change in variants]


def outcome(c):
    effective = dict(c)
    crypt = None
    if 'crypt_species' in c:
        slot = rc.counter_slot(c['crypt_species'])
        assert slot in [0x1090, 0x1091]
        count = rc.change_counter(c['crypt_initial'], 1 if c['crypt_operation'] == 'insert' else -1)
        if c.get('crypt_owner', 7) == 7:
            effective[f'owner{slot:x}'] = count
        crypt = dict(slot=slot, count=count, room_tag=102 if c['crypt_operation'] == 'insert' else 0,
                     room55=1234 if c['crypt_operation'] == 'insert' else 0)
    getter = project(c['base_damage'], c['percent'], level=c['level'],
                     coefficient_bits=c['coefficient'], possessed=c['possessed'],
                     secondary=c['secondary'])
    cooldown_gate = not c['possessed'] and 100 <= ((c['last_attack'] - 1 + c['cooldown']) & 0xFFFFFFFF)
    target_valid = c['target_exists'] and c['target_state'] != 2
    active = not cooldown_gate and target_valid
    blocked = active and c['target_kind'] == 'creature' and bool(c['target_word445'])
    hit = active and not blocked
    delta = s32(-getter['result_bits'])
    # Cardinal angles are independently fixed by the original integer atan table.
    angle = {(0, -4096): 0, (0, 4096): 1024, (4096, 0): 512, (-4096, 0): 1536}[(c['dx'], c['dy'])]
    folded = 2046-angle if angle > 1023 else angle
    directional = (folded & 1023)//20 if c['target_kind'] == 'creature' else 0
    if directional > 15:
        delta = s32(delta + tz(s32(directional*delta), 100))
    for byte, multiplier in [(effective['owner1090'], lambda n: (n+10)*10),
                             (effective['owner1091'], lambda n: n*15+100)]:
        if 0 < byte < 128:
            delta = tz(s32(multiplier(byte)*delta), 100)
    applied = delta
    damage_call = hit and delta != 0
    if c['target_kind'] == 'object':
        if delta < 0 and not c['object_damageable']:
            applied = 0
    elif delta < 0:
        scale = 160 if c['target_word447'] else 128
        if c['target_possessed'] or c['target_word443']:
            scale -= 64
        if 0 < c['owner1095'] < 128:
            scale -= 8*c['owner1095']
        applied = tz(s32(max(0, scale)*delta), 128)
    if not damage_call:
        applied = 0
    hp = c['hp'] + applied
    assert hp > 0
    event = int(damage_call and c['target_kind'] == 'creature' and c['target_possessed'] and delta < 0)
    timestamp = damage_call and c['target_kind'] == 'creature'
    result = dict(getter=getter, angle=angle, direction_percent=directional,
                requested_delta=delta, applied_delta=applied, hp=hp, event=event,
                sound=int(hit), timestamp=bool(timestamp), damage_call=bool(damage_call),
                attack_return=int(cooldown_gate), active=active)
    if crypt is not None:
        result['crypt'] = crypt
    return result


def build(c):
    args, previous = sp.build(dict(sp.cases()[0], raw_index=4, percent=c['percent'],
                                  possessed=c['possessed'], other_flag=c['secondary']))
    original = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    memory = {a: b for a, b in original.items() if a < 0x11000000 and a not in [f.OBS, sp.DRIVER, 0x7FFEE000]}
    e = outcome(c)
    # Clone actual native class snapshots, then prepare explicit scene fields.
    if c['target_kind'] == 'creature':
        memory[TARGET] = bytearray(memory[cf.ACTOR])
        memory[TARGET_DEF] = bytearray(memory[cf.DEFINITION])
        f.put(memory[TARGET], 0x370, TARGET_DEF)
        f.put(memory[TARGET_DEF], 0x86F, 123, 2)
        for off, value in [(0x443, c['target_word443']), (0x445, c['target_word445']),
                           (0x447, c['target_word447']), (0xF4, c['target_heading'])]:
            f.put(memory[TARGET], off, value, 2)
    else:
        image = (ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes()
        assert len(image) == 393
        memory[TARGET] = bytearray(image)+bytearray([0xA5])*256
        memory[TARGET_DEF] = bytearray(memory[f.DEFINITION])
        f.put(memory[TARGET], 0x12D, TARGET_DEF)
        f.put(memory[TARGET_DEF], 0xF4, 0x200000 if c['object_damageable'] else 0)
    f.put(memory[TARGET], 4, 101, 2)
    f.put(memory[TARGET], 0x16, c['target_state'])
    f.put(memory[TARGET], 0x40, c['hp'])
    f.put(memory[0x6EE540], 101*4, TARGET)
    f.put(memory[0x6ED540], 101, int(c['target_exists']), 1)
    for off, delta in [(0x1A, c['dx']), (0x1E, c['dy'])]:
        f.put(memory[cf.ACTOR], off, int.from_bytes(memory[TARGET][off:off+4], 'little')+delta)
    for off, value, n in [(0x399, 101, 2), (0x403, c['level'], 1),
                           (0x3A5, c['last_attack'], 4), (0xF4, c['attacker_heading'], 2)]:
        f.put(memory[cf.ACTOR], off, value, n)
    for off, value, n in [(0x5C4, c['base_damage'], 2), (0x5CA, c['cooldown'], 4), (0x686, 0, 4)]:
        f.put(memory[cf.DEFINITION], off, value, n)
    for off, value in [(0x1090, c['owner1090']), (0x1091, c['owner1091']), (0x1095, c['owner1095'])]:
        f.put(memory[f.PLAYER], off, value, 1)
    session = memory[f.SESSION]
    session.extend(bytes(0x280-len(session)))
    f.put(session, 8, 0)
    gv = memory[f.GV]
    gv.extend(bytes(0x110-len(gv)))
    f.put(gv, 0x10C, 0x59D380)
    f.put(gv, 0x28, EVENT)
    # Prepared actor render-info has no loaded resource; native fallback executes.
    assert int.from_bytes(memory[cf.ACTOR][0x6C:0x70], 'little') == 0
    memory[CAP] = bytearray(24)
    memory[EVENT] = bytearray(bytes.fromhex('56578b74240c')+b'\xBF'+pack32(CAP+4)+b'\xB9'+pack32(4)
                             +bytes.fromhex('f3a5ff05')+pack32(CAP)+bytes.fromhex('5f5eb801000000c20400'))
    memory[RAW65] = bytearray(bytes(16)+struct.pack('<4I', 65, 4, c['coefficient'], c['level'])+bytes([0xA5])*64)
    if c.get('source_current'):
        doc = json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text(encoding='utf8'))
        data = Path(doc['source']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == doc['sha256']
        for address, words in [(sp.RAW, [224, 4, 60, 10]), (RAW65, [65, 4, 125, 2])]:
            row = next(r for r in doc['sections'][0]['records'] if r['words_u32'] == words)
            blob = bytes.fromhex(row['raw_hex'])
            assert data[row['offset']:row['offset']+16] == blob
            memory[address][16:32] = blob
    memory[f.OUT] = bytearray(24)+bytearray([0xA5])*40
    if 'crypt' in e:
        owner_id = c.get('crypt_owner', 7)
        room = bytearray(0xC0)+bytearray([0xA5])*64
        f.put(room, 4, 200, 2)
        f.put(room, 0x18, 26, 1)
        f.put(room, 0x1D, owner_id, 2)
        f.put(room, 0x53, 102 if c['crypt_operation'] == 'clear' else 0, 2)
        f.put(room, 0x55, 1234)
        memory[ROOM] = room
        member = bytearray(0x380)+bytearray([0xA5])*64
        f.put(member, 4, 102, 2)
        f.put(member, 0x12, 0)
        f.put(member, 0x36F, c['crypt_species'], 1)
        memory[MEMBER] = member
        for tag, address in [(200, ROOM), (102, MEMBER)]:
            f.put(memory[0x6EE540], tag*4, address)
            f.put(memory[0x6ED540], tag, 1, 1)
        owner = f.PLAYER
        if owner_id == 8:
            owner = OTHEROWNER
            memory[owner] = bytearray(0x1200)
            f.put(memory[0x6EE540], 8*4, owner)
            f.put(memory[0x6ED540], 8, 1, 1)
        f.put(memory[owner], e['crypt']['slot'], c['crypt_initial'], 1)
    checks = {a: bytearray(b) for a, b in memory.items() if a not in [f.GFX_CB, f.SOUND_CB, 0x28CFA4, EVENT]}
    if 'crypt' in e:
        f.put(checks[ROOM], 0x53, e['crypt']['room_tag'], 2)
        f.put(checks[ROOM], 0x55, e['crypt']['room55'])
        f.put(checks[owner], e['crypt']['slot'], e['crypt']['count'], 1)
    f.put(checks[sp.VARS], 0x31B, c['percent'], 2)
    f.put(checks[sp.VARS], 0x117+2*c['level'], c['coefficient'], 2)
    if c['possessed']:
        f.put(checks[cf.ACTOR], 0x3E6, int.from_bytes(checks[cf.ACTOR][0x3E6:0x3EA], 'little') | 0x80)
        f.put(checks[cf.ACTOR], 0x488, 0)
        f.put(checks[cf.ACTOR], 0x3EE, 0)
    if c['target_possessed']:
        f.put(checks[TARGET], 0x3E6, int.from_bytes(checks[TARGET][0x3E6:0x3EA], 'little') | 0x80)
        f.put(checks[TARGET], 0x488, 0)
        f.put(checks[TARGET], 0x3EE, 0)
    f.put(checks[cf.ACTOR], 0x3EA, int.from_bytes(checks[cf.ACTOR][0x3EA:0x3EE], 'little') & 0xFFFFEFFF)
    if e['sound']:
        f.put(checks[cf.ACTOR], 0x2E, 0x13572468)
    if e['damage_call']:
        f.put(checks[TARGET], 0x36, 7, 2)
    if e['timestamp']:
        for off in [0x38, 0x3C]:
            f.put(checks[TARGET], off, 100)
    f.put(checks[TARGET], 0x40, e['hp'])
    f.put(checks[f.SESSION], 0x26C, e['event'])
    if e['event']:
        struct.pack_into('<5I', checks[CAP], 0, 1, 7, 8, 123, 0x12)
    struct.pack_into('<6I', checks[f.OUT], 0, 1, e['attack_return'], e['sound'], 0, e['hp'], 1)
    driver = bytearray(b'\xA3'+pack32(f.OUT))
    def emit(b):
        driver.extend(b)
    def call(a):
        emit(b'\xE8'+pack32(a-(sp.DRIVER+len(driver)+5)))
    if 'crypt' in e:
        emit(b'\x68'+pack32(e['crypt']['room_tag'])+b'\xB9'+pack32(ROOM))
        call(0x4D8E60)
    emit(b'\x68'+pack32(RAW65))
    call(0x51DC40)
    emit(b'\xA3'+pack32(f.OUT+20))
    for a, enabled in [(cf.ACTOR, c['possessed']), (TARGET, c['target_possessed'])]:
        if enabled:
            emit(b'\x68'+pack32(1)+b'\xB9'+pack32(a))
            call(0x48DEA0)
    emit(b'\xB9'+pack32(cf.ACTOR))
    call(0x498E30)
    emit(b'\xA3'+pack32(f.OUT+4)+b'\xA1'+pack32(TARGET+0x40)+b'\xA3'+pack32(f.OUT+16))
    emit(b'\x68'+pack32(f.OBS)+b'\xC3')
    memory[sp.DRIVER] = driver
    stack = bytearray([0xA5])*16384
    struct.pack_into('<2I', stack, 8192, sp.DRIVER, sp.RAW)
    memory[0x7FFEE000] = stack
    observer = bytearray(b'\xBD'+pack32(1)+b'\xBB'+pack32(0))
    regions = [region(a, b) for a, b in memory.items()]
    shadow = 0x11000000
    for i, (a, wanted) in enumerate(checks.items()):
        regions.append(region(shadow, wanted))
        observer += compare_code(a, shadow, len(wanted))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow += ((len(wanted)+255)//256)*256
    observer += bytes.fromhex('8b15')+pack32(f.OUT+16)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    regions.append(region(f.OBS, observer))
    spans = sorted((int(r['address'], 16), len(bytes.fromhex(r['hex']))) for r in regions)
    assert all(a+n <= b for (a, n), (b, _) in zip(spans, spans[1:]))
    args.update(address='0x51dc70', memory=json.dumps(dict(regions=regions)),
                return_registers='EAX,EBP,EBX,ECX,EDX,EIP,ESP')
    return args, dict(outcome=e, checks=[dict(address=hex(a), bytes=len(b)) for a, b in checks.items()])


async def main():
    selected = cases()
    if len(sys.argv) > 1:
        selected = [c for c in selected if c['name'] == sys.argv[1]]
        assert selected
    params = StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),
        args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'), '--transport', 'stdio'],
        env={**os.environ, 'GHIDRA_MCP_URL': 'http://127.0.0.1:8089'})
    rows = []
    async with stdio_client(params) as (rd, wr):
        async with ClientSession(rd, wr) as session:
            await session.initialize()
            for c in selected:
                args, expected = build(c)
                tag = hashlib.sha256(json.dumps(args, sort_keys=True).encode()).hexdigest()[:10]
                path = ROOT/'ghidra'/f'phase105-attack-{c["name"]}-{tag}.json'
                if path.exists():
                    value = json.loads(path.read_text(encoding='utf8'))
                    assert value['input'] == args and value['expected'] == expected
                else:
                    response = await session.call_tool('emulate_function', args)
                    payload = '\n'.join(x.text for x in response.content if x.type == 'text')
                    try:
                        result = json.loads(payload)
                    except ValueError:
                        path.with_suffix('.error.txt').write_text(payload, encoding='utf8')
                        raise
                    value = dict(case=c, input=args, expected=expected, result=result.get('data', result))
                    path.write_text(json.dumps(value, indent=2), encoding='utf8')
                result = value['result']
                assert result['success'] and result['hit_return'], (c['name'], result)
                assert all(int(result['registers'][k], 16) == v for k, v in
                           dict(EAX=1, EBP=1, EBX=0, ECX=0, EDX=expected['outcome']['hp']).items()), (c['name'], result, expected['outcome'])
                rows.append(dict(case=c['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
                print(c['name'], expected['outcome'], 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase105-melee-attack-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
