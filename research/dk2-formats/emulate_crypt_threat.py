"""Native65/stat3 -> complete45E4F0 Threat distribution, or45C5D0 addition.

Prepared source/targets use actual phase90 class snapshots and original vtables.
Same-XY visibility runs original4512F0. Optional original Crypt setter changes
source/target owner counters before distribution; no gameplay stub or IO needed.
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
from native_threat import emitted, received
from native_room_counters import counter_slot, change_counter

ROOT = Path(__file__).resolve().parent
TARGET = 0x10500000
TARGET2 = 0x10510000
TARGET_OWNER = 0x10520000
ROOM = 0x10530000
MEMBER = 0x10540000


def cases():
    base = dict(mode='distribute', threat=100, level=1, coefficient=125, vampire=0, unique=0,
                maiden=0, slots=[['special', 101]], target_initial=0, target_threshold=50,
                source_initial=100, heart=[0, 0], target_alive=True, source_state=1,
                source_word44b=0, direct_delta=100)
    variants = [('base', {}), ('vampire1', dict(vampire=1)), ('vampire2', dict(vampire=2)),
                ('unique1-alone', dict(unique=1)), ('unique3-alone', dict(unique=3)),
                ('both1', dict(vampire=1, unique=1)), ('vampire1-unique3', dict(vampire=1, unique=3)),
                ('vampire2-unique1', dict(vampire=2, unique=1)),
                ('maiden1', dict(maiden=1)), ('both1-maiden1', dict(vampire=1, unique=1, maiden=1)),
                ('maiden10', dict(maiden=10)), ('maiden11-wrap', dict(maiden=11)),
                ('vampire128-signed', dict(vampire=128)), ('unique128-signed', dict(vampire=1, unique=128)),
                ('level2', dict(level=2)), ('level2-both', dict(level=2, vampire=1, unique=1)),
                ('threshold-equal', dict(target_threshold=100)),
                ('target-sum-wrap', dict(target_initial=65500)),
                ('heart-near', dict(heart=[5, 6])), ('heart-distance-clamp', dict(heart=[30, 30])),
                ('two-slots', dict(slots=[['lower0', 101], ['upper0', 102]])),
                ('duplicate-slots', dict(slots=[['special', 101], ['lower0', 101], ['upper0', 101]])),
                ('inactive-counts-in-denominator', dict(slots=[['special', 101], ['lower0', 103]])),
                ('no-slots', dict(slots=[])), ('target-inactive', dict(target_alive=False)),
                ('source-state-gate', dict(source_state=209)), ('source-word44b-gate', dict(source_word44b=1)),
                ('direct-base', dict(mode='add')), ('direct-maiden1', dict(mode='add', maiden=1)),
                ('direct-maiden11', dict(mode='add', maiden=11)),
                ('crypt-vampire-insert', dict(crypt_species=8, crypt_initial=0, crypt_operation='insert', crypt_owner=7)),
                ('crypt-unique-alone', dict(crypt_species=42, crypt_initial=0, crypt_operation='insert', crypt_owner=7)),
                ('crypt-unique-with-vampire', dict(vampire=1, crypt_species=42, crypt_initial=0, crypt_operation='insert', crypt_owner=7)),
                ('crypt-unique-clear', dict(vampire=1, unique=1, crypt_species=42, crypt_initial=1, crypt_operation='clear', crypt_owner=7)),
                ('crypt-maiden-target', dict(crypt_species=45, crypt_initial=0, crypt_operation='insert', crypt_owner=8)),
                ('crypt-maiden-source-owner', dict(crypt_species=45, crypt_initial=0, crypt_operation='insert', crypt_owner=7)),
                ('threat-zero', dict(threat=0)), ('threat200', dict(threat=200)),
                ('current-level-record', dict(level=2, source_current=True))]
    return [dict(base, **changes, name=name) for name, changes in variants]


def slot_offset(name):
    if name == 'special':
        return 0x502
    prefix, index = name[:-1], int(name[-1])
    assert prefix in ['lower', 'upper'] and 0 <= index < 8
    return (0x504 if prefix == 'lower' else 0x514)+2*index


def outcome(c):
    vampire, unique, maiden = c['vampire'], c['unique'], c['maiden']
    crypt = None
    if 'crypt_species' in c:
        slot = counter_slot(c['crypt_species'])
        count = change_counter(c['crypt_initial'], 1 if c['crypt_operation'] == 'insert' else -1)
        if c['crypt_owner'] == 7:
            if slot == 0x108E:
                vampire = count
            elif slot == 0x108F:
                unique = count
        elif slot == 0x108D:
            maiden = count
        crypt = dict(slot=slot, count=count, tag=110 if c['crypt_operation'] == 'insert' else 0,
                     room55=1234 if c['crypt_operation'] == 'insert' else 0)
    enabled = c['source_state'] not in [209, 255, 256, 257] and c['source_word44b'] == 0
    gain = c['direct_delta'] if c['mode'] == 'add' else emitted(c['threat'], c['level'], c['coefficient'], len(c['slots']), vampire, unique)
    adjusted = received(gain, maiden) if gain is not None else None
    additions = {101: 0, 102: 0}
    if c['mode'] == 'add':
        additions[101] = 1
    elif enabled:
        for _, tag in c['slots']:
            if tag == 101 and c['target_alive'] or tag == 102:
                additions[tag] += 1
    target_states = {}
    for tag in [101, 102]:
        n = additions[tag]
        initial = c['target_initial']
        target_states[str(tag)] = dict(value=(initial+(adjusted or 0)*n)&65535,
                                  source=100 if n and adjusted > c['target_threshold'] else 999)
    distance = min(10, max(abs(5-c['heart'][0]), abs(6-c['heart'][1])))
    result = dict(gain=gain, adjusted=adjusted, targets=target_states,
                  source524=0 if c['mode'] == 'distribute' and enabled else c['source_initial'],
                  source526=c['source_initial']*distance//10 if c['mode'] == 'distribute' and enabled else 12)
    if crypt is not None:
        result['crypt'] = crypt
    return result


def build(c):
    args, _ = sp.build(dict(sp.cases()[0], possessed=False))
    allmem = {int(r['address'], 16): bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    memory = {a: b for a, b in allmem.items() if a < 0x11000000 and a not in [f.OBS, sp.DRIVER, 0x7FFEE000]}
    e = outcome(c)
    memory[sp.RAW] = bytearray(bytes(16)+struct.pack('<4I', 65, 3, c['coefficient'], c['level'])+bytes([0xA5])*64)
    if c.get('source_current'):
        doc = json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text(encoding='utf8'))
        data = Path(doc['source']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == doc['sha256']
        row = next(r for r in doc['sections'][0]['records'] if r['words_u32'] == [65, 3, 125, 2])
        blob = bytes.fromhex(row['raw_hex'])
        assert data[row['offset']:row['offset']+16] == blob
        memory[sp.RAW][16:32] = blob
    f.put(memory[cf.DEFINITION], 0x698, c['threat'], 2)
    f.put(memory[cf.ACTOR], 0x403, c['level'], 1)
    f.put(memory[cf.ACTOR], 0x25E, c['source_state'])
    f.put(memory[cf.ACTOR], 0x44B, c['source_word44b'], 2)
    f.put(memory[cf.ACTOR], 0x524, c['source_initial'], 2)
    f.put(memory[cf.ACTOR], 0x526, 12, 2)
    for name, tag in c['slots']:
        f.put(memory[cf.ACTOR], slot_offset(name), tag, 2)
    f.put(memory[f.PLAYER], 0x108E, c['vampire'], 1)
    f.put(memory[f.PLAYER], 0x108F, c['unique'], 1)
    struct.pack_into('<2H', memory[f.PLAYER], 0xB43, *c['heart'])
    memory[TARGET_OWNER] = bytearray(0x1200)
    f.put(memory[TARGET_OWNER], 0x108D, c['maiden'], 1)
    f.put(memory[0x6EE540], 8*4, TARGET_OWNER)
    f.put(memory[0x6ED540], 8, 1, 1)
    snapshot = (ROOT/'ghidra/phase90-creature-factory-base.bin').read_bytes()
    for tag, address in [(101, TARGET), (102, TARGET2)]:
        memory[address] = bytearray(snapshot)+bytearray([0xA5])*256
        for off, value, width in [(4, tag, 2), (0x28, 8, 2), (0x2A, TARGET_OWNER, 4),
                                  (0x524, c['target_initial'], 2), (0x533, 999, 2),
                                  (0x535, c['target_threshold'], 2)]:
            f.put(memory[address], off, value, width)
        f.put(memory[0x6EE540], tag*4, address)
        f.put(memory[0x6ED540], tag, int(tag == 102 or c['target_alive']), 1)
    if 'crypt' in e:
        room = bytearray(0xC0)+bytearray([0xA5])*64
        for off, value, n in [(4, 200, 2), (0x18, 26, 1), (0x1D, c['crypt_owner'], 2),
                               (0x53, 110 if c['crypt_operation'] == 'clear' else 0, 2), (0x55, 1234, 4)]:
            f.put(room, off, value, n)
        memory[ROOM] = room
        member = bytearray(0x380)+bytearray([0xA5])*64
        f.put(member, 4, 110, 2)
        f.put(member, 0x36F, c['crypt_species'], 1)
        memory[MEMBER] = member
        for tag, address in [(200, ROOM), (110, MEMBER)]:
            f.put(memory[0x6EE540], tag*4, address)
            f.put(memory[0x6ED540], tag, 1, 1)
        owner = f.PLAYER if c['crypt_owner'] == 7 else TARGET_OWNER
        f.put(memory[owner], e['crypt']['slot'], c['crypt_initial'], 1)
    memory[f.OUT] = bytearray(8)+bytearray([0xA5])*24
    checks = {a: bytearray(b) for a, b in memory.items() if a not in [f.GFX_CB, f.SOUND_CB, 0x28CFA4]}
    f.put(checks[sp.VARS], 0x105+2*c['level'], c['coefficient'], 2)
    for off in [0x524, 0x526]:
        f.put(checks[cf.ACTOR], off, e[f'source{off:x}'], 2)
    for tag, address in [(101, TARGET), (102, TARGET2)]:
        f.put(checks[address], 0x524, e['targets'][str(tag)]['value'], 2)
        f.put(checks[address], 0x533, e['targets'][str(tag)]['source'], 2)
    if 'crypt' in e:
        f.put(checks[ROOM], 0x53, e['crypt']['tag'], 2)
        f.put(checks[ROOM], 0x55, e['crypt']['room55'])
        f.put(checks[owner], e['crypt']['slot'], e['crypt']['count'], 1)
    struct.pack_into('<2I', checks[f.OUT], 0, 1, e['targets']['101']['value'])
    driver = bytearray(b'\xA3'+pack32(f.OUT))
    def emit(b):
        driver.extend(b)
    def call(a):
        emit(b'\xE8'+pack32(a-(sp.DRIVER+len(driver)+5)))
    if 'crypt' in e:
        emit(b'\x68'+pack32(e['crypt']['tag'])+b'\xB9'+pack32(ROOM))
        call(0x4D8E60)
    if c['mode'] == 'add':
        emit(b'\x68'+pack32(100)+b'\x68'+pack32(c['direct_delta'])+b'\xB9'+pack32(TARGET))
        call(0x45C5D0)
    else:
        emit(b'\xB9'+pack32(cf.ACTOR))
        call(0x45E4F0)
    emit(bytes.fromhex('0fb705')+pack32(TARGET+0x524)+b'\xA3'+pack32(f.OUT+4)+b'\x68'+pack32(f.OBS)+b'\xC3')
    memory[sp.DRIVER] = driver
    stack = bytearray([0xA5])*16384
    struct.pack_into('<2I', stack, 8192, sp.DRIVER, sp.RAW)
    memory[0x7FFEE000] = stack
    observer = bytearray(b'\xBD'+pack32(1)+b'\xBB'+pack32(0))
    regions = [region(a, b) for a, b in memory.items()]
    shadow = 0x11000000
    for i, (a, expected) in enumerate(checks.items()):
        regions.append(region(shadow, expected))
        observer += compare_code(a, shadow, len(expected))+bytes.fromhex('85c07505bb')+pack32(i+1)+bytes.fromhex('21c5')
        shadow += ((len(expected)+255)//256)*256
    observer += bytes.fromhex('8b15')+pack32(f.OUT+4)+b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
    regions.append(region(f.OBS, observer))
    spans = sorted((int(r['address'], 16), len(bytes.fromhex(r['hex']))) for r in regions)
    assert all(a+n <= b for (a, n), (b, _) in zip(spans, spans[1:]))
    args.update(address='0x51dc40', memory=json.dumps(dict(regions=regions)),
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
                path = ROOT/'ghidra'/f'phase106-threat-{c["name"]}-{tag}.json'
                if path.exists():
                    value = json.loads(path.read_text(encoding='utf8'))
                    assert value['input'] == args and value['expected'] == expected
                else:
                    response = await session.call_tool('emulate_function', args)
                    result = json.loads('\n'.join(x.text for x in response.content if x.type == 'text'))
                    value = dict(case=c, input=args, expected=expected, result=result.get('data', result))
                    path.write_text(json.dumps(value, indent=2), encoding='utf8')
                result = value['result']
                assert result['success'] and result['hit_return'], (c['name'], result)
                assert all(int(result['registers'][k], 16) == v for k, v in
                           dict(EAX=1, EBP=1, EBX=0, ECX=0, EDX=expected['outcome']['targets']['101']['value']).items()), (c['name'], result, expected['outcome'])
                rows.append(dict(case=c['name'], file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
                print(c['name'], expected['outcome'], 'verified', flush=True)
    if len(selected) == len(cases()):
        (ROOT/'phase106-threat-emulations.json').write_text(json.dumps(dict(scope=__doc__, replays=rows), indent=2), encoding='utf8')


if __name__ == '__main__':
    asyncio.run(main())
