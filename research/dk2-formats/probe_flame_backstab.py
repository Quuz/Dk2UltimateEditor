"""Execute the installed Flame DLL's isolated post-angle melee arithmetic.

This is a segment check, not a complete Flame load, melee call, or live game.
The normalized result of the earlier angle helper is supplied in EAX.
"""
from pathlib import Path
import argparse, hashlib, json, struct, sys
ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT/'dependencies/unicorn-2.1.4'))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_MEM_READ
from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_EBP, UC_X86_REG_EDI, UC_X86_REG_ESP, UC_X86_REG_EIP
import native_melee_direction as original

START, STOP = 0x100201FA, 0x1002027C
FLAG, TARGET, STACK = 0x103DE560, 0x05000000, 0x07000000
sha = lambda b: hashlib.sha256(b).hexdigest()


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--dll', type=Path, default=Path(r'D:\Games\DK2\Dungeon Keeper 2\flame\Flame.dll'))
    ap.add_argument('--config', type=Path, default=Path(r'D:\Games\DK2\Dungeon Keeper 2\flame\config.toml'))
    args = ap.parse_args()
    raw = args.dll.read_bytes()
    expected_sha = '5df10c4c4ecdb168b3d12314e47d6b347db85b9aa1e2adf2280582043ec02ce1'
    assert sha(raw) == expected_sha, 'Re-attest segment addresses for a different DLL build'
    pe = pefile.PE(data=raw)
    assert pe.OPTIONAL_HEADER.ImageBase == 0x10000000
    symbols = {s.name: s.address for s in pe.DIRECTORY_ENTRY_EXPORT.symbols}
    assert symbols[b'?processDealDamage@CCreature@dk2@@QAEHXZ'] == 0x1FF60
    segment = pe.get_data(START - 0x10000000, STOP - START)
    instructions = [dict(address=hex(x.address), hex=x.bytes.hex(), instruction=x.mnemonic+' '+x.op_str)
                    for x in Cs(CS_ARCH_X86, CS_MODE_32).disasm(segment, START)]
    assert instructions[-1]['address'] == '0x1002027a'
    uc = Uc(UC_ARCH_X86, UC_MODE_32)
    uc.mem_map(0x10020000, 0x1000)
    uc.mem_write(0x10020000, pe.get_data(0x20000, 0x1000))
    for base in [FLAG & ~0xFFF, TARGET, STACK]:
        uc.mem_map(base, 0x1000)

    def run(angle, heading, enabled, base=100):
        uc.mem_write(FLAG, bytes([enabled]))
        uc.mem_write(TARGET+0xF4, struct.pack('<H', heading))
        for register, value in [(UC_X86_REG_EAX, angle), (UC_X86_REG_EBP, (-base)&0xFFFFFFFF),
                                (UC_X86_REG_EDI, TARGET), (UC_X86_REG_ESP, STACK+0x800)]:
            uc.reg_write(register, value)
        uc.emu_start(START, STOP, count=100)
        assert uc.reg_read(UC_X86_REG_EIP) == STOP
        return -struct.unpack('<i', struct.pack('<I', uc.reg_read(UC_X86_REG_EBP)))[0]

    outcomes = bytearray()
    checked = 0
    headings = [0, 1, 319, 512, 1024, 1536, 2046, 2047]
    for enabled in [False, True]:
        for heading in headings:
            for angle in range(2048):
                result = run(angle, heading, enabled)
                relative = (heading+2048-angle)%2048 if enabled else angle
                assert result == original.damage(100, relative), (angle, heading, enabled, result)
                outcomes.extend(struct.pack('<H', result))
                checked += 1
    rotation_checks = 0
    for heading in range(2048):
        for relative in [0, 1, 319, 320, 512, 1023, 1024, 1536, 1726, 1727, 2046, 2047]:
            assert run((heading-relative)&2047, heading, True) == original.damage(100, relative)
            rotation_checks += 1
    extreme_base_checks = 0
    for base in [0, 1, 99, 65535]:
        for relative in range(2048):
            assert run((-relative)&2047, 0, True, base) == original.damage(base, relative)
            extreme_base_checks += 1

    reads = []
    hook = uc.hook_add(UC_HOOK_MEM_READ, lambda _, access, address, size, value, user: reads.append(dict(address=hex(address), size=size)))
    uc.ctl_remove_cache(START, STOP)
    run(0, 1024, True)
    enabled_reads = reads.copy(); reads.clear()
    run(0, 1024, False)
    disabled_reads = reads.copy()
    uc.hook_del(hook)
    assert bytes(uc.mem_read(START, len(segment))) == segment
    assert dict(address=hex(TARGET+0xF4), size=2) in enabled_reads
    assert dict(address=hex(TARGET+0xF4), size=2) not in disabled_reads
    examples = []
    for label, angle, heading in [('front',0,0), ('side',512,0), ('rear',1024,0),
                                  ('same_positions_target_turned',0,1024), ('front_seam_a2047',2047,0),
                                  ('front_seam_a0',0,0), ('front_seam_a1',1,0)]:
        examples.append(dict(label=label, world_angle=angle, target_heading=heading,
                             original_damage=original.damage(100,angle),
                             Flame_fix_off=run(angle,heading,False), Flame_fix_on=run(angle,heading,True)))
    import tomllib
    config_raw = args.config.read_bytes()
    config = tomllib.loads(config_raw.decode('utf8'))
    mode = config['flame']['original-compatible']
    original_compatible_store = pe.get_data(0x116316, 7)
    assert original_compatible_store == bytes.fromhex('c60560e53d1000')
    report = dict(schema_version=1, date='2026-11-10', passed=True,
        probe_sha256=sha(Path(__file__).read_bytes()),
        dll=dict(file=str(args.dll), sha256=sha(raw), bytes=len(raw), image_base='0x10000000',
                 processDealDamage='0x1001ff60'),
        segment=dict(start=hex(START), stop_exclusive=hex(STOP), bytes=len(segment),
                     sha256=sha(segment), hex=segment.hex(), instructions=instructions),
        executed=dict(angle_heading_matrix=checked, common_rotation_controls=rotation_checks,
                      additional_u16_base_controls=extreme_base_checks, total=checked+rotation_checks+extreme_base_checks,
                      matrix_output_sha256=sha(outcomes)),
        confirmed=dict(target_heading_read_enabled=enabled_reads, target_heading_read_disabled=disabled_reads,
                       relative_formula='(target_heading + 2048 - normalized_world_angle) % 2048',
                       world_axis_bug_corrected=True, angular_fold_2046_seam_retained=True,
                       original_compatible_clears_fix=dict(pc='0x10116316', hex=original_compatible_store.hex(), flag=hex(FLAG), evidence='static')),
        examples=examples,
        local_config=dict(file=str(args.config), sha256=sha(config_raw), original_compatible=mode,
                          note='Saved value only; no live process/command-line override inspection; no configuration changes.'),
        limits=['Isolated original installed-DLL arithmetic segment executed; EAX angle, EBP base damage, target heading and flag prepared explicitly.',
                'No game function byte patches, no complete Flame function/load/init/scheduler/HP path execution, no live gameplay test.',
                'Earlier angle helper call and later modifiers/HP consumers excluded; original angle helper separately certified in phase194.',
                'Fix-on/fix-off flag values are experimental fixture inputs, not measured live runtime state.'])
    output = ROOT/'flame-backstab-verification.json'
    output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8',newline='\n')
    assert args.dll.read_bytes() == raw and args.config.read_bytes() == config_raw
    print(json.dumps(dict(passed=True, controls=report['executed'], examples=examples, saved_original_compatible=mode),ensure_ascii=False,indent=2))


if __name__ == '__main__':
    main()
