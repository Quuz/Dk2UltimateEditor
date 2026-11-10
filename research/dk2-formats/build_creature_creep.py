"""Native Creatures Ability Creep Percentage, integer metric and editor narrowing."""
from pathlib import Path
import json,re
from build_effect_semantics import asm_range,pe_read
from build_phase17_semantics import proof
from emulate_creature_creep import KERNELS

ROOT=Path(__file__).resolve().parent

def main():
    kernels=[]
    for key,entry,start,end,register in KERNELS:
        name='target' if key=='target' else 'move-'+key[0]
        file=f'phase24-creep-{name}-assembly.txt';lines=(ROOT/'ghidra'/file).read_text().splitlines()
        rows=[(int(s[:8],16),s) for s in lines if re.match(r'^[0-9a-f]{8}:',s)]
        flags=[a for a,line in rows if a<start and ' + 0x3e6]' in line];assert flags
        begin=max(flags);body=asm_range(file,begin,end-1)
        assert any(' + 0x760]' in line for line in body['instructions'])
        assert any(' + 0x2a3]' in line for line in body['instructions'])
        assert any('MUL '+register in line for line in body['instructions'])
        kernels.append(dict(key=key,function=entry,subject='self in this helper' if key=='target' else 'candidate being examined',
            emulator_start=start,end_exclusive=end,distance_register=register,evidence=body))
    labels=json.loads((ROOT/'editor-strings.json').read_text())['strings'];label=next(r for r in labels if r['id']==0xF85C)
    assert label['text']=='Ability Creep Percentage'
    game=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    binding=pe_read(game,0x509A96,10);assert binding.hex()=='c70554416e0050256f00'
    schema=dict(family='Creatures',record_variants=[5449,5537],raw_offset=0xEE4,raw_bytes=4,baseline_name='Unkee4',
        native_editor_label=label['text'],label_resource=label,raw_representation='Preserve all32 bits; this field is an integer percentage, without /4096.',
        executable_sha256=json.loads((ROOT/'executable-hash-verification.json').read_text()),
        game_storage=dict(converter=0x568E30,intermediate_offset=0x12D4,presence_offset=0x208,loader=0x492320,runtime_offset=0x760,runtime_bytes=4,
            evidence=[asm_range('phase24-creature-converter-assembly.txt',0x568E42,0x568E42),asm_range('phase24-creature-converter-assembly.txt',0x5694A3,0x5694AF),asm_range('game-creature-type-assembly.txt',0x493BDD,0x493BED)]),
        stat_override=dict(variable_type=77,stat_id=26,setter=0x494A70,rule='Setter dispatch narrows stat ID to u8, then case26 copies all32 supplied value bits to runtime+760, without TPS/fixed-point/byte conversion.',
            evidence=asm_range('phase24-stat-setter-assembly.txt',0x494C18,0x494C23)),
        creeping_state=dict(instance_offset=0x3E6,mask=0x1000,native_debug_label='Creeping',level_offset=0x403,definition_pointer_offset=0x370,
            evidence=proof('phase24-creature-state-dump.txt','s_Creeping__d_')),
        level_multiplier=dict(variable_type=65,stat_id=26,table_pointer_address=0x6E4154,table_offset=0x2A3,item_bytes=2,index='raw instance level byte; only level1 skips the table',read_signed=True,
            normal_pointer_binding=dict(address=0x509A96,bytes=binding.hex(),instruction='MOV dword ptr [0x6E4154],0x6F2550'),
            variable_rule='Type65 handler stores low16(ValueRaw) at0x6F261D +2*(statID*9 +level)+2. For stat26 this equals0x6F2550+0x2A3+2*level.',
            evidence=proof('game-variable65.txt','*(short *)((int)&DAT_006f261d')),
        arithmetic=dict(gate='Only while Creeping flag1000 is set; otherwise incoming metric bits unchanged.',
            metric='XY signed32 coordinate differences wrap32. Each signed64 square is shifted right12 and narrowed32; the two terms add modulo2^32. This is squared distance in Q12.',
            percentage='Level1: runtime DWORD bits. Other level bytes: trunc_to_zero(s32(low32(s16(levelMultiplier) * s32(rawPercentage)))/100).',
            weight='weighted = unsigned32(low32(incomingMetric * effectivePercentage)) //100. Low32 product occurs BEFORE the unsigned division.',
            bounds='No clamp to0..100 or nonnegative values in these fragments. Value0 produces metric0 when active.',
            sight='A later numeric sight gate compares signed32(metric) < signed32(low32(radiusTiles*radiusTiles*4096)); equality is rejected.',
            scope='Other faction, list, status, class-weight, nearest-candidate and LOS gates are outside this arithmetic projection.',
            distance_evidence=asm_range('phase24-creep-move-a-assembly.txt',0x45B232,0x45B27F),
            sight_evidence=asm_range('phase24-creep-target-assembly.txt',0x48F00D,0x48F026)),
        kernels=kernels,
        separate_movement_speed=dict(function=0x48ABB0,mode2_runtime_offset=0x5BC,raw_offset=0xD37,native_label='Shuffle Speed (Tiles Per Second)',
            scope='First-person input selects speed mode2 when Creeping. Inspected speed helper selects runtime5BC, not EE4/runtime760. Other health/terrain/possession modifiers still apply.',
            evidence=[asm_range('phase24-creep-speed-assembly.txt',0x48ABB0,0x48ABDE),proof('phase24-first-person-input.txt','iVar7 = FUN_0048abb0(2);')]),
        action=dict(function=0x517310,rule='If player possessed-creature WORD+134 is nonzero: flags=(flags & FFFFEFFF) | ((actionDWORD0 &1)<<12). Otherwise flags unchanged. Other state bits preserved.',
            evidence=asm_range('phase24-creep-action-assembly.txt',0x517310,0x517357)),
        native_editor=dict(intermediate_offset=0x12E4,presence_offset=0x208,model_offset=0x1740,model_bytes=1,panel_offset=0x1880,property_id=0xF85C,
            callback=0x4A01B0,writer=0x4A7F00,writer_record_bytes=5537,
            load='When presence nonzero, callback stores low8 of raw DWORD in model byte. Native converter sets presence for a complete record.',
            display='Property F85C reads model/panel as unsigned byte and presents it numerically.',
            update='After __ftol, update case stores low8 of its integer result. No full GUI float parser or validation range is claimed.',
            write='Writer zeros ECX, loads model byte into CL, stores full ECX DWORD. Present-field callback -> save projection is raw &255, including an unchanged property.',
            preservation='An independent editor should retain all32 original bits unless an explicit user edit or native-compatibility normalization is requested.',
            evidence=[proof('creature-record-decoder.txt','uVar3 = *(undefined4 *)(param_1 + 0xee4);'),
                proof('creature-record-decoder.txt','*(undefined4 *)((int)this + 0x208) = 1;'),
                proof('creature-record-decoder.txt','*(undefined4 *)((int)this + 0x12e4) = uVar3;'),
                proof('phase17-creature-editor-model.txt','*(char *)(this + 0x5d0) = (char)piVar1[0x4b9];'),
                proof('phase17-creature-main-panel-fill.txt','+ 0x1880) = *(undefined1 *)(*(int *)((int)this + 0x134) + 0x1740);'),
                proof('phase17-creature-f55-panel-extract.txt','case 0xf85c:'),
                proof('phase17-creature-f55-panel-extract.txt','*(double *)((int)param_1 + 0x60) = (double)*(byte *)((int)param_1 + 0x1880);'),
                proof('phase17-creature-f55-panel-update.txt','*(char *)(param_1 + 0x1880) = (char)lVar2;'),
                proof('phase17-creature-f55-panel-update.txt','case 0xf85c:'),
                proof('phase17-creature-main-panel-commit.txt','+ 0x1740) = *(undefined1 *)(param_1 + 0x1880);'),
                asm_range('phase24-editor-creature-model-assembly.txt',0x4A0F54,0x4A0F62),
                asm_range('phase17-creature-writer-assembly.txt',0x4A84B4,0x4A84C3),
                asm_range('phase17-creature-writer-assembly.txt',0x4A8B7D,0x4A8B84)]),
        native_emulations=json.loads((ROOT/'phase24-creep-emulations.json').read_text())['cases'],
        limitations=['Exact inspected EXE hashes only; no full-game behavioral or rendering validation.',
            'Kernels receive a precomputed metric and explicit level multiplier. Entry TEST adapter supplies the skipped Creeping flags condition; f1/f2 receive the original cached metric slot.',
            'Game pipeline chains original converter/loader fragments. Editor pipeline chains original callback/writer fragments; external construction, UI dispatch and resource loading skipped.',
            'Other previously unknown Creatures fields and the four Terrain/Traps/Objects scalars are not assigned new semantics by this result.',
            'Third-party template labels were leads; native debug text, PE resource, exact instructions and emulations establish the reported meaning.'])
    assert len(schema['native_emulations'])==116 and len(kernels)==11
    (ROOT/'creature-creep-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8')
    print('Creatures EE4:11 native weight sites, Creeping state, Stat26, editor byte narrowing;116 native runs')

if __name__=='__main__':main()
