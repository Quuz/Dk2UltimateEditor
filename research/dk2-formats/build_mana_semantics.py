"""Verified Terrain income, per-second ceiling and signed Trap upkeep consumers."""
from pathlib import Path
import json
from build_effect_semantics import asm_range
from build_phase17_semantics import proof

ROOT=Path(__file__).resolve().parent

def main():
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families'];fields=[]
    for family,offset,name,role in [
        ('Terrain',0x19C,'ManaGain','Signed contribution to player mana income accumulator+0x10E2 on ownership update.'),
        ('Terrain',0x19E,'MaxManaGain','Signed contribution to player maximum mana income per second+0x10DE; not stored-mana capacity.'),
        ('Traps',0x202,'ManaUsage','Signed upkeep contribution to player+0x10D6, subtracted from player income+0x10E2 when registered.')]:
        source=paths[family];p=next(p for p in source['joined_paths'] if int(p['raw_offset'],16)==offset)
        fields.append(dict(family=family,name=name,raw_offset=offset,raw_bytes=2,raw_storage='Preserve the original little-endian16-bit pattern; semantic view is signed16.',
            intermediate_offset=int(p['intermediate_offset'],16),runtime_offset=int(p['runtime_offset'],16),role=role,
            evidence=[proof(source['converter_file'],p['converter_evidence'][0]['assignment']),
                proof(source['converter_file'],p['converter_evidence'][-1]['assignment']),
                proof(source['runtime_file'],p['runtime_evidence']['assignment'])]))
    dump='phase23-player-mana-dump.txt'
    names=[]
    for offset,label in [(0x82,'CurrentMana'),(0x86,'ManaIncreaseRate'),(0x9A,'ManaGainTick'),
        (0x10DE,'MaxManaGainPerSecond'),(0x10E2,'ManaGainPerSecond'),(0x10E6,'BasicManaPerSecond'),
        (0x10EA,'MaxMana'),(0x10EE,'OneShotManaGain'),(0x10F2,'OneShotManaLoss')]:
        names.append(dict(offset=offset,bytes=4,native_debug_label=label,evidence=proof(dump,'s_'+label+'__d_')))
    schema=dict(program='DKII-DX.exe',program_sha256=json.loads((ROOT/'catalogue-loading.json').read_text())['program_sha256'],
        fields=fields,player_fields=names,
        ownership=dict(function=0x4559D0,node_owner_word_offset=8,node_terrain_id_offset=10,
            compare='Compare owner_word&0xFFF with low16(requested_owner). Assignment changes low12 owner bits only and preserves upper nibble.',
            different_owner='If old owner!=0, subtract signed Terrain+187/+189 from old player. Always add both signed values to the new player, including new owner0.',
            same_owner='If owner!=0 and world byte+7239 bit80 is set, add both values again; otherwise these accumulators are unchanged.',
            side_effect_scope='Also calls0x4BE070 and has game-mode4 side effects. Projection only returns mana field deltas; mode4 was disabled and0x4BE070 stubbed in isolated emulation.',
            evidence=asm_range('phase23-terrain-mana-node-update-assembly.txt',0x4559D0,0x455B7F)),
        terrain_transition=dict(function=0x455B90,room_flag=0x10,world_skip_flag=0x40,
            ordinary='If neither definition has ROOM and owner!=0 and world+7239 bit40 clear: subtract old signed gain unless old terrain ID0; add new signed gain.',
            room_branch='If either definition has ROOM: new ID17/26 adds1; otherwise old ID17/26 subtracts1. These branches do not test owner!=0 or world bit40.',
            precedence='New special ID branch wins, including17->26. Same old/new ID skips all changes.',
            ceiling='No writes to player+10DE in this helper. Do not infer that other callers or later rebuilds cannot update it.',
            id_update='Changed IDs are written to node+0xA even when mana adjustment is skipped.',
            evidence=asm_range('phase23-terrain-mana-node-helper-assembly.txt',0x455B90,0x455C93)),
        income=dict(preview_function=0x4C1770,tick_function=0x4C10E0,cost_helper=0x4C17A0,
            gross='min_signed32(s32(extra + basic + helper_cost + one_shot_gain), income_ceiling)',
            net_tick='Clear both one-shot DWORDs; compute gross with first cost-helper result; subtract second cost-helper result; multiply only positive net by player+86.',
            helper='0x4C17A0 includes imp/trap upkeep and conditional ability/possession costs; it is not a constant. Projection takes its two return values separately.',
            mode_branch='If global DWORD75AD4B==3: setting WORD75AEA7==2 doubles low32(delta); setting0 computes signed low32(delta*2), then adds sign-bias3 and SAR2. Numeric conditions only; mode labels not inferred.',
            stored_mana='Add final signed delta modulo2^32. Negative delta clamps only a negative result to0; nonnegative delta clamps only above player+10EA. Extreme overflow is preserved in the projection.',
            deadline='Unsigned now >= low32(last_tick+ticks_per_second); normal end of function writes now to player+9A. Early-return side paths are outside projection.',
            evidence=[asm_range('phase23-player-mana-tick-assembly.txt',0x4C1770,0x4C179F),
                asm_range('phase23-player-mana-income-assembly.txt',0x4C1109,0x4C1121),
                asm_range('phase23-player-mana-income-assembly.txt',0x4C1127,0x4C11E7),
                asm_range('phase23-player-mana-income-assembly.txt',0x4C16B1,0x4C16B1)]),
        trap_upkeep=dict(add_function=0x4C1B60,remove_function=0x4C1B90,
            rule='Signed16 usage from definition+96; add: maintain+=usage, extra-=usage. Remove reverses these operations. All DWORD arithmetic wraps32.',
            unknown_neighbor='raw+204/runtime+98 remains unresolved and is not consumed by these handlers.',
            caller_scope='4A7CE0 adds after a successful preceding helper only if instance+10C bit20 is clear.4A7B20 removes under its external zero-result gate and the same bit-clear condition.4A96D0 clears bit20 and adds in either TriggerType branch.4A96A0 calls remove then add around other binding helpers; those helpers are not fully analyzed here.',
            caller_evidence=[proof('phase23-trap-upkeep-add-path.txt','if ((*(byte *)(param_1 + 0x10c) & 0x20) == 0)'),
                proof('phase23-trap-upkeep-remove-path.txt','FUN_004c1b90(param_1);'),
                proof('phase23-trap-owner-upkeep.txt','FUN_004c1b90(param_1);'),
                proof('phase23-trap-owner-upkeep.txt','FUN_004c1b60(param_1);'),
                proof('phase23-trap-upkeep-refresh.txt','*(byte *)(param_1 + 0x10c) = *(byte *)(param_1 + 0x10c) & 0xdf;')],
            evidence=[asm_range('phase23-upkeep-add-assembly.txt',0x4C1B60,0x4C1B8D),asm_range('phase23-upkeep-remove-assembly.txt',0x4C1B90,0x4C1BBD)]),
        adjusted_pointer_search=dict(tool='scan_adjusted_pointers.py',file='phase23-adjusted-pointer-candidates.json',
            result='38 candidate overlaps in8729 mapped spans; no new semantic consumer established for the four unknown fields.',
            limitation='Short fall-through alias traces only; no whole-program pointer analysis, object identity inference or proof of global non-use.'),
        native_emulations=json.loads((ROOT/'phase23-mana-emulations.json').read_text())['cases'],
        limitations=['No modified gameplay or live renderer run.',
            'All native signedness/rules are for the exact inspected executable hash.',
            'Whole ownership handler external count callback is stubbed; game-mode4 side effects disabled.',
            'Income emulates arithmetic and deadline fragments, not the entire0x4C10E0 function or full cost helper.',
            'Do not derive a complete player/world income solely from terrain records; rooms, variables, creatures, spells and other state also contribute.',
            'Baseline raw schema names/types retained for audit; semantic signed views do not change field width.',
            'Terrain+198/+224, Traps+204 and gameplay effect of Objects+34C remain unresolved.'])
    assert len(schema['native_emulations'])==62
    (ROOT/'mana-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8')
    print('Mana schema:3 signed WORD consumers; ownership/type transitions; per-second ceiling and income arithmetic;62 native runs')

if __name__=='__main__':main()
